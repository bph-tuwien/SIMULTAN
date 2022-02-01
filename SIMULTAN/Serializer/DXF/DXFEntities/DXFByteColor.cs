using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// Serialization wrapper for <see cref="Color"/>
    /// </summary>
    internal class DXFByteColor : DXFEntity
    {
        #region CLASS MEMBERS

        private int dxf_nr_color_components;
        private int dxf_nr_color_components_read;
        private List<byte> dxf_color_components;

        internal Color dxf_parsed;

        #endregion

        #region .CTOR

        public DXFByteColor()
        {
            this.dxf_nr_color_components = 4;
            this.dxf_nr_color_components_read = 0;
            this.dxf_color_components = new List<byte> { 0, 0, 0, 0 };
        }

        #endregion

        #region OVERRIDES : Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.V5_VALUE:
                    if (this.dxf_nr_color_components > this.dxf_nr_color_components_read)
                    {
                        this.dxf_nr_color_components_read++;
                        this.dxf_color_components[0] = this.Decoder.ByteValue();
                    }
                    break;
                case (int)ParamStructCommonSaveCode.V6_VALUE:
                    if (this.dxf_nr_color_components > this.dxf_nr_color_components_read)
                    {
                        this.dxf_nr_color_components_read++;
                        this.dxf_color_components[1] = this.Decoder.ByteValue();
                    }
                    break;
                case (int)ParamStructCommonSaveCode.V7_VALUE:
                    if (this.dxf_nr_color_components > this.dxf_nr_color_components_read)
                    {
                        this.dxf_nr_color_components_read++;
                        this.dxf_color_components[2] = this.Decoder.ByteValue();
                    }
                    break;
                case (int)ParamStructCommonSaveCode.V8_VALUE:
                    if (this.dxf_nr_color_components > this.dxf_nr_color_components_read)
                    {
                        this.dxf_nr_color_components_read++;
                        this.dxf_color_components[3] = this.Decoder.ByteValue();
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

            this.dxf_parsed = Color.FromArgb(this.dxf_color_components[0], this.dxf_color_components[1], this.dxf_color_components[2], this.dxf_color_components[3]);
        }

        #endregion

    }
}
