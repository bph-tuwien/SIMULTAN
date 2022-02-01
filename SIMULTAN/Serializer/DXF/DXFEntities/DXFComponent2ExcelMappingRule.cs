using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFComponent2ExcelMappingRule : DXFEntity
    {
        #region CLASS MEMBERS

        protected List<long> dxf_Path;
        protected int dxf_nr_Path;

        protected string dxf_ToolName;
        protected string dxf_ToolFilePath;
        protected string dxf_RuleName;
        protected int dxf_RuleIndex;

        internal ExcelComponentMapping dxf_parsed;

        #endregion

        public DXFComponent2ExcelMappingRule()
        {
            this.dxf_Path = new List<long>();
            this.dxf_nr_Path = 0;

            this.dxf_ToolName = string.Empty;
            this.dxf_ToolFilePath = string.Empty;
            this.dxf_RuleName = string.Empty;
            this.dxf_RuleIndex = 0;
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ExcelMappingSaveCode.MAP_PATH:
                    this.dxf_nr_Path = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.MAP_TOOL_NAME:
                    this.dxf_ToolName = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.MAP_TOOL_FILE_PATH:
                    this.dxf_ToolFilePath = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.MAP_RULE_NAME:
                    this.dxf_RuleName = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.MAP_RULE_INDEX:
                    this.dxf_RuleIndex = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_Path > this.dxf_Path.Count)
                    {
                        long id = this.Decoder.LongValue();
                        this.dxf_Path.Add(id);
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            if (Decoder.CurrentFileVersion <= 8) //Translate component id parts of the key
            {
                var split = this.ENT_KEY.Split('.');

                // The path is everything from the end to the first item that cannot be parsed to a long
                for (int i = split.Length - 1; i >= 0; --i)
                {
                    if (long.TryParse(split[i], out var id))
                    {
                        split[i] = Decoder.TranslateComponentIdV8(id).ToString();
                    }
                    else
                        break;
                }

                this.ENT_KEY = string.Join(".", split);
            }

            // create the mapping and save it internally
            this.dxf_parsed = new ExcelComponentMapping(this.dxf_Path, this.dxf_ToolName, this.dxf_ToolFilePath, this.dxf_RuleName, this.dxf_RuleIndex);
        }

        #endregion
    }
}
