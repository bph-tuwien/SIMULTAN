using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF.DXFEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    public class DXFDecoderUsers : DXFDecoder
    {
        public List<SimUser> ParsedUsers { get; }

        public DXFDecoderUsers()
            : base()
        {
            this.ParsedUsers = new List<SimUser>();
        }

        internal override DXFEntity CreateEntity()
        {
            switch (this.FValue)
            {
                case ParamStructTypes.USER:
                    DXFEntity E = new DXFUser();
                    E.Decoder = this;
                    return E;
                default:
                    return base.CreateEntity();
            }
        }
    }
}
