using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides algorithms that work on EdgeLoop instances
    /// </summary>
    public static class EdgeLoopAlgorithms
    {
        /// <summary>
        /// Calculates the normal of an EdgeLoop.
        /// <para>The method assumes that the EdgeLoop describes a planar surface and is oriented counter clockwise</para>
        /// When no normal could be found (or NaN/Inf is generated during calculation), this methods returns (0,0,0).
        /// </summary>
        /// <param name="loop">The EdgeLoop to operate on</param>
        /// <returns>The normal vector</returns>
        public static SimVector3D NormalCCW(EdgeLoop loop)
        {
            return NormalCCW(loop.Edges.Select(x => x.StartVertex.Position).ToList());
        }

        /// <summary>
        /// Calculates the normal of an EdgeLoop given as a set of points.
        /// <para>The method assumes that the EdgeLoop describes a planar surface and is oriented counter clockwise</para>
        /// When no normal could be found (or NaN/Inf is generated during calculation), this methods returns (0,0,0).
        /// </summary>
        /// <param name="edgeLoopPositions">The set of positions in the EdgeLoop to operate on</param>
        /// <returns>The normal vector</returns>
        public static SimVector3D NormalCCW(List<SimPoint3D> edgeLoopPositions)
        {
            SimVector3D sumNormal = new SimVector3D(0, 0, 0);

            for (int i = 0; i < edgeLoopPositions.Count; i++)
            {
                SimPoint3D p = edgeLoopPositions[i];
                SimPoint3D np = edgeLoopPositions[(i + 1) % edgeLoopPositions.Count];
                sumNormal += SimVector3D.CrossProduct((SimVector3D)np, (SimVector3D)p);
            }

            sumNormal.Normalize();

            if (double.IsNaN(sumNormal.X) || double.IsNaN(sumNormal.Y) || double.IsNaN(sumNormal.Z) ||
                double.IsInfinity(sumNormal.X) || double.IsInfinity(sumNormal.Y) || double.IsInfinity(sumNormal.Z))
                return new SimVector3D(0, 0, 0);

            return sumNormal;
        }

        /// <summary>
        /// Returns the number of straight lines in a loop. Basically combines consecutive edges with the same direction.
        /// </summary>
        /// <param name="loop">The loop</param>
        /// <param name="tolerance">Calculation tolerance</param>
        /// <returns>The number of straight lines</returns>
        public static int StraightEdgeCount(EdgeLoop loop, double tolerance)
        {
            //Basic idea: Start with edge count. Reduce by 1 for every angle close to 90°
            int count = loop.Edges.Count;
            var angles = loop.Edges.Select(x => SimVector3D.DotProduct(EdgeAlgorithms.Direction(x), EdgeAlgorithms.Direction(x.Next)));
            count = count - angles.Count(x => x > 1 - tolerance);

            return count;
        }

        /// <summary>
        /// Determines if two edgeloops are similar. Similar means that they contain the same Edges
        /// </summary>
        /// <param name="loop1">First edge</param>
        /// <param name="loop2">Second edge</param>
        /// <returns>True when the loops are similar, False otherwise</returns>
        public static bool IsSimilar(EdgeLoop loop1, EdgeLoop loop2)
        {
            if (loop1.Edges.Count != loop2.Edges.Count)
                return false;

            if (loop1.Edges.All(e1 => loop2.Edges.FirstOrDefault(e2 => e2.Edge == e1.Edge) != null)
                && loop1.Edges.Count == loop2.Edges.Count)
                return true;
            return false;
        }

        /// <summary>
        /// Calculates the area of a EdgeLoop
        /// </summary>
        /// <param name="loop">The loop</param>
        /// <returns>The area (always positive)</returns>
        public static double Area(EdgeLoop loop)
        {
            return Area(loop.Edges.Select(x => x.StartVertex.Position));
        }
        /// <summary>
        /// Calculates the area of a EdgeLoop
        /// </summary>
        /// <param name="boundary">A list of boundary points</param>
        /// <returns>The area (always positive)</returns>
        public static double Area(IEnumerable<SimPoint3D> boundary)
        {
            //Strategy: Choose arbitrary point (X), for each edge (AB) calucate signed area of triangle XAB
            double sumArea = 0;


            SimVector3D normal = new SimVector3D(0, 0, 0);

            //Minimize error for normal by taking the largest one
            var np0 = boundary.First();
            var np1 = boundary.ElementAt(1);
            foreach (var np2 in boundary.Skip(2))
            {
                var triNorm = SimVector3D.CrossProduct(np1 - np0, np2 - np0);
                if (triNorm.LengthSquared > normal.LengthSquared)
                    normal = triNorm;
            }
            normal.Normalize();
            if (double.IsNaN(normal.X) || double.IsNaN(normal.Y) || double.IsNaN(normal.Z))
                return double.NaN;

            //Calc area
            var referencePoint = boundary.First();
            var lastPoint = boundary.ElementAt(1);
            foreach (var currentPoint in boundary.Skip(2))
            {
                var triNorm = SimVector3D.CrossProduct(lastPoint - referencePoint, currentPoint - referencePoint);
                double triArea = triNorm.Length;
                triNorm.Normalize();

                if (triArea > 0.0001)
                {
                    sumArea += triArea * Math.Sign(SimVector3D.DotProduct(triNorm, normal));
                }

                lastPoint = currentPoint;
            }

            return Math.Abs(sumArea / 2.0);
        }

        /// <summary>
        /// Returns the estimated size of the loop. For floor/ceiling loops, the direction of the axis can vary.
        /// </summary>
        /// <param name="loop"></param>
        /// <returns></returns>
        public static SimSize Size(EdgeLoop loop)
        {
            SimVector3D normal = NormalCCW(loop);
            var mapping = SizeMapping(loop, normal);
            return Size(loop.Edges.Select(x => x.StartVertex.Position), mapping);
        }

        internal static SimMatrix3D SizeMapping(EdgeLoop loop, SimVector3D normal)
        {
            if (FaceAlgorithms.IsFloor(normal) || FaceAlgorithms.IsCeiling(normal))
            {
                //Take minimum X, minimum Z vertex as base Vertex
                Vertex refVertex = loop.Edges.First().StartVertex;

                foreach (var pe in loop.Edges)
                {
                    var xdist = pe.StartVertex.Position.X - refVertex.Position.X;
                    if (xdist < -0.0001)
                        refVertex = pe.StartVertex;
                    else if (xdist < 0.0001) //Same X
                    {
                        var zdist = pe.StartVertex.Position.Z - refVertex.Position.Z;
                        if (zdist < -0.0001)
                            refVertex = pe.StartVertex;
                    }
                }

                //Search ref edge: Edge that starts at refVertex and goes to the vertex with smallest X/Z
                var candidateEdges = loop.Edges.Where(x => x.Edge.Vertices.Contains(refVertex));
                var refEdge = candidateEdges.First();
                var refEdgeVertex = refEdge.Edge.Vertices.First(x => x != refVertex);
                foreach (var pe in candidateEdges)
                {
                    var otherV = pe.Edge.Vertices.First(x => x != refVertex);

                    var xdist = refEdgeVertex.Position.X - otherV.Position.X;
                    if (xdist < -0.0001)
                    {
                        refEdge = pe;
                        refEdgeVertex = otherV;
                    }
                }

                var wDirection = EdgeAlgorithms.Direction(refEdge.Edge);
                var hDirection = SimVector3D.CrossProduct(wDirection, normal);
                hDirection.Normalize();

                return new SimMatrix3D(
                    wDirection.X, hDirection.X, normal.X, 0,
                    wDirection.Y, hDirection.Y, normal.Y, 0,
                    wDirection.Z, hDirection.Z, normal.Z, 0,
                    0, 0, 0, 1
                    );
            }
            else
            {
                var wDirection = SimVector3D.CrossProduct(normal, new SimVector3D(0, 1, 0));
                wDirection.Normalize();
                var hDirection = SimVector3D.CrossProduct(wDirection, normal);
                hDirection.Normalize();

                return new SimMatrix3D(
                    wDirection.X, hDirection.X, normal.X, 0,
                    wDirection.Y, hDirection.Y, normal.Y, 0,
                    wDirection.Z, hDirection.Z, normal.Z, 0,
                    0, 0, 0, 1
                    );
            }
        }
        internal static SimSize Size(IEnumerable<SimPoint3D> points, SimMatrix3D mapping)
        {
            var projectedPoints = points.Select(x => mapping.Transform(x));

            var minW = projectedPoints.Min(x => x.X);
            var maxW = projectedPoints.Max(x => x.X);

            var minH = projectedPoints.Min(x => x.Y);
            var maxH = projectedPoints.Max(x => x.Y);

            return new SimSize(maxW - minW, maxH - minH);
        }

        /// <summary>
        /// Returns the minimum and maximum Y value of all vertices in the loop
        /// </summary>
        /// <param name="loop">The edgeloop</param>
        /// <returns>Minimum and maximum Y value of all vertices</returns>
        public static (double min, double max) HeightMinMax(EdgeLoop loop)
        {
            double min = double.PositiveInfinity;
            double max = double.NegativeInfinity;

            foreach (var pedge in loop.Edges)
            {
                min = Math.Min(pedge.StartVertex.Position.Y, min);
                max = Math.Max(pedge.StartVertex.Position.Y, max);
            }

            return (min, max);
        }


        /// <summary>
        /// Flips an <see cref="EdgeLoop"/>
        /// </summary>
        /// <param name="loop">The <see cref="EdgeLoop"/></param>
        public static void Flip(EdgeLoop loop)
        {
            var reversed = loop.Edges.Reverse().ToList();
            loop.ModelGeometry.StartBatchOperation();
            loop.Edges.Clear();

            foreach (var e in reversed)
                loop.Edges.Add(e);

            loop.ModelGeometry.EndBatchOperation();
        }

        /// <summary>
        /// Tests if an <see cref="EdgeLoop"/> contains a point
        /// </summary>
        /// <param name="l">The loop</param>
        /// <param name="v">The point</param>
        /// <param name="tolerance">Tolerance to edges</param>
        /// <param name="zTolerance">Tolerance between point and the plane spanned by the <see cref="EdgeLoop"/></param>
        /// <returns>The geometric relation</returns>
        public static GeometricRelation Contains(EdgeLoop l, SimPoint3D v, double tolerance, double zTolerance)
        {
            var (polygon, mapping) = MapToXY(l);

            if (Contains2D(polygon, mapping.Transform(v), tolerance, zTolerance))
                return GeometricRelation.Contained;
            else
                return GeometricRelation.None;
        }

        /// <summary>
        /// Tests if an <see cref="EdgeLoop"/> contains another <see cref="EdgeLoop"/>
        /// </summary>
        /// <param name="loop">The <see cref="EdgeLoop"/></param>
        /// <param name="other">The other <see cref="EdgeLoop"/></param>
        /// <param name="tolerance">Tolerance to the edges</param>
        /// <param name="zTolerance">Tolerance between point and the plane spanned by the <see cref="EdgeLoop"/></param>
        /// <returns>The geometric relation</returns>
        public static GeometricRelation Contains(EdgeLoop loop, EdgeLoop other, double tolerance, double zTolerance)
        {
            var (polygon, mapping) = MapToXY(loop);

            var contains = other.Edges.Select(x => x.StartVertex.Position).Select(v => (Contains2D(polygon, mapping.Transform(v), tolerance, zTolerance)));

            if (contains.All(x => x))
                return GeometricRelation.Contained;
            else if (contains.Any(x => x))
                return GeometricRelation.Intersecting;
            else
                return GeometricRelation.None;
        }

        /// <summary>
        /// Tests if an <see cref="EdgeLoop"/> contains another <see cref="EdgeLoop"/>
        /// </summary>
        /// <param name="loop">The <see cref="EdgeLoop"/></param>
        /// <param name="other">The other <see cref="EdgeLoop"/></param>
        /// <param name="mapping">Matrix used to map the vertices into the XY-plane</param>
        /// <param name="tolerance">Tolerance to the edges</param>
        /// <param name="zTolerance">Tolerance between point and the plane spanned by the <see cref="EdgeLoop"/></param>
        /// <returns>The geometric relation</returns>
        public static GeometricRelation Contains(List<Edge> loop, List<Edge> other, SimMatrix3D mapping, double tolerance, double zTolerance)
        {
            var verts = EdgeAlgorithms.OrderedVertexLoop(loop);
            if (!verts.Any())
                throw new ArgumentException("loop does not form a closed loop");
            var polygon = verts.Select(x => mapping.Transform(x.Position)).ToList();
            var otherVerts = EdgeAlgorithms.OrderedVertexLoop(other);

            var contains = otherVerts.Select(x => x.Position).Select(v => (Contains2D(polygon, mapping.Transform(v), tolerance, zTolerance)));

            if (contains.All(x => x))
                return GeometricRelation.Contained;
            else if (contains.Any(x => x))
                return GeometricRelation.Intersecting;
            else
                return GeometricRelation.None;
        }

        private static (List<SimPoint3D> polygon, SimMatrix3D mapping) MapToXY(EdgeLoop l)
        {
            var mapping = LoopToXYMapping(l);
            return (
                l.Edges.Select(x => mapping.Transform(x.StartVertex.Position)).ToList(),
                mapping
                );
        }

        /// <summary>
        /// Extrudes an <see cref="EdgeLoop"/> by distance in normal direction.
        /// </summary>
        /// <param name="inputLoop">Loop to extrude</param>
        /// <param name="distance">Distance to extrude</param>
        /// <param name="normal">Normal direction to extrude to</param>
        /// <returns>All the generated geometry and the resulting end <see cref="EdgeLoop"/></returns>
        public static (List<BaseGeometry> generated, EdgeLoop endEdgeloop) Extrude(EdgeLoop inputLoop, double distance, SimVector3D normal)
        {
            List<BaseGeometry> result = new List<BaseGeometry>();

            Dictionary<Vertex, Vertex> vertexLookup = new Dictionary<Vertex, Vertex>();
            Dictionary<Vertex, Edge> edgeLookup = new Dictionary<Vertex, Edge>();
            List<Edge> topEdges = new List<Edge>();

            var edges = inputLoop.Edges.Select(x => x.Edge);

            edges.First().ModelGeometry.StartBatchOperation();

            //Duplicate edges
            foreach (var e in edges)
            {
                List<Edge> newEdges = new List<Edge>();

                foreach (var v in e.Vertices)
                {
                    if (!vertexLookup.ContainsKey(v))
                    {
                        var vClone = new Vertex(v.Layer, "Vertex", v.Position + normal * distance) { Color = new DerivedColor(v.Color) };
                        vertexLookup.Add(v, vClone);
                        result.Add(vClone);
                    }

                    if (!edgeLookup.ContainsKey(v))
                    {

                        var vNew = vertexLookup[v];
                        Edge vEdge = new Edge(e.Layer, "Edge",
                            new List<Vertex> { v, vNew })
                        { Color = new DerivedColor(e.Color) };
                        result.Add(vEdge);
                        edgeLookup.Add(v, vEdge);
                    }

                    newEdges.Add(edgeLookup[v]);
                }

                Edge edge = new Edge(e.Layer, "Edge",
                    new List<Vertex> { vertexLookup[e.Vertices[0]], vertexLookup[e.Vertices[1]] })
                {
                    Color = new DerivedColor(e.Color)
                };
                newEdges.Insert(1, edge);
                result.Add(edge);
                topEdges.Add(edge);

                newEdges.Insert(0, e);
                EdgeLoop loop = new EdgeLoop(e.Layer, "EdgeLoop",
                    newEdges)
                { Color = new DerivedColor(e.Color) };
                result.Add(loop);

                Face face = new Face(e.Layer, "Face", loop) { Color = new DerivedColor(e.Color) };
                result.Add(face);
            }

            var topEdgeloop = new EdgeLoop(inputLoop.Layer, "EdgeLoop", topEdges);

            edges.First().ModelGeometry.EndBatchOperation();

            return (result, topEdgeloop);
        }

        /// <summary>
        /// Calculates the mapping of a EdgeLoop into the XY plane
        /// </summary>
        /// <param name="loop"></param>
        /// <returns></returns>
        public static SimMatrix3D LoopToXYMapping(EdgeLoop loop)
        {
            var normal = new SimVector3D(0, 0, 0);
            int i = 1;
            while (normal.LengthSquared < 0.001 && i < loop.Edges.Count)
            {
                normal = SimVector3D.CrossProduct(EdgeAlgorithms.Direction(loop.Edges[0]), EdgeAlgorithms.Direction(loop.Edges[i]));
                i++;
            }

            var z_dir = normal;
            var x_dir = EdgeAlgorithms.Direction(loop.Edges[0]);
            var y_dir = SimVector3D.CrossProduct(z_dir, x_dir);

            return new SimMatrix3D(
                x_dir.X, y_dir.X, z_dir.X, 0,
                x_dir.Y, y_dir.Y, z_dir.Y, 0,
                x_dir.Z, y_dir.Z, z_dir.Z, 0,
                0, 0, 0, 1
                );
        }



        /// <summary>
        /// Tests if a point lies inside a polygon.
        /// This method assumes that the polygon lies in the XY plane.
        /// </summary>
        /// http://geomalgorithms.com/a03-_inclusion.html
        public static bool Contains2D(List<SimPoint3D> polygon, SimPoint3D point, double tolerance, double zTolerance)
        {
            if (Math.Abs(polygon[0].Z - point.Z) > zTolerance)
                return false;

            int windingNumber = 0;
            int n = polygon.Count;

            for (int i = 0; i < n; ++i)
            {
                if (polygon[i].Y <= point.Y + tolerance)
                {
                    if (polygon[(i + 1) % n].Y > point.Y - tolerance)
                        if (IsLeft(polygon[i], polygon[(i + 1) % n], point) > -tolerance)
                            ++windingNumber;
                }
                else
                {
                    if (polygon[(i + 1) % n].Y <= point.Y + tolerance)
                        if (IsLeft(polygon[i], polygon[(i + 1) % n], point) < tolerance)
                            --windingNumber;
                }
            }

            return (windingNumber != 0);
        }

        private static double IsLeft(SimPoint3D p0, SimPoint3D p1, SimPoint3D p2)
        {
            return ((p1.X - p0.X) * (p2.Y - p0.Y)) - ((p2.X - p0.X) * (p1.Y - p0.Y));
        }

        /// <summary>
        /// Calculates the perimeter of an EdgeLoop.
        /// Sums up the length of all edges
        /// </summary>
        /// <param name="loop">The loop</param>
        /// <returns>The perimeter length</returns>
        public static double Perimeter(EdgeLoop loop)
        {
            double peri = 0.0;

            foreach (var e in loop.Edges)
            {
                peri += (e.Edge.Vertices[0].Position - e.Edge.Vertices[1].Position).Length;
            }

            return peri;
        }
        /// <summary>
        /// Calculates the perimeter of an EdgeLoop (given as polygon corners).
        /// Sums up the length of all edges
        /// </summary>
        /// <param name="polygon">The polygon</param>
        /// <returns>The perimeter length</returns>
        public static double Perimeter(List<SimPoint3D> polygon)
        {
            double peri = 0.0;

            for (int i = 0; i < polygon.Count; ++i)
            {
                peri += (polygon[(i + 1) % polygon.Count] - polygon[i]).Length;
            }

            return peri;
        }

        /// <summary>
        /// Returns the face that has the edge loop as boundary
        /// </summary>
        /// <param name="loop">The loop</param>
        /// <returns>The face that has the edge loop as boundary</returns>
        public static Face BoundaryFace(EdgeLoop loop)
        {
            return loop.Faces.FirstOrDefault(x => x.Boundary == loop);
        }
    }
}
