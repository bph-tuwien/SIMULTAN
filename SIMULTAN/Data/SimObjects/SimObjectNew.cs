using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// Base class for all Simultan Objects
    /// Provides basic functionality for notifying the factory about changes and provides methods for access checking
    /// Note, that all properties require Write access to the owning object.
    /// </summary>
    /// <remarks>The definition is split into two classes (SimObjectNew, SimObjectNew&lt;T&gt;) to have a non generic base class
    /// for storing references to arbitrary SIMULTAN objects</remarks>
    public abstract class SimObjectNew : IReference, INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// The Id of this object. Usually, Simultan objects get their Id assigned when they are plugged into a factory.
        /// </summary>
        public SimId Id { get { return id; } internal set { id = value; NotifyPropertyChanged(nameof(Id)); NotifyChanged(); } }
        private SimId id;

        /// <summary>
        /// Returns only the local id of the instance.
        /// </summary>
        [ExcelMappingProperty("SIM_OBJECT_LOCALID")]
        public long LocalID { get { return this.id.LocalId; } }
        /// <summary>
        /// Returns only the global location of the instance.
        /// </summary>
        public Guid GlobalID { get { return this.id.GlobalId; } }

        /// <summary>
        /// The name of the object. It can be any string.
        /// </summary>
        [ExcelMappingProperty("SIM_OBJECT_NAME")]
        public string Name
        {
            get { return this.name; }
            set
            {
                if (this.name != value)
                {
                    NotifyWriteAccess();
                    this.name = value;
                    OnNameChanged();
                    this.NotifyPropertyChanged(nameof(Name));
                    this.NotifyChanged();
                }
            }
        }
        private string name;

        /// <summary>
        /// The description of the object.
        /// </summary>
        [ExcelMappingProperty("SIM_OBJECT_DESCRIPTION")]
        public string Description
        {
            get { return this.description; }
            set
            {
                if (this.description != value)
                {
                    NotifyWriteAccess();
                    this.description = value;
                    this.NotifyPropertyChanged(nameof(Description));
                    this.NotifyChanged();
                }
            }
        }
        private string description;

        #endregion

        #region Events

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Invokes the PropertyChanged event
        /// </summary>
        /// <param name="property">Name of the property</param>
        protected void NotifyPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the SimObjectNew class
        /// </summary>
        /// <param name="id">The id for this instance</param>
        protected SimObjectNew(SimId id)
        {
            this.id = id;
        }

        /// <summary>
        /// Has to be called whenever the state of an object which is relevant for the factory has changed.
        /// This method should notifies factory about changes.
        /// </summary>
        protected abstract void NotifyChanged();
        /// <summary>
        /// Has to be called before! the state which requires write access should be changed.
        /// This method does nothing by default.
        /// 
        /// Inherited types may choose to implement this method to check for write access and to record writing accesses.
        /// When the current user does not have writing access, this method has to throw a AccessDeniedException.
        /// </summary>
        protected abstract void NotifyWriteAccess();
        /// <summary>
        /// Called when the name changes. Use this in derived classes to react to name changes
        /// </summary>
        protected virtual void OnNameChanged() { }
    }

    /// <summary>
    /// Base class for all SIMULTAN Objects which are handled by a factory (should be all of them)
    /// Provides basic functionality for notifying the factory about changes and provides methods for access checking
    /// Note, that all properties require Write access to the owning object.
    /// </summary>
    public abstract class SimObjectNew<TFactory> : SimObjectNew where TFactory : class, ISimManagedCollection
    {
        /// <summary>
        /// A reference to the managing class.
        /// </summary>
        public TFactory Factory
        {
            get { return factory; }
            internal set
            {
                if (factory != value)
                {
                    var old = this.factory;
                    this.factory = value;
                    OnFactoryChanged(factory, old);
                }
            }
        }
        private TFactory factory = null;

        /// <summary>
        /// Initializes a new instance of the SimObjectNew class
        /// </summary>
        public SimObjectNew() : base(SimId.Empty) { }
        /// <summary>
        /// Initializes a new instance of the SimObjectNew class
        /// </summary>
        /// <param name="id">The id for this object</param>
        public SimObjectNew(SimId id) : base(id) { }

        /// <inheritdoc />
        protected override void NotifyChanged()
        {
            Factory?.NotifyChanged();
        }
        /// <inheritdoc />
        protected override void NotifyWriteAccess() { }

        /// <summary>
        /// Notifies the object that the factor has changed. 
        /// Parameters may be null when either the old or new factory isn't assigned
        /// </summary>
        /// <param name="newFactory">The new factory</param>
        /// <param name="oldFactory">The old factory</param>
        protected virtual void OnFactoryChanged(TFactory newFactory, TFactory oldFactory) { }
    }
}
