using SIMULTAN.Data.Assets;
using SIMULTAN.Serializer.DXF.DXFEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// De- and Encoder for files containing MultiLinks in a DXF format.
    /// </summary>
    public class DXFDecoderMultiLinks : DXFDecoder
    {
        /// <summary>
        /// Holds all parsed MultiLink objects.
        /// </summary>
        public List<MultiLink> ParsedLinks { get; }

        /// <summary>
        /// Initializes the decoder.
        /// </summary>
        public DXFDecoderMultiLinks()
            : base()
        {
            this.ParsedLinks = new List<MultiLink>();
        }

        /// <summary>
        /// Creates the appropriate entity according to the codes found in the file.
        /// </summary>
        /// <returns></returns>
        internal override DXFEntity CreateEntity()
        {
            switch (this.FValue)
            {
                case ParamStructTypes.MULTI_LINK:
                    DXFEntity E = new DXFMultiLink();
                    E.Decoder = this;
                    return E;
                default:
                    return base.CreateEntity();
            }
        }

    }
}
