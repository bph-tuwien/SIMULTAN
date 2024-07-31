using SIMULTAN.Data;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SIMULTAN.Data.SimMath;
using System.Diagnostics;

namespace SIMULTAN.Serializer.CODXF
{
    /// <summary>
    /// Methods for serializing/deserializing <see cref="SimNetwork"/> and related classes in a CODXF file
    /// </summary>
    public static class ComponentDxfIOSimNetworks
    {
        #region Syntax Connector

        /// <summary>
        /// Syntax for a connector
        /// </summary>
        internal static DXFEntityParserElementBase<SimNetworkConnector> ConnectorEntityElement =
            new DXFEntityParserElement<SimNetworkConnector>(ParamStructTypes.SIMNETWORK_CONNECTOR, ParseConnector,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.ENTITY_NAME),
                    new DXFSingleEntryParserElement<long>(SimNetworkSaveCode.SOURCE_PORT),
                    new DXFSingleEntryParserElement<long>(SimNetworkSaveCode.TARGET_PORT),
                    new DXFSingleEntryParserElement<SimColor>(SimNetworkSaveCode.COLOR),
                    new DXFSingleEntryParserElement<int>(SimNetworkSaveCode.GEOM_REP_FILE_KEY),
                    new DXFSingleEntryParserElement<ulong>(SimNetworkSaveCode.GEOM_REP_GEOM_ID),


                    new DXFStructArrayEntryParserElement<(double, double)>((int)SimNetworkSaveCode.CONTROL_POINTS, ParseConnectorControlPoints,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.POSITION_X) ,
                            new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.POSITION_Y),
                        }) { MinVersion = 26 },
                });

        #endregion

        #region Syntax Port

        /// <summary>
        /// Syntax for a port
        /// </summary>
        internal static DXFEntityParserElementBase<SimNetworkPort> PortEntityElement =
            new DXFEntityParserElement<SimNetworkPort>(ParamStructTypes.SIMNETWORK_PORT, ParsePort,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.ENTITY_NAME),
                    new DXFSingleEntryParserElement<PortType>(SimNetworkSaveCode.PORT_TYPE),
                    new DXFSingleEntryParserElement<SimColor>(SimNetworkSaveCode.COLOR),
                    new DXFSingleEntryParserElement<int>(SimNetworkSaveCode.GEOM_REP_FILE_KEY),
                    new DXFSingleEntryParserElement<ulong>(SimNetworkSaveCode.GEOM_REP_GEOM_ID),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.POSITION_X){MinVersion = 26},
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.POSITION_Y){MinVersion = 26}
                });

        #endregion

        #region Syntax Block

        /// <summary>
        /// Syntax for a port
        /// </summary>
        internal static DXFEntityParserElementBase<SimNetworkBlock> BlockEntityElement =
            new DXFComplexEntityParserElement<SimNetworkBlock>(
            new DXFEntityParserElement<SimNetworkBlock>(ParamStructTypes.SIMNETWORK_BLOCK, ParseBlock,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.ENTITY_NAME),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.POSITION_X),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.POSITION_Y),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.WIDTH),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.HEIGHT),
                    new DXFSingleEntryParserElement<SimColor>(SimNetworkSaveCode.COLOR),
                    new DXFSingleEntryParserElement<int>(SimNetworkSaveCode.GEOM_REP_FILE_KEY),
                    new DXFSingleEntryParserElement<ulong>(SimNetworkSaveCode.GEOM_REP_GEOM_ID),
                    new DXFEntitySequenceEntryParserElement<SimNetworkPort>(SimNetworkSaveCode.PORTS,
                        new DXFEntityParserElementBase<SimNetworkPort>[]
                        {
                            PortEntityElement
                        })
                }));


        #endregion

        #region Syntax Network

        /// <summary>
        /// Syntax for a network
        /// </summary>
        internal static DXFEntityParserElementBase<SimNetwork> NetworkEntityElement =
            new DXFComplexEntityParserElement<SimNetwork>(
            new DXFEntityParserElement<SimNetwork>(ParamStructTypes.SIMNETWORK, ParseNetwork,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.ENTITY_NAME),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.POSITION_X),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.POSITION_Y),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.WIDTH),
                    new DXFSingleEntryParserElement<double>(SimNetworkSaveCode.HEIGHT),
                    new DXFSingleEntryParserElement<SimColor>(SimNetworkSaveCode.COLOR),
                    new DXFSingleEntryParserElement<int>(SimNetworkSaveCode.GEOM_REP_FILE_KEY),
                    new DXFSingleEntryParserElement<ulong>(SimNetworkSaveCode.GEOM_REP_GEOM_ID),
                    new DXFSingleEntryParserElement<int>(SimNetworkSaveCode.GEOM_REP_INDEX),
                    new DXFEntitySequenceEntryParserElement<SimNetworkPort>(SimNetworkSaveCode.PORTS,
                        new DXFEntityParserElementBase<SimNetworkPort>[]
                        {
                            PortEntityElement
                        }),
                    new DXFEntitySequenceEntryParserElement<SimNetworkBlock>(SimNetworkSaveCode.BLOCKS,
                        new DXFEntityParserElementBase<SimNetworkBlock>[]
                        {

                            BlockEntityElement
                        }),
                    new DXFEntitySequenceEntryParserElement<SimNetwork>(SimNetworkSaveCode.SUBNETWORKS,
                        new DXFEntityParserElementBase<SimNetwork>[]
                        {
                            new DXFRecursiveEntityParserElement<SimNetwork>(ParamStructTypes.SIMNETWORK, "SimNetworkElement")
                        }),
                    new DXFEntitySequenceEntryParserElement<SimNetworkConnector>(SimNetworkSaveCode.CONNECTORS,
                        new DXFEntityParserElementBase<SimNetworkConnector>[]
                        {
                            ConnectorEntityElement
                        }),
                }))
            {
                Identifier = "SimNetworkElement"
            };

        #endregion

        #region Syntax Section

        /// <summary>
        /// Syntax for a network section
        /// </summary>
        internal static DXFSectionParserElement<SimNetwork> SimNetworkSectionEntityElement =
            new DXFSectionParserElement<SimNetwork>(ParamStructTypes.SIMNETWORK_SECTION,
                new DXFEntityParserElementBase<SimNetwork>[]
                {
                    NetworkEntityElement
                });

        #endregion


        #region Section

        internal static void WriteNetworkSection(IEnumerable<SimNetwork> networks, DXFStreamWriter writer)
        {
            writer.StartSection(ParamStructTypes.SIMNETWORK_SECTION, networks.Count());

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
            var networks = ComponentDxfIOSimNetworks.SimNetworkSectionEntityElement.Parse(reader, info);

            info.ProjectData.SimNetworks.StartLoading();
            foreach (var network in networks)
            {
                info.ProjectData.SimNetworks.Add(network);
            }
            info.ProjectData.SimNetworks.EndLoading();

            info.ProjectData.SimNetworks.RestoreReferences();
        }


        /// <summary>
        /// Attaches events for the blocks after loaded the SimComponentInstnaces
        /// </summary>
        /// <param name="reader">The DXF reader to read from</param>
        /// <param name="info">Info for the parser</param>
        internal static void SubscribeToEvents(DXFStreamReader reader, DXFParserInfo info)
        {
            for (int i = 0; i < info.ProjectData.SimNetworks.Count; i++)
            {
                SubscribeBlockEvents(info.ProjectData.SimNetworks[i]);
            }
        }

        private static void SubscribeBlockEvents(SimNetwork network)
        {
            for (int j = 0; j < network.ContainedElements.Count; j++)
            {
                if (network.ContainedElements[j] is SimNetworkBlock block)
                {
                    block.AttachEvents();
                }
                if (network.ContainedElements[j] is SimNetwork smnw)
                {
                    SubscribeBlockEvents(smnw);
                }
            }
        }

        #endregion

        #region Networks

        internal static void WriteNetwork(SimNetwork network, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SIMNETWORK);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimNetwork));
            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, network.Id.LocalId);
            writer.Write(ParamStructCommonSaveCode.ENTITY_NAME, network.Name);

            writer.Write(SimNetworkSaveCode.POSITION_X, network.Position.X);
            writer.Write(SimNetworkSaveCode.POSITION_Y, network.Position.Y);
            writer.Write(SimNetworkSaveCode.WIDTH, network.Width);
            writer.Write(SimNetworkSaveCode.HEIGHT, network.Height);
            writer.Write(SimNetworkSaveCode.COLOR, network.Color);


            //Geometry
            writer.Write(SimNetworkSaveCode.GEOM_REP_FILE_KEY, network.RepresentationReference.FileId);
            writer.Write(SimNetworkSaveCode.GEOM_REP_GEOM_ID, network.RepresentationReference.GeometryId);
            writer.Write(SimNetworkSaveCode.GEOM_REP_INDEX, network.IndexOfGeometricRepFile);

            writer.WriteEntitySequence(SimNetworkSaveCode.PORTS, network.Ports, WritePort);
            writer.WriteEntitySequence(SimNetworkSaveCode.BLOCKS, network.ContainedElements.OfType<SimNetworkBlock>(), WriteBlock);
            writer.WriteEntitySequence(SimNetworkSaveCode.SUBNETWORKS, network.ContainedElements.OfType<SimNetwork>(), WriteNetwork);
            writer.WriteEntitySequence(SimNetworkSaveCode.CONNECTORS, network.ContainedConnectors, WriteConnector);




            writer.EndComplexEntity();
        }

        private static SimNetwork ParseNetwork(DXFParserResultSet data, DXFParserInfo info)
        {
            long id = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
            string name = data.Get<string>(ParamStructCommonSaveCode.ENTITY_NAME, string.Empty);

            var posX = data.Get<double>(SimNetworkSaveCode.POSITION_X, 0.0);
            var posY = data.Get<double>(SimNetworkSaveCode.POSITION_Y, 0.0);
            var width = data.Get<double>(SimNetworkSaveCode.WIDTH, 200);
            var height = data.Get<double>(SimNetworkSaveCode.HEIGHT, 100);


            var geomFile = data.Get<int>(SimNetworkSaveCode.GEOM_REP_FILE_KEY, -1);
            var geomId = data.Get<ulong>(SimNetworkSaveCode.GEOM_REP_GEOM_ID, 0);
            var geomRepFileIndex = data.Get<int>(SimNetworkSaveCode.GEOM_REP_INDEX, -1);


            var ports = data.Get<SimNetworkPort[]>(SimNetworkSaveCode.PORTS, new SimNetworkPort[0]);
            var blocks = data.Get<SimNetworkBlock[]>(SimNetworkSaveCode.BLOCKS, new SimNetworkBlock[0]);
            var subNetworks = data.Get<SimNetwork[]>(SimNetworkSaveCode.SUBNETWORKS, new SimNetwork[0]);
            var connectors = data.Get<SimNetworkConnector[]>(SimNetworkSaveCode.CONNECTORS, new SimNetworkConnector[0]);

            var color = data.Get<SimColor>(SimNetworkSaveCode.COLOR, SimColors.Purple);


            try
            {
                return new SimNetwork(new SimId(info.GlobalId, id),
                    name, new SimPoint(posX, posY),
                    ports.Where(x => x != null),
                    Enumerable.Concat<BaseSimNetworkElement>(subNetworks, blocks).Where(x => x != null),
                    connectors.Where(x => x != null), color)
                {
                    RepresentationReference = new Data.GeometricReference(geomFile, geomId),
                    IndexOfGeometricRepFile = geomRepFileIndex,
                    Width = width,
                    Height = height,
                };
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimNetwork with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    id, name, e.Message, e.StackTrace
                    ));
                return null;
            }
        }

        #endregion

        #region Port

        internal static void WritePort(SimNetworkPort port, DXFStreamWriter writer)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SIMNETWORK_PORT);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimNetworkPort));
            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, port.Id.LocalId);
            writer.Write(ParamStructCommonSaveCode.ENTITY_NAME, port.Name);

            writer.Write(SimNetworkSaveCode.PORT_TYPE, port.PortType);
            writer.Write(SimNetworkSaveCode.COLOR, port.Color);

            //Relative position to the position of the parent
            writer.Write(SimNetworkSaveCode.POSITION_X, port.Position.X);
            writer.Write(SimNetworkSaveCode.POSITION_Y, port.Position.Y);

            //Geometry
            writer.Write(SimNetworkSaveCode.GEOM_REP_FILE_KEY, port.RepresentationReference.FileId);
            writer.Write(SimNetworkSaveCode.GEOM_REP_GEOM_ID, port.RepresentationReference.GeometryId);

        }

        private static SimNetworkPort ParsePort(DXFParserResultSet data, DXFParserInfo info)
        {
            long id = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
            string name = data.Get<string>(ParamStructCommonSaveCode.ENTITY_NAME, string.Empty);
            var portType = data.Get<PortType>(SimNetworkSaveCode.PORT_TYPE, PortType.Input);


            var posX = data.Get<double>(SimNetworkSaveCode.POSITION_X, double.NaN);
            var posY = data.Get<double>(SimNetworkSaveCode.POSITION_Y, double.NaN);

            var geomFile = data.Get<int>(SimNetworkSaveCode.GEOM_REP_FILE_KEY, -1);
            var geomId = data.Get<ulong>(SimNetworkSaveCode.GEOM_REP_GEOM_ID, 0);
            var color = data.Get<SimColor>(SimNetworkSaveCode.COLOR, SimColors.Purple);

            SimPoint position = default(SimPoint);
            if (!double.IsNaN(posX) && !double.IsNaN(posY))
            {
                position = new SimPoint(posX, posY);
            }
            try
            {
                return new SimNetworkPort(name, new SimId(info.GlobalId, id), portType, color, position)
                {
                    RepresentationReference = new GeometricReference(geomFile, geomId)
                };
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimNetworkPort with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    id, name, e.Message, e.StackTrace
                    ));
                return null;
            }
        }

        #endregion

        #region Connector

        internal static void WriteConnector(SimNetworkConnector connector, DXFStreamWriter writer)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SIMNETWORK_CONNECTOR);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimNetworkConnector));
            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, connector.Id.LocalId);
            writer.Write(ParamStructCommonSaveCode.ENTITY_NAME, connector.Name);
            writer.Write(SimNetworkSaveCode.SOURCE_PORT, connector.Source.Id.LocalId);
            writer.Write(SimNetworkSaveCode.TARGET_PORT, connector.Target.Id.LocalId);
            writer.Write(SimNetworkSaveCode.COLOR, connector.Color);

            //Geometry
            writer.Write(SimNetworkSaveCode.GEOM_REP_FILE_KEY, connector.RepresentationReference.FileId);
            writer.Write(SimNetworkSaveCode.GEOM_REP_GEOM_ID, connector.RepresentationReference.GeometryId);

            //Control points
            writer.WriteArray(SimNetworkSaveCode.CONTROL_POINTS, connector.Points,
                 (entry, iwriter) =>
                 {
                     iwriter.Write(SimNetworkSaveCode.POSITION_X, entry.X);
                     iwriter.Write(SimNetworkSaveCode.POSITION_Y, entry.Y);
                 });

        }

        private static SimNetworkConnector ParseConnector(DXFParserResultSet data, DXFParserInfo info)
        {
            long id = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
            string name = data.Get<string>(ParamStructCommonSaveCode.ENTITY_NAME, string.Empty);
            long sourcePort = data.Get<long>(SimNetworkSaveCode.SOURCE_PORT, 0);
            long targetPart = data.Get<long>(SimNetworkSaveCode.TARGET_PORT, 0);

            var geomFile = data.Get<int>(SimNetworkSaveCode.GEOM_REP_FILE_KEY, -1);
            var geomId = data.Get<ulong>(SimNetworkSaveCode.GEOM_REP_GEOM_ID, 0);
            var color = data.Get<SimColor>(SimNetworkSaveCode.COLOR, SimColors.Purple);

            var controlPoints = data.Get<(double, double)[]>(SimNetworkSaveCode.CONTROL_POINTS, new (double, double)[0]);
            var listPoints = new List<SimPoint>();
            controlPoints.ForEach(p => listPoints.Add(new SimPoint(p.Item1, p.Item2)));
            try
            {
                return new SimNetworkConnector(name, new SimId(info.GlobalId, id),
                    new SimId(info.GlobalId, sourcePort),
                    new SimId(info.GlobalId, targetPart), color, listPoints)
                {
                    RepresentationReference = new GeometricReference(geomFile, geomId)
                };
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimNetworkConnector with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    id, name, e.Message, e.StackTrace
                    ));
                return null;
            }
        }

        private static (double x, double y) ParseConnectorControlPoints(
                DXFParserResultSet data, DXFParserInfo info)
        {

            var x = data.Get<double>(SimNetworkSaveCode.POSITION_X, 0);
            var y = data.Get<double>(SimNetworkSaveCode.POSITION_Y, 0);
            return (x, y);
        }

        #endregion

        #region Blocks

        internal static void WriteBlock(SimNetworkBlock block, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SIMNETWORK_BLOCK);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimNetworkBlock));
            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, block.Id.LocalId);
            writer.Write(ParamStructCommonSaveCode.ENTITY_NAME, block.Name);
            writer.Write(SimNetworkSaveCode.COLOR, block.Color);

            writer.Write(SimNetworkSaveCode.POSITION_X, block.Position.X);
            writer.Write(SimNetworkSaveCode.POSITION_Y, block.Position.Y);
            writer.Write(SimNetworkSaveCode.WIDTH, block.Width);
            writer.Write(SimNetworkSaveCode.HEIGHT, block.Height);

            writer.WriteEntitySequence(SimNetworkSaveCode.PORTS, block.Ports, WritePort);


            //Geometry
            writer.Write(SimNetworkSaveCode.GEOM_REP_FILE_KEY, block.RepresentationReference.FileId);
            writer.Write(SimNetworkSaveCode.GEOM_REP_GEOM_ID, block.RepresentationReference.GeometryId);

            writer.EndComplexEntity();
        }

        private static SimNetworkBlock ParseBlock(DXFParserResultSet data, DXFParserInfo info)
        {
            long id = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
            string name = data.Get<string>(ParamStructCommonSaveCode.ENTITY_NAME, string.Empty);

            var posX = data.Get<double>(SimNetworkSaveCode.POSITION_X, 0.0);
            var posY = data.Get<double>(SimNetworkSaveCode.POSITION_Y, 0.0);
            var width = data.Get<double>(SimNetworkSaveCode.WIDTH, 200);
            var height = data.Get<double>(SimNetworkSaveCode.HEIGHT, 100);

            var ports = data.Get<SimNetworkPort[]>(SimNetworkSaveCode.PORTS, new SimNetworkPort[0]);


            var geomFile = data.Get<int>(SimNetworkSaveCode.GEOM_REP_FILE_KEY, -1);
            var geomId = data.Get<ulong>(SimNetworkSaveCode.GEOM_REP_GEOM_ID, 0);

            var color = data.Get<SimColor>(SimNetworkSaveCode.COLOR, SimColors.Purple);

            try
            {
                return new SimNetworkBlock(name, new SimPoint(posX, posY), new SimId(info.GlobalId, id), ports.Where(x => x != null), color)
                {
                    RepresentationReference = new GeometricReference(geomFile, geomId),
                    Width = width,
                    Height = height,
                };
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimNetworkBlock with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    id, name, e.Message, e.StackTrace
                    ));
                return null;
            }
        }

        #endregion
    }
}
