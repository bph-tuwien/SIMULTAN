using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Exception when GeometryModel is inconsistent
    /// </summary>
    public class ModelInconsistentException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="msg">Exception message</param>
        public ModelInconsistentException(string msg) : base(msg) { }
    }


    /// <summary>
    /// Provides algorithms that operate on the whole GeometryModel
    /// </summary>
    public static class GeometryModelAlgorithms
    {
        /// <summary>
        /// Sets the visibility of all contained elements
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="isVisible">The new visibility value</param>
        public static void SetAllVisibility(GeometryModelData model, bool isVisible)
        {
            model.StartBatchOperation();

            foreach (var layer in model.Layers)
                SetLayerVisibility(layer, isVisible);

            model.IsVisible = isVisible;

            model.Vertices.ForEach(x => x.IsVisible = isVisible);
            model.Edges.ForEach(x => x.IsVisible = isVisible);
            model.EdgeLoops.ForEach(x => x.IsVisible = isVisible);
            model.Faces.ForEach(x => x.IsVisible = isVisible);
            model.Volumes.ForEach(x => x.IsVisible = isVisible);

            model.EndBatchOperation();
        }
        private static void SetLayerVisibility(Layer layer, bool isVisible)
        {
            layer.IsVisible = isVisible;
            foreach (var l in layer.Layers)
                SetLayerVisibility(l, isVisible);
        }

        /// <summary>
        /// Returns all geometries that are either a parent or a child of the initialGeometry
        /// </summary>
        /// <param name="initialGeometry">The initial geometry</param>
        /// <returns>A list of all geometries that are affected by change to the initial geometry</returns>
        public static List<BaseGeometry> GetAllAffectedGeometries(IEnumerable<BaseGeometry> initialGeometry)
        {
            //First go down the tree and add all children
            List<BaseGeometry> finalGeometry = new List<BaseGeometry>(initialGeometry);
            foreach (var g in initialGeometry)
            {
                if (g is Edge)
                    finalGeometry.AddRange(((Edge)g).Vertices);
                else if (g is EdgeLoop)
                    finalGeometry.AddRange(((EdgeLoop)g).Edges.Select(x => x.Edge));
                else if (g is Face)
                {
                    finalGeometry.Add(((Face)g).Boundary);
                    finalGeometry.AddRange(((Face)g).Holes);
                }
                else if (g is Volume)
                {
                    finalGeometry.AddRange(((Volume)g).Faces.Select(x => x.Face));
                }
            }

            finalGeometry = finalGeometry.Distinct().ToList();

            for (int i = 0; i < finalGeometry.Count; ++i)
            {
                var g = finalGeometry[i];
                if (g is Vertex)
                    finalGeometry.AddRange(((Vertex)g).Edges);
                else if (g is Edge)
                    finalGeometry.AddRange(((Edge)g).PEdges.Select(x => x.Parent));
                else if (g is EdgeLoop)
                    finalGeometry.AddRange(((EdgeLoop)g).Faces);
                else if (g is Face)
                    finalGeometry.AddRange(((Face)g).PFaces.Select(x => x.Volume));
            }

            return finalGeometry.Distinct().ToList();
        }

        /// <summary>
        /// Returns all geometries that are either a parent or a child of the initialGeometry
        /// </summary>
        /// <param name="initialGeometry">The initial geometry</param>
        /// <returns>A list of all geometries that are affected by change to the initial geometry</returns>
        public static List<BaseGeometry> GetAllAffectedGeometries(IEnumerable<Vertex> initialGeometry)
        {
            HashSet<BaseGeometry> result = new HashSet<BaseGeometry>();

            foreach (var v in initialGeometry)
            {
                if (!result.Contains(v))
                {
                    result.Add(v);

                    foreach (var e in v.Edges)
                    {
                        if (!result.Contains(e))
                        {
                            result.Add(e);

                            foreach (var pe in e.PEdges)
                            {
                                if (!result.Contains(pe.Parent))
                                {
                                    result.Add(pe.Parent);

                                    if (pe.Parent is EdgeLoop)
                                    {
                                        foreach (var f in ((EdgeLoop)pe.Parent).Faces)
                                        {
                                            if (!result.Contains(f))
                                            {
                                                result.Add(f);

                                                foreach (var pf in f.PFaces)
                                                {
                                                    if (!result.Contains(pf.Volume))
                                                        result.Add(pf.Volume);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return result.ToList();
        }

        /// <summary>
        /// Gets a list of all geometries that depend on the initialGeometry
        /// </summary>
        /// <param name="initialGeometry">The initial geometry</param>
        /// <returns>A list of all "parent" geometries (geometries that depend on the initial geometry)</returns>
        public static List<BaseGeometry> GetAllContainingGeometries(IEnumerable<BaseGeometry> initialGeometry)
        {
            List<BaseGeometry> finalGeometry = new List<BaseGeometry>(initialGeometry);

            for (int i = 0; i < finalGeometry.Count; ++i)
            {
                var g = finalGeometry[i];

                if (g is Vertex)
                {
                    finalGeometry.AddRange(((Vertex)g).Edges.Where(x => !finalGeometry.Contains(x)));
                    finalGeometry.AddRange(((Vertex)g).ModelGeometry.ProxyGeometries.Where(x => x.Vertex == (Vertex)g));
                }
                else if (g is Edge)
                    finalGeometry.AddRange(((Edge)g).PEdges.Where(x => !finalGeometry.Contains(x.Parent)).Select(x => x.Parent));
                else if (g is EdgeLoop)
                    finalGeometry.AddRange(((EdgeLoop)g).Faces.Where(x => x.Boundary == g && !finalGeometry.Contains(x)));
                else if (g is Face)
                {
                    var f = (Face)g;
                    finalGeometry.AddRange(((Face)g).PFaces.Where(x => !finalGeometry.Contains(x.Volume)).Select(x => x.Volume));
                    if (!finalGeometry.Contains(f.Boundary))
                        finalGeometry.Add(f.Boundary);
                }
            }

            return finalGeometry.Distinct().ToList();


        }

        /// <summary>
        /// Returns a list of all visible geometries from a model (only Vertices, Edges, Faces and Volumes)
        /// </summary>
        /// <param name="model">The geometry model</param>
        /// <returns>A list of all visible geometries from a model (only Vertices, Edges, Faces and Volumes)</returns>
        public static IEnumerable<BaseGeometry> GetAllVisibleGeometries(GeometryModelData model)
        {
            List<BaseGeometry> result = new List<BaseGeometry>();

            result.AddRange(model.Vertices.Where(x => x.IsActuallyVisible));
            result.AddRange(model.Edges.Where(x => x.IsActuallyVisible));
            result.AddRange(model.Faces.Where(x => x.IsActuallyVisible));
            result.AddRange(model.Volumes.Where(x => x.IsActuallyVisible));

            return result;
        }

        /// <summary>
        /// Calculates a 3D axis aligned bound box around the models
        /// </summary>
        /// <param name="models">List of geometry models that should be contained in the bounding box</param>
        /// <param name="includeInvisible">Set to true when currently invisible geometry should be included</param>
        /// <returns></returns>
        public static Rect3D Boundary(IEnumerable<GeometryModelData> models, bool includeInvisible)
        {
            Point3D min = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            Point3D max = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

            foreach (var model in models)
            {
                model.Vertices.Where(x => includeInvisible || x.IsActuallyVisible).ForEach(x => BoundaryMinMax(x, ref min, ref max));
                model.Edges.Where(x => includeInvisible || x.IsActuallyVisible).SelectMany(x => x.Vertices)
                    .ForEach(x => BoundaryMinMax(x, ref min, ref max));
                model.Faces.Where(x => includeInvisible || x.IsActuallyVisible).SelectMany(x => BaseGeometryAlgorithms.GetVertices(x))
                    .ForEach(x => BoundaryMinMax(x, ref min, ref max));
                model.Volumes.Where(x => includeInvisible || x.IsActuallyVisible).SelectMany(x => BaseGeometryAlgorithms.GetVertices(x))
                    .ForEach(x => BoundaryMinMax(x, ref min, ref max));
            }

            return new Rect3D(min, (Size3D)(max - min));
        }

        private static void BoundaryMinMax(Vertex v, ref Point3D min, ref Point3D max)
        {
            min.X = Math.Min(min.X, v.Position.X);
            min.Y = Math.Min(min.Y, v.Position.Y);
            min.Z = Math.Min(min.Z, v.Position.Z);

            max.X = Math.Max(max.X, v.Position.X);
            max.Y = Math.Max(max.Y, v.Position.Y);
            max.Z = Math.Max(max.Z, v.Position.Z);
        }

        /// <summary>
        /// Calculates a 3D axis aligned bound box around some geometry objects
        /// </summary>
        /// <param name="geometry">List of geometry objects that should be contained in the bounding box</param>
        /// <param name="includeInvisible">Set to true when currently invisible geometry should be included</param>
        /// <returns></returns>
        public static Rect3D Boundary(IEnumerable<BaseGeometry> geometry, bool includeInvisible)
        {
            Point3D min = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            Point3D max = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

            foreach (var geom in geometry.Where(x => includeInvisible || x.IsActuallyVisible))
            {
                foreach (var v in BaseGeometryAlgorithms.GetVertices(geom))
                {
                    BoundaryMinMax(v, ref min, ref max);
                }
            }

            return new Rect3D(min, (Size3D)(max - min));
        }

        /// <summary>
        /// Checks if the model is in a consistent state. Tests whether all sub-geometries are also part of the model.
        /// Throws a ModelInconsistentException when not in a consistent state.
        /// </summary>
        /// <param name="model">The model to check</param>
        public static void CheckConsistency(GeometryModelData model)
        {
            foreach (var layer in model.Layers)
                CheckLayerConsistency(layer, model);

            //Check if all faces of a volume are contained in the model
            foreach (var volume in model.Volumes)
            {
                foreach (var pface in volume.Faces)
                    if (!model.Faces.Contains(pface.Face))
                        throw new ModelInconsistentException(string.Format("Face {0} (Id={1}) not contained in model but used in Volume {2} (Id={3})",
                            pface.Face.Name, pface.Face.Id, volume.Name, volume.Id
                            ));

                if (!volume.Layer.Elements.Contains(volume))
                    throw new ModelInconsistentException(string.Format("Volume {0} (Id={1}) not present on it's Layer {2} (Id={3})",
                            volume.Name, volume.Id, volume.Layer.Name, volume.Layer.Id
                            ));
            }

            //Check if all loops of a face are contained in model
            foreach (var face in model.Faces)
            {
                if (!model.EdgeLoops.Contains(face.Boundary))
                    throw new ModelInconsistentException(string.Format("EdgeLoop {0} (Id={1}) not contained in model but used as boundary in Face {2} (Id={3})",
                            face.Boundary.Name, face.Boundary.Id, face.Name, face.Id
                            ));

                foreach (var loop in face.Holes)
                    if (!model.EdgeLoops.Contains(loop))
                        throw new ModelInconsistentException(string.Format("EdgeLoop {0} (Id={1}) not contained in model but used as hole in Face {2} (Id={3})",
                                loop.Name, loop.Id, face.Name, face.Id
                                ));

                if (!face.Layer.Elements.Contains(face))
                    throw new ModelInconsistentException(string.Format("Face {0} (Id={1}) not present on it's Layer {2} (Id={3})",
                            face.Name, face.Id, face.Layer.Name, face.Layer.Id
                            ));
            }

            //Check if all edges of loop are contained in model
            foreach (var loop in model.EdgeLoops)
            {
                foreach (var pedge in loop.Edges)
                    if (!model.Edges.Contains(pedge.Edge))
                        throw new ModelInconsistentException(string.Format("Edge {0} (Id={1}) not contained in model but used in EdgeLoop {2} (Id={3})",
                                pedge.Edge.Name, pedge.Edge.Id, loop.Name, loop.Id
                                ));

                if (!loop.Layer.Elements.Contains(loop))
                    throw new ModelInconsistentException(string.Format("EdgeLoop {0} (Id={1}) not present on it's Layer {2} (Id={3})",
                            loop.Name, loop.Id, loop.Layer.Name, loop.Layer.Id
                            ));
            }

            //Check if all edges of polylines are contained in model
            foreach (var loop in model.Polylines)
            {
                foreach (var pedge in loop.Edges)
                    if (!model.Edges.Contains(pedge.Edge))
                        throw new ModelInconsistentException(string.Format("Edge {0} (Id={1}) not contained in model but used in Polyline {2} (Id={3})",
                                pedge.Edge.Name, pedge.Edge.Id, loop.Name, loop.Id
                                ));

                if (!loop.Layer.Elements.Contains(loop))
                    throw new ModelInconsistentException(string.Format("Polyline {0} (Id={1}) not present on it's Layer {2} (Id={3})",
                            loop.Name, loop.Id, loop.Layer.Name, loop.Layer.Id
                            ));
            }

            //Check if all vertices of edges are contained in model
            foreach (var edge in model.Edges)
            {
                foreach (var vertex in edge.Vertices)
                    if (!model.Vertices.Contains(vertex))
                        throw new ModelInconsistentException(string.Format("Vertex {0} (Id={1}) not contained in model but used in Edge {2} (Id={3})",
                                    vertex.Name, vertex.Id, edge.Name, edge.Id
                                    ));

                if (!edge.Layer.Elements.Contains(edge))
                    throw new ModelInconsistentException(string.Format("Edge {0} (Id={1}) not present on it's Layer {2} (Id={3})",
                            edge.Name, edge.Id, edge.Layer.Name, edge.Layer.Id
                            ));
            }

            //Check vertices
            foreach (var vertex in model.Vertices)
            {
                if (!vertex.Layer.Elements.Contains(vertex))
                    throw new ModelInconsistentException(string.Format("Vertex {0} (Id={1}) not present on it's Layer {2} (Id={3})",
                            vertex.Name, vertex.Id, vertex.Layer.Name, vertex.Layer.Id
                            ));
            }
        }

        private static void CheckLayerConsistency(Layer layer, GeometryModelData model)
        {
            if (layer.Parent != null)
            {
                if (!layer.Parent.Layers.Contains(layer))
                {
                    throw new ModelInconsistentException(string.Format("Layer {0} (Id={1}) not present in it's parent layer {2} (Id={3})",
                            layer.Name, layer.Id, layer.Parent.Name, layer.Parent.Id
                            ));
                }
            }

            foreach (var geometry in layer.Elements)
            {
                if (!model.ContainsGeometry(geometry))
                    throw new ModelInconsistentException(string.Format("Geometry {0} (Id={1}, Type={4}), referenced by Layer {2} (Id={3}) not present in GeometryModel",
                            geometry.Name, geometry.Id, layer.Name, layer.Id, geometry.GetType().Name
                            ));
            }

            foreach (var sublayer in layer.Layers)
            {
                CheckLayerConsistency(sublayer, model);
            }
        }

        /// <summary>
        /// Computes the union of 2 geometry models to sustain existing IDs and adds everything else from newGeometryModel 
        /// (returns union(ogm, ngm) + diff(ngm, union(ogm, ngm)))
        /// </summary>
        /// <param name="oldGeometryModel">Old model which gets updated with new geometry from newGeometryModel</param>
        /// <param name="newGeometryModel">GeometryModel with new changes which are copied to oldGeometryModel</param>
        /// <param name="outGeometryModel">The GeometryModel to which the data is added</param>
        public static void UpdateGeometryModelWithExistingIds(GeometryModelData oldGeometryModel, GeometryModelData newGeometryModel, GeometryModelData outGeometryModel)
        {
            if (oldGeometryModel.Polylines.Count > 0 || newGeometryModel.Polylines.Count > 0)
            {
                throw new ArgumentException("The models may not contain polylines");
            }

            outGeometryModel.StartBatchOperation();

            // copy new layer hierarchy
            Dictionary<Layer, Layer> layerNewToOut = new Dictionary<Layer, Layer>();
            foreach (var layer in newGeometryModel.Layers)
            {
                outGeometryModel.Layers.Add(CopyLayerFromTo(layer, layerNewToOut, outGeometryModel, true));
            }

            // generate union of geometry models
            Dictionary<BaseGeometry, BaseGeometry> mapNewToOut = new Dictionary<BaseGeometry, BaseGeometry>();
            Dictionary<BaseGeometry, BaseGeometry> mapOldToOut = new Dictionary<BaseGeometry, BaseGeometry>();
            Dictionary<BaseGeometry, BaseGeometry> mapNewToOld = new Dictionary<BaseGeometry, BaseGeometry>();

            UnionVertices(oldGeometryModel.Vertices, newGeometryModel.Vertices, layerNewToOut, mapNewToOut, mapOldToOut, mapNewToOld);
            UnionEdges(oldGeometryModel.Edges, newGeometryModel.Edges, layerNewToOut, mapNewToOut, mapOldToOut, mapNewToOld);
            UnionEdgeLoops(oldGeometryModel.EdgeLoops, newGeometryModel.EdgeLoops, layerNewToOut, mapNewToOut, mapOldToOut, mapNewToOld);
            UnionFaces(oldGeometryModel.Faces, newGeometryModel.Faces, layerNewToOut, mapNewToOut, mapOldToOut, mapNewToOld);
            UnionVolumes(oldGeometryModel.Volumes, newGeometryModel.Volumes, layerNewToOut, mapNewToOut, mapOldToOut, mapNewToOld);

            // add geometry from new model
            foreach (var vert in newGeometryModel.Vertices)
            {
                if (!mapNewToOut.ContainsKey(vert)) // new geometry
                {
                    CopyVertexFromTo(vert, layerNewToOut, mapNewToOut, false);
                }
            }

            foreach (var edge in newGeometryModel.Edges)
            {
                if (!mapNewToOut.ContainsKey(edge)) // new geometry
                {
                    CopyEdgeFromTo(edge, layerNewToOut, mapNewToOut, false);
                }
            }

            foreach (var el in newGeometryModel.EdgeLoops)
            {
                if (!mapNewToOut.ContainsKey(el)) // new geometry
                {
                    CopyEdgeLoopFromTo(el, layerNewToOut, mapNewToOut, false);
                }
            }

            foreach (var f in newGeometryModel.Faces)
            {
                if (!mapNewToOut.ContainsKey(f)) // new geometry
                {
                    CopyFaceFromTo(f, layerNewToOut, mapNewToOut, false);
                }
            }

            foreach (var volumes in newGeometryModel.Volumes)
            {
                if (!mapNewToOut.ContainsKey(volumes)) // new geometry
                {
                    CopyVolumeFromTo(volumes, layerNewToOut, mapNewToOut, false);
                }
            }

            outGeometryModel.EndBatchOperation();
        }

        private static void UnionVertices(IEnumerable<Vertex> oldVertices, IEnumerable<Vertex> newVertices,
                                            Dictionary<Layer, Layer> layerFromNewToOutMapping,
                                            Dictionary<BaseGeometry, BaseGeometry> mapNewToOut,
                                            Dictionary<BaseGeometry, BaseGeometry> mapOldToOut,
                                            Dictionary<BaseGeometry, BaseGeometry> mapNewToOld)
        {
            foreach (var newVertex in newVertices)
            {
                var existingVertex = oldVertices.FirstOrDefault(x => x.Position == newVertex.Position);
                if (existingVertex != null)
                {
                    Vertex v = CopyVertexFromTo(existingVertex, layerFromNewToOutMapping[newVertex.Layer], mapOldToOut, true);
                    mapNewToOld[newVertex] = existingVertex;
                    mapNewToOut[newVertex] = v;
                }
            }
        }

        private static void UnionEdges(IEnumerable<Edge> oldEdges, IEnumerable<Edge> newEdges,
                                        Dictionary<Layer, Layer> layerFromNewToOutMapping,
                                        Dictionary<BaseGeometry, BaseGeometry> mapNewToOut,
                                        Dictionary<BaseGeometry, BaseGeometry> mapOldToOut,
                                        Dictionary<BaseGeometry, BaseGeometry> mapNewToOld)
        {
            foreach (var newEdge in newEdges)
            {
                if (mapNewToOld.ContainsKey(newEdge.Vertices[0]) && mapNewToOld.ContainsKey(newEdge.Vertices[1]))
                {
                    Vertex oldStartVertex = (Vertex)mapNewToOld[newEdge.Vertices[0]];
                    Vertex oldEndVertex = (Vertex)mapNewToOld[newEdge.Vertices[1]];
                    var existingEdge = oldStartVertex.Edges.FirstOrDefault(e => e.Vertices.Any(v => v.Id == oldEndVertex.Id));
                    if (existingEdge != null)
                    {
                        var e = CopyEdgeFromTo(existingEdge, layerFromNewToOutMapping[newEdge.Layer], mapOldToOut, true);
                        mapNewToOld[newEdge] = existingEdge;
                        mapNewToOut[newEdge] = e;
                    }
                }
            }
        }

        private static void UnionEdgeLoops(IEnumerable<EdgeLoop> oldEdgeLoops, IEnumerable<EdgeLoop> newEdgeLoops,
                                            Dictionary<Layer, Layer> layerFromNewToOutMapping,
                                            Dictionary<BaseGeometry, BaseGeometry> mapNewToOut,
                                            Dictionary<BaseGeometry, BaseGeometry> mapOldToOut,
                                            Dictionary<BaseGeometry, BaseGeometry> mapNewToOld)
        {
            foreach (var newEdgeLoop in newEdgeLoops)
            {
                EdgeLoop existingEdgeLoop = null;

                bool newEdgePresent = false;
                foreach (var e in newEdgeLoop.Edges)
                {
                    if (!mapNewToOld.ContainsKey(e.Edge))
                    {
                        newEdgePresent = true;
                        break;
                    }
                }

                if (!newEdgePresent)
                {
                    var oldStartEdge = (Edge)mapNewToOld[newEdgeLoop.Edges[0].Edge];
                    existingEdgeLoop = (EdgeLoop)oldStartEdge.PEdges.FirstOrDefault(
                                        pe => pe.Parent.Edges.Count == newEdgeLoop.Edges.Count && pe.Parent.Edges.All(
                                            e => newEdgeLoop.Edges.Any(
                                                ne => mapNewToOld[ne.Edge].Id == e.Edge.Id)))?.Parent;

                    if (existingEdgeLoop != null)
                    {
                        var el = CopyEdgeLoopFromTo(existingEdgeLoop, layerFromNewToOutMapping[newEdgeLoop.Layer], mapOldToOut, true);
                        mapNewToOld[newEdgeLoop] = existingEdgeLoop;
                        mapNewToOut[newEdgeLoop] = el;
                    }
                }
            }
        }

        private static void UnionFaces(IEnumerable<Face> oldFaces, IEnumerable<Face> newFaces,
                                        Dictionary<Layer, Layer> layerFromNewToOutMapping,
                                        Dictionary<BaseGeometry, BaseGeometry> mapNewToOut,
                                        Dictionary<BaseGeometry, BaseGeometry> mapOldToOut,
                                        Dictionary<BaseGeometry, BaseGeometry> mapNewToOld)
        {
            foreach (var newFace in newFaces)
            {
                if (mapNewToOld.ContainsKey(newFace.Boundary))
                {
                    var oldEdgeLoop = (EdgeLoop)mapNewToOld[newFace.Boundary];
                    var existingFace = oldEdgeLoop.Faces.FirstOrDefault(f => f.Boundary.Edges.Count == newFace.Boundary.Edges.Count && f.Boundary.Id == mapNewToOld[newFace.Boundary].Id);
                    if (existingFace != null)
                    {
                        var f = CopyFaceFromTo(existingFace, layerFromNewToOutMapping[newFace.Layer], mapOldToOut, true);
                        mapNewToOld[newFace] = existingFace;
                        mapNewToOut[newFace] = f;
                    }
                }
            }
        }

        private static void UnionVolumes(IEnumerable<Volume> oldVolumes, IEnumerable<Volume> newVolumes,
                                        Dictionary<Layer, Layer> layerFromNewToOutMapping,
                                        Dictionary<BaseGeometry, BaseGeometry> mapNewToOut,
                                        Dictionary<BaseGeometry, BaseGeometry> mapOldToOut,
                                        Dictionary<BaseGeometry, BaseGeometry> mapNewToOld)
        {
            foreach (var newVolume in newVolumes)
            {
                Volume existingVolume = null;

                bool newFacePresent = false;
                foreach (var f in newVolume.Faces)
                {
                    if (!mapNewToOld.ContainsKey(f.Face))
                    {
                        newFacePresent = true;
                        break;
                    }
                }

                if (!newFacePresent)
                {
                    var oldStartFace = (Face)mapNewToOld[newVolume.Faces[0].Face];
                    existingVolume = oldStartFace.PFaces.FirstOrDefault(
                                            pf => pf.Volume.Faces.Count == newVolume.Faces.Count && pf.Volume.Faces.All(
                                                f => newVolume.Faces.Any(
                                                    nf => mapNewToOld[nf.Face].Id == f.Face.Id)))?.Volume;

                    if (existingVolume != null)
                    {
                        var vol = CopyVolumeFromTo(existingVolume, layerFromNewToOutMapping[newVolume.Layer], mapOldToOut, true);
                        mapNewToOld[newVolume] = existingVolume;
                        mapNewToOut[newVolume] = vol;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a deep copy of the from model in the to model
        /// </summary>
        /// <param name="from">The source model</param>
        /// <param name="to">The target model</param>
        /// <param name="preserveIds">When set to True, the ids are kept. Only works when the ids are not used in the to model.</param>
        public static void CopyContent(GeometryModelData from, GeometryModelData to, bool preserveIds = false)
        {
            to.StartBatchOperation();

            Dictionary<Layer, Layer> layerLookup = new Dictionary<Layer, Layer>();
            foreach (var l in from.Layers)
                to.Layers.Add(CopyLayerFromTo(l, layerLookup, to, preserveIds));

            //Stores old as key and new as value
            Dictionary<BaseGeometry, BaseGeometry> geometryLookup = new Dictionary<BaseGeometry, BaseGeometry>();
            foreach (var v in from.Vertices)
                CopyVertexFromTo(v, layerLookup, geometryLookup, preserveIds);
            foreach (var e in from.Edges)
                CopyEdgeFromTo(e, layerLookup, geometryLookup, preserveIds);
            foreach (var el in from.EdgeLoops)
                CopyEdgeLoopFromTo(el, layerLookup, geometryLookup, preserveIds);
            foreach (var el in from.Polylines)
                CopyPolylineFromTo(el, layerLookup, geometryLookup, preserveIds);
            foreach (var f in from.Faces)
                CopyFaceFromTo(f, layerLookup, geometryLookup, preserveIds);
            foreach (var v in from.Volumes)
                CopyVolumeFromTo(v, layerLookup, geometryLookup, preserveIds);
            foreach (var p in from.ProxyGeometries)
                CopyProxyFromTo(p, layerLookup, geometryLookup, preserveIds);

            foreach (var g in from.GeoReferences)
                CopyGeoReference(g, to, layerLookup, geometryLookup, preserveIds);

            //Restore parents
            foreach (var copiedGeometry in geometryLookup)
            {
                if (copiedGeometry.Key.Parent != null)
                {
                    //References to other models are just copied
                    //References inside the same model are set to the copied target
                    var reference = copiedGeometry.Key.Parent;
                    copiedGeometry.Value.Parent = new GeometryReference(reference.ModelID, reference.GeometryID, reference.Name, null, reference.ModelStore);
                }
            }

            to.EndBatchOperation();
        }


        private static void CopyBaseGeometryProperties(BaseGeometry target, BaseGeometry source, Layer targetLayer)
        {
            target.IsVisible = source.IsVisible;
            target.Color = CopyColorFromTo(source.Color, targetLayer, target.ModelGeometry);
        }

        private static ulong GetCopyId(BaseGeometry source, Layer targetLayer, bool preserveIds)
        {
            if (preserveIds)
                return source.Id;
            else
                return targetLayer.Model.GetFreeId();
        }

        private static Layer CopyLayerFromTo(Layer source, Dictionary<Layer, Layer> layers, GeometryModelData to, bool preserveIds)
        {
            if (source == null)
                return null;
            if (layers.ContainsKey(source))
                return layers[source];

            var parentLayer = CopyLayerFromTo(source.Parent, layers, to, preserveIds);

            var id = source.Id;
            if (!preserveIds)
                id = to.GetFreeId(false);

            Layer newLayer = new Layer(id, to, source.Name);
            newLayer.Color = CopyColorFromTo(source.Color, parentLayer, newLayer.Model);
            newLayer.IsVisible = source.IsVisible;
            layers.Add(source, newLayer);

            foreach (var childLayer in source.Layers)
                newLayer.Layers.Add(CopyLayerFromTo(childLayer, layers, to, preserveIds));

            return newLayer;
        }

        private static Vertex CopyVertexFromTo(Vertex source, Dictionary<Layer, Layer> layers, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            return CopyVertexFromTo(source, layers[source.Layer], copiedGeometries, preserveIds);
        }

        private static Vertex CopyVertexFromTo(Vertex source, Layer targetLayer, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries, bool preserveIds)
        {
            if (copiedGeometries.ContainsKey(source))
                return copiedGeometries[source] as Vertex;

            var id = GetCopyId(source, targetLayer, preserveIds);

            var v = new Vertex(id, targetLayer, source.Name, source.Position);
            CopyBaseGeometryProperties(v, source, targetLayer);
            copiedGeometries.Add(source, v);

            return v;
        }

        private static Edge CopyEdgeFromTo(Edge source, Dictionary<Layer, Layer> layers, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
        , bool preserveIds)
        {
            return CopyEdgeFromTo(source, layers[source.Layer], copiedGeometries, preserveIds);
        }

        private static Edge CopyEdgeFromTo(Edge source, Layer targetLayer, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
        , bool preserveIds)
        {
            if (copiedGeometries.ContainsKey(source))
                return copiedGeometries[source] as Edge;

            var v1 = CopyVertexFromTo(source.Vertices[0], targetLayer, copiedGeometries, preserveIds);
            var v2 = CopyVertexFromTo(source.Vertices[1], targetLayer, copiedGeometries, preserveIds);

            var id = GetCopyId(source, targetLayer, preserveIds);

            Edge result = new Edge(id, targetLayer, source.Name, new Vertex[] { v1, v2 });
            CopyBaseGeometryProperties(result, source, targetLayer);
            copiedGeometries.Add(source, result);

            return result;
        }

        private static EdgeLoop CopyEdgeLoopFromTo(EdgeLoop source, Dictionary<Layer, Layer> layers, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            return CopyEdgeLoopFromTo(source, layers[source.Layer], copiedGeometries, preserveIds);
        }

        private static EdgeLoop CopyEdgeLoopFromTo(EdgeLoop source, Layer targetLayer, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            if (copiedGeometries.ContainsKey(source))
                return copiedGeometries[source] as EdgeLoop;

            List<Edge> edges = new List<Edge>();
            foreach (var e in source.Edges)
            {
                var ce = CopyEdgeFromTo(e.Edge, targetLayer, copiedGeometries, preserveIds);
                edges.Add(ce);
            }

            var id = GetCopyId(source, targetLayer, preserveIds);
            var loop = new EdgeLoop(id, targetLayer, source.Name, edges);
            CopyBaseGeometryProperties(loop, source, targetLayer);
            copiedGeometries.Add(source, loop);

            return loop;
        }
        private static Polyline CopyPolylineFromTo(Polyline source, Dictionary<Layer, Layer> layers, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            return CopyPolylineFromTo(source, layers[source.Layer], copiedGeometries, preserveIds);
        }

        private static Polyline CopyPolylineFromTo(Polyline source, Layer targetLayer, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            if (copiedGeometries.ContainsKey(source))
                return (Polyline)copiedGeometries[source];

            List<Edge> edges = new List<Edge>();
            foreach (var e in source.Edges)
            {
                var ce = CopyEdgeFromTo(e.Edge, targetLayer, copiedGeometries, preserveIds);
                edges.Add(ce);
            }

            var id = GetCopyId(source, targetLayer, preserveIds);
            var polyline = new Polyline(id, targetLayer, source.Name, edges);
            CopyBaseGeometryProperties(polyline, source, targetLayer);
            copiedGeometries.Add(source, polyline);

            return polyline;
        }

        private static Face CopyFaceFromTo(Face source, Dictionary<Layer, Layer> layers, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            return CopyFaceFromTo(source, layers[source.Layer], copiedGeometries, preserveIds);
        }

        private static Face CopyFaceFromTo(Face source, Layer targetLayer, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            if (copiedGeometries.ContainsKey(source))
                return copiedGeometries[source] as Face;

            var boundary = CopyEdgeLoopFromTo(source.Boundary, targetLayer, copiedGeometries, preserveIds);

            List<EdgeLoop> holes = new List<EdgeLoop>();
            foreach (var h in source.Holes)
            {
                var loop = CopyEdgeLoopFromTo(h, targetLayer, copiedGeometries, preserveIds);
                holes.Add(loop);
            }

            var id = GetCopyId(source, targetLayer, preserveIds);
            Face face = new Face(id, targetLayer, source.Name, boundary, source.Orientation, holes);
            CopyBaseGeometryProperties(face, source, targetLayer);
            copiedGeometries.Add(source, face);

            return face;

        }

        private static Volume CopyVolumeFromTo(Volume source, Dictionary<Layer, Layer> layers, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            return CopyVolumeFromTo(source, layers[source.Layer], copiedGeometries, preserveIds);
        }

        private static Volume CopyVolumeFromTo(Volume source, Layer targetLayer, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            if (copiedGeometries.ContainsKey(source))
                return copiedGeometries[source] as Volume;

            List<Face> faces = new List<Face>();
            foreach (var f in source.Faces)
            {
                var cf = CopyFaceFromTo(f.Face, targetLayer, copiedGeometries, preserveIds);
                faces.Add(cf);
            }

            var id = GetCopyId(source, targetLayer, preserveIds);
            Volume volume = new Volume(id, targetLayer, source.Name, faces);
            CopyBaseGeometryProperties(volume, source, targetLayer);
            copiedGeometries.Add(source, volume);

            return volume;
        }

        private static ProxyGeometry CopyProxyFromTo(ProxyGeometry source, Dictionary<Layer, Layer> layers, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            return CopyProxyFromTo(source, layers[source.Layer], copiedGeometries, preserveIds);
        }

        private static ProxyGeometry CopyProxyFromTo(ProxyGeometry source, Layer targetLayer, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            if (copiedGeometries.ContainsKey(source))
                return (ProxyGeometry)copiedGeometries[source];

            var v = CopyVertexFromTo(source.Vertex, targetLayer, copiedGeometries, preserveIds);

            var id = GetCopyId(source, targetLayer, preserveIds);
            ProxyGeometry proxy = new ProxyGeometry(id, targetLayer, source.Name, v);
            CopyBaseGeometryProperties(proxy, source, targetLayer);

            proxy.Positions = source.Positions.ToList();
            proxy.Normals = source.Normals.ToList();
            proxy.Indices = source.Indices.ToList();

            proxy.Size = source.Size;
            proxy.Rotation = source.Rotation;

            copiedGeometries.Add(source, proxy);
            return proxy;
        }

        private static GeoReference CopyGeoReference(GeoReference source, GeometryModelData to,
            Dictionary<Layer, Layer> layers, Dictionary<BaseGeometry, BaseGeometry> copiedGeometries
            , bool preserveIds)
        {
            var g = new GeoReference(
                CopyVertexFromTo(source.Vertex, layers, copiedGeometries, preserveIds),
                source.ReferencePoint
                );

            to.GeoReferences.Add(g);

            return g;
        }

        private static DerivedColor CopyColorFromTo(DerivedColor source, Layer targetLayer, GeometryModelData model)
        {
            if (source.Parent is Layer)
            {
                DerivedColor result = new DerivedColor(source.LocalColor, targetLayer, source.PropertyName);
                result.IsFromParent = source.IsFromParent;
                return result;
            }
            else if (source.Parent is BaseGeometry && model.GeometryFromId(((BaseGeometry)source.Parent).Id) != null)
            {
                var sourceGeo = model.GeometryFromId(((BaseGeometry)source.Parent).Id);
                DerivedColor result = new DerivedColor(source.LocalColor, sourceGeo, source.PropertyName)
                {
                    IsFromParent = source.IsFromParent
                };
                return result;
            }
            else
            {
                DerivedColor result = new DerivedColor(source.Color);
                return result;
            }
        }
    }
}
