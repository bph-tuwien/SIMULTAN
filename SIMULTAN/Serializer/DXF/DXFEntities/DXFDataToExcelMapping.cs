using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper class for Mapping Data FROM Excel
    /// </summary>
    internal class DXFDataToExcelMapping : DXFEntity
    {
        #region CLASS MEMBERS

        protected string dxf_SheetName;
        protected Point4D dxf_Range;

        internal Type dxf_DataType;
        internal ExcelMappedData dxf_parsed;

        #endregion 

        public DXFDataToExcelMapping()
        {
            this.dxf_DataType = typeof(object);
            this.dxf_Range = new Point4D(0, 0, 0, 0);
            this.dxf_SheetName = "Table Unknown";
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ExcelMappingSaveCode.DATA_MAP_SHEET_NAME:
                    this.dxf_SheetName = this.Decoder.FValue;
                    break;
                case (int)ExcelMappingSaveCode.DATA_MAP_RANGE_X:
                    this.dxf_Range.X = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.DATA_MAP_RANGE_Y:
                    this.dxf_Range.Y = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.DATA_MAP_RANGE_Z:
                    this.dxf_Range.Z = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.DATA_MAP_RANGE_W:
                    this.dxf_Range.W = this.Decoder.IntValue();
                    break;
                case (int)ExcelMappingSaveCode.DATA_MAP_TYPE:
                    this.dxf_DataType = Type.GetType(this.Decoder.FValue, false);
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
            this.dxf_parsed = ExcelMappedData.CreateEmpty(this.dxf_SheetName, this.dxf_Range);
        }

        #endregion
    }
}
