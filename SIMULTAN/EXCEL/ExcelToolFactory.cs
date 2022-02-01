using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils.Collections;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Excel
{
    #region ENUMS
    public enum MappingSubject
    {
        COMPONENT,
        PARAMETER,
        GEOMETRY,
        GEOMETRY_POINT,
        GEOMETRY_AREA,
        GEOMETRY_ORIENTATION,
        GEOMETRY_INCLINE,
        INSTANCE
    }
    #endregion

    public class ExcelToolFactory : INotifyPropertyChanged
    {
        #region PROPERTIES, FIELDS

        public ProjectData ProjectData { get; }

        public ObservableCollection<ExcelTool> RegisteredTools { get; private set; }

        private void RegisteredTools_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (var oldItem in e.OldItems.OfType<ExcelTool>())
                    oldItem.Factory = null;
            if (e.NewItems != null)
                foreach (var newItem in e.NewItems.OfType<ExcelTool>())
                    newItem.Factory = this;
        }

        #endregion

        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion


        public ExcelToolFactory(ProjectData projectData)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));

            this.ProjectData = projectData;
            this.RegisteredTools = new ObservableCollection<ExcelTool>();
            this.RegisteredTools.CollectionChanged += RegisteredTools_CollectionChanged;
        }

        public void ClearRecord()
        {
            //Required because clear does not have OldItems set
            this.RegisteredTools.ForEach(x => x.Factory = null);
            this.RegisteredTools.Clear();
        }




        #region TOOL MANAGEMENT
        public ExcelTool CreateEmpty()
        {
            ExcelTool created = new ExcelTool();
            this.RegisteredTools.Add(created);
            return created;
        }

        public ExcelTool CreateCopyOf(ExcelTool _tool, string nameCopyFormat)
        {
            ExcelTool copy = new ExcelTool(_tool, nameCopyFormat);
            this.RegisteredTools.Add(copy);
            return copy;
        }

        public void RemoveExcelTool(ExcelTool _tool)
        {
            if (_tool == null) return;
            this.RegisteredTools.Remove(_tool);
        }

        #endregion

        #region PARSING
        /// <summary>
        /// Method used for parsing.
        /// </summary>
        /// <returns></returns>
        public ExcelTool CreateExcelTool(string _name, List<ExcelMappingNode> _rules, List<KeyValuePair<ExcelMappedData, Type>> _results, List<ExcelUnmappingRule> _result_unmappings,
                                        string _macro_name, string _last_path_to_file = null)
        {
            ExcelTool created = new ExcelTool(_name, _rules, _results, _result_unmappings, _macro_name, _last_path_to_file);
            this.RegisteredTools.Add(created);
            return created;
        }

        /// <summary>
        /// Method used for parsing.
        /// </summary>
        public ExcelMappingNode CreateExcelMappingNode(ExcelMappingNode _parent, string _sheet_name, Point _offset, string _name,
                                                     MappingSubject _subject, Dictionary<string, Type> _properties, ExcelMappingRange _accepted_range, bool _order_hrz,
                                                     bool _prepend, IEnumerable<(string propertyName, object filter)> _patterns_to_match_in_property, Point _offset_btw_applicaions,
                                                     int _max_elements_to_map, int _max_hierarchy_levels_to_traverse, TraversalStrategy _strategy, bool _node_is_active, int _version)
        {
            ExcelMappingNode rule = new ExcelMappingNode(_parent, _sheet_name, _offset, _name,
                                                        _subject, _properties, _accepted_range, _order_hrz,
                                                        _patterns_to_match_in_property, _offset_btw_applicaions,
                                                        _max_elements_to_map, _max_hierarchy_levels_to_traverse, _strategy, _node_is_active, _version);
            rule.prepend_content_to_children = _prepend;
            return rule;
        }

        /// <summary>
        /// Method used only for parsing.
        /// </summary>
        public ExcelUnmappingRule CreateExcelUnmappingNode(string _node_name, Type _data_type, ExcelMappedData _excel_data, bool _unmap_by_filter,
                                                           ObservableCollection<(string propertyName, object filter)> _filter_comp,
                                                           ObservableCollection<(string propertyName, object filter)> _filter_parameter,
                                                           long _target_param_id, Point _pointer)
        {
            ExcelUnmappingRule rule = new ExcelUnmappingRule(_node_name, _data_type, _excel_data, _unmap_by_filter,
                                                                _filter_comp, _filter_parameter, _target_param_id, _pointer);
            return rule;
        }
        #endregion

        #region RULE MANAGEMENT

        public void RestoreDependencies(ProjectData projectData)
        {
            foreach (ExcelTool tool in this.RegisteredTools)
            {
                foreach (ExcelUnmappingRule um in tool.OutputRules)
                {
                    if (!um.UnmapByFilter && um.TargetParameterID != -1)
                    {
                        var lookupId = new SimId(projectData.Owner, um.TargetParameterID);
                        um.TargetParameter = projectData.IdGenerator.GetById<SimParameter>(lookupId);
                        um.TargetParameterID = -1;
                    }
                }
            }
        }

        #endregion

        public StringBuilder Export()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.EXCEL_SECTION);

            foreach (ExcelTool t in this.RegisteredTools)
            {
                t.AddToExport(ref sb);
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // FINALIZE FILE
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.EOF);

            return sb;
        }
    }
}
