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

        internal DocumentAsset(AssetManager _manger, long _caller_id, int _path_code_to_asset, string _id)
            : base(_manger, _caller_id, _path_code_to_asset, _id)
        {

        }

        #endregion

        #region PARSING .CTOR

        internal DocumentAsset(AssetManager _manger, List<long> _caller_ids, int _path_code_to_asset, string _id)
            : base(_manger, _caller_ids, _path_code_to_asset, _id)
        {

        }

        #endregion

        #region ToString

        public override void AddToExport(ref StringBuilder _sb)
        {
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.ASSET_DOCU);                              // ASSET_DOCUMENT

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            base.AddToExport(ref _sb);
        }

        #endregion
    }
}
