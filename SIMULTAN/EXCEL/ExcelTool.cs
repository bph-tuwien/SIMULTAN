using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace SIMULTAN.Excel
{
    public class ExcelTool : INotifyPropertyChanged
    {
        #region PROPERTIES

        public ExcelToolFactory Factory { get; internal set; }

        private string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                if (this.name != value)
                {
                    var old_value = this.name;
                    this.name = value;

                    if (Factory != null)
                        RenameToolInComponents(Factory.ProjectData.Components, old_value, this.name);

                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }

        // ------------------------------------------------------------------------------------- //
        private ObservableCollection<ExcelMappingNode> inputRules;
        public ObservableCollection<ExcelMappingNode> InputRules
        {
            get { return this.inputRules; }
        }

        private void InputRules_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                throw new NotImplementedException("Clear/Reset is not implemented");

            if (e.OldItems != null)
                foreach (var item in e.OldItems.OfType<ExcelMappingNode>())
                    item.Tool = null;
            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<ExcelMappingNode>())
                    item.Tool = this;
        }

        // ------------------------------------------------------------------------------------- //

        private string macro_name;
        public string MacroName
        {
            get { return this.macro_name; }
            set
            {
                if (this.macro_name != value)
                {
                    this.macro_name = value;
                    NotifyPropertyChanged(nameof(MacroName));
                }
            }
        }

        // ------------------------------------------------------------------------------------- //
        private ObservableCollection<KeyValuePair<ExcelMappedData, Type>> outputRangeRules;
        public ObservableCollection<KeyValuePair<ExcelMappedData, Type>> OutputRangeRules
        {
            get { return this.outputRangeRules; }
        }

        // ------------------------------------------------------------------------------------- //

        // ------------------------------------------------------------------------------------- //
        private ObservableCollection<ExcelUnmappingRule> outputRules;
        public ObservableCollection<ExcelUnmappingRule> OutputRules
        {
            get { return this.outputRules; }
            set
            {
                if (this.outputRules != null)
                {
                    this.outputRules.CollectionChanged -= OutputRules_CollectionChanged;
                }
                this.outputRules = value;
                if (this.outputRules != null)
                {
                    this.outputRules.CollectionChanged += OutputRules_CollectionChanged;
                }
            }
        }

        private void OutputRules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
                throw new NotImplementedException("Clear/Reset is not implemented");

            if (e.OldItems != null)
                foreach (var item in e.OldItems.OfType<ExcelUnmappingRule>())
                    item.Tool = null;
            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<ExcelUnmappingRule>())
                    item.Tool = this;
        }

        // ------------------------------------------------------------------------------------- //

        public int MaxLevel { get { return maxLevel; } set { maxLevel = value; NotifyPropertyChanged(nameof(MaxLevel)); } }
        private int maxLevel = 3;

        public int MaxRefLevel { get { return maxRefLevel; } set { maxRefLevel = value; NotifyPropertyChanged(nameof(MaxRefLevel)); } }
        private int maxRefLevel = 1;


        public Stack<(SimComponent component, SimSlot slot)> CallStack { get; private set; }

        #endregion

        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion

        #region .CTOR
        internal ExcelTool()
        {
            this.name = "";

            this.inputRules = new ObservableCollection<ExcelMappingNode> { ExcelMappingNode.Nothing_Rule() };
            var default_rule = ExcelMappingNode.Default_Rule(null);
            this.inputRules.Add(default_rule);
            this.inputRules.CollectionChanged += InputRules_CollectionChanged;

            this.macro_name = "";

            this.outputRangeRules = new ObservableCollection<KeyValuePair<ExcelMappedData, Type>>();

            this.outputRules = new ObservableCollection<ExcelUnmappingRule>();
            this.outputRules.CollectionChanged += OutputRules_CollectionChanged;
        }

        internal ExcelTool(string _name, IEnumerable<ExcelMappingNode> _rules, 
            IEnumerable<KeyValuePair<ExcelMappedData, Type>> _results, 
            IEnumerable<ExcelUnmappingRule> _result_unmappings,
            string _macro_name)
        {
            this.name = _name;

            this.inputRules = new ObservableCollection<ExcelMappingNode> { ExcelMappingNode.Nothing_Rule() };
            foreach (var r in _rules)
            {
                r.Tool = this;
                this.inputRules.Add(r);
            }
            this.inputRules.CollectionChanged += InputRules_CollectionChanged;

            this.macro_name = _macro_name;

            this.outputRangeRules = new ObservableCollection<KeyValuePair<ExcelMappedData, Type>>();
            if (_results != null)
            {
                foreach (var r in _results)
                {
                    this.outputRangeRules.Add(r);
                }
            }

            this.outputRules = new ObservableCollection<ExcelUnmappingRule>();
            if (_result_unmappings != null)
            {
                foreach (var r in _result_unmappings)
                {
                    r.Tool = this;
                    this.outputRules.Add(r);
                }
            }
            this.outputRules.CollectionChanged += OutputRules_CollectionChanged;
        }

        internal ExcelTool(ExcelTool _original, string nameCopyFormat = "{0}")
        {
            this.name = "";//Do not copy name, otherwise you'll get problems when you rename it since excel tools are identified by name

            this.inputRules = new ObservableCollection<ExcelMappingNode>();
            foreach (ExcelMappingNode rule in _original.inputRules)
            {
                var isNothing = ExcelMappingNode.IsNothing(rule);
                ExcelMappingNode rule_copy;
                if (!isNothing)
                {
                    rule_copy = new ExcelMappingNode(rule);
                }
                else
                    rule_copy = ExcelMappingNode.Nothing_Rule();

                rule_copy.Tool = this;
                this.inputRules.Add(rule_copy);
            }
            this.inputRules.CollectionChanged += InputRules_CollectionChanged;

            this.macro_name = _original.macro_name;

            this.outputRangeRules = new ObservableCollection<KeyValuePair<ExcelMappedData, Type>>();
            if (_original.outputRangeRules != null)
            {
                foreach (var entry in _original.outputRangeRules)
                {
                    KeyValuePair<ExcelMappedData, Type> entry_copy = new KeyValuePair<ExcelMappedData, Type>(new ExcelMappedData(entry.Key), entry.Value);
                    this.outputRangeRules.Add(entry_copy);
                }
            }

            this.outputRules = new ObservableCollection<ExcelUnmappingRule>();
            if (_original.outputRules != null)
            {
                foreach (ExcelUnmappingRule rule in _original.outputRules)
                {
                    ExcelUnmappingRule rule_copy = new ExcelUnmappingRule(rule, nameCopyFormat);
                    rule_copy.Tool = this;
                    this.outputRules.Add(rule_copy);
                }
            }
            this.outputRules.CollectionChanged += OutputRules_CollectionChanged;

        }
        #endregion

        #region METHODS: MAPPING
        public int GetIndexOfRule(ExcelMappingNode _rule)
        {
            if (_rule == null) return -1;
            if (this.inputRules.Count == 0) return -1;
            if (this.inputRules.Contains(_rule))
            {
                return this.inputRules.IndexOf(_rule);
            }
            else
            {
                return -1;
            }
        }

        public List<ExcelMappedData> MapToInput(List<KeyValuePair<SimComponent, ExcelMappingNode>> _comp_to_rule_map, out ExcelMappingTrace _trace)
        {
            List<ExcelMappedData> mapping = new List<ExcelMappedData>();
            _trace = new ExcelMappingTrace();
            if (_comp_to_rule_map == null) return mapping;
            if (_comp_to_rule_map.Count == 0) return mapping;

            Point offset = new Point(0, 0);
            Dictionary<ExcelMappingNode, Point> total_offsets = new Dictionary<ExcelMappingNode, Point>();
            foreach (var entry in _comp_to_rule_map)
            {
                SimComponent source = entry.Key;
                ExcelMappingNode map = entry.Value;
                if (source == null || map == null) continue;

                // re-calculate the offset
                Point total_offset = new Point(0, 0);
                if (total_offsets.ContainsKey(map))
                    total_offset = total_offsets[map];
                else
                    total_offsets.Add(map, new Point(0, 0));

                map.Reset();
                offset = new Point(0, 0);
                if (total_offset.X > 0 || total_offset.Y > 0)
                {
                    if (map.OrderHorizontally)
                        offset = new Point(0, total_offset.Y - map.OffsetFromParent.Y);
                    else
                        offset = new Point(total_offset.X - map.OffsetFromParent.X, 0);
                }

                //Reset callstack
                this.CallStack = new Stack<(SimComponent component, SimSlot slot)>();

                // apply rule
                List<ExcelMappedData> targets = map.ApplyRuleTo(source, offset, out _trace);
                total_offset = ExcelMappedData.GetTotalOffset(targets);
                total_offsets[map] = total_offset;

                this.CallStack = null;

                mapping.AddRange(targets);
                map.Reset();
            }

            return mapping;
        }
        #endregion

        #region METHODS: DISPLAY

        public (List<ExcelComponentMappingInfo> mappings, bool timeout) GetMappings(List<SimComponent> _comps, int _max_admissible_level, int _max_admissible_ref_level, long _max_found = long.MaxValue)
        {
            List<ExcelComponentMappingInfo> to_map_combo = new List<ExcelComponentMappingInfo>();
            bool timeout = false;
            if (_comps == null) return (to_map_combo, timeout);
            if (_comps.Count == 0) return (to_map_combo, timeout);

            List<(int, bool, SimComponent)> to_map = new List<(int, bool, SimComponent)>();
            foreach (var m in _comps)
            {
                List<KeyValuePair<int, SimComponent>> m_to_map = new List<KeyValuePair<int, SimComponent>>();
                List<KeyValuePair<int, SimId>> m_to_map_control = new List<KeyValuePair<int, SimId>>();
                List<KeyValuePair<int, bool>> m_to_map_issubcomponentpath = new List<KeyValuePair<int, bool>>();
                bool in_time = m.GetFlatSubAndRefCompListWLevels(ref m_to_map, ref m_to_map_control, ref m_to_map_issubcomponentpath, true, 0, 0, _max_admissible_level, _max_admissible_ref_level, _max_found);
                to_map.AddRange(m_to_map.Zip(m_to_map_issubcomponentpath, (x, y) => (x.Key, y.Value, x.Value)));
                //System.Diagnostics.Debug.WriteLine(m.ToInfoString());
                timeout = !in_time;
                if (timeout) break;
            }
            ExcelComponentMappingInfo.NR_CREATED = 0;
            foreach (var entry in to_map)
            {
                var disp = new ExcelComponentMappingInfo(entry.Item1, entry.Item2, entry.Item3, this);
                to_map_combo.Add(disp);
                // look for a saved mapping in the component
                List<long> disp_path = ExcelComponentMappingInfo.GetPath(to_map_combo, disp);
                ExcelMappingNode corresponding_rule = entry.Item3.ExtractSelectedNodeInContext(this, disp_path);

                if (corresponding_rule != null)
                    disp.SelectedRule = corresponding_rule;
            }

            return (to_map_combo, timeout);
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return this.name + " [" + (this.inputRules.Count - 1).ToString() + "]";
        }

        #endregion

        #region UTILS

        public void MoveRuleUp(ExcelMappingNode _node)
        {
            var idx = this.InputRules.IndexOf(_node);
            if (idx > 0)
                this.inputRules.Move(idx, idx - 1);
        }

        public void MoveRuleDown(ExcelMappingNode _node)
        {
            var idx = this.InputRules.IndexOf(_node);
            if (idx < this.InputRules.Count - 1)
                this.inputRules.Move(idx, idx + 1);
        }

        /// <summary>
        /// Notifies a component collection about the change of the name of an Excel Tool
        /// </summary>
        /// <param name="components">The factory in which the Excel tool name has to be replaced</param>
        /// <param name="nameOld">The old tool name</param>
        /// <param name="nameNew">The new tool name</param>
        private static void RenameToolInComponents(IEnumerable<SimComponent> components, string nameOld, string nameNew)
        {
            foreach (var c in components)
            {
                if (c != null)
                {
                    var match = c.MappingsPerExcelTool.Where(x => x.Value.ToolName == nameOld).ToList();
                    foreach (var m in match)
                    {
                        // remove old
                        ExcelComponentMapping old_mapping = m.Value;
                        c.MappingsPerExcelTool.Remove(m.Key);

                        // add new
                        ExcelComponentMapping new_mapping = new ExcelComponentMapping(new List<long>(old_mapping.Path), nameNew, old_mapping.RuleName, old_mapping.RuleIndexInTool);
                        string new_key = new_mapping.ConstructKey();
                        RemoveDuplicateMapping(c, new_key, new_mapping);
                        c.MappingsPerExcelTool.Add(new_key, new_mapping);
                    }

                    RenameToolInComponents(c.Components.Select(x => x.Component), nameOld, nameNew);
                }
            }
        }

        /// <summary>
        /// Notifies a component collection about the change of the name of an Excel Tool Rule
        /// </summary>
        /// <param name="components">The factory in which the Excel rule name has to be replaced</param>
        /// <param name="toolName">The name of the Excel tool</param>
        /// <param name="ruleNameOld">The old name of the rule</param>
        /// <param name="ruleNameNew">The new name of the rule</param>
        internal static void RenameRuleInComponents(IEnumerable<SimComponent> components, string toolName, string ruleNameOld, string ruleNameNew)
        {
            foreach (var c in components)
            {
                if (c != null)
                {
                    var match = c.MappingsPerExcelTool.Where(x => x.Value.ToolName == toolName && x.Value.RuleName == ruleNameOld).ToList();
                    foreach (var m in match)
                    {
                        // remove old
                        ExcelComponentMapping old_mapping = m.Value;
                        c.MappingsPerExcelTool.Remove(m.Key);

                        // add new
                        ExcelComponentMapping new_mapping = new ExcelComponentMapping(new List<long>(old_mapping.Path), toolName, ruleNameNew, old_mapping.RuleIndexInTool);
                        string new_key = new_mapping.ConstructKey();
                        RemoveDuplicateMapping(c, new_key, new_mapping);
                        c.MappingsPerExcelTool.Add(new_key, new_mapping);
                    }

                    RenameRuleInComponents(c.Components.Select(x => x.Component), toolName, ruleNameOld, ruleNameNew);
                }
            }
        }

        private static void RemoveDuplicateMapping(SimComponent c, string key, ExcelComponentMapping mapping)
        {
            if (!c.MappingsPerExcelTool.ContainsKey(key))
            {
                string key_of_duplicate = null;
                foreach (var entry in c.MappingsPerExcelTool)
                {
                    if (entry.Value.ToolName == mapping.ToolName && entry.Value.RuleName == mapping.RuleName &&
                        ExcelComponentMapping.IsSamePath(entry.Value.Path, mapping.Path))
                    {
                        key_of_duplicate = entry.Key;
                    }
                }
                if (!(string.IsNullOrEmpty(key_of_duplicate)))
                    c.MappingsPerExcelTool.Remove(key_of_duplicate);
            }
        }

        #endregion
    }
}
