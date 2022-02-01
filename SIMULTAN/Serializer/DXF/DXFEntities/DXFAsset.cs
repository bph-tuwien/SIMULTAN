using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper class for asset
    /// </summary>
    internal abstract class DXFAsset : DXFEntity
    {
        #region CLASS MEMBERS

        internal int dxf_PathCodeToAsset;
        internal string dxf_ContainedObjectId;
        internal List<long> dxf_referencing_component_ids;
        internal int dxf_nr_referencing_component_ids;

        #endregion

        protected DXFAsset()
        {
            this.dxf_PathCodeToAsset = -1;
            this.dxf_ContainedObjectId = string.Empty;
            this.dxf_referencing_component_ids = new List<long>();
            this.dxf_nr_referencing_component_ids = 0;
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)AssetSaveCode.PATH_CODE:
                    this.dxf_PathCodeToAsset = this.Decoder.IntValue();
                    break;
                case (int)AssetSaveCode.CONTENT_ID:
                    this.dxf_ContainedObjectId = this.Decoder.FValue;
                    break;
                case (int)AssetSaveCode.REFERENCE_COL:
                    this.dxf_nr_referencing_component_ids = this.Decoder.IntValue();
                    break;
                case (int)AssetSaveCode.REFERENCE:
                    if (this.dxf_nr_referencing_component_ids > this.dxf_referencing_component_ids.Count)
                    {
                        long id = this.Decoder.LongValue();
                        this.dxf_referencing_component_ids.Add(Decoder.TranslateComponentIdV8(id));
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRRIDES: Post-processing

        internal override void OnLoaded()
        {
            base.OnLoaded();
        }

        #endregion

    }

    internal class DXFGeometricAsset : DXFAsset
    {

    }

    internal class DXFDocumentAsset : DXFAsset
    {

    }
}
