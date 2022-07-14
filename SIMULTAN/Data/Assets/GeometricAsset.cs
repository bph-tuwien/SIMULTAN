using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    public class GeometricAsset : Asset
    {
        #region .CTOR

        internal GeometricAsset(AssetManager _manger, long _caller_id, int _path_code_to_asset, string _id)
            : base(_manger, _caller_id, _path_code_to_asset, _id)
        {

        }

        #endregion

        #region PARSING .CTOR

        internal GeometricAsset(AssetManager _manger, IEnumerable<long> _caller_ids, int _path_code_to_asset, string _id)
            : base(_manger, _caller_ids, _path_code_to_asset, _id)
        {

        }

        #endregion

        #region METHODS: Overrides

        [Obsolete]
        public override object OpenAssetContent()
        {
            // deposit a call to open the geometry viewer with the correct path...
            return null;
        }


        #endregion

    }
}
