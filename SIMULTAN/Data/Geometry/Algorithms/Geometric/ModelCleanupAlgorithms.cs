﻿using SIMULTAN.Utils;
using SIMULTAN.Utils.BackgroundWork;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides algorithms to clean up a mesh
    /// </summary>
    public static class ModelCleanupAlgorithms
    {
        /// <summary>
        /// Removes duplicate vertices by merging vertices which are closer than a given tolerance.
        /// </summary>
        /// <param name="model">The mode to clean up</param>
        /// <param name="tolerance">The distance below which Vertices are merged</param>
        /// <param name="vertexGrid">Speedup structure containing all vertices</param>
        /// <param name="edgeGrid">Speedup structure containing all edges</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static int RemoveDuplicateVertices(GeometryModelData model, double tolerance,
            ref AABBGrid vertexGrid, ref AABBGrid edgeGrid,
            IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            //It would be good to implement some kind of cluster detection here but due to time reasons only iterative merging
            // is implemented. --bsteiner
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();

            if (vertexGrid == null)
            {
                var aabbs = model.Vertices.Select(x => new AABB(x)).ToList();
                var range = AABB.Merge(aabbs);
                vertexGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                vertexGrid.AddRange(aabbs);
            }

            backgroundInfo.ReportProgress(0);
            int numberVertices = model.Vertices.Count;

            model.StartBatchOperation();
            double t2 = tolerance * tolerance;

            int progressCounter = 0;
            foreach (var cell in vertexGrid.Cells)
            {
                if (cell != null)
                {
                    for (int i = 0; i < cell.Count - 1; ++i) //-1 because last element can't have duplicates afterwards
                    {
                        var vibox = cell[i];
                        var ivertex = (Vertex)vibox.Content;

                        for (int j = i + 1; j < cell.Count; ++j)
                        {
                            var vjbox = cell[j];
                            var jvertex = (Vertex)vjbox.Content;

                            if ((ivertex.Position - jvertex.Position).LengthSquared <= t2)
                            {
                                if (MergeVertices(model, ivertex, jvertex, vjbox, t2, ref vertexGrid, ref edgeGrid))
                                    j--;
                            }
                        }
                    }
                }

                if (backgroundInfo.CancellationPending)
                {
                    model.EndBatchOperation();
                    backgroundInfo.Cancel = true;
                    return 0;
                }

                progressCounter++;
                backgroundInfo.ReportProgress((int)((double)progressCounter / (vertexGrid.Cells.Length) * 100.0));
            }

            RemoveDegeneratedEdgeLoops(model);
            model.EndBatchOperation();

            return (numberVertices - model.Vertices.Count);
        }

        private static bool MergeVertices(GeometryModelData model, Vertex ivertex, Vertex jvertex, AABB jvertexBox,
            double t2, ref AABBGrid vertexGrid, ref AABBGrid edgeGrid)
        {
            //Find all loops that contain i and j
            var iloops = ivertex.Edges.SelectMany(x => x.PEdges).Select(x => x.Parent).Distinct();
            var jloops = jvertex.Edges.SelectMany(x => x.PEdges).Select(x => x.Parent).Distinct().ToList();

            var commonLoops = iloops.Where(x => jloops.Contains(x)).ToList();

            bool canMerge = true;

            foreach (var cLoop in commonLoops)
            {
                var ordered = EdgeAlgorithms.OrderLoop(cLoop.Edges);

                //Find the two sub-loops that are created by the merge, when one of the loops has 0 area, do not merge
                var cLoopVertices = EdgeAlgorithms.OrderedVertexLoop(cLoop.Edges.Select(x => x.Edge));
                int idxi = cLoopVertices.IndexOf(ivertex);
                int idxj = cLoopVertices.IndexOf(jvertex);

                bool isNeighbor = Math.Abs(idxi - idxj) == 1 ||
                    (idxi == 0 && idxj == cLoopVertices.Count - 1) ||
                    (idxj == 0 && idxi == cLoopVertices.Count - 1);

                if (!isNeighbor)
                {
                    var normal = new Vector3D(0, 0, 0);
                    if (cLoop.Faces.Count > 0)
                        normal = cLoop.Faces[0].Normal;

                    var area1 = SubLoopArea(cLoopVertices, idxi, idxj, cLoopVertices.Count, normal);
                    if (area1 <= t2)
                    {
                        canMerge = false;
                    }
                    else
                    {
                        var area2 = SubLoopArea(cLoopVertices, idxj, idxi, cLoopVertices.Count, normal);
                        if (area2 <= t2)
                            canMerge = false;
                    }
                }
            }

            if (canMerge)
            {
                foreach (var e in jvertex.Edges)
                {
                    e.Vertices[e.Vertices.IndexOf(jvertex)] = ivertex;

                    if (!ivertex.Edges.Contains(e))
                    {
                        ivertex.Edges.Add(e);
                    }
                    else
                    {
                        //Remove edge (twice same vertex, 0 length)
                        foreach (var pe in e.PEdges)
                        {
                            pe.Parent.Edges.Remove(pe);
                        }
                        e.RemoveFromModel();

                        if (edgeGrid != null)
                        {
                            AABB eBox = null;
                            var grid = edgeGrid;
                            edgeGrid.ForCell(e.Vertices[0].Position, x => eBox = grid.Cells[x.X, x.Y, x.Z].FirstOrDefault(b => b.Content == e));
                            if (eBox != null)
                                edgeGrid.Remove(eBox);
                        }


                    }
                }

                //Check if geo references are affected
                for (int refi = 0; refi < model.GeoReferences.Count; ++refi)
                {
                    var geoRef = model.GeoReferences[refi];
                    if (geoRef.Vertex == jvertex)
                    {
                        model.GeoReferences[refi] = new GeoReference(ivertex, geoRef.ReferencePoint);

                    }
                }

                model.Vertices.Remove(jvertex);

                //Update grid
                vertexGrid.Remove(jvertexBox);

                return true;
            }

            return false;
        }

        private static double SubLoopArea(List<Vertex> vertices, int start, int end, int mod, Vector3D normal)
        {
            int realEnd = end;
            if (realEnd < start)
                realEnd += mod;

            double area = 0;
            var refPoint = vertices[start].Position;

            for (int i = start + 1; i <= realEnd; ++i)
            {
                var v1 = vertices[i % mod];
                var v2 = vertices[(i + 1) % mod];

                var triNorm = Vector3D.CrossProduct(v1.Position - refPoint, v2.Position - refPoint);
                var triArea = triNorm.Length;
                triNorm.Normalize();

                if (triArea > 0.0001)
                    area += triNorm.Length * Math.Sign(Vector3D.DotProduct(normal, triNorm));
            }

            return Math.Abs(area / 2.0);
        }

        /// <summary>
        /// Removes duplicate edges by merging edges together
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="edgeGrid">Speedup structure containing all edges</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static int RemoveDuplicateEdges(GeometryModelData model, ref AABBGrid edgeGrid, IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            var numberEdges = model.Edges.Count;

            if (edgeGrid == null)
            {
                var aabbs = model.Edges.Select(x => new AABB(x)).ToList();
                var range = AABB.Merge(aabbs);
                edgeGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                edgeGrid.AddRange(aabbs);
            }

            model.StartBatchOperation();

            int progressCounter = 0;

            foreach (var cell in edgeGrid.Cells)
            {
                if (cell != null)
                {
                    for (int i = 0; i < cell.Count; ++i)
                    {
                        var eibox = cell[i];
                        var ei = (Edge)eibox.Content;

                        for (int j = i + 1; j < cell.Count; ++j)
                        {
                            var ejbox = cell[j];
                            var ej = (Edge)ejbox.Content;

                            if (EdgeAlgorithms.IsSimilarEdge(ei, ej))
                            {
                                foreach (var dePEdge in ej.PEdges)
                                {
                                    int idx = dePEdge.Parent.Edges.IndexOf(dePEdge);

                                    if (dePEdge.Parent.Edges.Any(x => x.Edge == ei))
                                    {
                                        dePEdge.Parent.Edges.RemoveAt(idx);
                                    }
                                    else
                                    {
                                        dePEdge.Parent.Edges[idx] = new PEdge(ei, GeometricOrientation.Undefined, dePEdge.Parent);
                                    }
                                }

                                model.Edges.Remove(ej);

                                //Remove from grid
                                edgeGrid.Remove(ejbox);

                                j--;
                            }
                        }
                    }
                }
                if (backgroundInfo.CancellationPending)
                {
                    model.EndBatchOperation();
                    backgroundInfo.Cancel = true;
                    return 0;
                }

                progressCounter++;
                backgroundInfo.ReportProgress((int)((double)progressCounter / (double)edgeGrid.Cells.Length * 100.0));


            }

            RemoveDegeneratedEdgeLoops(model);
            model.EndBatchOperation();

            return (numberEdges - model.Edges.Count);
        }

        /// <summary>
        /// Removes faces with similar edgeloops
        /// </summary>
        /// <param name="model">The model to check</param>
        /// <param name="faceGrid">Speedup structure containing all faces</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static int RemoveDuplicateFaces(GeometryModelData model, ref AABBGrid faceGrid,
            IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            var numFaces = model.Faces.Count;

            if (faceGrid == null)
            {
                var aabbs = model.Faces.Select(x => new AABB(x));
                var range = AABB.Merge(aabbs);
                faceGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                faceGrid.AddRange(aabbs);
            }

            model.StartBatchOperation();

            int progressCounter = 0;
            foreach (var cell in faceGrid.Cells)
            {
                if (cell != null)
                {
                    for (int i = 0; i < cell.Count; ++i)
                    {
                        var fibox = cell[i];
                        var facei = (Face)fibox.Content;

                        for (int j = i + 1; j < cell.Count; ++j)
                        {
                            var fjbox = cell[j];
                            var facej = (Face)fjbox.Content;
                            bool jumpToNexti = false;

                            if (EdgeLoopAlgorithms.IsSimilar(facei.Boundary, facej.Boundary)) //Similar enough -> Merge :)
                            {
                                //Decide which one to keep. If only one face is part of a volume: Keep that.
                                //If both are part of volume and volumes on different layers: Keep the one where nm
                                if (facei.PFaces.Count == 0 && facej.PFaces.Count != 0)
                                {
                                    (facej, facei) = (facei, facej);
                                    jumpToNexti = true;
                                }
                                else if (facei.PFaces.Count > 0 && facej.PFaces.Count > 0)
                                {
                                    if (VolumeAlgorithms.ElevationReference(facei.PFaces[0].Volume).floor < VolumeAlgorithms.ElevationReference(facej.PFaces[0].Volume).floor)
                                    {
                                        (facej, facei) = (facei, facej);
                                        fjbox = fibox;
                                        jumpToNexti = true;
                                    }
                                }

                                if (facej.Id == 35)
                                { }

                                //Merge holes (atm only similar when exactly same edges)
                                List<EdgeLoop> addHoles = new List<EdgeLoop>();
                                foreach (var hj in facej.Holes)
                                {
                                    hj.Faces.Remove(facej);

                                    if (hj.Faces.Contains(facei))
                                    {
                                        //Do nothing. EdgeLoop is already a hole in both faces
                                    }
                                    else if (!facei.Holes.Any(x => EdgeLoopAlgorithms.IsSimilar(x, hj)))
                                        addHoles.Add(hj);
                                    else
                                    {
                                        if (hj.Faces.Count == 0)
                                            hj.RemoveFromModel();
                                    }
                                }

                                facei.Holes.AddRange(addHoles);
                                addHoles.ForEach(x => x.Faces.Add(facei));

                                //Remove face j, replace with i
                                foreach (var pface in facej.PFaces)
                                {
                                    pface.Volume.Faces.Remove(pface);
                                    var newPface = pface.Volume.AddFace(facei);
                                    facei.PFaces.Add(newPface);
                                }

                                //Check if facej is contained as a hole and replace with i (unless i is already there
                                foreach (var containingFace in facej.Boundary.Faces.Where(x => x != facej))
                                {
                                    if (!containingFace.Holes.Contains(facei.Boundary))
                                        containingFace.Holes.Add(facei.Boundary);
                                    containingFace.Holes.Remove(facej.Boundary);
                                }

                                //Remove all loops from j
                                facej.Boundary.RemoveFromModel();
                                facej.RemoveFromModel();

                                //Update grid
                                faceGrid.Remove(fjbox);
                            }

                            if (jumpToNexti)
                            {
                                i--;
                                break;
                            }
                        }
                    }
                }

                if (backgroundInfo.CancellationPending)
                {
                    model.EndBatchOperation();
                    backgroundInfo.Cancel = true;
                    return 0;
                }

                progressCounter++;
                backgroundInfo.ReportProgress((int)((double)progressCounter / (double)faceGrid.Cells.Length * 100.0));
            }

            model.EndBatchOperation();

            return numFaces - model.Faces.Count;
        }

        /// <summary>
        /// Removes duplicate holes
        /// </summary>
        /// <param name="model">The GeometryModel</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static void RemoveDuplicateHoles(GeometryModelData model, IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            model.StartBatchOperation();

            int progressCounter = 0;

            foreach (var face in model.Faces)
            {
                for (int i = 0; i < face.Holes.Count - 1; ++i)
                {
                    var hi = face.Holes[i];

                    for (int j = i + 1; j < face.Holes.Count; ++j)
                    {
                        var hj = face.Holes[j];

                        if (EdgeLoopAlgorithms.IsSimilar(hi, hj))
                        {
                            face.Holes.RemoveAt(j);
                            j--;

                            hj.Faces.Remove(face);
                            if (hj.Faces.Count == 0)
                                hj.RemoveFromModel();
                        }
                    }
                }

                if (backgroundInfo.CancellationPending)
                {
                    model.EndBatchOperation();
                    backgroundInfo.Cancel = true;
                    return;
                }
                backgroundInfo.ReportProgress((int)((double)progressCounter / (double)model.Faces.Count * 100.0));
                progressCounter++;
            }

            model.EndBatchOperation();
        }

        /// <summary>
        /// Removes holesloops which have a similar loop in a face boundary (happens when copying hole faces into existing holes)
        /// </summary>
        /// <param name="model">The GeometryModel</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static int RemoveDuplicateHoleLoops(GeometryModelData model, IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            model.StartBatchOperation();

            int progressCounter = 0;
            int removeCount = 0;

            foreach (var face in model.Faces)
            {
                //Check all holes if there is a similar edgeloop which is the boundary of some other face
                for (int i = 0; i < face.Holes.Count; ++i)
                {
                    var holei = face.Holes[i];

                    if (!holei.Faces.Any(x => x.Boundary == holei))
                    {
                        foreach (var edge in holei.Edges)
                        {
                            var similarLoopEdge = edge.Edge.PEdges.FirstOrDefault(
                                x => x.Parent != holei && x.Parent is EdgeLoop jloop && EdgeLoopAlgorithms.IsSimilar(holei, jloop));
                            if (similarLoopEdge != null)
                            {
                                //Replace
                                var similarLoop = (EdgeLoop)similarLoopEdge.Parent;
                                face.Holes[i] = similarLoop;
                                holei.Faces.Remove(face);

                                if (holei.Faces.Count == 0)
                                {
                                    holei.Edges.ForEach(x => x.Edge.PEdges.Remove(x));
                                    holei.RemoveFromModel();
                                }

                                removeCount++;

                                //Restart same hole
                                i--;
                                break;
                            }
                        }
                    }
                }

                if (backgroundInfo.CancellationPending)
                {
                    model.EndBatchOperation();
                    backgroundInfo.Cancel = true;
                    return 0;
                }
                backgroundInfo.ReportProgress((int)((double)progressCounter / (double)model.Faces.Count * 100.0));
                progressCounter++;
            }

            model.EndBatchOperation();

            return removeCount;
        }

        /// <summary>
        /// Removes duplicate volumes from a model.
        /// Duplicate volumes are volumes that have the exact same set of faces.
        /// </summary>
        /// <param name="model">The geometry model</param>
        /// <param name="backgroundInfo">A background worker info. May be Null when no background worker exists</param>
        /// <param name="volumeGrid">The grid used to handle volumes</param>
        public static int RemoveDuplicateVolumes(GeometryModelData model, ref AABBGrid volumeGrid,
            IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            if (volumeGrid == null)
            {
                var aabbs = model.Volumes.Select(x => new AABB(x)).ToList();
                var range = AABB.Merge(aabbs);
                volumeGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                volumeGrid.AddRange(aabbs);
            }

            HashSet<(Volume, Volume)> doneTests = new HashSet<(Volume, Volume)>();

            var numVol = model.Volumes.Count;

            model.StartBatchOperation();

            int progressCounter = 0;

            foreach (var cell in volumeGrid.Cells)
            {
                if (cell != null)
                {
                    for (int i = 0; i < cell.Count; ++i)
                    {
                        var ibox = cell[i];
                        var ivolume = (Volume)ibox.Content;

                        for (int j = i + 1; j < cell.Count; ++j)
                        {
                            var jbox = cell[j];
                            var jvolume = (Volume)jbox.Content;

                            if (!doneTests.Contains((ivolume, jvolume)) && !doneTests.Contains((jvolume, ivolume)))
                            {
                                doneTests.Add((ivolume, jvolume));

                                if (ivolume.Faces.Count == jvolume.Faces.Count && ivolume.Faces.All(ipf => jvolume.Faces.Any(jpf => ipf.Face == jpf.Face)))
                                {
                                    //Volumes contain the exact same faces
                                    jvolume.RemoveFromModel();
                                    volumeGrid.Remove(jbox);
                                    j--;
                                }
                            }
                        }
                    }
                }

                if (backgroundInfo.CancellationPending)
                {
                    model.EndBatchOperation();
                    backgroundInfo.Cancel = true;
                    return 0;
                }
                backgroundInfo.ReportProgress((int)((double)progressCounter / (double)volumeGrid.Cells.Length * 100.0));
                progressCounter++;
            }

            model.EndBatchOperation();

            return numVol - model.Volumes.Count;
        }

        /// <summary>
        /// Removes faces with 0 size
        /// </summary>
        /// <param name="model">The model in which the faces should be removed</param>
        /// <param name="areaTolerance">Minimum area below which faces are considered degenerated</param>
        /// <param name="faceGrid">Speedup structure containing all faces</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static int RemoveDegeneratedFaces(GeometryModelData model, double areaTolerance, ref AABBGrid faceGrid,
            IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            if (faceGrid == null)
            {
                var aabbs = model.Faces.Select(x => new AABB(x));
                var range = AABB.Merge(aabbs);
                faceGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                faceGrid.AddRange(aabbs);
            }

            model.StartBatchOperation();

            //Search all degenerated faces
            List<Face> degenFaces = new List<Face>(model.Faces.Where(x =>
                x.Boundary.Edges.Count < 3 ||
                EdgeLoopAlgorithms.Area(x.Boundary) < areaTolerance)
                );
            //We could add several more constraints here, but this one should do it for now

            backgroundInfo.ReportProgress(50);

            var faceGridRef = faceGrid;

            foreach (var degenFace in degenFaces)
            {
                RemoveDegeneratedFace(model, degenFace, faceGridRef);
            }

            model.EndBatchOperation();

            backgroundInfo.ReportProgress(100);

            return degenFaces.Count;
        }

        private static void RemoveDegeneratedFace(GeometryModelData model, Face face, AABBGrid faceGrid)
        {
            foreach (var pface in face.PFaces)
                pface.Volume.Faces.Remove(pface);

            foreach (var holeParent in face.Boundary.Faces.Where(x => x.Boundary != face.Boundary))
            {
                holeParent.Holes.Remove(face.Boundary);
            }

            face.Boundary.RemoveFromModel();
            face.RemoveFromModel();

            //Update Grid
            AABB aabb = null;
            faceGrid.ForCell(face.Boundary.Edges.First().StartVertex.Position,
                x => { aabb = faceGrid.Cells[x.X, x.Y, x.Z].FirstOrDefault(box => box.Content == face); });
            if (aabb != null)
                faceGrid.Remove(aabb);
        }



        /// <summary>
        /// Handles Edge-Edge intersection (in one point) and overlapping edges
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="tolerance">Tolerance for the calculation</param>
        /// <param name="edgeGrid">Speedup structure containing all edges</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static int SplitEdgeEdgeIntersections(GeometryModelData model, double tolerance, ref AABBGrid edgeGrid, IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            if (edgeGrid == null)
            {
                var aabbs = model.Edges.Select(x => new AABB(x)).ToList();
                var range = AABB.Merge(aabbs);
                edgeGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                edgeGrid.AddRange(aabbs);
            }

            HashSet<(Edge, Edge)> testDone = new HashSet<(Edge, Edge)>();

            model.StartBatchOperation();

            int splitCounter = 0;
            int progressCounter = 0;
            foreach (var cell in edgeGrid.Cells)
            {
                if (cell != null)
                {
                    for (int i = 0; i < cell.Count - 1; ++i)
                    {
                        var eibox = cell[i];
                        var ei = (Edge)eibox.Content;

                        for (int j = i + 1; j < cell.Count; ++j)
                        {
                            var ejbox = cell[j];
                            var ej = (Edge)ejbox.Content;

                            if (!testDone.Contains((ei, ej)) && !testDone.Contains((ej, ei)))
                            {
                                testDone.Add((ei, ej));

                                if (!EdgeAlgorithms.IsOnSameLine(ei, ej, tolerance))
                                {
                                    //Handle intersections in exactly one point
                                    var intersectionResult = EdgeAlgorithms.EdgeEdgeIntersection(ei, ej, tolerance);

                                    if (intersectionResult.isIntersecting)//Split both lines
                                    {
                                        //Calc intersection position
                                        var intersectionPoint = ei.Vertices[0].Position + intersectionResult.t1 * (ei.Vertices[1].Position - ei.Vertices[0].Position);

                                        //Remove old edges
                                        model.Edges.Remove(ej);
                                        model.Edges.Remove(ei);

                                        //Add 4 new edges + 1 vertex
                                        var intersectionVertex = new Vertex(ei.Layer, string.Format("{0} - {1}", ei.Name, ej.Name), intersectionPoint);
                                        var ei_part1 = new Edge(ei.Layer, string.Format("{0} ({1})", ei.Name, 1), new List<Vertex> { ei.Vertices[0], intersectionVertex });
                                        var ei_part2 = new Edge(ei.Layer, string.Format("{0} ({1})", ei.Name, 2), new List<Vertex> { intersectionVertex, ei.Vertices[1] });

                                        var ej_part1 = new Edge(ej.Layer, string.Format("{0} ({1})", ej.Name, 1), new List<Vertex> { ej.Vertices[0], intersectionVertex });
                                        var ej_part2 = new Edge(ej.Layer, string.Format("{0} ({1})", ej.Name, 2), new List<Vertex> { intersectionVertex, ej.Vertices[1] });

                                        //Search for all occasions of ei and replace it there
                                        EdgeAlgorithms.ReplaceEdge(ei, new List<Edge> { ei_part1, ei_part2 });
                                        EdgeAlgorithms.ReplaceEdge(ej, new List<Edge> { ej_part1, ej_part2 });

                                        //Update grid
                                        edgeGrid.Remove(eibox);
                                        edgeGrid.Remove(ejbox);
                                        edgeGrid.Add(new AABB(ei_part1));
                                        edgeGrid.Add(new AABB(ei_part2));
                                        edgeGrid.Add(new AABB(ej_part1));
                                        edgeGrid.Add(new AABB(ej_part2));

                                        //Exclude subparts from tests
                                        testDone.Add((ei_part1, ei_part2));
                                        testDone.Add((ei_part1, ej_part1));
                                        testDone.Add((ei_part1, ej_part2));
                                        testDone.Add((ei_part2, ej_part1));
                                        testDone.Add((ei_part2, ej_part2));
                                        testDone.Add((ej_part1, ej_part2));

                                        i--;
                                        splitCounter++;
                                        break;
                                    }
                                }
                            }
                        }

                    }
                }

                if (backgroundInfo.CancellationPending)
                {
                    model.EndBatchOperation();
                    backgroundInfo.Cancel = true;
                    return 0;
                }

                progressCounter++;
                backgroundInfo.ReportProgress((int)((double)progressCounter / (double)edgeGrid.Cells.Length * 100.0));
            }

            model.EndBatchOperation();
            return splitCounter;
        }

        /// <summary>
        /// Handles vertices that are placed on an edge
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="tolerance">The calculation tolerance</param>
        /// <param name="vertexGrid">Speedup structure containing all vertices</param>
        /// <param name="edgeGrid">Speedup structure containing all edges</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static int SplitEdgeVertexIntersections(GeometryModelData model, double tolerance,
            ref AABBGrid vertexGrid, ref AABBGrid edgeGrid,
            IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            int splitCounter = 0;

            if (edgeGrid == null)
            {
                var aabbs = model.Edges.Select(x => new AABB(x)).ToList();
                var range = AABB.Merge(aabbs);
                edgeGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                edgeGrid.AddRange(aabbs);
            }
            if (vertexGrid == null)
            {
                var aabbs = model.Vertices.Select(x => new AABB(x)).ToList();
                var range = AABB.Merge(aabbs);
                vertexGrid = new AABBGrid(edgeGrid.Min, edgeGrid.Max, new Vector3D(5, 5, 5));
                vertexGrid.AddRange(aabbs);
            }

            model.StartBatchOperation();

            for (int x = 0; x < edgeGrid.Cells.GetLength(0); x++)
            {
                for (int y = 0; y < edgeGrid.Cells.GetLength(1); y++)
                {
                    for (int z = 0; z < edgeGrid.Cells.GetLength(2); z++)
                    {
                        var edgeCell = edgeGrid.Cells[x, y, z];
                        var vertexCell = vertexGrid.Cells[x, y, z];
                        if (edgeCell != null && vertexCell != null)
                        {
                            for (int i = 0; i < edgeCell.Count; ++i)
                            {
                                var eibox = edgeCell[i];
                                var ei = (Edge)eibox.Content;
                                var eiLength = (ei.Vertices[0].Position - ei.Vertices[1].Position).Length;
                                var eiTolerance = tolerance / eiLength;

                                for (int j = 0; j < vertexCell.Count; ++j)
                                {
                                    var vjbox = vertexCell[j];
                                    var vj = (Vertex)vjbox.Content;

                                    var dt = EdgeAlgorithms.EdgePointIntersection(ei, vj.Position);

                                    bool canSplit = dt.t.InRange(eiTolerance, 1 - eiTolerance);

                                    if (Math.Abs(dt.d) <= tolerance)
                                    {
                                        if (canSplit)
                                        {
                                            //Check if this split would invalidate an edgeloop
                                            if (!ei.PEdges.Any(eip => eip.Parent.Edges.Any(parentPEdge => parentPEdge.StartVertex == vj)))
                                            {
                                                model.Edges.Remove(ei);

                                                List<Edge> parts = new List<Edge> {
                                                    new Edge(ei.Layer, string.Format("{0} ({1})", ei.Name, 1),
                                                        new List<Vertex> { ei.Vertices[0], vj }),
                                                    new Edge(ei.Layer, string.Format("{0} ({1})", ei.Name, 1),
                                                        new List<Vertex> { vj, ei.Vertices[1] })
                                                };

                                                EdgeAlgorithms.ReplaceEdge(ei, parts);

                                                //Update grid
                                                edgeGrid.Remove(eibox);
                                                edgeGrid.Add(new AABB(parts[0]));
                                                edgeGrid.Add(new AABB(parts[1]));

                                                i--;
                                                splitCounter++;
                                                break;
                                            }
                                        }
                                        //Other case: Too close to end, but vertices itself are too far apart -> merge vertices
                                        else if (dt.t.InRange(1e-10, 1 - 1e-10) &&
                                            (vj.Position - ei.Vertices[0].Position).Length > tolerance &&
                                            (vj.Position - ei.Vertices[1].Position).Length > tolerance)
                                        {
                                            var vi = ei.Vertices[0];
                                            if (dt.t > 0.5)
                                                vi = ei.Vertices[1];

                                            var merged = MergeVertices(model, vi, vj, vjbox, tolerance * tolerance, ref vertexGrid, ref edgeGrid);
                                            if (merged)
                                            {
                                                i--;
                                                splitCounter++;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }

                    if (backgroundInfo.CancellationPending)
                    {
                        model.EndBatchOperation();
                        backgroundInfo.Cancel = true;
                        return 0;
                    }
                    backgroundInfo.ReportProgress(
                        (int)((x * edgeGrid.Cells.GetLength(1)) + y) / (edgeGrid.Cells.GetLength(0) * edgeGrid.Cells.GetLength(1))
                        );
                }
            }

            model.EndBatchOperation();
            return splitCounter;
        }

        /// <summary>
        /// Stores the results of a split face operation
        /// </summary>
        public struct SplitFaceResult
        {
            /// <summary>
            /// True when the operation has succeeded, otherwise False.
            /// </summary>
            public bool success;
            /// <summary>
            /// Contains the exception which caused the operation to fail when success is False
            /// </summary>
            public Exception exception;
            /// <summary>
            /// Contains the face at which the exception has happened when success is False.
            /// Only set when the exception can be attributed to a specific face
            /// </summary>
            public Face exceptionFace;
        }

        /// <summary>
        /// Splits overlapping faces
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="tolerance">The calculation tolerance</param>
        /// <param name="vertexGrid">Speedup structure containing all vertices</param>
        /// <param name="faceGrid">Speedup structure containing all faces</param>
        /// <param name="splitNameFormat">
        /// Format used in string.Format to generate names for split faces.
        /// Supports two arguments: 0 is the name of the original face, 1 is a running number identifying the split face part
        /// </param>
        /// <param name="errorLayerName">The name of the layer used to store invalid geometry. The layer is (if needed) created unless it exists</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static SplitFaceResult SplitFaces(GeometryModelData model, double tolerance, ref AABBGrid vertexGrid, ref AABBGrid faceGrid,
            string errorLayerName, string splitNameFormat = "{0} ({1})",
            IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            var result = new SplitFaceResult { success = true, exception = null, exceptionFace = null };

            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            if (vertexGrid == null)
            {
                List<AABB> aabbs = model.Vertices.Select(x => new AABB(x)).ToList();
                var range = AABB.Merge(aabbs);
                vertexGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                vertexGrid.AddRange(aabbs);
            }

            model.StartBatchOperation();

            try
            {

                //First handle openings that are not used as boundary of some face
                var nonFaceOpenings = model.EdgeLoops.Where(x => x.Faces.All(f => f.Boundary != x)).ToList();
                foreach (var op in nonFaceOpenings)
                {
                    var vertices = model.Vertices.Where(v => op.Edges.Any(pe => pe.Edge.Vertices.Contains(v)) ||
                                                              EdgeLoopAlgorithms.Contains(op, v.Position, 0, 0.01) == GeometricRelation.Contained).ToArray();

                    var edges = vertices.SelectMany(v => v.Edges.Where(e => vertices.Contains(e.Vertices[0]) && vertices.Contains(e.Vertices[1]))).Distinct().ToArray();

                    //Remove disconnected parts. This mainly removes faces that are completely contained in fi (openings)
                    (var cleanedVertices, var cleanedEdges, _) = CleanupCycle(vertices, edges, op.Edges.First().StartVertex,
                        null, op.ModelGeometry, errorLayerName);

                    //Find cycles
                    var mapping = EdgeLoopAlgorithms.LoopToXYMapping(op);
                    var cycles = FindCycles(cleanedEdges, cleanedVertices, mapping, op);

                    if (cycles.Count > 1)
                    {
                        List<EdgeLoop> replaceList = new List<EdgeLoop>();
                        int replaceCounter = 1;

                        foreach (var cycle in cycles)
                        {
                            EdgeLoop boundary = new EdgeLoop(op.Layer, string.Format(splitNameFormat, op.Name, replaceCounter), cycle)
                            {
                                IsVisible = op.IsVisible,
                                Color = new DerivedColor(op.Color.Color, op.Layer, nameof(Layer.Color))
                                {
                                    IsFromParent = op.Color.IsFromParent
                                }
                            };
                            replaceList.Add(boundary);

                            replaceCounter++;
                        }

                        //Replace
                        ReplaceHole(op, replaceList);
                        model.EdgeLoops.Remove(op);
                    }
                }

                //Sort faces such that openings are split first
                var faces = model.Faces.ToList();
                faces.Sort((x, y) =>
                {
                    if (x.Holes.Contains(y.Boundary))
                        return -1;
                    else if (y.Holes.Contains(x.Boundary))
                        return 1;
                    return 0;
                });

                for (int i = 0; i < faces.Count; ++i)
                {
                    try
                    {
                        var fi = faces[i];

                        //Collect potential face data
                        //v' = [vertices + all vertices inside polygon]
                        //all edges connecting two vertices in v'
                        HashSet<Vertex> vertices = new HashSet<Vertex>(fi.Boundary.Edges.Count);
                        AABB faceAABB = new AABB(fi);

                        var vGridCopy = vertexGrid;

                        vertexGrid.ForEachCell(faceAABB, x =>
                        {
                            var cell = vGridCopy.Cells[x.X, x.Y, x.Z];
                            if (cell != null)
                            {
                                foreach (var item in cell)
                                {
                                    var v = (Vertex)item.Content;
                                    if (!vertices.Contains(v) &&
                                        (
                                        fi.Boundary.Edges.Any(pe => pe.Edge.Vertices.Contains(v)) ||
                                        FaceAlgorithms.Contains(fi, v.Position, 0, 0.01) == GeometricRelation.Contained
                                        ))
                                    {
                                        vertices.Add(v);
                                    }
                                }
                            }
                        });

                        var edges = vertices.SelectMany(v => v.Edges.Where(e => vertices.Contains(e.Vertices[0]) && vertices.Contains(e.Vertices[1]))).Distinct().ToArray();

                        //Remove disconnected parts. This mainly removes faces that are completely contained in fi (openings)
                        (var cleanedVertices, var cleanedEdges, var destroyedHoles) = CleanupCycle(
                            vertices, edges, fi.Boundary.Edges.First().StartVertex, fi, fi.ModelGeometry, errorLayerName);

                        //Find cycles
                        var mapping = FaceAlgorithms.FaceToXYMapping(fi);

                        var cycles = FindCycles(cleanedEdges, cleanedVertices, mapping, fi.Boundary);

                        //remove cycles that were a hole before
                        for (int ci = 0; ci < cycles.Count; ++ci)
                        {
                            var cycle = cycles[ci];

                            var potentialLoops = destroyedHoles.Where(x => x.Edges.Count == cycle.Count).ToList();
                            foreach (var loop in potentialLoops)
                            {
                                if (cycle.All(e => loop.Edges.Any(pe => pe.Edge == e)))
                                {
                                    cycles.RemoveAt(ci);
                                    ci--;
                                    break;
                                }
                            }
                        }

                        if (cycles.Count > 1)
                        {
                            //Create face for each cycle
                            List<Face> replaceFaces = new List<Face>();
                            int replaceCounter = 1;
                            foreach (var cycle in cycles)
                            {
                                EdgeLoop boundary = new EdgeLoop(fi.Layer, String.Format(splitNameFormat, fi.Name, replaceCounter), cycle)
                                {
                                    IsVisible = fi.IsVisible,
                                    Color = new DerivedColor(fi.Color.Color, fi.Layer, nameof(Layer.Color))
                                    {
                                        IsFromParent = fi.Color.IsFromParent
                                    }
                                };

                                Face face = new Face(fi.Layer, string.Format(splitNameFormat, fi.Name, replaceCounter), boundary, fi.Orientation)
                                {
                                    IsVisible = fi.IsVisible,
                                    Color = new DerivedColor(fi.Color.Color, fi.Layer, nameof(Layer.Color))
                                    {
                                        IsFromParent = fi.Color.IsFromParent
                                    }
                                };

                                //Find all openings that belong into this cycle
                                foreach (var hole in fi.Holes.Where(x =>
                                    !destroyedHoles.Contains(x) &&
                                    x.Edges.All(e => FaceAlgorithms.Contains(face, e.StartVertex.Position, tolerance, tolerance) == GeometricRelation.Contained)))
                                {
                                    face.Holes.Add(hole);
                                    hole.Faces.Add(face);
                                }

                                replaceFaces.Add(face);
                                replaceCounter++;
                            }

                            //Replace
                            ReplaceFace(fi, replaceFaces);

                            //Remove old face
                            fi.Boundary.Faces.Remove(fi);
                            if (fi.Boundary.Faces.Count == 0)
                                fi.Boundary.RemoveFromModel();

                            fi.Holes.ForEach(x => x.Faces.Remove(fi));
                            fi.Holes.Where(x => x.Faces.Count == 0).ForEach(x => x.RemoveFromModel());

                            model.Faces.Remove(fi);
                        }

                    }
                    catch (Exception e)
                    {
                        result = new SplitFaceResult { success = false, exception = e, exceptionFace = faces[i] };

                        if (Debugger.IsAttached)
                            throw;

                        break; //End method because something is already broken
                    }


                    if (backgroundInfo.CancellationPending)
                    {
                        model.EndBatchOperation();
                        backgroundInfo.Cancel = true;
                        return new SplitFaceResult { success = true, exception = null, exceptionFace = null };
                    }
                    backgroundInfo.ReportProgress((int)((double)i / (double)model.Faces.Count * 100.0));
                }
            }
            catch (Exception e)
            {
                result = new SplitFaceResult { success = false, exception = e, exceptionFace = null };

                if (Debugger.IsAttached)
                    throw;
            }

            model.EndBatchOperation();

            //Regenerate face grid
            faceGrid = new AABBGrid(vertexGrid.Min, vertexGrid.Max, new Vector3D(5, 5, 5));
            faceGrid.AddRange(model.Faces.Select(x => new AABB(x)));

            return result;
        }

        /// <summary>
        /// Adds faces that are completely contained in another faces as opening
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="tolerance">The calculation tolerance</param>
        /// <param name="faceGrid">Speedup structure containing all faces</param>
        /// <param name="backgroundInfo">The background algorithm info for this task</param>
        public static void SplitContainedFaces(GeometryModelData model, double tolerance, ref AABBGrid faceGrid,
            IBackgroundAlgorithmInfo backgroundInfo = null)
        {
            if (backgroundInfo == null)
                backgroundInfo = new EmptyBackgroundAlgorithmInfo();
            backgroundInfo.ReportProgress(0);

            if (faceGrid == null)
            {
                var aabbs = model.Faces.Select(x => new AABB(x)).ToList();
                var range = AABB.Merge(aabbs);
                faceGrid = new AABBGrid(range.min, range.max, new Vector3D(5, 5, 5));
                faceGrid.AddRange(aabbs);
            }
            HashSet<(BaseGeometry, BaseGeometry)> testsDone = new HashSet<(BaseGeometry, BaseGeometry)>();

            model.StartBatchOperation();

            int progressCounter = 0;
            foreach (var cell in faceGrid.Cells)
            {
                if (cell != null)
                {
                    //BODY
                    for (int i = 0; i < cell.Count; ++i)
                    {
                        var fi = (Face)cell[i].Content;

                        for (int j = i + 1; j < cell.Count; ++j)
                        {
                            var fj = (Face)cell[j].Content;
                            if (!testsDone.Contains((fi, fj)))
                            {
                                if (!fi.Holes.Contains(fj.Boundary) && //Not already a hole
                                    !fj.Boundary.Edges.Any(ej => ej.Edge.PEdges.Any(ejOtherPEdge => ejOtherPEdge.Parent == fi.Boundary)) && //No common edge
                                    FaceAlgorithms.Contains2D(fi, fj, 0.0, tolerance) == GeometricRelation.Contained) //Contained in face
                                {
                                    fi.Holes.Add(fj.Boundary);
                                }

                                testsDone.Add((fi, fj));
                            }
                        }
                    }
                }

                if (backgroundInfo.CancellationPending)
                {
                    model.EndBatchOperation();
                    backgroundInfo.Cancel = true;
                    return;
                }

                progressCounter++;
                backgroundInfo.ReportProgress((int)((double)progressCounter / (double)faceGrid.Cells.Length * 100.0));

            }

            model.EndBatchOperation();
        }

        /// <summary>
        /// Removes the error layer when there are no elements on it
        /// </summary>
        /// <param name="model">The model from which the error layer should be removed</param>
        /// <param name="errorLayerName">The name of the layer used to store invalid geometry</param>
        public static void RemoveErrorLayerIfEmpty(GeometryModelData model, string errorLayerName)
        {
            var errorLayer = model.Layers.FirstOrDefault(x => x.Name == errorLayerName);
            if (errorLayer != null && errorLayer.Elements.Count == 0)
            {
                model.Layers.Remove(errorLayer);
            }
        }


        private static void ReplaceFace(Face original, List<Face> replacements)
        {
            foreach (var pface in original.PFaces)
            {
                pface.Volume.Faces.Remove(pface);

                foreach (var rep in replacements)
                {
                    var newPFace = new PFace(rep, pface.Volume, original.Orientation);
                    pface.Volume.Faces.Add(newPFace);
                    rep.PFaces.Add(newPFace);
                }
            }
        }
        private static void ReplaceHole(EdgeLoop original, List<EdgeLoop> replacements)
        {
            foreach (var face in original.Faces.Where(x => x.Boundary != original).ToList())
            {
                face.Holes.Remove(original);

                foreach (var rep in replacements)
                {
                    face.Holes.Add(rep);
                }
            }
        }


        private static List<List<Edge>> FindCycles(IEnumerable<Edge> edges, IEnumerable<Vertex> vertices, Matrix3D xyMapping, EdgeLoop boundary)
        {
            if (edges.Count() == 0)
                return new List<List<Edge>>();

            Dictionary<Vertex, Point3D> vertex2D = new Dictionary<Vertex, Point3D>();
            vertices.ForEach(x => vertex2D.Add(x, xyMapping.Transform(x.Position)));

            List<(List<Edge> cycle, double signedArea)> cycles = null;
            IEnumerable<Edge> currentEdges = edges;

            bool anythingReduced = true;
            while (anythingReduced)
            {
                anythingReduced = false;

                cycles = FindCycles(currentEdges, vertex2D);
                var boundaryCycle = cycles.First(x => x.signedArea > 0);

                //Eliminate external edges that are contained in the boundary cycle
                List<Edge> reducedEdges = new List<Edge>();

                foreach (var edge in currentEdges)
                {
                    if (boundary.Edges.Any(pe => pe.Edge == edge))
                        reducedEdges.Add(edge);
                    else if (!boundaryCycle.cycle.Contains(edge))
                        reducedEdges.Add(edge);
                    else
                        anythingReduced = true;
                }

                currentEdges = reducedEdges;
            }

            return cycles.Where(x => x.signedArea < 0).Select(x => x.cycle).ToList();
        }

        private static (IEnumerable<Vertex> v, IEnumerable<Edge> e, IEnumerable<EdgeLoop> destroyedHoles) CleanupCycle(
            IEnumerable<Vertex> vertices, IEnumerable<Edge> edges,
            Vertex boundaryVertex, Face face, GeometryModelData model, string errorLayerName)
        {
            Stack<Vertex> todoStack = new Stack<Vertex>();
            todoStack.Push(boundaryVertex);

            HashSet<Vertex> vertexStack = new HashSet<Vertex>();
            HashSet<Edge> finalEdges = new HashSet<Edge>();
            List<EdgeLoop> destroyedHoles = new List<EdgeLoop>();

            //Eliminate edges that are not reachable from the boundary
            while (todoStack.Count > 0)
            {
                var startVertex = todoStack.Pop();

                if (!vertexStack.Contains(startVertex))
                {
                    vertexStack.Add(startVertex);

                    foreach (var edge in startVertex.Edges)
                    {
                        if (edges.Contains(edge) && !finalEdges.Contains(edge))
                        {
                            finalEdges.Add(edge);
                            todoStack.Push(edge.Vertices.First(x => x != startVertex));
                        }
                    }
                }
            }

            //Exclude holes unless a vertex would have <= 1 edges afterwards
            if (face != null)
            {
                foreach (var hole in face.Holes)
                {
                    var holeVertices = hole.Edges.SelectMany(x => x.Edge.Vertices).Distinct().ToArray();
                    var holeEdges = hole.Edges.Select(x => x.Edge).ToArray();

                    bool producesTanglingEdges = false;

                    //Check if tangling edges would be produced
                    foreach (var v in holeVertices)
                    {
                        int edgeCount = v.Edges.Count(x => finalEdges.Contains(x) && !holeEdges.Contains(x));
                        if (edgeCount == 1) //0 -> completely removed, 2 -> probably a path through
                        {
                            producesTanglingEdges = true;
                            destroyedHoles.Add(hole);
                            break;
                        }
                    }

                    if (!producesTanglingEdges)
                    {
                        foreach (var edge in hole.Edges)
                        {
                            if (finalEdges.Contains(edge.Edge) && !face.Boundary.Edges.Any(x => x.Edge == edge.Edge))
                                finalEdges.Remove(edge.Edge);
                        }


                        var interFaceEdges = finalEdges.Where(
                                x => holeVertices.Contains(x.Vertices[0]) && holeVertices.Contains(x.Vertices[1]) && !face.Boundary.Edges.Any(be => be.Edge == x)
                                )
                            .ToArray();

                        foreach (var iEdge in interFaceEdges)
                            finalEdges.Remove(iEdge);
                    }
                }
            }

            var finalVertices = finalEdges.SelectMany(x => x.Vertices).Distinct().ToList();

            //This is a hack and will only work if there are no two holes adjacent to each other
            var disconnectedVert = finalVertices.FirstOrDefault(v => finalEdges.Count(e => e.Vertices.Contains(v)) < 2);
            while (disconnectedVert != null)
            {
                var potentialEdges = edges.Where(e => !finalEdges.Contains(e) && e.Vertices.Contains(disconnectedVert));
                var potentialEdge = potentialEdges.FirstOrDefault(e => finalVertices.Contains(e.Vertices.First(v => v != disconnectedVert)));

                if (potentialEdge == null)
                {
                    //Find error layer
                    var errorLayer = model.Layers.FirstOrDefault(x => x.Name == errorLayerName);
                    if (errorLayer == null)
                    {
                        errorLayer = new Layer(model, errorLayerName) { Color = new DerivedColor(System.Windows.Media.Colors.Red) };
                        model.Layers.Add(errorLayer);
                    }

                    if (face != null)
                        face.Layer = errorLayer;

                    return (new List<Vertex>(), new List<Edge>(), new List<EdgeLoop>());
                }


                finalEdges.Add(potentialEdge);
                disconnectedVert = finalVertices.FirstOrDefault(v => finalEdges.Count(e => e.Vertices.Contains(v)) < 2);
            }

            return (finalVertices, finalEdges, destroyedHoles);
        }

        private static List<(List<Edge> cycle, double signedArea)> FindCycles(IEnumerable<Edge> edges, Dictionary<Vertex, Point3D> mapping)
        {
            if (edges.Count() == 0)
                return new List<(List<Edge> cycle, double signedArea)>();

            List<(List<Edge> cycle, double signedArea)> cycles = new List<(List<Edge> cycle, double signedArea)>();

            Dictionary<Edge, int> edgeUsage = new Dictionary<Edge, int>(); // 0 = not used, 1 = clockwise, -1 = counterclockwise, 2 = both
            edges.ForEach(e => edgeUsage.Add(e, 0));


            var startEdge = edges.First();
            while (startEdge != null)
            {
                var currentEdge = startEdge;
                List<Edge> cycle = new List<Edge>() { currentEdge };

                var cycleStartVertex = currentEdge.Vertices[0];
                if (edgeUsage[currentEdge] == 1)
                {
                    cycleStartVertex = currentEdge.Vertices[1];
                    edgeUsage[currentEdge] = 2;
                }
                else if (edgeUsage[currentEdge] == -1)
                    edgeUsage[currentEdge] = 2;
                else
                    edgeUsage[currentEdge] = 1;
                var nextStartVertex = currentEdge.Vertices.First(v => v != cycleStartVertex);

                bool cycleClosed = false;
                double signedArea = SignedAreaXY(cycleStartVertex, nextStartVertex, x => mapping[x]);

                while (!cycleClosed)
                {
                    var lastStartVertex = currentEdge.Vertices.First(x => x != nextStartVertex);
                    var currentStartVertex = nextStartVertex;

                    //Search for edges that connect to the start vertex, are not in the cycle and are not completely used
                    var nextEdgeCandidates = currentStartVertex.Edges.Where(x => !cycle.Contains(x) && edgeUsage.ContainsKey(x) && edgeUsage[x] != 2);

                    var currentDirection = mapping[currentStartVertex] - mapping[lastStartVertex];
                    currentDirection.Normalize();

                    //Find candidate with minimal angle
                    var minCandidate = nextEdgeCandidates.ArgMin(x =>
                    {
                        var dir = mapping[x.Vertices.First(v => v != currentStartVertex)] - mapping[currentStartVertex];
                        dir.Normalize();

                        return SignedAngle(currentDirection.XY(), dir.XY());
                    });

                    currentEdge = minCandidate.value;
                    cycle.Add(currentEdge);

                    if (edgeUsage[minCandidate.value] == 0)
                        edgeUsage[minCandidate.value] = (minCandidate.value.Vertices[0] == currentStartVertex) ? 1 : -1;
                    else if (edgeUsage[minCandidate.value] == 1 || edgeUsage[minCandidate.value] == -1)
                        edgeUsage[minCandidate.value] = 2;

                    nextStartVertex = minCandidate.value.Vertices.First(x => x != currentStartVertex);

                    signedArea += SignedAreaXY(currentStartVertex, nextStartVertex, x => mapping[x]);

                    if (nextStartVertex == cycleStartVertex)
                        cycleClosed = true;
                }

                cycles.Add((cycle, signedArea));

                //Find next start edge
                startEdge = edges.FirstOrDefault(e => edgeUsage[e] != 2);
            }

            return cycles;
        }

        private static double SignedAngle(Vector v1, Vector v2)
        {
            var v1l = v1.Length;
            var v2l = v2.Length;

            double angle1 = Math.Atan2(v1.Y / v1l, v1.X / v1l);
            double angle2 = Math.Atan2(v2.Y / v2l, v2.X / v2l);

            var angle = angle2 - angle1;
            if (angle < -Math.PI)
                angle += 2 * Math.PI;
            else if (angle > Math.PI)
                angle -= 2 * Math.PI;

            return angle;
        }

        private static double SignedAreaXY(Vertex v1, Vertex v2, Func<Vertex, Point3D> mapping)
        {
            var v1m = mapping(v1);
            var v2m = mapping(v2);

            return (v1m.X * v2m.Y - v2m.X * v1m.Y);
        }

        private static void RemoveDegeneratedEdgeLoops(GeometryModelData modelData)
        {
            for (int loopi = 0; loopi < modelData.EdgeLoops.Count; ++loopi)
            {
                var loop = modelData.EdgeLoops[loopi];

                if (!EdgeAlgorithms.OrderLoop(loop.Edges).isLoop) //Degenerated
                {
                    for (int facei = 0; facei < loop.Faces.Count; ++facei)
                    {
                        var face = loop.Faces[facei];

                        if (face.Boundary == loop) //Boundary -> remove face
                        {
                            foreach (var pface in face.PFaces)
                            {
                                pface.Volume.Faces.Remove(pface);
                            }
                            face.RemoveFromModel();
                            --facei;
                        }
                        else
                            face.Holes.Remove(loop);
                    }

                    loop.RemoveFromModel();
                    loopi--;
                }
            }
        }
    }
}