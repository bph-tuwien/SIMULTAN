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
    /// Class for connecting a component to an <see cref="EdgeLoop"/> instance.
    /// Maintains parameters for area, with and height. Transfers the loop as a 3d point sequence to the default
    /// instance (of type <see cref="SimComponentInstance"/>) of the source component.
    /// </summary>
    internal class ConnectorToEdgeLoop : ConnectorToBaseGeometry
    {
        #region .CTOR

        internal ConnectorToEdgeLoop(ComponentGeometryExchange _comm_manager,
                                 SimComponent _source_parent_comp, SimComponent _source_comp, int _index_of_geometry_model, EdgeLoop _target_loop)
            : base(_comm_manager, _source_parent_comp, _source_comp, _index_of_geometry_model, _target_loop)
        {
            //this.SynchronizeSourceWTarget(_target_loop);
        }

        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            EdgeLoop el = _target as EdgeLoop;
            return (el != null && el.Id == this.TargetId);
        }

        /// <inheritdoc/>
        protected override void UpdateSourceParametersDelayed(BaseGeometry _target)
        {
            if (this.DescriptiveSource == null || this.comm_manager == null) return;
            EdgeLoop el = _target as EdgeLoop;
            if (el == null) return;

            // TODO: replace all 0.0s with the correct call to the target geometry
            using (AccessCheckingDisabler.Disable(this.DescriptiveSource.Factory))
            {
                var area = EdgeLoopAlgorithms.Area(el);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA, area);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA_MIN, area);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_AREA_MAX, area);

                var size = EdgeLoopAlgorithms.Size(el);

                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_WIDTH, size.Width);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_WIDTH_MIN, size.Width);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_WIDTH_MAX, size.Width);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_HEIGHT, size.Height);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_HEIGHT_MIN, size.Height);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_HEIGHT_MAX, size.Height);

                var heights = EdgeLoopAlgorithms.HeightMinMax(el);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_F_AXES, heights.min);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_K_D_AXES, heights.max);

                // Update the path
                List<Point3D> f_boundary = el.Edges.Select(x => x.StartVertex.Position).ToList();
                this.DescriptiveSource.Instances[0].InstancePath = f_boundary;
            }
        }

        #endregion
    }
}
