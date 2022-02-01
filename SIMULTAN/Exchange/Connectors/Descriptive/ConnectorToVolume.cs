using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Class for connecting a component to a <see cref="Volume"/>.
    /// Maintains parameters for volume, area, perimeter and elevations.
    /// Contains no hierarchy of volumes or faces. The hierarchy is reflected only in the component structure.
    /// </summary>
    internal class ConnectorToVolume : ConnectorToBaseGeometry
    {
        #region .CTOR
        internal ConnectorToVolume(ComponentGeometryExchange _comm_manager,
                                 SimComponent _source_parent_comp, SimComponent _source_comp, int _index_of_geometry_model, Volume _target_volume)
            : base(_comm_manager, _source_parent_comp, _source_comp, _index_of_geometry_model, _target_volume)
        {
            //this.SynchronizeSourceWTarget(_target_volume);          
        }
        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            Volume vol = _target as Volume;
            return (vol != null && vol.Id == this.TargetId);
        }

        /// <inheritdoc/>
        protected override void UpdateSourceParametersDelayed(BaseGeometry _target)
        {
            if (this.DescriptiveSource == null || this.comm_manager == null) return;
            Volume v = _target as Volume;
            if (v == null) return;

            using (AccessCheckingDisabler.Disable(this.DescriptiveSource.Factory))
            {
                var volume_content = VolumeAlgorithms.VolumeBruttoNetto(v);
                var volume_areas = VolumeAlgorithms.AreaBruttoNetto(v);
                var volume_elevations = VolumeAlgorithms.Elevation(v);
                var volume_ref_elevations = VolumeAlgorithms.ElevationReference(v);
                var volume_heights = VolumeAlgorithms.Height(v);

                // TODO: replace all 0.0s with the correct call to the target geometry
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_FOK, volume_elevations.floor);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_FOK_ROH, double.NaN);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_F_AXES, volume_ref_elevations.floor);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_DUK, volume_elevations.ceiling);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_DUK_ROH, volume_elevations.floor);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_D_AXES, volume_ref_elevations.ceiling);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_H_NET, volume_heights.min);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_H_GROSS, volume_heights.max);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_H_AXES, volume_heights.reference);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_L_PERIMETER, VolumeAlgorithms.FloorPerimeter(v));
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA_BGF, volume_areas.areaBrutto);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA_NGF, volume_areas.areaNetto);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA_NF, double.NaN);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA_AXES, volume_areas.areaReference);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_VOLUME_BRI, volume_content.volumeBrutto);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_VOLUME_NRI, volume_content.volumeNetto);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_VOLUME_NRI_NF, volume_content.volumeNetto);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_VOLUME_AXES, volume_content.volume);
            }
        }

        #endregion

    }

}
