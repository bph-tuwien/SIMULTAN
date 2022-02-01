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

        internal GeometricAsset(AssetManager _manger, List<long> _caller_ids, int _path_code_to_asset, string _id)
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

        #region ToString

        public override void AddToExport(ref StringBuilder _sb)
        {
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.ASSET_GEOM);                              // ASSET_GEOMETRIC

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            base.AddToExport(ref _sb);
        }

        #endregion
    }
}
