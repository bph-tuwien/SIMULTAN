using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Class for connecting a component to an edge. Maintains parameters for length.
    /// </summary>
    internal class ConnectorToEdge : ConnectorToBaseGeometry
    {
        #region .CTOR

        internal ConnectorToEdge(ComponentGeometryExchange _comm_manager,
                                  SimComponent _source_parent_comp, SimComponent _source_comp, int _index_of_geometry_model, Edge _target_edge)
            : base(_comm_manager, _source_parent_comp, _source_comp, _index_of_geometry_model, _target_edge)
        {
            //this.SynchronizeSourceWTarget(_target_edge);
        }

        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            Edge edge = _target as Edge;
            return (edge != null && edge.Id == this.TargetId);
        }

        /// <inheritdoc/>
        protected override void UpdateSourceParametersDelayed(BaseGeometry _target)
        {
            if (this.DescriptiveSource == null || this.comm_manager == null) return;

            // TODO: replace all 0.0s with the correct call to the target geometry
            using (AccessCheckingDisabler.Disable(this.DescriptiveSource.Factory))
            {
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_LENGTH, 0.0);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_LENGTH_MIN, 0.0);
                SetOrCreateParameter(this.DescriptiveSource, ReservedParameters.RP_LENGTH_MAX, 0.0);
            }
        }

        #endregion
    }
}
