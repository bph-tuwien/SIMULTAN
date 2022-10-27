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
    /// Manages the connection between a <see cref="SimComponentInstance"/> of type <see cref="SimInstanceType.GeometricVolume"/>
    /// and a <see cref="Volume"/>
    /// </summary>
    internal class VolumeVolumeConnector : BaseGeometryConnector<Volume>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeVolumeConnector"/> class
        /// </summary>
        /// <param name="volume">The volume</param>
        /// <param name="placement">The instance placement</param>
        internal VolumeVolumeConnector(Volume volume, SimInstancePlacementGeometry placement) : base(volume, placement)
        {
            using (AccessCheckingDisabler.Disable(placement.Instance.Component.Factory))
            {
                //Check that parameters exist
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_K_FOK, ReservedParameters.RP_K_FOK,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_K_FOK_ROH, ReservedParameters.RP_K_FOK_ROH,
                    SimParameterInstancePropagation.PropagateAlways, double.NaN);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_K_F_AXES, ReservedParameters.RP_K_F_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_K_DUK, ReservedParameters.RP_K_DUK,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_K_DUK_ROH, ReservedParameters.RP_K_DUK_ROH,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_K_D_AXES, ReservedParameters.RP_K_D_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_H_NET, ReservedParameters.RP_H_NET,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_H_GROSS, ReservedParameters.RP_H_GROSS,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_H_AXES, ReservedParameters.RP_H_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_L_PERIMETER, ReservedParameters.RP_L_PERIMETER,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_AREA_BGF, ReservedParameters.RP_AREA_BGF,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_AREA_NGF, ReservedParameters.RP_AREA_NGF,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_AREA_NF, ReservedParameters.RP_AREA_NF,
                    SimParameterInstancePropagation.PropagateAlways, double.NaN);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_AREA_AXES, ReservedParameters.RP_AREA_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_VOLUME_BRI, ReservedParameters.RP_VOLUME_BRI,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_VOLUME_NRI, ReservedParameters.RP_VOLUME_NRI,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_VOLUME_NRI_NF, ReservedParameters.RP_VOLUME_NRI_NF,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameterKeys.RP_VOLUME_AXES, ReservedParameters.RP_VOLUME_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                this.Placement.Instance.Component.Name = volume.Name;
            }

            ExchangeHelpers.CreateAssetIfNotExists(placement.Instance.Component, volume);

        }

        private void UpdateParameters(bool geometryExists)
        {
            double volumeRef = 0.0, volumeNetto = 0.0, volumeBrutto = 0.0;
            double areaRef = 0.0, areaNetto = 0.0, areaBrutto = 0.0;
            double elevFloor = 0.0, elevCeiling = 0.0;
            double refElevFloor = 0.0, refElevCeiling = 0.0;
            double heightRef = 0.0, heightMin = 0.0, heightMax = 0.0;
            double floorPerim = 0.0;

            if (geometryExists)
            {
                (volumeRef, volumeBrutto, volumeNetto) = VolumeAlgorithms.VolumeBruttoNetto(TypedGeometry);
                (areaRef, areaBrutto, areaNetto) = VolumeAlgorithms.AreaBruttoNetto(TypedGeometry);
                (elevFloor, elevCeiling) = VolumeAlgorithms.Elevation(TypedGeometry);

                (refElevFloor, refElevCeiling) = VolumeAlgorithms.ElevationReference(TypedGeometry);
                (heightRef, heightMin, heightMax) = VolumeAlgorithms.Height(TypedGeometry);
                floorPerim = VolumeAlgorithms.FloorPerimeter(TypedGeometry);
            }

            using (AccessCheckingDisabler.Disable(Placement.Instance.Factory))
            {
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_K_FOK, elevFloor);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_K_FOK_ROH, double.NaN);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_K_F_AXES, refElevFloor);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_K_DUK, elevCeiling);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_K_DUK_ROH, elevFloor);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_K_D_AXES, refElevCeiling);

                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_H_NET, heightMin);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_H_GROSS, heightMax);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_H_AXES, heightRef);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_L_PERIMETER, floorPerim);

                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_AREA_BGF, areaBrutto);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_AREA_NGF, areaNetto);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_AREA_NF, double.NaN);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_AREA_AXES, areaRef);

                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_VOLUME_BRI, volumeBrutto);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_VOLUME_NRI, volumeNetto);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_VOLUME_NRI_NF, volumeNetto);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameterKeys.RP_VOLUME_AXES, volumeRef);
            }
        }

        #region BaseGeometryConnector

        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
            //Update parameter values
            UpdateParameters(true);
        }
        /// <inheritdoc />
        internal override void OnPlacementRemoved()
        {
            //No update needed, the component will be removed anyway
        }
        /// <inheritdoc />
        internal override void OnGeometryRemoved()
        {
            base.OnGeometryRemoved();

            //Reset parameter values
            //UpdateParameters(false);
        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
            //Update parameter values
            UpdateParameters(true);
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

        /// <inheritdoc/>
        internal override void OnOffsetSurfacesChanged()
        {
            UpdateParameters(true);
        }

        #endregion
    }
}
