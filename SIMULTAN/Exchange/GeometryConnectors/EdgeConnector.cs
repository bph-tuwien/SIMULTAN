using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange;
using SIMULTAN.Exchange.GeometryConnectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    internal class EdgeConnector : BaseGeometryConnector<Edge>
    {
        internal EdgeConnector(Edge geometry, SimInstancePlacementGeometry placement) : base(geometry, placement)
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Factory))
            {
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_LENGTH, ReservedParameters.RP_LENGTH,
                    SimParameterInstancePropagation.PropagateNever, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_COUNT, ReservedParameters.RP_COUNT,
                    SimParameterInstancePropagation.PropagateNever, 0.0);
            }
        }

        private void UpdateComponent(bool placementDeleted, bool geometryMissing)
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Component.Factory))
            {
                var countParam = Placement.Instance.Component.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_COUNT));
                if (countParam != null)
                {
                    Placement.Instance.InstanceParameterValuesPersistent[countParam] = placementDeleted ? 0.0 : 1.0;
                    countParam.ValueCurrent = Placement.Instance.Component.Instances.Sum(x => x.InstanceParameterValuesPersistent[countParam]);
                }

                var lengthParam = Placement.Instance.Component.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_LENGTH));
                if (lengthParam != null)
                {
                    Placement.Instance.InstanceParameterValuesPersistent[lengthParam] =
                        geometryMissing || placementDeleted ? 0.0 : EdgeAlgorithms.Length(TypedGeometry);
                    lengthParam.ValueCurrent = Placement.Instance.Component.Instances.Sum(x => x.InstanceParameterValuesPersistent[lengthParam]);
                }

                UpdateInstancePath(geometryMissing);
            }
        }

        private void UpdateInstancePath(bool geometryMissing)
        {
            if (!geometryMissing && TypedGeometry.Vertices != null && TypedGeometry.Vertices.Count > 0)
            {
                this.Placement.Instance.InstancePath = TypedGeometry.Vertices.Select(x => x.Position).ToList();
            }
            else
            {
                this.Placement.Instance.InstancePath = new List<Point3D>(); ;
            }
        }

        #region BaseGeometryConnector

        internal override void OnGeometryChanged()
        {
            UpdateComponent(false, false);
        }
        internal override void OnTopologyChanged()
        {
            UpdateComponent(false, false);
        }

        internal override void OnPlacementRemoved()
        {
            UpdateComponent(true, false);
        }
        internal override void OnGeometryRemoved()
        {
            base.OnGeometryRemoved();
            UpdateComponent(false, true);
        }

        protected override void OnTargetGeometryChanged(BaseGeometry oldGeometry, BaseGeometry newGeometry)
        {
            UpdateComponent(false, false);
        }

        internal override void OnConnectorsInitialized()
        {
            UpdateComponent(false, false);
        }

        #endregion
    }
}
