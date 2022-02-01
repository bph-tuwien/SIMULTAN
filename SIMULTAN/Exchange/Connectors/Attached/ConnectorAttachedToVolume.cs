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
    /// Manages the connection of a <see cref="SimComponentInstance"/> to the <see cref="Volume"/> it
    /// is attached to (or placed in).
    /// </summary>
    internal class ConnectorAttachedToVolume : ConnectorAttachedToBase
    {
        #region .CTOR

        /// <summary>
        /// Initializes an instance of the class ConnectorAttachedToVolume.
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing this instance</param>
        /// <param name="_source_parent_comp">the parent component of the source instance</param>
        /// <param name="_containing_connector">the connector that holds the container of the source</param>
        /// <param name="_source">the component instance that is being attached</param>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the geometry resides</param>
        /// <param name="_target_for_attachment">the volume in which the source is contained</param>
        public ConnectorAttachedToVolume(ComponentGeometryExchange _comm_manager, SimComponent _source_parent_comp,
                                        ConnectorRepresentativeToBase _containing_connector, SimComponentInstance _source,
                                        int _index_of_geometry_model, Volume _target_for_attachment)
            : base(_comm_manager, _source_parent_comp, _containing_connector, _source, _index_of_geometry_model, _target_for_attachment)
        {

        }

        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            return (_target is Volume && _target.Id == this.TargetId);
        }

        /// <inheritdoc/>
        public override void SynchronizeSourceWTarget(BaseGeometry _target)
        {
            if (this.AttachedSource == null) return;

            if (this.SynchTargetIsAdmissible(_target))
            {
                // ???
            }
            else
            {

            }
        }

        #endregion
    }
}
