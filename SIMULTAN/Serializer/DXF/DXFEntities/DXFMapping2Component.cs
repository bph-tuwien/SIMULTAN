using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper class for Component to Component Mapping
    /// </summary>
    internal class DXFMapping2Component : DXFEntity
    {
        #region CLASS MEMBERS

        public string dxf_Name { get; private set; }
        public long dxf_Calculator_LocalID { get; private set; }
        public Guid dxf_Calculator_GlobalID { get; private set; }

        public List<(long dataParameterId, long calculatorParameterId)> dxf_InputMapping { get; private set; }
        private int dxf_nr_InputMapping;
        private long dxf_input_entry_Key;
        private long dxf_input_entry_Value;
        public List<(long dataParameterId, long calculatorParameterId)> dxf_OutputMapping { get; private set; }
        private int dxf_nr_OutputMapping;
        private long dxf_output_entry_Key;
        private long dxf_output_entry_Value;

        // for being included in components
        internal CalculatorMapping dxf_parsed;

        #endregion

        public DXFMapping2Component()
        {
            this.dxf_Name = string.Empty;
            this.dxf_Calculator_LocalID = -1L;
            this.dxf_Calculator_GlobalID = Guid.Empty;

            this.dxf_InputMapping = new List<(long dataParameterId, long calculatorParameterId)>();
            this.dxf_nr_InputMapping = 0;
            this.dxf_input_entry_Key = -1;
            this.dxf_input_entry_Value = -1;

            this.dxf_OutputMapping = new List<(long dataParameterId, long calculatorParameterId)>();
            this.dxf_nr_OutputMapping = 0;
            this.dxf_output_entry_Key = -1;
            this.dxf_output_entry_Value = -1;
        }

        #region OVERRIDES: Read Property
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)Mapping2ComponentSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)Mapping2ComponentSaveCode.CALCULATOR:

                    if (this.Decoder.CurrentFileVersion < 1)
                    {
                        this.dxf_Calculator_GlobalID = Guid.Empty;
                        this.dxf_Calculator_LocalID = this.Decoder.LongValue();
                    }
                    else
                    {
                        var id = this.Decoder.GlobalAndLocalValue();
                        if (id.local > 0)
                        {
                            this.dxf_Calculator_GlobalID = id.global;
                            this.dxf_Calculator_LocalID = id.local;
                        }
                        else
                        {
                            this.dxf_Calculator_GlobalID = Guid.Empty;
                            this.dxf_Calculator_LocalID = 0;
                        }
                    }

                    break;
                case (int)Mapping2ComponentSaveCode.INPUT_MAPPING:
                    this.dxf_nr_InputMapping = this.Decoder.IntValue();
                    break;
                case (int)Mapping2ComponentSaveCode.INPUT_MAPPING_KEY:
                    if (this.dxf_nr_InputMapping > this.dxf_InputMapping.Count)
                    {
                        this.dxf_input_entry_Key = this.Decoder.LongValue();
                    }
                    break;
                case (int)Mapping2ComponentSaveCode.INPUT_MAPPING_VALUE:
                    if (this.dxf_nr_InputMapping > this.dxf_InputMapping.Count)
                    {
                        this.dxf_input_entry_Value = this.Decoder.LongValue();
                        this.dxf_InputMapping.Add((this.dxf_input_entry_Key, this.dxf_input_entry_Value));
                        this.dxf_input_entry_Key = -1L;
                        this.dxf_input_entry_Value = -1L;
                    }
                    break;
                case (int)Mapping2ComponentSaveCode.OUTPUT_MAPPING:
                    this.dxf_nr_OutputMapping = this.Decoder.IntValue();
                    break;
                case (int)Mapping2ComponentSaveCode.OUTPUT_MAPPING_KEY:
                    if (this.dxf_nr_OutputMapping > this.dxf_OutputMapping.Count)
                    {
                        this.dxf_output_entry_Key = this.Decoder.LongValue();
                    }
                    break;
                case (int)Mapping2ComponentSaveCode.OUTPUT_MAPPING_VALUE:
                    if (this.dxf_nr_OutputMapping > this.dxf_OutputMapping.Count)
                    {
                        this.dxf_output_entry_Value = this.Decoder.LongValue();
                        this.dxf_OutputMapping.Add((this.dxf_output_entry_Key, this.dxf_output_entry_Value));
                        this.dxf_output_entry_Key = -1L;
                        this.dxf_output_entry_Value = -1L;
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

            this.dxf_Calculator_LocalID = Decoder.TranslateComponentIdV8(dxf_Calculator_LocalID);

            // create the mapping and save it internally
            this.dxf_parsed = new CalculatorMapping(this.dxf_Name, new SimId(dxf_Calculator_GlobalID, dxf_Calculator_LocalID),
                this.dxf_InputMapping, this.dxf_OutputMapping, DXFDecoder.IdTranslation);
        }

        #endregion
    }
}
