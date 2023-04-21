using SIMULTAN.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using static SIMULTAN.Data.Taxonomy.SimTaxonomyEntryReference;

namespace SIMULTAN.Data.Taxonomy
{

    /// <summary>
    /// Data class for a Taxonomy entry
    /// </summary>
    [DebuggerDisplay("[TaxonomyEntry] {Name}, {Key}")]
    public class SimTaxonomyEntry : SimNamedObject<SimTaxonomyCollection>, IComparable<SimTaxonomyEntry>, IComparable
    {
        //~SimTaxonomyEntry() { Console.WriteLine("~SimTaxonomyEntry"); }

        private bool isDeleted = false;

        /// <summary>
        /// Key of the taxonomy entry.
        /// Needs to be unique inside the taxonomy (before it is added there)
        /// Key is used to identify entries inside the taxonomy independent of the possibly localized name of the entry.
        /// the SimId is only used internally to restore references.
        /// </summary>
        public string Key
        {
            get => key;
            set
            {
                if (key != value)
                {
                    NotifyWriteAccess();
                    if (Taxonomy != null)
                        Taxonomy.UnregisterEntry(this);

                    key = value;

                    if (Taxonomy != null)
                    {
                        Taxonomy.RegisterEntry(this);
                    }

                    NotifyPropertyChanged(nameof(Key));
                }
            }
        }
        private string key;

        /// <summary>
        /// The <see cref="SimTaxonomyEntry"/> child entries
        /// </summary>
        public SimChildTaxonomyEntryCollection Children { get; }

        /// <summary>
        /// The parent entry. Null if it is a root entry in the taxonomy.
        /// </summary>
        public SimTaxonomyEntry Parent
        {
            get => parent;
            internal set
            {
                if (parent != value)
                {
                    NotifyWriteAccess();
                    parent = value;
                    NotifyPropertyChanged(nameof(Parent));
                }
            }
        }
        private SimTaxonomyEntry parent;

        /// <summary>
        /// The <see cref="SimTaxonomy"/> this entry belongs to
        /// </summary>
        public SimTaxonomy Taxonomy
        {
            get => taxonomy;
            set
            {
                if (taxonomy != value)
                {
                    NotifyWriteAccess();

                    if (taxonomy != null)
                    {
                        if (Factory != null)
                        {
                            RemoveFromFactory(Factory);
                        }
                        taxonomy.UnregisterEntry(this);
                    }

                    taxonomy = value;

                    if (taxonomy != null)
                    {
                        taxonomy.RegisterEntry(this);
                        if (taxonomy.Factory != null)
                        {
                            AddToFactory(taxonomy.Factory);
                        }
                    }
                    Taxonomy?.Factory?.NotifyChanged();

                    foreach (var child in Children)
                        child.Taxonomy = taxonomy;

                    NotifyPropertyChanged(nameof(Taxonomy));
                }
            }
        }

        private ConditionalWeakTable<SimTaxonomyEntryReference, TaxonomyReferenceDeleter> references;

        private void AddToFactory(SimTaxonomyCollection factory)
        {
            // currently not in a factory
            if (Factory == null)
            {
                // needs a new Id
                if (Id == SimId.Empty)
                {
                    Id = factory.ProjectData.IdGenerator.NextId(this, factory.CalledFromLocation);
                }
                // was in a factory before because it has the same GlobalID, so set the id location again
                // (was moved inside the taxonomy)
                else if (factory.CalledFromLocation.GlobalID != Guid.Empty &&
                    Id.GlobalId == factory.CalledFromLocation.GlobalID)
                {
                    Id = new SimId(factory.CalledFromLocation, Id.LocalId);
                }
                else
                {
                    if (factory.IsLoading)
                    {
                        Id = new SimId(factory.CalledFromLocation, Id.LocalId);
                        factory.ProjectData.IdGenerator.Reserve(this, Id);
                    }
                    else
                    {
                        throw new NotSupportedException("Existing Ids may only be used during a loading operation");
                    }
                }

                Factory = factory;
            }
            else
            {
                if (Factory != factory)
                {
                    throw new ArgumentException("Taxonomy entries must be part of the same factory as the taxonomy");
                }

                // remove from parent collection cause it got moved to another one
                if (Parent != null)
                {
                    Parent.Children.RemoveWithoutDelete(this);
                }
                else if (Taxonomy != null)
                {
                    Taxonomy.Entries.RemoveWithoutDelete(this);
                }

            }
        }

        private void RemoveFromFactory(SimTaxonomyCollection factory)
        {
            factory.ProjectData.IdGenerator.Remove(this);
            // remove location from SimId but keey global/local Ids
            Id = new SimId(Id.GlobalId, Id.LocalId);
            Factory = null;

            if (!factory.IsMergeInProgress)
                OnIsBeingDeleted();
        }

        /// <summary>
        /// Emits the IsBeingDeletedEvent on each entry of the entry hierarchy
        /// </summary>
        public void OnIsBeingDeleted()
        {
            if (!isDeleted)
            {
                isDeleted = true;
                var values = references.GetType().GetProperty("Values", BindingFlags.Instance | BindingFlags.NonPublic);
                var actual = (ICollection<TaxonomyReferenceDeleter>)values.GetValue(references);
                foreach (var del in actual)
                {
                    del(this);
                }
                foreach (var child in Children)
                {
                    child.OnIsBeingDeleted();
                }
            }
        }

        private SimTaxonomy taxonomy;

        /// <summary>
        /// Creates a new Taxonomy entry
        /// </summary>
        /// <param name="key">The key of the taxonomy entry, cannot be null or empty</param>
        /// <param name="name">The name of the taxonomy entry</param>
        /// <param name="description">The description of the taxonomy entry</param>
        public SimTaxonomyEntry(string key, string name, string description) : this(key, name)
        {
            Description = description ?? "";
        }

        /// <summary>
        /// Creates a new Taxonomy entry
        /// </summary>
        /// <param name="key">The key of the taxonomy entry, cannot be null or empty</param>
        /// <param name="name">The name of the taxonomy entry</param>
        public SimTaxonomyEntry(string key, string name) : this()
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentException("Taxonomy entry key cannot be null or empty");
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Key = key;
            Name = name;
        }

        /// <summary>
        /// Creates a new Taxonomy entry
        /// </summary>
        public SimTaxonomyEntry() : base()
        {
            Children = new SimChildTaxonomyEntryCollection(this);
            references = new ConditionalWeakTable<SimTaxonomyEntryReference, TaxonomyReferenceDeleter>();
        }

        /// <summary>
        /// Creates a new Taxonomy entry with the given id
        /// </summary>
        /// <param name="id">The id</param>
        public SimTaxonomyEntry(SimId id) : base(id)
        {
            Children = new SimChildTaxonomyEntryCollection(this);
            references = new ConditionalWeakTable<SimTaxonomyEntryReference, TaxonomyReferenceDeleter>();
        }

        /// <summary>
        /// Adds a delegate that should be called on a taxonomy reference when the taxonomy entry gets deleted
        /// </summary>
        /// <param name="reference">The reference which should be notified</param>
        /// <param name="deleteEntry">The delegate that should be called when the entry gets deleted</param>
        internal void AddDeleteReference(SimTaxonomyEntryReference reference, TaxonomyReferenceDeleter deleteEntry)
        {
            if (reference == null)
                throw new ArgumentNullException(nameof(reference));
            if (deleteEntry == null)
                throw new ArgumentNullException(nameof(deleteEntry));
            try
            {
                references.Add(reference, deleteEntry);
            }
            catch (ArgumentException e)
            {
                throw new DeleteActionAlreadyRegisteredException("Delete action for taxonomy entry reference was already registered. Taxonomy entry references need to be cloned on reassigned if the deleter is in use.", e);
            }
        }

        /// <summary>
        /// Removes a taxonomy reference from the delete notification list
        /// </summary>
        /// <param name="reference"></param>
        internal void RemoveDeleteReference(SimTaxonomyEntryReference reference)
        {
            if (!isDeleted)
            {
                references.Remove(reference);
            }
        }

        /// <summary>
        /// Called when the factory has changed
        /// </summary>
        /// <param name="newValue">The old factory</param>
        /// <param name="oldValue">The new factory</param>
        internal void NotifyFactoryChanged(SimTaxonomyCollection newValue, SimTaxonomyCollection oldValue)
        {
            if (Taxonomy != null)
            {
                if (oldValue != null)
                    RemoveFromFactory(oldValue);
                if (newValue != null)
                    AddToFactory(newValue);

                Children.NotifyFactoryChanged(newValue, oldValue);
            }
        }

        /// <inheritdoc />
        protected override void NotifyPropertyChanged(string property)
        {
            base.NotifyPropertyChanged(property);

            if (this.Factory != null)
                this.Factory.NotifyTaxonomyEntryPropertyChanged(this, property);
        }

        /// <inheritdoc/>
        public int CompareTo(SimTaxonomyEntry other)
        {
            return String.Compare(Name, other.Name);
        }

        /// <inheritdoc/>
        protected override void NotifyWriteAccess()
        {
            if (Taxonomy != null && Taxonomy.IsReadonly)
                throw new AccessDeniedException("Cannot change read only taxonomy.");

            base.NotifyWriteAccess();
        }

        /// <summary>
        /// Tells the entry that the entries in it's child collection have changed
        /// </summary>
        internal void NotifyChildrenChanged()
        {
            NotifyPropertyChanged(nameof(Children));
        }

        /// <inheritdoc/>
        public int CompareTo(object obj)
        {
            if (obj is SimTaxonomyEntry entry)
            {
                return CompareTo(entry);
            }
            return 1;
        }
    }
}
