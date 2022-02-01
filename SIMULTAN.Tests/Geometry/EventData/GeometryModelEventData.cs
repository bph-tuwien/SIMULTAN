using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.EventData
{
    public class GeometryModelEventData : PropertyChangedEventData
    {
        public List<IEnumerable<BaseGeometry>> TopologyChangedEventData { get; private set; }
        public int BatchOperationFinishedCount { get; private set; }
        public List<IEnumerable<BaseGeometry>> AddEventData { get; private set; }
        public List<IEnumerable<BaseGeometry>> RemoveEventData { get; private set; }
        public List<IEnumerable<BaseGeometry>> GeometryChangedEventData { get; private set; }
        public List<IEnumerable<BaseGeometry>> OperationFinished { get; private set; }


        public GeometryModelEventData(GeometryModelData model) : base(model)
        {
            TopologyChangedEventData = new List<IEnumerable<BaseGeometry>>();
            BatchOperationFinishedCount = 0;
            AddEventData = new List<IEnumerable<BaseGeometry>>();
            RemoveEventData = new List<IEnumerable<BaseGeometry>>();
            GeometryChangedEventData = new List<IEnumerable<BaseGeometry>>();
            OperationFinished = new List<IEnumerable<BaseGeometry>>();

            model.TopologyChanged += (object sender, IEnumerable<BaseGeometry> geometry) => TopologyChangedEventData.Add(geometry);
            model.BatchOperationFinished += (object sender, EventArgs args) => BatchOperationFinishedCount++;
            model.GeometryAdded += (object sender, IEnumerable<BaseGeometry> add) => AddEventData.Add(add);
            model.GeometryRemoved += (object sender, IEnumerable<BaseGeometry> remove) => RemoveEventData.Add(remove);
            model.GeometryChanged += (object sender, IEnumerable<BaseGeometry> changed) => GeometryChangedEventData.Add(changed);
            model.OperationFinished += (object sender, IEnumerable<BaseGeometry> affectedGeometries) => OperationFinished.Add(affectedGeometries);
        }

        public override void Reset()
        {
            base.Reset();
            TopologyChangedEventData.Clear();
            BatchOperationFinishedCount = 0;
            AddEventData.Clear();
            RemoveEventData.Clear();
            GeometryChangedEventData.Clear();
            OperationFinished.Clear();
        }
    }
}
