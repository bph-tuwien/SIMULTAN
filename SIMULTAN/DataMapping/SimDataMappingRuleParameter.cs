using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// The properties of a <see cref="SimBaseParameter"/> that can be mapped
    /// </summary>
    public enum SimDataMappingParameterMappingProperties
    {
        /// <summary>
        /// The name of the parameter. (string)
        /// </summary>
        Name = 0,
        /// <summary>
        /// The local Id of the parameter. (int)
        /// </summary>
        Id = 1,
        /// <summary>
        /// The current value of the parameter. (double)
        /// </summary>
        Value = 2,
        /// <summary>
        /// The text value of the parameter. (string)
        /// </summary>
        Description = 3,
        /// <summary>
        /// The unit of the parameter. (string)
        /// </summary>
        Unit = 4,
        /// <summary>
        /// The minimum value of the string. (double)
        /// </summary>
        Min = 5,
        /// <summary>
        /// The maximum value of the string. (double)
        /// </summary>
        Max = 6
    }

    /// <summary>
    /// The range of values that this rule writes.
    /// </summary>
    public enum SimDataMappingParameterRange
    {
        /// <summary>
        /// The rule writes a single value.
        /// </summary>
        SingleValue = 0,
        /// <summary>
        /// The rule writes a row vector corresponding to the current row of a table
        /// </summary>
        CurrentRow = 1,
        /// <summary>
        /// The rule writes a column vector corresponding to the current column of a table
        /// </summary>
        CurrentColumn = 2,
        /// <summary>
        /// The rule writes a full table
        /// </summary>
        Table = 3,
    }

    /// <summary>
    /// Mapping rule for <see cref="SimBaseParameter"/>
    /// </summary>
    public class SimDataMappingRuleParameter :
        SimDataMappingRuleBase<SimDataMappingParameterMappingProperties, SimDataMappingFilterParameter>,
        ISimDataMappingComponentRuleChild, ISimDataMappingInstanceRuleChild
    {
        /// <summary>
        /// The value range that should be written. Only relevant when the parameter has a <see cref="SimMultiValueBigTable"/> attached
        /// </summary>
        public SimDataMappingParameterRange ParameterRange
        {
            get { return parameterRange; }
            set
            {
                if (parameterRange != value)
                {
                    parameterRange = value;
                    NotifyPropertyChanged();
                }
            }
        }
        private SimDataMappingParameterRange parameterRange = SimDataMappingParameterRange.SingleValue;


        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingRuleParameter"/> class
        /// </summary>
        /// <param name="sheetName">The name of the worksheet</param>
        public SimDataMappingRuleParameter(string sheetName) : base(sheetName) { }

        /// <inheritdoc />
        public override void Execute(object rootObject, SimTraversalState state, SimMappedData data)
        {
            //Cycle prevention can be skipped because this rule doesn't have children -> no cycles possible

            if (rootObject is SimComponent component)
            {
                foreach (var parameter in component.Parameters)
                {
                    if (state.MatchCount >= this.MaxMatches)
                        break;

                    if (Filter.All(f => f.Match(parameter)))
                        HandleMatch(parameter, state, data);
                }
            }
            else if (rootObject is SimComponentInstance instance)
            {
                foreach (var parameter in instance.Component.Parameters)
                {
                    if (state.MatchCount >= this.MaxMatches)
                        break;

                    if (Filter.All(f => f.Match(parameter)))
                        HandleInstanceMatch(parameter, instance, state, data);
                }
            }
        }

        private void HandleInstanceMatch(SimBaseParameter parameter, SimComponentInstance instance, SimTraversalState state, SimMappedData data)
        {
            AdvanceReferencePoint(state);

            WriteProperties(state,
                property => WriteInstanceMatchProperty(parameter, instance, property, state, data)
            );
        }

        private void HandleMatch(SimBaseParameter parameter, SimTraversalState state, SimMappedData data)
        {
            //Advance position for this rule
            AdvanceReferencePoint(state);

            WriteProperties(state, 
                property => WriteMatchProperty(parameter, property, state, data)
            );
        }

        private void WriteMatchProperty(SimBaseParameter parameter, SimDataMappingParameterMappingProperties property,
            SimTraversalState state, SimMappedData data)
        {
            switch (property)
            {
                case SimDataMappingParameterMappingProperties.Name:
                    data.AddData(this.SheetName, state.CurrentPosition, parameter.NameTaxonomyEntry.TextOrKey, this);
                    break;
                case SimDataMappingParameterMappingProperties.Id:
                    data.AddData(this.SheetName, state.CurrentPosition, (int)parameter.Id.LocalId, this);
                    break;
                case SimDataMappingParameterMappingProperties.Value:
                    if (parameter.ValueSource is SimMultiValueBigTableParameterSource tableSource)
                    {
                        RowColumnIndex startingPosition = state.CurrentPosition;

                        switch (this.ParameterRange)
                        {
                            case SimDataMappingParameterRange.SingleValue:
                                data.AddData(this.SheetName, state.CurrentPosition, parameter.Value, this);
                                break;
                            case SimDataMappingParameterRange.CurrentRow:
                                for (int i = 0; i < tableSource.Table.ColumnHeaders.Count; ++i)
                                {
                                    data.AddData(this.SheetName, state.CurrentPosition, tableSource.Table[tableSource.Row, i], this);
                                    if (i < tableSource.Table.ColumnHeaders.Count - 1)
                                        state.CurrentPosition += new RowColumnIndex(0, 1);                                        
                                }

                                //Reset positioning
                                if (this.MappingDirection == SimDataMappingDirection.Vertical) //Reset Column
                                    state.CurrentPosition = new RowColumnIndex(state.CurrentPosition.Row, startingPosition.Column);

                                break;
                            case SimDataMappingParameterRange.CurrentColumn:
                                for (int i = 0; i < tableSource.Table.RowHeaders.Count; ++i)
                                {
                                    data.AddData(this.SheetName, state.CurrentPosition, tableSource.Table[i, tableSource.Column], this);
                                    if (i < tableSource.Table.RowHeaders.Count - 1)
                                        state.CurrentPosition += new RowColumnIndex(1, 0);
                                }

                                //Reset positioning
                                if (this.MappingDirection == SimDataMappingDirection.Horizontal) //Reset Column
                                    state.CurrentPosition = new RowColumnIndex(startingPosition.Row, state.CurrentPosition.Column);

                                break;
                            case SimDataMappingParameterRange.Table:
                                for (int r = 0; r < tableSource.Table.RowHeaders.Count; ++r)
                                {
                                    for (int c = 0; c < tableSource.Table.ColumnHeaders.Count; ++c)
                                    {
                                        data.AddData(this.SheetName, state.CurrentPosition, tableSource.Table[r, c], this);
                                        if (c < tableSource.Table.ColumnHeaders.Count - 1)
                                            state.CurrentPosition += new RowColumnIndex(0, 1);
                                    }

                                    if (r < tableSource.Table.RowHeaders.Count - 1)
                                        state.CurrentPosition = new RowColumnIndex(state.CurrentPosition.Row + 1, startingPosition.Column);
                                }

                                //Reset positioning
                                if (this.MappingDirection == SimDataMappingDirection.Vertical) //Reset Column
                                    state.CurrentPosition = new RowColumnIndex(state.CurrentPosition.Row, startingPosition.Column);
                                else if (this.MappingDirection == SimDataMappingDirection.Horizontal) //Reset Column
                                    state.CurrentPosition = new RowColumnIndex(startingPosition.Row, state.CurrentPosition.Column);

                                break;
                        }
                    }
                    else //Always single value
                        data.AddData(this.SheetName, state.CurrentPosition, parameter.Value, this);

                    break;
                case SimDataMappingParameterMappingProperties.Description:
                    data.AddData(this.SheetName, state.CurrentPosition, parameter.Description, this);
                    break;
                case SimDataMappingParameterMappingProperties.Unit:
                    {
                        if (parameter is SimDoubleParameter dparam)
                            data.AddData(this.SheetName, state.CurrentPosition, dparam.Unit, this);
                        else if (parameter is SimIntegerParameter iparam)
                            data.AddData(this.SheetName, state.CurrentPosition, iparam.Unit, this);
                        else
                            data.AddData(this.SheetName, state.CurrentPosition, null, this);
                    }
                    break;
                case SimDataMappingParameterMappingProperties.Min:
                    {
                        if (parameter is SimDoubleParameter dparam)
                            data.AddData(this.SheetName, state.CurrentPosition, dparam.ValueMin, this);
                        else if (parameter is SimIntegerParameter iparam)
                            data.AddData(this.SheetName, state.CurrentPosition, iparam.ValueMin, this);
                        else
                            data.AddData(this.SheetName, state.CurrentPosition, null, this);
                    }
                    break;
                case SimDataMappingParameterMappingProperties.Max:
                    {
                        if (parameter is SimDoubleParameter dparam)
                            data.AddData(this.SheetName, state.CurrentPosition, dparam.ValueMax, this);
                        else if (parameter is SimIntegerParameter iparam)
                            data.AddData(this.SheetName, state.CurrentPosition, iparam.ValueMax, this);
                        else
                            data.AddData(this.SheetName, state.CurrentPosition, null, this);
                    }
                    break;
            }
        }
        private void WriteInstanceMatchProperty(SimBaseParameter parameter, SimComponentInstance instance, SimDataMappingParameterMappingProperties property,
            SimTraversalState state, SimMappedData data)
        {
            switch (property)
            {
                case SimDataMappingParameterMappingProperties.Value:
                    data.AddData(this.SheetName, state.CurrentPosition, instance.InstanceParameterValuesPersistent[parameter], this);
                    break;
                default:
                    WriteMatchProperty(parameter, property, state, data);
                    break;
            }
        }

        /// <inheritdoc />
        protected override void OnToolChanged() { }

        #region Clone

        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A deep copy of the rule</returns>
        public SimDataMappingRuleParameter Clone()
        {
            var copy = new SimDataMappingRuleParameter(this.SheetName)
            {
                Name = this.Name,
                MaxMatches = this.MaxMatches,
                MaxDepth = this.MaxDepth,
                OffsetParent = this.OffsetParent,
                OffsetConsecutive = this.OffsetConsecutive,
                MappingDirection = this.MappingDirection,
                ReferencePointParent = this.ReferencePointParent,
                ParameterRange = this.ParameterRange,
            };

            copy.Properties.AddRange(this.Properties);
            copy.Filter.AddRange(this.Filter.Select(x => x.Clone()));
            return copy;
        }

        /// <inheritdoc />
        ISimDataMappingComponentRuleChild ISimDataMappingComponentRuleChild.Clone()
        {
            return this.Clone();
        }
        /// <inheritdoc />
        ISimDataMappingInstanceRuleChild ISimDataMappingInstanceRuleChild.Clone()
        {
            return this.Clone();
        }

        #endregion
    }
}
