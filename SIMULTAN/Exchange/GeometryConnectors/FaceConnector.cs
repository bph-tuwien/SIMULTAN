using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    /// <summary>
    /// Manages the connection between a <see cref="SimComponentInstance"/> of type <see cref="SimInstanceType.AttributesFace"/>
    /// and a <see cref="Face"/>
    /// </summary>
    internal class FaceConnector : BaseGeometryConnector<Face>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FaceConnector"/> class
        /// </summary>
        /// <param name="geometry">The face</param>
        /// <param name="placement">The instance placement</param>
        internal FaceConnector(Face geometry, SimInstancePlacementGeometry placement) : base(geometry, placement)
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Factory))
            {
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_AREA,
                    SimParameterInstancePropagation.PropagateNever, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_COUNT,
                    SimParameterInstancePropagation.PropagateNever, 0.0);
            }
        }

        private void UpdateComponent(bool placementDeleted, bool geometryMissing)
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Component.Factory))
            {
                var countParam = Placement.Instance.Component.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_COUNT);
                if (countParam != null)
                {
                    Placement.Instance.InstanceParameterValuesPersistent[countParam] = placementDeleted ? 0.0 : 1.0;
                    countParam.ValueCurrent = Placement.Instance.Component.Instances.Sum(x => x.InstanceParameterValuesPersistent[countParam]);
                }

                var areaParam = Placement.Instance.Component.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_AREA);
                if (areaParam != null)
                {
                    Placement.Instance.InstanceParameterValuesPersistent[areaParam] = 
                        geometryMissing || placementDeleted ? 0.0 : FaceAlgorithms.Area(TypedGeometry);
                    areaParam.ValueCurrent = Placement.Instance.Component.Instances.Sum(x => x.InstanceParameterValuesPersistent[areaParam]);
                }
            }
        }


        #region BaseGeometryConnector

        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
            UpdateComponent(false, false);
        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
            UpdateComponent(false, false);
        }

        /// <inheritdoc />
        internal override void OnPlacementRemoved()
        {
            UpdateComponent(true, false);
        }
        /// <inheritdoc />
        internal override void OnGeometryRemoved()
        {
            base.OnGeometryRemoved();
            UpdateComponent(false, true);
        }

        /// <inheritdoc />
        protected override void OnTargetGeometryChanged(BaseGeometry oldGeometry, BaseGeometry newGeometry)
        {
            UpdateComponent(false, false);
        }
        /// <inheritdoc />
        internal override void OnConnectorsInitialized()
        {
            UpdateComponent(false, false);
        }

        #endregion
    }
}
