using MathNet.Numerics.LinearAlgebra;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Improved offset generator
    /// Uses a heuristic based solve on each vertex
    /// </summary>
    /// 
    /// -- Details --
    /// 
    /// --- Exterior Faces ---
    ///  For each exterior face (0 or 1 PFace) run around boundary vertices.
    ///  For each vertex find all adjacent faces. Calculate orientation and if there is a shared edge between the current face and the adjacent one
    ///  This gives a number of directions and offsets
    ///  Try to find three linear independed directions
    ///  - Case 1: Only one direction -> return that direction
    ///  - Case 2: Two directions -> add a third direction perpendicular to the first two with offset 0
    ///  - Case 3: Three directions -> solve equation system
    ///    - Test all directions/offsets against that solution
    ///    
    ///  Special cases:
    ///  A: Two parallel faces with different offset
    ///   - Solution: Remove faces parallel to the current face from search space (happens anyway because they are not lin.indep.)
    ///  B: Four directions with conflicting offsets
    ///   - Solution: If no solution to the equation system exists, remove all faces without shared edge, try to solve again
    ///   
    /// Revisions:
    /// Rev2: Try to only take one face per adjacent pedge (hopefully excludes wrong offsets in T-junctions
    public class ImprovedOffsetSurfaceGenerator
    {
        /// <summary>
        /// Updates the offset mesh
        /// </summary>
        /// <param name="model">The model</param>
        public static void Update(GeometryModelData model)
        {
            model.OffsetModel.Faces.Clear();

            if (model.Model.Exchange != null)
            {
                List<(Face f1, int f1orient, Face f2, int f2orient)> setbackFaces = new List<(Face f1, int f1orient, Face f2, int f2orient)>();

                HandleExteriorFaces(model, model.Faces, ref setbackFaces);
                HandleInteriorFaces(model, model.Volumes);

                HandleMissingFaces(model);

                HandleHoles(model.Faces, model);
                HandleAdditionalSurfaces(model, setbackFaces);
            }
        }
        /// <summary>
        /// Performs a partial update on the offset mesh
        /// </summary>
        /// <param name="model">The offset model</param>
        /// <param name="invalidatedGeometry">The invalidated geometry (only Faces are taken into account)</param>
        /// <returns>A list of all modified faces</returns>
        public static IEnumerable<Face> Update(GeometryModelData model, IEnumerable<BaseGeometry> invalidatedGeometry)
        {
            //Stopwatch watch = new Stopwatch();
            //watch.Start();

            //Directly affected faces
            HashSet<Face> affectedFaces = new HashSet<Face>(invalidatedGeometry.Where(x => x is Face).Select(x => (Face)x));

            if (model.Model != null && model.Model.Exchange != null)
            {
                if (affectedFaces.Count > 0)
                {
                    //Expand faces once in each direction
                    foreach (var f in affectedFaces.ToList())
                    {
                        foreach (var pedge in f.Boundary.Edges)
                        {
                            foreach (var edgeAroundVertex in pedge.StartVertex.Edges)
                            {
                                foreach (var adjPEdge in edgeAroundVertex.PEdges)
                                {
                                    var adjFace = adjPEdge.Parent.Faces.FirstOrDefault(x => x.Boundary == adjPEdge.Parent);
                                    if (adjFace != null && !affectedFaces.Contains(adjFace))
                                        affectedFaces.Add(adjFace);
                                }
                            }
                        }
                    }

                    foreach (var f in affectedFaces.ToList())
                    {
                        if (!model.ContainsGeometry(f))
                        {
                            affectedFaces.Remove(f);
                            model.OffsetModel.Faces.Remove((f, GeometricOrientation.Forward));
                            model.OffsetModel.Faces.Remove((f, GeometricOrientation.Backward));
                        }
                    }

                    List<(Face f1, int f1orient, Face f2, int f2orient)> setbackSurfaces = new List<(Face f1, int f1orient, Face f2, int f2orient)>();
                    HandleExteriorFaces(model, affectedFaces, ref setbackSurfaces);
                    var affectedVolumes = affectedFaces.SelectMany(x => x.PFaces.Select(pf => pf.Volume)).Distinct().ToList();
                    HandleInteriorFaces(model, affectedVolumes);

                    HandleMissingFaces(model);

                    HandleHoles(affectedFaces.Concat(affectedVolumes.SelectMany(x => x.Faces).Select(x => x.Face)).Distinct(), model);
                    HandleAdditionalSurfaces(model, setbackSurfaces);
                }
            }

            //watch.Stop();
            //Console.WriteLine("Update for {0} faces: {1}", invalidatedGeometry.Count(x => x is Face), watch.ElapsedMilliseconds);

            return affectedFaces;
        }



        private static void HandleMissingFaces(GeometryModelData model)
        {
            //It happens sometimes (in degeneration cases) that both pfaces have the same orientation.
            //It also happens sometimes for hole faces between two volumes that non of the faces have been generated
            foreach (var face in model.Faces)
            {
                var forwardExists = model.OffsetModel.Faces.ContainsKey((face, GeometricOrientation.Forward));
                var backwardExists = model.OffsetModel.Faces.ContainsKey((face, GeometricOrientation.Backward));

                if (!forwardExists && !backwardExists)
                {
                    Console.WriteLine("Both sides missing: Face {0}", face.Id);

                    model.OffsetModel.Faces[(face, GeometricOrientation.Forward)] = new OffsetFace(face);
                    model.OffsetModel.Faces[(face, GeometricOrientation.Forward)].Boundary.AddRange(face.Boundary.Edges.Select(x => x.StartVertex.Position));

                    foreach (var hole in face.Holes)
                    {
                        model.OffsetModel.Faces[(face, GeometricOrientation.Forward)].Openings.Add(hole,
                            hole.Edges.Select(x => x.StartVertex.Position).ToList());
                    }
                    model.OffsetModel.Faces[(face, GeometricOrientation.Backward)] = new OffsetFace(face);
                    model.OffsetModel.Faces[(face, GeometricOrientation.Backward)].Boundary.AddRange(face.Boundary.Edges.Select(x => x.StartVertex.Position));

                    foreach (var hole in face.Holes)
                    {
                        model.OffsetModel.Faces[(face, GeometricOrientation.Backward)].Openings.Add(hole,
                            hole.Edges.Select(x => x.StartVertex.Position).ToList());
                    }
                }
                if (!forwardExists)
                {
                    model.OffsetModel.Faces[(face, GeometricOrientation.Forward)] = model.OffsetModel.Faces[(face, GeometricOrientation.Backward)];
                }
                else if (!backwardExists)
                {
                    model.OffsetModel.Faces[(face, GeometricOrientation.Backward)] = model.OffsetModel.Faces[(face, GeometricOrientation.Forward)];
                }
            }
        }


        //Interior Faces

        private static void HandleInteriorFaces(GeometryModelData model, IEnumerable<Volume> volumes)
        {
            Dictionary<(Face, Vertex, int), Vector3D> offsetCalculationCache = new Dictionary<(Face, Vertex, int), Vector3D>();

            foreach (var currentVolume in volumes.Where(x => x.IsConsistentOriented))
            {
                var facesInVolume = currentVolume.Faces;

                foreach (var currentPFace in currentVolume.Faces)
                {
                    int orientation = (int)currentPFace.Orientation;

                    var offsetFace = new OffsetFace(currentPFace.Face);
                    offsetFace.Offset = GetOffsetFromDir(model.Model.Exchange.GetFaceOffset(currentPFace.Face), orientation);

                    foreach (var currentPEdge in currentPFace.Face.Boundary.Edges)
                    {
                        var currentVertex = currentPEdge.StartVertex;

                        if (offsetCalculationCache.ContainsKey((currentPFace.Face, currentVertex, orientation)))
                        {
                            offsetFace.Boundary.Add(currentVertex.Position + offsetCalculationCache[(currentPFace.Face, currentVertex, orientation)]);
                        }
                        else
                        {
                            var currentVertexFaces = FacesWithOrient(currentVertex, currentPFace.Face, orientation, x => x.PFaces.Count >= 1 &&
                                x.PFaces.Any(pf => pf.Volume == currentVolume));

                            (var offset, var isValid) = SolveOffset(currentVertexFaces, currentPFace.Face, currentVertex, model.Model.Exchange,
                                offsetCalculationCache, true);

                            //if (!isValid)
                            //	Console.WriteLine("Failed to solve offset-surface at Face {0}, Vertex {1}", face.Id, currentVertex.Id);

                            offsetFace.Boundary.Add(currentVertex.Position + offset);

                            var test = currentVertex.Position + offset;
                        }
                    }

                    if (currentPFace.Face.Boundary.Edges.Count != offsetFace.Boundary.Count)
                        Console.WriteLine("Mismatch between edge count and boundary count");

                    //Add offset surface
                    model.OffsetModel.Faces[(currentPFace.Face, (GeometricOrientation)orientation)] = offsetFace;
                }
            }
        }


        //Exterior Faces
        private static void HandleExteriorFaces(GeometryModelData model,
            IEnumerable<Face> faces,
            ref List<(Face f1, int f1orient, Face f2, int f2orient)> setbackSurfaces)
        {
            Dictionary<(Face, Vertex, int), Vector3D> offsetCalculationCache = new Dictionary<(Face, Vertex, int), Vector3D>();

            foreach (var face in faces.Where(x => x.PFaces.Count <= 1)) //Only exterior faces
            {
                //For exterior faces of volume -> just calculate outside.
                //For faces not part of volume -> calculate both sides
                //When volume is inconsistent -> also calculate both sides. This fixes a few problems because Pface.Orientation would be undefined.
                int[] orientations;
                if (face.PFaces.Count == 0 || !face.PFaces[0].Volume.IsConsistentOriented)
                    orientations = new int[] { 1, -1 };
                else
                    orientations = new int[] { -(int)face.PFaces[0].Orientation };

                foreach (var orientation in orientations)
                {
                    var offsetFace = new OffsetFace(face);
                    offsetFace.Offset = GetOffsetFromDir(model.Model.Exchange.GetFaceOffset(face), orientation);

                    //Run around loop and calculate new points
                    foreach (var pedge in face.Boundary.Edges)
                    {
                        var currentVertex = pedge.StartVertex;

                        if (offsetCalculationCache.ContainsKey((face, currentVertex, orientation)))
                        {
                            offsetFace.Boundary.Add(currentVertex.Position + offsetCalculationCache[(face, currentVertex, orientation)]);
                        }
                        else
                        {
                            //Find all faces around currentVertex
                            var currentVertexFaces = FacesWithOrient(currentVertex, face, orientation, x => x.PFaces.Count <= 1);

                            //Identify faces where additional closing faces are necessary
                            for (int i = 0; i < currentVertexFaces.Count; ++i)
                            {
                                var fi = currentVertexFaces.Keys.ElementAt(i);
                                var fidata = currentVertexFaces[fi];

                                if (fi != face &&
                                    face.Id < fi.Id &&
                                    fidata.hasCommonEdge && Math.Abs(Vector3D.DotProduct(fi.Normal, face.Normal)) > 0.99) //Parallel
                                {
                                    if (Math.Abs(
                                            GetOffsetFromDir(model.Model.Exchange.GetFaceOffset(face), orientation) -
                                            GetOffsetFromDir(model.Model.Exchange.GetFaceOffset(fi), fidata.orientModifier)
                                            ) > 0.01)
                                    {
                                        //Remove duplicates
                                        if (!setbackSurfaces.Any(x => x.f1 == face && x.f1orient == orientation && x.f2 == fi))
                                            setbackSurfaces.Add((face, orientation, fi, fidata.orientModifier));
                                    }
                                }
                            }

                            (var offset, var isValid) = SolveOffset(currentVertexFaces, face, currentVertex, model.Model.Exchange,
                                offsetCalculationCache, true);
                            offsetFace.Boundary.Add(currentVertex.Position + offset);
                        }
                    }

                    if (face.Boundary.Edges.Count != offsetFace.Boundary.Count)
                        Console.WriteLine("Exterior face: Mismatch between edge count and offsetsurface boundary count");

                    //Add offset surface
                    model.OffsetModel.Faces[(face, (GeometricOrientation)orientation)] = offsetFace;
                }
            }
        }

        private static Dictionary<Face, (int orientModifier, bool hasCommonEdge)> FacesWithOrient_V2(
            Vertex sourceVertex, Face sourceFace, int orientation, Func<Face, bool> faceSelector
            )
        {
            Dictionary<Face, (int orientModifier, bool hasCommonEdge)> result = new Dictionary<Face, (int orientModifier, bool hasCommonEdge)>();

            //Select all relevant faces
            List<Face> adjFaces = new List<Face>();

            foreach (var edge in sourceVertex.Edges)
            {
                foreach (var pedge in edge.PEdges)
                {
                    if (pedge.Parent is EdgeLoop)
                    {
                        var l = (EdgeLoop)pedge.Parent;
                        adjFaces.Add(l.Faces.FirstOrDefault(x => x.Boundary == l));
                    }
                }
            }



            return result;
        }


        private static Dictionary<Face, (int orientModifier, bool hasCommonEdge)> FacesWithOrient(
            Vertex sourceVertex, Face sourceFace, int orientation, Func<Face, bool> faceSelector
            )
        {
            Dictionary<Face, (int orientModifier, bool hasCommonEdge)> result = new Dictionary<Face, (int orientModifier, bool hasCommonEdge)>();

            //Find all faces around vertex that fit the selector rule
            //calculate orientation modifier and whether it has a common edge with the sourceFace
            Stack<Face> faceStack = new Stack<Face>();
            faceStack.Push(sourceFace);
            result.Add(sourceFace, (orientation, true));

            while (faceStack.Count > 0)
            {
                var currentFace = faceStack.Pop();

                //Find edges that contain v and are part of currentFace
                var currentFaceEdges = currentFace.Boundary.Edges.Where(x => x.Edge.Vertices.Contains(sourceVertex) && x.Edge.PEdges.Count > 1);
                var currentOrient = result[currentFace].orientModifier;

                //Find adj faces that are not handled yet

                foreach (var currentPEdge in currentFaceEdges)
                {
                    var currentEdgeDirection = EdgeAlgorithms.Direction(currentPEdge);
                    var currentFaceDirection = DetectionAlgorithms.DirectionFromFaceAndEdge(currentFace, currentPEdge);

                    double rotationDirection = Vector3D.DotProduct(
                                                Vector3D.CrossProduct(currentEdgeDirection, currentFaceDirection),
                                                currentFace.Normal * currentOrient
                                                );

                    var adjPEdges = currentPEdge.Edge.PEdges.Where(x => x != currentPEdge && x.Parent is EdgeLoop);

                    PEdge adjPEdge = null;
                    //profCount1++;

                    if (rotationDirection < 0)
                    {
                        adjPEdge = adjPEdges.ArgMax(x => AngleBetweenFaces(x, currentFaceDirection, currentEdgeDirection)).value;
                    }
                    else
                    {
                        adjPEdge = adjPEdges.ArgMin(x => AngleBetweenFaces(x, currentFaceDirection, currentEdgeDirection)).value;
                    }

                    if (adjPEdge != null)
                    {
                        var adjFaces = ((EdgeLoop)adjPEdge.Parent).Faces.Where(faceSelector);

                        foreach (var adjFace in adjFaces)
                        {
                            if (!result.ContainsKey(adjFace))
                            {
                                int orientModifier = 1;
                                if ((int)currentPEdge.Orientation * (int)currentFace.Orientation == (int)adjPEdge.Orientation * (int)adjFace.Orientation)
                                    orientModifier = -1;

                                bool isAdjacentToSource = false;
                                if (currentFace == sourceFace)
                                    isAdjacentToSource = true;

                                result.Add(adjFace, (currentOrient * orientModifier, isAdjacentToSource));
                                faceStack.Push(adjFace);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static double AngleBetweenFaces(PEdge x, Vector3D currentFaceDirection, Vector3D currentEdgeDirection)
        {
            if (((EdgeLoop)x.Parent).Faces.Count == 0)
                return double.PositiveInfinity;

            var face = ((EdgeLoop)x.Parent).Faces.First();
            var dir = DetectionAlgorithms.DirectionFromFaceAndEdge(face, x);
            var angle = Math.Atan2(
                Vector3D.DotProduct(Vector3D.CrossProduct(currentFaceDirection, dir), currentEdgeDirection),
                Vector3D.DotProduct(currentFaceDirection, dir));
            if (angle < 0)
                angle = 2.0 * Math.PI + angle; //+ because angle is negative
            return angle;
        }


        private static (Vector3D offset, bool isValid) SolveOffset(
            Dictionary<Face, (int orientModifier, bool hasCommonEdge)> faces,
            Face sourceFace,
            Vertex sourceVertex,
            IOffsetQueryable offsetQuery,
            Dictionary<(Face, Vertex, int), Vector3D> offsetVertexCache,
            bool useFacesWithoutCommonEdge)
        {
            //Find all directions/offsets in the system
            List<(Vector3D dir, double off, Face face)> availableDirections = new List<(Vector3D dir, double off, Face face)>();

            //Make sure that source face is included and first in list
            var sourceOffset = GetOffsetFromDir(offsetQuery.GetFaceOffset(sourceFace), faces[sourceFace].orientModifier);
            availableDirections.Add((
                        sourceFace.Normal * faces[sourceFace].orientModifier,
                        sourceOffset,
                        sourceFace
                        ));

            foreach (var f in faces)
            {
                var foffset = GetOffsetFromDir(offsetQuery.GetFaceOffset(f.Key), f.Value.orientModifier);
                var normal = f.Key.Normal * f.Value.orientModifier;

                if (!IsJumpFace(sourceFace, sourceOffset, f.Key, foffset) //Solves special case A
                    && (useFacesWithoutCommonEdge || f.Value.hasCommonEdge)) //Part of special case B handling
                {
                    availableDirections.Add((
                        normal,
                        foffset,
                        f.Key
                        ));
                }
            }



            //Find three linear independed directions
            List<(Vector3D dir, double off)> directions = new List<(Vector3D dir, double off)>();

            //Add sourceface to ensure that it gets used
            directions.Add(
                (sourceFace.Normal * faces[sourceFace].orientModifier, GetOffsetFromDir(offsetQuery.GetFaceOffset(sourceFace), faces[sourceFace].orientModifier))
                );

            int i = 0;
            //Second vector
            for (; i < availableDirections.Count; i++)
            {
                if (Math.Abs(Vector3D.DotProduct(directions[0].dir, availableDirections[i].dir)) < 0.99)
                {
                    directions.Add((
                        availableDirections[i].dir,
                        availableDirections[i].off
                        ));
                    break;
                }
            }

            i++;

            //Third vector
            for (; i < availableDirections.Count; i++)
            {
                if (Vector3D.CrossProduct(
                        Vector3D.CrossProduct(directions[0].dir, availableDirections[i].dir),
                        Vector3D.CrossProduct(directions[1].dir, availableDirections[i].dir)
                    ).LengthSquared > 0.01)
                {
                    directions.Add((
                        availableDirections[i].dir,
                        availableDirections[i].off
                        ));
                    break;
                }
            }

            //Evaluate directions
            if (directions.Count == 2) //Two vectors, no third -> add a perpendicular = 0 constaint
            {
                directions.Add((Vector3D.CrossProduct(directions[0].dir, directions[1].dir), 0));
            }

            //Calculate offset
            if (directions.Count == 1)
            {
                return (directions[0].dir * directions[0].off, true);
            }
            else //Must be 3 (2 is already handled above)
            {
                Matrix<double> A = Matrix<double>.Build.DenseOfArray(new double[,]
                {
                    { directions[0].dir.X, directions[0].dir.Y, directions[0].dir.Z },
                    { directions[1].dir.X, directions[1].dir.Y, directions[1].dir.Z },
                    { directions[2].dir.X, directions[2].dir.Y, directions[2].dir.Z }
                });

                Vector<double> b = Vector<double>.Build.Dense(new double[] { directions[0].off, directions[1].off, directions[2].off });

                var x = A.Solve(b);
                var xvec = new Vector3D(x[0], x[1], x[2]);
                bool isValid = !x.Any(xval => double.IsInfinity(xval) || double.IsNaN(xval));

                if (isValid)
                {
                    //Evaluate solution
                    foreach (var f in availableDirections)
                    {
                        if (Math.Abs(Vector3D.DotProduct(f.dir, xvec) - f.off) > 0.001)
                            isValid = false;
                    }
                }

                if (isValid && availableDirections.Count == faces.Count + 1)
                {
                    foreach (var f in availableDirections)
                    {
                        offsetVertexCache[(f.face, sourceVertex, faces[f.face].orientModifier)] = xvec;
                    }
                }

                //Handling of special case B (conflicting goals)
                if (!isValid && useFacesWithoutCommonEdge)
                {
                    var secondAttempt = SolveOffset(faces, sourceFace, sourceVertex, offsetQuery, offsetVertexCache, false);
                    return secondAttempt;
                }

                return (xvec, isValid);
            }
        }

        private static double GetOffsetFromDir((double inner, double outer) offsets, int direction)
        {
            if (direction == 1)
                return offsets.outer;
            else
                return offsets.inner;
        }


        // Holes

        private static void HandleHoles(IEnumerable<Face> faces, GeometryModelData model)
        {
            foreach (var face in faces)
            {
                for (int i = -1; i <= 1; i += 2)
                {
                    GeometricOrientation orient = (GeometricOrientation)i;
                    if (model.OffsetModel.Faces.TryGetValue((face, orient), out var faceEntry))
                    {
                        faceEntry.Openings.Clear();

                        var dir = face.Normal * i * faceEntry.Offset;

                        foreach (var faceOpening in face.Holes)
                        {
                            faceEntry.Openings.Add(faceOpening, faceOpening.Edges.Select(x => x.StartVertex.Position + dir).ToList());
                        }
                    }
                }
            }
        }


        private static void HandleAdditionalSurfaces(GeometryModelData model, List<(Face f1, int f1orient, Face f2, int f2orient)> setbackFaces)
        {

            foreach (var face in model.Faces)
            {
                var ofaceOuter = model.OffsetModel.Faces[(face, GeometricOrientation.Forward)];
                var ofaceInner = model.OffsetModel.Faces[(face, GeometricOrientation.Backward)];

                //Closings for end-lines
                for (int i = 0; i < face.Boundary.Edges.Count; ++i)
                {
                    var edge = face.Boundary.Edges[i];

                    if (edge.Edge.PEdges.Count == 1)
                    {
                        var ip = (i + 1) % face.Boundary.Edges.Count;

                        List<Point3D> addPoints = new List<Point3D>()
                        {
                            ofaceOuter.Boundary[ip],
                            ofaceInner.Boundary[ip],
                            ofaceInner.Boundary[i],
                            ofaceOuter.Boundary[i],
                        };

                        ofaceInner.AdditionalQuads.Add(addPoints);

                        //Prevents additional edges at t-junctions
                        if (edge.StartVertex.Edges.Count < 4)
                        {
                            ofaceInner.AdditionalEdges.Add(addPoints[0]);
                            ofaceInner.AdditionalEdges.Add(addPoints[1]);
                        }
                        if (edge.Next.StartVertex.Edges.Count < 4)
                        {
                            ofaceInner.AdditionalEdges.Add(addPoints[2]);
                            ofaceInner.AdditionalEdges.Add(addPoints[3]);
                        }
                    }
                }

                //Opening side-faces					
                foreach (var innerHole in ofaceInner.Openings)
                {
                    if (ofaceOuter.Openings.TryGetValue(innerHole.Key, out var outerHole))
                    {
                        for (int j = 0; j < innerHole.Value.Count; j++)
                        {
                            var jplus = (j + 1) % innerHole.Value.Count;

                            //Cover faces
                            List<Point3D> addFace = new List<Point3D>()
                            {
                                innerHole.Value[j],
                                outerHole[j],
                                outerHole[jplus],
                                innerHole.Value[jplus],
                            };

                            ofaceInner.AdditionalQuads.Add(addFace);

                            //Edges
                            ofaceInner.AdditionalEdges.Add(innerHole.Value[j]);
                            ofaceInner.AdditionalEdges.Add(innerHole.Value[jplus]);
                            ofaceInner.AdditionalEdges.Add(outerHole[j]);
                            ofaceInner.AdditionalEdges.Add(outerHole[jplus]);
                            ofaceInner.AdditionalEdges.Add(innerHole.Value[j]);
                            ofaceInner.AdditionalEdges.Add(outerHole[j]);
                        }
                    }
                }
            }


            //Closing edges at vertices with 1 or two unclosed pedges
            foreach (var setb in setbackFaces)
            {
                //Always store 
                var oface = model.OffsetModel.Faces[(setb.f1, (GeometricOrientation)setb.f1orient)];
                if (setb.f1.Id > setb.f2.Id)
                    oface = model.OffsetModel.Faces[(setb.f2, (GeometricOrientation)setb.f2orient)];

                //Find common edge
                var commonPEdge1 = setb.f1.Boundary.Edges.FirstOrDefault(pe => pe.Edge.PEdges.Any(pe2 => pe2.Parent == setb.f2.Boundary));
                if (commonPEdge1 != null)
                {
                    //Find offset faces
                    var off1 = FaceOffsetFromDirection(model, setb.f1, setb.f1orient);
                    var off2 = FaceOffsetFromDirection(model, setb.f2, setb.f2orient);

                    //Find common edge and orientation
                    int idx1 = setb.f1.Boundary.Edges.IndexOf(commonPEdge1);
                    var commonPEdge2 = commonPEdge1.Edge.PEdges.FirstOrDefault(pe2 => pe2.Parent == setb.f2.Boundary);
                    int idx2 = setb.f2.Boundary.Edges.IndexOf(commonPEdge2);
                    var idx2plusminus = commonPEdge1.Orientation == commonPEdge2.Orientation ? 1 : -1;

                    //make sure that off1 is the one with the smaller offset (for consistent winding order)
                    if (off1.Offset > off2.Offset)
                    {
                        (off1, off2) = (off2, off1);
                        (idx1, idx2) = (idx2, idx1);
                    }

                    List<Point3D> points = null;
                    if (idx2plusminus == 1)
                    {
                        points = new List<Point3D>
                        {
                            off1.Boundary[(idx1 + 1) % off1.Boundary.Count],
                            off2.Boundary[(idx2 + 1) % off2.Boundary.Count],
                            off2.Boundary[idx2],
                            off1.Boundary[idx1],
                        };
                    }
                    else
                    {
                        points = new List<Point3D>
                        {
                            off1.Boundary[(idx1 + 1) % off1.Boundary.Count],
                            off2.Boundary[idx2],
                            off2.Boundary[(idx2 + 1) % off2.Boundary.Count],
                            off1.Boundary[idx1],
                        };
                    }


                    oface.AdditionalQuads.Add(points);
                    oface.AdditionalEdges.AddRange(points);
                }
            }
        }

        private static OffsetFace FaceOffsetFromDirection(GeometryModelData model, Face face, int orient)
        {
            return model.OffsetModel.Faces[(face, (GeometricOrientation)orient)];
        }

        private static bool IsJumpFace(Face f1, double f1offset, Face f2, double f2offset)
        {
            if (Math.Abs(Vector3D.DotProduct(f1.Normal, f2.Normal)) > 0.99)
            {
                return Math.Abs(f1offset - f2offset) > 0.001;
            }
            return false;
        }
    }
}