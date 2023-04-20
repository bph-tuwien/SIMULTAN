using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// A collection of root rules (<see cref="SimDataMappingRuleComponent"/>) in a <see cref="SimDataMappingTool"/>.
    /// Stores, in addition to the root rules, the mapping between rules and components.
    /// </summary>
    public class SimDataMappingRootRuleCollection : ObservableCollection<SimDataMappingRuleComponent>
    {
        private Dictionary<SimDataMappingRuleComponent, List<SimComponent>> mappedComponents 
            = new Dictionary<SimDataMappingRuleComponent, List<SimComponent>>();

        /// <summary>
        /// The data mapping tool to which this collection belongs
        /// </summary>
        public SimDataMappingTool Owner { get; }

        /// <summary>
        /// Invoked whenever the association between components and rules has changed
        /// </summary>
        public event EventHandler MappingChanged;

        #region ObservableCollection Overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, SimDataMappingRuleComponent item)
        {
            if (mappedComponents.ContainsKey(item))
                throw new ArgumentException("Item is already contained in the collection");

            this.mappedComponents.Add(item, new List<SimComponent>());
            SetValues(item);
            base.InsertItem(index, item);
        }
        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var item = this[index];

            UnsetValues(item);
            this.mappedComponents.Remove(item);
            base.RemoveItem(index);
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SimDataMappingRuleComponent item)
        {
            var oldItem = this[index];

            UnsetValues(oldItem);
            mappedComponents.Remove(oldItem);

            SetValues(item);
            mappedComponents.Add(item, new List<SimComponent>());

            base.SetItem(index, item);
        }
        /// <inheritdoc />
        protected override void ClearItems()
        {
            this.mappedComponents.Clear();

            foreach (var item in this)
                UnsetValues(item);
            base.ClearItems();
        }

        private void SetValues(SimDataMappingRuleComponent rule)
        {
            rule.Tool = this.Owner;
        }

        private void UnsetValues(SimDataMappingRuleComponent rule)
        {
            rule.Tool = null;
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingRootRuleCollection"/> class
        /// </summary>
        /// <param name="owner">The data mapping tool to which this collection belongs</param>
        public SimDataMappingRootRuleCollection(SimDataMappingTool owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            this.Owner = owner;
        }

        /// <summary>
        /// Adds an additional mapping between a component and a root mapping rule. The rule needs to be part of this collection.
        /// Mapping to child rules is currently not supported.
        /// </summary>
        /// <param name="rule">The rule that should be mapped to the component</param>
        /// <param name="component">The component which is associated with the rule</param>
        public void AddMapping(SimDataMappingRuleComponent rule, SimComponent component)
        {
            if (this.mappedComponents.TryGetValue(rule, out var comps))
            {
                if (!comps.Contains(component))
                    comps.Add(component);
            }
            else
                throw new ArgumentException("Rule is not part of the collection");

            this.MappingChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Adds additional mappings between components and a root mapping rule. The rule needs to be part of this collection.
        /// Mapping to child rules is currently not supported.
        /// </summary>
        /// <param name="rule">The rule that should be mapped to the component</param>
        /// <param name="components">The components which are associated with the rule</param>
        public void AddMappings(SimDataMappingRuleComponent rule, IEnumerable<SimComponent> components)
        {
            if (this.mappedComponents.TryGetValue(rule, out var comps))
            {
                foreach (var component in components)
                {
                    if (!comps.Contains(component))
                        comps.Add(component);
                }
            }
            else
                throw new ArgumentException("Rule is not part of the collection");

            this.MappingChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Removes a mapping between a rule and a component
        /// </summary>
        /// <param name="rule">The rule from which the mapping should be removed</param>
        /// <param name="component">The component that should be disassociated from the component</param>
        public void RemoveMapping(SimDataMappingRuleComponent rule, SimComponent component)
        {
            if (this.mappedComponents.TryGetValue(rule, out var comps))
                comps.Remove(component);

            this.MappingChanged?.Invoke(this, EventArgs.Empty);
        }
        /// <summary>
        /// Returns all components mapped to a rule
        /// </summary>
        /// <param name="rule">The rule to query</param>
        /// <returns>The components associated with the rule</returns>
        public IEnumerable<SimComponent> GetMappings(SimDataMappingRuleComponent rule)
        {
            if (this.mappedComponents.TryGetValue(rule, out var comps))
                return comps;
            else
                return Enumerable.Empty<SimComponent>();
        }

        /// <summary>
        /// Returns all mappings in this collection
        /// </summary>
        public IDictionary<SimDataMappingRuleComponent, List<SimComponent>> Mappings => mappedComponents;
    }
}
