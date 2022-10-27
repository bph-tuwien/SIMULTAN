using SIMULTAN.Data.Assets;
using SIMULTAN.Data.ValueMappings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Contains references to SimGeo and GeoMap files and additional information for visualization in the SitePlanner
    /// </summary>
    public class SitePlannerProject : INotifyPropertyChanged
    {
        #region EVENTS

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="property">The name of the property</param>
        protected void NotifyPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion


        #region Value Mappings

        /// <summary>
        /// Stores a list of all ValueMappings that are available in this project
        /// </summary>
        public ObservableCollection<SimValueMapping> ValueMappings { get; }

        /// <summary>
        /// The currently active ValueMapping. Has to be one of the mappings stored in <see cref="ValueMappings"/>
        /// </summary>
        public SimValueMapping ActiveValueMapping
        {
            get { return activeValueMapping; }
            set
            {
                if (activeValueMapping != value)
                {
                    activeValueMapping = value;
                    NotifyPropertyChanged(nameof(ActiveValueMapping));
                }
            }
        }
        private SimValueMapping activeValueMapping = null;

        /// <summary>
        /// When set to True, the rendered color should be affected by the <see cref="ActiveValueMapping"/>. 
        /// Otherwise, the color of the buildings may be used.
        /// </summary>
        public bool IsValueMappingEnabled
        {
            get { return isValueMappingEnabled; }
            set
            {
                if (isValueMappingEnabled != value)
                {
                    isValueMappingEnabled = value;
                    NotifyPropertyChanged(nameof(IsValueMappingEnabled));
                }
            }
        }
        private bool isValueMappingEnabled = false;

        #endregion

        /// <summary>
        /// Stores the factory this project belongs to
        /// </summary>
        public SitePlannerManager Factory { get; set; }

        /// <summary>
        /// Path to the DXF file that contains this SitePlannerProject
        /// </summary>
        public ResourceFileEntry SitePlannerFile { get; private set; }

        /// <summary>
        /// Maps contained in this project. One map consists of at least an image.
        /// </summary>
        public ObservableCollection<SitePlannerMap> Maps { get; private set; }

        /// <summary>
        /// Buildings contained in this project. One building consists of at least a SimGeo file.
        /// </summary>
        public SitePlannerBuildingCollection Buildings { get; private set; }

        /// <summary>
        /// Initializes a new instance of the SitePlannerProject class
        /// </summary>
        /// <param name="projectFile">ResourceFileEntry of SitePlanner file (spdxf)</param>
        public SitePlannerProject(ResourceFileEntry projectFile)
        {
            this.SitePlannerFile = projectFile;

            Buildings = new SitePlannerBuildingCollection(this);
            Maps = new ObservableCollection<SitePlannerMap>();

            this.ValueMappings = new ObservableCollection<SimValueMapping>();
        }


        /// <summary>
        /// Returns whether a specified SimGeo file is contained in this project
        /// </summary>
        /// <param name="simgeo">ResourceFileEntry of SimGeo file</param>
        /// <returns>true if specified file is contained in this project</returns>
        public bool ContainsGeometryModel(ResourceFileEntry simgeo)
        {
            return this.Buildings.FirstOrDefault(x => x.GeometryModelRes.ResourceFile.Key == simgeo.Key) != null;
        }

        /// <summary>
        /// Returns whether a specified GeoMap file is contained in this project
        /// </summary>
        /// <param name="geomap">ResourceFileEntry of GeoMap file</param>
        /// <returns>true if specified file is contained in this project</returns>
        public bool ContainsGeoMap(ResourceFileEntry geomap)
        {
            return this.Maps.FirstOrDefault(x => x.GeoMapRes.ResourceFile.Key == geomap.Key) != null;
        }
    }
}
