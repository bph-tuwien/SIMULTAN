using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects.ManagedFiles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
