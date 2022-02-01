using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF.DXFEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    public class DXFDecoderMeta : DXFDecoder
    {
        public HierarchicProjectMetaData ParsedMetaData { get; internal set; }

        internal override DXFEntity CreateEntity()
        {
            switch (this.FValue)
            {
                case ParamStructTypes.PROJECT_METADATA:
                    DXFEntity E = new DXFHierarchicProjectMetaData();
                    E.Decoder = this;
                    return E;
                default:
                    return base.CreateEntity();
            }
        }

    }
}
