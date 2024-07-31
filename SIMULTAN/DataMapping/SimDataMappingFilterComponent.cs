using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Properties that can be filtered in a <see cref="SimComponent"/>
    /// </summary>
    public enum SimDataMappingComponentFilterProperties
    {
        /// <summary>
        /// Filter is applied to the name of the component. 
        /// Supports string, Regex or <see cref="SimTaxonomyEntryReference"/>.
        /// </summary>
        Name = 0,
        /// <summary>
        /// Filter is applied to the slot of a component.
        /// Supports <see cref="SimTaxonomyEntryReference"/>. Child components and references also support <see cref="SimSlot"/>.
        /// </summary>
        Slot = 1,
        /// <summary>
        /// Filter is applied to <see cref="SimInstanceState.IsRealized"/>.
        /// Supports bool
        /// </summary>
        InstanceIsRealized = 2,
        /// <summary>
        /// Filter is applied to the instance type of a component
        /// Supports <see cref="SimInstanceType"/>.
        /// </summary>
        InstanceType = 3,
    }

    /// <summary>
    /// Filter for components, child components and component references. Used by the <see cref="SimDataMappingRuleComponent"/>
    /// </summary>
    public class SimDataMappingFilterComponent : SimDataMappingFilterBase<SimDataMappingComponentFilterProperties>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingFilterComponent"/> class
        /// </summary>
        /// <param name="property">The property that should be filtered</param>
        /// <param name="value">The value the property is compared to. See description of <see cref="SimDataMappingComponentFilterProperties"/>
        /// to see which value types are supported</param>
        public SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties property, object value)
            : base(property, value) { }

        /// <summary>
        /// Creates a deep copy of the filter. Uses the <see cref="SimDataMappingFilterBase{TPropertyEnum}.CloneFilterValue(object)"/> method to clone the filter value.
        /// </summary>
        /// <returns>A deep copy of the filter</returns>
        public SimDataMappingFilterComponent Clone()
        {
            return new SimDataMappingFilterComponent(this.Property, CloneFilterValue(this.Value));
        }

        /// <summary>
        /// Returns True when the filtered object matches the filter
        /// </summary>
        /// <param name="matchObject">The object the filter should be applied to</param>
        /// <returns>True when the filter matches the object, otherwise False</returns>
        public bool Match(SimComponent matchObject)
        {
            //Get property
            switch (this.Property)
            {
                case SimDataMappingComponentFilterProperties.Name:
                    if (this.Value is string s)
                        return matchObject.Name == s;
                    else if (this.Value is Regex r)
                        return r.IsMatch(matchObject.Name);
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingComponentFilterProperties.Slot:
                    if (this.Value is SimTaxonomyEntryReference tref)
                        return matchObject.Slots.Any(t => t.Target == tref.Target);
                    else if (this.Value is SimSlot slot)
                    {
                        if (matchObject.ParentContainer != null)
                        {
                            return matchObject.ParentContainer.Slot.SlotBase == slot.SlotBase && slot.SlotExtension == string.Empty;
                        }
                        return matchObject.Slots[0] == slot.SlotBase && slot.SlotExtension == string.Empty;
                    }

                    else if (this.Value == null)
                        return true;
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingComponentFilterProperties.InstanceType:
                    if (this.Value is SimInstanceType itype)
                        return matchObject.InstanceType == itype;
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingComponentFilterProperties.InstanceIsRealized:
                    if (this.Value is bool isRealized)
                        return matchObject.InstanceState.IsRealized == isRealized;
                    else
                        throw new NotSupportedException("Unsupported value type");
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns True when the filtered object matches the filter
        /// </summary>
        /// <param name="matchObject">The object the filter should be applied to</param>
        /// <returns>True when the filter matches the object, otherwise False</returns>
        public bool Match(SimChildComponentEntry matchObject)
        {
            switch (this.Property)
            {
                case SimDataMappingComponentFilterProperties.Slot:
                    if (this.Value is SimTaxonomyEntryReference tref)
                        return matchObject.Slot.SlotBase == tref;
                    else if (this.Value is SimSlot tslot)
                    {
                        if (matchObject.Slot.SlotBase != tslot.SlotBase)
                            return false;
                        return string.IsNullOrEmpty(tslot.SlotExtension) || (tslot.SlotExtension == matchObject.Slot.SlotExtension);
                    }
                    else if (this.Value == null)
                        return true;
                    else
                        throw new NotSupportedException("Unsupported value type");
                default:
                    return Match(matchObject.Component);
            }
        }

        /// <summary>
        /// Returns True when the filtered object matches the filter
        /// </summary>
        /// <param name="matchObject">The object the filter should be applied to</param>
        /// <returns>True when the filter matches the object, otherwise False</returns>
        public bool Match(SimComponentReference matchObject)
        {
            switch (this.Property)
            {
                case SimDataMappingComponentFilterProperties.Slot:
                    if (this.Value is SimTaxonomyEntryReference tref)
                        return matchObject.Slot.SlotBase == tref;
                    else if (this.Value is SimSlot tslot)
                        return (matchObject.Slot == tslot) || (string.IsNullOrEmpty(tslot.SlotExtension) && matchObject.Slot.SlotBase == tslot.SlotBase);
                    else if (this.Value == null)
                        return true;
                    else
                        throw new NotSupportedException("Unsupported value type");
                default:
                    return Match(matchObject.Target);
            }
        }
    }
}
