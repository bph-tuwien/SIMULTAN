using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using Point4D = System.Windows.Media.Media3D.Point4D;

namespace SIMULTAN.Excel
{
    /// <summary>
    /// Regulates the branch selection during component traversal.
    /// </summary>
    public enum TraversalStrategy
    {
        /// <summary>
        /// Traverses only subcomponents.
        /// </summary>
        SUBTREE_ONLY = 0,
        /// <summary>
        /// Traverses only references.
        /// </summary>
        REFERENCES_ONLY = 1,
        /// <summary>
        /// Traverses both subcomponents (first) and references (second).
        /// </summary>
        SUBTREE_AND_REFERENCES = 2
    }

    [Flags]
    public enum TraversalStrategyNew
    {
        None = 0,
        Self = 1,
        /// <summary>
        /// Traverses only subcomponents.
        /// </summary>
        SubtreeOnly = 2,
        /// <summary>
        /// Traverses only references.
        /// </summary>
        ReferencesOnly = 4,

        Subtree = Self | SubtreeOnly,
        References = Self | ReferencesOnly,
        SubtreeAndReferences = Self | SubtreeOnly | ReferencesOnly
    }

    public enum ExcelMappingRange
    {
        SingleValue = 0,
        VectorValues = 1,
        MatrixValues = 2
    }

    public class ExcelMappingNode : INotifyPropertyChanged
    {
        /// <summary>
        /// The version currently saved with the excel rule.
        /// </summary>
        public const int CurrentRuleVersion = 1;

        private Stack<(SimComponent, SimSlot)> GetCallStack()
        {
            var rule = this;
            while (rule.Parent != null)
                rule = rule.Parent;

            return rule.Tool?.CallStack;
        }


        #region STATIC CONST

        private static readonly bool DEBUG_VERBOSE_ON = false;
        private static readonly bool DEBUG_VERY_VERBOSE_ON = false;
        private static readonly bool DEBUG_TMP_ON = false;
        private static readonly bool DEBUG_IN_SUBRULES_ON = false;
        private static readonly bool TRACE_TREE_ON = false;
        private static readonly bool TRACKER_IS_VERBOSE = false;

        #endregion

        #region PROPERTIES / CLASS MEMBERS

        public ExcelTool Tool { get; internal set; }


        // structure
        private ExcelMappingNode parent_internal;
        public ExcelMappingNode Parent
        {
            get { return this.parent_internal; }
            internal set
            {
                if (this.parent_internal != value)
                {
                    if (this.parent_internal != null)
                    {
                        this.parent_internal.children.Remove(this);
                    }

                    this.parent_internal = value;

                    if (this.parent_internal != null)
                    {
                        this.parent_internal.children.Add(this);
                    }
                }
            }
        }

        private int GetRuleDepth()
        {
            if (this.Parent == null)
                return 0;
            else
                return 1 + this.Parent.GetRuleDepth();
        }

        private ElectivelyObservableCollection<ExcelMappingNode> children;
        public IReadOnlyObservableCollection<ExcelMappingNode> Children
        {
            get { return this.children; }
        }

        // excel mapping
        private string sheet_name;
        public string SheetName
        {
            get { return this.sheet_name; }
            set
            {
                if (this.sheet_name != value)
                {
                    this.sheet_name = value;
                    this.NotifyPropertyChanged(nameof(this.SheetName));
                }
            }
        }

        private Point offset_from_parent;
        public Point OffsetFromParent
        {
            get { return this.offset_from_parent; }
            set
            {
                if (this.offset_from_parent != value)
                {
                    this.offset_from_parent = value;
                    this.NotifyPropertyChanged(nameof(this.OffsetFromParent));
                }
            }
        }

        // content
        private string node_name;
        public string NodeName
        {
            get { return this.node_name; }
            set
            {
                if (this.node_name != value)
                {
                    var old_value = this.node_name;
                    this.node_name = value;

                    if (this.Tool != null && this.Tool.Factory != null)
                        ExcelTool.RenameRuleInComponents(this.Tool.Factory.ProjectData.Components, this.Tool.Name, old_value, this.node_name);

                    this.NotifyPropertyChanged(nameof(this.NodeName));
                }
            }
        }

        private MappingSubject subject;
        public MappingSubject Subject
        {
            get { return this.subject; }
            set
            {
                if (this.subject != value)
                {
                    this.subject = value;
                    if (this.subject == MappingSubject.Instance &&
                        this.properties != null &&
                        (this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesTemporary)) || this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesPersistent))))
                    {
                        this.range_of_values_pre_property = ExcelMappingRange.VectorValues;
                    }

                    this.Properties = new ObservableDictionary<string, Type>();
                    this.PatternsToMatchInProperty.Clear();

                    this.NotifyPropertyChanged(nameof(this.Subject));
                }
            }
        }

        private ObservableDictionary<string, Type> properties;
        public ObservableDictionary<string, Type> Properties
        {
            get { return this.properties; }
            set
            {
                if (this.properties != null)
                    this.properties.CollectionChanged -= this.Properties_CollectionChanged;

                this.properties = value;

                if (this.properties != null)
                    this.properties.CollectionChanged += this.Properties_CollectionChanged;

                this.NotifyPropertyChanged(nameof(this.Properties));
            }
        }

        private void Properties_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.subject == MappingSubject.Instance &&
                        this.properties != null &&
                        (this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesTemporary)) || this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesPersistent))))
                this.range_of_values_pre_property = ExcelMappingRange.VectorValues;
        }

        private ExcelMappingRange range_of_values_pre_property;
        public ExcelMappingRange RangeOfValuesPerProperty
        {
            get { return this.range_of_values_pre_property; }
            set
            {
                if (this.range_of_values_pre_property != value)
                {
                    this.range_of_values_pre_property = value;
                    this.NotifyPropertyChanged(nameof(this.RangeOfValuesPerProperty));
                }
            }
        }

        private bool order_horizontally;
        public bool OrderHorizontally
        {
            get { return this.order_horizontally; }
            set
            {
                if (this.order_horizontally != value)
                {
                    this.order_horizontally = value;
                    this.NotifyPropertyChanged(nameof(this.OrderHorizontally));
                }
            }
        }

        internal bool prepend_content_to_children;
        public bool PrependContentToChildren
        {
            get { return this.prepend_content_to_children; }
            set
            {
                if (this.prepend_content_to_children != value)
                {
                    this.prepend_content_to_children = value;
                    this.NotifyPropertyChanged(nameof(this.PrependContentToChildren));
                }
            }
        }

        // conditions
        public ObservableCollection<(string propertyName, object filter)> PatternsToMatchInProperty
        {
            get { return this.patterns_to_match_in_property; }
            private set
            {
                this.patterns_to_match_in_property = value;
                this.NotifyPropertyChanged(nameof(this.PatternsToMatchInProperty));
            }
        }
        private ObservableCollection<(string propertyName, object filter)> patterns_to_match_in_property;

        // application
        protected int nr_applications; // per subject instance (e.g. component), not total!

        public Point OffsetBtwApplications
        {
            get { return this.offset_btw_applications; }
            set
            {
                if (this.offset_btw_applications != value)
                {
                    this.offset_btw_applications = value;
                    this.NotifyPropertyChanged(nameof(this.OffsetBtwApplications));
                }
            }
        }
        private Point offset_btw_applications;

        // traversal
        public int MaxElementsToMap
        {
            get { return this.max_elements_to_map; }
            set
            {
                if (this.max_elements_to_map != value)
                {
                    this.max_elements_to_map = value.Clamp(0, 20000);
                    this.NotifyPropertyChanged(nameof(this.MaxElementsToMap));
                }
            }
        }
        private int max_elements_to_map;

        public int MaxHierarchyLevelsToTraverse
        {
            get { return this.max_hierarchy_levels_to_traverse; }
            set
            {
                if (this.max_hierarchy_levels_to_traverse != value)
                {
                    this.max_hierarchy_levels_to_traverse = value.Clamp(0, 10);
                    this.NotifyPropertyChanged(nameof(this.MaxHierarchyLevelsToTraverse));
                }
            }
        }
        private int max_hierarchy_levels_to_traverse;

        private int nr_mapped_elements;
        private int nr_traversed_element_levels;

        #endregion

        #region PROPERTIES: Traversal fine tuning

        /// <summary>
        /// The strategy to use during component traversal.
        /// </summary>
        public TraversalStrategy Strategy
        {
            get { return this.strategy; }
            set
            {
                if (this.strategy != value)
                {
                    this.strategy = value;
                    this.NotifyPropertyChanged(nameof(this.Strategy));
                }
            }
        }
        private TraversalStrategy strategy = TraversalStrategy.SUBTREE_AND_REFERENCES;

        /// <summary>
        /// If true, the rule in the node gets executed, otherwise it is skipped in favour of other rules on the same hierarchical level.
        /// Child rules are also skipped.
        /// </summary>
        public bool NodeIsActive
        {
            get { return this.node_is_active; }
            set
            {
                if (this.node_is_active != value)
                {
                    this.node_is_active = value;
                    this.NotifyPropertyChanged(nameof(this.NodeIsActive));
                }
            }
        }
        private bool node_is_active = true;

        #endregion

        #region PROPERTIES: Traversal
        private Dictionary<SimId, int> visited_comps_by_me;
        private List<long> visited_params_by_me;
        private List<long> visited_geoms_by_me;

        private List<SimId> comps_passed_through_filter;

        internal ExcelMappingTraceTree TraceTree { get; set; }

        private (int, int, int, int) SummarizeOffsets()
        {
            return ((int)this.OffsetFromParent.X, (int)this.OffsetFromParent.Y, (int)this.OffsetBtwApplications.X, (int)this.OffsetBtwApplications.Y);
        }


        private static void AddVisit(Dictionary<SimId, Dictionary<ExcelMappingNode, int>> _record, SimComponent _comp, ExcelMappingNode _visitor)
        {
            if (!_record.ContainsKey(_comp.Id))
                _record.Add(_comp.Id, new Dictionary<ExcelMappingNode, int>());
            if (!_record[_comp.Id].ContainsKey(_visitor))
                _record[_comp.Id].Add(_visitor, 0);
            _record[_comp.Id][_visitor] += 1;

            #region DEBUG TMP
            //if (DEBUG_TMP_ON && _comp.LocalID == 18399)
            //{
            //    Debug.WriteLine("Added Visit [" + _record[_comp.ID][_visitor] +  "] to " + _comp.LocalID + " by " + _visitor.NodeName);
            //}
            #endregion
        }

        private static int GetNrVisitsBy(Dictionary<SimId, Dictionary<ExcelMappingNode, int>> _record, SimComponent _comp, ExcelMappingNode _visitor)
        {
            if (_record.ContainsKey(_comp.Id))
            {
                if (_record[_comp.Id].ContainsKey(_visitor))
                {
                    return _record[_comp.Id][_visitor];
                }
            }
            return 0;
        }

        private static void Subtract(Dictionary<SimId, Dictionary<ExcelMappingNode, int>> _minuend, Dictionary<SimId, int> _subtrahend, List<SimId> _excluded,
                                     ExcelMappingNode _visitor, int _lower_cap = 0)
        {

            Dictionary<SimId, int> result = new Dictionary<SimId, int>();
            foreach (var entry in _subtrahend)
            {
                if (entry.Value != 0 && !_excluded.Contains(entry.Key) && _minuend.ContainsKey(entry.Key) && _minuend[entry.Key].ContainsKey(_visitor))
                    result.Add(entry.Key, Math.Max(_minuend[entry.Key][_visitor] - entry.Value, _lower_cap));
            }
            foreach (var entry in result)
            {
                #region DEBUG VERY VERBOSE
                bool show = _minuend[entry.Key][_visitor] > 0;
                if (DEBUG_VERY_VERBOSE_ON)
                {
                    if (show)
                        Console.Write("Reset of visits to " + entry.Key.LocalId + " by " + _visitor.NodeName + " from " + _minuend[entry.Key][_visitor]);
                }
                #endregion

                _minuend[entry.Key][_visitor] = entry.Value;

                #region DEBUG VERY VERBOSE
                if (DEBUG_VERY_VERBOSE_ON)
                {
                    if (show)
                        Console.WriteLine(" to " + _minuend[entry.Key][_visitor]);
                }
                #endregion
            }
        }

        #endregion

        #region PROPERTIES FOR DISPLAY

        [Obsolete("Still in use by the Excel Mapping Window")]
        public string DisplayName { get { return this.node_name + " [" + this.sheet_name + "]"; } }
        [Obsolete("Still in use by the Excel Mapping Window")]
        public string DisplayContent { get { return this.ToIndentedString(""); } }

        #endregion

        #region PROPERTIES: Version! ---------------------------------- TODO: SERIALIZER

        /// <summary>
        /// The version of the rule. This has influence on the rule traversal.
        /// </summary>
        public int Version
        {
            get { return this.version; }
            set
            {
                if (this.version != value)
                {
                    this.version = value;
                    foreach (var sN in this.Children)
                    {
                        sN.Version = value;
                    }
                }
            }
        }
        private int version;

        #endregion

        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion


        #region .CTOR

        internal ExcelMappingNode(ExcelMappingNode _parent, string _sheet_name, Point _offset, string _node_name,
                                   MappingSubject _subject, Dictionary<string, Type> _properties, ExcelMappingRange _accepted_range, bool _order_hrz,
                                   IEnumerable<(string propertyName, object filter)> _patterns_to_match_in_property, Point _offset_btw_applicaions,
                                   int _max_elements_to_map = 10, int _max_hierarchy_levels_to_traverse = 3,
                                   TraversalStrategy _strategy = TraversalStrategy.SUBTREE_ONLY, bool _node_is_active = true, int _version = ExcelMappingNode.CurrentRuleVersion)
        {
            this.children = new ElectivelyObservableCollection<ExcelMappingNode>();

            this.sheet_name = _sheet_name;
            this.offset_from_parent = _offset;

            this.node_name = _node_name;
            this.subject = _subject;
            this.properties = (_properties == null) ? null : new ObservableDictionary<string, Type>(_properties);
            if (this.properties != null)
            {
                this.properties.CollectionChanged -= this.Properties_CollectionChanged;
                this.properties.CollectionChanged += this.Properties_CollectionChanged;
            }
            this.range_of_values_pre_property = _accepted_range;
            if (_subject == MappingSubject.Instance &&
                _properties != null &&
                (_properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesTemporary)) || _properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesPersistent))))
                this.range_of_values_pre_property = ExcelMappingRange.VectorValues;
            this.order_horizontally = _order_hrz;

            this.prepend_content_to_children = false;

            if (_patterns_to_match_in_property != null)
                this.PatternsToMatchInProperty = new ObservableCollection<(string propertyName, object filter)>(_patterns_to_match_in_property);
            else
                this.PatternsToMatchInProperty = new ObservableCollection<(string propertyName, object filter)>();

            this.nr_applications = 0;
            this.offset_btw_applications = _offset_btw_applicaions;

            this.MaxElementsToMap = _max_elements_to_map;
            this.MaxHierarchyLevelsToTraverse = _max_hierarchy_levels_to_traverse;
            this.nr_mapped_elements = 0;
            this.nr_traversed_element_levels = 0;

            this.strategy = _strategy;
            this.node_is_active = _node_is_active;
            this.Version = _version;

            this.comps_passed_through_filter = new List<SimId>();
            this.visited_comps_by_me = new Dictionary<SimId, int>();
            this.visited_params_by_me = new List<long>();
            this.visited_geoms_by_me = new List<long>();
            this.Parent = _parent;
        }

        #endregion

        #region COPY .CTOR

        internal ExcelMappingNode(ExcelMappingNode _original, string copyNameFormat = "{0}")
        {
            this.children = new ElectivelyObservableCollection<ExcelMappingNode>();
            foreach (ExcelMappingNode child in _original.children)
            {
                ExcelMappingNode child_copy = new ExcelMappingNode(child, copyNameFormat);
                child_copy.Parent = this;
            }

            this.sheet_name = _original.sheet_name;
            this.offset_from_parent = new Point(_original.offset_from_parent.X, _original.offset_from_parent.Y);

            this.node_name = string.Format(copyNameFormat, _original.node_name);
            this.subject = _original.subject;

            if (_original.properties != null)
            {
                this.properties = new ObservableDictionary<string, Type>();
                foreach (var entry in _original.properties)
                {
                    this.properties.Add(entry.Key, entry.Value);
                }
                this.properties.CollectionChanged -= this.Properties_CollectionChanged;
                this.properties.CollectionChanged += this.Properties_CollectionChanged;
            }

            this.range_of_values_pre_property = _original.range_of_values_pre_property;
            this.order_horizontally = _original.order_horizontally;

            this.prepend_content_to_children = _original.prepend_content_to_children;

            if (_original.patterns_to_match_in_property != null)
            {
                this.patterns_to_match_in_property = new ObservableCollection<(string propertyName, object filter)>();
                foreach (var entry in _original.PatternsToMatchInProperty)
                {
                    this.patterns_to_match_in_property.Add((entry.propertyName, ExcelMappingNode.CopyFilterObject(entry.filter)));
                }
            }

            this.nr_applications = 0;
            this.offset_btw_applications = _original.offset_btw_applications;

            this.MaxElementsToMap = _original.MaxElementsToMap;
            this.MaxHierarchyLevelsToTraverse = _original.MaxHierarchyLevelsToTraverse;
            this.nr_mapped_elements = 0;
            this.nr_traversed_element_levels = 0;

            this.strategy = _original.Strategy;
            this.node_is_active = _original.NodeIsActive;
            this.Version = ExcelMappingNode.CurrentRuleVersion;

            this.comps_passed_through_filter = new List<SimId>();
            this.visited_comps_by_me = new Dictionary<SimId, int>();
            this.visited_params_by_me = new List<long>();
            this.visited_geoms_by_me = new List<long>();
        }

        #endregion


        #region INFO

        public int NrAllChildrenRules()
        {
            int nr = 0;
            if (this.children == null) return nr;

            nr = this.children.Count;
            foreach (var child in this.children)
            {
                nr += child.NrAllChildrenRules();
            }
            return nr;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            //string info = (this.Parent == null) ? string.Empty : this.Parent.node_name + " -> ";
            //info += "Bereich \"" + this.node_name + "\" auf Blatt \"" + this.sheet_name + "\"";
            //return info;

            return this.ToIndentedString("");
        }

        private string ToIndentedString(string indent)
        {
            string info = indent + this.node_name + " [" + this.sheet_name + "]";
            if (string.IsNullOrEmpty(indent))
                indent = " ->";
            else
                indent = "   " + indent;
            foreach (var child in this.children)
            {
                info += Environment.NewLine + child.ToIndentedString(indent);
            }

            return info;
        }

        internal static object CopyFilterObject(object _o)
        {
            if (_o == null) return null;

            if (_o is string) return _o.ToString();
            if (_o is int) return (int)_o;
            if (_o is long) return (long)_o;
            if (_o is double) return (double)_o;
            if (_o is bool) return (bool)_o;

            if (_o is InstanceStateFilter instFilter)
            {
                return new InstanceStateFilter(instFilter.Type, instFilter.IsRealized);
            }

            if (_o is ICloneable)
                return ((ICloneable)_o).Clone();
            else
                return _o.ToString();
        }

        #endregion

        #region Rule Application

        public const int MAX_VISITS_PER_COMPONENT_IN_ONE_RULE = 100;

        public List<ExcelMappedData> ApplyRuleTo(SimComponent _comp, Point _offset, out ExcelMappingTrace trace)
        {
            List<ExcelMappedData> all_results = new List<ExcelMappedData>();
            trace = new ExcelMappingTrace();

            if (_comp == null) return all_results;

            List<SimId> all_visitable_comps = new List<SimId>();
            List<long> all_visitable_params = new List<long>();
            List<long> all_visitable_geom_rels = new List<long>();
            GetAllVisitableElements(_comp, ref all_visitable_comps, ref all_visitable_params, ref all_visitable_geom_rels);
            Dictionary<SimId, Dictionary<ExcelMappingNode, int>> all_visited_comp_records = all_visitable_comps.ToDictionary(x => x, x => new Dictionary<ExcelMappingNode, int>());
            //Dictionary<SimObjectId, int> all_visited_comps = all_visitable_comps.ToDictionary(x => x, x => 0);
            Dictionary<long, bool> all_visited_params = all_visitable_params.ToDictionary(x => x, x => false);
            Dictionary<long, bool> all_visited_instances = all_visitable_geom_rels.ToDictionary(x => x, x => false);
            Dictionary<long, List<bool>> all_visited_geom = all_visitable_geom_rels.ToDictionary(x => x, x => Enumerable.Repeat(false, 5).ToList());

            //Debug.WriteLine("RUN Excel Mapping");
            List<ExcelMappedData> result = new List<ExcelMappedData>();
            ExcelMappingTracker tracker = new ExcelMappingTracker();
            this.ApplyRuleOnceTo(_comp, _offset, tracker, ref result, ref all_visited_comp_records, ref all_visited_params, ref all_visited_geom, ref all_visited_instances, true, trace);
            all_results.AddRange(result);
            #region TRACE TREE
            if (TRACE_TREE_ON)
            {
                if (this.TraceTree != null)
                {
                    this.TraceTree.RecognizeRepeatsOfSameDepth();
                    this.TraceTree.SetDynamicOffsets_Pass1();
                    this.TraceTree.SetDynamicOffsets_Pass2();
                    Console.WriteLine(this.TraceTree.GetContent());
                    // this.TraceTree.AdaptResultOffsetsToRepeatCallsToSameRule();
                }

                Console.WriteLine(tracker.GetContent(false));
            }
            #endregion

            return all_results;
        }


        public void ApplyRuleOnceTo(SimComponent _comp, Point _starting_position, ExcelMappingTracker _tracker, ref List<ExcelMappedData> result,
                                    ref Dictionary<SimId, Dictionary<ExcelMappingNode, int>> _all_visits_to_comps, ref Dictionary<long, bool> _all_visited_params, ref Dictionary<long, List<bool>> _all_visited_geom, ref Dictionary<long, bool> _all_visited_instanes,
                                    bool _check_if_visited, ExcelMappingTrace _trace, List<ExcelMappedData> _from_parent = null)
        {
            #region DEBUG TMP
            if (DEBUG_TMP_ON)
            {
                //if ((_comp.ID.LocalId == 18349 || _comp.ID.LocalId == 18351) && this.node_name == "Öffnung")
                //{

                //}
            }
            #endregion

            if (!this.NodeIsActive)
                return;
            #region TRACE
            _trace.AddStep(this.GetRuleDepth(), this.node_name, _comp.ToInfoString(), ">> ENTERED ApplyRuleOnceTo " + _comp.LocalID.ToString() + " at " + _starting_position.ToString(), false, false);
            #endregion

            if (result == null)
                result = new List<ExcelMappedData>();
            if (_comp == null) return;

            #region TRACE
            _trace.AddStep(this.GetRuleDepth(), this.node_name, string.Empty, "NR MAPPED: " + this.nr_mapped_elements + " of " + this.max_elements_to_map + "; NR LEVELS: " + this.nr_traversed_element_levels + " of " + this.max_hierarchy_levels_to_traverse, false, false);
            #endregion
            if (this.nr_mapped_elements >= this.max_elements_to_map || this.nr_traversed_element_levels >= this.max_hierarchy_levels_to_traverse) return;

            // --------------------------------------- handle the COMPONENT node --------------------------------------------- //
            bool rule_was_applied_to_a_comp = false;
            int nr_mapped_elements_prev = this.nr_mapped_elements;

            List<ExcelMappedData> node_result = new List<ExcelMappedData>();
            bool node_was_applied = false;
            Point offset_next = new Point(0, 0);
            Point offset_prev = _starting_position;
            Point offset_start = new Point(_starting_position.X + this.nr_applications * this.offset_btw_applications.X,
                                           _starting_position.Y + this.nr_applications * this.offset_btw_applications.Y);
            #region DEBUG VERBOSE
            if (DEBUG_VERBOSE_ON)
                Console.WriteLine(">>>>>>>> " + this.NodeName + " for " + _comp.Name + " at prev (" + offset_prev.X + ", " + offset_prev.Y + ") and start (" + offset_start.X + ", " + offset_start.Y + ")");
            #endregion
            switch (this.subject)
            {
                case MappingSubject.Component:
                    #region TRACE
                    _trace.AddStep(this.GetRuleDepth(), this.node_name, _comp.ToInfoString(), "APPLYING...", false, false);
                    #endregion
                    (node_result, node_was_applied) = this.ApplyMappingToSingleComponent(_comp, offset_start, _tracker, _check_if_visited, _trace, ref _all_visits_to_comps, out offset_next);
                    if (node_was_applied)
                    {
                        this.nr_applications++;
                        this.nr_mapped_elements++;
                        #region TRACE
                        // _trace.AddStep(this.GetRuleDepth(), this.node_name, string.Empty, "NR APPLICATIONS: " + this.nr_applications.ToString() + "; NR MAPPED ELEMS: " + this.nr_mapped_elements, false, false);
                        #endregion
                        #region TRACE TREE
                        if (TRACE_TREE_ON)
                            this.TraceTree = new ExcelMappingTraceTree(this.Parent?.TraceTree, this.NodeName, "[" + this.nr_applications + "]", this.SummarizeOffsets(), _comp.LocalID, (node_result.Count > 0) ? node_result[0] : ExcelMappedData.CreateEmpty(string.Empty, new Point4D()));
                        #endregion
                    }
                    else
                    {
                        if (this.Version >= 1)
                            offset_next = offset_prev;
                        #region TRACE
                        _trace.AddStep(this.GetRuleDepth(), this.node_name, _comp.ToInfoString(), "BEGIN Recursion Components", true, false, false);
                        #endregion
                        // component did not pass filter -> look deeper
                        // ============================================================================================================= //
                        // --------------------------------------- component recursion, same rule -------------------------------------- //
                        // ============================================================================================================= //


                        if (this.Strategy != TraversalStrategy.REFERENCES_ONLY)
                        {
                            #region DEBUG VERBOSE
                            if (DEBUG_VERBOSE_ON)
                                Console.WriteLine("------------------------------------------------ 1 not REFERENCES_ONLY");
                            #endregion
                            foreach (var entry in _comp.Components)
                            {
                                this.GetCallStack().Push((_comp, entry.Slot));

                                SimComponent sC = entry.Component;
                                var nrv = GetNrVisitsBy(_all_visits_to_comps, sC, this);
                                #region DEBUG VERY VERBOSE
                                if (DEBUG_VERY_VERBOSE_ON)
                                    Console.WriteLine("[][][][][][][][][][] before A1 Visit Nr " + nrv + " for " + sC.LocalID + " by " + this.NodeName);
                                #endregion

                                if (sC != null && nrv == 0)
                                {
                                    #region TRACE
                                    _trace.AddStep(this.GetRuleDepth() + 1, this.node_name, _comp.ToInfoString(), ">> ApplyRuleOnceTo in Subtree: " + sC.ToInfoString(), false, false, false);
                                    #endregion
                                    this.nr_traversed_element_levels++;
                                    #region DEBUG VERBOSE
                                    if (DEBUG_VERBOSE_ON)
                                        Console.WriteLine(">>>>>> " + this.NodeName + " for " + _comp.Id.LocalId + ":" + _comp.Name + ">>>>>>   A1");
                                    #endregion
                                    this.ApplyRuleOnceTo(sC, offset_next, _tracker, ref result, ref _all_visits_to_comps, ref _all_visited_params, ref _all_visited_geom, ref _all_visited_instanes, true, _trace, _from_parent);
                                    this.nr_traversed_element_levels--;
                                }

                                this.GetCallStack().Pop();
                            }
                        }

                        if (this.Strategy != TraversalStrategy.SUBTREE_ONLY)
                        {
                            #region DEBUG VERBOSE
                            if (DEBUG_VERBOSE_ON)
                                Console.WriteLine("------------------------------------------------ 1 not SUBTREE_ONLY");
                            #endregion
                            var copy_rComps = new SortedList<string, SimComponentReference>(
                                _comp.ReferencedComponents.Where(x => x.Target != null)
                                    .ToDictionary(x => x.Slot.ToSerializerString(), x => x));
                            foreach (var entry in copy_rComps)
                            {
                                this.GetCallStack().Push((_comp, entry.Value.Slot));

                                SimComponent rC = entry.Value.Target;
                                var nrv = GetNrVisitsBy(_all_visits_to_comps, rC, this);
                                var limit = Math.Max(GetMaxNrOfReferencesToReferenceChain(rC, this.TotalNrTraversedLevels()), ExcelMappingNode.MAX_VISITS_PER_COMPONENT_IN_ONE_RULE);
                                #region DEBUG VERY VERBOSE
                                if (DEBUG_VERY_VERBOSE_ON)
                                    Console.WriteLine("[][][][][][][][][][] before A2 Visit Nr " + nrv + "/ " + limit + " for " + rC.LocalID + " by " + this.NodeName);
                                #endregion

                                if (rC != null && nrv < limit)
                                {
                                    #region TRACE
                                    _trace.AddStep(this.GetRuleDepth() + 1, this.node_name, _comp.ToInfoString(), ">> ApplyRuleOnceTo in References: " + rC.ToInfoString(), false, false, false);
                                    #endregion
                                    this.nr_traversed_element_levels++;
                                    #region DEBUG VERBOSE
                                    if (DEBUG_VERBOSE_ON)
                                        Console.WriteLine(">>>>>> " + this.NodeName + " for " + _comp.Id.LocalId + ":" + _comp.Name + ">>>>>>   A2");
                                    #endregion
                                    this.ApplyRuleOnceTo(rC, offset_next, _tracker, ref result, ref _all_visits_to_comps, ref _all_visited_params, ref _all_visited_geom, ref _all_visited_instanes, false, _trace, _from_parent);
                                    this.nr_traversed_element_levels--;
                                }

                                this.GetCallStack().Pop();
                            }
                        }

                        #region TRACE
                        _trace.AddStep(this.GetRuleDepth(), this.node_name, _comp.ToInfoString(), "END Recursion Components", false, true, false);
                        _trace.AddStep(this.GetRuleDepth(), this.node_name, string.Empty, "END: traversed levels " + this.nr_traversed_element_levels, false, false, false);
                        _trace.AddStep(this.GetRuleDepth(), this.node_name, string.Empty, "END: visited locally: "/* + Summarize(this.visited_comps_by_me)*/, false, false, false);
                        #endregion
                    }
                    break;
                case MappingSubject.Parameter:
                    (node_result, node_was_applied) = this.ApplyMappingToSingleParameter(_comp, offset_start, _tracker, _trace, ref _all_visited_params, out offset_next);
                    if (node_was_applied)
                    {
                        this.nr_applications++;
                        #region TRACE TREE
                        if (TRACE_TREE_ON)
                            this.TraceTree = new ExcelMappingTraceTree(this.Parent?.TraceTree, this.NodeName, "[" + this.nr_applications + "]", this.SummarizeOffsets(), _comp.LocalID, (node_result.Count > 0) ? node_result[0] : ExcelMappedData.CreateEmpty(string.Empty, new Point4D()));
                        #endregion
                    }
                    break;
                case MappingSubject.Geometry:
                case MappingSubject.GeometryPoint:
                case MappingSubject.GeometryArea:
                case MappingSubject.GeometricIncline:
                case MappingSubject.GeometricOrientation:
                    (node_result, node_was_applied) = this.ApplyMappingToSingleGeomR(_comp, offset_start, _tracker, _trace, ref _all_visited_geom, out offset_next);
                    if (node_was_applied)
                    {
                        this.nr_applications++;
                        #region TRACE TREE
                        if (TRACE_TREE_ON)
                            this.TraceTree = new ExcelMappingTraceTree(this.Parent?.TraceTree, this.NodeName, "[" + this.nr_applications + "]", this.SummarizeOffsets(), _comp.LocalID, (node_result.Count > 0) ? node_result[0] : ExcelMappedData.CreateEmpty(string.Empty, new Point4D()));
                        #endregion
                    }
                    break;
                case MappingSubject.Instance:
                    var offset_start_i = offset_start;
                    int counter = 0;
                    foreach (var instance in _comp.Instances)
                    {
                        if (counter == 0)
                            (node_result, node_was_applied) = this.ApplyMappingToSingleInstance(_comp, offset_start_i, _tracker, _trace, ref _all_visited_instanes, true, out offset_next);
                        else
                        {
                            (var tmp_result, var tmp_was_applied) = this.ApplyMappingToSingleInstance(_comp, offset_start_i, _tracker, _trace, ref _all_visited_instanes, false, out offset_next);
                            if (tmp_was_applied)
                                node_result.AddRange(tmp_result);
                            node_was_applied |= tmp_was_applied;
                        }

                        offset_start_i = offset_next;
                        if (node_was_applied)
                        {
                            this.nr_applications++;
                            #region TRACE TREE
                            if (TRACE_TREE_ON)
                                this.TraceTree = new ExcelMappingTraceTree(this.Parent?.TraceTree, this.NodeName, "[" + this.nr_applications + "]", this.SummarizeOffsets(), _comp.LocalID, (node_result.Count > 0) ? node_result[0] : ExcelMappedData.CreateEmpty(string.Empty, new Point4D()));
                            #endregion
                        }
                        counter++;
                    }
                    break;
            }
            if (_from_parent != null)
            {
                List<ExcelMappedData> copy_from_parent = ExcelMappedData.Copy(_from_parent);
                copy_from_parent.ForEach(x => x.OffsetBy(new Point((this.nr_applications - 1) * this.offset_btw_applications.X,
                                                                   (this.nr_applications - 1) * this.offset_btw_applications.Y)));
                result.AddRange(copy_from_parent);
            }
            result.AddRange(node_result);

            rule_was_applied_to_a_comp = (nr_mapped_elements_prev < this.nr_mapped_elements);
            #region DEBUG VERBOSE
            if (DEBUG_VERBOSE_ON)
            {
                Console.WriteLine(">>><<< " + this.NodeName + " for " + _comp.Name + " >>><<< B condition: " + nr_mapped_elements_prev + " <? " + this.nr_mapped_elements);
                Console.WriteLine(">>><<< " + this.NodeName + " for " + _comp.Name + " >>><<< was applied: " + node_was_applied);
            }
            #endregion
            #region TRACE
            if (node_was_applied)
            {
                _trace.AddStep(this.GetRuleDepth() + 1, this.node_name, string.Empty, "applied in THIS ROUND", false, false);
            }
            #endregion

            // ============================================================================================================= //
            // ------------------------------------- rule recursion AND component recursion -------------------------------- //
            // ============================================================================================================= //
            if (node_was_applied)
            {
                #region TRACE
                _trace.AddStep(this.GetRuleDepth(), this.node_name, string.Empty, "BEGIN Recursion Rules", true, false);
                #endregion                
                foreach (ExcelMappingNode sN in this.children)
                {
                    List<ExcelMappedData> to_prepend = (this.prepend_content_to_children) ? new List<ExcelMappedData>(node_result) : null;
                    if (sN.subject == MappingSubject.Component)
                    {
                        // ------------------------------------------ DOUBLE RECURSION ------------------------------------- //
                        if (sN.Strategy != TraversalStrategy.REFERENCES_ONLY)
                        {
                            #region DEBUG VERBOSE
                            if (DEBUG_VERBOSE_ON)
                                Console.WriteLine("------------------------------------------------ 2 not REFERENCES_ONLY");
                            #endregion
                            foreach (var entry in _comp.Components)
                            {
                                this.GetCallStack().Push((_comp, entry.Slot));

                                SimComponent sC = entry.Component;
                                var nrv = GetNrVisitsBy(_all_visits_to_comps, sC, sN);
                                #region DEBUG VERY VERBOSE
                                if (DEBUG_VERY_VERBOSE_ON)
                                    Console.WriteLine("[][][][][][][][][][] before B1 Visit Nr " + nrv + " for " + sC.LocalID + " by " + sN.NodeName);
                                #endregion

                                if (sC != null && nrv == 0)
                                {
                                    #region TRACE
                                    _trace.AddStep(Math.Max(0, this.GetRuleDepth() - 1), this.node_name, string.Empty, sN.NodeName + " >> ApplyRuleOnceTo in Subtree: " + sC.ToInfoString(), false, false);
                                    #endregion
                                    #region DEBUG VERBOSE 
                                    if (DEBUG_VERBOSE_ON)
                                        Console.WriteLine(">>>>>> " + sN.NodeName + " for " + _comp.Id.LocalId + ":" + sC.Name + ">>>>>>   B1");
                                    #endregion
                                    sN.ApplyRuleOnceTo(sC, offset_next, _tracker, ref result, ref _all_visits_to_comps, ref _all_visited_params, ref _all_visited_geom, ref _all_visited_instanes, true, _trace, to_prepend);
                                }

                                this.GetCallStack().Pop();
                            }
                        }


                        if (sN.Strategy != TraversalStrategy.SUBTREE_ONLY)
                        {
                            #region DEBUG VERBOSE
                            if (DEBUG_VERBOSE_ON)
                                Console.WriteLine("------------------------------------------------ 2 not SUBTREE_ONLY");
                            #endregion
                            var copy_rComps = new SortedList<string, SimComponentReference>(
                                _comp.ReferencedComponents.Where(x => x.Target != null)
                                    .ToDictionary(x => x.Slot.ToSerializerString(), x => x));
                            foreach (var entry in copy_rComps)
                            {
                                this.GetCallStack().Push((_comp, entry.Value.Slot));

                                SimComponent rC = entry.Value.Target;
                                var nrv = GetNrVisitsBy(_all_visits_to_comps, rC, sN);
                                var limit = Math.Max(GetMaxNrOfReferencesToReferenceChain(rC, sN.TotalNrTraversedLevels()), ExcelMappingNode.MAX_VISITS_PER_COMPONENT_IN_ONE_RULE);
                                #region DEBUG VERY VERBOSE
                                if (DEBUG_VERY_VERBOSE_ON)
                                    Console.WriteLine("[][][][][][][][][][] before B2 Visit Nr " + nrv + " / " + limit + " for " + rC.LocalID + " by " + sN.NodeName);
                                #endregion

                                if (rC != null && nrv < limit)
                                {
                                    #region TRACE
                                    _trace.AddStep(Math.Max(0, this.GetRuleDepth() - 1), this.node_name, string.Empty, sN.NodeName + " >> ApplyRuleOnceTo in References: " + rC.ToInfoString(), false, false);
                                    #endregion
                                    #region DEBUG VERBOSE
                                    if (DEBUG_VERBOSE_ON)
                                        Console.WriteLine(">>>>>> " + sN.NodeName + " for " + _comp.Id.LocalId + ":" + rC.Name + ">>>>>>   B2");
                                    #endregion
                                    sN.ApplyRuleOnceTo(rC, offset_next, _tracker, ref result, ref _all_visits_to_comps, ref _all_visited_params, ref _all_visited_geom, ref _all_visited_instanes, false, _trace, to_prepend);
                                }

                                this.GetCallStack().Pop();
                            }
                        }
                    }
                    else
                    {
                        // ---------------------------------------- SIMPLE RULE RECURSION ---------------------------------- //
                        #region DEBUG VERBOSE
                        if (DEBUG_VERBOSE_ON)
                            Console.WriteLine(">>>>>> " + sN.NodeName + " for " + _comp.Id.LocalId + ":" + _comp.Name + ">>>>>>   B3");
                        #endregion
                        sN.ApplyRuleOnceTo(_comp, offset_next, _tracker, ref result, ref _all_visits_to_comps, ref _all_visited_params, ref _all_visited_geom, ref _all_visited_instanes, true, _trace, to_prepend);
                    }

                    // 14.10.2020 reset the number of applications of the child node:
                    if (this.Version == 0)
                    {
                        // do nothing for the old rules   
                    }
                    else
                    {
                        sN.nr_applications = 0;
                    }
                }
                #region DEBUG VERBOSE
                if (DEBUG_VERBOSE_ON)
                    Console.WriteLine("<<<<<< " + this.NodeName + " RULE RECURSION COMPLETE at " + _comp.Name);
                #endregion
                #region TRACE
                _trace.AddStep(this.GetRuleDepth(), this.node_name, string.Empty, "END Recursion Rules", false, true);
                _trace.AddStep(this.GetRuleDepth(), this.node_name, string.Empty, "END: traversed levels " + this.nr_traversed_element_levels, false, false);
                //_trace.AddStep(this.GetRuleDepth(), this.node_name, string.Empty, "END: visited locally: " + Summarize(this.visited_comps_by_me), false, false);
                #endregion
            }

            #region DEBUG TMP
            if (DEBUG_TMP_ON)
            {
                if (this.comps_passed_through_filter.Any(x => x.LocalId == 18399))
                    Console.WriteLine("DO NOT DELETE 18399");
            }
            #endregion
            // 21.09.2020: allow revisiting of components by other rules (unless they were already mapped)
            ExcelMappingNode.Subtract(_all_visits_to_comps, this.visited_comps_by_me, this.comps_passed_through_filter, this);
            this.ResetValidChildVisits();
        }

        public void Reset()
        {
            this.nr_applications = 0;
            this.nr_mapped_elements = 0;
            this.nr_traversed_element_levels = 0;
            this.comps_passed_through_filter.Clear();
            this.visited_comps_by_me.Clear();
            this.visited_params_by_me.Clear();
            this.visited_geoms_by_me.Clear();

            foreach (var child in this.children)
            {
                child.Reset();
            }
        }

        public void ResetValidChildVisits()
        {
            foreach (var child in this.children)
            {
                child.comps_passed_through_filter.Clear();
                child.ResetValidChildVisits();
            }
        }
        #endregion

        #region Rule Application: Type Specific (Component, Parameter, Instance)

        private (List<ExcelMappedData> result, bool wasApplied) ApplyMappingToSingleComponent(SimComponent _comp, Point _starting_position, ExcelMappingTracker _tracker, bool _check_if_visited, ExcelMappingTrace _trace,
                                                                        ref Dictionary<SimId, Dictionary<ExcelMappingNode, int>> all_visits_to_comps, out Point position_after)
        {
            #region DEBUG
            if (DEBUG_IN_SUBRULES_ON)
                Debug.WriteLine(this.node_name + " ApplyMappingToSingleComponent " + _comp.ToInfoString() + " at " + _starting_position.ToString());
            #endregion

            List<ExcelMappedData> result = new List<ExcelMappedData>();
            position_after = _starting_position;

            if (this.subject != MappingSubject.Component) return (result, false);
            if (_comp == null) return (result, false);

            // ................................................ visits global ..................................... //
            #region DEBUG TMP
            if (DEBUG_TMP_ON)
            {
                if (all_visits_to_comps[_comp.Id].ContainsKey(this) && all_visits_to_comps[_comp.Id][this] == 21)
                {

                }
            }
            #endregion
            if (_check_if_visited && all_visits_to_comps[_comp.Id].ContainsKey(this) && all_visits_to_comps[_comp.Id][this] > 0) return (result, false);
            if (all_visits_to_comps[_comp.Id].ContainsKey(this) && all_visits_to_comps[_comp.Id][this] > Math.Max(GetMaxNrOfReferencesToReferenceChain(_comp, this.TotalNrTraversedLevels()), ExcelMappingNode.MAX_VISITS_PER_COMPONENT_IN_ONE_RULE)) return (result, false);
            AddVisit(all_visits_to_comps, _comp, this);
            // ................................................ visits global ..................................... //

            // ============================================== visits local ========================================= //
            if (this.visited_comps_by_me.ContainsKey(_comp.Id))
                this.visited_comps_by_me[_comp.Id] += 1;
            else
                this.visited_comps_by_me.Add(_comp.Id, 1);
            // ============================================== visits local ========================================= //

            // apply filter
            bool comp_passes_filter = ExcelMappingNode.InstancePassesFilter<SimComponent>(_comp, this.PatternsToMatchInProperty,
                this.GetCallStack());
            if (!comp_passes_filter) return (result, false);
            #region TRACE
            _trace.AddStep(this.GetRuleDepth() + 1, this.node_name, _comp.ToInfoString(), _comp.ToInfoString() + " ---------------------------------- PASSED FILTER!", false, false);
            #endregion
            this.comps_passed_through_filter.Add(_comp.Id);

            Point current_pos = new Point(_starting_position.X + this.offset_from_parent.X, _starting_position.Y + this.offset_from_parent.Y);
            // ............................................. tracking .............................................. //
            current_pos = this.PerformOffsetCorrectionBasedOnTracking(_tracker, current_pos, _comp.Name, true, TRACKER_IS_VERBOSE);
            // ............................................. tracking .............................................. //

            // get component property values (size 1x1 only)
            List<object> property_values = ExcelMappingNode.GetPropertyValues<SimComponent>(_comp, this.properties);
            // map them to the correct range
            foreach (object o in property_values)
            {
                ExcelMappedData m = null;
                if (o is double)
                    m = ExcelMappedData.MapOneDoubleTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, 1, 1), (double)o);
                else
                    m = ExcelMappedData.MapOneStringTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, 1, 1), o.ToString());

                if (m != null)
                {
                    result.Add(m);
                    // advance current position
                    if (this.order_horizontally)
                        current_pos = new Point(current_pos.X + 1, current_pos.Y);
                    else
                        current_pos = new Point(current_pos.X, current_pos.Y + 1);
                }
            }

            position_after = current_pos;
            // ............................................. tracking .............................................. //
            _tracker.AddMappingRecord(this, this.nr_applications, true, result);
            // ............................................. tracking .............................................. //
            return (result, true);
        }

        private Point PerformOffsetCorrectionBasedOnTracking(ExcelMappingTracker _tracker, Point _current_pos, string _info, bool _correct, bool _verbose)
        {
            Point new_pos = _current_pos;
            // ............................................. tracking .............................................. //
            if (_verbose)
                Console.WriteLine("- - - " + this.NodeName + "[" + this.nr_applications + "] at " + new_pos.X + "; " + new_pos.Y + " - - - " + _info);

            if (_verbose)
                Console.Write("BB FULL \t\t\t\t before  -> ");
            var bb_FULL = _tracker.GetFullBoundingBox();
            if (_verbose)
                Console.WriteLine(bb_FULL.GetContent());

            if (_verbose)
                Console.Write("BB COMPLETE PARENT \t\t before  -> ");
            var bb_P = _tracker.GetBoundingBoxOfParent(this);
            if (_verbose)
                Console.WriteLine(bb_P.GetContent());

            if (_verbose)
                Console.Write("BB PARENT \t\t\t\t before  -> ");
            var bb_Ps = _tracker.GetBoundingBoxOfParentWoChildren(this);
            if (_verbose)
                Console.WriteLine(bb_Ps.GetContent());

            if (_verbose)
                Console.Write("BB LAST APPL \t\t\t before  -> ");
            var bb_L = _tracker.GetBoundingBoxOfLastApplicationOf(this);
            if (_verbose)
                Console.WriteLine(bb_L.GetContent());

            if (_verbose)
            {
                Console.Write("Extents element \t\t before  -> ");
                Console.WriteLine(_tracker.ExtentsOfLast.GetContent());
            }
            // ......................................... tracking reaction ......................................... //
            if (_correct)
            {
                if (this.Version >= 1 && (this.Parent != null || this.nr_applications > 0))
                {
                    // compare with last application of the same rule (differentiate btw single line and multiline rules!)
                    if (!this.OrderHorizontally && new_pos.X < bb_L.EndX)
                        new_pos = new Point(bb_L.EndX + Math.Max(1, this.offset_btw_applications.X), new_pos.Y);
                    if (this.OrderHorizontally && new_pos.Y < bb_L.EndY)
                        new_pos = new Point(new_pos.X, bb_L.EndY + Math.Max(1, this.offset_btw_applications.Y));

                    if (!this.OrderHorizontally && bb_L.SizeX > 1 && new_pos.X <= bb_L.EndX)
                        new_pos = new Point(bb_L.EndX + Math.Max(1, this.offset_btw_applications.X), new_pos.Y);
                    if (this.OrderHorizontally && bb_L.SizeY > 1 && new_pos.Y <= bb_L.EndY)
                        new_pos = new Point(new_pos.X, bb_L.EndY + Math.Max(1, this.offset_btw_applications.Y));

                    // compare with the element that came before
                    if ((!this.OrderHorizontally && new_pos.X < _tracker.ExtentsOfLast.EndX) ||
                        (this.OrderHorizontally && new_pos.Y < _tracker.ExtentsOfLast.EndY))
                    {
                        Point p = new_pos;
                        int i = 1;
                        while (_tracker.CausesOverlap(p))
                        {
                            if (!this.OrderHorizontally)
                                p = new Point(bb_P.EndX + i, p.Y);
                            else
                                p = new Point(p.X, bb_P.EndY + i);
                            i++;
                        }
                        new_pos = p;
                    }

                }
                if (_verbose)
                    Console.WriteLine("corrected " + new_pos.X + "; " + new_pos.Y);
            }
            // ............................................. tracking .............................................. //
            return new_pos;
        }


        private (List<ExcelMappedData> result, bool wasApplied) ApplyMappingToSingleParameter(SimComponent _comp_parent, Point _starting_position, ExcelMappingTracker _tracker, ExcelMappingTrace _trace,
                                                                        ref Dictionary<long, bool> all_visited_params, out Point position_after)
        {
            List<ExcelMappedData> result = new List<ExcelMappedData>();
            position_after = _starting_position;

            if (this.subject != MappingSubject.Parameter) return (result, false);
            if (_comp_parent == null) return (result, false);

            // check if the parent passed the filter of the parent rule added 12.09.2018
            if (this.Parent != null)
            {
                if (!(this.Parent.comps_passed_through_filter.Contains(_comp_parent.Id)))
                    return (result, false);
            }

            // apply filter
            SimParameter p = null;
            foreach (var param in _comp_parent.Parameters)
            {
                bool param_passes_filter = ExcelMappingNode.InstancePassesFilter<SimParameter>(param, this.PatternsToMatchInProperty,
                    this.GetCallStack());
                if (param_passes_filter && !all_visited_params[param.Id.LocalId])
                {
                    all_visited_params[param.Id.LocalId] = true;
                    p = param;
                    break;
                }
            }
            if (p == null) return (result, false);
            #region TRACE
            {
                var infoString = "{" + p.Id.LocalId + "}" + p.Name + " " + p.ValueCurrent.ToString("F2");
                _trace.AddStep(this.GetRuleDepth() + 1, this.node_name, infoString, infoString + " ---------------------------------- PASSED FILTER!", false, false);
            }
            #endregion

            Point current_pos = new Point(_starting_position.X + this.offset_from_parent.X, _starting_position.Y + this.offset_from_parent.Y);
            // ............................................. tracking ............................................... //
            current_pos = this.PerformOffsetCorrectionBasedOnTracking(_tracker, current_pos, p.Name, false, TRACKER_IS_VERBOSE);
            // ............................................. tracking ............................................... //

            // 1. get parameter property values (size 1x1 only)
            List<object> property_values = ExcelMappingNode.GetPropertyValues<SimParameter>(p, this.properties);
            // 2. get table values, if required
            double[,] values = null;
            if (this.range_of_values_pre_property != ExcelMappingRange.SingleValue &&
                p.MultiValuePointer != null && p.MultiValuePointer is SimMultiValueBigTable.SimMultiValueBigTablePointer && this.properties.ContainsKey("ValueCurrent"))
            {
                var all_values = ExcelMappingNode.GetCurrentValueTableOf(_comp_parent, p);
                var mvbtPointer = (SimMultiValueBigTable.SimMultiValueBigTablePointer)p.MultiValuePointer;

                if (this.order_horizontally)
                {
                    // take column
                    if (this.range_of_values_pre_property == ExcelMappingRange.VectorValues)
                    {
                        values = new double[all_values.GetLength(0), 1];
                        for (int i = 0; i < all_values.GetLength(0); ++i)
                            values[i, 0] = all_values[i, mvbtPointer.Column];
                    }
                    else
                        values = all_values;
                }
                else
                {
                    // take row
                    if (this.range_of_values_pre_property == ExcelMappingRange.VectorValues)
                    {
                        values = new double[1, all_values.GetLength(1)];
                        for (int i = 0; i < all_values.GetLength(1); ++i)
                            values[0, i] = all_values[mvbtPointer.Row, i];
                    }
                    else
                        values = all_values;
                }
            }

            // map them to the correct range
            int counter = 0;
            foreach (object o in property_values)
            {
                ExcelMappedData m = null;
                if (values != null && this.properties.ElementAt(counter).Key == "ValueCurrent")
                    m = ExcelMappedData.MapDoublesTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, values.GetLength(1), values.GetLength(0)), values);
                else if (o is double)
                    m = ExcelMappedData.MapOneDoubleTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, 1, 1), (double)o);
                else
                    m = ExcelMappedData.MapOneStringTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, 1, 1), o.ToString());

                if (m != null)
                {
                    result.Add(m);
                    // advance current position
                    if (this.order_horizontally)
                        current_pos = new Point(current_pos.X + m.SizeInColumns, current_pos.Y + m.SizeInRows - 1);
                    else
                        current_pos = new Point(current_pos.X + m.SizeInColumns - 1, current_pos.Y + m.SizeInRows);

                }
                counter++;
            }

            position_after = current_pos;
            // ............................................. tracking ............................................... //
            _tracker.AddMappingRecord(this, this.nr_applications, true, result);
            // ............................................. tracking ............................................... // 
            return (result, true);
        }


        private (List<ExcelMappedData> result, bool wasApplied) ApplyMappingToSingleInstance(SimComponent _comp_parent, Point _starting_position, ExcelMappingTracker _tracker, ExcelMappingTrace _trace,
                                                                        ref Dictionary<long, bool> _all_visited_instances, bool _map_p_names, out Point position_after)
        {
            List<ExcelMappedData> result = new List<ExcelMappedData>();
            position_after = _starting_position;

            if (this.subject != MappingSubject.Instance) return (result, false);
            if (_comp_parent == null) return (result, false);

            // check if the parent passed the filter of the parent rule
            if (this.Parent != null)
            {
                if (!(this.Parent.comps_passed_through_filter.Contains(_comp_parent.Id)))
                    return (result, false);
            }

            // no filter here
            SimComponentInstance gr = null;
            foreach (SimComponentInstance gr_to_test in _comp_parent.Instances)
            {
                if (!_all_visited_instances[gr_to_test.Id.LocalId])
                {
                    _all_visited_instances[gr_to_test.Id.LocalId] = true;
                    gr = gr_to_test;
                    break;
                }
            }
            if (gr == null) return (result, false);
            #region TRACE
            _trace.AddStep(this.GetRuleDepth() + 1, this.node_name, gr.ToString(), gr.ToString() + " ---------------------------------- SELECTED FOR MAPPING!", false, false);
            #endregion

            Point current_pos = (_map_p_names) ? new Point(_starting_position.X + this.offset_from_parent.X, _starting_position.Y + this.offset_from_parent.Y) : _starting_position;
            // ............................................. tracking ............................................... //
            current_pos = this.PerformOffsetCorrectionBasedOnTracking(_tracker, current_pos, gr.Name, true, TRACKER_IS_VERBOSE);
            // ............................................. tracking ............................................... //

            #region DEBUG TMP
            //if (DEBUG_TMP_ON)
            //    Debug.WriteLine("INSTANCE B current pos: {0}", current_pos.ToString(), 0);
            #endregion
            // 1. get parameter property values (size 1x1 only)
            List<object> property_values = ExcelMappingNode.GetPropertyValues<SimComponentInstance>(gr, this.properties);
            // 2. get parameter instance values, if required
            List<List<string>> all_labels = new List<List<string>>();
            double[,] all_values;
            if (this.range_of_values_pre_property != ExcelMappingRange.SingleValue &&
                (this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesTemporary)) ||
                 this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesPersistent))))
            {
                SimInstanceParameterCollection values = null;

                if (this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesTemporary)))
                    values = gr.InstanceParameterValuesTemporary;
                else
                    values = gr.InstanceParameterValuesPersistent;

                if (this.order_horizontally)
                {
                    all_values = new double[1, values.Count];
                    var labels = new List<string>();

                    foreach (var p in values)
                    {
                        labels.Add(p.Key.Name);
                        all_values[1, labels.Count - 1] = p.Value;
                    }

                    all_labels.Add(labels);
                }
                else
                {
                    all_values = new double[values.Count, 1];

                    foreach (var p in values)
                    {
                        all_labels.Add(new List<string> { p.Key.Name });
                        all_values[all_labels.Count - 1, 1] = p.Value;
                    }
                }
            }
            else
                all_values = new double[,] { };

            // map them to the correct range
            int counter = 0;
            bool p_names_included = (this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesTemporary)) ||
                this.properties.ContainsKey(nameof(SimComponentInstance.InstanceParameterValuesPersistent)));
            Point cumulative_offset = new Point();
            foreach (object o in property_values)
            {
                ExcelMappedData m1 = null;
                ExcelMappedData m2 = null;
                Point m_offset = this.order_horizontally ? new Point(0, 1) : new Point(1, 0);
                if (this.properties.ElementAt(counter).Key == nameof(SimComponentInstance.InstanceParameterValuesTemporary) ||
                    this.properties.ElementAt(counter).Key == nameof(SimComponentInstance.InstanceParameterValuesPersistent))
                {
                    if (_map_p_names)
                        m1 = ExcelMappedData.MapStringsTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, all_labels[0].Count, all_labels.Count), all_labels);
                    m_offset = (m1 == null) ? (this.order_horizontally ? new Point(0, 1) : new Point(1, 0)) :
                                                    (this.order_horizontally ? new Point(0, m1.SizeInRows) : new Point(m1.SizeInColumns, 0));
                    m2 = ExcelMappedData.MapDoublesTo(this.sheet_name, new Point4D(current_pos.X + m_offset.X, current_pos.Y + m_offset.Y, all_values.GetLength(1), all_values.GetLength(0)), all_values);
                }
                else if (o is double)
                {
                    if (p_names_included)
                        m1 = ExcelMappedData.MapOneDoubleTo(this.sheet_name, new Point4D(current_pos.X + m_offset.X, current_pos.Y + m_offset.Y, 1, 1), (double)o);
                    else
                        m1 = ExcelMappedData.MapOneDoubleTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, 1, 1), (double)o);
                }
                else
                {
                    if (p_names_included)
                        m1 = ExcelMappedData.MapOneStringTo(this.sheet_name, new Point4D(current_pos.X + m_offset.X, current_pos.Y + m_offset.Y, 1, 1), o.ToString());
                    else
                        m1 = ExcelMappedData.MapOneStringTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, 1, 1), o.ToString());
                }

                if (m1 != null)
                    result.Add(m1);
                if (m2 != null)
                    result.Add(m2);

                // advance current position
                int m1SizeInColumns = (m1 == null) ? 0 : m1.SizeInColumns;
                int m1SizeInRows = (m1 == null) ? 0 : m1.SizeInRows;
                int m2SizeInColumns = (m2 == null) ? 0 : m2.SizeInColumns;
                int m2SizeInRows = (m2 == null) ? 0 : m2.SizeInRows;

                if (this.order_horizontally)
                {
                    current_pos = new Point(current_pos.X + m1SizeInColumns + m2SizeInColumns, current_pos.Y);
                    cumulative_offset = new Point(cumulative_offset.X + m1SizeInColumns + m2SizeInColumns, cumulative_offset.Y + Math.Max(m1SizeInRows, m2SizeInRows));
                }
                else
                {
                    current_pos = new Point(current_pos.X, current_pos.Y + m1SizeInRows + m2SizeInRows);
                    cumulative_offset = new Point(cumulative_offset.X + Math.Max(m1SizeInColumns, m2SizeInColumns), cumulative_offset.Y + m1SizeInRows + m2SizeInRows);
                }

                if (counter == property_values.Count - 1)
                {
                    if (!this.order_horizontally)
                        current_pos = new Point(current_pos.X + Math.Max(m1SizeInColumns, m2SizeInColumns), current_pos.Y);
                    else
                        current_pos = new Point(current_pos.X, current_pos.Y + Math.Max(m1SizeInRows, m2SizeInRows));
                    if (this.order_horizontally)
                        current_pos.X -= cumulative_offset.X;
                    else
                        current_pos.Y -= cumulative_offset.Y;
                }
                counter++;
            }
            position_after = current_pos;
            // ............................................. tracking ............................................... //
            _tracker.AddMappingRecord(this, this.nr_applications, true, result);
            // ............................................. tracking ............................................... //
            return (result, true);
        }

        #endregion

        #region Rule Application: Type Specific (Geometry)

        public (List<ExcelMappedData> result, bool wasApplied) ApplyMappingToSingleGeomR(SimComponent _comp_parent, Point _starting_position, ExcelMappingTracker _tracker, ExcelMappingTrace _trace,
                                                                    ref Dictionary<long, List<bool>> all_visited_geom, out Point position_after)
        {
            #region TRACE
            _trace.AddStep(this.GetRuleDepth() + 1, this.node_name, _comp_parent.ToInfoString(), "ApplyMappingToSingleGeomR " + _comp_parent.ToInfoString() + " at " + _starting_position.ToString(), false, false);
            #endregion
            List<ExcelMappedData> result = new List<ExcelMappedData>();
            position_after = _starting_position;

            if (this.subject == MappingSubject.Component ||
                this.subject == MappingSubject.Parameter) return (result, false);
            if (_comp_parent == null) return (result, false);

            // apply filter
            int visitor_index = ExcelMappingNode.ToIndex(this.subject);
            SimComponentInstance gr = null;

            if (_comp_parent.InstanceType == SimInstanceType.GeometricSurface)
            {
                foreach (SimComponentInstance gr_to_test in _comp_parent.Instances)
                {
                    bool gr_passes_filter = ExcelMappingNode.InstancePassesFilter<SimComponentInstance>(gr_to_test, this.PatternsToMatchInProperty,
                        this.GetCallStack());
                    if (gr_passes_filter && !all_visited_geom[gr_to_test.Id.LocalId][visitor_index])
                    {
                        all_visited_geom[gr_to_test.Id.LocalId][visitor_index] = true;
                        gr = gr_to_test;
                        break;
                    }
                }
            }
            if (gr == null) return (result, false);

            Point current_pos = new Point(_starting_position.X + this.offset_from_parent.X, _starting_position.Y + this.offset_from_parent.Y);
            // ............................................. tracking ............................................... //  
            // ............................................. tracking ............................................... //

            // get property values (size 1x1 only)
            List<object> property_values = new List<object>();
            if (this.subject == MappingSubject.Geometry)
                property_values = ExcelMappingNode.GetPropertyValues<SimComponentInstance>(gr, this.properties);
            else
                property_values = GetGeometryAttribute(gr, this.subject);

            // map them to the correct range
            foreach (object o in property_values)
            {
                ExcelMappedData m = null;
                if (o is double)
                    m = ExcelMappedData.MapOneDoubleTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, 1, 1), (double)o);
                else
                    m = ExcelMappedData.MapOneStringTo(this.sheet_name, new Point4D(current_pos.X, current_pos.Y, 1, 1), o.ToString());

                if (m != null)
                {
                    result.Add(m);
                    // advance current position
                    if (this.order_horizontally)
                        current_pos = new Point(current_pos.X + 1, current_pos.Y);
                    else
                        current_pos = new Point(current_pos.X, current_pos.Y + 1);
                }
            }

            position_after = current_pos;
            // ............................................. tracking ............................................... //
            _tracker.AddMappingRecord(this, this.nr_applications, true, result);
            // ............................................. tracking ............................................... //
            return (result, true);
        }

        private static int ToIndex(MappingSubject _subject)
        {
            switch (_subject)
            {
                case MappingSubject.Geometry:
                    return 0;
                case MappingSubject.GeometryArea:
                    return 1;
                case MappingSubject.GeometricIncline:
                    return 2;
                case MappingSubject.GeometricOrientation:
                    return 3;
                case MappingSubject.GeometryPoint:
                    return 4;
                case MappingSubject.Instance:
                    return 5;
                default:
                    return -1;
            }
        }

        #endregion

        #region Apply Rule: Generic Property Extraction, Filter

        internal static List<object> GetPropertyValues<T>(T _instance, IDictionary<string, Type> _props)
        {
            List<object> property_values = new List<object>();

            if (_instance == null) return property_values;
            if (_props == null) return property_values;
            if (_props.Count == 0) return property_values;

            foreach (var entry in _props)
            {
                string p_name = entry.Key;
                Type p_type = entry.Value;
                PropertyInfo p_info = typeof(T).GetProperty(p_name);

                if (p_info != null)
                {
                    if (p_info.Name == nameof(SimComponent.CurrentSlot) && _instance is SimComponent comp2)
                    {
                        if (comp2.ParentContainer == null)
                            property_values.Add(comp2.CurrentSlot.Base);
                        else
                            property_values.Add(comp2.ParentContainer.Slot.ToSerializerString());
                    }
                    else if (p_info.PropertyType == p_type)
                    {
                        object value = p_info.GetValue(_instance);
                        if (value != null)
                            property_values.Add(value);
                    }
                }
            }
            return property_values;
        }


        internal static bool InstancePassesFilter<T>(T _instance, IEnumerable<(string propertyName, object filter)> _pattern_prop,
            Stack<(SimComponent component, SimSlot slot)> callStack)
        {
            if (_instance == null) return false;
            if (_pattern_prop == null) return true;
            if (!_pattern_prop.Any()) return true;

            bool passes_filter = true;
            foreach (var entry in _pattern_prop)
            {
                object pattern = entry.filter;
                string prop_name = entry.propertyName;
                PropertyInfo p_info = typeof(T).GetProperty(prop_name);
                if (p_info != null)
                {
                    if (p_info.PropertyType == typeof(string))
                    {
                        string prop_value = p_info.GetValue(_instance).ToString();
                        passes_filter &= (prop_value.Contains(pattern.ToString()));
                    }
                    else if (p_info.Name == nameof(SimComponent.InstanceState) && _instance is SimComponent comp &&
                        pattern is InstanceStateFilter fo)
                    {
                        var passes = comp.InstanceState.IsRealized == fo.IsRealized &&
                            comp.InstanceType == fo.Type;
                        passes_filter &= passes;
                    }
                    else if (p_info.Name == nameof(SimComponent.CurrentSlot) && _instance is SimComponent comp2)
                    {
                        bool slotMatches = false;
                        var filterSplit = SimDefaultSlots.SplitExtensionSlot((string)entry.filter);

                        if (comp2.ParentContainer == null)
                            slotMatches |= comp2.CurrentSlot.Base == filterSplit.slot;
                        else
                            slotMatches |= (comp2.ParentContainer.Slot.SlotBase.Base == filterSplit.slot &&
                                (filterSplit.extension == "" || comp2.ParentContainer.Slot.SlotExtension == filterSplit.extension));

                        if (callStack != null && callStack.Count > 0)
                        {
                            var lastStackEntry = callStack.Peek();
                            slotMatches |= lastStackEntry.slot.ToSerializerString() == (string)entry.filter;
                        }

                        passes_filter &= slotMatches;
                    }
                    else if (p_info.PropertyType.GetCustomAttribute<FlagsAttribute>() != null && typeof(Enum).IsAssignableFrom(p_info.PropertyType)
                        && pattern is Enum patternEnum && p_info.PropertyType == pattern.GetType())
                    {
                        var enumProperty = (Enum)p_info.GetValue(_instance);
                        passes_filter &= enumProperty.HasFlag(patternEnum);
                    }
                    else
                    {
                        try
                        {
                            object prop_value = p_info.GetValue(_instance);
                            passes_filter &= (prop_value.Equals(pattern));
                        }
                        catch (Exception)
                        {
                            return false;
                        }
                    }
                }
                else
                    return false;
            }

            return passes_filter;
        }

        #endregion

        #region Apply Rule: Parameter Value Table Extraction

        public static double[,] GetCurrentValueTableOf(SimComponent component, SimParameter parameter)
        {
            if (parameter == null) return new double[,] { };

            if (parameter.MultiValuePointer != null)
            {
                // has a table as value source
                if (parameter.MultiValuePointer.ValueField is SimMultiValueBigTable table)
                {
                    return table.GetData();
                }
            }
            else if (parameter.Propagation == SimInfoFlow.FromReference && component != null)
            {
                // references a parameter with a table as value source
                var directReference = parameter.GetReferencedParameter();
                if (directReference != null)
                {
                    if (directReference.MultiValuePointer != null && directReference.MultiValuePointer.ValueField is SimMultiValueBigTable table)
                    {
                        return table.GetData();
                    }
                    else
                    {
                        return new double[,] { { directReference.ValueCurrent } };
                    }
                }
                else
                {
                    // the reference is not in use -> take the current value instead
                    return new double[,] { { directReference.ValueCurrent } };
                }
            }
            else
            {
                // is a scalar value
                return new double[,] { { parameter.ValueCurrent } };
            }

            return new double[,] { };
        }

        #endregion

        #region METHODS: Value Propagation, Children

        public void RemoveChildNode(ExcelMappingNode _child)
        {
            if (_child == null) return;

            this.children.Remove(_child);
        }

        #endregion

        #region STATIC: Hard-Coded Rules (TO BE MADE DYNAMIC LATER)

        private static readonly string RULE_NO_RULE_NAME = "- - -";

        public static ExcelMappingNode Nothing_Rule()
        {
            ExcelMappingNode root = new ExcelMappingNode(null, RULE_NO_RULE_NAME, new Point(0, 0), RULE_NO_RULE_NAME, MappingSubject.Component, null, ExcelMappingRange.SingleValue, true, null, new Point(0, 0));
            return root;
        }

        public static bool IsNothing(ExcelMappingNode _to_test)
        {
            if (_to_test == null) return false;
            return (_to_test.node_name == RULE_NO_RULE_NAME);
        }

        public static ExcelMappingNode Default_Rule(ExcelMappingNode _parent)
        {
            string sheet_name = (_parent == null) ? "" : _parent.sheet_name;
            ExcelMappingNode default_rule = new ExcelMappingNode(_parent, sheet_name, new Point(0, 0), "",
                MappingSubject.Component, new Dictionary<string, Type> { { "Name", typeof(string) } },
                ExcelMappingRange.SingleValue, true, null, new Point(0, 0));
            return default_rule;
        }

        public static ExcelMappingNode Copy(ExcelMappingNode source, string name, string copyNameFormat = "{0} (Copy)")
        {
            var newRule = new ExcelMappingNode(source, copyNameFormat);
            newRule.NodeName = name;

            newRule.Parent = source.Parent;

            return newRule;
        }

        #endregion

        #region UTILS

        private int TotalNrTraversedLevels()
        {
            #region DEBUG TMP
            if (DEBUG_TMP_ON)
            {
                //if (this.NodeName == "Fenstertyp")
                //{

                //}
            }
            #endregion
            int levels = this.max_hierarchy_levels_to_traverse;
            if (this.Parent != null)
                levels += this.Parent.TotalNrTraversedLevels();
            return levels;
        }

        public void MoveRuleUp(ExcelMappingNode _node)
        {
            int idx = this.children.IndexOf(_node);
            if (idx > 0)
                this.children.Move(idx, idx - 1);
        }

        public void MoveRuleDown(ExcelMappingNode _node)
        {
            int idx = this.children.IndexOf(_node);
            if (idx < this.children.Count - 1)
                this.children.Move(idx, idx + 1);
        }

        private enum Orientation
        {
            XZ, XY, YZ
        }

        private static double CalculatePolygonSignedArea(List<Point3D> _polygon, Orientation _plane)
        {
            if (_polygon == null || _polygon.Count < 3)
                return 0.0;

            int n = _polygon.Count;
            double area = 0;

            for (int i = 0; i < n; i++)
            {
                if (_plane == Orientation.XZ)
                    area += (_polygon[(i + 1) % n].Z + _polygon[i].Z) * (_polygon[(i + 1) % n].X - _polygon[i].X);
                else if (_plane == Orientation.YZ)
                    area += (_polygon[(i + 1) % n].Y + _polygon[i].Y) * (_polygon[(i + 1) % n].Z - _polygon[i].Z);
                else
                    area += (_polygon[(i + 1) % n].Y + _polygon[i].Y) * (_polygon[(i + 1) % n].X - _polygon[i].X);

            }

            return (area * 0.5);
        }

        private static double CalculatePolygonLargestSignedProjectedArea(List<Point3D> _polygon)
        {
            double areaXZ = CalculatePolygonSignedArea(_polygon, Orientation.XZ);
            double areaXY = CalculatePolygonSignedArea(_polygon, Orientation.XY);
            double areaYZ = CalculatePolygonSignedArea(_polygon, Orientation.YZ);

            double areaXZ_m = Math.Abs(areaXZ);
            double areaXY_m = Math.Abs(areaXY);
            double areaYZ_m = Math.Abs(areaYZ);

            if (areaXZ_m > areaXY_m && areaXZ_m > areaYZ_m)
                return areaXZ;
            else if (areaXY_m > areaXZ_m && areaXY_m > areaYZ_m)
                return areaXY;
            else
                return areaYZ;
        }

        public static Vector3D GetPolygonNormalNewell(List<Point3D> _polygon)
        {
            if (_polygon == null)
                return new Vector3D(0, 0, 0);

            int n = _polygon.Count;
            if (n < 3)
                return new Vector3D(0, 0, 0);

            Vector3D normal = new Vector3D(0, 0, 0);
            for (int i = 0; i < n; i++)
            {
                normal.X -= (float)((_polygon[i].Z - _polygon[(i + 1) % n].Z) *
                                    (_polygon[i].Y + _polygon[(i + 1) % n].Y));
                normal.Y -= (float)((_polygon[i].X - _polygon[(i + 1) % n].X) *
                                    (_polygon[i].Z + _polygon[(i + 1) % n].Z));
                normal.Z -= (float)((_polygon[i].Y - _polygon[(i + 1) % n].Y) *
                                    (_polygon[i].X + _polygon[(i + 1) % n].X));
            }

            normal.Normalize();
            return normal;
        }

        /// <summary>
        /// Examines the reference graph backwards from caller and returns the maximal number of components referencing a component node over the given nr of levels.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_levels_to_examine">the number of reference levels to be taken into account</param>
        /// <returns>maximal number of references to a component node</returns>
        private static int GetMaxNrOfReferencesToReferenceChain(SimComponent _comp, int _levels_to_examine)
        {
            var excluded = new List<SimId>();
            var max_nr_refs = GetMaxNrOfReferencesToReferenceChainRecursion(_comp, _levels_to_examine, ref excluded);
            return max_nr_refs;
        }

        private static int GetMaxNrOfReferencesToReferenceChainRecursion(SimComponent _comp, int _levels_to_examine, ref List<SimId> _excluded)
        {
            _excluded.Add(_comp.Id);
            int max_nr_refs = _comp.ReferencedBy.Count;
            if (_levels_to_examine > 0)
            {
                foreach (var c in _comp.ReferencedBy)
                {
                    if (!_excluded.Contains(c.TargetId))
                        max_nr_refs = Math.Max(max_nr_refs, GetMaxNrOfReferencesToReferenceChainRecursion(c.Target, _levels_to_examine - 1, ref _excluded));
                }
            }
            return max_nr_refs;
        }

        /// <summary>
        /// Prepares a traversal hierarchy for sub- or referenced components, parameters, and geometric relationships (semantic instances)
        /// of the calling component. It is used for traversal via hierarchical excel mapping rules.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="comps">the traversable sub- or referenced components</param>
        /// <param name="parameters">the traversable parameters</param>
        /// <param name="geom_relations">the traversable geometric relationships (or semantic instances) of all involved components</param>
        private static void GetAllVisitableElements(SimComponent _comp, ref List<SimId> comps, ref List<long> parameters, ref List<long> geom_relations)
        {
            // initialization
            if (comps == null)
                comps = new List<SimId>();
            if (parameters == null)
                parameters = new List<long>();
            if (geom_relations == null)
                geom_relations = new List<long>();

            // termination (avoid loops due to circular referencing)
            if (comps.Contains(_comp.Id))
                return;

            // traversal
            comps.Add(_comp.Id);
            parameters.AddRange(_comp.Parameters.Select(x => x.Id.LocalId));
            geom_relations.AddRange(_comp.Instances.Select(x => x.Id.LocalId));

            // recursion
            foreach (var entry in _comp.Components)
            {
                SimComponent sC = entry.Component;
                if (sC == null) continue;

                GetAllVisitableElements(sC, ref comps, ref parameters, ref geom_relations);
            }
            foreach (var entry in _comp.ReferencedComponents)
            {
                SimComponent rC = entry.Target;
                if (rC == null) continue;

                GetAllVisitableElements(rC, ref comps, ref parameters, ref geom_relations);
            }
        }

        #endregion

        #region METHODS: Attribute Extraction From Path -> Area, Incline, Orientation, Single Point

        private static List<object> GetGeometryAttribute(SimComponentInstance instance, MappingSubject _subject, int _index = 1)
        {
            List<object> attribs = new List<object>();
            if (instance.InstancePath.Count < 3) return attribs;

            if (instance.InstanceType != SimInstanceType.GeometricSurface) return attribs;

            double tolerance = 0.01;
            switch (_subject)
            {
                case MappingSubject.GeometryArea:
                    double area = Math.Abs(CalculatePolygonLargestSignedProjectedArea(instance.InstancePath));
                    attribs.Add(area);
                    break;
                case MappingSubject.GeometricIncline:
                    Vector3D normal_1 = GetPolygonNormalNewell(instance.InstancePath);
                    double incline = TranslateToExcelIncline(normal_1);
                    attribs.Add(incline);
                    break;
                case MappingSubject.GeometricOrientation:
                    Vector3D normal_2 = GetPolygonNormalNewell(instance.InstancePath);
                    double incline_ctrl = TranslateToExcelIncline(normal_2);
                    double orientation = TranslateToExcelOrientation(normal_2);
                    if (Math.Abs(Math.Abs(incline_ctrl) - 90) < tolerance)
                        orientation = 0;
                    attribs.Add(orientation);
                    break;
                case MappingSubject.GeometryPoint:
                    // just for completeness: get the first actual point of the path
                    if (_index >= 0 && _index < instance.InstancePath.Count)
                        attribs.Add(instance.InstancePath[_index].ToString());
                    break;
            }

            return attribs;
        }

        public static double TranslateToExcelOrientation(Vector3D _v)
        {
            double tolerance = 0.0001;
            if (Math.Abs(_v.X) < tolerance && Math.Abs(_v.Y) < tolerance && Math.Abs(_v.Z) < tolerance) return 0;

            Vector3D north = new Vector3D(0, 0, 1);
            Vector3D up = new Vector3D(0, 1, 0);

            Vector3D vN = new Vector3D(_v.X, 0, _v.Z);
            vN.Normalize();

            var dp = Math.Atan2(Vector3D.DotProduct(Vector3D.CrossProduct(vN, north), up), Vector3D.DotProduct(vN, north));
            var angle = dp / Math.PI * 180.0;
            if (angle < -tolerance)
                angle += 360.0;

            return angle;
        }

        public static double TranslateToExcelIncline(Vector3D _v)
        {
            if (_v == null) return 0;

            double tolerance = 0.0001;
            if (Math.Abs(_v.X) < tolerance && Math.Abs(_v.Y) < tolerance && Math.Abs(_v.Z) < tolerance) return 0;

            Vector3D down = new Vector3D(0, -1, 0);

            Vector3D vN = new Vector3D(_v.X, _v.Y, _v.Z);
            vN.Normalize();

            double dot_w_down = Vector3D.DotProduct(down, -vN);
            double angle_to_down = Math.Acos(dot_w_down) * 180 / Math.PI;

            return angle_to_down - 90;
        }

        #endregion
    }
}
