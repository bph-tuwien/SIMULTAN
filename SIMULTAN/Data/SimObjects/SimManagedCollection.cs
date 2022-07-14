using SIMULTAN.Projects;
using System;
using System.Collections.ObjectModel;

namespace SIMULTAN.Data
{
    /// <summary>
    /// Interface for all managed collections. Provides methods for getting notified about changes
    /// </summary>
    public interface ISimManagedCollection : ILocated
    {
        /// <summary>
        /// Invoked when the project's change status has changed
        /// </summary>
        event EventHandler<EventArgs> HasChangesChanged;

        /// <summary>
        /// Returns the time of the last modification to the collection
        /// </summary>
        DateTime LastChange { get; }


        /// <summary>
        /// The project data this collection belongs to.
        /// </summary>
        ProjectData ProjectData { get; }

        /// <summary>
        /// Returns True when the factory has changes
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        /// Has to be called by items in the collection when changes have happened
        /// </summary>
        void NotifyChanged();
    }

    /// <summary>
    /// Base class for all managed collections
    /// </summary>
    /// <typeparam name="ItemT"></typeparam>
    public abstract class SimManagedCollection<ItemT> : ObservableCollection<ItemT>, ISimManagedCollection
    {
        #region ILocated

        /// <summary>
        /// The project data this collection belongs to.
        /// </summary>
        public ProjectData ProjectData { get; }

        /// <inheritdoc />
        public IReferenceLocation CalledFromLocation
        {
            get { return this.calledFromLocation; }
            private set
            {
                if ((this.calledFromLocation == default && value != default) ||
                    (this.calledFromLocation != default && value == default) ||
                    (this.calledFromLocation != default && value != default && this.calledFromLocation.GlobalID != value.GlobalID))
                {
                    this.calledFromLocation = value;
                    this.OnCalledFromLocationChanged();
                }
            }
        }
        private IReferenceLocation calledFromLocation;
        /// <inheritdoc />
        public virtual void SetCallingLocation(IReferenceLocation callingLocation)
        {
            this.CalledFromLocation = callingLocation;
        }
        /// <inheritdoc />
        protected virtual void OnCalledFromLocationChanged() { }

        #endregion

        #region HasChanges

        /// <inheritdoc />
        public event EventHandler<EventArgs> HasChangesChanged;

        /// <inheritdoc />
        public DateTime LastChange { get; private set; } = DateTime.Now;

        /// <inheritdoc />
        public bool HasChanges
        {
            get => this.hasChanges;
            private set
            {
                if (this.hasChanges != value)
                {
                    this.hasChanges = value;
                    this.HasChangesChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        private bool hasChanges = false;

        /// <inheritdoc />
        public void NotifyChanged()
        {
            this.HasChanges = true;
            this.LastChange = DateTime.Now;
        }

        /// <summary>
        /// Resets the <see cref="HasChanges"/> property
        /// </summary>
        public void ResetChanges()
        {
            this.HasChanges = false;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the SimManagedCollection class
        /// </summary>
        /// <param name="owner">The project data this instance belongs to</param>
        public SimManagedCollection(ProjectData owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            this.ProjectData = owner;
        }
    }
}
