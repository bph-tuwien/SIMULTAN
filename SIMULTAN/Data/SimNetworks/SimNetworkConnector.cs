using SIMULTAN.Serializer.DXF;
using System;
using System.Text;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Class for representing a connector between two ports in a SimNetwork
    /// </summary>
    public partial class SimNetworkConnector : SimNamedObject<ISimManagedCollection>
    {
        private SimId loadingSourceId, loadingTargetId;


        /// <summary>
        /// Representing the parent network.
        /// </summary>
        public SimNetwork ParentNetwork { get; internal set; }
        /// <summary>
        /// Source Port
        /// </summary>
        public SimNetworkPort Source { get; set; }
        /// <summary>
        /// The target Port
        /// </summary>
        public SimNetworkPort Target { get; set; }


        #region .CTOR
        /// <summary>
        /// Initializes a new SimNetworkConnector
        /// </summary>
        /// <param name="source">The source port</param>
        /// <param name="target">The target port</param>
        public SimNetworkConnector(SimNetworkPort source, SimNetworkPort target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            this.Source = source;
            this.Target = target;
        }
        /// <summary>
        /// For Parsing
        /// </summary>
        /// <param name="id">The loaded id of the SimNetworkConnector</param>
        internal SimNetworkConnector(SimId id)
        {
            this.Id = id;
        }
        
        internal SimNetworkConnector(string name, SimId id, SimId loadingSourceId, SimId loadingTargetId)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.Name = name;
            this.Id = id;
            this.loadingSourceId = loadingSourceId;
            this.loadingTargetId = loadingTargetId;
        }

        #endregion

    
        internal void RestoreReferences()
        {
            if (this.Factory != null)
            {
                if (this.loadingSourceId != SimId.Empty)
                    this.Source = Factory.ProjectData.IdGenerator.GetById<SimNetworkPort>(loadingSourceId);
                if (this.loadingTargetId != SimId.Empty)
                    this.Target = Factory.ProjectData.IdGenerator.GetById<SimNetworkPort>(loadingTargetId);

                this.loadingSourceId = SimId.Empty;
                this.loadingTargetId = SimId.Empty;
            }
        }
    }
}
