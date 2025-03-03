using Assimp;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Interface used to identify possible child rules for the <see cref="SimDataMappingRuleInstance"/> class
    /// </summary>
    public interface ISimDataMappingInstanceRuleChild : ISimDataMappingRuleBase
    {
        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A deep copy of the rule</returns>
        ISimDataMappingInstanceRuleChild Clone();
    }

    /// <summary>
    /// The properties of a <see cref="SimComponentInstance"/> that can be mapped
    /// </summary>
    public enum SimDataMappingInstanceMappingProperties
    {
        /// <summary>
        /// The name of the instance. (string)
        /// </summary>
        Name = 0,
        /// <summary>
        /// The local id of the instance. (int)
        /// </summary>
        Id = 1,
    }

    /// <summary>
    /// Mapping rule for <see cref="SimComponentInstance"/>
    /// </summary>
    public class SimDataMappingRuleInstance 
        : SimDataMappingRuleBase<SimDataMappingInstanceMappingProperties, SimDataMappingFilterInstance>,
        ISimDataMappingComponentRuleChild, ISimDataMappingVolumeRuleChild, ISimDataMappingFaceRuleChild
    {
        /// <summary>
        /// The child rules
        /// </summary>
        public ObservableCollection<ISimDataMappingInstanceRuleChild> Rules { get; } = new ObservableCollection<ISimDataMappingInstanceRuleChild>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingRuleInstance"/> class
        /// </summary>
        /// <param name="sheetName">The name of the worksheet</param>
        public SimDataMappingRuleInstance(string sheetName) : base(sheetName) { }

        /// <inheritdoc />
        public override void Execute(object rootObject, SimTraversalState state, SimMappedData data)
        {
            if (rootObject is SimComponent rootComp)
            {
                foreach (var instance in rootComp.Instances)
                {
                    if (state.MatchCount > this.MaxMatches)
                        break;

                    if (!state.VisitedObjects.Contains(instance) &&
                        Filter.All(f => f.Match(instance)))
                    {
                        state.VisitedObjects.Add(instance);

                        HandleMatch(instance, state, data);

                        state.VisitedObjects.Remove(instance);
                    }
                }
            }
            else if (rootObject is BaseGeometry volume) //Only Face and Volume possible at the moment
            {
                var exchange = volume.ModelGeometry.Model.Exchange;

                foreach (var placement in exchange.GetPlacements(volume))
                {
                    if (!state.VisitedObjects.Contains(placement.Instance) &&
                        Filter.All(f => f.Match(placement.Instance)))
                    {
                        state.VisitedObjects.Add(placement.Instance);

                        HandleMatch(placement.Instance, state, data);

                        state.VisitedObjects.Remove(placement.Instance);
                    }
                }
            }
        }

        private void HandleMatch(SimComponentInstance instance, SimTraversalState state, SimMappedData data)
        {
            //Advance position for this rule
            AdvanceReferencePoint(state);

            WriteProperties(state, property =>
            {
                switch (property)
                {
                    case SimDataMappingInstanceMappingProperties.Name:
                        data.AddData(this.SheetName, state.CurrentPosition, instance.Name, this);
                        break;
                    case SimDataMappingInstanceMappingProperties.Id:
                        data.AddData(this.SheetName, state.CurrentPosition, instance.Id.LocalId, this);
                        break;
                }
            });

            //Handle child rules
            ExecuteChildRules(this.Rules, instance, state, data);
        }

        /// <inheritdoc />
        protected override void OnToolChanged()
        {
            foreach (var r in this.Rules)
                r.Tool = Tool;
        }

        #region Clone

        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A deep copy of the rule</returns>
        public SimDataMappingRuleInstance Clone()
        {
            var copy = new SimDataMappingRuleInstance(this.SheetName)
            {
                Name = this.Name,
                MaxMatches = this.MaxMatches,
                MaxDepth = this.MaxDepth,
                OffsetParent = this.OffsetParent,
                OffsetConsecutive = this.OffsetConsecutive,
                MappingDirection = this.MappingDirection,
                ReferencePointParent = this.ReferencePointParent,
            };

            copy.Properties.AddRange(this.Properties);
            copy.Filter.AddRange(this.Filter.Select(x => x.Clone()));

            copy.Rules.AddRange(this.Rules.Select(x => x.Clone()));

            return copy;
        }

        /// <inheritdoc />
        ISimDataMappingComponentRuleChild ISimDataMappingComponentRuleChild.Clone()
        {
            return this.Clone();
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
