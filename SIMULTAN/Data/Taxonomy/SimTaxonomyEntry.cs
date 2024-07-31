using SIMULTAN.Exceptions;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static SIMULTAN.Data.Taxonomy.SimTaxonomyEntryReference;

namespace SIMULTAN.Data.Taxonomy
{

    /// <summary>
    /// Data class for a Taxonomy entry
    /// </summary>
    [DebuggerDisplay("[TaxonomyEntry] {Key}")]
    public class SimTaxonomyEntry : SimObjectNew<SimTaxonomyCollection>, ISimTaxonomyElement, IComparable<SimTaxonomyEntry>, IComparable
    {
        #region Fields

        private bool isDeleted = false;

        private ConditionalWeakTable<SimTaxonomyEntryReference, TaxonomyReferenceDeleter> references;

        #endregion

        #region Properties

        /// <summary>
        /// Localization of the taxonomy entry
        /// </summary>
        public SimTaxonomyLocalization Localization { get; }

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
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentException("Taxonomy entry key cannot be null or empty");

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
                        // add missing languages
                        taxonomy.Languages.ForEach(x => Localization.AddLanguage(x));
                        // add languages not in taxonomy
                        foreach (var lang in this.Localization.Entries.Keys.ToList())
                        {
                            if (!taxonomy.Languages.Contains(lang))
                            {
                                throw new NotSupportedException(string.Format("Entry contains language {0} that is not supported by the Taxonomy",
                                    lang.DisplayName));
                            }
                        }
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
        private SimTaxonomy taxonomy;

        #endregion

        #region .CTOR

        /// <summary>
        /// Creates a new Taxonomy entry with a default translation
        /// </summary>
        /// <param name="id">Id of the entry</param>
        /// <param name="key">The key of the taxonomy entry, cannot be null or empty</param>
        /// <param name="name">The name of the taxonomy entry</param>
        /// <param name="description">The description of the taxonomy entry</param>
        /// <param name="culture">The culture to set the name and description for, used the InvariantCulture if null</param>
        public SimTaxonomyEntry(SimId id, string key, string name, string description = "", CultureInfo culture = null) : base(id)
        {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentException("Taxonomy entry key cannot be null or empty");

            this.Id = id;
            this.Key = key;

            this.Children = new SimChildTaxonomyEntryCollection(this);
            this.references = new ConditionalWeakTable<SimTaxonomyEntryReference, TaxonomyReferenceDeleter>();
            this.Localization = new SimTaxonomyLocalization(this);

            if (name != null && description != null)
            {
                culture = culture ?? CultureInfo.InvariantCulture;
                Localization.AddLanguage(culture);
                Localization.SetLanguage(new SimTaxonomyLocalizationEntry(culture, name, description));
            }
        }

        /// <summary>
        /// Creates a new Taxonomy entry with a default translation
        /// </summary>
        /// <param name="key">The key of the taxonomy entry, cannot be null or empty</param>
        /// <param name="name">The name of the taxonomy entry</param>
        /// <param name="description">The description of the taxonomy entry</param>
        /// <param name="culture">The culture to set the name and description for, used the InvariantCulture if null</param>
        public SimTaxonomyEntry(string key, string name, string description = "", CultureInfo culture = null)
            : this(SimId.Empty, key, name, description, culture) { }

        /// <summary>
        /// Creates a new Taxonomy entry with the given id
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="key">The key of the taxonomy entry, cannot be null or empty</param>
        public SimTaxonomyEntry(SimId id, string key) : this(id, key, null, null, null) { }

        /// <summary>
        /// Creates a new Taxonomy entry
        /// </summary>
        /// <param name="key">The key of the taxonomy entry, cannot be null or empty</param>
        public SimTaxonomyEntry(string key) : this(SimId.Empty, key) { }

        #endregion

        #region Methods

        /// <inheritdoc />
        public void NotifyLocalizationChanged()
        {
            Factory?.NotifyTaxonomyEntryPropertyChanged(this, nameof(Localization));
        }

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
            // remove location from SimId but key global/local Ids
            Id = new SimId(Id.GlobalId, Id.LocalId);

            if (!factory.IsMergeInProgress)
                OnIsBeingDeleted();

            Factory = null;
        }

        /// <summary>
        /// Emits the IsBeingDeletedEvent on each entry of the entry hierarchy
        /// </summary>
        public void OnIsBeingDeleted()
        {
            if (!isDeleted)
            {
                isDeleted = true;
                if (!this.Factory.IsClosing)
                {
                    foreach (var entry in references)
                    {
                        entry.Value(this);
                    }
                }
                foreach (var child in Children)
                {
                    child.OnIsBeingDeleted();
                }
            }
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
        [Obsolete("Use compare manually using localized names")]
        public int CompareTo(SimTaxonomyEntry other)
        {
            if (Debugger.IsAttached)
                throw new NotImplementedException("Should not be used anymore, replace with localized compare");

            return 0;
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
        [Obsolete("Use localized comparison")]
        public int CompareTo(object obj)
        {
            if (Debugger.IsAttached)
                throw new NotImplementedException("Should not be used anymore, replace with localized compare");

            if (obj is SimTaxonomyEntry entry)
            {
                return CompareTo(entry);
            }
            return 1;
        }

        #endregion

        /// <summary>
        /// Returns true when the entry key as well as the taxonomy key of this entry match the parameters
        /// </summary>
        /// <param name="taxonomyKey">The key of the taxonomy</param>
        /// <param name="entryKey">The key of the entry</param>
        /// <returns>Returns true when the entry key as well as the taxonomy key of this entry match the parameters, otherwise False</returns>
        public bool Matches(string taxonomyKey, string entryKey)
        {
            return this.Key == entryKey && this.Taxonomy != null && this.Taxonomy.Key == taxonomyKey;
        }
    }
}
