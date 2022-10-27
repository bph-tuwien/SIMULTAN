using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry.UndoRedo
{
    internal class RemoveLayerUndoItem : IUndoItem
    {
        private ObservableCollection<Layer> layerList;
        private Layer layer;
        int idx;

        public RemoveLayerUndoItem(ObservableCollection<Layer> layerList, Layer layer)
        {
            this.layerList = layerList;
            this.layer = layer;

            Redo();
        }

        public UndoExecutionResult Execute()
        {
            return UndoExecutionResult.Executed;
        }

        public void Redo()
        {
            idx = layerList.IndexOf(layer);
            layerList.RemoveAt(idx);
            layer.DetachEvents();

            layer.Elements.Clear();
        }

        public void Undo()
        {
            layerList.Insert(idx, layer);
            layer.AttachEvents();
        }
    }
}
