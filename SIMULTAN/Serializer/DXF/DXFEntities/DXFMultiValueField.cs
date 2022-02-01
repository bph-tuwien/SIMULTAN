using SIMULTAN.Data;
using SIMULTAN.Data.MultiValues;
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
    /// wrapper for class SimMultiValueField3D
    /// </summary>
    internal class DXFMultiValueField3D : DXFField
    {
        #region CLASS MEMBERS

        // specific info
        public int dxf_NrX { get; protected set; }
        public int dxf_NrY { get; protected set; }
        public int dxf_NrZ { get; protected set; }

        // actual value field
        protected int dxf_nr_xs;
        protected int dxf_nr_xs_read;

        protected int dxf_nr_ys;
        protected int dxf_nr_ys_read;

        protected int dxf_nr_zs;
        protected int dxf_nr_zs_read;

        protected int dxf_nr_field_vals;
        protected int dxf_nr_field_vals_read;
        protected Point4D dxf_field_entry_current;
        protected Point4D dxf_field_entry_current_read;

        public List<double> dxf_Xs { get; private set; }
        public List<double> dxf_Ys { get; private set; }
        public List<double> dxf_Zs { get; private set; }
        public Dictionary<Point3D, double> dxf_Field { get; protected set; }

        #endregion

        public DXFMultiValueField3D()
            : base()
        {
            this.dxf_nr_xs = 0;
            this.dxf_nr_xs_read = 0;

            this.dxf_nr_ys = 0;
            this.dxf_nr_ys_read = 0;

            this.dxf_nr_zs = 0;
            this.dxf_nr_zs_read = 0;

            this.dxf_nr_field_vals = 0;
            this.dxf_nr_field_vals_read = 0;
            this.dxf_field_entry_current = new Point4D(0, 0, 0, 0);
            this.dxf_field_entry_current_read = new Point4D(0, 0, 0, 0);

            this.dxf_Xs = new List<double>();
            this.dxf_Ys = new List<double>();
            this.dxf_Zs = new List<double>();
            this.dxf_Field = new Dictionary<Point3D, double>();
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)MultiValueSaveCode.NrX:
                    this.dxf_NrX = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.NrY:
                    this.dxf_NrY = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.NrZ:
                    this.dxf_NrZ = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.XS:
                    // marks the start of the sequence of values along the X axis
                    this.dxf_nr_xs = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.YS:
                    // marks the start of the sequence of values along the Y axis
                    this.dxf_nr_ys = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.ZS:
                    // marks the start of the sequence of values along the Z axis
                    this.dxf_nr_zs = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.FIELD:
                    // marks the start of the sequence of values in the FIELD
                    this.dxf_nr_field_vals = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_xs > this.dxf_nr_xs_read)
                    {
                        this.dxf_Xs.Add(this.Decoder.DoubleValue());
                        this.dxf_nr_xs_read++;
                    }
                    else if (this.dxf_nr_ys > this.dxf_nr_ys_read)
                    {
                        this.dxf_Ys.Add(this.Decoder.DoubleValue());
                        this.dxf_nr_ys_read++;
                    }
                    else if (this.dxf_nr_zs > this.dxf_nr_zs_read)
                    {
                        this.dxf_Zs.Add(this.Decoder.DoubleValue());
                        this.dxf_nr_zs_read++;
                    }
                    else if (this.dxf_nr_field_vals > this.dxf_nr_field_vals_read)
                    {
                        if (this.dxf_field_entry_current_read.X == 0)
                        {
                            this.dxf_field_entry_current.X = this.Decoder.DoubleValue();
                            this.dxf_field_entry_current_read.X = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_field_vals > this.dxf_nr_field_vals_read)
                    {
                        if (this.dxf_field_entry_current_read.Y == 0)
                        {
                            this.dxf_field_entry_current.Y = this.Decoder.DoubleValue();
                            this.dxf_field_entry_current_read.Y = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    if (this.dxf_nr_field_vals > this.dxf_nr_field_vals_read)
                    {
                        if (this.dxf_field_entry_current_read.Z == 0)
                        {
                            this.dxf_field_entry_current.Z = this.Decoder.DoubleValue();
                            this.dxf_field_entry_current_read.Z = 1;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.W_VALUE:
                    if (this.dxf_nr_field_vals > this.dxf_nr_field_vals_read)
                    {
                        if (this.dxf_field_entry_current_read.W == 0)
                        {
                            this.dxf_field_entry_current.W = this.Decoder.DoubleValue();
                            this.dxf_field_entry_current_read.W = 1;
                        }
                        // check if the entry was parsed completely
                        if (this.dxf_field_entry_current_read.X == 1 &&
                            this.dxf_field_entry_current_read.Y == 1 &&
                            this.dxf_field_entry_current_read.Z == 1 &&
                            this.dxf_field_entry_current_read.W == 1)
                        {
                            Point3D key = new Point3D(this.dxf_field_entry_current.X,
                                                      this.dxf_field_entry_current.Y,
                                                      this.dxf_field_entry_current.Z);
                            if (!this.dxf_Field.ContainsKey(key))
                                this.dxf_Field.Add(key, this.dxf_field_entry_current.W);
                            this.dxf_field_entry_current_read = new Point4D(0, 0, 0, 0);
                            this.dxf_nr_field_vals_read++;
                        }
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

        #region OVERRIDES : Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            // check value lists for consistency
            bool data_consistent = true;
            data_consistent &= (this.dxf_nr_xs == this.dxf_nr_xs_read);
            data_consistent &= (this.dxf_nr_xs == this.dxf_NrX);
            data_consistent &= (this.dxf_nr_ys == this.dxf_nr_ys_read);
            data_consistent &= (this.dxf_nr_ys == this.dxf_NrY);
            data_consistent &= (this.dxf_nr_zs == this.dxf_nr_zs_read);
            data_consistent &= (this.dxf_nr_zs == this.dxf_NrZ);
            data_consistent &= (this.dxf_nr_field_vals == this.dxf_nr_field_vals_read);

            if (!data_consistent) return;

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

            // construct the value field
            this.Decoder.ProjectData.ValueManager.StartLoading();
            var table = new SimMultiValueField3D(this.ENT_ID, this.dxf_MVName,
                                            this.dxf_Xs, this.dxf_MVUnitX,
                                            this.dxf_Ys, this.dxf_MVUnitY,
                                            this.dxf_Zs, this.dxf_MVUnitZ,
                                            this.dxf_Field,
                                            this.dxf_MVCanInterpolate);
            this.Decoder.ProjectData.ValueManager.Add(table);
            this.Decoder.ProjectData.ValueManager.EndLoading();
        }

        #endregion
    }
}
