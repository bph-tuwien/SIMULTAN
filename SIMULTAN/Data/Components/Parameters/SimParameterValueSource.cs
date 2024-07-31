using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Base class for all parameter value sources.
    /// Value sources allow the parameter value to be propagated from some other source. Examples are
    /// Parameters which are attached to MultiValues (<see cref="SimMultiValueParameterSource"/> and Parameters which receive their parameter from
    /// Geometry <see cref="SimGeometryParameterSource"/>.
    /// </summary>
    public abstract class SimParameterValueSource : IDisposable, INotifyPropertyChanged
    {
        /// <summary>
        /// The parameter which is propagated by this ValueSource
        /// </summary>
        public virtual SimBaseParameter TargetParameter
        {
            get { return targetParameter; }
            internal set
            {
                targetParameter = value;
            }
        }
        /// <summary>
        /// The parameter which is propagated by this ValueSource
        /// </summary>
        protected SimBaseParameter targetParameter = null;

        /// <summary>
        /// Restores references after loading the project. Override if needed.
        /// </summary>
        /// <param name="idGenerator">The id generator to lookup references.</param>
        internal virtual void RestoreReferences(SimIdGenerator idGenerator)
        { }

        #region IDisposable

        /// <summary>
        /// Stores whether this instance has been disposed
        /// </summary>
        protected bool IsDisposed { get; private set; } = false;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
        /// <summary>
        /// Called when the instance is disposed
        /// </summary>
        /// <param name="isDisposing">True when called from the Dispose() method, False when called from the finalizer</param>
        protected virtual void Dispose(bool isDisposing)
        {
            IsDisposed = true;
        }

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the value of this pointer changes. 
        /// This can happen either because the addressing is changed or because the ValueField has changed.
        /// </summary>
        public event EventHandler ValueChanged;

        /// <summary>
        /// Invokes the ValueChanged event
        /// </summary>
        protected virtual void NotifyValueChanged()
        {
            this.ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies that a property changed.
        /// </summary>
        /// <param name="propertyName">The property name</param>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (!String.IsNullOrEmpty(propertyName))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        /// <summary>
        /// Creates a copy of this pointer
        /// </summary>
        /// <returns></returns>
        public abstract SimParameterValueSource Clone();
        /// <summary>
        /// Called when the component of the TargetParameter has changed.
        /// </summary>
        /// <param name="newComponent"></param>
        internal virtual void OnParameterComponentChanged(SimComponent newComponent) { }

        /// <summary>
        /// Restores all taxonomy entry references after the default taxonomies were updated.
        /// </summary>
        /// <param name="project">The ProjectData</param>
        public virtual void RestoreDefaultTaxonomyReferences(ProjectData project)
        { }
    }
}