using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimNetworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Serializer.CSV
{
    /// <summary>
    /// Holds functions for exporting the SimNetwork
    /// </summary>
    public static class SimNetworkToCSVExporter
    {
        /// <summary>
        /// Exports the SimNetwork into a .CSV file.
        /// </summary>
        /// <param name="network">The network being exported</param>
        public static void Export(SimNetwork network)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));

            FileInfo info = new FileInfo(network.Name);
            // Configure save file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {

                OverwritePrompt = true,
                FileName = info.Name, // Default file name
                DefaultExt = ".csv", // Default file extension
                Filter = "csv files|*.csv" // Filter files by extension

            };
            // Show save file dialog box
            Nullable<bool> result = new Nullable<bool>();

            result = dlg.ShowDialog();
            var fileName = dlg.FileName;
            // Process save file dialog box results
            if (result.HasValue && result == true)
            {
                ExportElements(network, dlg.FileName);

                int index = fileName.IndexOf(".");
                var fileNameWithoutExtension = fileName.Substring(0, index);
                ExportPorts(network, fileNameWithoutExtension + "_Ports.csv");
                ExportConnectors(network, fileNameWithoutExtension + "_Connectors.csv");
                ExportComponentInstances(network, fileNameWithoutExtension + "_Components.csv");
            }
        }


        private static void ExportElements(SimNetwork network, string filePathAndName)
        {
            var allElements = GetContainedElementsRecursively(network);

            List<List<string>> dataRows = new List<List<string>>();
            var headerRows = new List<string> { "ID", "Name", "Type", "ParentID", "X", "Y" };
            dataRows.Add(headerRows);
            foreach (var element in allElements)
            {
                List<string> row = new List<string>();

                row.Add(element.Id.LocalId.ToString());
                row.Add(element.Name);
                if (element is SimNetwork nw)
                {
                    row.Add("SimNetwork");
                }
                if (element is SimNetworkBlock block)
                {
                    row.Add("SimNetworkBlock");
                }
                if (element.ParentNetwork != null)
                {
                    row.Add(element.ParentNetwork.Id.LocalId.ToString());
                }
                else
                {
                    row.Add("");
                }

                row.Add(element.Position.X.ToString());
                row.Add(element.Position.Y.ToString());

                dataRows.Add(row);
            }


            CSVExporter exporter = new CSVExporter(headerRows.Count, ",");
            exporter.AddMultipleRecords(dataRows);

            // save to file
            exporter.WriteFile(filePathAndName);

        }

        private static void ExportConnectors(SimNetwork network, string filePathAndName)
        {
            var allConnectors = GetConnectorsRecursively(network);



            List<List<string>> dataRows = new List<List<string>>();
            var headerRows = new List<string> { "ID", "Name", "SourceID", "TargetID" };
            dataRows.Add(headerRows);
            foreach (var element in allConnectors)
            {
                List<string> row = new List<string>();
                row.Add(element.Id.LocalId.ToString());
                row.Add(element.Name);
                row.Add(element.Source.Id.LocalId.ToString());
                row.Add(element.Target.Id.LocalId.ToString());


                dataRows.Add(row);
            }

            CSVExporter exporter = new CSVExporter(headerRows.Count, ",");
            exporter.AddMultipleRecords(dataRows);

            // save to file
            exporter.WriteFile(filePathAndName);

        }


        private static void ExportComponentInstances(SimNetwork network, string filePathAndName)
        {
            var headerRows = new List<string>() { "ID", "ComponentName", "NetworkElementID", "NetworkElementType" };

            var portsWithComponents = GetPortsRecusrively(network).Where(t => t.ComponentInstance != null);
            var blocksWIthComponents = GetContainedElementsRecursively(network)
                .Where(t => t is IElementWithComponent element && element.ComponentInstance != null);

            if (blocksWIthComponents.Count() > 0 || portsWithComponents.Count() > 0)
            {
                List<SimComponent> componnets = new List<SimComponent>();

                var portComponents = portsWithComponents.GroupBy(t => new { t.ComponentInstance.Component }).Select(g => g.Key).Select(c => c.Component);
                var blockComponets = blocksWIthComponents.GroupBy(t => new { ((IElementWithComponent)t).ComponentInstance.Component }).Select(g => g.Key).Select(c => c.Component);


                List<string> parameterNames = new List<string>();
                foreach (var group in portComponents)
                {
                    foreach (var param in group.Parameters)
                    {
                        parameterNames.Add(param.Name);
                        headerRows.Add(param.Name);
                    }
                }
                foreach (var group in blockComponets)
                {
                    foreach (var param in group.Parameters)
                    {
                        parameterNames.Add(param.Name);
                        headerRows.Add(param.Name);
                    }
                }



                List<List<string>> dataRows = new List<List<string>>();
                dataRows.Add(headerRows);
                foreach (var item in blocksWIthComponents)
                {
                    List<string> row = new List<string>();
                    if (item is SimNetworkBlock block)
                    {
                        row.Add(block.ComponentInstance.Id.LocalId.ToString());
                        row.Add(block.ComponentInstance.Component.Name);
                        row.Add(block.Id.LocalId.ToString());
                        row.Add("SimNetworkBlock");
                        foreach (var paramName in parameterNames)
                        {

                            if (block.ComponentInstance.InstanceParameterValuesPersistent.Any(t => t.Key.Name == paramName))
                            {
                                row.Add(block.ComponentInstance.InstanceParameterValuesPersistent.FirstOrDefault(t => t.Key.Name == paramName).Value.ToString());
                            }
                            else
                            {
                                row.Add("");
                            }
                        }
                    }
                    dataRows.Add(row);
                }

                foreach (var item in portsWithComponents)
                {
                    List<string> row = new List<string>();
                    if (item is SimNetworkPort port)
                    {
                        row.Add(port.ComponentInstance.Id.LocalId.ToString());
                        row.Add(port.ComponentInstance.Component.Name);
                        row.Add(port.Id.LocalId.ToString());
                        row.Add("SimNetworkPort");
                        foreach (var paramName in parameterNames)
                        {

                            if (port.ComponentInstance.InstanceParameterValuesPersistent.Any(t => t.Key.Name == paramName))
                            {
                                row.Add(port.ComponentInstance.InstanceParameterValuesPersistent.FirstOrDefault(t => t.Key.Name == paramName).Value.ToString());
                            }
                            else
                            {
                                row.Add("");
                            }
                        }
                    }

                    dataRows.Add(row);
                }

                CSVExporter exporter = new CSVExporter(headerRows.Count, ",");
                exporter.AddMultipleRecords(dataRows);

                // save to file
                exporter.WriteFile(filePathAndName);
            }

        }

        private static void ExportPorts(SimNetwork network, string filePathAndName)
        {
            var allPorts = GetPortsRecusrively(network);
            var headerRows = new List<string> { "ID", "Name", "PortType", "ParentID", "ParentName" };

            List<List<string>> dataRows = new List<List<string>>();
            dataRows.Add(headerRows);
            foreach (var element in allPorts)
            {
                List<string> row = new List<string>();
                row.Add(element.Id.LocalId.ToString());
                row.Add(element.Name);
                row.Add(element.PortType.ToString());
                row.Add(element.ParentNetworkElement.Id.LocalId.ToString());
                row.Add(element.ParentNetworkElement.Name);


                dataRows.Add(row);
            }

            CSVExporter exporter = new CSVExporter(headerRows.Count, ",");
            exporter.AddMultipleRecords(dataRows);

            // save to file
            exporter.WriteFile(filePathAndName);
        }



        private static List<SimNetworkPort> GetPortsRecusrively(SimNetwork network)
        {
            List<SimNetworkPort> result = new List<SimNetworkPort>();


            foreach (var port in network.Ports)
            {
                result.Add(port);
            }
            foreach (var item in network.ContainedElements)
            {
                if (item is SimNetwork subNetwork)
                {
                    result.AddRange(GetPortsRecusrively(subNetwork));
                }
                if (item is SimNetworkBlock block)
                {
                    foreach (var port in block.Ports)
                    {
                        result.Add(port);
                    }
                }
            }

            return result;

        }

        private static List<BaseSimNetworkElement> GetContainedElementsRecursively(SimNetwork network)
        {
            List<BaseSimNetworkElement> result = new List<BaseSimNetworkElement>();


            foreach (var item in network.ContainedElements)
            {
                if (item is SimNetwork subNetwork)
                {
                    result.AddRange(GetContainedElementsRecursively(subNetwork));
                }
                result.Add(item);
            }

            return result;


        }

        private static List<SimNetworkConnector> GetConnectorsRecursively(SimNetwork network)
        {
            List<SimNetworkConnector> result = new List<SimNetworkConnector>();

            foreach (var connector in network.ContainedConnectors)
            {
                result.Add(connector);
            }
            foreach (var item in network.ContainedElements)
            {
                if (item is SimNetwork subNetwork)
                {
                    result.AddRange(GetConnectorsRecursively(subNetwork));
                }
            }
            return result;
        }

    }
}
