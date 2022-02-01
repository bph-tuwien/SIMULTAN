using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFExcelTool : DXFEntityContainer
    {
        private string dxf_Name;
        private string dxf_LastPathToFile;
        private string dxf_Macro_Name;
        private List<ExcelMappingNode> dxf_Rules;
        private int dxf_nr_Rules;
        private List<KeyValuePair<ExcelMappedData, Type>> dxf_Results;
        private int dxf_nr_Results;
        private List<ExcelUnmappingRule> dxf_ResultUnmappings;
        private int dxf_nr_ResultUnmappings;

        public DXFExcelTool()
        {
            this.dxf_Name = string.Empty;
            this.dxf_LastPathToFile = string.Empty;
            this.dxf_Macro_Name = string.Empty;
            this.dxf_Rules = new List<ExcelMappingNode>();
            this.dxf_nr_Rules = 0;
            this.dxf_Results = new List<KeyValuePair<ExcelMappedData, Type>>();
            this.dxf_nr_Results = 0;
            this.dxf_ResultUnmappings = new List<ExcelUnmappingRule>();
            this.dxf_nr_ResultUnmappings = 0;
        }

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ExcelMappingSaveCode.TOOL_NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.TOOL_LAST_PATH_TO_FILE:
                    this.dxf_LastPathToFile = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.TOOL_MACRO_NAME:
                    this.dxf_Macro_Name = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.TOOL_RULES:
                    this.dxf_nr_Rules = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.TOOL_RESULTS:
                    this.dxf_nr_Results = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.TOOL_RESULT_UNMAPPINGS:
                    this.dxf_nr_ResultUnmappings = this.Decoder.IntValue();
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

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
                    if (sNode != null && this.dxf_nr_Rules > this.dxf_Rules.Count)
                    {
                        // take the parsed excel node
                        this.dxf_Rules.Add(sNode.dxf_parsed);
                        add_successful &= true;
                    }
                    DXFDataToExcelMapping sMap = sE as DXFDataToExcelMapping;
                    if (sMap != null && this.dxf_nr_Results > this.dxf_Results.Count)
                    {
                        // take the parsed mapping
                        this.dxf_Results.Add(new KeyValuePair<ExcelMappedData, Type>(sMap.dxf_parsed, sMap.dxf_DataType));
                        add_successful &= true;
                    }
                    DXFExcelUnmappingRule sUnmap = sE as DXFExcelUnmappingRule;
                    if (sUnmap != null && this.dxf_nr_ResultUnmappings > this.dxf_ResultUnmappings.Count)
                    {
                        // take the parsed unmapping
                        this.dxf_ResultUnmappings.Add(sUnmap.dxf_parsed);
                        add_successful &= true;
                    }
                }
            }
            return add_successful;
        }

        internal override void OnLoaded()
        {
            base.OnLoaded();

            this.Decoder.ProjectData.ExcelToolMappingManager.CreateExcelTool(this.dxf_Name, this.dxf_Rules, this.dxf_Results, this.dxf_ResultUnmappings, this.dxf_Macro_Name, this.dxf_LastPathToFile);
        }
    }
}
