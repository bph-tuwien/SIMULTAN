using SIMULTAN.Data.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFMultiLink : DXFEntity
    {
        private Dictionary<int, string> dxf_Representations;
        private int dxf_nr_Representations;

        private int current_key;
        private bool current_key_set;
        private string current_path;

        public DXFMultiLink()
        {
            this.dxf_Representations = new Dictionary<int, string>();
            this.dxf_nr_Representations = 0;

            this.current_key = -1;
            this.current_key_set = false;
            this.current_path = null;
        }

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.NUMBER_OF:
                    this.dxf_nr_Representations = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.COORDS_X:
                    current_key = this.Decoder.IntValue();
                    current_key_set = true;
                    this.SetNextEntry();
                    break;
                case (int)ParamStructCommonSaveCode.COORDS_Y:
                    current_path = this.Decoder.FValue;
                    this.SetNextEntry();
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        private void SetNextEntry()
        {
            if (this.current_key_set && !string.IsNullOrEmpty(this.current_path) &&
                this.dxf_nr_Representations > this.dxf_Representations.Count)
            {
                this.dxf_Representations.Add(this.current_key, this.current_path);

                // reset
                this.current_key = -1;
                this.current_key_set = false;
                this.current_path = null;
            }
        }

        internal override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder is DXFDecoderMultiLinks)
            {
                //// TMP - REMOVE IMMEDIATELY
                //Dictionary<int, string> tmp = new Dictionary<int, string>();
                //foreach (var item in this.dxf_Representations)
                //{
                //    tmp.Add(-904356, item.Value);
                //}
                (this.Decoder as DXFDecoderMultiLinks).ParsedLinks.Add(new MultiLink(this.dxf_Representations));
                //(this.Decoder as DXFDecoderMultiLinks).ParsedLinks.Add(new MultiLink(tmp));
            }
        }
    }
}
