using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    ///  wrapper of classes <see cref="SimFlowNetworkEdge"/>
    /// </summary>
    internal class DXF_FlNetEdge_Preparsed
    {
        internal Guid dxf_LOCATION { get; private set; }
        internal long dxf_ID { get; private set; }
        internal string dxf_Name { get; private set; }
        internal string dxf_Description { get; private set; }
        internal bool dxf_IsValid { get; private set; }
        public int dxf_RepresentationReference_FileId { get; private set; }
        public ulong dxf_RepresentationReference_GeometryId { get; private set; }
        internal long dxf_StartNode_ID { get; private set; }
        internal long dxf_EndNode_ID { get; private set; }

        internal DXF_FlNetEdge_Preparsed(Guid _location, long _id, string _name, string _description,
                                         bool _is_valid, int _repres_ref_file_id, ulong _repres_ref_geom_id,
                                         long _start_node_id, long _end_node_id)
        {
            this.dxf_LOCATION = _location;
            this.dxf_ID = _id;
            this.dxf_Name = _name;
            this.dxf_Description = _description;
            this.dxf_IsValid = _is_valid;
            this.dxf_RepresentationReference_FileId = _repres_ref_file_id;
            this.dxf_RepresentationReference_GeometryId = _repres_ref_geom_id;
            this.dxf_StartNode_ID = _start_node_id;
            this.dxf_EndNode_ID = _end_node_id;
        }
    }
}
