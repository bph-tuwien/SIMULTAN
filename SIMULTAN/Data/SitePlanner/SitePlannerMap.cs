using SIMULTAN.Projects;
using SIMULTAN.Utils.ElevationProvider;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Represents a single map in a SitePlannerProject. Consists of an image and a set of georeferences.
    /// </summary>
    public class SitePlannerMap : INotifyPropertyChanged
    {
        #region EVENTS: tracing

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Resource reference for a geometry file (SimGeo)
        /// </summary>
        public ResourceReference GeoMapRes
        {
            get => geoMapRes;
            set
            {
                var oldValue = geoMapRes;
                geoMapRes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeoMapRes)));
            }
        }
        private ResourceReference geoMapRes;

        /// <summary>
        /// The ElevationProvider associated with this map.
        /// </summary>
        public IBulkElevationProvider ElevationProvider
        {
            get => elevationProvider;
            set
            {
                if (elevationProvider != value)
                {
                    var oldValue = elevationProvider;
                    elevationProvider = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ElevationProvider)));
                }
            }
        }
        private IBulkElevationProvider elevationProvider;

        /// <summary>
        /// The grid cell size in meters.
        /// </summary>
        public int GridCellSize
        {
            get
            {
                return gridCellSize;
            }
            set
            {
                if (gridCellSize != value)
                {
                    int oldValue = GridCellSize;
                    gridCellSize = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GridCellSize)));
                }
            }
        }
        private int gridCellSize = 100;

        /// <summary>
        /// Provides the type name of the the ElevationProvider if set.
        /// Else can be set to perform lookup up of the ElevationProvider from the IElevationProviderService.
        /// Cannot be set if ElevationProvider is not null.
        /// </summary>
        public string ElevationProviderTypeName
        {
            get
            {
                if (elevationProvider != null)
                {
                    return elevationProvider.GetType().Name;
                }
                else if (elevationProviderTypeName != null)
                {
                    return elevationProviderTypeName;
                }

                return "";
            }
            set
            {
                if (elevationProvider != null)
                {
                    throw new NotSupportedException("Cannot set elevation provider type name if ElevationProvider is not null.");
                }
                elevationProviderTypeName = value;
            }
        }
        private string elevationProviderTypeName;


        /// <summary>
        /// Initializes a new instance of the SitePlannerMap class
        /// </summary>
        /// <param name="geoMapRes">Resource reference to GeoMap file</param>
        public SitePlannerMap(ResourceReference geoMapRes)
        {
            this.geoMapRes = geoMapRes;
        }
    }
}
