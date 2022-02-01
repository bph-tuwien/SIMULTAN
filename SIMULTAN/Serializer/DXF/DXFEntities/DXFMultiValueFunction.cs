using SIMULTAN.Data;
using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper for class SimMultiValueFunction
    /// </summary>
    internal class DXFMultiValueFunction : DXFField
    {
        #region CLASS MEMBERS

        // specific info
        public double dxf_MinX { get; protected set; }
        public double dxf_MaxX { get; protected set; }
        public double dxf_MinY { get; protected set; }
        public double dxf_MaxY { get; protected set; }

        public int dxf_NrZ { get; protected set; }
        public double dxf_MinZ { get; protected set; }
        public double dxf_MaxZ { get; protected set; }

        // actual function field
        protected int dxf_nr_zs;
        protected int dxf_nr_zs_read;

        protected int dxf_nr_functions;
        protected int dxf_nr_functions_read;
        protected Point4D dxf_funct_point_current;
        protected Point4D dxf_funct_point_current_read;
        protected List<Point3D> dxf_funct_current;

        public List<double> dxf_Zs { get; protected set; }
        public List<List<Point3D>> dxf_FunctionGraphs { get; protected set; }

        protected int dxf_nr_fct_names;
        protected int dxf_nr_fct_names_read;

        public List<string> dxf_FunctionNames { get; protected set; }

        #endregion

        public DXFMultiValueFunction()
        {
            this.dxf_nr_zs = 0;
            this.dxf_nr_zs_read = 0;

            this.dxf_nr_functions = 0;
            this.dxf_nr_functions_read = 0;
            this.dxf_funct_point_current = new Point4D(0, 0, 0, 0);
            this.dxf_funct_point_current_read = new Point4D(0, 0, 0, 0);
            this.dxf_funct_current = new List<Point3D>();

            this.dxf_Zs = new List<double>();
            this.dxf_FunctionGraphs = new List<List<Point3D>>();

            this.dxf_nr_fct_names = 0;
            this.dxf_nr_fct_names_read = 0;

            this.dxf_FunctionNames = new List<string>();
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)MultiValueSaveCode.MinX:
                    this.dxf_MinX = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxX:
                    this.dxf_MaxX = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MinY:
                    this.dxf_MinY = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxY:
                    this.dxf_MaxY = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.NrZ:
                    this.dxf_NrZ = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MinZ:
                    this.dxf_MinZ = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.MaxZ:
                    this.dxf_MaxZ = this.Decoder.DoubleValue();
                    break;
                case (int)MultiValueSaveCode.ZS:
                    // marks the start of the sequence of values along the Z axis
                    this.dxf_nr_zs = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.FIELD:
                    // marks the start of the sequence of values in the FIELD
                    this.dxf_nr_functions = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.ROW_NAMES:
                    // marks the strt of the sequence of function names
                    this.dxf_nr_fct_names = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_zs > this.dxf_nr_zs_read)
                    {
                        this.dxf_Zs.Add(this.Decoder.DoubleValue());
                        this.dxf_nr_zs_read++;
                    }
                    else if (this.dxf_nr_functions > this.dxf_nr_functions_read)
                    {
                        if (this.dxf_funct_point_current_read.X == 0)
                        {
                            this.dxf_funct_point_current.X = this.Decoder.DoubleValue();
                            this.dxf_funct_point_current_read.X = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_functions > this.dxf_nr_functions_read)
                    {
                        if (this.dxf_funct_point_current_read.Y == 0)
                        {
                            this.dxf_funct_point_current.Y = this.Decoder.DoubleValue();
                            this.dxf_funct_point_current_read.Y = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    if (this.dxf_nr_functions > this.dxf_nr_functions_read)
                    {
                        if (this.dxf_funct_point_current_read.Z == 0)
                        {
                            this.dxf_funct_point_current.Z = this.Decoder.DoubleValue();
                            this.dxf_funct_point_current_read.Z = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.W_VALUE:
                    if (this.dxf_nr_functions > this.dxf_nr_functions_read)
                    {
                        if (this.dxf_funct_point_current_read.W == 0)
                        {
                            this.dxf_funct_point_current.W = this.Decoder.DoubleValue();
                            this.dxf_funct_point_current_read.W = 1;
                        }
                        // check if the entry was parsed completely
                        if (this.dxf_funct_point_current_read.X == 1 &&
                            this.dxf_funct_point_current_read.Y == 1 &&
                            this.dxf_funct_point_current_read.Z == 1 &&
                            this.dxf_funct_point_current_read.W == 1)
                        {
                            Point3D fpoint = new Point3D(this.dxf_funct_point_current.X,
                                                         this.dxf_funct_point_current.Y,
                                                         this.dxf_funct_point_current.Z);
                            this.dxf_funct_current.Add(fpoint);
                            this.dxf_funct_point_current_read = new Point4D(0, 0, 0, 0);

                            // finalize function
                            if (this.dxf_funct_point_current.W == ParamStructTypes.END_OF_LIST)
                            {
                                this.dxf_FunctionGraphs.Add(this.dxf_funct_current);
                                this.dxf_funct_current = new List<Point3D>();
                                this.dxf_nr_functions_read++;
                            }
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_fct_names > this.dxf_nr_fct_names_read)
                    {
                        this.dxf_FunctionNames.Add(this.Decoder.FValue);
                        this.dxf_nr_fct_names_read++;
                    }
                    break;
                default:
                    // DXFField: ENTITY_NAME, ID, MVType, Name, CanInterpolate,
                    // MVDisplayVector_NUMDIM, MVDisplayVector_CELL_INDEX_X, MVDisplayVector_CELL_INDEX_Y, MVDisplayVector_CELL_INDEX_Z, MVDisplayVector_CELL_INDEX_W,
                    // MVDisplayVector_POS_IN_CELL_REL_X, MVDisplayVector_POS_IN_CELL_REL_Y, MVDisplayVector_POS_IN_CELL_REL_Z,
                    // MVDisplayVector_POS_IN_CELL_ABS_X, MVDisplayVector_POS_IN_CELL_ABS_Y, MVDisplayVector_POS_IN_CELL_ABS_Z,
                    // MVDisplayVector_VALUE,
                    // MVUnitX, MVUnitY, MVUnitZ
                    //
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

            // check value lists for consistency
            bool data_consistent = true;
            data_consistent &= (this.dxf_nr_zs == this.dxf_nr_zs_read);
            data_consistent &= (this.dxf_nr_zs == this.dxf_NrZ);
            data_consistent &= (this.dxf_nr_functions == this.dxf_nr_functions_read);

            if (!data_consistent) return;

            // construct the function field
            if (this.dxf_FunctionNames.Count < this.dxf_FunctionGraphs.Count)
            {
                for (int i = 0; i < this.dxf_FunctionGraphs.Count; i++)
                {
                    this.dxf_FunctionNames.Add("Fct " + i);
                }
            }
            else if (this.dxf_FunctionNames.Count > this.dxf_FunctionGraphs.Count)
            {
                while (this.dxf_FunctionNames.Count > this.dxf_FunctionGraphs.Count)
                    this.dxf_FunctionNames.RemoveAt(this.dxf_FunctionNames.Count - 1);
            }

            //Construct graphs
            List<SimMultiValueFunctionGraph> graphs = new List<SimMultiValueFunctionGraph>(dxf_FunctionNames.Count);
            for (int i = 0; i < dxf_FunctionNames.Count; ++i)
                graphs.Add(new SimMultiValueFunctionGraph(dxf_FunctionNames[i], dxf_FunctionGraphs[i]));

            Rect bounds = new Rect(new Point(this.dxf_MinX, this.dxf_MinY), new Point(this.dxf_MaxX, this.dxf_MaxY));

            if (Decoder.CurrentFileVersion < 6) //Id translation
            {
                if (DXFDecoder.MultiValueCount > 1000000)
                    throw new Exception("Too many ValueFields");

                var newId = DXFDecoder.MultiValueIdOffset + DXFDecoder.MultiValueCount;
                DXFDecoder.MultiValueCount++;

                if (!DXFDecoder.IdTranslation.ContainsKey((typeof(SimMultiValue), this.ENT_ID)))
                    DXFDecoder.IdTranslation.Add((typeof(SimMultiValue), this.ENT_ID), newId);
                else
                {
                    Decoder.Log(string.Format("Multiple ValueFields with Id {0} found. Name=\"{1}\" Original Name=\"{2}\"",
                        this.ENT_ID, this.dxf_MVName,
                        this.Decoder.ProjectData.IdGenerator.GetById<SimMultiValue>(
                            new SimId(this.Decoder.ProjectData.Owner, DXFDecoder.IdTranslation[(typeof(SimMultiValue), this.ENT_ID)])
                            ).Name
                        ));
                }

                this.ENT_ID = newId;
            }

            this.Decoder.ProjectData.ValueManager.StartLoading();

            var mv = new SimMultiValueFunction(this.ENT_ID, this.dxf_MVName,
                                               this.dxf_MVUnitX, this.dxf_MVUnitY, this.dxf_MVUnitZ, bounds,
                                               this.dxf_Zs, graphs);
            this.Decoder.ProjectData.ValueManager.Add(mv);
            this.Decoder.ProjectData.ValueManager.EndLoading();

        }

        #endregion
    }
}
