using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using static Assimp.Metadata;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Data types that are supported by the filtering methods.
    /// This enumeration is only used to serialize filter values.
    /// </summary>
    public enum SimDataMappingFilterType
    {
        /// <summary>
        /// A <see cref="System.String"/>
        /// </summary>
        String = 0,
        /// <summary>
        /// A <see cref="System.Text.RegularExpressions.Regex"/>
        /// </summary>
        Regex = 1,
        /// <summary>
        /// A <see cref="SimTaxonomyEntry"/>
        /// </summary>
        TaxonomyEntry = 2,
        /// <summary>
        /// A <see cref="SimSlot"/>
        /// </summary>
        Slot = 3,
        /// <summary>
        /// A <see cref="SimInstanceType"/>
        /// </summary>
        InstanceType = 4,
        /// <summary>
        /// A <see cref="System.Boolean"/>
        /// </summary>
        Boolean = 5,
        /// <summary>
        /// A <see cref="SimInfoFlow"/>
        /// </summary>
        InfoFlow = 6,
        /// <summary>
        /// A <see cref="SimCategory"/>
        /// </summary>
        Category = 7,
        /// <summary>
        /// A <see cref="System.Int32"/>
        /// </summary>
        Integer = 8,
        /// <summary>
        /// A <see cref="SimDataMappingFaceType"/>
        /// </summary>
        WallType = 9,
        /// <summary>
        /// Used whenever the <see cref="SimDataMappingFilterBase.Value"/> is Null
        /// </summary>
        Null = 10,
    }

    /// <summary>
    /// Base class for data mapping rule filter
    /// </summary>
    public abstract class SimDataMappingFilterBase : INotifyPropertyChanged
    {
        /// <summary>
        /// The value that the property should be filtered with
        /// </summary>
        public object Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;

                    if (value is SimSlot slot)
                        slot.SlotBase.SetDeleteAction(TaxonomyEntryDeleted);
                    else if (value is SimTaxonomyEntryReference tref)
                        tref.SetDeleteAction(TaxonomyEntryDeleted);

                    NotifyPropertyChanged();
                }
            }
        }
        private object value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingFilterBase"/> class
        /// </summary>
        /// <param name="value">The value of the property</param>
        public SimDataMappingFilterBase(object value)
        {
            this.Value = value;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> events
        /// </summary>
        /// <param name="property">The name of the property</param>
        protected void NotifyPropertyChanged([CallerMemberName] string property = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        private void TaxonomyEntryDeleted(SimTaxonomyEntry caller)
        {
            this.Value = null;
        }

        /// <summary>
        /// Looks up taxonomy entries for default slot by their name.
        /// Do this if the default taxonomies changed, could mean that the project is migrated.
        /// </summary>
        /// <param name="projectData">The project data in which the taxonomy entry is found</param>
        internal void RestoreDefaultTaxonomyReferences(ProjectData projectData)
        {
            if (Value is SimTaxonomyEntryReference tref)
            {
                var entry = projectData.IdGenerator.GetById<SimTaxonomyEntry>(tref.TaxonomyEntryId);
                this.Value = new SimTaxonomyEntryReference(entry);
            }
            else if (Value is SimSlot slot)
            {
                var entry = projectData.IdGenerator.GetById<SimTaxonomyEntry>(slot.SlotBase.TaxonomyEntryId);
                this.Value = new SimSlot(new SimTaxonomyEntryReference(entry), slot.SlotExtension);
            }
        }
    }

    /// <summary>
    /// Base class for data mapping rule filter
    /// </summary>
    /// <typeparam name="TPropertyEnum">The filter property enumeration</typeparam>
    public abstract class SimDataMappingFilterBase<TPropertyEnum> : SimDataMappingFilterBase
    {
        /// <summary>
        /// The property that should be filtered
        /// </summary>
        public TPropertyEnum Property { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingFilterBase{TPropertyEnum}"/> class
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public SimDataMappingFilterBase(TPropertyEnum property, object value) : base(value)
        {
            this.Property = property;
        }

        /// <summary>
        /// Clones the filter value. Needs to be implemented for all supported types
        /// </summary>
        /// <param name="filterValue">The filter value</param>
        /// <returns>A copy of the filter value</returns>
        protected static object CloneFilterValue(object filterValue)
        {
            switch (filterValue)
            {
                case SimTaxonomyEntryReference tref:
                    return new SimTaxonomyEntryReference(tref);
                case SimSlot slot:
                    return new SimSlot(slot);
                default:
                    return filterValue;
            }
        }
    }
}
