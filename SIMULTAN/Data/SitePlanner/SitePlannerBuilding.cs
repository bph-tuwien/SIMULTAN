using SIMULTAN.Data.SimMath;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Represents a single building in a SitePlannerProject. Consists of at least 1 SimGeo file.
    /// </summary>
    public class SitePlannerBuilding : INotifyPropertyChanged
    {
        #region EVENTS: tracing

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// The project in which this building is contained
        /// </summary>
        public SitePlannerProject Project { get; internal set; }

        /// <summary>
        /// Identifier unique across project
        /// </summary>
        public ulong ID
        {
            get => id;
            set
            {
                var oldValue = id;
                id = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ID)));
            }
        }
        private ulong id;

        /// <summary>
        /// Resource reference for a geometry file (SimGeo)
        /// </summary>
        public ResourceReference GeometryModelRes
        {
            get => geometryModelRes;
            set
            {
                var oldValue = geometryModelRes;
                geometryModelRes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeometryModelRes)));
            }
        }
        private ResourceReference geometryModelRes;

        /// <summary>
        /// Custom color used for this building
        /// </summary>
        public SimColor CustomColor
        {
            get => customColor;
            set
            {
                var oldValue = customColor;
                customColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomColor)));
            }
        }
        private SimColor customColor = SimColor.FromRgb(255, 255, 255);

        /// <summary>
        /// Initializes a new instance of the SitePlannerBuilding class
        /// </summary>
        /// <param name="ID">Unique id</param>
        /// <param name="geometryModelRes">Resource reference to SimGeo file</param>
        public SitePlannerBuilding(ulong ID, ResourceReference geometryModelRes)
        {
            this.id = ID;
            this.geometryModelRes = geometryModelRes;
        }
    }
}
