using SIMULTAN.Data.Assets;
using SIMULTAN.Data.MultiValues;
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
    /// DXF Decoder for <see cref="SitePlannerProject"/> files (.spdxf)
    /// </summary>
    public class DXFDecoderSitePlanner : DXFDecoder
    {
        /// <summary>
        /// Final GeoMap which was stored in the DXF file
        /// </summary>
        public SitePlannerProject Project { get; internal set; }

        /// <summary>
        /// Asset manager which contains the referenced image file
        /// </summary>
        public AssetManager AssetManager { get; private set; }

        /// <summary>
        /// SitePlannerManager
        /// </summary>
        public SitePlannerManager Manager { get; private set; }

        public SimMultiValueCollection MultiValueCollection { get; private set; }

        /// <inheritdoc />
        public DXFDecoderSitePlanner(ResourceFileEntry mapFile, AssetManager assetManager, SitePlannerManager manager, SimMultiValueCollection multiValueCollection)
            : base()
        {
            this.Project = new SitePlannerProject(mapFile);
            this.AssetManager = assetManager;
            this.Manager = manager;
            this.MultiValueCollection = multiValueCollection;
        }

        /// <inheritdoc />
        internal override DXFEntity CreateEntity()
        {
            switch (this.FValue)
            {
                case ParamStructTypes.SITEPLANNER:
                    DXFSitePlanner E = new DXFSitePlanner();
                    E.Decoder = this;
                    return E;
                default:
                    return base.CreateEntity();
            }
        }
    }
}
