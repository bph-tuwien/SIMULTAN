using SIMULTAN;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer.CSV;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


namespace SIMULTAN.Serializer.SpecializedExporters
{
    /// <summary>
    /// A list of possible problems during the network export
    /// </summary>
    public enum CoolingNetworkErrorReason
    {
        /// <summary>
        /// One or more parameters are missing.
        /// Data: {0} a comma separated list of missing parameters
        /// </summary>
        MissingParameter,
        /// <summary>
        /// The network does not contain nodes/edges
        /// Data: Empty
        /// </summary>
        NetworkEmpty,
        /// <summary>
        /// Some nodes or edges do not have components assigned
        /// Data: Empty
        /// </summary>
        EmptyNodesEdges,
        /// <summary>
        /// Some components are missing summation parameters
        /// Data: {0} a comma separated list of missing parameters
        /// </summary>
        MissingSumParameter,
        /// <summary>
        /// There is no chiller node in the network
        /// Data: Empty
        /// </summary>
        MissingChillerNode,
    }

    /// <summary>
    /// Stores a reason and additional data for an error message
    /// </summary>
    public class CoolingNetworkError
    {
        /// <summary>
        /// The reason
        /// </summary>
        public CoolingNetworkErrorReason Reason { get; }
        /// <summary>
        /// The data for the error message. See <see cref="CoolingNetworkErrorReason"/> for a specification of what the data contains.
        /// </summary>
        public object[] Data { get; }

        /// <summary>
        /// Initializes a new instance of the CoolingNetworkError class
        /// </summary>
        /// <param name="reason">The reason</param>
        /// <param name="data">Data for the reason</param>
        public CoolingNetworkError(CoolingNetworkErrorReason reason, object[] data)
        {
            this.Reason = reason;
            this.Data = data;
        }
    }



    /// <summary>
    /// Exporters for the electrical grid and for the cooling system
    /// </summary>
    public class ExportForSimulation
    {

        #region ErrorHandling ElectricalGrid

        /// <summary>
        /// Gets the required parameters for an cooling system 
        /// </summary>
        private static RequiredExportParameters GetRequiredParametersCooling()
        {
            //Required parameters for the simulationParameter component
            List<string> requiredParameters = new List<string>();
            List<string> requiredEdgeParameters = new List<string>();
            List<string> requiredNodeParameters = new List<string>();

            requiredParameters.Add("endTime");
            requiredParameters.Add("startTime");
            requiredParameters.Add("timeStep");
            requiredParameters.Add("supplyTemperature");
            requiredParameters.Add("roughnessLambdaStart");
            requiredParameters.Add("densityStart");
            requiredParameters.Add("sectionPressureFL_start");
            requiredParameters.Add("sectionPressureRL_start");





            RequiredExportParameters requiredParams = new RequiredExportParameters("Lastprofil", "Anteil", requiredParameters, requiredEdgeParameters, requiredNodeParameters);
            return requiredParams;
        }

        /// <summary>
        /// Returns a list of errors of the errors which should be handled before the Cooling system can be exported
        /// </summary>
        public static List<CoolingNetworkError> CheckCoolingNetworkErrors(SimFlowNetwork network, SimComponent component)
        {
            RequiredExportParameters requiredParameters = GetRequiredParametersCooling();

            List<string> requiredSimParams = CheckSimulationParameters(component, requiredParameters);
            List<CoolingNetworkError> Errors = new List<CoolingNetworkError>();
            List<SimFlowNetworkNode> networkNodes = network.GetNestedNodes();



            string missingParams = string.Join(", ", requiredSimParams.ToArray());
            if (requiredSimParams.Count > 0)
            {
                //Show error message with the missing parameters
                Errors.Add(new CoolingNetworkError(CoolingNetworkErrorReason.MissingParameter, new object[] { missingParams }));
                return Errors;
            }

            if (IfEmptyNetwork(network))
            {
                Errors.Add(new CoolingNetworkError(CoolingNetworkErrorReason.NetworkEmpty, new object[] { }));
                return Errors;
            }
            if (IfEmptyNodesOrEdges(network))
            {
                Errors.Add(new CoolingNetworkError(CoolingNetworkErrorReason.EmptyNodesEdges, new object[] { }));
                return Errors;
            }
            //Checking required node parameters

            List<SimFlowNetworkNode> simNetworkNodes = networkNodes.Where(n => n.Content.Component.Parameters.Any(p => p.Name == requiredParameters.SummarizedParameter)).ToList();
            if (simNetworkNodes.Count == 0)
            {
                Errors.Add(new CoolingNetworkError(CoolingNetworkErrorReason.MissingSumParameter, new object[] { requiredParameters.SummarizedParameter }));
                return Errors;
            }

            //Checking if the network contains any node which corresponds the Kältezentrale (chiller nodes) - THIS SHOULD BE REMOVED IN LATER STAGES !! 
            if (!networkNodes.Any(n => n.Content.Name == "Kältezentrale"))
            {
                Errors.Add(new CoolingNetworkError(CoolingNetworkErrorReason.MissingChillerNode, new object[] { }));
                return Errors;
            }

            return Errors;
        }






        /// <summary>
        /// Checks if the selected parameter for a simulation export has the required parameters
        /// </summary>
        private static List<string> CheckSimulationParameters(SimComponent component, RequiredExportParameters requiredParameters)
        {

            List<string> missingParameters = new List<string>();

            foreach (var sParam in requiredParameters.SimulationParameters)
            {
                if (!component.Parameters.Any(p => p.Name == sParam))
                {
                    missingParameters.Add(sParam);
                }
            }
            return missingParameters;
        }





        private static bool IfEmptyNetwork(SimFlowNetwork network)
        {
            if (network.ContainedNodes.Count == 0 &&
                network.ContainedFlowNetworks.Count == 0 &&
                network.ContainedEdges.Count == 0)
                return true;
            else
                return false;
        }


        private static bool IfEmptyNodesOrEdges(SimFlowNetwork network)
        {
            if (network.GetNestedEdges().Any(e => e.Content == null) ||
                network.GetNestedNodes().Any(e => e.Content == null))
                return true;
            else
                return false;
        }

        #endregion

        /// <summary>
        /// Exporters for cooling system
        /// </summary>
        public static void Export(SimFlowNetwork network, List<SimMultiValueBigTable> bigTables, SimComponent component, DirectoryInfo selectedFolder)
        {
            string folderPath = selectedFolder.FullName;

            if (!string.IsNullOrEmpty(folderPath))
            {
                //Writing the CSV for.... 
                XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
                {
                    Indent = true,
                };

                //Writing the XML which contains the info of the simulation 
                using (XmlWriter writer = XmlWriter.Create(folderPath + "/info_in.xml", xmlWriterSettings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("SimulationData");
                    writer.WriteStartElement("SimulationParameters");
                    foreach (var param in component.Parameters)
                    {
                        if (param.Unit == "datetime")
                        {
                            writer.WriteStartElement(param.Name);
                            writer.WriteAttributeString("Unit", param.Unit);
                            writer.WriteString(param.TextValue);
                            writer.WriteEndElement();
                        }
                        if (param.Name == "timeStep")
                        {
                            writer.WriteStartElement(param.Name);
                            writer.WriteAttributeString("Unit", "minutes");
                            writer.WriteString(param.ValueCurrent.ToString(CultureInfo.InvariantCulture));
                            writer.WriteEndElement();

                        }
                        if (param.Name == "Vorlauftemperatur")
                        {
                            writer.WriteStartElement(param.Name);
                            writer.WriteString(param.ValueCurrent.ToString(CultureInfo.InvariantCulture));
                            writer.WriteEndElement();
                        }
                        if (param.Name == "supplyTemperature")
                        {
                            writer.WriteStartElement(param.Name);
                            writer.WriteString(param.ValueCurrent.ToString(CultureInfo.InvariantCulture));
                            writer.WriteEndElement();
                        }

                        if (param.Name == "roughnessLambdaStart")
                        {
                            writer.WriteStartElement(param.Name);
                            writer.WriteString(param.ValueCurrent.ToString(CultureInfo.InvariantCulture));
                            writer.WriteEndElement();
                        }

                        if (param.Name == "densityStart")
                        {
                            writer.WriteStartElement(param.Name);
                            writer.WriteString(param.ValueCurrent.ToString(CultureInfo.InvariantCulture));
                            writer.WriteEndElement();
                        }
                        if (param.Name == "sectionPressureFL_start")
                        {
                            writer.WriteStartElement(param.Name);
                            writer.WriteString(param.ValueCurrent.ToString(CultureInfo.InvariantCulture));
                            writer.WriteEndElement();
                        }
                        if (param.Name == "sectionPressureRL_start")
                        {
                            writer.WriteStartElement(param.Name);
                            writer.WriteString(param.ValueCurrent.ToString(CultureInfo.InvariantCulture));
                            writer.WriteEndElement();
                        }



                    }
                    writer.WriteStartElement("outputFolder");
                    var xmlstring = component.Parameters.Where(p => p.Name == "outputFolder").FirstOrDefault().TextValue.ToString(); ;
                    //(@"c:\temp").ToString();
                    writer.WriteString(xmlstring);
                    writer.WriteEndElement();



                    writer.WriteStartElement("outputXML");
                    writer.WriteString(component.Parameters.Where(p => p.Name == "outputXML").FirstOrDefault().TextValue.ToString());
                    writer.WriteEndElement();
                    writer.WriteEndElement();



                    writer.WriteStartElement("SimulationFiles");
                    writer.WriteStartElement("Simulation");
                    writer.WriteAttributeString("To", component.Parameters.Where(p => p.Name == "endTime").FirstOrDefault().TextValue);
                    writer.WriteAttributeString("From", component.Parameters.Where(p => p.Name == "startTime").FirstOrDefault().TextValue);

                    if (network != null)
                    {

                        NetworkToCSVExporter.Export(network, true, false, folderPath);

                        writer.WriteStartElement("SimulationFile");
                        writer.WriteAttributeString("Usage", "Vertices");
                        writer.WriteAttributeString("Format", "csv");
                        writer.WriteAttributeString("Path", "vertices" + ".csv");
                        writer.WriteEndElement();

                        writer.WriteStartElement("SimulationFile");
                        writer.WriteAttributeString("Usage", "Edges");
                        writer.WriteAttributeString("Format", "csv");
                        writer.WriteAttributeString("Path", "edges" + ".csv");
                        writer.WriteEndElement();

                        writer.WriteStartElement("SimulationFile");
                        writer.WriteAttributeString("Usage", "Chillers");
                        writer.WriteAttributeString("Format", "csv");
                        writer.WriteAttributeString("Path", "chillers" + ".csv");
                        writer.WriteEndElement();


                        writer.WriteStartElement("SimulationFile");
                        writer.WriteAttributeString("Usage", "LoadProfile");
                        writer.WriteAttributeString("Format", "csv");
                        writer.WriteAttributeString("Path", "LoadProfile" + ".csv");
                        writer.WriteEndElement();


                    }
                    //export aggregated bigtable based on the network: 
                    NetworkDependentAggregatedValueFieldExporter.ExportCoolingLoad(network, "LoadProfile", true, folderPath);

                    if (bigTables != null)
                    {
                        foreach (var table in bigTables)
                        {

                            writer.WriteStartElement("SimulationFile");
                            writer.WriteAttributeString("Usage", table.Name);
                            writer.WriteAttributeString("Format", "csv");
                            writer.WriteAttributeString("Path", (table.Name + ".csv").ToLower());


                            writer.WriteEndElement();

                            BigTableToCSVExporter.Export(table, true, false, folderPath);
                        }
                    }
                    writer.WriteEndElement();

                    //
                    writer.WriteEndElement();

                    //
                    writer.WriteEndElement();
                    writer.WriteEndDocument();

                }
            }


        }
    }
}
