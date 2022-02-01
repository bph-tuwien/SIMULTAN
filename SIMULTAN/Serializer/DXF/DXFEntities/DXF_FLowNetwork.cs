using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXF_FlowNetwork : DXFEntityContainer
    {
        #region CLASS MEMBERS

        // SimFlowNetworkElement
        public long dxf_ID { get; protected set; }
        public string dxf_Name { get; protected set; }
        public string dxf_Description { get; protected set; }
        public long dxf_Content_ID { get; protected set; }
        public bool dxf_IsValid { get; protected set; }

        // SimFlowNetworkNode
        protected Point dxf_Position;

        protected List<SimFlowNetworkCalcRule> dxf_CalculationRules;
        protected int dxf_nr_CalculationRules;
        protected int dxf_nr_CalculationRules_read;
        protected SimFlowNetworkCalcRule dxf_current_rule;

        // FlowNetwork
        private List<SimFlowNetworkNode> dxf_contained_nodes;
        private int dxf_nr_contained_nodes;

        private List<SimFlowNetwork> dxf_contained_networks;
        private int dxf_nr_contained_networks;

        private List<DXF_FlNetEdge_Preparsed> dxf_contained_edges_preparsed;
        private int dxf_nr_contained_edges;

        public SimUserRole dxf_Manager { get; private set; }
        public int dxf_IndexOfGeometryRepFile { get; private set; }
        public long dxf_Node_Start_ID { get; private set; }
        public long dxf_Node_End_ID { get; private set; }
        public bool dxf_IsDirected { get; private set; }

        // parsed encapsulated class
        internal SimFlowNetwork dxf_parsed;

        // for nodes and edges that have their OnLoad method deferred
        List<DXF_FlNetNode> for_deferred_N_AddEntity;
        List<DXF_FlowNetwork> for_deferred_NW_AddEntity;
        List<DXF_FlNetEdge> for_deferred_E_AddEntity;

        #endregion

        #region .CTOR

        public DXF_FlowNetwork()
        {
            // SimFlowNetworkElement
            this.dxf_ID = -1;
            this.dxf_Name = string.Empty;
            this.dxf_Description = string.Empty;
            this.dxf_Content_ID = -1;
            this.dxf_IsValid = false;

            // SimFlowNetworkNode
            this.dxf_Position = new Point(0, 0);

            this.dxf_CalculationRules = new List<SimFlowNetworkCalcRule>();
            this.dxf_nr_CalculationRules = 0;
            this.dxf_nr_CalculationRules_read = 0;
            this.dxf_current_rule = new SimFlowNetworkCalcRule();

            // FlowNetwork
            this.dxf_contained_nodes = new List<SimFlowNetworkNode>();
            this.dxf_nr_contained_nodes = 0;

            this.dxf_contained_networks = new List<SimFlowNetwork>();
            this.dxf_nr_contained_networks = 0;

            this.dxf_contained_edges_preparsed = new List<DXF_FlNetEdge_Preparsed>();
            this.dxf_nr_contained_edges = 0;

            this.dxf_Manager = SimUserRole.ADMINISTRATOR;
            this.dxf_IndexOfGeometryRepFile = -1;
            this.dxf_Node_Start_ID = -1;
            this.dxf_Node_End_ID = -1;

            this.dxf_IsDirected = true;

            // for nodes and edges that have their OnLoad method deferred
            this.for_deferred_N_AddEntity = new List<DXF_FlNetNode>();
            this.for_deferred_NW_AddEntity = new List<DXF_FlowNetwork>();
            this.for_deferred_E_AddEntity = new List<DXF_FlNetEdge>();
        }

        #endregion

        #region OVERRIDES: Processing

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
                case (int)FlowNetworkSaveCode.CONTENT_ID:
                    this.dxf_Content_ID = this.Decoder.LongValue();
                    break;
                case (int)FlowNetworkSaveCode.IS_VALID:
                    this.dxf_IsValid = (this.Decoder.IntValue() == 1) ? true : false;
                    break;
                case (int)FlowNetworkSaveCode.POSITION_X:
                    this.dxf_Position.X = this.Decoder.DoubleValue();
                    break;
                case (int)FlowNetworkSaveCode.POSITION_Y:
                    this.dxf_Position.Y = this.Decoder.DoubleValue();
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULES:
                    this.dxf_nr_CalculationRules = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Suffix_Operands = this.Decoder.FValue;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Suffix_Result = this.Decoder.FValue;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_DIRECTION:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Direction = (this.Decoder.FValue == "1") ? SimFlowNetworkCalcDirection.Forward : SimFlowNetworkCalcDirection.Backward;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_OPERATOR:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Operator = SimFlowNetworkCalcRule.StringToOperator(this.Decoder.FValue);
                        this.dxf_CalculationRules.Add(this.dxf_current_rule);
                        this.dxf_current_rule = new SimFlowNetworkCalcRule();
                        this.dxf_nr_CalculationRules_read++;
                    }
                    break;
                case (int)FlowNetworkSaveCode.MANAGER:
                    this.dxf_Manager = ComponentUtils.StringToComponentManagerType(this.Decoder.FValue);
                    break;
                case (int)FlowNetworkSaveCode.GEOM_REP:
                    this.dxf_IndexOfGeometryRepFile = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CONTAINED_NODES:
                    this.dxf_nr_contained_nodes = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CONTIANED_NETW:
                    this.dxf_nr_contained_networks = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CONTAINED_EDGES:
                    this.dxf_nr_contained_edges = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.NODE_SOURCE:
                    this.dxf_Node_Start_ID = this.Decoder.LongValue();
                    break;
                case (int)FlowNetworkSaveCode.NODE_SINK:
                    this.dxf_Node_End_ID = this.Decoder.LongValue();
                    break;
                case (int)FlowNetworkSaveCode.IS_DIRECTED:
                    this.dxf_IsDirected = (this.Decoder.FValue == "1");
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY, ENT_LOCATION
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }
        #endregion

        #region OVERRIDES: Adding Entities

        internal override bool AddEntity(DXFEntity _e)
        {
            // handle depending on type
            if (_e == null) return false;
            bool add_successful = false;

            DXFComponentSubContainer container = _e as DXFComponentSubContainer;
            if (container != null)
            {
                add_successful = true;
                foreach (DXFEntity sE in container.EC_Entities)
                {
                    DXF_FlNetNode node = sE as DXF_FlNetNode;
                    DXF_FlowNetwork nw = sE as DXF_FlowNetwork;
                    DXF_FlNetEdge edge = sE as DXF_FlNetEdge;
                    if (node != null && nw == null && this.dxf_nr_contained_nodes > this.dxf_contained_nodes.Count)
                    {
                        if (node.dxf_parsed != null)
                        {
                            this.dxf_contained_nodes.Add(node.dxf_parsed);
                            add_successful &= true;
                        }
                        else
                            add_successful = false;
                    }
                    else if (node == null && nw != null && this.dxf_nr_contained_networks > this.dxf_contained_networks.Count)
                    {
                        if (nw.dxf_parsed != null)
                        {
                            // remove from the Factory record first (the record contains only top-level networks)
                            this.Decoder.ProjectData.NetworkManager.RemoveNetworkRegardlessOfLocking(nw.dxf_parsed, false);
                            this.dxf_contained_networks.Add(nw.dxf_parsed);
                            add_successful &= true;
                        }
                        else
                            add_successful = false;
                    }
                    if (edge != null && this.dxf_nr_contained_edges > this.dxf_contained_edges_preparsed.Count)
                    {
                        if (edge.dxf_preparsed != null)
                        {
                            this.dxf_contained_edges_preparsed.Add(edge.dxf_preparsed);
                            add_successful &= true;
                        }
                        else
                            add_successful = false;
                    }
                }
            }

            return add_successful;
        }

        // to be called AFTER the OnLoad method for the deferred entities
        internal override void AddDeferredEntities()
        {
            foreach (DXF_FlNetNode n in this.for_deferred_N_AddEntity)
            {
                if (n != null && n.dxf_parsed != null && this.dxf_nr_contained_nodes > this.dxf_contained_nodes.Count)
                    this.dxf_contained_nodes.Add(n.dxf_parsed);
            }

            foreach (DXF_FlowNetwork nw in this.for_deferred_NW_AddEntity)
            {
                if (nw != null && nw.dxf_parsed != null && this.dxf_nr_contained_networks > this.dxf_contained_networks.Count)
                {
                    // remove from the Factory record first (the record contains only top-level networks)
                    this.Decoder.ProjectData.NetworkManager.RemoveNetworkRegardlessOfLocking(nw.dxf_parsed, false);
                    this.dxf_contained_networks.Add(nw.dxf_parsed);
                }
            }

            foreach (DXF_FlNetEdge e in this.for_deferred_E_AddEntity)
            {
                if (e != null && e.dxf_preparsed != null && this.dxf_nr_contained_edges > this.dxf_contained_edges_preparsed.Count)
                    this.dxf_contained_edges_preparsed.Add(e.dxf_preparsed);
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            // complete the edge parsing...
            List<SimFlowNetworkEdge> contained_edges = new List<SimFlowNetworkEdge>();
            foreach (DXF_FlNetEdge_Preparsed ep in this.dxf_contained_edges_preparsed)
            {
                // look for the start and end nodes
                SimFlowNetworkNode start = this.dxf_contained_nodes.Find(x => x.ID.LocalId == ep.dxf_StartNode_ID);
                if (start == null)
                    start = this.dxf_contained_networks.Find(x => x.ID.LocalId == ep.dxf_StartNode_ID);

                SimFlowNetworkNode end = this.dxf_contained_nodes.Find(x => x.ID.LocalId == ep.dxf_EndNode_ID);
                if (end == null)
                    end = this.dxf_contained_networks.Find(x => x.ID.LocalId == ep.dxf_EndNode_ID);

                if (start != null && end != null)
                {
                    SimFlowNetworkEdge parsed_edge = new SimFlowNetworkEdge(ep.dxf_LOCATION, ep.dxf_ID, ep.dxf_Name, ep.dxf_Description, ep.dxf_IsValid, start, end);
                    parsed_edge.RepresentationReference = new GeometricReference(ep.dxf_RepresentationReference_FileId, ep.dxf_RepresentationReference_GeometryId);
                    contained_edges.Add(parsed_edge);
                }
            }

            //Bugfix: global ids were set to Empty for network elements in some older versions
            if (this.ENT_LOCATION == Guid.Empty)
                this.ENT_LOCATION = this.Decoder.ProjectData.Owner.GlobalID;

            // parse the FlowNetwork
            this.dxf_parsed = this.Decoder.ProjectData.NetworkManager.ReconstructNetwork(
                    this.ENT_LOCATION, this.dxf_ID, this.dxf_Name, this.dxf_Description, this.dxf_IsValid,
                    this.dxf_Position, this.dxf_Manager, this.dxf_IndexOfGeometryRepFile,
                    this.dxf_contained_nodes, contained_edges, this.dxf_contained_networks,
                    this.dxf_Node_Start_ID, this.dxf_Node_End_ID, this.dxf_IsDirected, this.dxf_CalculationRules, true);
        }

        #endregion

        #region OVERRIDES: To String

        public override string ToString()
        {
            string dxfS = "DXF_FlowNetwork ";
            if (!(string.IsNullOrEmpty(this.dxf_Name)))
                dxfS += this.dxf_Name + " ";
            if (!(string.IsNullOrEmpty(this.dxf_Description)))
                dxfS += "(" + this.dxf_Description + ")";

            dxfS += ": ";

            int n0 = this.dxf_contained_nodes.Count();
            int n1 = this.dxf_contained_networks.Count();
            int n2 = this.dxf_contained_edges_preparsed.Count();
            dxfS += "[ " + n0.ToString() + " nodes, " + n1.ToString() + " networks, " + n2.ToString() + " edges ]\n";

            dxfS += "\n";
            return dxfS;
        }

        #endregion

    }
}
