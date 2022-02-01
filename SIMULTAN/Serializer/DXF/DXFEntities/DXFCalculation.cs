using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper of class Calculation
    /// </summary>
    internal class DXFCalculation : DXFEntity
    {
        #region CLASS MEMBERS

        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public string dxf_Expression { get; protected set; }

        public Dictionary<string, long?> dxf_InputParams_Ref { get; protected set; }
        private int dxf_nr_InputParams;
        private int dxf_nr_InputParams_read;
        private string dxf_IP_key;
        private long dxf_IP_id;
        private Point dxf_IP_read;
        public Dictionary<string, long?> dxf_ReturnParams_Ref { get; protected set; }
        private int dxf_nr_ReturnParams;
        private int dxf_nr_ReturnParams_read;
        private string dxf_RP_key;
        private long dxf_RP_id;
        private Point dxf_RP_read;

        public List<MultiValueCalculationBinaryOperation> dxf_VectorOperations { get; protected set; }
        private int dxf_VectorOperations_nr;

        private int dxf_ParamRanges_nr;
        private int dxf_ParamRndSettings_nr;

        public Dictionary<string, CalculationParameterMetaData> dxf_MetaData { get; protected set; }
        private CalculationParameterMetaData dxf_CurrentMetaData;

        private bool readRange = false, readRandomize = false;

        private int dxf_NrExecutions;
        private bool dxf_AverageMultipleExecutionResults;
        private bool dxf_OverrideVectorResult;

        // wrap the underlying type
        internal CalculationInitializationData dxf_parsed;

        #endregion

        #region .CTOR

        public DXFCalculation()
            : base()
        {
            this.dxf_InputParams_Ref = new Dictionary<string, long?>();
            this.dxf_nr_InputParams = 0;
            this.dxf_nr_InputParams_read = 0;
            this.dxf_IP_key = string.Empty;
            this.dxf_IP_id = -1;
            this.dxf_IP_read = new Point(0, 0);

            this.dxf_ReturnParams_Ref = new Dictionary<string, long?>();
            this.dxf_nr_ReturnParams = 0;
            this.dxf_nr_ReturnParams_read = 0;
            this.dxf_RP_key = string.Empty;
            this.dxf_RP_id = -1;
            this.dxf_RP_read = new Point(0, 0);

            this.dxf_VectorOperations = new List<MultiValueCalculationBinaryOperation>();
            this.dxf_VectorOperations_nr = 0;
            this.dxf_ParamRanges_nr = 0;
            this.dxf_ParamRndSettings_nr = 0;

            this.dxf_MetaData = new Dictionary<string, CalculationParameterMetaData>();

            this.dxf_NrExecutions = 1;
            this.dxf_AverageMultipleExecutionResults = false;
            this.dxf_OverrideVectorResult = false;
        }

        #endregion

        #region OVERRIDES: Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)CalculationSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)CalculationSaveCode.EXPRESSION:
                    this.dxf_Expression = this.Decoder.FValue;
                    break;
                case (int)CalculationSaveCode.PARAMS_INPUT:
                    this.dxf_nr_InputParams = this.Decoder.IntValue();
                    break;
                case (int)CalculationSaveCode.PARAMS_OUTPUT:
                    this.dxf_nr_ReturnParams = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_InputParams > this.dxf_nr_InputParams_read && this.dxf_IP_read.X == 0)
                    {
                        this.dxf_IP_key = this.Decoder.FValue;
                        this.dxf_IP_read.X = 1;
                    }
                    else if (this.dxf_nr_ReturnParams > this.dxf_nr_ReturnParams_read && this.dxf_RP_read.X == 0)
                    {
                        this.dxf_RP_key = this.Decoder.FValue;
                        this.dxf_RP_read.X = 1;
                    }
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_REF:
                    {
                        if (this.dxf_nr_InputParams > this.dxf_nr_InputParams_read && this.dxf_IP_read.Y == 0)
                        {
                            if (this.Decoder.FValue == "NULL")
                                this.dxf_IP_read.Y = 2;
                            else
                            {
                                this.dxf_IP_id = this.Decoder.LongValue();
                                this.dxf_IP_read.Y = 1;
                            }
                        }
                        if (this.dxf_IP_read.X == 1 && this.dxf_IP_read.Y != 0)
                        {
                            if (!(this.dxf_InputParams_Ref.ContainsKey(this.dxf_IP_key)))
                            {
                                if (this.dxf_IP_read.Y == 2)
                                    this.dxf_InputParams_Ref.Add(this.dxf_IP_key, null);
                                else
                                    this.dxf_InputParams_Ref.Add(this.dxf_IP_key, this.dxf_IP_id);
                            }
                            this.dxf_nr_InputParams_read++;
                            this.dxf_IP_read = new Point(0, 0);
                        }
                        if (this.dxf_nr_ReturnParams > this.dxf_nr_ReturnParams_read && this.dxf_RP_read.Y == 0)
                        {
                            if (this.Decoder.FValue == "NULL")
                                this.dxf_RP_read.Y = 2;
                            else
                            {
                                this.dxf_RP_id = this.Decoder.LongValue();
                                this.dxf_RP_read.Y = 1;
                            }
                        }
                        if (this.dxf_RP_read.X == 1 && this.dxf_RP_read.Y != 0)
                        {
                            if (!(this.dxf_ReturnParams_Ref.ContainsKey(this.dxf_IP_key)))
                            {
                                if (dxf_RP_read.Y == 2)
                                    this.dxf_ReturnParams_Ref.Add(this.dxf_RP_key, null);
                                else
                                    this.dxf_ReturnParams_Ref.Add(this.dxf_RP_key, this.dxf_RP_id);
                            }
                            this.dxf_nr_ReturnParams_read++;
                            this.dxf_RP_read = new Point(0, 0);
                        }
                    }
                    break;
                case (int)CalculationSaveCode.VECTOR_CALC_OPERATIONS:
                    this.dxf_VectorOperations_nr = this.Decoder.IntValue();
                    break;
                case (int)CalculationSaveCode.VECTOR_CALC_RANGES:
                    this.dxf_ParamRanges_nr = this.Decoder.IntValue();
                    this.readRange = true; this.readRandomize = false;
                    break;
                case (int)CalculationSaveCode.VECTOR_CALC_RANDOM:
                    this.dxf_ParamRndSettings_nr = this.Decoder.IntValue();
                    this.readRandomize = true; this.readRange = false;
                    break;
                case (int)CalculationSaveCode.VECTOR_CALC_NR_EXEC:
                    this.dxf_NrExecutions = this.Decoder.IntValue();
                    break;
                case (int)CalculationSaveCode.VECTOR_CALC_AVERAGE:
                    this.dxf_AverageMultipleExecutionResults = this.Decoder.IntValue() == 1;
                    break;
                case (int)CalculationSaveCode.VECTOR_CALC_OVERRIDE:
                    this.dxf_OverrideVectorResult = this.Decoder.IntValue() == 1;
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    {
                        if (this.dxf_VectorOperations_nr > this.dxf_VectorOperations.Count)
                        {
                            MultiValueCalculationBinaryOperation op = (MultiValueCalculationBinaryOperation)this.Decoder.IntValue();

                            //Legacy operation handling
                            if (op == MultiValueCalculationBinaryOperation.VECTOR_SUM)
                                op = MultiValueCalculationBinaryOperation.MATRIX_SUM;
                            else if (op == MultiValueCalculationBinaryOperation.MATRIX_SCALAR_SUM)
                                op = MultiValueCalculationBinaryOperation.MATRIX_SUM_REPEAT_ROWCOLUMN;
                            else if (op == MultiValueCalculationBinaryOperation.MATRIX_SCALAR_PRODUCT)
                                op = MultiValueCalculationBinaryOperation.MATRIX_PRODUCT_PERELEMENT_REPEAT;

                            this.dxf_VectorOperations.Add(op);
                        }
                        else if (readRange)
                        {
                            var idx = this.Decoder.IntValue();
                            if (Decoder.CurrentFileVersion <= 3) //Up to 3, the index was stored 1-based
                                idx -= 1;

                            this.dxf_CurrentMetaData.Range = new RowColumnRange(
                                idx, dxf_CurrentMetaData.Range.RowCount,
                                dxf_CurrentMetaData.Range.ColumnStart, dxf_CurrentMetaData.Range.ColumnCount
                                );
                        }
                        else if (readRandomize)
                        {
                            this.dxf_CurrentMetaData.RandomizeRelativeMean = Decoder.DoubleValue();
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    {
                        if (readRange)
                        {
                            var count = this.Decoder.IntValue();
                            if (Decoder.CurrentFileVersion <= 3) //Up to 3: value contains End instead of Count
                                count -= dxf_CurrentMetaData.Range.RowStart;

                            this.dxf_CurrentMetaData.Range = new RowColumnRange(
                                dxf_CurrentMetaData.Range.RowStart, dxf_CurrentMetaData.Range.ColumnStart,
                                count, dxf_CurrentMetaData.Range.ColumnCount
                                );
                        }
                        else if (readRandomize)
                        {
                            this.dxf_CurrentMetaData.RandomizeDeviation = Decoder.DoubleValue();
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    {
                        if (readRange)
                        {
                            var idx = this.Decoder.IntValue();
                            if (Decoder.CurrentFileVersion <= 3) //Up to 3, the index was stored 1-based
                                idx -= 1;

                            this.dxf_CurrentMetaData.Range = new RowColumnRange(
                                dxf_CurrentMetaData.Range.RowStart, idx,
                                dxf_CurrentMetaData.Range.RowCount, dxf_CurrentMetaData.Range.ColumnCount
                                );
                        }
                        else if (readRandomize)
                        {
                            this.dxf_CurrentMetaData.RandomizeDeviationMode = (CalculationParameterMetaData.DeviationModeType)Decoder.IntValue();
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.W_VALUE:
                    {
                        if (readRange)
                        {
                            var count = this.Decoder.IntValue();
                            if (Decoder.CurrentFileVersion <= 3) //Up to 3: value contains End instead of Count
                                count -= dxf_CurrentMetaData.Range.ColumnStart;

                            this.dxf_CurrentMetaData.Range = new RowColumnRange(
                                dxf_CurrentMetaData.Range.RowStart, dxf_CurrentMetaData.Range.ColumnStart,
                                dxf_CurrentMetaData.Range.RowCount, count
                                );
                        }
                        else if (readRandomize)
                        {
                            this.dxf_CurrentMetaData.IsRandomized = Decoder.FValue == "1";
                        }
                    }
                    break;

                case (int)ParamStructCommonSaveCode.V5_VALUE: //Start of entry in range/randomize list
                    {
                        string symbol = null;

                        if (Decoder.CurrentFileVersion <= 3) //Entries are indexed by parameter id
                        {
                            var paramId = this.Decoder.LongValue();
                            symbol = this.dxf_InputParams_Ref.FirstOrDefault(x => x.Value.HasValue && x.Value == paramId).Key;
                        }
                        else
                        {
                            symbol = this.Decoder.FValue;
                        }

                        if (!this.dxf_MetaData.TryGetValue(symbol, out this.dxf_CurrentMetaData))
                        {
                            this.dxf_CurrentMetaData = new CalculationParameterMetaData()
                            {
                                RandomizeDeviationMode = CalculationParameterMetaData.DeviationModeType.Relative,
                                RandomizeIsClamping = true,
                                RandomizeClampDeviation = 1.0,
                            };
                            this.dxf_MetaData.Add(symbol, this.dxf_CurrentMetaData);
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.V6_VALUE:
                    {
                        if (readRandomize)
                        {
                            this.dxf_CurrentMetaData.RandomizeIsClamping = Decoder.FValue == "1";
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.V7_VALUE:
                    {
                        if (readRandomize)
                        {
                            this.dxf_CurrentMetaData.RandomizeClampDeviation = Decoder.DoubleValue();
                        }
                    }
                    break;

                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            if (Decoder.CurrentFileVersion < 5) //Id translation
            {
                if (DXFDecoder.CalculationCount > 1000000)
                    throw new Exception("Too many Calculations");

                var newId = DXFDecoder.CalculationIdOffset + DXFDecoder.CalculationCount;
                DXFDecoder.CalculationCount++;

                if (!DXFDecoder.IdTranslation.ContainsKey((typeof(SimCalculation), this.dxf_ID)))
                    DXFDecoder.IdTranslation.Add((typeof(SimCalculation), this.dxf_ID), newId);
                else
                {
                    var original = Decoder.ProjectData.IdGenerator.GetById<SimCalculation>(
                        new SimId(this.Decoder.ProjectData.Owner, DXFDecoder.IdTranslation[(typeof(SimCalculation), this.dxf_ID)])
                        );

                    Decoder.Log(string.Format("Multiple Calculations with Id {0} found. Name=\"{1}\" Other Parameter Name=\"{2}\"" +
                        " New Id current: {3}, New Id original: {4}",
                        this.dxf_ID, this.dxf_Name,
                        original != null ? original.Name : "???",
                        newId, original.Id.LocalId
                        ));
                }

                this.dxf_ID = newId;

                //Exchange parameter Ids
                foreach (var key in dxf_InputParams_Ref.Keys.ToList())
                    if (dxf_InputParams_Ref[key].HasValue)
                        dxf_InputParams_Ref[key] = DXFDecoder.IdTranslation[(typeof(SimParameter), dxf_InputParams_Ref[key].Value)];

                foreach (var key in dxf_ReturnParams_Ref.Keys.ToList())
                    if (dxf_ReturnParams_Ref[key].HasValue)
                        dxf_ReturnParams_Ref[key] = DXFDecoder.IdTranslation[(typeof(SimParameter), dxf_ReturnParams_Ref[key].Value)];
            }

            this.dxf_parsed = new CalculationInitializationData(this.dxf_ID,
                                                     this.dxf_Name, this.dxf_Expression, this.dxf_InputParams_Ref, this.dxf_ReturnParams_Ref,
                                                     this.dxf_VectorOperations,
                                                     this.dxf_MetaData,
                                                     this.dxf_NrExecutions,
                                                     this.dxf_AverageMultipleExecutionResults ? SimResultAggregationMethod.Average : SimResultAggregationMethod.Separate,
                                                     this.dxf_OverrideVectorResult);
        }

        #endregion
    }
}
