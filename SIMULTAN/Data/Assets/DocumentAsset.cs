using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    public class DocumentAsset : Asset
    {
        #region .CTOR

        internal DocumentAsset(AssetManager _manger, long componentId, int resourceKey, string _id)
            : base(_manger, componentId, resourceKey, _id)
        {

        }

        #endregion

        #region PARSING .CTOR

        internal DocumentAsset(AssetManager _manger, IEnumerable<long> _caller_ids, int _path_code_to_asset, string _id)
            : base(_manger, _caller_ids, _path_code_to_asset, _id)
        {

        }

        #endregion
    }
}
