using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    /// <summary>
    /// Manages the connection between a <see cref="SimComponentInstance"/> of type <see cref="SimInstanceType.GeometricSurface"/>
    /// and a <see cref="EdgeLoop"/>. Used to represent empty holes in <see cref="VolumeFaceConnector"/>.
    /// This connector is only used for empty holes and is exchanged with a <see cref="VolumeFaceConnector"/> 
    /// when the hole EdgeLoop is a boundary of another Face.
    /// </summary>
    internal class VolumeFaceHoleConnector : BaseGeometryConnector<EdgeLoop>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeFaceHoleConnector"/> class
        /// </summary>
        /// <param name="loop">The <see cref="EdgeLoop"/></param>
        /// <param name="placement">The instance placement</param>
        internal VolumeFaceHoleConnector(EdgeLoop loop, SimInstancePlacementGeometry placement) : base(loop, placement)
        {
            //Create parameters
            using (AccessCheckingDisabler.Disable(placement.Instance.Factory))
            {
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_AREA,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_AREA_MIN,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_AREA_MAX,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_WIDTH,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_WIDTH_MIN,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_WIDTH_MAX,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_HEIGHT,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_HEIGHT_MIN,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_HEIGHT_MAX,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_K_F_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_K_D_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                this.Placement.Instance.Component.Name = loop.Name;
            }

            ExchangeHelpers.CreateAssetIfNotExists(placement.Instance.Component, loop);
        }

        private void UpdateParameters(bool geometryExists)
        {
            double area = 0.0;
            Size size = new Size(0, 0);
            double kfAxis = 0.0, kdAxis = 0.0;
            List<Point3D> boundary = new List<Point3D>();

            if (geometryExists)
            {
                area = EdgeLoopAlgorithms.Area(TypedGeometry);
                size = EdgeLoopAlgorithms.Size(TypedGeometry);
                (kfAxis, kdAxis) = EdgeLoopAlgorithms.HeightMinMax(TypedGeometry);

                for (int i = 0; i < TypedGeometry.Edges.Count; i++)
                    boundary.Add(TypedGeometry.Edges[i].StartVertex.Position);
            }

            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_AREA, area);
            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_AREA_MIN, area);
            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_AREA_MAX, area);

            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_WIDTH, size.Width);
            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_WIDTH_MIN, size.Width);
            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_WIDTH_MAX, size.Width);
            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_HEIGHT, size.Height);
            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_HEIGHT_MIN, size.Height);
            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_HEIGHT_MAX, size.Height);

            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_K_F_AXES, kfAxis);
            ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_K_D_AXES, kdAxis);

            Placement.Instance.InstancePath = boundary;
        }

        #region BaseGeometryConnector

        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
            UpdateParameters(true);
        }
        /// <inheritdoc />
        internal override void OnPlacementRemoved()
        {
            //Nothing needed, component will be deleted
        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
            UpdateParameters(true);
        }
        /// <inheritdoc />
        internal override void OnGeometryRemoved()
        {
            base.OnGeometryRemoved();
            UpdateParameters(false);
        }
        /// <inheritdoc />
        protected override void OnTargetGeometryChanged(BaseGeometry oldGeometry, BaseGeometry newGeometry)
        {
            UpdateParameters(true);

            using (AccessCheckingDisabler.Disable(this.Placement.Instance.Component.Factory))
            {
                this.Placement.Instance.Component.Name = newGeometry.Name;
            }
        }
        /// <inheritdoc />
        internal override void OnConnectorsInitialized()
        {
            UpdateParameters(true);
        }
        /// <inheritdoc />
        internal override void OnGeometryNameChanged(BaseGeometry geometry)
        {
            base.OnGeometryNameChanged(geometry);

            using (AccessCheckingDisabler.Disable(this.Placement.Instance.Component.Factory))
            {
                this.Placement.Instance.Component.Name = geometry.Name;
            }
        }
        #endregion
    }
}
