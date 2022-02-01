using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SIMULTAN.Excel
{
    public class StructureNode : System.ComponentModel.INotifyPropertyChanged
    {
        #region STATIC: Constants

        public const long CONTENT_SIMPLE = 1;
        public const long CONTENT_COMPLEX = 2;
        public const long CONTENT_TEXT = 3;
        public const long CONTENT_SUB_TEXT = 4;
        public const long CONTENT_IS_ID = 5;

        #endregion

        #region STATIC: Operators

        public static bool operator ==(StructureNode _sn1, StructureNode _sn2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(_sn1, _sn2))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)_sn1 == null) || ((object)_sn2 == null))
            {
                return false;
            }

            // Return true if the fields match:
            return _sn1.ContentMatch(_sn2);
        }

        public static bool operator !=(StructureNode _hc1, StructureNode _hc2)
        {
            return !(_hc1 == _hc2);
        }

        #endregion

        #region STATIC: Instance creation specific

        protected static StructureNode CreateFrom(SimParameter _node_source, StructureNode _sn_parent)
        {
            if (_node_source == null) return null;

            // a parameter node cannot be w/o parent component
            if (_sn_parent == null) return null;
            if (!_sn_parent.ContentType_Used || _sn_parent.ContentType == null) return null;
            if (_sn_parent.ContentType != typeof(SimComponent)) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            node.IDAsLong = _node_source.Id.LocalId;
            node.IDAsLong_Used = true;
            node.IDAsString = _node_source.Name;
            node.IDAsString_Used = true;
            node.ContentType = typeof(SimParameter);
            node.ContentType_Used = true;

            // structure
            node.ParentNode = _sn_parent;

            return node;
        }

        protected static StructureNode CreateFrom(SimComponentInstance _node_source, StructureNode _sn_parent)
        {
            if (_node_source == null) return null;

            List<HierarchicalContainer> g_content = new List<HierarchicalContainer>(); ;
            for (int i = 0; i < _node_source.InstancePath.Count; i++)
            {
                g_content.Add(new Point3DContainer(_node_source.Id.LocalId, i, _node_source.InstancePath[i]));
            }

            if (g_content.Count == 0) return null;

            // a geometric relationship node cannot exist w/o parent component
            if (_sn_parent == null) return null;
            if (!_sn_parent.ContentType_Used || _sn_parent.ContentType == null) return null;
            if (_sn_parent.ContentType != typeof(SimComponent)) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            node.IDAsLong = _node_source.Id.LocalId;
            node.IDAsLong_Used = true;
            node.ContentType = typeof(SimComponentInstance);
            node.ContentType_Used = true;

            // structure
            node.ParentNode = _sn_parent;

            foreach (Point3DContainer p3dc in g_content)
            {
                StructureNode p3dc_sn = StructureNode.CreateFrom(p3dc, node);
                if (p3dc_sn != null)
                    node.children_nodes.Add(p3dc_sn);
            }

            return node;
        }

        protected static StructureNode CreateFrom(Point3DContainer _node_source, StructureNode _sn_parent)
        {
            if (_node_source == null) return null;

            // a point 3d container cannot exist w/o a parent geometric relationship
            if (_sn_parent == null) return null;
            if (!_sn_parent.ContentType_Used || _sn_parent.ContentType == null) return null;
            if (_sn_parent.ContentType != typeof(SimComponentInstance)) return null;
            if (_sn_parent.IDAsLong != _node_source.ID_primary) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            node.IDAsLong = _node_source.ID_primary;
            node.IDAsLong_Used = true;
            node.IDAsInt_1 = _node_source.ID_secondary;
            node.IDAsInt_1_Used = true;
            node.ContentType = typeof(Point3DContainer);
            node.ContentType_Used = true;

            // structure
            node.ParentNode = _sn_parent;

            return node;
        }

        #endregion

        #region STATIC: Instance creation specific: Components including references

        public static StructureNode CreateFromFlattened(SimComponent _node_source, int _level, int _max_depth, StructureNode _sn_parent, string _content_structure_filter, bool _named = false)
        {
            if (_node_source == null) return null;

            List<KeyValuePair<int, SimComponent>> levels = new List<KeyValuePair<int, SimComponent>>();
            List<KeyValuePair<int, SimId>> levels_control = new List<KeyValuePair<int, SimId>>();
            List<KeyValuePair<int, bool>> levels_is_subcomponent = new List<KeyValuePair<int, bool>>();
            _node_source.GetFlatSubAndRefCompListWLevels(ref levels, ref levels_control, ref levels_is_subcomponent, true, _level, _level, _max_depth, _max_depth);

            List<int> unique_level_keys = levels.GroupBy(x => x.Key).Select(gr => gr.First().Key).ToList();
            Dictionary<int, StructureNode> last_node_on_level = unique_level_keys.Select(x => new KeyValuePair<int, StructureNode>(x, null)).ToDictionary(x => x.Key, x => x.Value);

            foreach (var entry in levels)
            {
                int level = entry.Key;
                StructureNode n_parent = null;
                if (level > 0)
                    n_parent = last_node_on_level[level - 1];

                StructureNode n = StructureNode.CreateSingleFrom(entry.Value, level, n_parent, _content_structure_filter, _named);
                if (n != null && n_parent != null)
                    n_parent.children_nodes.Add(n);

                last_node_on_level[level] = n;
            }

            return last_node_on_level[0];
        }

        private static StructureNode CreateSingleFrom(SimComponent _node_source, int _level, StructureNode _sn_parent, string _content_structure_filter, bool _named = false)
        {
            if (_node_source == null) return null;

            // create the node
            StructureNode node = new StructureNode();

            // content
            node.IDAsLong = _node_source.Id.LocalId;
            node.IDAsLong_Used = true;
            node.ContentType = typeof(SimComponent);
            node.ContentType_Used = true;
            if (_named)
            {
                node.IDAsString = _node_source.ToInfoString();
                node.IDAsString_Used = true;
            }

            // structure
            if (_sn_parent != null)
            {
                // the parent has to be a component
                if (!_sn_parent.ContentType_Used || _sn_parent.ContentType == null) return null;
                if (_sn_parent.ContentType != typeof(SimComponent)) return null;
                // no self-parenting
                if (_sn_parent.IDAsLong == _node_source.Id.LocalId) return null;

                node.ParentNode = _sn_parent;
            }

            foreach (var p in _node_source.Parameters)
            {
                StructureNode p_sn = StructureNode.CreateFrom(p, node);
                if (p_sn != null)
                    node.children_nodes.Add(p_sn);
            }

            foreach (SimComponentInstance gr in _node_source.Instances)
            {
                StructureNode gr_sn = StructureNode.CreateFrom(gr, node);
                if (gr_sn != null)
                    node.children_nodes.Add(gr_sn);
            }

            if (!(string.IsNullOrEmpty(_content_structure_filter)) && _node_source.MappingsPerExcelTool != null)
            {
                var all_rules = _node_source.MappingsPerExcelTool.Values.Where(x => x.ToolName == _content_structure_filter);
                if (all_rules.Count() > 0)
                {
                    node.ContentStructure2D = new List<List<object>>();
                    foreach (var rule in all_rules)
                    {
                        node.ContentStructure2D.Add(rule.Path.Cast<object>().ToList());
                    }
                }
            }

            return node;
        }


        #endregion

        #region PROPERTIES: INotifyPropertyChanged

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(_propName));
        }

        #endregion  

        #region PROPERTIES: Structure

        protected List<StructureNode> children_nodes;
        public ReadOnlyCollection<StructureNode> ChildrenNodes { get { return this.children_nodes.AsReadOnly(); } }
        public StructureNode ParentNode { get; protected set; }

        // links
        public StructureNode LinkTargetNode { get; set; }

        // processing
        protected bool marked_upward_propagating;
        public bool Marked
        {
            get { return this.marked_upward_propagating; }
            set
            {
                this.marked_upward_propagating = value;
                if (this.ParentNode != null)
                    this.PropagateMarkUpwards();
                this.RegisterPropertyChanged("Marked");
            }
        }

        protected void PropagateMarkUpwards()
        {
            if (this.ParentNode != null)
            {
                bool parent_marked = false;
                foreach (StructureNode child in this.ParentNode.ChildrenNodes)
                {
                    parent_marked |= child.Marked;
                }
                this.ParentNode.Marked = parent_marked;
            }
        }

        // display
        protected bool isExpanded;
        public bool IsExpanded
        {
            get { return this.isExpanded; }
            set
            {
                this.isExpanded = value;
                if (this.ParentNode != null)
                    this.PropagateExpandUpwards();
                this.RegisterPropertyChanged("IsExpanded");
            }
        }

        protected bool isSelected;
        public virtual bool IsSelected
        {
            get { return this.isSelected; }
            set
            {
                this.isSelected = value;
                if (this.ParentNode != null)
                    this.PropagateExpandUpwards();
                this.RegisterPropertyChanged("IsSelected");
            }
        }
        protected void PropagateExpandUpwards()
        {
            if (this.ParentNode != null)
            {
                bool parent_expanded = false;
                foreach (StructureNode child in this.ParentNode.ChildrenNodes)
                {
                    parent_expanded |= child.IsExpanded || child.IsSelected;
                }
                this.ParentNode.IsExpanded = parent_expanded;
            }
        }

        #endregion

        #region PROPERTIES: Content Id

        public long IDAsLong { get; protected set; }
        public bool IDAsLong_Used { get; protected set; }

        public int IDAsInt_1 { get; protected set; }
        public bool IDAsInt_1_Used { get; protected set; }

        public int IDAsInt_2 { get; protected set; }
        public bool IDAsInt_2_Used { get; protected set; }

        public string IDAsString { get; protected set; }
        public bool IDAsString_Used { get; protected set; }

        public Type ContentType { get; protected set; }
        public bool ContentType_Used { get; protected set; }

        #endregion

        #region PROPERTIES: Content structure info

        public List<List<object>> ContentStructure2D { get; protected set; }

        #endregion

        #region .CTOR
        protected StructureNode()
        {
            // structure
            this.children_nodes = new List<StructureNode>();
            this.ParentNode = null;
            this.LinkTargetNode = null;
            this.marked_upward_propagating = false;

            // content
            this.IDAsLong = -1L;
            this.IDAsLong_Used = false;

            this.IDAsInt_1 = -1;
            this.IDAsInt_1_Used = false;

            this.IDAsInt_2 = -1;
            this.IDAsInt_2_Used = false;

            this.IDAsString = string.Empty;
            this.IDAsString_Used = false;

            this.ContentType = null;
            this.ContentType_Used = false;
        }
        #endregion

        #region METHODS: Structural Match

        public static List<StructureNode> FindStructureMatchFor(StructureNode _template, List<StructureNode> _search_space, bool _match_parameter_names, bool _match_geometry_content, bool _match_additional_strucure, bool _exclude_self)
        {
            List<StructureNode> matches = new List<StructureNode>();
            if (_template == null || _search_space == null) return matches;
            if (_search_space.Count == 0) return matches;

            foreach (StructureNode n in _search_space)
            {
                StructureNode.FindStructureMatchFor(_template, n, _match_parameter_names, _match_geometry_content, _match_additional_strucure, _exclude_self, ref matches);
            }

            return matches;
        }

        private static void FindStructureMatchFor(StructureNode _template, StructureNode _candidate, bool _match_parameter_names, bool _match_geometry_content, bool _match_additional_strucure, bool _exclude_self, ref List<StructureNode> matches)
        {
            if (matches == null)
                matches = new List<StructureNode>();

            // perform matching
            bool is_match = StructureNode.IsDirectStructureMatchFor(_template, _candidate, _match_parameter_names, _match_geometry_content, _match_additional_strucure, _exclude_self);
            if (is_match)
                matches.Add(_candidate);

            if (_candidate.ChildrenNodes.Count > 0)
            {
                foreach (var sN in _candidate.ChildrenNodes)
                {
                    StructureNode.FindStructureMatchFor(_template, sN, _match_parameter_names, _match_geometry_content, _match_additional_strucure, _exclude_self, ref matches);
                }
            }
        }


        private static bool IsDirectStructureMatchFor(StructureNode _template, StructureNode _candidate, bool _match_parameter_names, bool _match_geometry_content, bool _match_additional_strucure, bool _exclude_self)
        {
            // debug
            // System.Diagnostics.Debug.WriteLine( string.IsNullOrEmpty(_candidate.IDAsString) ? "---" : _candidate.IDAsString);

            // debug
            if (_template.IDAsString.Contains("Geometrische_Fläche") && _candidate.IDAsString.Contains("Geometrische_Fläche"))
            {

            }

            bool is_match = true;
            is_match &= _template.ContentType == _candidate.ContentType;

            if (is_match && _exclude_self)
            {
                if (_template.IDAsLong == _candidate.IDAsLong)
                    is_match = false;
            }

            if (is_match)
            {
                is_match &= _template.ContentType_Used == _candidate.ContentType_Used;
                is_match &= _template.IDAsLong_Used == _candidate.IDAsLong_Used;
                is_match &= _template.IDAsString_Used == _candidate.IDAsString_Used;
                is_match &= _template.IDAsInt_1_Used == _candidate.IDAsInt_1_Used;
                is_match &= _template.IDAsInt_2_Used == _candidate.IDAsInt_2_Used;

                is_match &= _template.ChildrenNodes.Count <= _candidate.ChildrenNodes.Count;
                is_match &= _template.ChildrenNodes.Where(x => x.ContentType == typeof(SimComponent)).Count() ==
                            _candidate.ChildrenNodes.Where(x => x.ContentType == typeof(SimComponent)).Count(); // !!!! only this needs to be an exact match!!!
                is_match &= _template.ChildrenNodes.Where(x => x.ContentType == typeof(SimParameter)).Count() <=
                            _candidate.ChildrenNodes.Where(x => x.ContentType == typeof(SimParameter)).Count();
                is_match &= _template.ChildrenNodes.Where(x => x.ContentType == typeof(SimComponentInstance)).Count() <=
                            _candidate.ChildrenNodes.Where(x => x.ContentType == typeof(SimComponentInstance)).Count();
                if (_match_geometry_content)
                    is_match &= _template.ChildrenNodes.Where(x => x.ContentType == typeof(Point3DContainer)).Count() <=
                                _candidate.ChildrenNodes.Where(x => x.ContentType == typeof(Point3DContainer)).Count();

                // Parameter-specific
                if (_match_parameter_names && _template.ContentType == typeof(SimParameter))
                    is_match &= _template.IDAsString == _candidate.IDAsString;

                if (is_match)
                {
                    if (_template.ChildrenNodes.Count > 0)
                    {
                        if (_template.ContentType != typeof(SimComponentInstance) || _match_geometry_content)
                        {
                            foreach (StructureNode tsN in _template.ChildrenNodes)
                            {
                                bool found_match = false;
                                foreach (StructureNode csN in _candidate.ChildrenNodes)
                                {
                                    found_match = StructureNode.IsDirectStructureMatchFor(tsN, csN, _match_parameter_names, _match_geometry_content, _match_additional_strucure, _exclude_self);
                                    if (found_match)
                                        break;
                                }
                                is_match &= found_match;
                                if (!is_match)
                                    break;
                            }
                        }
                    }
                }

                // finally, compare structural 2d info
                if (is_match && _match_additional_strucure)
                {
                    if (_template.ContentStructure2D == null && _candidate.ContentStructure2D == null)
                        is_match &= true;
                    else if (_template.ContentStructure2D != null && _candidate.ContentStructure2D != null)
                    {
                        is_match &= _template.ContentStructure2D.Count == _candidate.ContentStructure2D.Count;
                        if (is_match && _template.ContentStructure2D.Count > 0)
                        {
                            is_match &= _template.ContentStructure2D.Zip(_candidate.ContentStructure2D, (x, y) => x.Count == y.Count).All(x => x == true);
                        }
                    }
                    else
                        is_match &= false;
                }

            }

            return is_match;
        }


        #endregion

        #region UTILS

        protected int GetNrAncestors()
        {
            if (this.ParentNode == null) return 0;

            return this.ParentNode.GetNrAncestors() + 1;
        }

        protected string GetIndent()
        {
            int nr_ancestors = this.GetNrAncestors();
            if (nr_ancestors == 0) return string.Empty;

            string indent = string.Empty;
            for (int i = 0; i < nr_ancestors; i++)
            {
                indent += "\t";
            }

            return indent;
        }

        #endregion

        #region OVERRIDES: Equality

        public override bool Equals(object obj)
        {
            StructureNode sn = obj as StructureNode;
            if (sn == null)
                return false;
            else
                return this.ContentMatch(sn);
        }

        public override int GetHashCode()
        {
            int hash_code = this.IDAsLong.GetHashCode() ^ this.IDAsInt_1.GetHashCode() ^ this.IDAsInt_2.GetHashCode();

            if (this.IDAsString_Used)
                hash_code ^= this.IDAsString.GetHashCode();

            if (this.ContentType_Used && this.ContentType != null)
                hash_code ^= this.ContentType.GetHashCode();

            return hash_code;
        }

        protected bool ContentMatch(StructureNode _sn)
        {
            if (this.IDAsString_Used != _sn.IDAsString_Used) return false;
            if (this.IDAsInt_1_Used != _sn.IDAsInt_1_Used) return false;
            if (this.IDAsInt_2_Used != _sn.IDAsInt_2_Used) return false;
            if (this.IDAsString_Used != _sn.IDAsString_Used) return false;
            if (this.ContentType_Used != _sn.ContentType_Used) return false;

            bool equal = true;

            equal &= ((this.IDAsLong_Used) ? (this.IDAsLong == _sn.IDAsLong) : true);
            equal &= ((this.IDAsInt_1_Used) ? (this.IDAsInt_1 == _sn.IDAsInt_1) : true);
            equal &= ((this.IDAsInt_2_Used) ? (this.IDAsInt_2 == _sn.IDAsInt_2) : true);
            equal &= ((this.IDAsString_Used) ? (this.IDAsString == _sn.IDAsString) : true);
            equal &= ((this.ContentType_Used) ? (this.ContentType == _sn.ContentType) : true);

            return equal;
        }

        #endregion

        #region OVERRIDES: ToString

        public override string ToString()
        {
            string representation = this.GetIndent();
            representation += (this.Marked) ? "oSN[" : "SN[";

            representation += (this.IDAsLong_Used) ? " IDL:" + this.IDAsLong.ToString() : " IDL:-";
            representation += (this.IDAsInt_1_Used) ? " IDI1:" + this.IDAsInt_1.ToString() : " IDI1:-";
            representation += (this.IDAsInt_2_Used) ? " IDI2:" + this.IDAsInt_2.ToString() : " IDI2:-";
            representation += (this.IDAsString_Used && this.IDAsString != null) ? " IDS:" + this.IDAsString : " IDS:-";
            representation += (this.ContentType_Used && this.ContentType != null) ? " CT:" + this.ContentType.ToString() : " CT:-";
            representation += " ]";

            representation += "(" + this.children_nodes.Count + ")";

            if (this.LinkTargetNode != null)
                representation += "->" + this.LinkTargetNode.ToString();

            if (this.children_nodes.Count > 0)
            {
                representation += "\n";
                foreach (StructureNode child in this.children_nodes)
                {
                    representation += child.ToString() + "\n";
                }
            }

            return representation;
        }

        public string ToSimpleString()
        {
            string representation = "SN[";

            representation += (this.IDAsLong_Used) ? " IDL:" + this.IDAsLong.ToString() : "";
            representation += (this.IDAsInt_1_Used) ? " IDI1:" + this.IDAsInt_1.ToString() : "";
            representation += (this.IDAsInt_2_Used) ? " IDI2:" + this.IDAsInt_2.ToString() : "";
            representation += (this.IDAsString_Used && this.IDAsString != null) ? " IDS:" + this.IDAsString : "";

            string cont_type_string = (this.ContentType_Used && this.ContentType != null) ? this.ContentType.ToString() : "";
            if (!string.IsNullOrEmpty(cont_type_string))
            {
                string[] type_comps = cont_type_string.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (type_comps != null && type_comps.Length > 0)
                {
                    representation += "\nCT:" + type_comps[type_comps.Length - 1];
                }
            }

            representation += "]";

            return representation;
        }

        public string ToInfoString()
        {
            string representation = "";

            representation += (this.IDAsLong_Used) ? " IDL:" + this.IDAsLong.ToString() : "";
            representation += (this.IDAsInt_1_Used) ? " IDI1:" + this.IDAsInt_1.ToString() : "";
            representation += (this.IDAsInt_2_Used) ? " IDI2:" + this.IDAsInt_2.ToString() : "";
            representation += (this.IDAsString_Used && this.IDAsString != null) ? " IDS:" + this.IDAsString : "";

            string cont_type_string = (this.ContentType_Used && this.ContentType != null) ? this.ContentType.ToString() : "";
            if (!string.IsNullOrEmpty(cont_type_string))
            {
                string[] type_comps = cont_type_string.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                if (type_comps != null && type_comps.Length > 0)
                {
                    if (type_comps.Length > 1)
                        representation += " CT:" + type_comps[type_comps.Length - 2] + "." + type_comps[type_comps.Length - 1];
                    else
                        representation += " CT:" + type_comps[type_comps.Length - 1];
                }
            }

            return representation;
        }

        #endregion
    }
}
