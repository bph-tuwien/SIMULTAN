﻿using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.DataMapping;
using SIMULTAN.Excel;
using SIMULTAN.Exchange;
using SIMULTAN.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using static SIMULTAN.Data.SimNetworks.SimNetwork;

namespace SIMULTAN.Projects
{
    /// <summary>
    /// Holds all data managers - for components and networks, for excel mapping rules, for value fields etc.
    /// </summary>
    public abstract class ProjectData
    {
        /// <summary>
        /// The owner of the project data. Usually the project.
        /// </summary>
        public IReferenceLocation Owner 
        { 
            get { return this.owner; } 
            internal set 
            {
                this.SetCallingLocation(value);
            } 
        }
        private IReferenceLocation owner;

        #region PROPERTIES: Managers

        /// <summary>
        /// Manages the network interactions.
        /// </summary>
        public SimNetworkFactory NetworkManager { get; }
        /// <summary>
        /// Stores all root components in the project
        /// </summary>
        public SimComponentCollection Components { get; }

        /// <summary>
        /// Stores all root networks in the project
        /// </summary>
        public SimNetworkCollection SimNetworks { get; }

        /// <summary>
        /// The ValueMappings in the project
        /// </summary>
        public SimValueMappingCollection ValueMappings { get; }

        /// <summary>
        /// Stores all user defined component lists
        /// </summary>
        public SimUserComponentListCollection UserComponentLists { get; }

        /// <summary>
        /// Manages all Multi-Value-Fields: <see cref="SimMultiValueField3D"/>, <see cref="SimMultiValueFunction"/>, 
        /// <see cref="SimMultiValueBigTable"/>. Parameters within or outside of components can references such values.
        /// </summary>
        public SimMultiValueCollection ValueManager { get; }

        /// <summary>
        /// Manages a library of parameters, external to any component.
        /// </summary>
        public ParameterFactory ParameterLibraryManager { get; }

        /// <summary>
        /// Manages a collection of valid users for the project.
        /// </summary>
		public SimUsersManager UsersManager { get; }

        /// <summary>
        /// Manages a collection of external links for the linked resources of the <see cref="AssetManager"/>.
        /// </summary>
        public MultiLinkManager MultiLinkManager { get; }

        /// <summary>
        /// Manages the assets and resources in the project
        /// </summary>
        public AssetManager AssetManager { get; }

        /// <summary>
        /// Generates ids for all members of this project
        /// </summary>
        public SimIdGenerator IdGenerator { get; } = new SimIdGenerator();

        /// <summary>
        /// Stores all currently active <see cref="GeometryModel"/>.
        /// Does NOT contain all GeometryModels of the Project, but only the ones that are currently loaded
        /// </summary>
        public SimGeometryModelCollection GeometryModels { get; }

        /// <summary>
        /// Provides methods for synchronizing Geometry with Components
        /// </summary>
        public ComponentGeometryExchange ComponentGeometryExchange { get; }

        /// <summary>
        /// Stores all the SitePlannerProjects in the project
        /// </summary>
        public SitePlannerManager SitePlannerManager { get; }

        /// <summary>
        /// Stores the taxonomies in this project
        /// </summary>
        public SimTaxonomyCollection Taxonomies { get; }

        /// <summary>
        /// Stores the DataMapping tools in this project
        /// </summary>
        public SimDataMappingToolCollection DataMappingTools { get; }

        /// <summary>
        /// Stores the geometry relations in this project
        /// </summary>
        public SimGeometryRelationCollection GeometryRelations { get; }

        /// <summary>
        /// Dispatcher timer factory used for the OffsetSurfaceGenerator. This will be remove once the OffsetSurfaceGenerator is reworked.
        /// </summary>
        [Obsolete("Temporary solution for the offset surface generator, will be removed once the generator is reworked")]
        public IDispatcherTimerFactory DispatcherTimerFactory { get; set; }

        /// <summary>
        /// Synchronization context used to run events on the main thread for thread safety.
        /// Does not synchronize by default and needs to be set with a platform specific implementation 
        /// in case synchronization is required.
        /// </summary>
        public ISynchronizeInvoke SynchronizationContext { get; set; }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes all data managers and attaches their respective event handlers.
        /// </summary>
        /// <param name="synchronizationContext">Synchronization context used to run events on the main thread for thread safety.</param>
        /// <param name="dispatcherTimer">Dispatcher timer factory used for the OffsetSurfaceGenerator.</param>
        public ProjectData(ISynchronizeInvoke synchronizationContext, IDispatcherTimerFactory dispatcherTimer)
        {
            if (synchronizationContext == null)
                throw new ArgumentNullException(nameof(synchronizationContext));
            if (dispatcherTimer == null)
                throw new ArgumentNullException(nameof(dispatcherTimer));

            this.SynchronizationContext = synchronizationContext;
            this.DispatcherTimerFactory = dispatcherTimer;

            this.UsersManager = new SimUsersManager();
            this.MultiLinkManager = new MultiLinkManager();

            this.AssetManager = new AssetManager(this);
            this.NetworkManager = new SimNetworkFactory(this);

            this.Components = new SimComponentCollection(this);
            this.SimNetworks = new SimNetworkCollection(this);

            this.UserComponentLists = new SimUserComponentListCollection();
            this.ValueMappings = new SimValueMappingCollection(this);

            this.MultiLinkManager.SecondaryDataManager = this.AssetManager;
            this.MultiLinkManager.UserEncryptionUtiliy = this.UsersManager;

            this.ValueManager = new SimMultiValueCollection(this);
            this.DataMappingTools = new SimDataMappingToolCollection(this);

            this.ParameterLibraryManager = new ParameterFactory();

            this.GeometryModels = new SimGeometryModelCollection(this);
            this.SitePlannerManager = new SitePlannerManager(this);

            this.Taxonomies = new SimTaxonomyCollection(this);

            this.GeometryRelations = new SimGeometryRelationCollection(this);

            this.ComponentGeometryExchange = new ComponentGeometryExchange(this);
        }


        /// <summary>
        /// Clears all managers in the project data
        /// </summary>
        public void Clear()
        {
            this.UsersManager.Clear();

            using (AccessCheckingDisabler.Disable(this.Components))
            {
                this.Taxonomies.IsClosing = true;

                this.SitePlannerManager.ClearRecord();
                this.SitePlannerManager.SetCallingLocation(null);

                this.MultiLinkManager.Clear();
                this.NetworkManager.ClearRecord();
                this.AssetManager.Reset();

                this.ValueManager.Clear();
                this.ValueManager.SetCallingLocation(null);

                this.Components.Clear();
                this.Components.SetCallingLocation(null);

                this.SimNetworks.Clear();
                this.SimNetworks.SetCallingLocation(null);

                this.DataMappingTools.Clear();
                this.DataMappingTools.SetCallingLocation(null);
                this.ParameterLibraryManager.ParameterRecord.Clear();

                this.ValueMappings.Clear();
                this.ValueMappings.SetCallingLocation(null);

                this.UserComponentLists.Clear();

                this.Taxonomies.ClearAllItems(true);
                this.Taxonomies.SetCallingLocation(null);

                this.GeometryRelations.Clear();
                this.GeometryRelations.SetCallingLocation(null);

                this.IdGenerator.Reset();

                this.Taxonomies.IsClosing = false;
            }
        }

        #endregion

        /// <summary>
        /// Sets the calling location for all manager in this project data
        /// </summary>
        /// <param name="caller">The caller</param>
        public void SetCallingLocation(IReferenceLocation caller)
        {
            this.owner = caller;
            this.ValueManager.SetCallingLocation(caller);
            this.NetworkManager.SetCallingLocation(caller);
            this.Components.SetCallingLocation(caller);
            this.SitePlannerManager.SetCallingLocation(caller);
            this.ValueMappings.SetCallingLocation(caller);
            this.Taxonomies.SetCallingLocation(caller);
            this.GeometryRelations.SetCallingLocation(caller);
            this.SimNetworks.SetCallingLocation(caller);
            this.DataMappingTools.SetCallingLocation(caller);
        }

        /// <summary>
        /// Log file which contains errors and warnings generated during import
        /// </summary>
        public FileInfo ImportLogFile { get; set; }

        /// <summary>
        /// Looks up taxonomy entries for default slot by their name.
        /// Do this if the default taxonomies changed, could mean that the project is migrated.
        /// </summary>
        /// <param name="taxonomyFileVersion">The file version of the loaded managed taxonomy file</param>
        public void RestoreDefaultTaxonomyReferences(ulong taxonomyFileVersion = 0)
        {
            Components.RestoreDefaultTaxonomyReferences(taxonomyFileVersion);
            AssetManager.RestoreDefaultTaxonomyReferences();
            GeometryRelations.RestoreDefaultTaxonomyReferences();
            DataMappingTools.RestoreDefaultTaxonomyReferences();
        }
    }
}
