using Assimp;
using SIMULTAN;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides algorithms operation on Volume instances
    /// </summary>
    public static class VolumeAlgorithms
    {

        /// <summary>
        /// Finds and returns all faces, their holes and hole faces.
        /// Hole faces may be null if they do not have ones.
        /// </summary>
        /// <param name="volume">The volume</param>
        /// <returns>All faces of holes that are in the volume.</returns>
        private static List<(PFace parent, EdgeLoop hole, Face holeFace)> FindHoleFaces(Volume volume)
        {
            return volume.Faces.SelectMany(face => face.Face.Holes.Select(hole => (face, hole, hole.Faces.Find(b => b != face.Face)))).ToList();
        }

        /// <summary>
        /// Finds a consistent orientation by adjusting PFace.Orientation
        /// </summary>
        /// <param name="volume">The volume to operate on</param>
        /// <returns>True when a consistent volume was found, False when no consistent volume is found</returns>
        public static bool FindConsistentOrientation(Volume volume)
        {
            var toProcess = new Stack<PFace>();

            // find all hole faces
            var holes = FindHoleFaces(volume);
            var holeLookup = holes.Select(x => x.hole).ToHashSet();
            var holeFaces = holes.Where(x => x.holeFace != null).Select(x => x.holeFace).ToHashSet();
            var edgeHoleLookup = holeLookup.SelectMany(hole => hole.Edges.Select(edge => (edge.Edge, hole)))
                .GroupBy(x => x.Edge)
                .ToDictionary(k => k.Key, k => k.Select(x => x.hole).ToList());

            var allVertices = volume.Faces.SelectMany(x => x.Face.Boundary.Edges.Select(pe => pe.StartVertex)).Distinct().Select(x => x.Position).ToList();
            //Find a surface that has all other vertices in the same halfspace. The half-space where the vertices lie is then defined as inside.
            foreach (var face in volume.Faces)
            {
                if (!holeFaces.Contains(face.Face)) // exclude holes
                {
                    var hs = FaceAlgorithms.Halfspace(face.Face, allVertices);

                    if (hs != GeometricOrientation.Undefined)
                    {
                        face.Orientation = hs;
                        toProcess.Push(face);
                        break;
                    }
                }
            }

            // TODO: What should happen if we cannot find a starting face to process? In some cases this is possible

            // Build lookup table for faces with same edges
            // (no need to check for holes recursively cause the faces of the holes need to be in the volumes face list anyway)
            var edgeFaceLookup = new Dictionary<Edge, (bool isHoleEdge, List<(PFace face, bool isHole)> faces)>();
            foreach (var face in volume.Faces)
            {
                var isHole = holeLookup.Contains(face.Face.Boundary);
                // the combined boundary of all holes except edges that are in multiple holes.
                // We need to make sure to not include edge that are in multiple holes,
                // otherwise the assumption of an edge only has two adjacent faces is wrong
                var allHolesBoundary = face.Face.Holes.SelectMany(h => h.Edges.Select(x => x.Edge))
                    .GroupBy(x => x).Where(x => x.Count() == 1).Select(x => x.Key);
                foreach (var (pedge, isHoleEdge) in face.Face.Boundary.Edges.Select(x => (x.Edge, false))
                    .Concat(allHolesBoundary.Select(x => (x, true)))) // only add hole edges once cause holes could share and edge
                {
                    if (edgeFaceLookup.TryGetValue(pedge, out var elist))
                    {
                        elist.faces.Add((face, isHole));
                        // if it turns out that the edge is actually a hole edge, we need to update the entry
                        if (elist.isHoleEdge != isHoleEdge && isHoleEdge)
                        {
                            edgeFaceLookup[pedge] = (true, elist.faces);
                        }
                    }
                    else
                    {
                        var list = new List<(PFace face, bool isHole)> { (face, isHole) };
                        edgeFaceLookup.Add(pedge, (isHoleEdge, list));
                    }
                }
            }

            // All edges must have two adjacent faces, otherwise it is not a closed volume
            if (edgeFaceLookup.Values.Any(x => x.faces.Count != 2))
            {
                volume.Faces.ForEach(f => f.Orientation = GeometricOrientation.Undefined);
                return false;
            }

            HashSet<PFace> processedFaces = new HashSet<PFace>();
            while (toProcess.Any())
            {
                var currentFace = toProcess.Pop();

                if (processedFaces.Add(currentFace))
                {
                    // Check adjacent faces
                    foreach (var e in currentFace.Face.Boundary.Edges
                        .Concat(currentFace.Face.Holes.SelectMany(x => x.Edges)))
                    {
                        if (!edgeFaceLookup.ContainsKey(e.Edge)) // can happen when geometry is inconsistent after cleanup
                        {
                            volume.Faces.ForEach(f => f.Orientation = GeometricOrientation.Undefined);
                            return false;
                        }

                        var (isEdgeOfHole, otherFaces) = edgeFaceLookup[e.Edge];
                        var (otherPFace, isHoleFace) = otherFaces.First(x => x.face != currentFace); // otherFaces always contains 2 faces

                        if (!processedFaces.Contains(otherPFace))
                        {
                            if (isHoleFace) // if the other face is the face of a hole, needs special handling cause it does not share edges with the containing face
                            {
                                if (otherPFace.Orientation == GeometricOrientation.Undefined)
                                    otherPFace.Orientation = GeometricOrientation.Forward;

                                // compare face normal and hole face normal (which already contains the face orientation) to figure out the pface orientation
                                // make sure we just have 1/-1 for numerical stability, hole and face need to be on sample plane anyway
                                int dir = SimVector3D.DotProduct(currentFace.Face.Normal, otherPFace.Face.Normal) > 0 ? 1 : -1;

                                if ((int)currentFace.Orientation != (int)otherPFace.Orientation * dir)
                                {
                                    otherPFace.Orientation = (GeometricOrientation)(-(int)(otherPFace.Orientation));
                                }
                            }
                            else
                            {
                                if (otherPFace.Orientation == GeometricOrientation.Undefined)
                                {
                                    otherPFace.Orientation = GeometricOrientation.Forward;
                                }

                                var otherPEdge = otherPFace.Face.Boundary.Edges.FirstOrDefault(pe => pe.Edge == e.Edge);

                                bool otherIsContainingFace = false; // determines if the otherPFace is the face containing the hole that the current pface is attached to
                                if (otherPEdge == null) // cannot find if otherPFace is the face containing the hole (currentFace is the additional face attached to a hole (not the face of the hole))
                                {
                                    // find the edge in the hole boundary instead in that case
                                    if (edgeHoleLookup.TryGetValue(e.Edge, out var boundary))
                                    {
                                        if (boundary.Count > 1) // found an edge that is in two holes, but in this case we should have found a hole face or it is inconsistent (hole face is missing)
                                            return false;
                                        otherIsContainingFace = true;
                                        otherPEdge = boundary.First().Edges.FirstOrDefault(pe => pe.Edge == e.Edge);
                                        if (otherPEdge == null)
                                        {
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }

                                // if the current edge is a hole we need to calculate the hole orientation and use that instead of the containing face
                                if (isEdgeOfHole)
                                {
                                    if (otherIsContainingFace) // currentFace is the face attached to a hole, e is a PEdge of the face adjacent to the hole
                                    {
                                        // Calculate current "virtual pface" orientation of an imaginary hole face.
                                        var holeBoundary = edgeHoleLookup[e.Edge];
                                        if (holeBoundary.Count > 1) // found an edge that is in two holes, but in this case we should have found a hole face or it is inconsistent (hole face is missing)
                                            return false;
                                        var normal = EdgeLoopAlgorithms.NormalCCW(holeBoundary.First());
                                        int dir = SimVector3D.DotProduct(otherPFace.Face.Normal, normal) > 0 ? 1 : -1; // 1 if same dir as containing face -1 otherwise
                                        var otherOrientation = (GeometricOrientation)(dir * (int)otherPFace.Orientation);
                                        if ((int)otherOrientation * (int)otherPEdge.Orientation !=
                                            (int)currentFace.Orientation * (int)currentFace.Face.Orientation * (int)e.Orientation)
                                        {
                                            otherPFace.Orientation = (GeometricOrientation)(-(int)(otherPFace.Orientation));
                                        }
                                    }
                                    else // currentFace is face that contains the holes, e is an PEdge of the hole boundary
                                    {
                                        // Calculate current "virtual pface" orientation of an imaginary hole face.
                                        // Hole face orientation is assumed forward (1) in CCW boundary normal direction
                                        var normal = EdgeLoopAlgorithms.NormalCCW((EdgeLoop)e.Parent);
                                        int dir = SimVector3D.DotProduct(currentFace.Face.Normal, normal) > 0 ? 1 : -1; // 1 if same dir as containing face -1 otherwise
                                        var currentOrientation = (GeometricOrientation)(dir * (int)currentFace.Orientation);
                                        if ((int)currentOrientation * (int)e.Orientation !=
                                            (int)otherPFace.Orientation * (int)otherPFace.Face.Orientation * (int)otherPEdge.Orientation)
                                        {
                                            otherPFace.Orientation = (GeometricOrientation)(-(int)(otherPFace.Orientation));
                                        }
                                    }
                                }
                                else
                                {
                                    //Orientation is fine when edge goes in the opposite direction in adjacent faces.
                                    //Since faces also have orientation, this means different orientations when face orientation is similar
                                    // and similar orientation when face orientations are the other way round.
                                    if ((int)currentFace.Orientation * (int)currentFace.Face.Orientation * (int)e.Orientation ==
                                        (int)otherPFace.Orientation * (int)otherPFace.Face.Orientation * (int)otherPEdge.Orientation)
                                    {
                                        //Not correct, turn around other pface
                                        otherPFace.Orientation = (GeometricOrientation)(-(int)(otherPFace.Orientation));
                                    }
                                }
                            }

                            toProcess.Push(otherPFace);
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Returns all edges where a volume is not closed properly (either because there is only one adjacent face, 
        /// or because there are more than two adjacent faces)
        /// </summary>
        /// <param name="volume">The volume for which edges are checked</param>
        /// <returns>All edges where the volume is not closed</returns>
        public static IEnumerable<Edge> FindUnclosedEdges(Volume volume)
        {
            if (volume.IsConsistentOriented)
                return Enumerable.Empty<Edge>();

            HashSet<Edge> unclosedEdges = new HashSet<Edge>();
            HashSet<EdgeLoop> volumeFaceLoops = volume.Faces.Select(x => x.Face.Boundary)
                .Concat(volume.Faces.SelectMany(x => x.Face.Holes)).ToHashSet();

            foreach (var pface in volume.Faces)
            {
                foreach (var pedge in pface.Face.Boundary.Edges.Concat(pface.Face.Holes.SelectMany(x => x.Edges)))
                {
                    if (pedge.Edge.PEdges.Where(x => x.Parent is EdgeLoop).Count(x => volumeFaceLoops.Contains((EdgeLoop)x.Parent)) != 2)
                    {
                        unclosedEdges.Add(pedge.Edge);
                    }
                }
            }

            return unclosedEdges;
        }

        /// <summary>
        /// Calculates the center (3D average) of a volume
        /// </summary>
        /// <param name="v">The volume</param>
        /// <returns>The center</returns>
        public static SimPoint3D Center(Volume v)
        {
            List<BaseGeometry> geom = new List<BaseGeometry>();
            ContainedGeometry(v, ref geom);

            SimVector3D center = new SimVector3D(0, 0, 0);
            int count = 0;
            foreach (var vert in geom.Distinct().Where(x => x is Vertex))
            {
                center += (SimVector3D)((Vertex)vert).Position;
                count++;
            }

            center /= count;

            return (SimPoint3D)center;
        }

        /// <summary>
        /// Returns all the geometry contained in this edge (volume + faces + edge + vertices)
        /// </summary>
        /// <param name="v">The volume</param>
        /// <param name="geometries">The resulting geometry list</param>
        public static void ContainedGeometry(Volume v, ref List<BaseGeometry> geometries)
        {
            geometries.Add(v);

            foreach (var f in v.Faces)
                FaceAlgorithms.ContainedGeometry(f.Face, ref geometries);
        }

        /// <summary>
        /// Merges two volumes that share a common surface. (Second volume gets deleted)
        /// </summary>
        /// <param name="v1">First volume</param>
        /// <param name="v2">Second volume</param>
        public static List<BaseGeometry> MergeVolumes(Volume v1, Volume v2)
        {
            if (v1.ModelGeometry != v2.ModelGeometry)
                throw new Exception("Connect does not work between different models");

            var commonSurfaces = v1.Faces.Where(pface => v2.Faces.FirstOrDefault(v2pface => pface.Face == v2pface.Face) != null).Select(x => x.Face).ToList();
            List<BaseGeometry> removedGeometry = new List<BaseGeometry>(commonSurfaces);

            if (commonSurfaces.Count > 0)
            {
                removedGeometry.Add(v2);

                v1.ModelGeometry.StartBatchOperation();

                //Remove common faces from both volumes
                RemoveFacesFromVolume(v1, commonSurfaces);
                RemoveFacesFromVolume(v2, commonSurfaces);

                foreach (var pf in v2.Faces)
                {
                    v1.AddFace(pf.Face);
                }

                //Remove one of the volumes
                v2.RemoveFromModel();
                //Remove common faces
                commonSurfaces.Where(f => f.PFaces.Count <= 2).ForEach(x =>
                {
                    x.RemoveFromModel();
                }
                );

                //Remove unused edge loops
                foreach (var cs in commonSurfaces)
                {
                    if (cs.Boundary.Faces.All(x => commonSurfaces.Contains(x))) //Keep it if it is used in some other geometry (e.g. as a hole)
                        cs.Boundary.RemoveFromModel();
                }

                v1.ModelGeometry.EndBatchOperation();
            }

            return removedGeometry;
        }

        /// <summary>
        /// Removes faces from a volume. The volume might become inconsistent afterwards
        /// </summary>
        /// <param name="v">The volume where the faces should be removed</param>
        /// <param name="faces">The faces to remove</param>
        public static void RemoveFacesFromVolume(Volume v, List<Face> faces)
        {
            for (int i = 0; i < v.Faces.Count; ++i)
            {
                if (faces.Contains(v.Faces[i].Face))
                {
                    v.Faces.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Adds faces from a volume. The volume might become inconsistent afterwards
        /// </summary>
        /// <param name="v">The volume where the faces should be added</param>
        /// <param name="faces">The faces to add</param>
        public static void AddFacesToVolume(Volume v, IEnumerable<Face> faces)
        {
            if (faces.Any(x => x.ModelGeometry != v.ModelGeometry))
                throw new Exception("Connect does not work between different models");

            foreach (var f in faces)
            {
                if (!v.Faces.Any(x => x.Face == f)) //Not already in volume
                {
                    v.Faces.Add(new PFace(f, v, GeometricOrientation.Undefined));
                }
            }
        }


        /// <summary>
        /// Calculates the volume of a volume
        /// </summary>
        /// <param name="vol">The volume to calculate the volume from</param>
        /// <returns>The volume of the volume</returns>
        public static double Volume(Volume vol)
        {
            if (!vol.IsConsistentOriented)
                return double.NaN;

            return Math.Abs(SignedVolume(vol.Faces.Select(x => (x.Face, x.Orientation))));
        }

        /// <summary>
        /// Computes the signed volume of a list of faces
        /// </summary>
        /// <param name="faces">List of faces. Must not be closed.</param>
        /// <returns>Signed volume from face list</returns>
        public static double SignedVolume(IEnumerable<(Face f, GeometricOrientation o)> faces)
        {
            double sumVol = 0;

            var d = new SimPoint3D(0, 0, 0);

            var holeFaces = faces.SelectMany(face => face.f.Holes.Select(hole => hole.Faces.Find(b => b != face.f))).ToHashSet();

            var faceLookup = faces.Select(x => x.f).ToHashSet();

            foreach (var face in faces)
            {
                if (holeFaces.Contains(face.f)) // ignore hole faces, they don't count for the volume
                    continue;

                (var pos, _, var ind) = FaceAlgorithms.TriangulateBoundary(face.f, face.o);

                for (int i = 0; i < ind.Count; i += 3)
                {
                    var ind1 = ind[i + 1];
                    var ind2 = ind[i + 2];

                    if (face.f.Orientation == GeometricOrientation.Backward)
                    {
                        ind1 = ind[i + 2];
                        ind2 = ind[i + 1];
                    }

                    var tetraVol = SimVector3D.DotProduct(pos[ind[i]] - d, SimVector3D.CrossProduct(pos[ind1] - d, pos[ind2] - d));
                    sumVol += tetraVol;
                }

                foreach (var hole in face.f.Holes)
                {
                    var holeFace = hole.Faces.Find(x => x != face.f);

                    // subtract the hole volume if it doesn't have a face or if the hole face is not in the volume
                    if (holeFace == null || !faceLookup.Contains(holeFace))
                    {
                        var boundaryNormal = EdgeLoopAlgorithms.NormalCCW(hole);
                        int dir = SimVector3D.DotProduct(face.f.Normal * (int)face.o, boundaryNormal) > 0 ? 1 : -1;

                        (pos, _, ind) = FaceAlgorithms.TriangulateBoundary(hole.Edges.Select(x => x.StartVertex.Position).ToList(), face.f.Normal * (int)face.o);
                        for (int i = 0; i < ind.Count; i += 3)
                        {
                            var ind1 = ind[i + 1];
                            var ind2 = ind[i + 2];

                            if (dir == -1) // flip if hole boundary points in other direction
                            {
                                ind1 = ind[i + 2];
                                ind2 = ind[i + 1];
                            }

                            var tetraVol = SimVector3D.DotProduct(pos[ind[i]] - d, SimVector3D.CrossProduct(pos[ind1] - d, pos[ind2] - d));
                            sumVol -= tetraVol;
                        }
                    }
                }
            }
            return sumVol / 6.0;
        }

        private static double Volume(List<SimPoint3D> referenceFace, GeometricOrientation faceOrientation, GeometricOrientation pfaceOrientation,
            List<SimPoint3D> offsetBoundary)
        {
            //Assumption: referenceFace and offsetBoundary have the same number of vertices
            if (referenceFace.Count != offsetBoundary.Count)
                return double.NaN;

            double sumVol = 0;
            var d = new SimPoint3D(0, 0, 0);

            SimVector3D normal = EdgeLoopAlgorithms.NormalCCW(referenceFace) * (int)faceOrientation * (int)pfaceOrientation;

            //Reference face
            {
                (var pos, _, var ind) = FaceAlgorithms.TriangulateBoundary(referenceFace, normal);

                for (int i = 0; i < ind.Count; i += 3)
                {
                    var ind1 = ind[i + 1];
                    var ind2 = ind[i + 2];

                    if (faceOrientation == GeometricOrientation.Backward)
                    {
                        ind1 = ind[i + 2];
                        ind2 = ind[i + 1];
                    }

                    var tetraVol = SimVector3D.DotProduct(pos[ind[i]] - d, SimVector3D.CrossProduct(pos[ind1] - d, pos[ind2] - d));
                    sumVol += tetraVol;
                }
            }

            //Offset face
            {
                (var pos, _, var ind) = FaceAlgorithms.TriangulateBoundary(offsetBoundary, normal);

                for (int i = 0; i < ind.Count; i += 3)
                {
                    var ind1 = ind[i + 1];
                    var ind2 = ind[i + 2];

                    if (faceOrientation == GeometricOrientation.Forward)
                    {
                        ind1 = ind[i + 2];
                        ind2 = ind[i + 1];
                    }

                    var tetraVol = SimVector3D.DotProduct(pos[ind[i]] - d, SimVector3D.CrossProduct(pos[ind1] - d, pos[ind2] - d));
                    sumVol += tetraVol;
                }
            }

            //Side faces
            for (int i = 0; i < referenceFace.Count; i++)
            {
                var ind1 = i;
                var ind2 = (i + 1) % referenceFace.Count;

                var rv1 = referenceFace[ind1];
                var rv2 = referenceFace[ind2];
                var ov1 = offsetBoundary[ind1];
                var ov2 = offsetBoundary[ind2];

                if (faceOrientation == GeometricOrientation.Forward)
                {
                    (rv1, rv2) = (rv2, rv1);
                    (ov1, ov2) = (ov2, ov1);
                }

                var tetraVol1 = SimVector3D.DotProduct(rv1 - d, SimVector3D.CrossProduct(rv2 - d, ov2 - d));
                sumVol += tetraVol1;
                var tetraVol2 = SimVector3D.DotProduct(rv1 - d, SimVector3D.CrossProduct(ov2 - d, ov1 - d));
                sumVol += tetraVol2;
            }

            return Math.Abs(sumVol / 6.0);
        }

        /// <summary>
        /// Returns the gross and the net volume
        /// </summary>
        /// <param name="vol">The volume</param>
        /// <returns>The gross and net volume as given by the offset surfaces</returns>
        public static (double volume, double volumeBrutto, double volumeNetto) VolumeBruttoNetto(Volume vol)
        {
            //General algorithm:
            //1. Calc volume of reference planes
            //2. Subtract area for each wall (by using the offset-face)
            var refVolume = Volume(vol);

            var netto = refVolume;
            var brutto = refVolume;

            foreach (var pface in vol.Faces)
            {
                if (vol.ModelGeometry.OffsetModel.Faces.ContainsKey((pface.Face, pface.Orientation)))
                {
                    var offsetFace1 = vol.ModelGeometry.OffsetModel.Faces[(pface.Face, pface.Orientation)];
                    var offsetFace2 = vol.ModelGeometry.OffsetModel.Faces[(pface.Face, (GeometricOrientation)(-(int)pface.Orientation))];

                    //Boundary
                    {
                        var nettoVol = Volume(pface.Face.Boundary.Edges.Select(x => x.StartVertex.Position).ToList(),
                            pface.Face.Orientation, pface.Orientation,
                            offsetFace1.Boundary);

                        netto -= nettoVol;

                        if (pface.Face.PFaces.Count < 2)
                        {
                            var bruttoVol = Volume(offsetFace2.Boundary, pface.Face.Orientation, pface.Orientation,
                                pface.Face.Boundary.Edges.Select(x => x.StartVertex.Position).ToList());
                            brutto += bruttoVol;
                        }
                    }

                    //Holes
                    foreach (var hole in pface.Face.Holes)
                    {
                        var offsetFace1Hole = offsetFace1.Openings[hole];
                        var offsetFace2Hole = offsetFace2.Openings[hole];

                        var orient = (int)pface.Face.Orientation;
                        if (SimVector3D.DotProduct(pface.Face.Normal, EdgeLoopAlgorithms.NormalCCW(hole)) > 0)
                            orient *= -1;

                        var nettoVol = Volume(hole.Edges.Select(x => x.StartVertex.Position).ToList(),
                            (GeometricOrientation)orient, pface.Orientation,
                            offsetFace1Hole);

                        netto += nettoVol;

                        if (pface.Face.PFaces.Count < 2)
                        {
                            var bruttoVol = Volume(offsetFace2Hole, (GeometricOrientation)orient, pface.Orientation,
                                hole.Edges.Select(x => x.StartVertex.Position).ToList());
                            brutto -= bruttoVol;
                        }
                    }
                }
            }

            return (refVolume, brutto, netto);
        }

        /// <summary>
        /// Returns the minimum and maximum height of a volume
        /// </summary>
        /// <param name="vol">The volume</param>
        /// <returns>The minimum and maximum height difference between floor and ceiling</returns>
        public static (double reference, double min, double max) Height(Volume vol)
        {
            var floorFaces = vol.Faces.Where(x => FaceAlgorithms.IsFloor(x)).ToList();
            var ceilingFaces = vol.Faces.Where(x => FaceAlgorithms.IsCeiling(x)).ToList();

            var ceilInnerParams = FacesYMinMax(ceilingFaces, vol.ModelGeometry, GeometricOrientation.Backward);
            var floorInnerParams = FacesYMinMax(floorFaces, vol.ModelGeometry, GeometricOrientation.Backward);

            var ceilOuterParams = FacesYMinMax(ceilingFaces, vol.ModelGeometry, GeometricOrientation.Forward);
            var floorOuterParams = FacesYMinMax(floorFaces, vol.ModelGeometry, GeometricOrientation.Forward);

            var refFloorParams = FacesYMinMax(floorFaces.Select(x => x.Face));
            var refCeilParams = FacesYMinMax(ceilingFaces.Select(x => x.Face));

            return ((refCeilParams.max + refCeilParams.min) / 2.0 - (refFloorParams.max + refFloorParams.min) / 2.0,
                ceilInnerParams.min - floorInnerParams.max, ceilOuterParams.max - floorOuterParams.min);
        }

        private static (double min, double max) FacesYMinMax(IEnumerable<PFace> faces, GeometryModelData model, GeometricOrientation orientation)
        {
            var min = double.PositiveInfinity;
            var max = double.NegativeInfinity;

            foreach (var pf in faces)
            {
                if (!model.OffsetModel.Faces.ContainsKey((pf.Face, pf.Orientation)))
                    return (double.NaN, double.NaN); //Happens when the size is requested for an already deleted volume

                OffsetFace offset = model.OffsetModel.Faces[(pf.Face, pf.Orientation)];

                min = Math.Min(offset.Boundary.Min(x => x.Y), min);
                max = Math.Max(offset.Boundary.Max(x => x.Y), max);
            }

            return (min, max);
        }

        private static (double min, double max) FacesYMinMax(IEnumerable<Face> faces)
        {
            var yvalues = faces.SelectMany(x => x.Boundary.Edges.Select(e => e.StartVertex.Position.Y));
            if (yvalues.Count() == 0)
                return (double.NaN, double.NaN);
            var min = yvalues.Min();
            var max = yvalues.Max();
            return (min, max);
        }

        /// <summary>
        /// Calculates the area of the floor for the reference geometry, as well as for net and gross offsets.
        /// AREABRUTTO IS ALWAYS double.NaN as it is still unclear how to calculate it.
        /// </summary>
        /// <param name="volume">The volume</param>
        /// <returns>
        /// areaReference: Area of the reference geometry of the floor,
        /// areaBrutto: Always double.NaN
        /// areaNetto: Net area (inner area) of the floor
        /// </returns>
        public static (double areaReference, double areaBrutto, double areaNetto) AreaBruttoNetto(Volume volume)
        {
            //Reference area
            var floorFaces = volume.Faces.Where(x => FaceAlgorithms.IsFloor(x)).ToList();

            double areaReference = 0.0, areaNetto = 0.0;
            foreach (var pf in floorFaces)
            {
                var area = FaceAlgorithms.AreaInnerOuter(pf.Face);
                var innerArea = pf.Orientation == GeometricOrientation.Forward ? area.outerArea : area.innerArea;

                areaReference += area.area;
                areaNetto += innerArea;
            }

            return (areaReference, double.NaN, areaNetto);
        }
        /// <summary>
        /// Calculates absolute elevations for upper floor surfaces (Fussbodenoberkante) and lowest ceiling surface (Deckenunterkante)
        /// </summary>
        /// <param name="volume">The volume</param>
        /// <returns>
        /// floor: Highest point on the floor
        /// ceiling: Lowest point on the ceiling
        /// </returns>
        public static (double floor, double ceiling) Elevation(Volume volume)
        {
            var floorFaces = volume.Faces.Where(x => FaceAlgorithms.IsFloor(x));
            var ceilingFaces = volume.Faces.Where(x => FaceAlgorithms.IsCeiling(x));

            var ceilInnerParams = FacesYMinMax(ceilingFaces, volume.ModelGeometry, GeometricOrientation.Backward);
            var floorInnerParams = FacesYMinMax(floorFaces, volume.ModelGeometry, GeometricOrientation.Backward);

            var floorReference = FacesYMinMax(floorFaces.Select(x => x.Face));

            return ((floorInnerParams.min + floorInnerParams.max) / 2.0, (ceilInnerParams.min + ceilInnerParams.max) / 2.0);
        }
        /// <summary>
        /// Calculates absolute elevations of the reference geometry
        /// </summary>
        /// <param name="volume">The volume</param>
        /// <returns>
        /// floor: Average height of the floor reference geometry
        /// ceiling: Average height of the ceiling reference geometry
        /// </returns>
        public static (double floor, double ceiling) ElevationReference(Volume volume)
        {
            var floorFaces = volume.Faces.Where(x => FaceAlgorithms.IsFloor(x)).Select(x => x.Face);
            var ceilingFaces = volume.Faces.Where(x => FaceAlgorithms.IsCeiling(x)).Select(x => x.Face);

            var floorParams = FacesYMinMax(floorFaces);
            var ceilParams = FacesYMinMax(ceilingFaces);

            return ((floorParams.max + floorParams.min) / 2.0, (ceilParams.max + ceilParams.min) / 2.0);
        }

        /// <summary>
        /// Calculates the perimeter of the floor.
        /// Sums up the perimeter sizes of all floor faces.
        /// DOES NOT HANDLE DUPLICATE EDGES (E.G. WHEN TWO FLOOR POLYGONS INTERSECT)
        /// </summary>
        /// <param name="volume">The floor</param>
        /// <returns>The perimeter length of the floor.</returns>
        public static double FloorPerimeter(Volume volume)
        {
            double peri = 0.0;

            foreach (var pf in volume.Faces.Where(x => FaceAlgorithms.IsFloor(x)))
            {
                if (pf.Orientation == GeometricOrientation.Forward)
                    peri += FaceAlgorithms.Perimeter(pf.Face).inner;
                else if (pf.Orientation == GeometricOrientation.Backward)
                    peri += FaceAlgorithms.Perimeter(pf.Face).outer;
            }

            return peri;
        }

        /// <summary>
        /// Tests if a point is inside a volume
        /// </summary>
        /// <param name="volume">The volume</param>
        /// <param name="point">The point</param>
        /// <returns>True when the point lies inside the volume, otherwise False</returns>
        public static bool IsInside(Volume volume, SimPoint3D point)
        {
            int count = 0;

            foreach (var pface in volume.Faces)
            {
                if (FaceAlgorithms.IntersectsRay(pface.Face, point, new SimVector3D(1, 0, 0)))
                    count++;
            }

            if (count % 2 == 1)
                return true;

            return false;
        }
    }
}
