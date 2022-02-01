using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides methods for working with layers
    /// </summary>
    public static class LayerAlgorithms
    {
        /// <summary>
        /// Deletes the layer and it's content
        /// </summary>
        /// <param name="layer">The layer to delete</param>
        /// <returns>A list of undo items for this operation</returns>
        public static List<IUndoItem> DeleteLayerAndContent(Layer layer)
        {
            layer.Model.StartBatchOperation();
            List<IUndoItem> undoItems = new List<IUndoItem>();
            List<BaseGeometry> deleteGeometry = new List<BaseGeometry>();

            DeleteLayerAndContent(layer, undoItems, deleteGeometry);

            deleteGeometry = deleteGeometry.Distinct().ToList();

            //Handle deleted openings in faces that are not deleted
            foreach (var gl in deleteGeometry.Where(x => x is EdgeLoop))
            {
                var l = (EdgeLoop)gl;

                foreach (var holeFace in l.Faces.Where(x => x.Boundary != l && !deleteGeometry.Contains(x)))
                {
                    holeFace.Holes.Remove(l);
                    undoItems.Add(new HoleRemoveUndoItem(holeFace, l));
                }
            }

            //Delete geometry
            deleteGeometry.ForEach(x => x.RemoveFromModel());
            undoItems.Insert(0, new GeometryRemoveUndoItem(deleteGeometry, layer.Model));

            layer.Model.EndBatchOperation();

            return undoItems;
        }

        private static void DeleteLayerAndContent(Layer layer, List<IUndoItem> undoItems, List<BaseGeometry> deleteGeometry)
        {
            foreach (var l in layer.Layers.ToList())
                DeleteLayerAndContent(l, undoItems, deleteGeometry);

            layer.Layers.Clear();

            //Delete basegeometries on current layer
            var geom = GeometryModelAlgorithms.GetAllContainingGeometries(layer.Elements);
            geom.Reverse();
            deleteGeometry.AddRange(geom);

            if (layer.Parent == null)
                undoItems.Add(CollectionUndoItem.Remove(layer.Model.Layers, layer));
            else
                undoItems.Add(CollectionUndoItem.Remove(layer.Parent.Layers, layer));
        }
    }
}
