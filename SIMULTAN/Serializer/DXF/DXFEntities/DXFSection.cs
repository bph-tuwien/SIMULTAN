using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFSection : DXFEntityContainer
    {
        public override void ReadPoperty()
        {
            if ((this.ENT_Name == null) && (this.Decoder.FCode == (int)ParamStructCommonSaveCode.ENTITY_NAME))
            {
                this.ENT_Name = this.Decoder.FValue;
            }
            switch (this.ENT_Name)
            {
                case ParamStructTypes.ENTITY_SECTION:
                    this.Decoder.FEntities = this;
                    break;
            }
        }
    }
}
