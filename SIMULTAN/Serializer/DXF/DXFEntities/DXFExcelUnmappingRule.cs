using SIMULTAN.Data.Components;
using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFExcelUnmappingRule : DXFEntityContainer
    {
        #region CLASS MEMBERS

        private string dxf_NodeName;
        private Type dxf_DataType;
        private ExcelMappedData dxf_ExcelData;
        private bool dxf_UnmapByFilter;

        private ObservableCollection<(string propertyName, object filter)> dxf_PatternsToMatchInPropertyOfComp;
        private int dxf_nr_PatternsToMatchInPropertyOfComp;
        private ObservableCollection<(string propertyName, object filter)> dxf_PatternsToMatchInPropertyOfParam;
        private int dxf_nr_PatternsToMatchInPropertyOfParam;

        private object dxf_filter_key;
        private string dxf_filter_value;
        private int dxf_filter_key_parse_err;

        private long dxf_TargetComponent_ID;
        private Guid dxf_TargetComponent_Location;
        private long dxf_TargetParameterID;
        private Point dxf_TargetPointer;

        internal ExcelUnmappingRule dxf_parsed;

        #endregion

        #region .CTOR

        public DXFExcelUnmappingRule()
        {
            this.dxf_NodeName = string.Empty;
            this.dxf_DataType = typeof(string);
            this.dxf_ExcelData = null;

            this.dxf_UnmapByFilter = true;

            this.dxf_PatternsToMatchInPropertyOfComp = new ObservableCollection<(string propertyName, object filter)>();
            this.dxf_nr_PatternsToMatchInPropertyOfComp = 0;
            this.dxf_PatternsToMatchInPropertyOfParam = new ObservableCollection<(string propertyName, object filter)>();
            this.dxf_nr_PatternsToMatchInPropertyOfParam = 0;

            this.dxf_filter_key = null;
            this.dxf_filter_value = null;
            this.dxf_filter_key_parse_err = 0;

            this.dxf_TargetComponent_ID = -1L;
            this.dxf_TargetComponent_Location = Guid.Empty;
            this.dxf_TargetParameterID = -1L;
            this.dxf_TargetPointer = new Point(0, 0);
        }

        #endregion

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ExcelMappingSaveCode.RULE_NODE_NAME:
                    this.dxf_NodeName = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.UNMAP_FILTER_COMP:
                    this.dxf_nr_PatternsToMatchInPropertyOfComp = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.UNMAP_FILTER_PARAM:
                    this.dxf_nr_PatternsToMatchInPropertyOfParam = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.UNMAP_FILTER_SWITCH:
                    this.dxf_UnmapByFilter = (this.Decoder.IntValue() == 1);
                    break;
                case (int)ExcelMappingSaveCode.UNMAP_TARGET_COMP_ID:
                    this.dxf_TargetComponent_ID = this.Decoder.LongValue();
                    break;
                case (int)ExcelMappingSaveCode.UNMAP_TARGET_COMP_LOCATION:
                    this.dxf_TargetComponent_Location = new Guid(this.Decoder.FValue);
                    break;
                case (int)ExcelMappingSaveCode.UNMAP_TARGET_PARAM:
                    this.dxf_TargetParameterID = this.Decoder.LongValue();
                    break;
                case (int)ExcelMappingSaveCode.UNMAP_PARAM_POINTER_X:
                    this.dxf_TargetPointer = new Point(this.Decoder.IntValue(), this.dxf_TargetPointer.Y);
                    break;
                case (int)ExcelMappingSaveCode.UNMAP_PARAM_POINTER_Y:
                    this.dxf_TargetPointer = new Point(this.dxf_TargetPointer.X, this.Decoder.IntValue());
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_PatternsToMatchInPropertyOfComp > this.dxf_PatternsToMatchInPropertyOfComp.Count)
                    {
                        this.dxf_filter_value = this.Decoder.FValue;
                        this.dxf_PatternsToMatchInPropertyOfComp.Add((this.dxf_filter_value, this.dxf_filter_key));

                        this.dxf_filter_key = null;
                        this.dxf_filter_value = null;
                    }
                    else if (this.dxf_nr_PatternsToMatchInPropertyOfParam > this.dxf_PatternsToMatchInPropertyOfParam.Count)
                    {
                        this.dxf_filter_value = this.Decoder.FValue;
                        this.dxf_PatternsToMatchInPropertyOfParam.Add((this.dxf_filter_value, this.dxf_filter_key));

                        this.dxf_filter_key = null;
                        this.dxf_filter_value = null;
                    }
                    break;
                case (int)ParamStructCommonSaveCode.V10_VALUE:
                    if (this.dxf_nr_PatternsToMatchInPropertyOfComp > this.dxf_PatternsToMatchInPropertyOfComp.Count ||
                        this.dxf_nr_PatternsToMatchInPropertyOfParam > this.dxf_PatternsToMatchInPropertyOfParam.Count)
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
                    DXFDataToExcelMapping sMap = sE as DXFDataToExcelMapping;
                    if (sMap != null && this.dxf_ExcelData == null)
                    {
                        // take the parsed mapping
                        this.dxf_DataType = sMap.dxf_DataType;
                        this.dxf_ExcelData = sMap.dxf_parsed;
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
            if (Decoder.CurrentFileVersion < 5) //Id translation
            {
                if (this.dxf_TargetComponent_ID != -1) //Invalid mapping
                {
                    if (DXFDecoder.IdTranslation.TryGetValue((typeof(SimParameter), dxf_TargetParameterID), out var newId))
                        this.dxf_TargetParameterID = newId;
                }
            }

            this.dxf_parsed = this.Decoder.ProjectData.ExcelToolMappingManager.CreateExcelUnmappingNode(this.dxf_NodeName, this.dxf_DataType, this.dxf_ExcelData, this.dxf_UnmapByFilter,
                        this.dxf_PatternsToMatchInPropertyOfComp, this.dxf_PatternsToMatchInPropertyOfParam,
                        this.dxf_TargetParameterID, this.dxf_TargetPointer);
        }

        #endregion
    }
}
