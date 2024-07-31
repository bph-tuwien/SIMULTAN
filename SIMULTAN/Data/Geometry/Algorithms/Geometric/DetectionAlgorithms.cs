using Assimp;
using MathNet.Numerics.LinearAlgebra;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using Sprache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides methods for detecting geometric properties in a model
    /// </summary>
    public static class DetectionAlgorithms
    {
        /// <summary>
        /// Detects faces in the XZ plane
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="tolerance">Calculation tolerance</param>
        public static void DetectXZFaces(GeometryModelData model, double tolerance)
        {
            DetectXZFaces(model, model.Edges, tolerance);
        }

        /// <summary>
        /// Detects faces in the XZ plane for a given set of edges
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="edges">The edges</param>
        /// <param name="tolerance">Calculation tolerance</param>
        public static void DetectXZFaces(GeometryModelData model, IEnumerable<Edge> edges, double tolerance)
        {
            model.StartBatchOperation();

            List<Tuple<double, List<Edge>>> levels = new List<Tuple<double, List<Edge>>>();

            //Sort by XZ layer, ignore non-xz edges
            foreach (var edge in edges)
            {
                var dir = edge.Vertices[0].Position - edge.Vertices[1].Position;
                if (Math.Abs(dir.Y) <= tolerance)
                {
                    var level = levels.FirstOrDefault(x => Math.Abs(x.Item1 - edge.Vertices[0].Position.Y) <= tolerance);
                    if (level == null)
                    {
                        level = new Tuple<double, List<Edge>>(edge.Vertices[0].Position.Y, new List<Edge>());
                        levels.Add(level);
                    }

                    level.Item2.Add(edge);
                }
            }

            foreach (var level in levels)
                DetectXZFacesInLevel(level.Item2, tolerance);

            model.EndBatchOperation();
        }


        /// <summary>
        /// Fits a plane through points in least square sense.
        /// </summary>
        /// <param name="points">Collection of 3D points to fit plane through</param>
        /// <returns></returns>
        public static (SimPoint3D center, SimVector3D normal, double d) BestFittingPlane(IEnumerable<SimPoint3D> points)
        {
            //Calculate centroid
            SimVector3D center = new SimVector3D(0, 0, 0);
            foreach (var p in points)
                center += (SimVector3D)p;
            center /= points.Count();

            //Move all vertices such that centroid = 0,0,0
            var centeredVertices = points.Select(x => x - center).ToList();

            //Setup dense matrix
            var matrix = Matrix<double>.Build.Dense(3, centeredVertices.Count);
            for (int i = 0; i < centeredVertices.Count; ++i)
            {
                matrix[0, i] = centeredVertices[i].X;
                matrix[1, i] = centeredVertices[i].Y;
                matrix[2, i] = centeredVertices[i].Z;
            }

            //Perform SVD
            var svd = matrix.Svd(true);

            int smallestIdx = -1;
            double smallestEigVal = double.PositiveInfinity;
            for (int i = 0; i < svd.S.Count; ++i)
            {
                if (svd.S[i] < smallestEigVal)
                {
                    smallestEigVal = svd.S[i];
                    smallestIdx = i;
                }
            }

            //Calculate plane
            SimVector3D n = new SimVector3D(svd.U[0, smallestIdx], svd.U[1, smallestIdx], svd.U[2, smallestIdx]);
            n.Normalize();
            double d = -SimVector3D.DotProduct(n, center);

            return ((SimPoint3D)center, n, d);
        }

        /// <summary>
        /// Computes a direction vector along a face relative to a given edge.
        /// </summary>
        /// <param name="face">The final vector lies on the plane of this face</param>
        /// <param name="edge">The final vector is orthogonal to this edge</param>
        /// <returns>Normalized direction vector</returns>
        public static SimVector3D DirectionFromFaceAndEdge(Face face, PEdge edge)
        {
            SimQuaternion rt = face.Normal.Length == 0.0 ? SimQuaternion.Identity : new SimQuaternion(face.Normal, 90);
            SimMatrix3D rm = new SimMatrix3D();
            rm.Rotate(rt);

            SimVector3D direction = EdgeAlgorithms.Direction(edge);

            SimVector3D lineMiddlePoint = ((SimVector3D)edge.Edge.Vertices[0].Position + (SimVector3D)edge.Edge.Vertices[1].Position) / 2.0;

            var result = rm.Transform(direction);
            result.Normalize();

            if (FaceAlgorithms.Contains(face, (SimPoint3D)(lineMiddlePoint + result * 0.01), 0.001, 0.01) != GeometricRelation.Contained)
                result *= -1;

            return result;
        }


        /// <summary>
        /// Detects faces from a set of edges. First removes dangling edges, then tries to find one closed edge loop.
        /// </summary>
        /// <param name="edges">List of edges which could define a face</param>
        /// <param name="model">The model into which the face should be generated</param>
        /// <returns></returns>
        public static List<BaseGeometry> DetectFacesFromEdges(List<Edge> edges, GeometryModelData model)
        {
            List<BaseGeometry> addedGeometry = new List<BaseGeometry>();

            var noDangling = RemoveDanglingEdges(edges);

            var edgeGroups = EdgeConnectedVertexGroups(noDangling);

            if (edges != null && edges.Count >= 3 && edgeGroups.Count == 1 && edgeGroups[0].First() == edgeGroups[0].Last())
            {
                var connectedEdges = new List<Edge>();

                for (int i = 0; i < edgeGroups[0].Count - 1; ++i)
                {
                    connectedEdges.Add(edges.First(x => x.Vertices.Contains(edgeGroups[0][i]) && x.Vertices.Contains(edgeGroups[0][(i + 1)])));
                }

                if (EdgeAlgorithms.OrderLoop(connectedEdges).isLoop)
                {
                    model.StartBatchOperation();
                    var specialCaseEL = new EdgeLoop(edges[0].Layer, "{0}", connectedEdges);
                    Face specialCaseF = new Face(edges[0].Layer, "{0}", specialCaseEL);
                    addedGeometry.Add(specialCaseEL);
                    addedGeometry.Add(specialCaseF);
                    model.EndBatchOperation();
                    return addedGeometry;
                }
            }

            return addedGeometry;
        }


        private static void DetectXZFacesInLevel(List<Edge> edges, double tolerance)
        {
            //Remove edges that don't have connecting edges on both sides
            List<Edge> localEdges = RemoveDanglingEdges(edges);

            Dictionary<Edge, int> edgeUsed = new Dictionary<Edge, int>(); // 0 unused, -1 backward used, 1 forward used, 2 both used
            localEdges.ForEach(x => edgeUsed.Add(x, 0));

            while (edgeUsed.Any(x => x.Value != 2))
            {
                //Select start edge and vertex
                var startEdgeEntry = edgeUsed.First(x => x.Value != 2);
                var currentEdge = startEdgeEntry.Key;
                var currentEdgeStartVertex = currentEdge.Vertices[0];
                var currentEdgeEndVertex = currentEdge.Vertices[1];

                if (startEdgeEntry.Value == 1)
                {
                    currentEdgeStartVertex = currentEdge.Vertices[1];
                    currentEdgeEndVertex = currentEdge.Vertices[0];
                }

                if (Math.Abs(startEdgeEntry.Value) == 1)
                    edgeUsed[startEdgeEntry.Key] = 2;
                else
                    edgeUsed[startEdgeEntry.Key] = 1;

                var startVertex = currentEdgeStartVertex;
                bool cycleClosed = false;

                //Follow cycle
                List<Edge> cycle = new List<Edge> { currentEdge };
                double area = SignedAreaXZ(currentEdgeStartVertex, currentEdgeEndVertex);

                while (!cycleClosed)
                {
                    //Find vertex to connect next edge
                    var nextVertex = currentEdgeEndVertex;
                    var nextEdgeCandidates = nextVertex.Edges.Where(
                        x => !cycle.Contains(x) &&
                        edgeUsed.ContainsKey(x) &&
                        x.Vertices.Any(y => y == nextVertex)
                        );

                    //Search for edge with smallest signed angle to current edge
                    var currentDirection = nextVertex.Position - currentEdgeStartVertex.Position;

                    var minAngle = double.PositiveInfinity;
                    Edge minCandidate = null;

                    foreach (var nextCandidate in nextEdgeCandidates)
                    {
                        var nextDirection = nextCandidate.Vertices.First(x => x != nextVertex).Position - nextVertex.Position;

                        //Calculate angle in range [-pi, pi]
                        var angle = SignedAngle(new SimVector(currentDirection.X, currentDirection.Z), new SimVector(nextDirection.X, nextDirection.Z));

                        if (angle < minAngle)
                        {
                            minCandidate = nextCandidate;
                            minAngle = angle;
                        }
                    }

                    if (minCandidate == null)
                    {
                        break; //Cycle can't be closed.
                    }

                    //Update area
                    var nextNextVertex = minCandidate.Vertices.First(x => x != nextVertex);
                    area += SignedAreaXZ(nextVertex, nextNextVertex);

                    //Use found edge as next edge
                    currentEdge = minCandidate;
                    currentEdgeStartVertex = nextVertex;
                    currentEdgeEndVertex = nextNextVertex;

                    //Mark used edge
                    var usedCount = edgeUsed[currentEdge];
                    if (usedCount == 0 && currentEdge.Vertices[0] == currentEdgeStartVertex)
                        edgeUsed[currentEdge] = 1;
                    else if (usedCount == 0 && currentEdge.Vertices[1] == currentEdgeStartVertex)
                        edgeUsed[currentEdge] = -1;
                    else if (usedCount == 1 || edgeUsed[currentEdge] == -1)
                        edgeUsed[currentEdge] = 2;
                    else if (usedCount == 2)
                    {
                        Debug.Write("Failed: ");
                        cycle.ForEach(x => Debug.Write("{0}, ", x.Name));
                        throw new Exception(string.Format("Unable to detect faces around edge {0}", currentEdge.Name));
                    }

                    //Add to cycle and repeat
                    cycle.Add(currentEdge);
                    if (currentEdgeEndVertex == startVertex)
                        cycleClosed = true;
                }

                //Identify if cycle is not outer cycle
                if (area < 0)
                {
                    cycle.Reverse();

                    //Check if loop/window exist
                    var potentialLoops = cycle.First().PEdges.Where(x => x.Parent is EdgeLoop).Select(x => (EdgeLoop)x.Parent).ToList();
                    bool loopExists = false;

                    foreach (var pL in potentialLoops)
                    {
                        loopExists |= cycle.All(ce => ce.PEdges.Any(cepe => cepe.Parent == pL));
                        if (loopExists)
                            break;
                    }

                    if (!loopExists)
                    {
                        EdgeLoop el = new EdgeLoop(cycle.First().Layer, "{0}", cycle);
                        Face f = new Face(el.Layer, "{0}", el);
                    }
                }

            }
        }

        private static double SignedAreaXZ(Vertex v1, Vertex v2)
        {
            return (v1.Position.X * v2.Position.Z - v2.Position.X * v1.Position.Z);
        }

        private static List<Edge> RemoveDanglingEdges(List<Edge> edges)
        {
            List<Edge> result = new List<Edge>(edges);

            bool iterationNeeded = true;

            while (iterationNeeded)
            {
                int count = result.Count;
                result.RemoveWhere(e => e.Vertices.Any(v => v.Edges.Count(ve => result.Contains(ve)) <= 1));

                if (count == result.Count)
                    iterationNeeded = false;

            }

            return result;
        }

        /// <summary>
        /// Computes the signed angle (in counter clockwise direction) between two vectors in 2D.
        /// </summary>
        /// <param name="v1">vector 1</param>
        /// <param name="v2">vector 2</param>
        /// <returns>Signed angle between two 2D vectors in the range [-pi, pi].</returns>
		public static double SignedAngle(SimVector v1, SimVector v2)
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

        /// <summary>
        /// Computes the signed angle between two directions along a third direction
        /// </summary>
        /// <param name="v1">First direction</param>
        /// <param name="v2">Second direction</param>
        /// <param name="vn">Rotation Axis</param>
        /// <returns>The signed angle</returns>
        public static double SignedAngle(SimVector3D v1, SimVector3D v2, SimVector3D vn)
        {
            v1.Normalize();
            v2.Normalize();
            double angle = Math.Acos(SimVector3D.DotProduct(v1, v2));
            SimVector3D cross = SimVector3D.CrossProduct(v1, v2);
            if (SimVector3D.DotProduct(vn, cross) < 0)
            {
                angle = -angle;
            }

            return angle;
        }

        /// <summary>
        /// Computes the shortest distance between angles in degrees, i.e. distance is always less than 180°
        /// https://gist.github.com/shaunlebron/8832585
        /// </summary>
        /// <param name="a0">First angle in degrees</param>
        /// <param name="a1">Second angle in degrees</param>
        /// <returns>Shortest distance between angles in degrees</returns>
        public static double ShortAngleDist(double a0, double a1)
        {
            var max = 360.0;
            var da = (a1 - a0) % max;
            return 2.0 * da % max - da;
        }

        /// <summary>
        /// Finds all connected (share a vertex) edge loops and returns all the connected clusters.
        /// </summary>
        /// <param name="loops">The loops</param>
        /// <returns>An array off all connected loops in the list</returns>
        public static List<EdgeLoop>[] FindConnectedEdgeLoopGroups(IEnumerable<EdgeLoop> loops)
        {
            // find adjacent loops
            var adjacentLoops = loops.SelectMany(loop => loop.Edges.SelectMany(edge => edge.Edge.Vertices.Select(v => (vertex: v, loop: loop))))
                .GroupBy(x => x.vertex)
                .Select(k => k.Select(x => x.loop).Distinct().ToList());

            var allLoops = adjacentLoops.SelectMany(x => x).Distinct().ToList();

            // create index lookup for adjacency matrix generation
            var indexLookup = new Dictionary<EdgeLoop, int>();
            var count = 0;
            foreach (var loop in allLoops)
            {
                indexLookup.Add(loop, count++);
            }

            // build adjacency matrix
            bool[,] adjacencies = new bool[count, count];
            foreach (var connected in adjacentLoops)
            {
                for (int i = 0; i < connected.Count - 1; i++)
                {
                    for (int j = i + 1; j < connected.Count; j++)
                    {
                        int a = indexLookup[connected[i]];
                        int b = indexLookup[connected[j]];
                        adjacencies[a, b] = true;
                        adjacencies[b, a] = true;
                    }
                }
            }

            var result = DetectAdjacencies(allLoops, adjacencies);

            return result;
        }

        /// <summary>
        /// Find all connected clusters of edges.
        /// </summary>
        /// <param name="edges">The edges</param>
        /// <returns>Array of edge lists that are connected.</returns>
        public static List<Edge>[] FindConnectedEdgeGroups(this IEnumerable<Edge> edges)
        {

            // lookup from each vertex to all connected edges
            var vertexEdgeLookup = edges.SelectMany(x => x.Vertices.Select(v => (v, x))).GroupBy(x => x.v)
                .ToDictionary(x => x.Key, x => x.Select(y => y.x).ToList());

            var vertices = vertexEdgeLookup.Keys.ToList();
            var count = vertices.Count;
            var vertexIndices = vertices.Select((x, i) => (x, i)).ToDictionary(x => x.x, x => x.i);

            // build adjacency of all connected vertices
            var adjacencies = new bool[count, count];
            for (int i = 0; i < count; i++)
            {
                foreach (var edge in vertexEdgeLookup[vertices[i]])
                {
                    foreach (var vertex in edge.Vertices)
                    {
                        var j = vertexIndices[vertex];
                        adjacencies[i, j] = true;
                        adjacencies[j, i] = true;
                    }
                }
            }

            // find connected vertex groups
            var vertexGroups = DetectAdjacencies(vertices, adjacencies);
            // find all edges of the vertex groups
            return vertexGroups.Select(x => x.SelectMany(v => vertexEdgeLookup[v]).Distinct().ToList()).ToArray();
        }

        private static List<T>[] DetectAdjacencies<T>(List<T> allElements, bool[,] adjacencies)
        {
            // find connected elements (from https://stackoverflow.com/questions/8124626/finding-connected-components-of-adjacency-matrix-graph)
            int count = allElements.Count;
            int[] marks = new int[count];
            int components = 0;
            var queue = new Queue<int>();
            for (int i = 0; i < count; i++)
            {
                if (marks[i] == 0)
                {
                    components++;
                    queue.Enqueue(i);
                    while (queue.Any())
                    {
                        var current = queue.Dequeue();
                        marks[current] = components;
                        for (int j = 0; j < count; j++)
                        {
                            if (adjacencies[current, j] && marks[j] == 0)
                                queue.Enqueue(j);
                        }
                    }
                }
            }

            // all with same component in marks belong to the same cluster
            var result = new List<T>[components];
            for (int i = 0; i < count; i++)
            {
                var current = marks[i] - 1;
                if (result[current] == null)
                {
                    result[current] = new List<T>() { allElements[i] };
                }
                else
                {
                    result[current].Add(allElements[i]);
                }
            }

            return result;
        }

        private static List<List<Vertex>> EdgeConnectedVertexGroups(List<Edge> edges)
        {
            List<List<Vertex>> result = new List<List<Vertex>>();

            if (edges == null || edges.Count == 0)
                return result;

            //Remove edges which connect the same vertex
            var localEdges = new List<Edge>(edges.Where(x => x.Vertices[0] != x.Vertices[1]));
            if (localEdges.Count == 0)
                return result;

            //Try to find vertex which is only contained in one edge
            var start = localEdges.SelectMany(x => x.Vertices).FirstOrDefault(x => x.Edges.Count(xe => localEdges.Contains(xe)) <= 1);

            if (start == null) // No end vertex -> closed edge ring -> choose any start
                start = localEdges[0].Vertices[0];

            while (localEdges.Count > 0)
            {
                List<Vertex> group = new List<Vertex> { start };
                bool edgeFound = true;

                while (edgeFound)
                {
                    var nextEdge = localEdges.FirstOrDefault(x => x.Vertices.Contains(start));

                    edgeFound = nextEdge != null;

                    if (edgeFound)
                    {
                        localEdges.Remove(nextEdge);
                        start = nextEdge.Vertices.First(x => x != start);
                        group.Add(start);
                    }
                }

                result.Add(group);

                if (localEdges.Count > 0)
                    start = localEdges.SelectMany(x => x.Vertices).First(x => x.Edges.Count(xe => localEdges.Contains(xe)) <= 1);
            }

            return result;
        }
    }
}
