using SIMULTAN.Data.Assets;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Serializer.DXF.DXFEntities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// DXF Decoder for <see cref="GeoMap"/> files (.gmdxf)
    /// </summary>
    public class DXFDecoderGeoReferences : DXFDecoder
    {
        /// <summary>
        /// Final GeoMap which was stored in the DXF file
        /// </summary>
        public GeoMap Map { get; internal set; }

        /// <summary>
        /// Asset manager which contains the referenced image file
        /// </summary>
        public AssetManager AssetManager { get; private set; }

        /// <inheritdoc />
        public DXFDecoderGeoReferences(ResourceFileEntry mapFile, AssetManager assetManager)
            : base()
        {
            this.Map = new GeoMap(mapFile);
            this.AssetManager = assetManager;
        }

        /// <inheritdoc />
        internal override DXFEntity CreateEntity()
        {
            switch (this.FValue)
            {
                case ParamStructTypes.GEOMAP:
                    DXFGeoReference E = new DXFGeoReference();
                    E.Decoder = this;
                    return E;
                default:
                    return base.CreateEntity();
            }
        }
    }
}
