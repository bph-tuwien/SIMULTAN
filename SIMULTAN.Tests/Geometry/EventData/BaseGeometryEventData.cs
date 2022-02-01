using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.EventData
{
    public class BaseGeometryEventData : PropertyChangedEventData
    {
        public int GeometryChangedCount;
        public int TopologyChangedCount;


        public BaseGeometryEventData(BaseGeometry geom) : base(geom)
        {
            Reset();
            geom.GeometryChanged += (object sender) => { GeometryChangedCount++; };
            geom.TopologyChanged += (object sender) => { TopologyChangedCount++; };
        }

        public override void Reset()
        {
            base.Reset();
            PropertyChangedData.Clear();
            GeometryChangedCount = 0;
            TopologyChangedCount = 0;
        }
    }
}
