using SIMULTAN.Data.Assets;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Exchange;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// EventArgs for the Replaced event
    /// </summary>
    public class GeometryModelReplacedEventArgs : EventArgs
    {
        /// <summary>
        /// The old geometry data
        /// </summary>
        public GeometryModelData OldGeometry { get; }
        /// <summary>
        /// The new geometry data
        /// </summary>
        public GeometryModelData NewGeometry { get; }

        /// <summary>
        /// Initializes a new instance of the GeometryModelReplacedEventArgs class
        /// </summary>
        /// <param name="oldGeometry">The old geometry data</param>
        /// <param name="newGeometry">The new geometry data</param>
        public GeometryModelReplacedEventArgs(GeometryModelData oldGeometry, GeometryModelData newGeometry)
        {
            this.OldGeometry = oldGeometry;
            this.NewGeometry = newGeometry;
        }
    }

    /// <summary>
    /// Contains data for the geometry of a SIMULTAN geometry file
    /// </summary>
    public class GeometryModel : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Returns the display name
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                NotifyPropertyChanged(nameof(Name));
            }
        }
        private string name;

        /// <summary>
        /// The geometry of this model. Invokes the Replaced event when changed
        /// </summary>
        public GeometryModelData Geometry
        {
            get { return geometryModel; }
            set
            {
                if (geometryModel != value)
                {
                    if (value.Model != null && value.Model != this)
                        throw new ArgumentException("Geometry has already been used with another model");

                    var old = geometryModel;
                    geometryModel = value;
                    geometryModel.Model = this;
                    NotifyReplaced(old, geometryModel);
                    NotifyPropertyChanged(nameof(Geometry));

                    OffsetQuery_GeometryInvalidated(this, null);
                }
            }
        }
        private GeometryModelData geometryModel;

        /// <summary>
        /// Gets or sets the permissions that are applicable to this model
        /// </summary>
        public OperationPermission Permissions
        {
            get
            {
                return permissions;
            }
            set
            {
                if (permissions != value)
                {
                    permissions = value;
                    NotifyPropertyChanged(nameof(Permissions));
                }

            }
        }
        private OperationPermission permissions;

        /// <summary>
        /// Stores the geometry file for this model
        /// </summary>
        public ResourceFileEntry File
        {
            get
            {
                return file;
            }
            set
            {
                if (file != value)
                {
                    file = value;
                    NotifyPropertyChanged(nameof(File));
                }
            }
        }
        private ResourceFileEntry file;

        /// <summary>
        /// A list of all models that are linked to this model
        /// </summary>
        public ObservableCollection<GeometryModel> LinkedModels { get; private set; }

        /// <summary>
        /// The <see cref="ComponentGeometryExchange"/> associated with this model
        /// </summary>
        public ComponentGeometryExchange Exchange
        {
            get
            {
                return offsetQuery;
            }
            set
            {
                if (offsetQuery != null)
                    offsetQuery.GeometryInvalidated -= this.OffsetQuery_GeometryInvalidated;

                offsetQuery = value;

                if (offsetQuery != null)
                    offsetQuery.GeometryInvalidated += this.OffsetQuery_GeometryInvalidated;
            }
        }

        private ComponentGeometryExchange offsetQuery = null;

        /// <summary>
        /// Stores the <see cref="SimValueMapping"/> that can be applied to this <see cref="GeometryModel"/>
        /// </summary>
        public ObservableCollection<SimValueMapping> ValueMappings { get; } = new ObservableCollection<SimValueMapping>();

        /// <summary>
        /// Stores the currently active value mapping of this model
        /// </summary>
        public SimValueMapping ActiveValueMapping
        {
            get { return activeValueMapping; }
            set
            {
                if (activeValueMapping != value)
                {
                    this.activeValueMapping = value;
                    NotifyPropertyChanged(nameof(ActiveValueMapping));
                }
            }
        }
        private SimValueMapping activeValueMapping = null;

        /// <summary>
        /// Stores whether the value mapping is currently active
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

        /// <summary>
        /// The tolerance used during cleanup operations
        /// </summary>
        public double CleanupTolerance { get { return cleanupTolerance; } set { this.cleanupTolerance = value; NotifyPropertyChanged(nameof(CleanupTolerance)); } }
        private double cleanupTolerance = 0.05;

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the Geometry of this model has changed
        /// </summary>
        public event EventHandler<GeometryModelReplacedEventArgs> Replaced;
        /// <summary>
        /// The PropertyChanged EventHandler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyReplaced(GeometryModelData oldModel, GeometryModelData newModel)
        {
            this.Replaced?.Invoke(this, new GeometryModelReplacedEventArgs(oldModel, newModel));
        }

        private void NotifyPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the GeometryModel class
        /// </summary>
        /// <param name="name">The display name</param>
        /// <param name="file">The geometry file to use</param>
        /// <param name="permissions">The permissions for this model</param>
        /// <param name="geometry">The geometry for this model</param>
        public GeometryModel(string name, ResourceFileEntry file, OperationPermission permissions, GeometryModelData geometry)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (geometry.Model != null && geometry.Model != this)
                throw new ArgumentException("Geometry has already been used with another model");

            this.Name = name;
            this.File = file;
            this.Permissions = permissions;
            this.Geometry = geometry;
            this.Geometry.Model = this;
            this.LinkedModels = new ObservableCollection<GeometryModel>();
        }


        private void OffsetQuery_GeometryInvalidated(object sender, System.Collections.Generic.IEnumerable<BaseGeometry> affected_geometry)
        {
            if (Geometry != null && Geometry.OffsetModel != null)
                Geometry.OffsetModel.OnGeometryInvalidated(affected_geometry);
        }

    }
}
