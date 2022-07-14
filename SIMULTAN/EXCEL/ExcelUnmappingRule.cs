using SIMULTAN;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
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
    public class ExcelUnmappingRule : INotifyPropertyChanged
    {
        #region PROPERTIES

        public ExcelTool Tool { get; internal set; }

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

                    if (Tool != null && Tool.Factory != null)
                        ExcelTool.RenameRuleInComponents(Tool.Factory.ProjectData.Components, this.Tool.Name, old_value, this.node_name);

                    NotifyPropertyChanged(nameof(NodeName));
                }
            }
        }

        private Type data_type;
        public Type DataType => data_type;

        private ExcelMappedData excel_data;
        public ExcelMappedData ExcelData
        {
            get { return this.excel_data; }
            private set
            {
                this.excel_data = value;
                NotifyPropertyChanged(nameof(ExcelData));
            }
        }
        public bool UnmapByFilter { get; }

        // alternative 1: filter
        public ObservableCollection<(string propertyName, object filter)> PatternsToMatchInPropertyOfComp
        {
            get { return this.patterns_to_match_in_property_of_comp; }
            private set
            {
                this.patterns_to_match_in_property_of_comp = value;
            }
        }
        private ObservableCollection<(string propertyName, object filter)> patterns_to_match_in_property_of_comp;

        public ObservableCollection<(string propertyName, object filter)> PatternsToMatchInPropertyOfParam
        {
            get { return this.patterns_to_match_in_property_of_param; }
            set
            {
                this.patterns_to_match_in_property_of_param = value;
            }
        }
        private ObservableCollection<(string propertyName, object filter)> patterns_to_match_in_property_of_param;

        internal SimParameter target_parameter;
        public SimParameter TargetParameter
        {
            get { return this.target_parameter; }
            set
            {
                if (this.target_parameter != value)
                {
                    if (this.target_parameter != null)
                        this.target_parameter.IsBeingDeleted -= this.Target_parameter_IsBeingDeleted;

                    this.target_parameter = value;

                    if (this.target_parameter != null)
                        this.target_parameter.IsBeingDeleted += this.Target_parameter_IsBeingDeleted;

                    NotifyPropertyChanged(nameof(TargetParameter));
                }
            }
        }

        public Point TargetPointer
        {
            get { return this.target_pointer; }
            set
            {
                if (this.target_pointer != value)
                {
                    this.target_pointer = value;
                    NotifyPropertyChanged(nameof(TargetPointer));
                }
            }
        }
        private Point target_pointer;

        // May only be used during loading
        internal long TargetParameterID { get; set; } = -1L;


        #endregion

        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void NotifyPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion

        #region .CTOR

        public ExcelUnmappingRule(string _name, Type _data_type, ExcelMappedData _excel_data_location,
            IEnumerable<(string propertyName, object filter)> _filter_component,
            IEnumerable<(string propertyName, object filter)> _filter_parameter, Point _pointer)
            : this(_name, _data_type, _excel_data_location, true,
                  new ObservableCollection<(string, object)>(_filter_component),
                  new ObservableCollection<(string, object)>(_filter_parameter), null, _pointer)
        { }

        public ExcelUnmappingRule(string _name, Type _data_type, ExcelMappedData _excel_data_location,
            SimParameter _target_param, Point _pointer)
            : this(_name, _data_type, _excel_data_location, false,
                  new ObservableCollection<(string, object)>(), new ObservableCollection<(string, object)>(),
                  _target_param, _pointer)
        { }

        private ExcelUnmappingRule(string _name, Type _data_type, ExcelMappedData _excel_data_location, bool _unmap_by_filter,
            ObservableCollection<(string propertyName, object filter)> _filter_component, ObservableCollection<(string propertyName, object filter)> _filter_parameter,
            SimParameter _target_param, Point _pointer)
        {
            this.node_name = _name;
            this.data_type = _data_type;
            this.excel_data = _excel_data_location;
            this.target_pointer = _pointer;

            this.UnmapByFilter = _unmap_by_filter;

            this.PatternsToMatchInPropertyOfComp = _filter_component;
            this.PatternsToMatchInPropertyOfParam = _filter_parameter;

            this.TargetParameter = _target_param;
        }

        #endregion

        #region COPY .CTOR

        public ExcelUnmappingRule(ExcelUnmappingRule _original, string copyNameFormat)
        {
            this.node_name = string.Format(copyNameFormat, _original.node_name);
            this.data_type = _original.data_type;
            this.excel_data = new ExcelMappedData(_original.excel_data);

            this.UnmapByFilter = _original.UnmapByFilter;
            this.patterns_to_match_in_property_of_comp = new ObservableCollection<(string propertyName, object filter)>(_original.patterns_to_match_in_property_of_comp);
            this.patterns_to_match_in_property_of_param = new ObservableCollection<(string propertyName, object filter)>(_original.patterns_to_match_in_property_of_param);

            if (!this.UnmapByFilter && _original.TargetParameter != null)
            {
                this.TargetParameter = _original.TargetParameter;
            }

            this.target_pointer = new Point(_original.target_pointer.X, _original.target_pointer.Y);
        }

        #endregion

        #region PARSING .CTOR
        internal ExcelUnmappingRule(string _name, Type _data_type, ExcelMappedData _excel_data_location, bool _unmap_by_filter,
                                    ObservableCollection<(string propertyName, object filter)> _filter_component,
                                    ObservableCollection<(string propertyName, object filter)> _filter_parameter,
                                    long _target_param_id, Point _pointer)
        {
            this.node_name = _name;
            this.data_type = _data_type;
            this.excel_data = _excel_data_location;

            this.UnmapByFilter = _unmap_by_filter;
            this.patterns_to_match_in_property_of_comp = _filter_component;
            if (this.patterns_to_match_in_property_of_comp == null)
                this.patterns_to_match_in_property_of_comp = new ObservableCollection<(string propertyName, object filter)>();

            this.patterns_to_match_in_property_of_param = _filter_parameter;
            if (this.patterns_to_match_in_property_of_param == null)
                this.patterns_to_match_in_property_of_param = new ObservableCollection<(string propertyName, object filter)>();

            this.TargetParameterID = _target_param_id;
            this.target_pointer = _pointer;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            string content = this.node_name + "[" + this.excel_data.SheetName + " " + this.excel_data.ToString() + "] ";
            content += (this.UnmapByFilter) ? "FILTER " : "TARGET ";
            return content;
        }

        #endregion

        #region Rule Application

        public void ApplyUnmapping(SimMultiValueBigTable _table, SimComponentCollection _comp_factory)
        {
            if (this.UnmapByFilter)
                this.ApplyFilterUnmapping(_table, _comp_factory);
            else
                ApplyTargetedUnmapping(_table);
        }

        private void ApplyTargetedUnmapping(SimMultiValueBigTable _table)
        {
            if (this.TargetParameter != null)
                this.ApplySingleTargetedUnmapping(this.TargetParameter, _table);
        }

        private void ApplySingleTargetedUnmapping(SimParameter _target_param, SimMultiValueBigTable _table)
        {
            if (_target_param == null || _table == null) return;

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(_table, (int)this.target_pointer.Y - 1, (int)this.target_pointer.X - 1);
            _target_param.MultiValuePointer = ptr;
        }

        private void ApplyFilterUnmapping(SimMultiValueBigTable _table, SimComponentCollection _factory)
        {
            List<SimParameter> filtered_targets = this.UnmappingTargets(_factory);
            filtered_targets.ForEach(p => this.ApplySingleTargetedUnmapping(p, _table));
        }

        public List<SimParameter> UnmappingTargets(SimComponentCollection _factory)
        {
            if (_factory == null)
                throw new ArgumentNullException("{0} may not be null", nameof(_factory));

            if (this.UnmapByFilter)
            {
                // filter components
                var filtered_params = FindTargetParameters(_factory, this.patterns_to_match_in_property_of_comp, this.patterns_to_match_in_property_of_param);
                return filtered_params;
            }
            else
            {
                return new List<SimParameter> { TargetParameter };
            }
        }

        private static List<SimParameter> FindTargetParameters(SimComponentCollection _factory, IEnumerable<(string propertyName, object filter)> componentFilter,
            IEnumerable<(string propertyName, object filter)> parameterFilter)
        {
            List<SimParameter> filtered = new List<SimParameter>();
            foreach (var c in _factory)
            {
                FindComponent(c, componentFilter, parameterFilter, filtered);
            }
            return filtered;
        }
        private static void FindComponent(SimComponent component, IEnumerable<(string propertyName, object filter)> componentFilter,
            IEnumerable<(string propertyName, object filter)> parameterFilter,
            List<SimParameter> results)
        {
            if (ExcelMappingNode.InstancePassesFilter(component, componentFilter, null))
            {
                foreach (var entry in component.Parameters)
                {
                    if (ExcelMappingNode.InstancePassesFilter(entry, parameterFilter, null))
                        results.Add(entry);
                }
            }

            foreach (var child in component.Components.Where(x => x.Component != null))
                FindComponent(child.Component, componentFilter, parameterFilter, results);
        }

        #endregion

        #region STATIC: Hard-Coded Rules

        public static ExcelUnmappingRule Default_FilterRule()
        {
            ExcelMappedData excel_data_container = ExcelMappedData.CreateEmpty("Tabelle mit Ergebnissen", new System.Windows.Media.Media3D.Point4D(1, 1, 10, 2));

            var filter_c = new ObservableCollection<(string propertyName, object filter)> { ("CurrentSlot", SimDefaultSlots.Undefined) };
            var filter_p = new ObservableCollection<(string propertyName, object filter)> { ("Name", "pattern in name"), ("Unit", "pattern in unit") };

            ExcelUnmappingRule rule = new ExcelUnmappingRule("New Unmapping", typeof(string), excel_data_container, filter_c, filter_p, new Point(1, 1));
            return rule;
        }

        public static ExcelUnmappingRule Default_TargetRule(SimParameter _p)
        {
            ExcelMappedData excel_data_container = ExcelMappedData.CreateEmpty("Tabelle mit Ergebnissen", new System.Windows.Media.Media3D.Point4D(1, 1, 10, 2));
            ExcelUnmappingRule rule = new ExcelUnmappingRule("New Unmapping", typeof(string), excel_data_container, _p, new Point(1, 1));
            return rule;
        }

        #endregion

        private void Target_parameter_IsBeingDeleted(object sender)
        {
            this.TargetParameter = null;
        }
    }
}
