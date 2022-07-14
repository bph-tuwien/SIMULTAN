using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Users;
using SIMULTAN.Excel;
using SIMULTAN.Exchange;
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
        public abstract IReferenceLocation Owner { get; }

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
        /// Stores all user defined component lists
        /// </summary>
        public SimUserComponentListCollection UserComponentLists { get; }

        /// <summary>
        /// Manages all Multi-Value-Fields: <see cref="SimMultiValueField3D"/>, <see cref="SimMultiValueFunction"/>, 
        /// <see cref="SimMultiValueBigTable"/>. Parameters within or outside of components can references such values.
        /// </summary>
        public SimMultiValueCollection ValueManager { get; }
        /// <summary>
        /// Manages the mapping between the components and various Excel tools. Each mapping 
        /// is in a separate <see cref="ExcelTool"/>, even if it references the same Excel sheet.
        /// </summary>
        public ExcelToolFactory ExcelToolMappingManager { get; }

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

        public ComponentGeometryExchange ComponentGeometryExchange { get; }

        public SitePlannerManager SitePlannerManager { get; }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes all data managers and attaches their respective event handlers.
        /// </summary>
        public ProjectData()
        {
            this.UsersManager = new SimUsersManager();
            this.MultiLinkManager = new MultiLinkManager();

            this.AssetManager = new AssetManager(this);
            this.NetworkManager = new SimNetworkFactory(this);

            this.Components = new SimComponentCollection(this);
            this.SimNetworks = new SimNetworkCollection(this);

            this.UserComponentLists = new SimUserComponentListCollection();

            this.MultiLinkManager.SecondaryDataManager = this.AssetManager;
            this.MultiLinkManager.UserEncryptionUtiliy = this.UsersManager;

            this.ValueManager = new SimMultiValueCollection(this);

            this.ExcelToolMappingManager = new ExcelToolFactory(this);

            this.ParameterLibraryManager = new ParameterFactory();

            this.GeometryModels = new SimGeometryModelCollection(this);
            this.SitePlannerManager = new SitePlannerManager(this);

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

                this.ExcelToolMappingManager.ClearRecord();
                this.ParameterLibraryManager.ClearRecord();

                this.UserComponentLists.Clear();

                this.IdGenerator.Reset();
            }
        }

        #endregion

        /// <summary>
        /// Sets the calling location for all manager in this project data
        /// </summary>
        /// <param name="caller">The caller</param>
        public void SetCallingLocation(IReferenceLocation caller)
        {
            this.ValueManager.SetCallingLocation(caller);
            this.NetworkManager.SetCallingLocation(caller);
            this.Components.SetCallingLocation(caller);
            this.SitePlannerManager.SetCallingLocation(caller);
            this.SimNetworks.SetCallingLocation(caller);
        }
    }
}
