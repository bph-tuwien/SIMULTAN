using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFFileVersion : DXFEntity
    {
        private ulong parsed_version;
        private long max_calculation_id = -1;

        public DXFFileVersion()
        {
            this.parsed_version = DXFDecoder.NO_FILE_VERSION;
        }

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.COORDS_X:
                    this.parsed_version = this.Decoder.UlongValue();
                    break;
                case (int)ComponentFileMetaInfoSaveCode.MAX_CALCULATION_ID:
                    max_calculation_id = this.Decoder.LongValue();
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        internal override void OnLoaded()
        {
            this.Decoder.CurrentFileVersion = this.parsed_version;

            if (this.max_calculation_id != -1)
            {
                if (parsed_version < 7)
                    this.Decoder.ProjectData.IdGenerator.LoaderMaxId = DXFDecoder.MaxTranslationId;
                else
                    this.Decoder.ProjectData.IdGenerator.LoaderMaxId = this.max_calculation_id;
            }
        }
    }
}
