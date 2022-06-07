using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    ///  wrapper of class Parameter
    /// </summary>
    internal class DXFParameter : DXFEntity
    {
        #region CLASS MEMBERS

        // general
        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public string dxf_Unit { get; protected set; }
        public SimCategory dxf_Category { get; protected set; }
        public SimInfoFlow dxf_Propagation { get; protected set; }

        // value management
        public double dxf_ValueMin { get; protected set; }
        public double dxf_ValueMax { get; protected set; }
        public double dxf_ValueCurrent { get; protected set; }

        // value field management
        public long dxf_ValueFieldRef { get; protected set; }

        protected Guid dxf_mvfp_location;
        protected long dxf_mvfp_mvid;
        protected double dxf_mvfp_axisValueX;
        protected double dxf_mvfp_axisValueY;
        protected double dxf_mvfp_axisValueZ;
        protected string dxf_mvfp_string;


        // display vector (for choosing values or interpolation of the referenced value field)
        protected int dxf_mvdv_num_dim;
        protected List<int> dxf_mvdv_cell_indices;
        protected Point dxf_mvdv_cell_size;
        protected Point dxf_mvdv_pos_rel;
        protected Point dxf_mvdv_pos_abs;
        protected double dxf_mvdv_value;

        public bool dxf_isAutomaticallyGenerated = false;

        // time stamp
        public DateTime dxf_TimeStamp { get; private set; }

        // string value
        public string dxf_TextValue { get; private set; }

        // operations
        protected SimParameterOperations dxf_AllowedOperations;

        // for being included in components
        internal SimParameter dxf_parsed;

        private SimParameterInstancePropagation dxf_instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance;

        #endregion

        public DXFParameter()
        {
            this.dxf_mvdv_num_dim = 0;
            this.dxf_mvdv_cell_indices = new List<int> { -1, -1, -1, -1 };
            this.dxf_mvdv_cell_size = new Point(0, 0);
            this.dxf_mvdv_pos_rel = new Point(0, 0);
            this.dxf_mvdv_pos_abs = new Point(0, 0);

            this.dxf_mvfp_location = Guid.Empty;
            this.dxf_mvfp_mvid = -1;
            this.dxf_mvfp_axisValueX = 0;
            this.dxf_mvfp_axisValueY = 0;
            this.dxf_mvfp_axisValueZ = 0;
            this.dxf_mvfp_string = string.Empty;

            this.dxf_TextValue = string.Empty;
            this.dxf_AllowedOperations = SimParameterOperations.All;
        }

        #region OVERRIDES : Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)ParameterSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)ParameterSaveCode.UNIT:
                    this.dxf_Unit = this.Decoder.FValue;
                    break;
                case (int)ParameterSaveCode.CATEGORY:
                    string cat_as_str = this.Decoder.FValue;
                    this.dxf_Category = ComponentUtils.StringToCategory(cat_as_str);
                    break;
                case (int)ParameterSaveCode.PROPAGATION:
                    string prop_as_str = this.Decoder.FValue;
                    this.dxf_Propagation = ComponentUtils.StringToInfoFlow(prop_as_str);
                    break;
                case (int)ParameterSaveCode.VALUE_MIN:
                    this.dxf_ValueMin = this.Decoder.DoubleValue();
                    break;
                case (int)ParameterSaveCode.VALUE_MAX:
                    this.dxf_ValueMax = this.Decoder.DoubleValue();
                    break;
                case (int)ParameterSaveCode.VALUE_CURRENT:
                    this.dxf_ValueCurrent = this.Decoder.DoubleValue();
                    break;
                case (int)ParamStructCommonSaveCode.TIME_STAMP:
                    DateTime dt_tmp = DateTime.Now;
                    bool dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                    if (dt_p_success)
                        this.dxf_TimeStamp = dt_tmp;
                    break;
                case (int)ParameterSaveCode.VALUE_TEXT:
                    this.dxf_TextValue = this.Decoder.FValue;
                    break;
                case (int)ParameterSaveCode.ALLOWED_OPERATIONS:
                    bool success = Enum.TryParse<SimParameterOperations>(this.Decoder.FValue, out var result);
                    if (success)
                        this.dxf_AllowedOperations = result;
                    break;
                case (int)ParameterSaveCode.VALUE_FIELD_REF:
                    this.dxf_ValueFieldRef = this.Decoder.LongValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_NUMDIM:
                    this.dxf_mvdv_num_dim = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_X:
                    this.dxf_mvdv_cell_indices[0] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Y:
                    this.dxf_mvdv_cell_indices[1] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_Z:
                    this.dxf_mvdv_cell_indices[2] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_INDEX_W:
                    this.dxf_mvdv_cell_indices[3] = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_W:
                    this.dxf_mvdv_cell_size.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_CELL_SIZE_H:
                    this.dxf_mvdv_cell_size.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_X:
                    this.dxf_mvdv_pos_rel.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Y:
                    this.dxf_mvdv_pos_rel.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_REL_Z:
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_X:
                    this.dxf_mvdv_pos_abs.X = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Y:
                    this.dxf_mvdv_pos_abs.Y = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_POS_IN_CELL_ABS_Z:
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_VALUE:
                    this.dxf_mvdv_value = this.Decoder.DoubleValue();
                    break;

                case (int)MultiValueSaveCode.MVDisplayVector_AXIS_VAL_X:
                    this.dxf_mvfp_axisValueX = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_AXIS_VAL_Y:
                    this.dxf_mvfp_axisValueY = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_AXIS_VAL_Z:
                    this.dxf_mvfp_axisValueZ = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_MVLOCATION:
                    this.dxf_mvfp_location = new Guid(this.Decoder.FValue);
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_MVID:
                    this.dxf_mvfp_mvid = this.Decoder.LongValue();
                    break;
                case (int)MultiValueSaveCode.MVDisplayVector_GRAPH_NAME:
                    this.dxf_mvfp_string = this.Decoder.FValue;
                    break;
                case (int)ParameterSaveCode.IS_AUTOGENERATED:
                    this.dxf_isAutomaticallyGenerated = this.Decoder.FValue == "1";
                    break;
                case (int)ParameterSaveCode.INSTANCE_PROPAGATION:
                    int ipm = this.Decoder.IntValue();
                    this.dxf_instancePropagationMode = (SimParameterInstancePropagation)ipm;
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

            SimMultiValuePointer valuePointer = null;

            if (dxf_mvfp_mvid == -1) //Old file (before MV pointer refactoring)
            {
                // look for the associated value field
                SimMultiValue field = this.Decoder.ProjectData.ValueManager.GetByID(Guid.Empty, this.dxf_ValueFieldRef);

                // construct the pointer into the value field
                if (field is SimMultiValueBigTable)
                {
                    var table = (SimMultiValueBigTable)field;
                    var row = dxf_mvdv_cell_indices[0];
                    if (row >= table.RowHeaders.Count)
                    {
                        row = table.RowHeaders.Count - 1;
                        Console.WriteLine("Row index out of bounds for valuefield {0}", table.Name);
                    }
                    var column = dxf_mvdv_cell_indices[1];
                    if (column >= table.ColumnHeaders.Count)
                    {
                        column = table.ColumnHeaders.Count - 1;
                        Console.WriteLine("Column index out of bounds for valuefield {0}", table.Name);
                    }

                    valuePointer = new SimMultiValueBigTable.SimMultiValueBigTablePointer(table, row, column);
                }
                else if (field is SimMultiValueFunction)
                {
                    var function = (SimMultiValueFunction)field;

                    var graphIdx = this.dxf_mvdv_cell_indices[0];
                    if (graphIdx >= function.Graphs.Count)
                    {
                        graphIdx = 0;
                        Console.WriteLine("Unable to find correct graph");
                    }
                    var graph = function.Graphs[graphIdx];

                    valuePointer = new SimMultiValueFunction.MultiValueFunctionPointer(function,
                        graph.Name, graph.Points[0].X, 0.0);
                    Console.WriteLine("ValueField for parameter {0} not correctly attached", dxf_Name);
                }
                else if (field is SimMultiValueField3D)
                {
                    var table = (SimMultiValueField3D)field;

                    valuePointer = new SimMultiValueField3D.SimMultiValueField3DPointer(table,
                        dxf_mvdv_cell_indices[0], dxf_mvdv_cell_indices[1], dxf_mvdv_cell_indices[2]
                        );
                    //TODO ValuePointer refactoring
                    //Add relative coordinates
                    Console.WriteLine("ValueField for parameter {0} may be attached to a wrong location", dxf_Name);
                }
            }
            else
            {
                if (Decoder.CurrentFileVersion < 6)
                    this.dxf_mvfp_mvid = DXFDecoder.IdTranslation[(typeof(SimMultiValue), this.dxf_mvfp_mvid)];

                //Atm, only references to ValueFields in the current project are supported
                var projectId = this.Decoder.ProjectData.Owner != null ? this.Decoder.ProjectData.Owner.GlobalID : Guid.Empty;
                SimMultiValue field = this.Decoder.ProjectData.IdGenerator.GetById<SimMultiValue>(new SimId(projectId, this.dxf_mvfp_mvid));

                if (field != null)
                {
                    valuePointer = field.CreateNewPointer();
                    valuePointer.SetFromParameters(dxf_mvfp_axisValueX, dxf_mvfp_axisValueY, dxf_mvfp_axisValueZ, dxf_mvfp_string);
                }

            }

            //Handle move operation for pre-version 1
            var allowedOperations = this.dxf_AllowedOperations;
            if (Decoder.CurrentFileVersion <= 0)
            {
                allowedOperations |= SimParameterOperations.Move;
            }

            if (Decoder.CurrentFileVersion < 5) //Id translation
            {
                if (DXFDecoder.ParameterCount > 1000000)
                    throw new Exception("Too many Parameters");

                var newId = DXFDecoder.ParameterIdOffset + DXFDecoder.ParameterCount;
                DXFDecoder.ParameterCount++;

                //Ids should be unique, but some old projects have errors in them
                if (!DXFDecoder.IdTranslation.ContainsKey((typeof(SimParameter), this.dxf_ID)))
                {
                    DXFDecoder.IdTranslation.Add((typeof(SimParameter), this.dxf_ID), newId);
                }
                else
                {
                    var originalParameter = this.Decoder.ProjectData.ParameterLibraryManager.ParameterRecord.FirstOrDefault(
                            x => x.Id.LocalId == DXFDecoder.IdTranslation[(typeof(SimParameter), this.dxf_ID)]
                            );
                    if (originalParameter == null)
                    {
                        originalParameter = this.Decoder.ProjectData.IdGenerator.GetById<SimParameter>(
                            new SimId(this.Decoder.ProjectData.Owner, DXFDecoder.IdTranslation[(typeof(SimParameter), this.dxf_ID)])
                            );
                    }

                    Decoder.Log(string.Format("Multiple Parameters with Id {0} found. Name=\"{1}\" Other Parameter Name=\"{2}\"" +
                        " New Id current: {3}, New Id original: {4}",
                        this.dxf_ID, this.dxf_Name,
                        originalParameter != null ? originalParameter.Name : "???",
                        newId, originalParameter != null ? originalParameter.Id.LocalId : -1
                        ));
                }

                this.dxf_ID = newId;
            }

            if (dxf_ID == 0)
                Console.WriteLine("test");

            // create the parameter (and save it internally)
            if (Decoder.DecoderMode == DXFDecoderMode.Parameters)
            {
                this.dxf_parsed =
                this.Decoder.ProjectData.ParameterLibraryManager.ReconstructParameter(this.dxf_ID, this.dxf_Name, this.dxf_Unit, this.dxf_Category, this.dxf_Propagation,
                                                            this.dxf_ValueMin, this.dxf_ValueMax, this.dxf_ValueCurrent,
                                                            valuePointer, this.dxf_TimeStamp, this.dxf_TextValue,
                                                            this.dxf_AllowedOperations, this.dxf_instancePropagationMode,
                                                            this.dxf_isAutomaticallyGenerated);
            }
            else
            {
                this.dxf_parsed = new SimParameter(dxf_ID, dxf_Name, dxf_Unit, dxf_Category, dxf_Propagation,
                                               dxf_ValueCurrent, dxf_ValueMin, dxf_ValueMax,
                                               dxf_TextValue, valuePointer, dxf_AllowedOperations,
                                               dxf_instancePropagationMode, dxf_isAutomaticallyGenerated);
            }
        }

        #endregion
    }
}
