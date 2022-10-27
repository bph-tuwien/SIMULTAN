using SIMULTAN;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides algorithms operation on Volume instances
    /// </summary>
    public static class VolumeAlgorithms
    {
        /// <summary>
        /// Finds a consistent orientation by adjusting PFace.Orientation
        /// </summary>
        /// <param name="volume">The volume to operate on</param>
        /// <returns>True when a consistent volume was found, False when no consistent volume is found</returns>
        public static bool FindConsistentOrientation(Volume volume)
        {
            List<PFace> toProcess = new List<PFace>();

            var allVertices = volume.Faces.SelectMany(x => x.Face.Boundary.Edges.Select(pe => pe.StartVertex)).Distinct().Select(x => x.Position).ToList();
            //Find a surface that has all other vertices in the same halfspace. The half-space where the vertices lie is then definined as inside.
            foreach (var face in volume.Faces)
            {
                var hs = FaceAlgorithms.Halfspace(face.Face, allVertices);

                if (hs != GeometricOrientation.Undefined)
                {
                    face.Orientation = hs;
                    toProcess.Add(face);
                    break;
                }
            }


            //Build lookuptable for faces with same edges
            Dictionary<Edge, List<PFace>> edgeFaceLookup = new Dictionary<Edge, List<PFace>>();
            foreach (var face in volume.Faces)
            {
                foreach (var pedge in face.Face.Boundary.Edges)
                {
                    if (edgeFaceLookup.TryGetValue(pedge.Edge, out var elist))
                        elist.Add(face);
                    else
                        edgeFaceLookup.Add(pedge.Edge, new List<PFace> { face });
                }
            }


            HashSet<PFace> processedFaces = new HashSet<PFace>();

            while (toProcess.Count > 0)
            {
                var currentFace = toProcess.Last();
                toProcess.RemoveAt(toProcess.Count - 1);

                if (!processedFaces.Contains(currentFace))
                {
                    processedFaces.Add(currentFace);

                    //Check adjacent triangles
                    foreach (var e in currentFace.Face.Boundary.Edges)
                    {
                        //var otherPFace = volume.Faces.FirstOrDefault(x => x != currentFace && x.Face.Boundary.Edges.Any(pe => pe.Edge == e.Edge));
                        var otherPFace = edgeFaceLookup[e.Edge].FirstOrDefault(x => x != currentFace);

                        if (otherPFace == null) //Error, abort. Happens when the volume is not closed
                        {
                            volume.Faces.ForEach(f => f.Orientation = GeometricOrientation.Undefined);
                            return false;
                        }

                        if (!processedFaces.Contains(otherPFace))
                        {
                            if (otherPFace.Orientation == GeometricOrientation.Undefined)
                            {
                                otherPFace.Orientation = GeometricOrientation.Forward;
                            }

                            var otherPEdge = otherPFace.Face.Boundary.Edges.First(pe => pe.Edge == e.Edge);

                            //Orientation is fine when edge goes in the opposite direction in adjacent faces.
                            //Since faces also have orientation, this means different orientations when face orientation is similar
                            // and similar orientation when face orientations are the other way round.
                            if ((int)currentFace.Orientation * (int)currentFace.Face.Orientation * (int)e.Orientation ==
                                (int)otherPFace.Orientation * (int)otherPFace.Face.Orientation * (int)otherPEdge.Orientation)
                            {
                                //Not correct, turn around other pface
                                otherPFace.Orientation = (GeometricOrientation)(-(int)(otherPFace.Orientation));
                            }

                            toProcess.Add(otherPFace);
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
            HashSet<EdgeLoop> volumeFaceLoops = volume.Faces.Select(x => x.Face.Boundary).ToHashSet();

            foreach (var pface in volume.Faces)
            {
                foreach (var pedge in pface.Face.Boundary.Edges)
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
        public static Point3D Center(Volume v)
        {
            List<BaseGeometry> geom = new List<BaseGeometry>();
            ContainedGeometry(v, ref geom);

            Vector3D center = new Vector3D(0, 0, 0);
            int count = 0;
            foreach (var vert in geom.Distinct().Where(x => x is Vertex))
            {
                center += (Vector3D)((Vertex)vert).Position;
                count++;
            }

            center /= count;

            return (Point3D)center;
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

            double sumVol = 0;

            //var d = vol.Faces[0].Face.Boundary.Edges[0].StartVertex.Position;
            var d = new Point3D(10, 10, 10);

            foreach (var face in vol.Faces)
            {
                (var pos, _, var ind) = FaceAlgorithms.TriangulateBoundary(face);

                for (int i = 0; i < ind.Count; i += 3)
                {
                    var ind1 = ind[i + 1];
                    var ind2 = ind[i + 2];

                    if (face.Face.Orientation == GeometricOrientation.Backward)
                    {
                        ind1 = ind[i + 2];
                        ind2 = ind[i + 1];
                    }

                    var tetraVol = Vector3D.DotProduct(pos[ind[i]] - d, Vector3D.CrossProduct(pos[ind1] - d, pos[ind2] - d));
                    sumVol += tetraVol;
                }
            }

            return Math.Abs(sumVol / 6.0);
        }

        /// <summary>
        /// Computes the signed volume of a list of faces
        /// </summary>
        /// <param name="faces">List of faces. Must not be closed.</param>
        /// <returns>Signed volume from face list</returns>
        public static double SignedVolume(List<(Face f, GeometricOrientation o)> faces)
        {
            double sumVol = 0;

            var d = new Point3D(0, 0, 0);

            foreach (var face in faces)
            {
                (var pos, _, var ind) = FaceAlgorithms.TriangulateBoundary(face.f, face.f.Orientation);

                for (int i = 0; i < ind.Count; i += 3)
                {
                    var ind1 = ind[i + 1];
                    var ind2 = ind[i + 2];

                    if (face.o == GeometricOrientation.Backward)
                    {
                        ind1 = ind[i + 2];
                        ind2 = ind[i + 1];
                    }

                    var tetraVol = Vector3D.DotProduct(pos[ind[i]] - d, Vector3D.CrossProduct(pos[ind1] - d, pos[ind2] - d));
                    sumVol += tetraVol;
                }
            }

            return sumVol / 6.0;
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
                var refArea = FaceAlgorithms.Area(pface.Face);
                var nettoArea = refArea;
                var nettoOffset = 0.0;
                var bruttoArea = refArea;
                var bruttoOffset = 0.0;

                if (vol.ModelGeometry.OffsetModel.Faces.ContainsKey((pface.Face, pface.Orientation)))
                {
                    var offsetFace1 = vol.ModelGeometry.OffsetModel.Faces[(pface.Face, pface.Orientation)];
                    var offsetFace2 = vol.ModelGeometry.OffsetModel.Faces[(pface.Face, (GeometricOrientation)(-(int)pface.Orientation))];

                    nettoArea = EdgeLoopAlgorithms.Area(offsetFace1.Boundary);
                    nettoOffset = offsetFace1.Offset;

                    if (pface.Face.PFaces.Count < 2)
                    {
                        bruttoArea = EdgeLoopAlgorithms.Area(offsetFace2.Boundary);
                        bruttoOffset = offsetFace2.Offset;
                    }
                }

                netto -= (nettoOffset / 3.0) * (refArea + nettoArea + Math.Sqrt(refArea * nettoArea));
                brutto += (bruttoOffset / 3.0) * (refArea + bruttoArea + Math.Sqrt(refArea * bruttoArea));
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
        public static bool IsInside(Volume volume, Point3D point)
        {
            int count = 0;
            
            foreach (var pface in volume.Faces)
            {
                if (FaceAlgorithms.IntersectsRay(pface.Face, point, new Vector3D(1, 0, 0)))
                    count++;
            }

            if (count % 2 == 1)
                return true;

            return false;
        }
    }
}
