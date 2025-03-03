using MathNet.Numerics.LinearAlgebra.Factorization;
using SIMULTAN;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Enumeration for describing geometric relations for intersection tests
    /// </summary>
    public enum GeometricRelation
    {
        /// <summary>
        /// Fully contained
        /// </summary>
        Contained,
        /// <summary>
        /// Intersecting
        /// </summary>
        Intersecting,
        /// <summary>
        /// No intersection, not contained
        /// </summary>
        None
    }

    /// <summary>
    /// Provides algorithms that operate on Face instances
    /// </summary>
    public static class FaceAlgorithms
    {

        /// <summary>
        /// Segments a PFace into triangles. Orientation is derived from the PFace.Orientation property.
        /// </summary>
        /// <param name="face">The face to triangulate</param>
        /// <param name="orientation">Orientation of the face</param>
        /// <returns>A tuple containing positions, normals and indices for the triangulation.</returns>
        public static (List<SimPoint3D> positions, List<SimVector3D> normals, List<int> indices) Triangulate(Face face, GeometricOrientation orientation = GeometricOrientation.Forward)
        {
            List<SimPoint3D> positions;
            List<SimVector3D> normals;
            List<int> indices;

            try
            {
                var matrix = FaceToXZMapping(face, orientation);

                (var poly, var holes, var additionalFaces) = BoundaryAndHoles(face, matrix);

                if (poly.Count == 0)
                    return (new List<SimPoint3D>(), new List<SimVector3D>(), new List<int>());

                CleanupPolygon(poly, GeometrySettings.Instance.Tolerance);
                holes.ForEach(x => CleanupPolygon(x, GeometrySettings.Instance.Tolerance));
                holes.RemoveWhere(x => x.Count == 0);
                additionalFaces.ForEach(x => CleanupPolygon(x, GeometrySettings.Instance.Tolerance));
                additionalFaces.RemoveWhere(x => x.Count == 0);

                Triangulation.PolygonComplexFill(poly, holes,
                    out positions, out indices, orientation == GeometricOrientation.Backward);

                // if we have additional faces, triangulate and combine them with the other mesh
                if (additionalFaces.Any())
                {
                    var toCombine = new List<(List<SimPoint3D> positions, List<int> indices)>() { (positions, indices) };
                    foreach (var aface in additionalFaces)
                    {
                        Triangulation.PolygonComplexFill(aface, null,
                            out var aPos, out var aIndices, orientation == GeometricOrientation.Backward);
                        toCombine.Add((aPos, aIndices));
                    }

                    (positions, indices) = Triangulation.CombineMeshes(toCombine);
                }

                if (positions == null || indices == null)
                    return (new List<SimPoint3D>(), new List<SimVector3D>(), new List<int>());

                matrix.Invert();
                for (int i = 0; i < positions.Count; ++i)
                {
                    positions[i] = matrix.Transform(positions[i]);
                }
                normals = Enumerable.Repeat(face.Normal, positions.Count).ToList();
            }
            catch (Exception)
            {
                positions = new List<SimPoint3D>();
                normals = new List<SimVector3D>();
                indices = new List<int>();
            }

            return (positions, normals, indices);
        }

        /// <summary>
        /// Triangulates a list of boundary vertices and holes
        /// </summary>
        /// <param name="boundary">The boundary</param>
        /// <param name="holes">List of holes</param>
        /// <param name="normal">The face normal</param>
        /// <param name="orientation">Orientation of the face (winding order)</param>
        /// <returns>A tuple containing position, normal and indices for the triangulation.</returns>
        public static (List<SimPoint3D> positions, List<SimVector3D> normals, List<int> indices) Triangulate(
            List<SimPoint3D> boundary, IEnumerable<List<SimPoint3D>> holes, SimVector3D normal, GeometricOrientation orientation = GeometricOrientation.Forward)
        {
            List<SimPoint3D> positions;
            List<SimVector3D> normals;
            List<int> indices;

            try
            {
                var matrix = FaceToXZMapping(boundary, normal);
                var poly = boundary.Select(x => matrix.Transform(x)).ToList();

                CleanupPolygon(poly, GeometrySettings.Instance.Tolerance);

                List<List<SimPoint3D>> holepoly = null;
                if (holes != null)
                {
                    holepoly = holes.Select(h => h.Select(x => matrix.Transform(x)).ToList()).ToList();
                    holepoly.ForEach(x => CleanupPolygon(x, GeometrySettings.Instance.Tolerance));
                }

                Triangulation.PolygonComplexFill(poly, holepoly,
                    out positions, out indices, orientation == GeometricOrientation.Backward);

                if (positions == null || indices == null)
                    return (new List<SimPoint3D>(), new List<SimVector3D>(), new List<int>());

                matrix.Invert();
                for (int i = 0; i < positions.Count; ++i)
                {
                    positions[i] = matrix.Transform(positions[i]);
                }
                normals = Enumerable.Repeat(normal, positions.Count).ToList();
            }
            catch (Exception)
            {
                positions = new List<SimPoint3D>();
                normals = new List<SimVector3D>();
                indices = new List<int>();
            }

            return (positions, normals, indices);
        }

        /// <summary>
        /// Triangulates a face, leaving away all holes
        /// </summary>
        /// <param name="boundary">Boundary vertex positions</param>
        /// <param name="normal">The face normal</param>
        /// <param name="orientation">Orientation of the face (winding order)</param>
        /// <returns>A tuple containing position, normal and indices for the triangulation.</returns>
        public static (List<SimPoint3D> positions, List<SimVector3D> normals, List<int> indices) TriangulateBoundary(List<SimPoint3D> boundary, SimVector3D normal,
            GeometricOrientation orientation = GeometricOrientation.Forward)
        {
            return Triangulate(boundary, null, normal, orientation);
        }

        /// <summary>
        /// Triangulates a face, leaving away all holes
        /// </summary>
        /// <param name="face">The face</param>
        /// <param name="orientation">Orientation of the face</param>
        /// <returns>A tuple containing position, normal and indices for the triangulation.</returns>
        public static (List<SimPoint3D> positions, List<SimVector3D> normals, List<int> indices) TriangulateBoundary(Face face, GeometricOrientation orientation)
        {
            return TriangulateBoundary(face.Boundary.Edges.Select(x => x.StartVertex.Position).ToList(), face.Normal * (double)orientation,
                orientation);
        }

        /// <summary>
        /// Triangulates a face, leaving away all holes
        /// </summary>
        /// <param name="face">The face</param>
        /// <returns>A tuple containing position, normal and indices for the triangulation.</returns>
        public static (List<SimPoint3D> positions, List<SimVector3D> normals, List<int> indices) TriangulateBoundary(PFace face)
        {
            return TriangulateBoundary(face.Face, face.Orientation);
        }

        /// <summary>
        /// Returns a matrix that maps the face into the XZ plane
        /// </summary>
        /// <param name="face">The face which should be mapped</param>
        /// <param name="orientation">Defines which side of the Face will point upwards</param>
        /// <returns>A rotation matrix</returns>
        private static SimMatrix3D FaceToXZMapping(Face face, GeometricOrientation orientation)
        {
            return FaceToXZMapping(face.Boundary.Edges.Select(x => x.StartVertex.Position).ToList(), face.Normal * (int)face.Orientation);
        }

        private static SimMatrix3D FaceToXZMapping(List<SimPoint3D> boundary, SimVector3D normal)
        {
            var basey = normal;

            var basex = boundary[1] - boundary[0];

            var idx = 2;
            while (basex.LengthSquared < GeometrySettings.Instance.ToleranceSquared && idx < boundary.Count)
            {
                basex = boundary[idx] - boundary[idx - 1];
                idx++;
            }
            if (basex.LengthSquared < GeometrySettings.Instance.ToleranceSquared)
                throw new ArgumentException("Face does not define a plane (no non-zero length edges)");

            basex.Normalize();

            var basez = SimVector3D.CrossProduct(basex, basey);

            return new SimMatrix3D(
                basex.X, basey.X, basez.X, 0.0,
                basex.Y, basey.Y, basez.Y, 0.0,
                basex.Z, basey.Z, basez.Z, 0.0,
                0.0, 0.0, 0.0, 1.0
                );
        }

        /// <summary>
        /// Returns a matrix that maps the face into the XY plane
        /// </summary>
        /// <param name="face">The face which should be mapped</param>
        /// <param name="orientation">Defines which side of the Face will point upwards</param>
        /// <returns>A rotation matrix</returns>
        public static SimMatrix3D FaceToXYMapping(Face face, GeometricOrientation orientation = GeometricOrientation.Undefined)
        {
            var orientMod = (int)orientation;
            if (orientMod == 0)
                orientMod = 1;

            var z_dir = face.Normal * orientMod;
            var x_dir = EdgeAlgorithms.Direction(face.Boundary.Edges[0]);
            var y_dir = SimVector3D.CrossProduct(z_dir, x_dir);

            return new SimMatrix3D(
                x_dir.X, y_dir.X, z_dir.X, 0,
                x_dir.Y, y_dir.Y, z_dir.Y, 0,
                x_dir.Z, y_dir.Z, z_dir.Z, 0,
                0, 0, 0, 1
                );
        }

        /// <summary>
        /// Determines the size of the face in it's embedding plane
        /// This function maps the face in the XY plane and then calculates the size there.
        /// </summary>
        /// <param name="face">The face</param>
        /// <returns>The size</returns>
        public static SimSize XYSize(Face face)
        {
            try
            {
                var mapping = FaceToXYMapping(face, GeometricOrientation.Forward);

                var points2D = face.Boundary.Edges.Select(x => mapping.Transform(x.StartVertex.Position).XY());

                var minX = points2D.Min(x => x.X);
                var maxX = points2D.Max(x => x.X);
                var minY = points2D.Min(x => x.Y);
                var maxY = points2D.Max(x => x.Y);

                return new SimSize(maxX - minX, maxY - minY);
            }
            catch (Exception)
            {
                return new SimSize(0, 0);
            }
        }

        /// <summary>
        /// Calculates the center of the face (center means geometric average)
        /// </summary>
        /// <param name="face">The face</param>
        /// <returns>The center</returns>
        public static SimPoint3D Center(Face face)
        {
            SimVector3D sum = new SimVector3D(0, 0, 0);
            face.Boundary.Edges.ForEach(x => sum += (SimVector3D)x.StartVertex.Position);
            sum = sum / (double)face.Boundary.Edges.Count;

            return (SimPoint3D)sum;
        }

        /// <summary>
        /// Returns all the geometry contained in this edge (faces + edge + vertices + hole-edges + hole-vertices)
        /// </summary>
        /// <param name="f">The face</param>
        /// <param name="geometries">The resulting geometry list</param>
        public static void ContainedGeometry(Face f, ref List<BaseGeometry> geometries)
        {
            geometries.Add(f);

            foreach (var e in f.Boundary.Edges)
            {
                EdgeAlgorithms.ContainedGeometry(e.Edge, ref geometries);
            }

            foreach (var h in f.Holes)
            {
                foreach (var e in h.Edges)
                {
                    EdgeAlgorithms.ContainedGeometry(e.Edge, ref geometries);
                }
            }
        }

        /// <summary>
        /// Checks whether a face contains a point
        /// </summary>
        /// <param name="f">The face</param>
        /// <param name="v">The point</param>
        /// <param name="tolerance">Tolerance towards the boundary</param>
        /// <param name="zTolerance">Tolerance to the plane spanned by the face</param>
        /// <returns>Contained when point is inside polygon, None otherwise</returns>
        public static GeometricRelation Contains(Face f, SimPoint3D v, double tolerance, double zTolerance)
        {
            var (polygon, mapping) = MapToXY(f);

            if (EdgeLoopAlgorithms.Contains2D(polygon, mapping.Transform(v), tolerance, zTolerance))
                return GeometricRelation.Contained;
            else
                return GeometricRelation.None;
        }
        /// <summary>
        /// Checks whether a face contains or intersects an edge
        /// </summary>
        /// <param name="f">The face</param>
        /// <param name="e">The edge</param>
        /// <param name="tolerance">Tolerance towards the boundary</param>
        /// <param name="zTolerance">Tolerance to the plane spanned by the face</param>
        /// <returns>Contained when the edge is fully contained in the polygon, Intersecting when the edge intersects the polygon boundary, None otherwise</returns>
        public static GeometricRelation Contains2D(Face f, Edge e, double tolerance, double zTolerance)
        {
            var (polygon, mapping) = MapToXY(f);

            var edgeVerts = e.Vertices.Select(x => mapping.Transform(x.Position)).ToList();

            bool anyInside = false;

            foreach (var ev in edgeVerts)
            {
                var inside = EdgeLoopAlgorithms.Contains2D(polygon, ev, tolerance, zTolerance);
                anyInside |= inside;
            }

            bool edgeIntersects = Intersects2D(polygon, edgeVerts[0], edgeVerts[1], tolerance);

            if (anyInside && !edgeIntersects)
                return GeometricRelation.Contained;
            else if (anyInside)
                return GeometricRelation.Intersecting;
            else
                return GeometricRelation.None;
        }
        /// <summary>
        /// Checks whether a face contains or intersects another face
        /// </summary>
        /// <param name="fouter">The container face</param>
        /// <param name="f">The containing face</param>
        /// <param name="tolerance">Tolerance towards the boundary</param>
        /// <param name="zTolerance">Tolerance to the plane spanned by the face</param>
        /// <returns>Contained when the face is fully contained in the polygon, Intersecting when the face intersects the polygon, None otherwise</returns>
        public static GeometricRelation Contains2D(Face fouter, Face f, double tolerance, double zTolerance)
        {
            var (polygon, mapping) = MapToXY(fouter);
            var innerPolygon = f.Boundary.Edges.Select(x => mapping.Transform(x.StartVertex.Position)).ToList();

            bool anyVertexInside = false;
            bool allVerticesInside = true;
            bool anyEdgeIntersecting = false;

            int n = innerPolygon.Count;
            for (int i = 0; i < n; ++i)
            {
                var pcontained = EdgeLoopAlgorithms.Contains2D(polygon, innerPolygon[i], tolerance, zTolerance);
                anyVertexInside |= pcontained;
                allVerticesInside &= pcontained;
                anyEdgeIntersecting |= Intersects2D(polygon, innerPolygon[i], innerPolygon[(i + 1) % n], tolerance);

                if (anyVertexInside && anyEdgeIntersecting)
                    return GeometricRelation.Intersecting;
            }

            if (allVerticesInside)
                return GeometricRelation.Contained;
            else if (anyVertexInside)
                return GeometricRelation.Intersecting;
            else
                return GeometricRelation.None;
        }

        /// <summary>
        /// Calculates the hessian form of the plane spanned by a face
        /// </summary>
        /// <param name="f">The face</param>
        /// <returns>The hessian form</returns>
        public static ClipPlane HessianPlane(Face f)
        {
            return new ClipPlane(f.Boundary.Edges.First().StartVertex.Position, f.Normal);
        }
        /// <summary>
        /// Tests whether two hessian forms are similar
        /// </summary>
        /// <param name="plane1">First plane</param>
        /// <param name="plane2">Second plane</param>
        /// <param name="angleTolerance">Tolerance (angular)</param>
        /// <param name="distanceTolerance">Tolerance (distance)</param>
        /// <returns></returns>
        public static bool IsSamePlane((SimVector3D n, double p) plane1, (SimVector3D n, double p) plane2, double angleTolerance, double distanceTolerance)
        {
            var p2 = plane2;

            var angle = SimVector3D.DotProduct(plane1.n, p2.n);
            if (angle < 0)
            {
                p2.n = -p2.n;
                p2.p = -p2.p;
                angle = SimVector3D.DotProduct(plane1.n, p2.n);
            }
            if (angle >= 1.0 - angleTolerance && Math.Abs(plane1.p - p2.p) <= distanceTolerance)
                return true;
            return false;
        }


        private static bool Intersects2D(List<SimPoint3D> polygon, SimPoint3D p0, SimPoint3D p1, double tolerance)
        {
            if (Math.Abs(polygon[0].Z - p0.Z) > tolerance || Math.Abs(polygon[0].Z - p1.Z) > tolerance)
                return false;

            int n = polygon.Count;
            for (int i = 0; i < n; ++i)
            {
                if (EdgeAlgorithms.EdgeEdgeIntersection2D(p0, p1, polygon[i], polygon[(i + 1) % n], tolerance).isIntersecting)
                    return true;
            }

            return false;
        }

        private static (List<SimPoint3D> polygon, SimMatrix3D mapping) MapToXY(Face f)
        {
            try
            {
                var mapping = FaceToXYMapping(f);
                return (
                    f.Boundary.Edges.Select(x => mapping.Transform(x.StartVertex.Position)).ToList(),
                    mapping
                    );
            }
            catch (Exception)
            {
                return (new List<SimPoint3D>(), SimMatrix3D.Identity);
            }
        }

        private static void CleanupPolygon(List<SimPoint3D> polygon, double tolerance)
        {
            if (polygon.Count == 0)
                return;

            var t2 = tolerance * tolerance;
            var areat4half = (t2 * t2) / 2.0;

            SimVector3D lastEdgeDir = polygon.First() - polygon.Last();

            for (int i = 0; i < polygon.Count; ++i)
            {
                var iplus = (i + 1) % polygon.Count;

                if ((polygon[i] - polygon[iplus]).LengthSquared < t2) //Remove 0 length edges
                {
                    polygon.RemoveAt(iplus);
                    i--;
                    continue;
                }

                SimVector3D edgeDir = polygon[iplus] - polygon[i]; //Merge edges going in the same direction
                if (
                    lastEdgeDir.LengthSquared > t2 &&
                    SimVector3D.CrossProduct(edgeDir, lastEdgeDir).LengthSquared < areat4half)
                {
                    polygon.RemoveAt(i);
                    i--;
                    if (i >= 0)
                        edgeDir = polygon[(i + 1) % polygon.Count] - polygon[i % polygon.Count];
                    else
                        edgeDir = polygon.First() - polygon.Last();
                }
                lastEdgeDir = edgeDir;
            }
        }

        /// <summary>
        /// Calculates the area of a face (Area of the boundary minus area of holes)
        /// The method produces wrong results when holes are not fully contained in the face or when holes overlap
        /// </summary>
        /// <param name="f">The face</param>
        /// <returns>The area of the face</returns>
        public static double Area(Face f)
        {
            double area = EdgeLoopAlgorithms.Area(f.Boundary);

            foreach (var h in f.Holes)
                area -= EdgeLoopAlgorithms.Area(h);

            return area;
        }

        /// <summary>
        /// Returns the minimum and maximum area of the offset surfaces as well as the area of the reference surface
        /// </summary>
        /// <param name="f">The face</param>
        /// <returns>The area of the reference surface (area), the minimum area of the offset surfaces (offsetAreaMin) 
        /// and maximum area or the offset surfaces (offsetAreaMax)</returns>
        public static (double area, double offsetAreaMin, double offsetAreaMax) AreaMinMax(Face f)
        {
            (double area, double innerArea, double outerArea) = AreaInnerOuter(f);

            var minArea = Math.Min(innerArea, outerArea);
            var maxArea = Math.Max(innerArea, outerArea);

            return (area, minArea, maxArea);
        }
        /// <summary>
        /// Calculates the area of the face, the area of the inner offset surface and the area of the outer offset surface
        /// </summary>
        /// <param name="f">The face</param>
        /// <returns>area: Area of the reference geometry, innerArea: Area of the inner offset surface, outerArea: Area of the outer offset surface</returns>
        public static (double area, double innerArea, double outerArea) AreaInnerOuter(Face f)
        {
            double area = EdgeLoopAlgorithms.Area(f.Boundary);

            var holeArea = 0.0;
            foreach (var h in f.Holes)
                holeArea += EdgeLoopAlgorithms.Area(h);

            var innerArea = area;
            var outerArea = area;

            if (f.ModelGeometry.OffsetModel.Faces.ContainsKey((f, GeometricOrientation.Backward)))
                innerArea = EdgeLoopAlgorithms.Area(f.ModelGeometry.OffsetModel.Faces[(f, GeometricOrientation.Backward)].Boundary);
            if (f.ModelGeometry.OffsetModel.Faces.ContainsKey((f, GeometricOrientation.Forward)))
                outerArea = EdgeLoopAlgorithms.Area(f.ModelGeometry.OffsetModel.Faces[(f, GeometricOrientation.Forward)].Boundary);

            return (area - holeArea, innerArea - holeArea, outerArea - holeArea);
        }

        /// <summary>
        /// Returns a size estimate for the face. For floor/ceiling faces the direction of the used axis can vary
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static (SimSize size, SimSize minSize, SimSize maxSize) Size(Face f)
        {
            SimMatrix3D mapping = EdgeLoopAlgorithms.SizeMapping(f.Boundary, f.Normal);
            var size = EdgeLoopAlgorithms.Size(f.Boundary.Edges.Select(x => x.StartVertex.Position), mapping);
            var minSize = size;
            var maxSize = size;

            if (f.ModelGeometry.OffsetModel.Faces.ContainsKey((f, GeometricOrientation.Forward)))
            {
                var innerSize = EdgeLoopAlgorithms.Size(f.ModelGeometry.OffsetModel.Faces[(f, GeometricOrientation.Backward)].Boundary, mapping);
                var outerSize = EdgeLoopAlgorithms.Size(f.ModelGeometry.OffsetModel.Faces[(f, GeometricOrientation.Forward)].Boundary, mapping);

                minSize = new SimSize(Math.Min(innerSize.Width, outerSize.Width), Math.Min(innerSize.Height, outerSize.Height));
                maxSize = new SimSize(Math.Max(innerSize.Width, outerSize.Width), Math.Max(innerSize.Height, outerSize.Height));
            }

            return (size, minSize, maxSize);

        }

        /// <summary>
        /// Returns the minimum and maximum Y value of all vertices in a face
        /// </summary>
        /// <param name="f">The Face</param>
        /// <returns>Minimum and maximum Y value of all vertices</returns>
        public static (double min, double max) HeightMinMax(Face f)
        {
            return EdgeLoopAlgorithms.HeightMinMax(f.Boundary);
        }


        internal static double MaxAngleForFloor { get { return 0.178; } }
        internal static double MaxAngleForCeiling { get { return 1.047; } } //60°

        /// <summary>
        /// Returns whether this surface is a floor or ceiling polygon
        /// </summary>
        /// <param name="f">The face</param>
        /// <returns>True when this face belongs to floor or ceiling.</returns>
        public static bool IsFloorOrCeiling(Face f)
        {
            return FaceAlgorithms.IsFloor(f.Normal) || FaceAlgorithms.IsCeiling(f.Normal);
        }
        /// <summary>
        /// Returns whether this surface is a floor polygon (Slope smaller than MaxAngleForFloor, normal pointing upwards)
        /// </summary>
        /// <param name="f">The face</param>
        /// <returns>True when this face belongs to the floor.</returns>
        public static bool IsFloor(PFace f)
        {
            var normal = f.Face.Normal * (int)f.Orientation;
            return IsFloor(normal);
        }
        /// <summary>
        /// Returns whether a surface with this normal is a floor polygon (Slope smaller than MaxAngleForFloor, normal pointing upwards)
        /// </summary>
        /// <param name="normal">The face normal</param>
        /// <returns>True when this face belongs to the floor.</returns>
        public static bool IsFloor(SimVector3D normal)
        {
            return (Math.Acos(SimVector3D.DotProduct(normal, new SimVector3D(0, 1, 0))) < FaceAlgorithms.MaxAngleForFloor);
        }
        /// <summary>
        /// Returns whether this surface is a ceiling polygon (Slope smaller than MaxAngleForCeiling, normal pointing downwards)
        /// </summary>
        /// <param name="f">The face</param>
        /// <returns>True when this face belongs to the ceiling.</returns>
        public static bool IsCeiling(PFace f)
        {
            var normal = f.Face.Normal * (int)f.Orientation;
            return IsCeiling(normal);
        }
        /// <summary>
        /// Returns whether a surface with this normal is a ceiling polygon (Slope smaller than MaxAngleForCeiling, normal pointing downwards)
        /// </summary>
        /// <param name="normal">The face normal</param>
        /// <returns>True when this face belongs to the ceiling.</returns>
        public static bool IsCeiling(SimVector3D normal)
        {
            return (Math.Acos(SimVector3D.DotProduct(normal, new SimVector3D(0, -1, 0))) < FaceAlgorithms.MaxAngleForCeiling);
        }


        /// <summary>
        /// Tests whether the faces form a closed hull (not taking openings into account)
        /// </summary>
        /// <param name="faces">A list of faces to test</param>
        /// <returns>True when the faces form a closed hull</returns>
        public static bool IsClosedHull(IEnumerable<Face> faces)
        {
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));
            if (!faces.Any())
                throw new ArgumentException("Input has to contain at least one face");


            var edgeFaceLookup = new Dictionary<Edge, int>();
            foreach (var face in faces)
            {
                // the combined boundary of all holes except edges that are in multiple holes.
                // We need to make sure to not include edge that are in multiple holes,
                // otherwise the assumption of an edge only has two adjacent faces is wrong
                var allHolesBoundary = face.Holes.SelectMany(h => h.Edges.Select(x => x.Edge))
                    .GroupBy(x => x).Where(x => x.Count() == 1).Select(x => x.Key);

                foreach (var pedge in face.Boundary.Edges.Select(x => x.Edge)
                    .Concat(allHolesBoundary))
                {
                    if (edgeFaceLookup.TryGetValue(pedge, out var count))
                    {
                        edgeFaceLookup[pedge] = count + 1;
                    }
                    else
                    {
                        edgeFaceLookup.Add(pedge, 1);
                    }
                }
            }

            // All edges must have two adjacent faces, otherwise it is not a closed volume
            if (edgeFaceLookup.Values.Any(x => x != 2))
                return false;

            return true;
        }

        /// <summary>
        /// Returns if the face is the face of a hole inside another face.
        /// </summary>
        /// <param name="face">The face</param>
        /// <returns>True if this face is the face of a hole.</returns>
        public static bool IsFaceOfHole(Face face)
        {
            return face.Boundary.Faces.Count > 1 &&
                face.Boundary.Faces.Exists(x => x != face && x.Holes.Contains(face.Boundary));
        }

        /// <summary>
        /// Tries to find a volume which contains all faces
        /// </summary>
        /// <param name="faces">The list of faces</param>
        /// <param name="model">The model to search in</param>
        /// <returns>A volume that contains all faces, or null when no such volume exists</returns>
        public static Volume FindCommonVolume(IEnumerable<Face> faces, GeometryModelData model)
        {
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));
            if (faces.Count() < 0)
                throw new ArgumentException("Input has to contain at least one face");

            return model.Volumes.FirstOrDefault(v => faces.All(f => v.Faces.Any(pf => pf.Face == f)));
        }


        /// <summary>
        /// Determines whether a set of points lies completely on one side of the plane
        /// </summary>
        /// <param name="face">The face that defines the plane</param>
        /// <param name="points">The list of points to test</param>
        /// <param name="tolerance">Points in this distance to the plane are excluded from test</param>
        /// <returns>Forward or Backward when the points are completely on one side, Undefined when the are located on both sides</returns>
        public static GeometricOrientation Halfspace(Face face, IEnumerable<SimPoint3D> points, double tolerance = 0.0001)
        {
            var hessian = HessianPlane(face);

            var pd = points.Select(x => SimVector3D.DotProduct(hessian.Normal, (SimVector3D)x) + hessian.Distance);

            if (pd.All(x => x >= -tolerance))
                return GeometricOrientation.Forward;
            else if (pd.All(x => x <= tolerance))
                return GeometricOrientation.Backward;
            return GeometricOrientation.Undefined;
        }

        private static (List<SimPoint3D> boundaries, List<List<SimPoint3D>> holes, List<List<SimPoint3D>> additionalFaces) BoundaryAndHoles(Face face, SimMatrix3D matrix)
        {
            var mergedHoles = new List<HashSet<Edge>>();
            var additionalFaces = new List<HashSet<Edge>>();
            var boundary = EdgeAlgorithms.OrderedPointLoop(face.Boundary.Edges.Select(x => x.Edge), matrix);

            // Algorithm:
            // 1. Find all connected hole clusters
            // 2. In each cluster, check if the edges that are only in a single EdgeLoop (edges that are not adjacent) form a closed loop
            //     2.1 If yes, add that boundary as the hole (If holes share an edge this will be the outside boundary)
            //     2.2 Else, check for connected clusters again in those boundary edges 
            //         (e.g. When multiple adjacent holes form a "donut" with an inner and outer loop)
            //         2.2.1 In those cluster only use the ones that form a loop (ignore the rest for now)
            //         2.2.2 Check if one of those loops contains all the others spatially
            //             2.2.2.1 If yes, add that outer loop as hole and the remaining ones as additional faces
            //             2.2.2.2 Else, Try to find a boundary with the 2D outside boundary check and add it as a whole
            //                 Additionally check remaining edges if they also form a loop and add them as additional faces


            var edgeHoleLookup = face.Holes.SelectMany(hole => hole.Edges.Select(edge => (edge.Edge, hole)))
                .GroupBy(x => x.Edge)
                .ToDictionary(k => k.Key, k => k.Select(x => x.hole).ToHashSet());

            var connected = DetectionAlgorithms.FindConnectedEdgeLoopGroups(face.Holes);
            foreach (var cluster in connected)
            {
                // boundary are all edges that are only in a single loop
                var clusterBoundary = cluster.SelectMany(c => c.Edges.Where(e => edgeHoleLookup[e.Edge].Count == 1))
                    .Select(e => e.Edge);
                // boundary may be empty if it was already in the hole list
                if (clusterBoundary.Any())
                {
                    // check if this is already a loop, else if not, it may contain multiple loops
                    if (EdgeAlgorithms.OrderLoop(clusterBoundary).isLoop)
                    {
                        mergedHoles.Add(clusterBoundary.ToHashSet());
                    }
                    else
                    {
                        var mapping = FaceToXYMapping(face);
                        // find connected groups in cluster, could be multiple loops
                        var connectedGroups = DetectionAlgorithms.FindConnectedEdgeGroups(clusterBoundary);
                        // find all edge loops, ignore non edge loops
                        var loops = connectedGroups.Select(x => EdgeAlgorithms.OrderLoop(x)).ToList();

                        List<Edge> foundLoop = null;
                        // find loop that contains all other edges
                        for (int i = 0; i < loops.Count && foundLoop == null; i++)
                        {
                            var (isloop, loop) = loops[i];
                            if (isloop)
                            {
                                // check if all other edges are contained in that loop
                                var containsAll = true;
                                for (int j = 0; j < connectedGroups.Length && containsAll; j++)
                                {
                                    if (i != j) // using normal for loops so we can easily exclude the same loop from check
                                        containsAll &= EdgeLoopAlgorithms.Contains(loop, connectedGroups[j], mapping, face.ModelGeometry.Model.CleanupTolerance, face.ModelGeometry.Model.CleanupTolerance) == GeometricRelation.Contained;
                                }
                                if (containsAll)
                                    foundLoop = loop;
                            }
                        }
                        // add that as new hole boundary and remove others
                        if (foundLoop != null)
                        {
                            mergedHoles.Add(foundLoop.ToHashSet());
                            // add rest of loops as additional faces
                            additionalFaces.AddRange(loops.Where(x => x.isLoop && x.loop != foundLoop).Select(x => x.loop.ToHashSet()));
                        }
                        // we couldn't find a loop this way, try to find the outside boundary using ray intersections and angles
                        else
                        {
                            foreach (var group in connectedGroups)
                            {
                                // try to find outside boundary
                                var newBoundary = FindOutsideBoundary2D(mapping, group);
                                if (newBoundary != null)
                                {
                                    mergedHoles.Add(newBoundary);
                                    // check if the remaining geometry is also a boundary and add it back as face
                                    var remainder = group.Except(newBoundary);
                                    if (remainder.Any())
                                    {
                                        var additionalBoundary = FindOutsideBoundary2D(mapping, remainder);
                                        if (additionalBoundary != null)
                                            additionalFaces.Add(additionalBoundary);
                                    }
                                }
                            }
                        }
                    }
                }
                // if no boundary was found we need to find the loop in the cluster that contains all the others
                else
                {
                    var vertices = cluster.SelectMany(c => c.Edges.Select(e => e.StartVertex)).ToHashSet();
                    EdgeLoop foundBoundary = null;
                    foreach (var loop in cluster)
                    {
                        if (vertices.All(x => EdgeLoopAlgorithms.Contains(loop, x.Position, face.ModelGeometry.Model.CleanupTolerance, face.ModelGeometry.Model.CleanupTolerance) == GeometricRelation.Contained))
                        {
                            foundBoundary = loop;
                            break;
                        }
                    }
                    // if we found the boundary, remove all others
                    if (foundBoundary != null)
                    {
                        mergedHoles.Add(foundBoundary.Edges.Select(x => x.Edge).ToHashSet());
                    }
                }
            }

            var holes = mergedHoles.Select(x => EdgeAlgorithms.OrderedPointLoop(x, matrix)).ToList();
            var additionalBoundaries = additionalFaces.Select(x => EdgeAlgorithms.OrderedPointLoop(x, matrix)).Where(x => x != null).ToList();

            if (boundary == null || holes.Any(x => x == null))
            {
                //Fallback for faces that result in multiple disconnected parts
                var poly = face.Boundary.Edges.Select(x => matrix.Transform(x.StartVertex.Position)).ToList();
                List<List<SimPoint3D>> fbholes = face.Holes.Select(h => h.Edges.Select(
                    e => matrix.Transform(e.StartVertex.Position)
                    ).ToList()).ToList();

                foreach (var h in fbholes)
                {
                    SimVector3D center = new SimVector3D(0, 0, 0);
                    h.ForEach(x => center += ((SimVector3D)x / (double)h.Count));

                    for (int i = 0; i < h.Count; ++i)
                    {
                        var scaled = ((SimVector3D)h[i] - center) * 0.999 + center;
                        h[i] = new SimPoint3D(scaled.X, h[i].Y, scaled.Z);
                    }
                }

                //Since we know that the windows touch the boundary: scale them such that they don't touch

                return (poly, fbholes, additionalBoundaries);
            }

            return (boundary, holes, additionalBoundaries);
        }

        internal static HashSet<Edge> FindOutsideBoundary2D(SimMatrix3D mapping, IEnumerable<Edge> group)
        {
            // project into 2D plane
            // find start edge (search max X direction, pick "good" edge)
            // find inside direction, ray intersection in +- edge normals excluding start edge, direction with odd # intersections is inside
            // for all next edges calculate signed angle, depending on inside dir use edge with min or max angle
            var vertexPosLookup = group.SelectMany(x => x.Vertices).Distinct().ToDictionary(x => x, x => mapping.Transform(x.Position));
            var (startEdge, dir) = group.Select(e => (e, dir: FindEdgeInsideDirection2D(e, group, vertexPosLookup)))
                .FirstOrDefault(x => x.dir != 0);
            if (startEdge == null) // if we could not find a boundary edge, we cannot find a boundary
                return null;

            var currentEdge = startEdge;
            var pool = group.ToHashSet();
            pool.Remove(startEdge);
            var startVertex = startEdge.Vertices[0];
            var currentVertex = startEdge.Vertices[1];
            var newBoundary = new HashSet<Edge>() { startEdge };
            while (pool.Any() && currentVertex != startVertex)
            {

                // find next edges
                var nextEdges = pool.Where(x => x.Vertices.Contains(currentVertex)).ToList();
                if (nextEdges.Count == 0)
                {
                    return null; // we found dangling edge, abort
                }
                else if (nextEdges.Count == 1) // continue if only one next edge
                {
                    currentVertex = nextEdges[0].Vertices.First(x => x != currentVertex);
                    currentEdge = nextEdges[0];
                    newBoundary.Add(currentEdge);
                    pool.Remove(currentEdge);
                }
                else
                {
                    var curDir = vertexPosLookup[currentVertex] - vertexPosLookup[currentEdge.Vertices.First(x => x != currentVertex)];
                    curDir.Normalize();

                    var curpos = vertexPosLookup[currentVertex];
                    var dirs = nextEdges.Select(x => vertexPosLookup[x.Vertices.First(v => v != currentVertex)] - curpos).ToList();
                    dirs.ForEach(x => x.Normalize());

                    var angles = dirs.Select(x => DetectionAlgorithms.SignedAngle(curDir.XY(), x.XY()));
                    // find the index of the edge with min/max angle depending on the start edge inside direction
                    var (_, _, maxIndex) = dir > 0 ? angles.ArgMax(x => x) : angles.ArgMin(x => x);
                    currentEdge = nextEdges[maxIndex];
                    currentVertex = currentEdge.Vertices.First(x => x != currentVertex);
                    newBoundary.Add(currentEdge);
                    pool.Remove(currentEdge);
                }
            }
            // if we got to the start we found a boundary
            if (startVertex == currentVertex)
            {
                return newBoundary;
            }

            return null;
        }

        /// <summary>
        /// Performs a polygon inside/outside test to check which side of a 2d edge points inside the polygon and if the edge is on the outside boundary of the polygon.
        /// </summary>
        /// <param name="checkEdge">Edge to check the direction for</param>
        /// <param name="polygon">All edges of the polygon</param>
        /// <param name="vertexPosLookup">Lookup of the vertex positions that were transformed to the XY plane. Edge vertices still have their original position.</param>
        /// <returns>1 if the right side of the 2D edge is determined to be inside the polygon, -1 if the left side is inside and 0 if it is not a boundary edge.</returns>
        internal static int FindEdgeInsideDirection2D(Edge checkEdge, IEnumerable<Edge> polygon, Dictionary<Vertex, SimPoint3D> vertexPosLookup)
        {

            // intersection test:
            // ray origin is center of edge
            // subtract ray origin from all points
            // check if y of edge dir is bigger than x then compare in x dir?
            // don't use normal vectors, check in x or y direction for intersections
            // then if you check in x dir, you only need to check if the vertices of the edges are on opposite sides of the x axis
            // if one side has zero intersection we found a boundary edge, the opposite side is the inside side

            var v1Pos = vertexPosLookup[checkEdge.Vertices[0]];
            var v2Pos = vertexPosLookup[checkEdge.Vertices[1]];
            var edgeDir = v2Pos - v1Pos;
            // start at halfway point on edge
            var rayStart = v1Pos + edgeDir * 0.5;
            // we search for intersections in global X direction if abs(Y) > abs(X) otherwise we search in global Y dir
            var searchX = Math.Abs(edgeDir.Y) > Math.Abs(edgeDir.X);
            // decided if we search in positive or negative direction (positive is to the right of the direction vector)
            var searchDir = searchX ? (edgeDir.Y > 0 ? 1 : -1) : (edgeDir.X > 0 ? -1 : 1);
            int posIntersections = 0;
            int negIntersections = 0;
            foreach (var edge in polygon)
            {
                if (edge == checkEdge) // ignore start edge
                    continue;
                // subtract start point from vertex positions, then our ray is just the X or Y axis
                var p1 = vertexPosLookup[edge.Vertices[0]] - rayStart;
                var p2 = vertexPosLookup[edge.Vertices[1]] - rayStart;
                if (searchX)
                {
                    // intersection found if the edge crosses the Y axis
                    if (p1.Y > 0 && p2.Y <= 0 ||
                        p1.Y <= 0 && p2.Y > 0)
                    {
                        // y coord fraction is proportional to the intersection distance
                        var frac = Math.Abs(p1.Y / (p2.Y - p1.Y));
                        var intersection = p1 + frac * (p2 - p1);
                        // check on which side the intersection happened
                        if (intersection.X > 0)
                        {
                            if (searchDir > 0)
                                posIntersections++;
                            else
                                negIntersections++;
                        }
                        else
                        {
                            if (searchDir < 0)
                                posIntersections++;
                            else
                                negIntersections++;
                        }
                    }
                }
                else
                {
                    // intersection found if the edge crosses the X axis
                    if (p1.X > 0 && p2.X <= 0 ||
                        p1.X <= 0 && p2.X > 0)
                    {
                        var frac = Math.Abs(p1.X / (p2.X - p1.X));
                        var intersection = p1 + frac * (p2 - p1);
                        if (intersection.Y > 0)
                        {
                            if (searchDir > 0)
                                posIntersections++;
                            else
                                negIntersections++;
                        }
                        else
                        {
                            if (searchDir < 0)
                                posIntersections++;
                            else
                                negIntersections++;
                        }
                    }
                }
            }

            // one side needs to have zero intersections otherwise it is not a boundary edge
            if (posIntersections > 0 && negIntersections == 0)
            {
                return 1; // 1 means the inside of the polygon is on the right side of the edge in edge direction
            }
            else if (negIntersections > 0 && posIntersections == 0)
            {
                return -1;
            }
            return 0; // this happens when the polygon is not cleaned properly or a wrong start edge was chosen
        }

        /// <summary>
        /// Calculates the perimeter of the face. Sums the parimeter of the bounding and all holes.
        /// DOES NOT HANDLE DUPLICATES EDGES (E.G. WHEN TWO HOLES INTERSECT OR WHEN A HOLE INTERSECTS THE BOUNDARY)
        /// </summary>
        /// <param name="f">The face</param>
        /// <returns>The perimeter lenth</returns>
        public static (double inner, double outer) Perimeter(Face f)
        {
            if (f.ModelGeometry.OffsetModel.Faces.ContainsKey((f, GeometricOrientation.Forward)))
            {
                double inner = EdgeLoopAlgorithms.Perimeter(f.ModelGeometry.OffsetModel.Faces[(f, GeometricOrientation.Backward)].Boundary);
                double outer = EdgeLoopAlgorithms.Perimeter(f.ModelGeometry.OffsetModel.Faces[(f, GeometricOrientation.Forward)].Boundary);

                foreach (var hole in f.Holes)
                {
                    var hperimeter = EdgeLoopAlgorithms.Perimeter(hole);
                    inner += hperimeter;
                    outer += hperimeter;
                }

                return (inner, outer);
            }

            return (double.NaN, double.NaN);
        }

        /// <summary>
        /// Splits a face such that all openings can be handles as adjacent faces.
        /// When no openings are present, nothing happens
        /// </summary>
        /// <param name="f">The face to split</param>
        public static void SplitFace(Face f)
        {
            if (f.Holes.Count > 0)
            {
                //Decompose the outer polygon
                var decomposed = Triangulation.DecomposeInSimplePolygons(f.Boundary.Edges.Select(x => x.StartVertex.Position).ToList(),
                    f.Holes.Select(h => h.Edges.Select(he => he.StartVertex.Position).ToList()).ToList());

                List<Vertex> originalVertices = new List<Vertex>();
                List<Edge> originalEdges = new List<Edge>();

                foreach (var edge in f.Boundary.Edges)
                {
                    originalVertices.Add(edge.StartVertex);
                    originalEdges.Add(edge.Edge);
                }
                foreach (var hole in f.Holes)
                {
                    foreach (var edge in hole.Edges)
                    {
                        originalVertices.Add(edge.StartVertex);
                        originalEdges.Add(edge.Edge);
                    }
                }

                //Split surrounding face
                List<Face> newFaces = new List<Face>();
                for (int fi = 0; fi < decomposed.Count; ++fi)
                {
                    var facePoints = decomposed[fi];

                    List<Edge> faceEdges = new List<Edge>();

                    for (int pi = 0; pi < facePoints.Count; ++pi)
                    {
                        var p0 = facePoints[pi];
                        var p1 = facePoints[(pi + 1) % facePoints.Count];

                        var v0 = SplitFace_GetOrCreateVertex(p0, originalVertices, f.Layer);
                        var v1 = SplitFace_GetOrCreateVertex(p1, originalVertices, f.Layer);

                        faceEdges.Add(SplitFace_GetOrCreateEdge(v0, v1, originalEdges, f.Layer));
                    }

                    EdgeLoop newBoundary = new EdgeLoop(f.Layer, "{0}", faceEdges);
                    Face newFace = new Face(f.Layer, string.Format("{0} ({1})", f.Name, fi), newBoundary, GeometricOrientation.Forward, null);
                    newFaces.Add(newFace);
                }

                //Check if hole faces have to be added to volumes
                List<Face> holeFaces = f.Holes.Select(h => h.Faces.FirstOrDefault(hf => hf.Boundary == h)).Where(x => x != null).Distinct().ToList();

                //Replace face in volume
                foreach (var pface in f.PFaces.ToList())
                {
                    pface.Volume.Faces.Remove(pface);
                    pface.Volume.Faces.AddRange(newFaces.Select(x => new PFace(x, pface.Volume, GeometricOrientation.Undefined)));
                    pface.Volume.Faces.AddRange(holeFaces.Select(x => new PFace(x, pface.Volume, GeometricOrientation.Undefined)));
                }

                //Remove original
                f.Boundary.RemoveFromModel();
                f.RemoveFromModel();
            }
        }
        private static Vertex SplitFace_GetOrCreateVertex(SimPoint3D pos, List<Vertex> existingVertices, Layer createLayer)
        {
            var v = existingVertices.FirstOrDefault(x => (x.Position - pos).LengthSquared < 0.00001);
            if (v == null)
            {
                v = new Vertex(createLayer, "Vertex {0}", pos);
                existingVertices.Add(v);
            }

            return v;
        }
        private static Edge SplitFace_GetOrCreateEdge(Vertex v0, Vertex v1, List<Edge> existingEdges, Layer createLayer)
        {
            var e = existingEdges.FirstOrDefault(x => x.Vertices.Contains(v0) && x.Vertices.Contains(v1));
            if (e == null)
            {
                e = new Edge(createLayer, "Edge {0}", new List<Vertex> { v0, v1 });
                existingEdges.Add(e);
            }

            return e;
        }


        /// <summary>
        /// Returns the incline (angle to up vector) and the orientation (angle to north=[0,0,1]) of a face in radians
        /// </summary>
        /// <param name="pface">The face</param>
        /// <param name="tolerance">Tolerance for calculations</param>
        /// <returns>
        ///     Incline: Between -PI/2 and PI/2. 0 for vertical walls, -PI/2 for horizontal floors, PI/2 for horizontal ceilings 
        ///     Orientation: Angle in the xz-plane of the face normal to the north direction [0,0,1]. 0 when the face is horizontal (|incline| == PI/2).
        /// </returns>
        public static (double incline, double orientation) OrientationIncline(PFace pface, double tolerance = 0.0001)
        {
            if (pface == null)
                throw new ArgumentNullException(nameof(pface));

            var inwardNormal = pface.Face.Normal * (int)pface.Orientation;
            return OrientationIncline(-inwardNormal, tolerance);
        }

        /// <summary>
        /// Returns the incline (angle to XZ-plane) and the orientation (angle to north=[0,0,1]) of a face normal in radians
        /// </summary>
        /// <param name="normal">The face normal</param>
        /// <param name="tolerance">Tolerance for calculations</param>
        /// <returns>
        ///     Incline: Between -PI/2 and PI/2. 0 for vertical walls, -PI/2 for horizontal floors, PI/2 for horizontal ceilings 
        ///     Orientation: Angle in the xz-plane of the face normal to the north direction [0,0,1]. 0 when the face is horizontal (|incline| == 90).
        /// </returns>
        public static (double incline, double orientation) OrientationIncline(SimVector3D normal, double tolerance = 0.0001)
        {
            var inclineDot = -normal.Y; //Same as dot(normal, vec3(0,1,0))
            var incline = Math.Acos(inclineDot) - Math.PI / 2.0;

            var orientation = 0.0;
            if (Math.Abs(incline) < Math.PI / 2.0 - tolerance)
            {
                normal.Y = 0.0;
                normal.Normalize();

                var north = new SimVector3D(0, 0, 1);
                orientation = -Math.Atan2(SimVector3D.CrossProduct(normal, north).Y, //dot( cross (n, north), up)
                                         normal.Z); //dot (n, north)

                if (orientation < -tolerance)
                    orientation += 2.0 * Math.PI;
            }

            return (incline, orientation);
        }

        /// <summary>
        /// Extrudes a face along its normal by a given height
        /// </summary>
        /// <param name="faces">The faces to extrude. Each face is treated separately, but shared edges are extruded only once</param>
        /// <param name="referenceFace">A reference face, only needed to return the extruded reference face</param>
        /// <param name="height">The height of the extrusion</param>
        /// <returns>
        /// A tuple containing:
        ///  - createdGeometry: A list of all geometries created by this operation
        ///  - extrudedFaces: A list of the top faces of each extrusion. Order is the same as in the input face list
        ///  - extrudedReferenceFace: The top face of the extrusion of the referenceFace
        /// </returns>
        public static (List<BaseGeometry> createdGeometry, List<Face> extrudedFaces, Face extrudedReferenceFace) Extrude(
            IEnumerable<Face> faces, Face referenceFace, double height)
        {
            SimVector3D normal = EdgeLoopAlgorithms.NormalCCW(faces.First().Boundary);
            return Extrude(faces, referenceFace, height, normal);
        }

        /// <summary>
        /// Extrudes a face along its normal by a given height
        /// </summary>
        /// <param name="faces">The faces to extrude. Each face is treated separately, but shared edges are extruded only once</param>
        /// <param name="referenceFace">A reference face, only needed to return the extruded reference face</param>
        /// <param name="height">The height of the extrusion</param>
        /// <param name="normal">Normal direction to extrude to</param>
        /// <returns>
        /// A tuple containing:
        ///  - createdGeometry: A list of all geometries created by this operation
        ///  - extrudedFaces: A list of the top faces of each extrusion. Order is the same as in the input face list
        ///  - extrudedReferenceFace: The top face of the extrusion of the referenceFace
        /// </returns>
        public static (List<BaseGeometry> createdGeometry, List<Face> extrudedFaces, Face extrudedReferenceFace) Extrude(
            IEnumerable<Face> faces, Face referenceFace, double height, SimVector3D normal)
        {
            List<Face> selectFaces = new List<Face>();
            List<BaseGeometry> result = new List<BaseGeometry>();
            Dictionary<Vertex, Vertex> vertexLookup = new Dictionary<Vertex, Vertex>();
            Dictionary<Vertex, Edge> edgeLookup = new Dictionary<Vertex, Edge>();
            Dictionary<Edge, (Face face, Edge topEdge)> faceLookup = new Dictionary<Edge, (Face face, Edge topEdge)>();

            Face extrudedReferenceFace = null;

            var model = faces.First().ModelGeometry;
            model.StartBatchOperation();

            foreach (var face in faces)
            {
                List<Face> volumeFaces = new List<Face>() { face };
                List<Edge> topEdges = new List<Edge>();

                foreach (var e in face.Boundary.Edges)
                {
                    if (!faceLookup.ContainsKey(e.Edge))
                    {
                        List<Edge> faceLoop = new List<Edge>();

                        foreach (var v in e.Edge.Vertices)
                        {
                            if (!vertexLookup.ContainsKey(v))
                            {
                                var newV = new Vertex(v.Layer, "", v.Position + normal * height) { Color = new DerivedColor(v.Color) };
                                vertexLookup.Add(v, newV);
                                result.Add(newV);
                            }
                            if (!edgeLookup.ContainsKey(v))
                            {
                                Edge vedge = new Edge(e.Edge.Layer, "",
                                    new List<Vertex> { v, vertexLookup[v] })
                                { Color = new DerivedColor(e.Edge.Color) };
                                edgeLookup.Add(v, vedge);
                                result.Add(vedge);
                            }

                            faceLoop.Add(edgeLookup[v]);
                        }

                        Edge edge = new Edge(e.Edge.Layer, "",
                            new List<Vertex> { vertexLookup[e.Edge.Vertices[0]], vertexLookup[e.Edge.Vertices[1]] })
                        {
                            Color = new DerivedColor(e.Edge.Color)
                        };
                        result.Add(edge);
                        topEdges.Add(edge);

                        faceLoop.Insert(1, edge);
                        faceLoop.Insert(0, e.Edge);

                        EdgeLoop loop = new EdgeLoop(e.Edge.Layer, "", faceLoop) { Color = new DerivedColor(e.Edge.Color) };
                        result.Add(loop);

                        Face newFace = new Face(e.Edge.Layer, "", loop) { Color = new DerivedColor(e.Edge.Color) };
                        result.Add(newFace);
                        volumeFaces.Add(newFace);
                        faceLookup.Add(e.Edge, (newFace, edge));
                    }
                    else
                    {
                        (var newFace, var topEdge) = faceLookup[e.Edge];
                        volumeFaces.Add(newFace);
                        topEdges.Add(topEdge);
                    }
                }

                EdgeLoop topLoop = new EdgeLoop(face.Layer, "", topEdges) { Color = new DerivedColor(face.Color) };
                result.Add(topLoop);

                Face topFace = new Face(face.Layer, "", topLoop) { Color = new DerivedColor(face.Color) };
                result.Add(topFace);
                volumeFaces.Add(topFace);
                selectFaces.Add(topFace);

                Volume volume = new Volume(face.Layer, "", volumeFaces) { Color = new DerivedColor(face.Color) };
                result.Add(volume);

                if (face == referenceFace)
                    extrudedReferenceFace = topFace;
            }

            model.EndBatchOperation();

            return (result, selectFaces, extrudedReferenceFace);

        }

        /// <summary>
        /// Checks whether a face is intersected by a line segment.
        /// Order of start and end of the line doesn't matter for this algorithm.
        /// </summary>
        /// <param name="face">The face</param>
        /// <param name="lineStart">The starting point of the line</param>
        /// <param name="lineEnd">The end point of the line</param>
        /// <param name="tolerance">The numerical tolerance</param>
        /// <returns>True when the line segment intersects the face, otherwise False</returns>
        public static bool IntersectsLine(Face face, SimPoint3D lineStart, SimPoint3D lineEnd, double tolerance = 0.0001)
        {
            var hnf = HessianPlane(face);
            (var p, var t) = hnf.IntersectLine(lineStart, lineEnd);

            if (t >= -tolerance && t <= 1.0 + tolerance)
            {
                return Contains(face, p, tolerance, tolerance) == GeometricRelation.Contained;
            }
            else
                return false;
        }

        /// <summary>
        /// Checks whether a face is intersected by a ray.
        /// Only intersections in positive direction from the ray starting point are considered intersections.
        /// </summary>
        /// <param name="face">The face to test</param>
        /// <param name="rayStart">The starting point of the ray</param>
        /// <param name="rayDirection">The direction of the ray</param>
        /// <param name="tolerance">The numerical tolerance</param>
        /// <returns>True when the ray intersects the face, otherwise False</returns>
        public static bool IntersectsRay(Face face, SimPoint3D rayStart, SimVector3D rayDirection, double tolerance = 0.0001)
        {
            var hnf = HessianPlane(face);
            (var p, var t) = hnf.IntersectLine(rayStart, rayStart + rayDirection);

            if (t >= -tolerance)
            {
                return Contains(face, p, tolerance, tolerance) == GeometricRelation.Contained;
            }
            else
                return false;
        }

        /// <summary>
        /// Tries to find a direction orthogonal to the <paramref name="inputEdge"/> that points into the plane of the given <paramref name="face"/>.
        /// Returns the zero vector if it could not be found (Happens if face does not have an area)
        /// </summary>
        /// <param name="inputEdge">The input edge</param>
        /// <param name="face">The face</param>
        /// <param name="epsilon">The epsilon for numerical stability</param>
        /// <returns>The direction orthogonal to the <paramref name="inputEdge"/> pointing into <paramref name="face"/></returns>
        public static SimVector3D FindOrthogonalDirectionInFace(Edge inputEdge, Face face, double epsilon)
        {
            var startVertex = inputEdge.Vertices[0];
            var edgeDir = EdgeAlgorithms.Direction(inputEdge);

            // get vertices of boundary loop except the ones from the input edge
            var vertices = face.Boundary.Edges.Where(x => !inputEdge.Vertices.Contains(x.StartVertex)).Select(x => x.StartVertex);
            // try to find another edge with a vertex that forms a non collinear direction to the edgeDir
            foreach (var vertex in vertices)
            {
                var dir = vertex.Position - startVertex.Position;
                dir.Normalize();
                // fails if colinear
                if (GeometryAlgorithms.TryOrthogonalize(edgeDir, dir, epsilon, out var orth))
                {
                    return orth;
                }
            }

            return new SimVector3D();
        }

        /// <summary>
        /// Gets the angle between two orthogonal face directions around an edge direction.
        /// </summary>
        /// <param name="currentFaceDirection">is the direction orthogonal to the <paramref name="edgeDirection"/> of the current face.</param>
        /// <param name="edgeDirection">is the edges direction</param>
        /// <param name="otherFaceDirection">is the direction orthogonal to the <paramref name="edgeDirection"/> of the other edge to find the angle of</param>
        /// <returns>The angle of <paramref name="otherFaceDirection"/> to <paramref name="currentFaceDirection"/> around <paramref name="edgeDirection"/> from 0 to 2 pi</returns>
        public static double AngleBetweenOrthogonalFaceDirections(SimVector3D currentFaceDirection, SimVector3D edgeDirection, SimVector3D otherFaceDirection)
        {
            var angle = Math.Atan2(
                SimVector3D.DotProduct(SimVector3D.CrossProduct(currentFaceDirection, otherFaceDirection), edgeDirection),
                SimVector3D.DotProduct(currentFaceDirection, otherFaceDirection));
            if (angle < 0)
                angle = 2.0 * Math.PI + angle; //+ because angle is negative
            return angle;
        }

    }
}
