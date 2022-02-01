using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SIMULTAN.Data.Geometry.GeometrySelectionModel;

namespace SIMULTAN.Tests.Geometry.EventData
{
    public class SelectionModelEventData
    {
        public List<SelectionChangedEventArgs> SelectionChangedEventData;
        public List<ActiveGeometryChangedEventArgs> ActiveChangedEventData;

        public SelectionModelEventData(GeometrySelectionModel selectionModel)
        {
            this.SelectionChangedEventData = new List<SelectionChangedEventArgs>();
            this.ActiveChangedEventData = new List<ActiveGeometryChangedEventArgs>();

            selectionModel.SelectionChanged += (object sender, SelectionChangedEventArgs args) => SelectionChangedEventData.Add(args);
            selectionModel.ActiveChanged += (object sender, ActiveGeometryChangedEventArgs args) => ActiveChangedEventData.Add(args);
        }

        public void Reset()
        {
            this.SelectionChangedEventData.Clear();
            this.ActiveChangedEventData.Clear();
        }
    }
}
