using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper of class <see cref="SimFlowNetworkElement"/>
    /// </summary>
    internal class DXF_FlNetElement : DXFEntity
    {
        #region CLASS MEMBERS

        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public string dxf_Description { get; protected set; }
        public bool dxf_IsValid { get; protected set; }

        public int dxf_RepresentationReference_FileId { get; private set; }
        public ulong dxf_RepresentationReference_GeometryId { get; private set; }

        #endregion

        public DXF_FlNetElement()
        {
            this.dxf_ID = -1;
            this.dxf_Name = string.Empty;
            this.dxf_Description = string.Empty;
            this.dxf_IsValid = false;
            this.dxf_RepresentationReference_FileId = -1;
            this.dxf_RepresentationReference_GeometryId = ulong.MaxValue;
        }

        #region OVERRIDES : Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)FlowNetworkSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)FlowNetworkSaveCode.DESCRIPTION:
                    this.dxf_Description = this.Decoder.FValue;
                    break;
                case (int)FlowNetworkSaveCode.IS_VALID:
                    this.dxf_IsValid = (this.Decoder.IntValue() == 1) ? true : false;
                    break;
                case (int)FlowNetworkSaveCode.GEOM_REP_NE_FILE_INDEX:
                    this.dxf_RepresentationReference_FileId = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.GEOM_REP_NE_GEOM_ID:
                    this.dxf_RepresentationReference_GeometryId = this.Decoder.UlongValue();
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

    }
}
