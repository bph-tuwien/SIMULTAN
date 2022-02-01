using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// The base class for all geometry connectors. It defines synchronization and update methods.
    /// </summary>
    internal abstract class ConnectorBase : Connector
    {
        protected Dispatcher Dispatcher { get; }

        #region FIELDS

        /// <summary>
        /// The instance holding and managing all connectors
        /// </summary>
        protected ComponentGeometryExchange comm_manager;

        private DispatcherTimer timer_UpdateSourceParameters;
        private BaseGeometry delayed_update_target;

        #endregion

        #region .CTOR

        /// <summary>
        /// Initializes an instance of the class ConnectorBase
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing this instance</param>
        /// <param name="_source_parent_comp">the parent component of the source(component or instance)</param>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the geometry resides</param>
        /// <param name="_target_geometry">the target geometry</param>
        protected ConnectorBase(ComponentGeometryExchange _comm_manager,
                                 SimComponent _source_parent_comp, int _index_of_geometry_model, BaseGeometry _target_geometry)
            : base(_source_parent_comp, _index_of_geometry_model, (_target_geometry == null) ? ulong.MaxValue : _target_geometry.Id)
        {
            this.Dispatcher = Dispatcher.CurrentDispatcher;

            this.comm_manager = _comm_manager;

            // timer
            this.timer_UpdateSourceParameters = new DispatcherTimer();
            this.timer_UpdateSourceParameters.Interval = new TimeSpan(0, 0, 1);
            this.timer_UpdateSourceParameters.Tick += new EventHandler(OnUpdateSourceParametersDelayTimerTick);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Sets the values of geometry-dependent parameters. Creates / deletes / adapts
        /// sub-components and referenced components in the source, depending on the type
        /// of the target geometry. Updates the component assets.
        /// </summary>
        /// <param name="_target">geometry to synchronize with</param>
        public virtual void SynchronizeSourceWTarget(BaseGeometry _target)
        {
            this.SyncState = SynchronizationState.SYNCHRONIZED;
        }

        /// <summary>
        /// Part of the synchronization routine. Checks the compatibility
        /// between a connector type and a target type, checks that the id of the geometry is the same as TargetId
        /// </summary>
        /// <param name="_target">geometry to synchronize with</param>
        /// <returns>true, if the geometry is of the correct type</returns>
        protected virtual bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            return true;
        }

        /// <summary>
        /// Delays the execution of the actual parameter update.
        /// </summary>
        /// <param name="_target">geometry to extract parameters from</param>
        protected void UpdateSourceParameters(BaseGeometry _target)
        {
            this.delayed_update_target = _target;

            if (this.comm_manager.EnableAsyncUpdates)
            {
                this.timer_UpdateSourceParameters.Stop();
                this.timer_UpdateSourceParameters.Start();
            }
            else
            {
                this.OnUpdateSourceParametersDelayTimerTick(null, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Part of the synchronization routine. Delayed execution. Fills the reserved parameters of the source
        /// component type or instance with information derived from the target geometry.
        /// </summary>
        /// <param name="_target">geometry to extract parameters from</param>
        protected virtual void UpdateSourceParametersDelayed(BaseGeometry _target)
        { }

        /// <summary>
        /// Part of the synchronization routine. Triggers the delayed update of parameters.
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">unimportant, not used parameters</param>
        protected void OnUpdateSourceParametersDelayTimerTick(object sender, EventArgs e)
        {
            this.timer_UpdateSourceParameters.Stop();
            this.Dispatcher.Invoke(() =>
            {
                this.UpdateSourceParametersDelayed(this.delayed_update_target);
                this.delayed_update_target = null;
            });
        }

        #endregion
    }
}
