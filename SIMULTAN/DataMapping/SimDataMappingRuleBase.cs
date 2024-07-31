using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Describes the direction in which consecutive properties are written
    /// </summary>
    public enum SimDataMappingDirection
    {
        /// <summary>
        /// Properties are written in the same row, next column
        /// </summary>
        Horizontal = 0,
        /// <summary>
        /// Properties are written in the same column, next row
        /// </summary>
        Vertical = 1,
    }

    /// <summary>
    /// The reference position from which the next consecutive rule is calculating it's offset.
    /// The reference position is relative to the bounding box of all values written by the previous rule
    /// </summary>
    public enum SimDataMappingReferencePoint
    {
        /// <summary>
        /// The positioning for the next rule starts at the bottom-left of the bounding box of the previous rule
        /// </summary>
        BottomLeft = 0,
        /// <summary>
        /// The positioning for the next rule starts at the top-right of the bounding box of the previous rule
        /// </summary>
        TopRight = 1,
        /// <summary>
        /// The positioning for the next rule starts at the top-left of the bounding box of the previous rule
        /// </summary>
        TopLeft = 2,
    }

    /// <summary>
    /// Interface for all mapping rules
    /// </summary>
    public interface ISimDataMappingRuleBase : INotifyPropertyChanged
    {
        /// <summary>
        /// The tool to which this rule belongs
        /// </summary>
        SimDataMappingTool Tool { get; set; }

        /// <summary>
        /// The name of the rule
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// The name of the worksheet into which the data should be written. The current implementation expects all child rules to have
        /// the same SheetName as their parent. If not, offset calculation might be off.
        /// </summary>
        string SheetName { get; set; }

        /// <summary>
        /// The maximum number of objects that are found by this rule
        /// </summary>
        int MaxMatches { get; set; }
        /// <summary>
        /// The maximum tree depth in which the rule is searching
        /// </summary>
        int MaxDepth { get; set; }

        /// <summary>
        /// Relative position to the previous rule. When the rule is a top-level rule, the offset is relative to the worksheet origin.
        /// All positions assume index (0,0) is cell A1.
        /// The position origin is set by the <see cref="ReferencePointParent"/> property
        /// </summary>
        RowColumnIndex OffsetParent { get; set; }
        /// <summary>
        /// Relative position to the last invocation of the same rule. When the rule is a top-level rule, the offset is relative to the worksheet origin.
        /// All positions assume index (0,0) is cell A1.
        /// The position origin is set by the <see cref="ReferencePointParent"/> property
        /// </summary>
        RowColumnIndex OffsetConsecutive { get; set; }

        /// <summary>
        /// The direction in which properties of this rule are written
        /// </summary>
        SimDataMappingDirection MappingDirection { get; set; }
        /// <summary>
        /// The origin point for the offset of this rule to the parent rule. Used in combination with <see cref="OffsetParent"/>
        /// </summary>
        SimDataMappingReferencePoint ReferencePointParent { get; set; }
        /// <summary>
        /// The origin point for the offset of this rule to the previous execution of the same rule. Used in combination with <see cref="OffsetConsecutive"/>
        /// </summary>
        SimDataMappingReferencePoint ReferencePointConsecutive { get; set; }

        /// <summary>
        /// Executes the rule on a root object
        /// </summary>
        /// <param name="rootObject">The object onto which the rule is applied</param>
        /// <param name="state">The current state of the traversal</param>
        /// <param name="data">The result data of the mapping operation</param>
        void Execute(object rootObject, SimTraversalState state, SimMappedData data);

        /// <summary>
        /// Looks up taxonomy entries for default slot by their name.
        /// Do this if the default taxonomies changed, could mean that the project is migrated.
        /// </summary>
        void RestoreDefaultTaxonomyReferences();
    }

    /// <summary>
    /// Base class for all mapping rules.
    /// 
    /// Provides methods for mapping parameters, executing child rules and advancing the positions afterwards. Typically, 
    /// all inheriting classes will need to call the following order of commands:
    /// <code>
    /// //Writer properties of current rule
    /// WriteProperties(state, p => { /*...*/ data.AddData(/*...*/);  });
    /// 
    /// ExecuteChildRules(children, root, state, data);
    /// 
    /// AdvanceReferencePoint(state);
    /// </code>
    /// </summary>
    /// <typeparam name="TPropertyEnumeration">Enumeration type containing the available properties for this rule type</typeparam>
    /// <typeparam name="TFilter">Type of the filter for this rule type</typeparam>
    public abstract class SimDataMappingRuleBase<TPropertyEnumeration, TFilter> : ISimDataMappingRuleBase
        where TFilter : SimDataMappingFilterBase
    {
        #region Properties

        /// <inheritdoc />
        public SimDataMappingTool Tool
        {
            get { return tool; }
            set
            {
                if (tool != value)
                {
                    tool = value;
                    NotifyPropertyChanged(nameof(tool));
                    OnToolChanged();
                }
            }
        }
        private SimDataMappingTool tool;

        /// <inheritdoc />
        public string Name 
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    this.name = value;
                    NotifyPropertyChanged();
                }    
            }
        }
        private string name;

        /// <summary>
        /// The properties that this rule writes to the result data. The properties are written in the order they are stored in this collection
        /// </summary>
        public ObservableCollection<TPropertyEnumeration> Properties { get; } = new ObservableCollection<TPropertyEnumeration>();
        /// <summary>
        /// The filter that are applied to find matching objects
        /// </summary>
        public ObservableCollection<TFilter> Filter { get; } = new ObservableCollection<TFilter>();

        /// <inheritdoc />
        public int MaxMatches { get; set; } = int.MaxValue;
        /// <inheritdoc />
        public int MaxDepth { get; set; } = int.MaxValue;

        /// <inheritdoc />
        public RowColumnIndex OffsetParent { get; set; } = new RowColumnIndex(0, 0);
        /// <inheritdoc />
        public RowColumnIndex OffsetConsecutive { get; set; } = new RowColumnIndex(0, 0);

        /// <inheritdoc />
        public string SheetName { get; set; }
        /// <inheritdoc />
        public SimDataMappingDirection MappingDirection { get; set; }
        /// <inheritdoc />
        public SimDataMappingReferencePoint ReferencePointParent { get; set; } = SimDataMappingReferencePoint.BottomLeft;
        /// <inheritdoc />
        public SimDataMappingReferencePoint ReferencePointConsecutive { get; set; } = SimDataMappingReferencePoint.BottomLeft;

        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingRuleBase{TPropertyEnumeration, TFilter}"/> class
        /// </summary>
        /// <param name="sheetName">The name of the worksheet this rule is going to write to</param>
        public SimDataMappingRuleBase(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
                throw new ArgumentException(nameof(sheetName));
            
            this.SheetName = sheetName;
        }


        /// <inheritdoc />
        public abstract void Execute(object rootObject, SimTraversalState state, SimMappedData data);

        /// <summary>
        /// Writes the properties to the result set and advances the current position.
        /// The write action has to supplied by inheriting classes to write a property of a matching object to the data set and gets
        /// called for each property stored in <see cref="Properties"/>. The state is automatically advanced by 1 element for each write operation
        /// based on the <see cref="MappingDirection"/>.
        /// </summary>
        /// <param name="state">The current state of traversal</param>
        /// <param name="write">Write operation that stores the property into the result set</param>
        protected void WriteProperties(SimTraversalState state, Action<TPropertyEnumeration> write)
        {
            //Write properties
            var propertyRange = new RowColumnRange(state.CurrentPosition.Row, state.CurrentPosition.Column, 0, 0);

            for (int pi = 0; pi < this.Properties.Count; pi++)
            {
                var property = this.Properties[pi];

                //Perform write
                write(property);

                //Add write position to range
                propertyRange = RowColumnRange.Merge(propertyRange, state.CurrentPosition);

                //Update current position for next property
                if (pi < this.Properties.Count - 1) //All except for last
                {
                    if (this.MappingDirection == SimDataMappingDirection.Horizontal)
                        state.CurrentPosition += new RowColumnIndex(0, 1);
                    else if (this.MappingDirection == SimDataMappingDirection.Vertical)
                        state.CurrentPosition += new RowColumnIndex(1, 0);
                }
            }

            //Merge range
            state.RangeStack[state.RangeStack.Count - 1] = RowColumnRange.Merge(state.RangeStack[state.RangeStack.Count - 1], propertyRange);
        }

        /// <summary>
        /// Advances the reference point for the application of the next rule. Sets the reference point
        /// according to the <see cref="ReferencePointParent"/> property.
        /// This method needs to be called by inheriting classes after their properties and the properties of all child components have been written.
        /// </summary>
        /// <param name="state">The current traversal state</param>
        protected void AdvanceReferencePoint(SimTraversalState state)
        {
            var parentRange = new RowColumnRange(0, 0, 1, 1);
            if (state.RangeStack.Count > 0)
                parentRange = state.RangeStack[state.RangeStack.Count - 1];


            var referencePosition = new RowColumnIndex(parentRange.RowStart, parentRange.ColumnStart);
            
            var refPos = ReferencePointParent;
            if (state.MatchCount > 0)
                refPos = ReferencePointConsecutive;

            switch (refPos)
            {
                case SimDataMappingReferencePoint.TopRight:
                    referencePosition = new RowColumnIndex(parentRange.RowStart, parentRange.ColumnStart + Math.Max(0, parentRange.ColumnCount - 1));
                    break;
                case SimDataMappingReferencePoint.BottomLeft:
                    referencePosition = new RowColumnIndex(parentRange.RowStart + Math.Max(0, parentRange.RowCount - 1), parentRange.ColumnStart);
                    break;
                case SimDataMappingReferencePoint.TopLeft: //Nothing to do
                    break;
                default:
                    throw new NotSupportedException("Unsupported enum value");
            }

            //Add offset
            if (state.MatchCount == 0)
                state.CurrentPosition = referencePosition + this.OffsetParent;
            else
                state.CurrentPosition = referencePosition + this.OffsetConsecutive;

            //Start new range for this rule
            state.RangeStack.Add(new RowColumnRange(state.CurrentPosition.Row, state.CurrentPosition.Column, 0, 0));

            state.MatchCount++;
        }
    

        /// <summary>
        /// Executes the child rules of a rule and makes sure that positions are advanced correctly between
        /// executions. The traversal states is updated accordingly.
        /// </summary>
        /// <param name="childRules">The collection of child rules</param>
        /// <param name="root">The root object on which the child rules should be executed</param>
        /// <param name="state">The current traversal state</param>
        /// <param name="data">The result data</param>
        protected void ExecuteChildRules(IEnumerable<ISimDataMappingRuleBase> childRules, object root,
            SimTraversalState state, SimMappedData data)
        {
            int rangeCount = state.RangeStack.Count;

            foreach (var rule in childRules)
            {
                SimTraversalState childState = new SimTraversalState()
                {
                    Depth = -1,
                    CurrentPosition = state.CurrentPosition,
                    IncludeRoot = false,
                    VisitedObjects = state.VisitedObjects,
                    ModelsToRelease = state.ModelsToRelease,
                    RangeStack = state.RangeStack,
                };

                rule.Execute(root, childState, data);

                //Merge ranges
                var currentRuleRange = state.RangeStack[rangeCount - 1];
                for (int i = rangeCount; i < state.RangeStack.Count; i++)
                {
                    var mergeRange = state.RangeStack[i];
                    if (mergeRange.RowCount > 0 || mergeRange.ColumnCount > 0)
                        currentRuleRange = RowColumnRange.Merge(currentRuleRange, mergeRange);
                }
                if (state.RangeStack.Count > rangeCount)
                {
                    state.RangeStack.RemoveRange(rangeCount, state.RangeStack.Count - rangeCount);
                    state.RangeStack[state.RangeStack.Count - 1] = currentRuleRange;
                }
            }
        }

        /// <summary>
        /// Called when the tool changes. Happens when the rule is assigned to another tool or to the first tool.
        /// </summary>
        protected abstract void OnToolChanged();

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName">The name of the property</param>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>
        /// Looks up taxonomy entries for default slot by their name.
        /// Do this if the default taxonomies changed, could mean that the project is migrated.
        /// </summary>
        public virtual void RestoreDefaultTaxonomyReferences()
        {
            foreach (var filter in Filter)
                filter.RestoreDefaultTaxonomyReferences(Tool.Factory.ProjectData);
        }
    }
}
