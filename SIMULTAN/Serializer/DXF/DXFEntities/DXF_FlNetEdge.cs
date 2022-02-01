using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXF_FlNetEdge : DXF_FlNetElement
    {
        #region CLASS MEMBERS

        private Guid dxf_start_node_location;
        private long dxf_start_node_id;
        private Guid dxf_end_node_location;
        private long dxf_end_node_id;

        // create the edge (and save it internally)
        internal DXF_FlNetEdge_Preparsed dxf_preparsed;

        #endregion

        public DXF_FlNetEdge()
        {
            this.dxf_start_node_location = Guid.Empty;
            this.dxf_start_node_id = -1;
            this.dxf_end_node_location = Guid.Empty;
            this.dxf_end_node_id = -1;
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)FlowNetworkSaveCode.START_NODE:
                    var locAndIdS = this.Decoder.GlobalAndLocalValue();
                    if (locAndIdS.local >= 0)
                    {
                        this.dxf_start_node_location = locAndIdS.global;
                        this.dxf_start_node_id = locAndIdS.local;
                    }
                    else
                    {
                        this.dxf_start_node_location = Guid.Empty;
                        this.dxf_start_node_id = this.Decoder.LongValue();
                    }
                    break;
                case (int)FlowNetworkSaveCode.END_NODE:
                    var locAndIdE = this.Decoder.GlobalAndLocalValue();
                    if (locAndIdE.local >= 0)
                    {
                        this.dxf_end_node_location = locAndIdE.global;
                        this.dxf_end_node_id = locAndIdE.local;
                    }
                    else
                    {
                        this.dxf_end_node_location = Guid.Empty;
                        this.dxf_end_node_id = this.Decoder.LongValue();
                    }
                    break;
                default:
                    // DXF_FlNetNode: ENTITY_ID, NAME, DESCRIPTION, CONTENT_ID, IS_VALID
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            //Bugfix: global ids were set to Empty for network elements in some older versions
            if (this.ENT_LOCATION == Guid.Empty)
                this.ENT_LOCATION = this.Decoder.ProjectData.Owner.GlobalID;

            // save the pre-parsed state
            // in order to find the start and end nodes, they need to have been processed already ->
            // full parsing in the FlowNetwork this edge belongs to
            this.dxf_preparsed = new DXF_FlNetEdge_Preparsed(this.ENT_LOCATION, this.dxf_ID, this.dxf_Name, this.dxf_Description,
                                                             this.dxf_IsValid, this.dxf_RepresentationReference_FileId, this.dxf_RepresentationReference_GeometryId,
                                                             this.dxf_start_node_id, this.dxf_end_node_id);

        }

        #endregion
    }
}
