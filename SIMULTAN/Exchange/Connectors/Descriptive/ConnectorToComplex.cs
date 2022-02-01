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
    /// Class for connecting a component with a <see cref="Volume"/>, that is interpreted as an architectural space.
    /// The source <see cref="SimComponent"/> receives automatically generated subcomponents for each 3D, 2D etc. representation
    /// within the volume. Instances of this class can be used to produce a bill of quantities.
    /// Represents only the top of the hierarchy. Contains no hierarchy of volumes or faces itself. The hierarchy is reflected only in the component structure.
    /// </summary>
    internal class ConnectorToComplex : ConnectorToBaseGeometry
    {
        #region .CTOR

        internal ConnectorToComplex(ComponentGeometryExchange _comm_manager,
                                  SimComponent _source_parent_comp, SimComponent _source_comp, int _index_of_geometry_model,
                                  Volume _target_volume)
            : base(_comm_manager, _source_parent_comp, _source_comp, _index_of_geometry_model, _target_volume)
        {
        }

        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            Volume vol = _target as Volume;
            return (vol != null && vol.Id == this.TargetId);
        }

        #endregion

    }
}
