using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Serializer.CODXF
{
    /// <summary>
    /// Provides methods for serializing networks to a component file
    /// </summary>
    public static class ComponentDxfIONetworks
    {
        #region Syntax

        /// <summary>
        /// Syntax for a network node
        /// </summary>
        internal static DXFEntityParserElementBase<SimFlowNetworkNode> NetworkNodeEntityElement =
            new DXFEntityParserElement<SimFlowNetworkNode>(ParamStructTypes.FLOWNETWORK_NODE, ParseNetworkNode,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.NAME),
                    new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.DESCRIPTION),
                    new DXFSingleEntryParserElement<bool>(FlowNetworkSaveCode.IS_VALID),
                    new DXFSingleEntryParserElement<int>(FlowNetworkSaveCode.GEOM_REP_FILE_KEY),
                    new DXFSingleEntryParserElement<ulong>(FlowNetworkSaveCode.GEOM_REP_GEOM_ID),
                    new DXFSingleEntryParserElement<double>(FlowNetworkSaveCode.POSITION_X),
                    new DXFSingleEntryParserElement<double>(FlowNetworkSaveCode.POSITION_Y),
                    new DXFStructArrayEntryParserElement<SimFlowNetworkCalcRule?>(FlowNetworkSaveCode.CALC_RULES, ParseCalculationRule,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS),
                            new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT),
                            new DXFSingleEntryParserElement<SimFlowNetworkCalcDirection>(FlowNetworkSaveCode.CALC_RULE_DIRECTION),
                            new DXFSingleEntryParserElement<SimFlowNetworkOperator>(FlowNetworkSaveCode.CALC_RULE_OPERATOR),
                        })
                });

        /// <summary>
        /// Syntax for a network edge
        /// </summary>
        internal static DXFEntityParserElementBase<SimFlowNetworkEdge> NetworkEdgeEntityElement =
            new DXFEntityParserElement<SimFlowNetworkEdge>(ParamStructTypes.FLOWNETWORK_EDGE, ParseNetworkEdge,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.NAME),
                    new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.DESCRIPTION),
                    new DXFSingleEntryParserElement<bool>(FlowNetworkSaveCode.IS_VALID),
                    new DXFSingleEntryParserElement<int>(FlowNetworkSaveCode.GEOM_REP_FILE_KEY),
                    new DXFSingleEntryParserElement<ulong>(FlowNetworkSaveCode.GEOM_REP_GEOM_ID),

                    new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.START_NODE_LOCALID) { MaxVersion = 11 },
                    new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.END_NODE_LOCALID) { MaxVersion = 11 },

                    new DXFSingleEntryParserElement<long>(FlowNetworkSaveCode.START_NODE_LOCALID) { MinVersion = 12 },
                    new DXFSingleEntryParserElement<Guid>(FlowNetworkSaveCode.START_NODE_GLOBALID) { MinVersion = 12 },
                    new DXFSingleEntryParserElement<long>(FlowNetworkSaveCode.END_NODE_LOCALID) { MinVersion = 12 },
                    new DXFSingleEntryParserElement<Guid>(FlowNetworkSaveCode.END_NODE_GLOBALID) { MinVersion = 12 },
                });

        /// <summary>
        /// Syntax for a network
        /// </summary>
        internal static DXFEntityParserElementBase<SimFlowNetwork> NetworkEntityElement =
            new DXFComplexEntityParserElement<SimFlowNetwork>(
            new DXFEntityParserElement<SimFlowNetwork>(ParamStructTypes.FLOWNETWORK, ParseNetwork,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.NAME),
                    new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.DESCRIPTION),
                    new DXFSingleEntryParserElement<bool>(FlowNetworkSaveCode.IS_VALID),
                    new DXFSingleEntryParserElement<int>(FlowNetworkSaveCode.GEOM_REP_FILE_KEY),
                    new DXFSingleEntryParserElement<ulong>(FlowNetworkSaveCode.GEOM_REP_GEOM_ID),
                    new DXFSingleEntryParserElement<double>(FlowNetworkSaveCode.POSITION_X),
                    new DXFSingleEntryParserElement<double>(FlowNetworkSaveCode.POSITION_Y),
                    new DXFStructArrayEntryParserElement<SimFlowNetworkCalcRule?>(FlowNetworkSaveCode.CALC_RULES, ParseCalculationRule,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS),
                            new DXFSingleEntryParserElement<string>(FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT),
                            new DXFSingleEntryParserElement<SimFlowNetworkCalcDirection>(FlowNetworkSaveCode.CALC_RULE_DIRECTION),
                            new DXFSingleEntryParserElement<SimFlowNetworkOperator>(FlowNetworkSaveCode.CALC_RULE_OPERATOR),
                        }),
                    new DXFSingleEntryParserElement<SimUserRole>(FlowNetworkSaveCode.MANAGER),
                    new DXFSingleEntryParserElement<int>(FlowNetworkSaveCode.GEOM_REP),

                    new DXFEntitySequenceEntryParserElement<SimFlowNetworkNode>(FlowNetworkSaveCode.CONTAINED_NODES, NetworkNodeEntityElement),
                    new DXFEntitySequenceEntryParserElement<SimFlowNetwork>(FlowNetworkSaveCode.CONTAINED_NETWORKS,
                        new DXFRecursiveEntityParserElement<SimFlowNetwork>(ParamStructTypes.FLOWNETWORK, "FlowNetwork")),
                    new DXFEntitySequenceEntryParserElement<SimFlowNetworkEdge>(FlowNetworkSaveCode.CONTAINED_EDGES, NetworkEdgeEntityElement),

                    new DXFSingleEntryParserElement<long>(FlowNetworkSaveCode.NODE_SOURCE),
                    new DXFSingleEntryParserElement<long>(FlowNetworkSaveCode.NODE_SINK),
                    new DXFSingleEntryParserElement<bool>(FlowNetworkSaveCode.IS_DIRECTED)
                }))
            {
                Identifier = "FlowNetwork"
            };

        /// <summary>
        /// Syntax for a network section
        /// </summary>
        internal static DXFSectionParserElement<SimFlowNetwork> NetworkSectionEntityElement =
            new DXFSectionParserElement<SimFlowNetwork>(ParamStructTypes.NETWORK_SECTION,
                new DXFEntityParserElementBase<SimFlowNetwork>[]
                {
                    NetworkEntityElement
                });

        #endregion

        /// <summary>
        /// Writes a network section to the DXF stream
        /// </summary>
        /// <param name="networks">The networks to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteNetworkSection(IEnumerable<SimFlowNetwork> networks, DXFStreamWriter writer)
        {
            writer.StartSection(ParamStructTypes.NETWORK_SECTION);

            foreach (var network in networks)
            {
                WriteNetwork(network, writer);
            }

            writer.EndSection();
        }
        /// <summary>
        /// Reads a network section. The results are stored in <see cref="DXFParserInfo.ProjectData"/>
        /// </summary>
        /// <param name="reader">The DXF reader to read from</param>
        /// <param name="info">Info for the parser</param>
        internal static void ReadNetworkSection(DXFStreamReader reader, DXFParserInfo info)
        {
            var networks = ComponentDxfIONetworks.NetworkSectionEntityElement.Parse(reader, info);

            foreach (var network in networks)
            {
                info.ProjectData.NetworkManager.NetworkRecord.Add(network);
            }
        }

        /// <summary>
        /// Writes a network to the DXF stream
        /// </summary>
        /// <param name="network">The network to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteNetwork(SimFlowNetwork network, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.FLOWNETWORK);
            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, network.ID.LocalId);
            writer.Write(FlowNetworkSaveCode.NAME, network.Name);
            writer.Write(FlowNetworkSaveCode.DESCRIPTION, network.Description);
            writer.Write(FlowNetworkSaveCode.IS_VALID, network.IsValid);
            writer.Write(FlowNetworkSaveCode.GEOM_REP_FILE_KEY, network.RepresentationReference.FileId);
            writer.Write(FlowNetworkSaveCode.GEOM_REP_GEOM_ID, network.RepresentationReference.GeometryId);
            writer.Write(FlowNetworkSaveCode.POSITION_X, network.Position.X);
            writer.Write(FlowNetworkSaveCode.POSITION_Y, network.Position.Y);
            writer.WriteArray(FlowNetworkSaveCode.CALC_RULES, network.CalculationRules, WriterCalculationRule);
            writer.Write(FlowNetworkSaveCode.MANAGER, network.Manager);
            writer.Write(FlowNetworkSaveCode.GEOM_REP, network.IndexOfGeometricRepFile);
            writer.WriteEntitySequence(FlowNetworkSaveCode.CONTAINED_NODES, network.ContainedNodes.Values, WriteNetworkNode);
            writer.WriteEntitySequence(FlowNetworkSaveCode.CONTAINED_NETWORKS, network.ContainedFlowNetworks.Values, WriteNetwork);
            writer.WriteEntitySequence(FlowNetworkSaveCode.CONTAINED_EDGES, network.ContainedEdges.Values, WriteNetworkEdge);
            writer.Write(FlowNetworkSaveCode.NODE_SOURCE, network.NodeStart_ID);
            writer.Write(FlowNetworkSaveCode.NODE_SINK, network.NodeEnd_ID);
            writer.Write(FlowNetworkSaveCode.IS_DIRECTED, network.IsDirected);

            writer.EndComplexEntity();

        }

        private static SimFlowNetwork ParseNetwork(DXFParserResultSet data, DXFParserInfo info)
        {
            var id = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
            var name = data.Get<string>(FlowNetworkSaveCode.NAME, string.Empty);
            var description = data.Get<string>(FlowNetworkSaveCode.DESCRIPTION, string.Empty);
            var isValid = data.Get<bool>(FlowNetworkSaveCode.IS_VALID, true);
            var geomFile = data.Get<int>(FlowNetworkSaveCode.GEOM_REP_FILE_KEY, -1);
            var geomId = data.Get<ulong>(FlowNetworkSaveCode.GEOM_REP_GEOM_ID, 0);
            var posX = data.Get<double>(FlowNetworkSaveCode.POSITION_X, 0.0);
            var posY = data.Get<double>(FlowNetworkSaveCode.POSITION_Y, 0.0);
            var calcRules = data.Get<SimFlowNetworkCalcRule?[]>(FlowNetworkSaveCode.CALC_RULES, new SimFlowNetworkCalcRule?[] { });

            var containedNodes = data.Get<SimFlowNetworkNode[]>(FlowNetworkSaveCode.CONTAINED_NODES, new SimFlowNetworkNode[] { });
            var containedEdges = data.Get<SimFlowNetworkEdge[]>(FlowNetworkSaveCode.CONTAINED_EDGES, new SimFlowNetworkEdge[] { });
            var containedNetworks = data.Get<SimFlowNetwork[]>(FlowNetworkSaveCode.CONTAINED_NETWORKS, new SimFlowNetwork[] { });

            var manager = data.Get<SimUserRole>(FlowNetworkSaveCode.MANAGER, SimUserRole.GUEST);
            int geomRepFile = data.Get<int>(FlowNetworkSaveCode.GEOM_REP, -1);

            var startNode = data.Get<long>(FlowNetworkSaveCode.NODE_SOURCE, -1);
            var endNode = data.Get<long>(FlowNetworkSaveCode.NODE_SINK, -1);
            bool isDirected = data.Get<bool>(FlowNetworkSaveCode.IS_DIRECTED, true);

            try
            {
                var network = new SimFlowNetwork(info.GlobalId, id, name, description, isValid,
                    new Point(posX, posY), manager, geomRepFile, containedNodes, containedEdges, containedNetworks,
                    startNode, endNode, isDirected, calcRules.Where(x => x.HasValue).Select(x => x.Value))
                {
                    RepresentationReference = new Data.GeometricReference(geomFile, geomId)
                };
                return network;
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimFlowNetwork with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    id, name, e.Message, e.StackTrace
                    ));
                return null;
            }
        }

        /// <summary>
        /// Writes a network node to the DXF stream
        /// </summary>
        /// <param name="node">The network node to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteNetworkNode(SimFlowNetworkNode node, DXFStreamWriter writer)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.FLOWNETWORK_NODE);
            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, node.ID.LocalId);
            writer.Write(FlowNetworkSaveCode.NAME, node.Name);
            writer.Write(FlowNetworkSaveCode.DESCRIPTION, node.Description);
            writer.Write(FlowNetworkSaveCode.IS_VALID, node.IsValid);
            writer.Write(FlowNetworkSaveCode.GEOM_REP_FILE_KEY, node.RepresentationReference.FileId);
            writer.Write(FlowNetworkSaveCode.GEOM_REP_GEOM_ID, node.RepresentationReference.GeometryId);
            writer.Write(FlowNetworkSaveCode.POSITION_X, node.Position.X);
            writer.Write(FlowNetworkSaveCode.POSITION_Y, node.Position.Y);
            writer.WriteArray(FlowNetworkSaveCode.CALC_RULES, node.CalculationRules, WriterCalculationRule);
        }

        private static SimFlowNetworkNode ParseNetworkNode(DXFParserResultSet data, DXFParserInfo info)
        {
            var id = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
            var name = data.Get<string>(FlowNetworkSaveCode.NAME, string.Empty);
            var description = data.Get<string>(FlowNetworkSaveCode.DESCRIPTION, string.Empty);
            var isValid = data.Get<bool>(FlowNetworkSaveCode.IS_VALID, true);
            var geomFile = data.Get<int>(FlowNetworkSaveCode.GEOM_REP_FILE_KEY, -1);
            var geomId = data.Get<ulong>(FlowNetworkSaveCode.GEOM_REP_GEOM_ID, 0);
            var posX = data.Get<double>(FlowNetworkSaveCode.POSITION_X, 0.0);
            var posY = data.Get<double>(FlowNetworkSaveCode.POSITION_Y, 0.0);
            var calcRules = data.Get<SimFlowNetworkCalcRule?[]>(FlowNetworkSaveCode.CALC_RULES, new SimFlowNetworkCalcRule?[] { });

            try
            {
                return new SimFlowNetworkNode(info.GlobalId, id, name, description, isValid,
                    new System.Windows.Point(posX, posY),
                    calcRules.Where(x => x.HasValue).Select(x => x.Value))
                {
                    RepresentationReference = new Data.GeometricReference(geomFile, geomId)
                };
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimFlowNetworkNode with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    id, name, e.Message, e.StackTrace
                    ));
                return null;
            }
        }

        /// <summary>
        /// Writes a network edge to the DXF stream
        /// </summary>
        /// <param name="edge">The network edge to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteNetworkEdge(SimFlowNetworkEdge edge, DXFStreamWriter writer)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.FLOWNETWORK_EDGE);
            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, edge.ID.LocalId);
            writer.Write(FlowNetworkSaveCode.NAME, edge.Name);
            writer.Write(FlowNetworkSaveCode.DESCRIPTION, edge.Description);
            writer.Write(FlowNetworkSaveCode.IS_VALID, edge.IsValid);
            writer.Write(FlowNetworkSaveCode.GEOM_REP_FILE_KEY, edge.RepresentationReference.FileId);
            writer.Write(FlowNetworkSaveCode.GEOM_REP_GEOM_ID, edge.RepresentationReference.GeometryId);
            writer.Write(FlowNetworkSaveCode.START_NODE_LOCALID, edge.Start.ID.LocalId);
            writer.Write(FlowNetworkSaveCode.START_NODE_GLOBALID, Guid.Empty);
            writer.Write(FlowNetworkSaveCode.END_NODE_LOCALID, edge.End.ID.LocalId);
            writer.Write(FlowNetworkSaveCode.END_NODE_GLOBALID, Guid.Empty);
        }

        private static SimFlowNetworkEdge ParseNetworkEdge(DXFParserResultSet data, DXFParserInfo info)
        {
            var id = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
            var name = data.Get<string>(FlowNetworkSaveCode.NAME, string.Empty);
            var description = data.Get<string>(FlowNetworkSaveCode.DESCRIPTION, string.Empty);
            var isValid = data.Get<bool>(FlowNetworkSaveCode.IS_VALID, true);
            var geomFile = data.Get<int>(FlowNetworkSaveCode.GEOM_REP_FILE_KEY, -1);
            var geomId = data.Get<ulong>(FlowNetworkSaveCode.GEOM_REP_GEOM_ID, 0);

            long startId = -1, endId = -1;

            if (info.FileVersion >= 12)
            {
                startId = data.Get<long>(FlowNetworkSaveCode.START_NODE_LOCALID, -1);
                endId = data.Get<long>(FlowNetworkSaveCode.END_NODE_LOCALID, -1);
            }
            else
            {
                startId = SimObjectId.FromString(data.Get<string>(FlowNetworkSaveCode.START_NODE_LOCALID, "-1")).local;
                endId = SimObjectId.FromString(data.Get<string>(FlowNetworkSaveCode.END_NODE_LOCALID, "-1")).local;
            }

            if (startId == -1)
            {
                info.Log(string.Format("Failed to parse StartNode Id for SimFlowNetworkEdge with Id={0}, Name=\"{1}\".\nId Text: {2}",
                    id, name, data.Get<string>(FlowNetworkSaveCode.START_NODE_LOCALID, "-1")
                    ));
                return null;
            }
            if (endId == -1)
            {
                info.Log(string.Format("Failed to parse EndNode Id for SimFlowNetworkEdge with Id={0}, Name=\"{1}\".\nId Text: {2}",
                    id, name, data.Get<string>(FlowNetworkSaveCode.END_NODE_LOCALID, "-1")
                    ));
                return null;
            }

            try
            {
                return new SimFlowNetworkEdge(info.GlobalId, id, name, description, isValid, 
                    startId, endId)
                {
                    RepresentationReference = new Data.GeometricReference(geomFile, geomId)
                };
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimFlowNetworkEdge with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    id, name, e.Message, e.StackTrace
                    ));
                return null;
            }
        }

        /// <summary>
        /// Writes a calculation rule to the DXF stream
        /// </summary>
        /// <param name="rule">The calculation rule to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriterCalculationRule(SimFlowNetworkCalcRule rule, DXFStreamWriter writer)
        {
            writer.Write(FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS, rule.Suffix_Operands);
            writer.Write(FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT, rule.Suffix_Result);
            writer.Write(FlowNetworkSaveCode.CALC_RULE_DIRECTION, rule.Direction);
            writer.Write(FlowNetworkSaveCode.CALC_RULE_OPERATOR, rule.Operator);
        }

        private static SimFlowNetworkCalcRule? ParseCalculationRule(DXFParserResultSet data, DXFParserInfo info)
        {
            var suffixOperands = data.Get<string>(FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS, string.Empty);
            var suffixResult = data.Get<string>(FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT, string.Empty);
            var direction = data.Get<SimFlowNetworkCalcDirection>(FlowNetworkSaveCode.CALC_RULE_DIRECTION, 
                SimFlowNetworkCalcDirection.Forward);
            var op = data.Get<SimFlowNetworkOperator>(FlowNetworkSaveCode.CALC_RULE_OPERATOR, SimFlowNetworkOperator.Addition);

            try
            {
                return new SimFlowNetworkCalcRule()
                {
                    Operator = op,
                    Direction = direction,
                    Suffix_Operands = suffixOperands,
                    Suffix_Result = suffixResult,
                };
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load FlowNetworkCalculationRule\nException: {2}\nStackTrace:\n{3}",
                    e.Message, e.StackTrace
                    ));
                return null;
            }
        }
    }
}
