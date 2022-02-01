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
    /// Class for connecting a component to a <see cref="Vertex"/>.
    /// </summary>
    internal class ConnectorToVertex : ConnectorToBaseGeometry
    {
        #region .CTOR

        internal ConnectorToVertex(ComponentGeometryExchange _comm_manager,
                                    SimComponent _source_parent_comp, SimComponent _source_comp, int _index_of_geometry_model, Vertex _target_vertex)
            : base(_comm_manager, _source_parent_comp, _source_comp, _index_of_geometry_model, _target_vertex)
        {
            //this.SynchronizeSourceWTarget(_target_vertex);
        }

        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            Vertex v = _target as Vertex;
            return (v != null && v.Id == this.TargetId);
        }

        #endregion
    }
}
