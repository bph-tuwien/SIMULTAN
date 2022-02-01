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
    /// abstract ancestor of DXF_ValueField, DXF_FunctionField, DXF_BigTable
    /// </summary>
    internal abstract class DXFField : DXFEntity
    {
        #region CLASS MEMBERS

        // general
        public long dxf_MVID { get; protected set; }
        public MultiValueType dxf_MVType { get; protected set; }
        public string dxf_MVName { get; protected set; }
        public bool dxf_MVCanInterpolate { get; protected set; }

        // display vector (for choosing values or interpolation)
        protected double dxf_mvdv_value;

        // general info
        public string dxf_MVUnitX { get; protected set; }
        public string dxf_MVUnitY { get; protected set; }
        public string dxf_MVUnitZ { get; protected set; }

        #endregion

        public DXFField()
            : base()
        {
            this.dxf_MVName = string.Empty;
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)MultiValueSaveCode.MVType:
                    this.dxf_MVType = (MultiValueType)this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.MVName:
                    this.dxf_MVName = this.Decoder.FValue;
                    break;
                case (int)MultiValueSaveCode.MVCanInterpolate:
                    this.dxf_MVCanInterpolate = (this.Decoder.IntValue() == 1) ? true : false;
                    break;
                case (int)MultiValueSaveCode.MVUnitX:
                    this.dxf_MVUnitX = this.Decoder.FValue;
                    break;
                case (int)MultiValueSaveCode.MVUnitY:
                    this.dxf_MVUnitY = this.Decoder.FValue;
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_MVID = this.ENT_ID;
                    break;
            }
        }

        #endregion
    }
}
