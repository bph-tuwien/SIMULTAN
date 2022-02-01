using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Excel
{
    public class ExcelMappingTraceStep
    {
        public int TraversalLevel { get; }
        public string RuleName { get; }

        public string ElementName { get; }

        public string TraversedElementInfo { get; }

        public bool IsRecursionStart { get; }

        public bool IsRecursionEnd { get; }

        public bool IsRecursionInRules { get; }

        // derived
        public int TraversalLevelInverse { get; }
        public bool IsElementInvolved { get; }

        public ExcelMappingTraceStep(int _traversal_level, string _rule_name, string _element_name, string _traversal_element_info,
                                        bool _is_recursion_start, bool _is_recursion_end, bool _is_recursion_in_rules = true)
        {
            this.TraversalLevel = _traversal_level;
            this.TraversalLevelInverse = 10 - this.TraversalLevel;
            this.RuleName = _rule_name;
            this.ElementName = _element_name;
            if (string.IsNullOrEmpty(this.ElementName))
                this.IsElementInvolved = false;
            else
                this.IsElementInvolved = true;
            this.TraversedElementInfo = _traversal_element_info;
            this.IsRecursionStart = _is_recursion_start;
            this.IsRecursionEnd = _is_recursion_end;
            this.IsRecursionInRules = _is_recursion_in_rules;
        }
    }

    public class ExcelMappingTrace
    {
        public List<ExcelMappingTraceStep> Trace { get; }

        public ExcelMappingTrace()
        {
            this.Trace = new List<ExcelMappingTraceStep>();
        }

        public void AddStep(int _traversal_level, string _rule_name, string _element_name, string _traversal_element_info,
                            bool _is_recursion_start, bool _is_recursion_end, bool _is_recursion_in_rules = true)
        {
            ExcelMappingTraceStep step = new ExcelMappingTraceStep(_traversal_level, _rule_name, _element_name, _traversal_element_info,
                                                                    _is_recursion_start, _is_recursion_end, _is_recursion_in_rules);
            this.Trace.Add(step);
        }
    }

    internal struct ExcelMappingOffsets
    {
        internal int ColumnsFromParent { get; }
        internal int RowsFromParent { get; }
        internal int ColumnsFromPrevApplication { get; }
        internal int RowsFromPrevApplication { get; }

        internal ExcelMappingOffsets((int, int, int, int) offsets)
        {
            this.ColumnsFromParent = offsets.Item1;
            this.RowsFromParent = offsets.Item2;
            this.ColumnsFromPrevApplication = offsets.Item3;
            this.RowsFromPrevApplication = offsets.Item4;
        }

        public override string ToString()
        {
            return "P " + ColumnsFromParent + "x" + RowsFromParent + " A " + ColumnsFromPrevApplication + "x" + RowsFromPrevApplication;
        }
    }

    internal class ExcelRuleApplicationDynamicOffset
    {
        internal string Cause { get; private set; }
        internal int X { get; private set; }
        internal int Y { get; private set; }
        internal List<string> Contributing { get; }

        internal ExcelRuleApplicationDynamicOffset(string cause, int x, int y)
        {
            this.Cause = cause;
            this.X = x;
            this.Y = y;
            this.Contributing = new List<string>();
        }

        internal void AddContribution(ExcelRuleApplicationDynamicOffset contribution)
        {
            if (contribution.X != default || contribution.Y != default)
            {
                this.Contributing.Add(contribution.Cause);
                this.X += contribution.X;
                this.Y += contribution.Y;
            }
        }

        internal void AddContributionX(ExcelRuleApplicationDynamicOffset contribution)
        {
            if (contribution.X != default)
            {
                this.Contributing.Add(contribution.Cause);
                this.X += contribution.X;
            }
        }

        internal void AddContributionY(ExcelRuleApplicationDynamicOffset contribution)
        {
            if (contribution.Y != default)
            {
                this.Contributing.Add(contribution.Cause);
                this.Y += contribution.Y;
            }
        }
    }

    public class ExcelMappingTraceTree
    {
        public bool IsRoot { get { return this.parent == null; } }

        public string NodeName { get; }

        public string TimesApplied { get; }

        internal ExcelMappingOffsets Offsets { get; }

        public long AppliedToId { get; }

        public List<ExcelMappingTraceTree> Children { get; }

        public ExcelMappingTraceTree Parent
        {
            get { return this.parent; }
            internal set
            {
                if (this.parent != value)
                {
                    if (this.parent != null)
                        this.parent.Children.Remove(this);
                    this.parent = value;
                    if (this.parent != null)
                        this.parent.Children.Add(this);
                }
            }
        }
        private ExcelMappingTraceTree parent;

        // Range X=start col, Y=start row, Z=size in cols, W=size in rows
        public ExcelMappedData Result { get; }

        public int IndexInRepeatSequence { get; private set; }

        public int Depth { get { return this.GetMaxDepth(); } }

        //public int DynamicOffsetFromResultX { get; private set; }
        //public int DynamicOffsetFromResultY { get; private set; }

        internal ExcelRuleApplicationDynamicOffset DynamicOffsets { get; private set; }

        public ExcelMappingTraceTree(ExcelMappingTraceTree parent, string nodeName, string timesApplied, (int, int, int, int) offsets, long appliedToId, ExcelMappedData result)
        {
            this.NodeName = nodeName;
            this.TimesApplied = timesApplied;
            this.Offsets = new ExcelMappingOffsets(offsets);
            this.AppliedToId = appliedToId;
            this.Children = new List<ExcelMappingTraceTree>();
            this.Parent = parent;
            this.Result = result;
            this.IndexInRepeatSequence = 0;
            this.DynamicOffsets = new ExcelRuleApplicationDynamicOffset(string.Empty, 0, 0);
        }

        internal void RecognizeRepeatsOfSameDepth()
        {
            Dictionary<string, int> repeats = new Dictionary<string, int>();
            ExcelMappingTraceTree first = null;
            foreach (var child in this.Children)
            {
                string key = child.NodeName;// + ":" + child.Depth;
                if (repeats.ContainsKey(key))
                {
                    repeats[key]++;
                    child.IndexInRepeatSequence = repeats[key];
                    child.RecognizeRepeatsOfSameDepth();
                    if (first != null)
                    {
                        first.IndexInRepeatSequence = 1;
                        first.RecognizeRepeatsOfSameDepth();
                    }
                }
                else
                {
                    repeats.Add(key, 1);
                    first = child;
                }
            }
        }

        private int GetMaxDepth()
        {
            if (this.Children.Count == 0)
                return 0;

            int max_depth = 1;
            foreach (var child in this.Children)
            {
                max_depth = Math.Max(max_depth, 1 + child.GetMaxDepth());
            }

            return max_depth;
        }

        private ExcelRuleApplicationDynamicOffset GetCumulativeOffsetFromChildrensOffsets()
        {
            int actual_offset_X = 0;
            int actual_offset_Y = 0;
            string offset_cause = string.Empty;
            if (this.Result != null)
            {
                foreach (var child in this.Children)
                {
                    if (child.Result != null)
                    {
                        // these should always be zero
                        var local_check_X = child.Result.Range.X - this.Result.Range.X - (child.Offsets.ColumnsFromParent + 1) - (child.Offsets.ColumnsFromPrevApplication) * Math.Max(0, child.IndexInRepeatSequence - 1);
                        var local_check_Y = child.Result.Range.Y - this.Result.Range.Y - (child.Offsets.RowsFromParent) - (child.Offsets.RowsFromPrevApplication) * Math.Max(0, child.IndexInRepeatSequence - 1);
                        // this should be the actual offset
                        var local_offset_X = child.Offsets.ColumnsFromPrevApplication * Math.Max(0, child.IndexInRepeatSequence - 1);
                        var local_offset_Y = child.Offsets.RowsFromPrevApplication * Math.Max(0, child.IndexInRepeatSequence - 1);

                        // record the offset
                        int actual_offset_X_prev = actual_offset_X;
                        int actual_offset_Y_prev = actual_offset_Y;
                        actual_offset_X = Math.Max((int)local_offset_X, actual_offset_X);
                        actual_offset_Y = Math.Max((int)local_offset_Y, actual_offset_Y);
                        if (actual_offset_X > actual_offset_X_prev || actual_offset_Y > actual_offset_Y_prev)
                            offset_cause = child.NodeName;
                    }
                }
            }
            return new ExcelRuleApplicationDynamicOffset(offset_cause, actual_offset_X, actual_offset_Y);
        }

        internal void SetDynamicOffsets_Pass1()
        {
            // extract info from children...
            if (this.Parent == null)
            {
                this.DynamicOffsets = new ExcelRuleApplicationDynamicOffset(string.Empty, 0, 0);
            }
            else
            {
                this.DynamicOffsets = this.GetCumulativeOffsetFromChildrensOffsets();
            }

            // prepare the children...
            // gather the offsets
            foreach (var child in this.Children)
            {
                child.SetDynamicOffsets_Pass1();
            }
            // stack them
            for (int i = 1; i < this.Children.Count; i++)
            {
                this.Children[i].DynamicOffsets.AddContribution(this.Children[i - 1].DynamicOffsets);
            }
        }

        internal void SetDynamicOffsets_Pass2()
        {
            foreach (var child in this.Children)
            {
                if (child.DynamicOffsets.Contributing.Count == 0 && !(this.DynamicOffsets.Cause == child.NodeName || this.DynamicOffsets.Contributing.Contains(child.NodeName)))
                {
                    if (child.DynamicOffsets.X == 0 && this.DynamicOffsets.X != 0)
                        child.DynamicOffsets.AddContributionX(this.DynamicOffsets);
                    if (child.DynamicOffsets.Y == 0 && this.DynamicOffsets.Y != 0)
                        child.DynamicOffsets.AddContributionY(this.DynamicOffsets);
                }
                //if (child.IndexInRepeatSequence == 0)
                //{
                //    if (child.DynamicOffsets.X == 0 && this.DynamicOffsets.X != 0)
                //        child.DynamicOffsets.AddContributionX(this.DynamicOffsets);
                //    if (child.DynamicOffsets.Y == 0 && this.DynamicOffsets.Y != 0)
                //        child.DynamicOffsets.AddContributionY(this.DynamicOffsets);

                //}
                child.SetDynamicOffsets_Pass2();
            }
        }

        internal string GetContent(int counter = 0)
        {
            string content = (counter <= 0) ? string.Empty : ((counter == 1) ? "\t" : Enumerable.Repeat("\t", counter).Aggregate((x, y) => x + y));
            string ind = (this.IndexInRepeatSequence == 0) ? string.Empty : "[" + this.IndexInRepeatSequence + " depth " + this.Depth + "]";
            string offset = " [OF " + this.DynamicOffsets.X + "x" + this.DynamicOffsets.Y + " for " + this.Offsets.ToString() + "]";
            content += this.NodeName + this.TimesApplied + " / " + this.AppliedToId + " " + ind + offset + " - - - "
                        + this.Result.GetContent() + " >> " + this.Result.Range.X.ToString() + "; " + this.Result.Range.Y.ToString() + "; " + this.Result.Range.Z.ToString() + "; " + this.Result.Range.W.ToString()
                        + Environment.NewLine;
            foreach (var child in this.Children)
            {
                content += child.GetContent(counter + 1);
            }

            return content;
        }

        internal void AdaptResultOffsetsToRepeatCallsToSameRule()
        {
            if (this.Parent != null)
            {
                if (this.Result != null && (this.DynamicOffsets.X != 0 || this.DynamicOffsets.Y != 0))
                {
                    var range_old = this.Result.Range;
                    this.Result.Range = new System.Windows.Media.Media3D.Point4D(range_old.X + this.DynamicOffsets.X,
                                                                                 range_old.Y + this.DynamicOffsets.Y,
                                                                                 range_old.Z,
                                                                                 range_old.W);
                }
            }

            int nr_children = this.Children.Count;
            for (int i = 0; i < nr_children; i++)
            {
                this.Children[i].AdaptResultOffsetsToRepeatCallsToSameRule();
            }

        }
    }
}
