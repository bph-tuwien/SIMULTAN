using SIMULTAN;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides algorithms that operate on edges
    /// </summary>
    public static class EdgeAlgorithms
    {
        /// <summary>
        /// Determines whether two edges describe the same information
        /// </summary>
        /// <param name="e1">First edge</param>
        /// <param name="e2">Second edge</param>
        /// <returns>True when the two edges describe the same information, False otherwise</returns>
        public static bool IsSimilarEdge(Edge e1, Edge e2)
        {
            if (e1.Vertices[0].Id == e2.Vertices[0].Id)
            {
                return (e1.Vertices[1].Id == e2.Vertices[1].Id);
            }
            else if (e1.Vertices[1].Id == e2.Vertices[0].Id)
            {
                return (e1.Vertices[0].Id == e2.Vertices[1].Id);
            }
            return false;
        }

        /// <summary>
        /// Returns the length of the edge.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <returns>The length of the edge.</returns>
        public static double Length(Edge edge)
        {
            return (edge.Vertices[1].Position - edge.Vertices[0].Position).Length;
        }

        /// <summary>
        /// Calculates the distance of the point to the line (d) and the line equation parameter of the closed point (t)
        /// </summary>
        /// <param name="e1">The edge</param>
        /// <param name="p">The point</param>
        /// <returns>Distance of the point to the line (d) and the line equation parameter of the closed point (t)</returns>
        public static (double d, double t) EdgePointIntersection(Edge e1, SimPoint3D p)
        {
            var a = p - e1.Vertices[0].Position;
            var b = e1.Vertices[1].Position - e1.Vertices[0].Position;
            var l = b.Length;

            var t = SimVector3D.DotProduct(a, b) / (l * l);
            var d = SimVector3D.CrossProduct(b, a).Length / l;
            return (d, t);
        }

        /// <summary>
        /// Calculates the intersection between two lines
        /// </summary>
        /// <param name="e1">First edge</param>
        /// <param name="e2">Second edge</param>
        /// <param name="endLineTolerance">Defines which region at start and end of lines are considered outside.
        /// The line parameter t has to be in [tolerance, 1-tolerance] to be considered intersecting</param>
        /// <param name="tolerance">The numerical tolerance for comparisons</param>
        /// <returns>A bool indicating whether the two edges intersect and the line parameters t1 for e1 and t2 for e2</returns>
        public static (bool isIntersecting, double t1, double t2) EdgeEdgeIntersection(Edge e1, Edge e2, double tolerance, double endLineTolerance = 0)
        {
            // Source: http://paulbourke.net/geometry/pointlineplane/calclineline.cs
            var p1 = e1.Vertices[0].Position;
            var p2 = e1.Vertices[1].Position;
            var p3 = e2.Vertices[0].Position;
            var p4 = e2.Vertices[1].Position;

            var p43 = p4 - p3;

            if (p43.LengthSquared < 0.00001)
                return (false, double.NaN, double.NaN);

            var p21 = p2 - p1;
            if (p21.LengthSquared < 0.00001)
                return (false, double.NaN, double.NaN);

            var p13 = p1 - p3;
            var d1343 = SimVector3D.DotProduct(p13, p43);
            var d4321 = SimVector3D.DotProduct(p43, p21);
            var d1321 = SimVector3D.DotProduct(p13, p21);
            var d4343 = SimVector3D.DotProduct(p43, p43);
            var d2121 = SimVector3D.DotProduct(p21, p21);

            var denom = d2121 * d4343 - d4321 * d4321;
            if (Math.Abs(denom) < 0.00001)
                return (false, double.NaN, double.NaN);

            var numer = d1343 * d4321 - d1321 * d4343;

            var mua = numer / denom;

            var muaTol = endLineTolerance / p21.Length;
            if (mua >= muaTol && mua <= 1 - muaTol)
            {
                var mub = (d1343 + d4321 * (mua)) / d4343;

                var mubTol = endLineTolerance / p43.Length;
                if (mub >= mubTol && mub <= 1 - mubTol)
                {
                    var ip1 = p1 + p21 * mua;
                    var ip2 = p3 + p43 * mub;

                    if ((ip2 - ip1).LengthSquared < tolerance * tolerance)
                        return (true, mua, mub);
                }
            }

            return (false, double.NaN, double.NaN);
        }

        /// <summary>
        /// Determines whether to edges are on the same line
        /// </summary>
        /// <param name="e1">First edge</param>
        /// <param name="e2">Second edge</param>
        /// <param name="tolerance">Accepted tolerance</param>
        /// <returns>True when both edges are on the same line, False otherwise</returns>
        public static bool IsOnSameLine(Edge e1, Edge e2, double tolerance)
        {
            double t2 = tolerance * tolerance * tolerance * tolerance; //Squared since lengthsquared + squared since area test

            var AB = e1.Vertices[1].Position - e1.Vertices[0].Position;
            var AC = e2.Vertices[0].Position - e1.Vertices[0].Position;

            if (SimVector3D.CrossProduct(AB, AC).LengthSquared <= t2)
            {
                var AD = e2.Vertices[1].Position - e1.Vertices[0].Position;
                if (SimVector3D.CrossProduct(AB, AD).LengthSquared <= t2)
                    return true;
                return false;
            }
            return false;
        }

        /// <summary>
        /// Calculates the direction of a PEdge
        /// </summary>
        /// <param name="e">The edge</param>
        /// <returns>The direction of the edge</returns>
        public static SimVector3D Direction(PEdge e)
        {
            SimVector3D d;

            if (e.Orientation == GeometricOrientation.Forward || e.Orientation == GeometricOrientation.Undefined)
                d = e.Edge.Vertices[1].Position - e.Edge.Vertices[0].Position;
            else
                d = e.Edge.Vertices[0].Position - e.Edge.Vertices[1].Position;

            d.Normalize();
            return d;
        }
        /// <summary>
        /// Returns the normalized direction of an edge. It is undefined which of the two possible directions is returned.
        /// </summary>
        /// <param name="e">The edge</param>
        /// <returns>One of the two possible directions of the edge</returns>
        public static SimVector3D Direction(Edge e)
        {
            SimVector3D d = e.Vertices[1].Position - e.Vertices[0].Position;

            if (d.LengthSquared < 0.000001)
                return new SimVector3D(0, 0, 0);

            d.Normalize();
            return d;
        }

        /// <summary>
        /// Returns all the geometry contained in this edge (edge + vertices)
        /// </summary>
        /// <param name="e">The edge</param>
        /// <param name="geometries">The resulting geometry list</param>
        public static void ContainedGeometry(Edge e, ref List<BaseGeometry> geometries)
        {
            geometries.Add(e);
            geometries.Add(e.Vertices[0]);
            geometries.Add(e.Vertices[1]);
        }

        /// <summary>
        /// Gets all adjacent faces of the edge and adds them to the faces list.
        /// </summary>
        /// <param name="e">The edge</param>
        /// <param name="faces">The faces list</param>
        public static void AdjacentFaces(this Edge e, ref HashSet<Face> faces)
        {
            var f = e.PEdges.SelectMany(x => x.Parent.Faces);
            foreach (var face in f)
            {
                if (!faces.Contains(face))
                    faces.Add(face);
            }
        }

        /// <summary>
        /// Tries to order the edges such that they form a closed loop by following the edges.
        /// Self intersecting loops will return false.
        /// </summary>
        /// <param name="edges">The edges</param>
        /// <returns>A bool indicating if the edges form a closed loop. The list contains the ordered edges or null when they don't form a loop.</returns>
        public static (bool isLoop, List<Edge> loop) OrderLoop(IEnumerable<Edge> edges)
        {
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));
            if (!edges.Any())
                throw new ArgumentException("Input has to contain at least one edge");
            var first = edges.First();
            return OrderLoop(edges, first, GeometricOrientation.Forward);
        }

        /// <summary>
        /// Tries to order the edges such that they form a closed loop by following the edges.
        /// Self intersecting loops will return false.
        /// Loop will be ordered starting with the startEdge and moving in the direction of the startVertex.
        /// </summary>
        /// <param name="edges">The edges</param>
        /// <param name="startEdge">Edge to start with</param>
        /// <param name="startEdgeOrientation">Orientation of the start edge</param>
        /// <returns>A bool indicating if the edges form a closed loop. The list contains the ordered edges or null when they don't form a loop.</returns>
        public static (bool isLoop, List<Edge> loop) OrderLoop(IEnumerable<Edge> edges, Edge startEdge, GeometricOrientation startEdgeOrientation)
        {
            //Error handling
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));
            if (startEdge == null)
                throw new ArgumentNullException(nameof(startEdge));

            var notHandled = new List<Edge>(edges);
            if (notHandled.Count == 0)
                throw new ArgumentException("Input has to contain at least one edge");

            //Run around loop
            var loop = new List<Edge>();
            var currentVertex = startEdgeOrientation == GeometricOrientation.Forward ? startEdge.Vertices[1] : startEdge.Vertices[0];
            var lastVertex = startEdge.Vertices.First(x => x != currentVertex);
            var addedVertices = new HashSet<Vertex> { currentVertex };

            loop.Add(startEdge);
            notHandled.Remove(startEdge);

            while (notHandled.Count > 0)
            {
                var nextEdgeIdx = notHandled.FindIndex(x => x.Vertices.Contains(currentVertex));

                if (nextEdgeIdx == -1) //No edge found -> can't be a closed loop
                    return (false, null);

                var nextEdge = notHandled[nextEdgeIdx];
                notHandled.RemoveAt(nextEdgeIdx);
                loop.Add(nextEdge);

                currentVertex = nextEdge.Vertices.First(x => x != currentVertex);
                if (addedVertices.Contains(currentVertex))
                    return (false, null);
                addedVertices.Add(currentVertex);
            }

            //Check if last point == first point
            if (currentVertex != lastVertex)
                return (false, null);

            return (true, loop);
        }
        /// <summary>
        /// Tries to order the pedges such that they form a closed loop by following the edges.
        /// Self intersecting loops will return false.
        /// </summary>
        /// <param name="edges">The PEdges</param>
        /// <returns>A bool indicating if the edges form a closed loop. The list contains the ordered edges or null when they don't form a loop.</returns>
        public static (bool isLoop, List<PEdge> loop) OrderLoop(IEnumerable<PEdge> edges)
        {
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));
            if (!edges.Any())
                throw new ArgumentException("Input has to contain at least one edge");

            return OrderLoop(edges, edges.First().Edge, GeometricOrientation.Forward);
        }

        /// <summary>
        /// Tries to order the pedges such that they form a closed loop by following the edges.
        /// Starts with a startEdge and moves in startEdgeOrientation direction
        /// </summary>
        /// <param name="edges">The PEdges</param>
        /// <param name="startEdge">The first Edge to handle</param>
        /// <param name="startEdgeOrientation">The orientation in which the first edge should be handled</param>
        /// <returns>A bool indicating if the edges form a closed loop. The list contains the ordered edges or null when they don't form a loop.</returns>
        public static (bool isLoop, List<PEdge> loop) OrderLoop(IEnumerable<PEdge> edges, Edge startEdge, GeometricOrientation startEdgeOrientation)
        {
            //Error handling
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));
            if (startEdge == null)
                throw new ArgumentNullException(nameof(startEdge));
            if (startEdgeOrientation == GeometricOrientation.Undefined)
                throw new ArgumentException("Undefined orientation is not supported");

            var notHandled = new List<PEdge>(edges);
            if (notHandled.Count == 0)
                throw new ArgumentException("Input has to contain at least one edge");

            //Run around loop
            var loop = new List<PEdge>() { };
            var currentVertex = startEdgeOrientation == GeometricOrientation.Forward ? startEdge.Vertices[1] : startEdge.Vertices[0];
            var lastVertex = startEdge.Vertices.First(x => x != currentVertex);
            var addedVertices = new HashSet<Vertex> { currentVertex };

            var startEdgeIdx = notHandled.FindIndex(x => x.Edge == startEdge);
            loop.Add(notHandled[startEdgeIdx]);
            notHandled.RemoveAt(startEdgeIdx);

            while (notHandled.Count > 0)
            {
                var nextEdgeIdx = notHandled.FindIndex(x => x.Edge.Vertices.Contains(currentVertex));

                if (nextEdgeIdx == -1) //No edge found -> can't be a closed loop
                    return (false, null);

                var nextEdge = notHandled[nextEdgeIdx];
                notHandled.RemoveAt(nextEdgeIdx);
                loop.Add(nextEdge);

                currentVertex = nextEdge.Edge.Vertices.First(x => x != currentVertex);
                if (addedVertices.Contains(currentVertex))
                    return (false, null);
                addedVertices.Add(currentVertex);
            }

            // check if we reached the last vertex of the first edge (if is closed)
            if (currentVertex != lastVertex)
                return (false, null);

            return (true, loop);
        }

        /// <summary>
        /// Orders a list of edges and returns an ordered list of corner points
        /// </summary>
        /// <param name="edges">The edges</param>
        /// <param name="matrix">An optional mapping matrix. Only used to transform the resulting point list.</param>
        /// <returns></returns>
        public static List<SimPoint3D> OrderedPointLoop(IEnumerable<Edge> edges, SimMatrix3D matrix)
        {
            var vertices = OrderedVertexLoop(edges);

            return vertices.Select(x => matrix.Transform(x.Position)).ToList();
        }

        /// <summary>
        /// Orders a list of edges and returns an ordered list of vertices
        /// </summary>
        /// <param name="edges">The edges</param>
        /// <returns>The ordered list of vertices, or an empty list if no loop could be found</returns>
        public static List<Vertex> OrderedVertexLoop(IEnumerable<Edge> edges)
        {
            if (!edges.Any())
                return new List<Vertex>();

            HashSet<Edge> hedges = new HashSet<Edge>(edges);

            var currentEdge = hedges.First();
            var currentVertex = currentEdge.Vertices.First();
            List<Vertex> points = new List<Vertex>();

            while (hedges.Count > 0)
            {
                // This is faster but could be wrong if vertices are not consistent
                // var nextEdge = currentVertex.Edges.FirstOrDefault(x => hedges.Contains(x));
                var nextEdge = hedges.FirstOrDefault(x => x.Vertices.Contains(currentVertex));

                if (nextEdge == null)
                    return new List<Vertex>();

                hedges.Remove(nextEdge);
                currentVertex = nextEdge.Vertices.First(x => x != currentVertex);

                points.Add(currentVertex);
            }

            return points;
        }

        /// <summary>
        /// Tests for Edge-Edge intersections in the 2D plane. Z is ignored
        /// https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect
        /// </summary>
        /// <param name="e0p0">First line first point</param>
        /// <param name="e0p1">First line second point</param>
        /// <param name="e1p0">Second line first point</param>
        /// <param name="e1p1">Second line second point</param>
        /// <param name="tolerance">Tolerance for calculations</param>
        /// <returns>True when the two edges intersect, False otherwise.
        /// t1 is intersection parameter along first line, t2 is intersection parameter along second line</returns>
        public static (bool isIntersecting, double t1, double t2) EdgeEdgeIntersection2D(SimPoint3D e0p0, SimPoint3D e0p1, SimPoint3D e1p0, SimPoint3D e1p1, double tolerance)
        {
            double s1_x = e0p1.X - e0p0.X;
            double s1_y = e0p1.Y - e0p0.Y;

            double s2_x = e1p1.X - e1p0.X;
            double s2_y = e1p1.Y - e1p0.Y;

            double div = (-s2_x * s1_y + s1_x * s2_y);
            if (Math.Abs(div) < tolerance)
                return (false, double.NaN, double.NaN);

            double s = (-s1_y * (e0p0.X - e1p0.X) + s1_x * (e0p0.Y - e1p0.Y));
            if (s < -tolerance || s > div + tolerance)
                return (false, double.NaN, double.NaN);

            double t = (s2_x * (e0p0.Y - e1p0.Y) - s2_y * (e0p0.X - e1p0.X));
            if (t < -tolerance || t > div + tolerance)
                return (false, double.NaN, double.NaN);

            return (true, s / div, t / div);
        }


        /// <summary>
        /// Replaces an edges with multiple new edges (also handles parent geometry)
        /// </summary>
        /// <param name="original">Original edge</param>
        /// <param name="replacements">List of edges that replace original</param>
        public static void ReplaceEdge(Edge original, List<Edge> replacements)
        {
            foreach (var pedge in original.PEdges)
            {
                int idx = pedge.Parent.Edges.IndexOf(pedge);
                pedge.Parent.Edges.RemoveAt(idx);

                if (pedge.Orientation == GeometricOrientation.Forward)
                {
                    for (int i = 0; i < replacements.Count; ++i)
                    {
                        var repPEdge = new PEdge(replacements[i], GeometricOrientation.Undefined, pedge.Parent);
                        pedge.Parent.Edges.Insert(idx + i, repPEdge);
                        replacements[i].PEdges.Add(repPEdge);
                    }
                }
                else// if (pedge.Orientation == GeometricOrientation.Undefined)
                {
                    for (int i = 0; i < replacements.Count; ++i)
                    {
                        var repPEdge = new PEdge(replacements[replacements.Count - i - 1], GeometricOrientation.Undefined, pedge.Parent);
                        pedge.Parent.Edges.Insert(idx + i, repPEdge);
                        replacements[replacements.Count - i - 1].PEdges.Add(repPEdge);
                    }
                }
            }
        }

        /// <summary>
        /// Tries to intersect two edges. If they intersect split both at the intersection and connect everything together again.
        /// </summary>
        /// <param name="edge1">The first edge</param>
        /// <param name="edge2">The second edge</param>
        /// <param name="tolerance">The detection tolerance</param>
        /// <returns>The created vertex, an array of created edges and an undo item for the whole operation.</returns>
        [Obsolete("Same as ModelCleanupAlgorithms.SplitEdgeEdgeIntersections")]
        public static (Vertex v, Edge[] edges, IUndoItem undoItem) IntersectEdges(Edge edge1, Edge edge2, double tolerance = 0)
        {
            if (edge1.ModelGeometry != edge2.ModelGeometry)
            {
                throw new ArgumentException("Edges can only be intersected if they are in the same geometry model.");
            }

            List<IUndoItem> undoItems = new List<IUndoItem>();

            var intersection = EdgeEdgeIntersection(edge1, edge2, tolerance);
            if (!intersection.isIntersecting)
                return (null, null, null);

            var edgedir = (edge1.Vertices[1].Position - edge1.Vertices[0].Position);
            var pointOnLine = edge1.Vertices[0].Position + edgedir * intersection.t1;

            edge1.ModelGeometry.StartBatchOperation();

            edge1.RemoveFromModel();
            edge2.RemoveFromModel();
            undoItems.Add(new GeometryRemoveUndoItem(new List<BaseGeometry>() { edge1 }, edge1.ModelGeometry));
            undoItems.Add(new GeometryRemoveUndoItem(new List<BaseGeometry>() { edge2 }, edge2.ModelGeometry));

            Vertex v = new Vertex(edge1.Layer, "", pointOnLine)
            {
                Color = new DerivedColor(edge1.Color)
            };
            Edge e11 = new Edge(edge1.Layer, "", new Vertex[] { edge1.Vertices[0], v })
            {
                Color = new DerivedColor(edge1.Color)
            };
            Edge e12 = new Edge(edge1.Layer, "", new Vertex[] { v, edge1.Vertices[1] })
            {
                Color = new DerivedColor(edge1.Color)
            };
            Edge e21 = new Edge(edge2.Layer, "", new Vertex[] { edge2.Vertices[0], v })
            {
                Color = new DerivedColor(edge2.Color)
            };
            Edge e22 = new Edge(edge2.Layer, "", new Vertex[] { v, edge2.Vertices[1] })
            {
                Color = new DerivedColor(edge2.Color)
            };

            undoItems.Add(new GeometryAddUndoItem(new List<BaseGeometry>() { v, e11, e12, e21, e22 }, edge1.ModelGeometry));

            //Update Loop-Face-Volume
            foreach (var pe in edge1.PEdges)
            {
                BaseEdgeContainer container = pe.Parent;
                var idx = container.Edges.IndexOf(pe);
                undoItems.Add(CollectionUndoItem.RemoveAt(container.Edges, idx));

                undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e11, pe.Orientation, container), idx));
                if (pe.Orientation == GeometricOrientation.Forward)
                    undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e12, pe.Orientation, container), idx + 1));
                else
                    undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e12, pe.Orientation, container), idx));
            }

            foreach (var pe in edge2.PEdges)
            {
                BaseEdgeContainer container = pe.Parent;
                var idx = container.Edges.IndexOf(pe);
                undoItems.Add(CollectionUndoItem.RemoveAt(container.Edges, idx));

                undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e21, pe.Orientation, container), idx));
                if (pe.Orientation == GeometricOrientation.Forward)
                    undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e22, pe.Orientation, container), idx + 1));
                else
                    undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e22, pe.Orientation, container), idx));
            }

            edge1.ModelGeometry.EndBatchOperation();

            return (v, new Edge[] { edge1, edge2 }, new BatchOperationGroupUndoItem(edge1.ModelGeometry, undoItems));
        }
        /// <summary>
        /// Splits an edge at a specific position
        /// </summary>
        /// <param name="e">The edge</param>
        /// <param name="splitPosition">The position for the split (should be close to the line)</param>
        public static (Vertex v, Edge[] edges, IUndoItem undoItem) SplitEdge(Edge e, SimPoint3D splitPosition)
        {
            List<IUndoItem> undoItems = new List<IUndoItem>();

            //Find closest point on edge
            var dp = EdgePointIntersection(e, splitPosition);
            var pointOnLine = (SimPoint3D)((1.0 - dp.t) * (SimVector3D)e.Vertices[0].Position + dp.t * (SimVector3D)e.Vertices[1].Position);

            e.ModelGeometry.StartBatchOperation();

            e.RemoveFromModel();
            undoItems.Add(new GeometryRemoveUndoItem(new List<BaseGeometry>() { e }, e.ModelGeometry));

            Vertex v = new Vertex(e.Layer, "", pointOnLine)
            {
                Color = new DerivedColor(e.Color)
            };
            Edge e1 = new Edge(e.Layer, "", new Vertex[] { e.Vertices[0], v })
            {
                Color = new DerivedColor(e.Color)
            };
            Edge e2 = new Edge(e.Layer, "", new Vertex[] { v, e.Vertices[1] })
            {
                Color = new DerivedColor(e.Color)
            };

            undoItems.Add(new GeometryAddUndoItem(new List<BaseGeometry>() { v, e1, e2 }, e.ModelGeometry));

            //Update Loop-Face-Volume
            foreach (var pe in e.PEdges)
            {
                BaseEdgeContainer container = pe.Parent;
                var idx = container.Edges.IndexOf(pe);
                undoItems.Add(CollectionUndoItem.RemoveAt(container.Edges, idx));

                undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e1, pe.Orientation, container), idx));
                if (pe.Orientation == GeometricOrientation.Forward)
                    undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e2, pe.Orientation, container), idx + 1));
                else
                    undoItems.Add(CollectionUndoItem.Insert(container.Edges, new PEdge(e2, pe.Orientation, container), idx));
            }

            e.ModelGeometry.EndBatchOperation();

            return (v, new Edge[] { e1, e2 }, new BatchOperationGroupUndoItem(e.ModelGeometry, undoItems));
        }

        /// <summary>
        /// Returns whether two given edges are connected
        /// </summary>
        /// <param name="e1">Edge 1</param>
        /// <param name="e2">Edge 2</param>
        /// <returns>true if edges share a vertex</returns>
        public static bool IsConnectedEdge(Edge e1, Edge e2)
        {
            return e1.Vertices[0] == e2.Vertices[0] || e1.Vertices[0] == e2.Vertices[1] || e1.Vertices[1] == e2.Vertices[0] || e1.Vertices[1] == e2.Vertices[1];
        }


        /// <summary>
        /// Returns if the edge is horizontal (the Y component of it's direction &lt;= epsilon)
        /// </summary>
        /// <param name="e">The edge</param>
        /// <param name="epsilon">The epsilon</param>
        /// <returns>If the edge is horizontal</returns>
        public static bool IsHorizontal(Edge e, double epsilon = 0.001)
        {
            return Direction(e).Y < epsilon;
        }
    }
}
