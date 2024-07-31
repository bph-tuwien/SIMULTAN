using MathNet.Numerics.LinearAlgebra;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                Dictionary<(Face, Vertex, int), SimVector3D> offsetCalculationCache = new Dictionary<(Face, Vertex, int), SimVector3D>();

                HandleExteriorFaces(model, model.Faces, ref setbackFaces, offsetCalculationCache);
                HandleInteriorFaces(model, model.Volumes, offsetCalculationCache);

                HandleMissingFaces(model);

                HandleHoles(model.Faces, model, offsetCalculationCache);
                //HandleAdditionalSurfaces(model, setbackFaces, offsetCalculationCache);
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
            Dictionary<(Face, Vertex, int), SimVector3D> offsetCalculationCache = new Dictionary<(Face, Vertex, int), SimVector3D>();

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
                    HandleExteriorFaces(model, affectedFaces, ref setbackSurfaces, offsetCalculationCache);
                    var affectedVolumes = affectedFaces.SelectMany(x => x.PFaces.Select(pf => pf.Volume)).Distinct().ToList();
                    HandleInteriorFaces(model, affectedVolumes, offsetCalculationCache);

                    HandleMissingFaces(model);

                    HandleHoles(affectedFaces.Concat(affectedVolumes.SelectMany(x => x.Faces).Select(x => x.Face)).Distinct(), model, offsetCalculationCache);
                }
            }

            //watch.Stop();
            //Debug.WriteLine("Update for {0} faces: {1}", invalidatedGeometry.Count(x => x is Face), watch.ElapsedMilliseconds);

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
                    Debug.WriteLine("Both sides missing: Face {0}", face.Id);

                    model.OffsetModel.Faces[(face, GeometricOrientation.Forward)] = new OffsetFace(face, GeometricOrientation.Forward);
                    model.OffsetModel.Faces[(face, GeometricOrientation.Forward)].Boundary.AddRange(face.Boundary.Edges.Select(x => x.StartVertex.Position));

                    foreach (var hole in face.Holes)
                    {
                        model.OffsetModel.Faces[(face, GeometricOrientation.Forward)].Openings.Add(hole,
                            hole.Edges.Select(x => x.StartVertex.Position).ToList());
                    }
                    model.OffsetModel.Faces[(face, GeometricOrientation.Backward)] = new OffsetFace(face, GeometricOrientation.Backward);
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

        private static void HandleInteriorFaces(GeometryModelData model, IEnumerable<Volume> volumes, 
            Dictionary<(Face, Vertex, int), SimVector3D> offsetCalculationCache)
        {

            foreach (var currentVolume in volumes.Where(x => x.IsConsistentOriented))
            {
                var facesInVolume = currentVolume.Faces;

                foreach (var currentPFace in currentVolume.Faces)
                {
                    if (currentPFace.Face.Id == 66)
                        Debug.WriteLine("FACE");

                    int orientation = (int)currentPFace.Orientation;

                    var offsetFace = new OffsetFace(currentPFace.Face, currentPFace.Orientation);
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
                            //	Debug.WriteLine("Failed to solve offset-surface at Face {0}, Vertex {1}", face.Id, currentVertex.Id);

                            offsetFace.Boundary.Add(currentVertex.Position + offset);

                            var test = currentVertex.Position + offset;
                        }
                    }

                    if (currentPFace.Face.Boundary.Edges.Count != offsetFace.Boundary.Count)
                        Debug.WriteLine("Mismatch between edge count and boundary count");

                    //Add offset surface
                    model.OffsetModel.Faces[(currentPFace.Face, (GeometricOrientation)orientation)] = offsetFace;
                }
            }
        }


        //Exterior Faces
        private static void HandleExteriorFaces(GeometryModelData model,
            IEnumerable<Face> faces,
            ref List<(Face f1, int f1orient, Face f2, int f2orient)> setbackSurfaces,
            Dictionary<(Face, Vertex, int), SimVector3D> offsetCalculationCache)
        {
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
                    var offsetFace = new OffsetFace(face, (GeometricOrientation)orientation);
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
                                    fidata.hasCommonEdge && Math.Abs(SimVector3D.DotProduct(fi.Normal, face.Normal)) > 0.99) //Parallel
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
                        Debug.WriteLine("Exterior face: Mismatch between edge count and offsetsurface boundary count");

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

                var potentialEdges = currentFace.Boundary.Edges;
                //Find edges that contain v and are part of currentFace
                var currentFaceEdges = potentialEdges.Where(x => x.Edge.Vertices.Contains(sourceVertex) && x.Edge.PEdges.Count > 1);
                var currentOrient = result[currentFace].orientModifier;

                //Find adj faces that are not handled yet

                foreach (var currentPEdge in currentFaceEdges)
                {
                    var currentEdgeDirection = EdgeAlgorithms.Direction(currentPEdge);
                    var currentFaceDirection = DetectionAlgorithms.DirectionFromFaceAndEdge(currentFace, currentPEdge);

                    double rotationDirection = SimVector3D.DotProduct(
                                                SimVector3D.CrossProduct(currentEdgeDirection, currentFaceDirection),
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
                        var adjLoop = ((EdgeLoop)adjPEdge.Parent);
                        var adjFaces = adjLoop.Faces.Where(faceSelector);

                        foreach (var adjFace in adjFaces)
                        {
                            if (!result.ContainsKey(adjFace))
                            {
                                int adjFaceOrientation = (int)adjFace.Orientation;
                                //Check if adjFace is accessed through a hole, in this case we need to compare normals instead of face orientation
                                //When accessed through a hole, the rule is inverted since we aren't crossing into the hole face, but into the face containing
                                //the hole
                                if (adjFace.Boundary != adjLoop)
                                {
                                    var holeNormal = EdgeLoopAlgorithms.NormalCCW(adjLoop);
                                    if (SimVector3D.DotProduct(holeNormal, adjFace.Normal) > 0)
                                        adjFaceOrientation = -1; //Holes have the exact other winding order than the face
                                    else
                                        adjFaceOrientation = 1;
                                }

                                int orientModifier = 1;
                                if ((int)currentPEdge.Orientation * (int)currentFace.Orientation == (int)adjPEdge.Orientation * adjFaceOrientation)
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

        private static double AngleBetweenFaces(PEdge x, SimVector3D currentFaceDirection, SimVector3D currentEdgeDirection)
        {
            if (((EdgeLoop)x.Parent).Faces.Count == 0)
                return double.PositiveInfinity;

            var face = ((EdgeLoop)x.Parent).Faces.First();
            var dir = DetectionAlgorithms.DirectionFromFaceAndEdge(face, x);
            var angle = Math.Atan2(
                SimVector3D.DotProduct(SimVector3D.CrossProduct(currentFaceDirection, dir), currentEdgeDirection),
                SimVector3D.DotProduct(currentFaceDirection, dir));
            if (angle < 0)
                angle = 2.0 * Math.PI + angle; //+ because angle is negative
            return angle;
        }


        private static (SimVector3D offset, bool isValid) SolveOffset(
            Dictionary<Face, (int orientModifier, bool hasCommonEdge)> faces,
            Face sourceFace,
            Vertex sourceVertex,
            IOffsetQueryable offsetQuery,
            Dictionary<(Face, Vertex, int), SimVector3D> offsetVertexCache,
            bool useFacesWithoutCommonEdge)
        {
            //Find all directions/offsets in the system
            List<(SimVector3D dir, double off, Face face)> availableDirections = new List<(SimVector3D dir, double off, Face face)>();

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
            List<(SimVector3D dir, double off)> directions = new List<(SimVector3D dir, double off)>();

            //Add sourceface to ensure that it gets used
            directions.Add(
                (sourceFace.Normal * faces[sourceFace].orientModifier, GetOffsetFromDir(offsetQuery.GetFaceOffset(sourceFace), faces[sourceFace].orientModifier))
                );

            int i = 0;
            //Second vector
            for (; i < availableDirections.Count; i++)
            {
                if (Math.Abs(SimVector3D.DotProduct(directions[0].dir, availableDirections[i].dir)) < 0.99)
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
                if (SimVector3D.CrossProduct(
                        SimVector3D.CrossProduct(directions[0].dir, availableDirections[i].dir),
                        SimVector3D.CrossProduct(directions[1].dir, availableDirections[i].dir)
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
                directions.Add((SimVector3D.CrossProduct(directions[0].dir, directions[1].dir), 0));
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
                var xvec = new SimVector3D(x[0], x[1], x[2]);
                bool isValid = !x.Any(xval => double.IsInfinity(xval) || double.IsNaN(xval));

                if (isValid)
                {
                    //Evaluate solution
                    foreach (var f in availableDirections)
                    {
                        if (Math.Abs(SimVector3D.DotProduct(f.dir, xvec) - f.off) > 0.001)
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

        private static void HandleHoles(IEnumerable<Face> faces, GeometryModelData model,
            Dictionary<(Face, Vertex, int), SimVector3D> offsetCalculationCache)
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
                            List<SimPoint3D> holeLoop = new List<SimPoint3D>();
                            foreach (var edge in faceOpening.Edges)
                            {
                                if (offsetCalculationCache.TryGetValue((face, edge.StartVertex, i), out var offset))
                                    holeLoop.Add(edge.StartVertex.Position + offset);
                                else
                                {
                                    offsetCalculationCache.Add((face, edge.StartVertex, i), dir);
                                    holeLoop.Add(edge.StartVertex.Position + dir);
                                }
                            }

                            faceEntry.Openings.Add(faceOpening, holeLoop);
                        }
                    }
                }
            }
        }

        private static bool IsJumpFace(Face f1, double f1offset, Face f2, double f2offset)
        {
            if (Math.Abs(SimVector3D.DotProduct(f1.Normal, f2.Normal)) > 0.99)
            {
                return Math.Abs(f1offset - f2offset) > 0.001;
            }
            return false;
        }
    }
}