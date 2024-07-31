using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Interface used to identify possible child rules for the <see cref="SimDataMappingRuleComponent"/> class
    /// </summary>
    public interface ISimDataMappingComponentRuleChild : ISimDataMappingRuleBase
    {
        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A deep copy of the rule</returns>
        ISimDataMappingComponentRuleChild Clone();
    }

    /// <summary>
    /// The properties of a <see cref="SimComponent"/> that can be mapped
    /// </summary>
    public enum SimDataMappingComponentMappingProperties
    {
        /// <summary>
        /// The name of the component. (string)
        /// </summary>
        Name = 0,
        /// <summary>
        /// The slot of the component with extension for child components. (string)
        /// </summary>
        Slot = 1,
        /// <summary>
        /// The local Id of the component. (int)
        /// </summary>
        Id = 2
    }

    /// <summary>
    /// Enumeration describing which elements should be traversed by the <see cref="SimDataMappingRuleComponent"/> rule
    /// </summary>
    [Flags]
    public enum SimDataMappingRuleTraversalStrategy
    {
        /// <summary>
        /// Child components are traversed
        /// </summary>
        Subtree = 1,
        /// <summary>
        /// Referenced components are traversed
        /// </summary>
        References = 2,
        /// <summary>
        /// Both, child components and referenced components are traversed
        /// </summary>
        SubtreeAndReferences = Subtree | References,
    }

    /// <summary>
    /// Mapping rule for <see cref="SimComponent"/>
    /// </summary>
    public class SimDataMappingRuleComponent :
        SimDataMappingRuleBase<SimDataMappingComponentMappingProperties, SimDataMappingFilterComponent>,
        ISimDataMappingComponentRuleChild, ISimDataMappingFaceRuleChild, ISimDataMappingVolumeRuleChild, ISimDataMappingInstanceRuleChild
    {
        /// <summary>
        /// Specifies which elements should be traversed
        /// </summary>
        public SimDataMappingRuleTraversalStrategy TraversalStrategy { get; set; } = SimDataMappingRuleTraversalStrategy.Subtree;

        /// <summary>
        /// The child rules
        /// </summary>
        public ObservableCollection<ISimDataMappingComponentRuleChild> Rules { get; } = new ObservableCollection<ISimDataMappingComponentRuleChild>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingRuleComponent"/> class
        /// </summary>
        /// <param name="sheetName">The name of the worksheet</param>
        public SimDataMappingRuleComponent(string sheetName) : base(sheetName) { }

        /// <inheritdoc />
        public override void Execute(object rootObject, SimTraversalState state, SimMappedData data)
        {
            if (rootObject is SimComponent rootComp)
            {
                if (!state.VisitedObjects.Contains(rootComp)) //Prevent double matches
                {
                    state.VisitedObjects.Add(rootComp);

                    if (state.IncludeRoot && Filter.All(f => f.Match(rootComp)))
                        HandleMatch(rootComp, rootComp.Slots[0].Target, null, state, data);
                    else
                        Traverse(rootComp, state, data);

                    state.VisitedObjects.Remove(rootComp);
                }
                else if (!state.IncludeRoot) //Needed because executing a SubRule otherwise excludes the root node
                {
                    Traverse(rootComp, state, data);
                }
            }
            else if (rootObject is SimChildComponentEntry rootEntry)
            {
                if (!state.VisitedObjects.Contains(rootEntry.Component))
                {
                    state.VisitedObjects.Add(rootEntry.Component);

                    if (Filter.All(f => f.Match(rootEntry)) && state.Depth >= 0) //Depth is -1 when child rules are executed on their root
                        HandleMatch(rootEntry.Component, rootEntry.Slot.SlotBase.Target, rootEntry.Slot.SlotExtension, state, data);
                    else
                        Traverse(rootEntry.Component, state, data);

                    state.VisitedObjects.Remove(rootEntry.Component);
                }
            }
            else if (rootObject is SimComponentReference rootReference)
            {
                if (!state.VisitedObjects.Contains(rootReference.Target))
                {
                    state.VisitedObjects.Add(rootReference.Target);

                    if (Filter.All(f => f.Match(rootReference)) && state.Depth >= 0) //Depth is -1 when child rules are executed on their root
                        HandleMatch(rootReference.Target, rootReference.Slot.SlotBase.Target, rootReference.Slot.SlotExtension, state, data);
                    else
                        Traverse(rootReference.Target, state, data);

                    state.VisitedObjects.Remove(rootReference.Target);
                }
            }
            else if (rootObject is SimComponentInstance instance)
            {
                if (!state.VisitedObjects.Contains(instance.Component) &&
                    Filter.All(f => f.Match(instance.Component)))
                {
                    state.VisitedObjects.Add(instance.Component);

                    HandleMatch(instance.Component,
                        instance.Component.ParentContainer != null ? instance.Component.ParentContainer.Slot.SlotBase.Target : instance.Component.Slots[0].Target,
                        null, state, data);

                    state.VisitedObjects.Remove(instance.Component);
                }
            }
            else if (rootObject is BaseGeometry geometry)
            {
                var exchange = geometry.ModelGeometry.Model.Exchange;

                foreach (var placement in exchange.GetPlacements(geometry))
                {
                    if (state.MatchCount >= this.MaxMatches)
                        break;

                    if (!state.VisitedObjects.Contains(placement.Instance) &&
                        !state.VisitedObjects.Contains(placement.Instance.Component) &&
                        Filter.All(f => f.Match(placement.Instance.Component)))
                    {
                        state.VisitedObjects.Add(placement.Instance);
                        state.VisitedObjects.Add(placement.Instance.Component);

                        HandleMatch(placement.Instance.Component,
                               placement.Instance.Component.ParentContainer != null ? placement.Instance.Component.ParentContainer.Slot.SlotBase.Target : placement.Instance.Component.Slots[0].Target,
                            null, state, data);

                        state.VisitedObjects.Remove(placement.Instance.Component);
                        state.VisitedObjects.Remove(placement.Instance);
                    }
                    else
                    {
                        state.VisitedObjects.Add(placement.Instance);
                        state.VisitedObjects.Add(placement.Instance.Component);

                        Traverse(placement.Instance.Component, state, data);

                        state.VisitedObjects.Remove(placement.Instance.Component);
                        state.VisitedObjects.Remove(placement.Instance);
                    }
                }
            }
            else
                throw new NotSupportedException("Invalid root object type");
        }

        private void Traverse(SimComponent comp, SimTraversalState state, SimMappedData data)
        {
            state.Depth++;

            if (state.Depth < this.MaxDepth)
            {
                //Check if any child component matches
                if (TraversalStrategy.HasFlag(SimDataMappingRuleTraversalStrategy.Subtree))
                {
                    foreach (var child in comp.Components.Where(x => x.Component != null))
                    {
                        if (state.MatchCount >= this.MaxMatches)
                            break;
                        Execute(child, state, data);
                    }
                }

                if (TraversalStrategy.HasFlag(SimDataMappingRuleTraversalStrategy.References))
                {
                    foreach (var child in comp.ReferencedComponents.Where(x => x.Target != null))
                    {
                        if (state.MatchCount >= this.MaxMatches)
                            break;
                        Execute(child, state, data);
                    }
                }
            }

            state.Depth--;
        }

        private void HandleMatch(SimComponent comp, SimTaxonomyEntry firstSlot, string extension, SimTraversalState state, SimMappedData data)
        {
            //Advance position for this rule
            AdvanceReferencePoint(state);

            //Write properties
            WriteProperties(state, property =>
            {
                //Store property
                switch (property)
                {
                    case SimDataMappingComponentMappingProperties.Id:
                        data.AddData(this.SheetName, state.CurrentPosition, (int)comp.Id.LocalId, this);
                        break;
                    case SimDataMappingComponentMappingProperties.Name:
                        data.AddData(this.SheetName, state.CurrentPosition, comp.Name, this);
                        break;
                    case SimDataMappingComponentMappingProperties.Slot:
                        string combSlot = firstSlot.Key;
                        if (!string.IsNullOrEmpty(extension))
                            combSlot = combSlot + "_" + extension;
                        data.AddData(this.SheetName, state.CurrentPosition, combSlot, this);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported property");
                }
            });

            //Handle child rules
            ExecuteChildRules(this.Rules, comp, state, data);
        }

        /// <inheritdoc />
        protected override void OnToolChanged()
        {
            foreach (var childRule in this.Rules)
                childRule.Tool = this.Tool;
        }

        #region Clone

        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A deep copy of the rule</returns>
        public SimDataMappingRuleComponent Clone()
        {
            var copy = new SimDataMappingRuleComponent(this.SheetName)
            {
                Name = this.Name,
                MaxMatches = this.MaxMatches,
                MaxDepth = this.MaxDepth,
                OffsetParent = this.OffsetParent,
                OffsetConsecutive = this.OffsetConsecutive,
                MappingDirection = this.MappingDirection,
                ReferencePointParent = this.ReferencePointParent,
                TraversalStrategy = this.TraversalStrategy,
            };

            copy.Properties.AddRange(this.Properties);
            copy.Filter.AddRange(this.Filter.Select(x => x.Clone()));

            copy.Rules.AddRange(this.Rules.Select(x => x.Clone()));

            return copy;
        }

        /// <inheritdoc />
        ISimDataMappingComponentRuleChild ISimDataMappingComponentRuleChild.Clone()
        {
            return Clone();
        }
        /// <inheritdoc />
        ISimDataMappingFaceRuleChild ISimDataMappingFaceRuleChild.Clone()
        {
            return this.Clone();
        }
        /// <inheritdoc />
        ISimDataMappingVolumeRuleChild ISimDataMappingVolumeRuleChild.Clone()
        {
            return this.Clone();
        }
        /// <inheritdoc />
        ISimDataMappingInstanceRuleChild ISimDataMappingInstanceRuleChild.Clone()
        {
            return this.Clone();
        }

        #endregion

        /// <inheritdoc />
        public override void RestoreDefaultTaxonomyReferences()
        {
            base.RestoreDefaultTaxonomyReferences();

            foreach (var childRule in Rules)
                childRule.RestoreDefaultTaxonomyReferences();
        }
    }
}
