using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// A SimObject that also contains a Name
    /// </summary>
    /// <typeparam name="TFactory"></typeparam>
    public abstract class SimNamedObject<TFactory> : SimObjectNew<TFactory> where TFactory : class, ISimManagedCollection
    {
        /// <summary>
        /// The name of the object. It can be any string.
        /// </summary>
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
        /// Creates a new Named SimObject
        /// </summary>
        public SimNamedObject() : base() { }

        /// <summary>
        /// Creates a new Named SimObject with an Id
        /// </summary>
        /// <param name="id">The id for this object</param>
        public SimNamedObject(SimId id) : base(id) { }

        /// <summary>
        /// Called when the name changes. Use this in derived classes to react to name changes
        /// </summary>
        protected virtual void OnNameChanged() { }
    }
}
