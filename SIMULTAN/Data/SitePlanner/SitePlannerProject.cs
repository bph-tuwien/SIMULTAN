using SIMULTAN.Data.Assets;
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
        #region EVENTS: tracing

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

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
        /// Value Mapping
        /// </summary>
        public ValueMap ValueMap
        {
            get => valueMap;
            set
            {
                if (valueMap != value)
                {
                    valueMap = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueMap)));
                }
            }
        }
        private ValueMap valueMap = null;

        /// <summary>
        /// Initializes a new instance of the SitePlannerProject class
        /// </summary>
        /// <param name="projectFile">ResourceFileEntry of SitePlanner file (spdxf)</param>
        public SitePlannerProject(ResourceFileEntry projectFile)
        {
            this.SitePlannerFile = projectFile;

            Buildings = new SitePlannerBuildingCollection(this);
            Maps = new ObservableCollection<SitePlannerMap>();

            this.ValueMap = new ValueMap();
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
