using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using SIMULTAN.Excel;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFExcelMappingNode : DXFEntityContainer
    {
        public static Dictionary<string, Type> DeserializerTypename { get; } = new Dictionary<string, Type>();

        static DXFExcelMappingNode()
        {
            foreach (var type in typeof(DXFExcelMappingNode).Assembly.GetTypes())
            {
                var serializerNameAttrib = type.GetCustomAttribute<DXFSerializerTypeNameAttribute>();
                if (serializerNameAttrib != null)
                {
                    DeserializerTypename.Add(serializerNameAttrib.Name, type);
                }
            }    
        }

        #region CLASS MEMBERS

        private string dxf_sheet_name;
        private Point dxf_offset_from_parent;
        private string dxf_node_name;
        private MappingSubject dxf_subject;

        private Dictionary<string, Type> dxf_Properties;
        private int dxf_nr_Properties;
        private string dxf_Properties_key;
        private Type dxf_Properties_value;

        private ExcelMappingRange dxf_accepted_range;
        private bool dxf_order_hrz;
        private bool dxf_prepend_content_to_children;

        private List<(string propertyName, object filter)> dxf_filter;
        private int dxf_nr_filter;
        private object dxf_filter_key;
        private string dxf_filter_value;
        private int dxf_filter_key_parse_err;

        private Point dxf_offset_btw_applications;

        private int dxf_MaxElementsToMap;
        private int dxf_MaxHierarchyLevelsToTraverse;
        private TraversalStrategy dxf_Strategy;
        private bool dxf_NodeIsActive;

        private int dxf_Version;

        private List<ExcelMappingNode> tmp_children;
        internal ExcelMappingNode dxf_parsed;

        #endregion

        #region .CTOR
        public DXFExcelMappingNode()
        {
            this.dxf_sheet_name = string.Empty;
            this.dxf_offset_from_parent = new Point(0, 0);
            this.dxf_node_name = string.Empty;
            this.dxf_subject = MappingSubject.COMPONENT;

            this.dxf_Properties = new Dictionary<string, Type>();
            this.dxf_nr_Properties = 0;

            this.dxf_accepted_range = ExcelMappingRange.SINGLE_VALUE;
            this.dxf_order_hrz = true;
            this.dxf_prepend_content_to_children = false;

            this.dxf_filter = new List<(string propertyName, object filter)>();
            this.dxf_nr_filter = 0;
            this.dxf_filter_key_parse_err = 0;

            this.dxf_offset_btw_applications = new Point(0, 0);

            this.dxf_MaxElementsToMap = int.MaxValue;
            this.dxf_MaxHierarchyLevelsToTraverse = int.MaxValue;
            this.dxf_Strategy = TraversalStrategy.SUBTREE_AND_REFERENCES;
            this.dxf_NodeIsActive = true;

            this.dxf_Version = 0;

            this.tmp_children = new List<ExcelMappingNode>();
        }
        #endregion

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ExcelMappingSaveCode.RULE_SHEET_NAME:
                    this.dxf_sheet_name = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.RULE_OFFSET_FROM_PARENT_X:
                    this.dxf_offset_from_parent.X = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.RULE_OFFSET_FROM_PARENT_Y:
                    this.dxf_offset_from_parent.Y = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.RULE_NODE_NAME:
                    this.dxf_node_name = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.RULE_SUBJECT:
                    MappingSubject tmp_subject = MappingSubject.COMPONENT;
                    bool success = Enum.TryParse<MappingSubject>(this.Decoder.FValue, out tmp_subject);
                    if (success)
                        this.dxf_subject = tmp_subject;
                    break;
                case (int)ExcelMappingSaveCode.RULE_PROPERTIES:
                    this.dxf_nr_Properties = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.RULE_ACCEPT_MULTI_VAL_PER_PROPERTY:
                    this.dxf_accepted_range = (ExcelMappingRange)this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.RULE_ORDER_HORIZONTALLY:
                    this.dxf_order_hrz = (this.Decoder.IntValue() == 1);
                    break;
                case (int)ExcelMappingSaveCode.RULE_FILTER:
                    this.dxf_nr_filter = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.RULE_OFFSET_BTW_APPLICATIONS_X:
                    this.dxf_offset_btw_applications.X = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.RULE_OFFSET_BTW_APPLICATIONS_Y:
                    this.dxf_offset_btw_applications.Y = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.TRAVERSE_MAX_ELEM:
                    this.dxf_MaxElementsToMap = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.TRAVERSE_MAX_LEVELS:
                    this.dxf_MaxHierarchyLevelsToTraverse = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.TRAVERSE_STRATEGY:
                    bool success1 = Enum.TryParse(this.Decoder.FValue, out TraversalStrategy ts);
                    if (success1)
                        this.dxf_Strategy = ts;
                    break;
                case (int)ExcelMappingSaveCode.TRAVERSAL_ACTIVATED:
                    this.dxf_NodeIsActive = this.Decoder.FValue == "1";
                    break;
                case (int)ExcelMappingSaveCode.VERSION:
                    this.dxf_Version = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.RULE_PREPEND_CONTENT_TO_CHILDREN:
                    this.dxf_prepend_content_to_children = (this.Decoder.IntValue() == 1);
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_Properties > this.dxf_Properties.Count)
                    {
                        this.dxf_Properties_key = this.Decoder.FValue;
                        if (this.dxf_subject == MappingSubject.COMPONENT && this.dxf_Properties_key == "ID" &&
                            this.Decoder.CurrentFileVersion < DXFDecoder.CurrentFileFormatVersion)
                        {
                            this.dxf_Properties_key = nameof(SimComponent.LocalID);
                        }
                    }
                    else if (this.dxf_nr_filter > this.dxf_filter.Count)
                    {
                        this.dxf_filter_value = this.Decoder.FValue;
                        this.dxf_filter.Add((this.dxf_filter_value, this.dxf_filter_key));

                        this.dxf_filter_key = null;
                        this.dxf_filter_value = null;
                    }
                    break;
                case (int)ParamStructCommonSaveCode.V10_VALUE:
                    if (this.dxf_nr_Properties > this.dxf_Properties.Count)
                    {
                        string type_name = this.Decoder.FValue;

                        if (!DeserializerTypename.TryGetValue(type_name, out this.dxf_Properties_value))
                        {
                            this.dxf_Properties_value = Type.GetType(type_name, false); // search mscorelib
                            if (this.dxf_Properties_value == null)
                                this.dxf_Properties_value = Type.GetType(type_name + ", " + System.Reflection.Assembly.GetExecutingAssembly().FullName, false);
                        }

                        if (this.dxf_Properties_value == null)
                            throw new Exception(string.Format("Failed to deserialize typename \"{0}\"", type_name));

                        this.dxf_Properties.Add(this.dxf_Properties_key, this.dxf_Properties_value);

                        this.dxf_Properties_key = null;
                        this.dxf_Properties_value = null;
                    }
                    else if (this.dxf_nr_filter > this.dxf_filter.Count)
                    {
                        string object_str = this.Decoder.FValue;
                        this.dxf_filter_key = ExcelMappingNode.DeserializeFilterObject(object_str);
                        if (this.dxf_filter_key == null)
                        {
                            this.dxf_filter_key_parse_err++;
                            this.dxf_filter_key = "ERROR " + this.dxf_filter_key_parse_err.ToString() + "!";
                        }
                    }
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }
        #endregion

        #region OVERRIDES: Adding Entities

        internal override bool AddEntity(DXFEntity _e)
        {
            // handle depending on type
            if (_e == null) return false;
            bool add_successful = false;

            DXFComponentSubContainer container = _e as DXFComponentSubContainer;
            if (container != null)
            {
                add_successful = true;
                foreach (DXFEntity sE in container.EC_Entities)
                {
                    DXFExcelMappingNode sNode = sE as DXFExcelMappingNode;
                    if (sNode != null)
                    {
                        // take the parsed excel node
                        this.tmp_children.Add(sNode.dxf_parsed);
                        // sNode.dxf_parsed.Parent = this.dxf_parsed;
                        add_successful &= true;
                    }
                }
            }
            return add_successful;
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            if (Decoder.CurrentFileVersion < 7)
            {
                for (int i = 0; i < this.dxf_filter.Count; ++i)
                {
                    if (dxf_filter[i].propertyName == "IsBoundInNW" && dxf_subject == MappingSubject.COMPONENT)
                        dxf_filter[i] = ("IsBoundInNetwork", dxf_filter[i].filter);
                    else if (dxf_filter[i].propertyName == "LastChangeToSave" && dxf_subject == MappingSubject.COMPONENT)
                    {
                        dxf_filter.RemoveAt(i);
                        i--;
                    }
                    else if (dxf_filter[i].propertyName == "GeometryRelationState" && dxf_subject == MappingSubject.COMPONENT)
                    {
                        dxf_filter[i] = ("InstanceState", dxf_filter[i].filter);
                    }

                    if (dxf_filter[i].filter is string str && str.StartsWith("ERROR") && str.EndsWith("!"))
                    {
                        dxf_filter.RemoveAt(i);
                        i--;
                    }
                }


                if (dxf_Properties.ContainsKey("IsBoundInNW"))
                {
                    this.dxf_Properties.Remove("IsBoundInNW");
                    this.dxf_Properties.Add("IsBoundInNetwork", typeof(bool));
                }
                if (dxf_Properties.ContainsKey("InstanceParamValues"))
                {
                    dxf_Properties.Remove("InstanceParamValues");
                    dxf_Properties["InstanceParameterValuesTemporary"] = typeof(SimInstanceParameterCollection);
                }
                if (dxf_Properties.ContainsKey("InstanceParameterValuesPersistent"))
                    dxf_Properties["InstanceParameterValuesPersistent"] = typeof(SimInstanceParameterCollection);

                if (dxf_subject == MappingSubject.COMPONENT && dxf_Properties.ContainsKey("LastChangeToSave"))
                {
                    this.dxf_Properties.Remove("LastChangeToSave");
                }
            }

            if (dxf_node_name == "Bauteil")
                Console.WriteLine("ASDF");

            // parent will be set when added to parent in the AddEntity method
            this.dxf_parsed = this.Decoder.ProjectData.ExcelToolMappingManager.CreateExcelMappingNode(null, this.dxf_sheet_name, this.dxf_offset_from_parent, this.dxf_node_name,
                                                                             this.dxf_subject, this.dxf_Properties, this.dxf_accepted_range, this.dxf_order_hrz,
                                                                             this.dxf_prepend_content_to_children, this.dxf_filter, this.dxf_offset_btw_applications,
                                                                             this.dxf_MaxElementsToMap, this.dxf_MaxHierarchyLevelsToTraverse,
                                                                             this.dxf_Strategy, this.dxf_NodeIsActive, this.dxf_Version);
            foreach (ExcelMappingNode sN in this.tmp_children)
            {
                sN.Parent = this.dxf_parsed;
            }
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return this.dxf_sheet_name + ": " + this.dxf_node_name;
        }
        #endregion
    }
}
