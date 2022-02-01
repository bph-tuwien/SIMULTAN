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

namespace SIMULTAN.Serializer.SpecializedExporters
{


    /// <summary>
    /// This class holds functions which are handling the export of ValueFields for Simulation exports. This exporter aggregates the 
    /// ValueFields based on their position in the Network. For each node which has a component which has an assigned a ValueField gets exported. 
    /// Nodes in the same Network (or subnetwork) are summarized together weighed with their "Anteil" value. 
    /// </summary>
    public class NetworkDependentAggregatedValueFieldExporter
    {
        /// <summary>
        /// Aggregates lastProfile tables for each subnetwork.
        /// </summary>
        public static void ExportCoolingLoad(SimFlowNetwork network, string fileName, bool showDialog, string folderPath)
        {
            FileInfo info = new FileInfo(fileName);
            List<List<string>> exportableString = ExportCooling(network);

            //NEED ERROR MESSAGE
            if (exportableString.FirstOrDefault() == null)
            {
                return;
            }
            CSVExporter exporter = new CSVExporter(exportableString.FirstOrDefault().Count);
            exporter.CSV_DELIMITER = ";";
            exporter.AddMultipleRecords(exportableString);
            // save to file
            exporter.WriteFile(folderPath + "/" + info.Name + ".csv");
        }


        private static List<List<string>> ExportCooling(SimFlowNetwork network)
        {
            List<List<SimFlowNetworkNode>> nodes = GetNodes(network);

            List<List<string>> columns = new List<List<string>>();
            List<string> row_header_column = new List<string>();

            //for each network or subnetwork in the flow network 
            foreach (List<SimFlowNetworkNode> NetworkNodes in nodes)
            {
                List<List<string>> column = new List<List<string>>();
                List<double> colToExport = new List<double>();

                foreach (SimFlowNetworkNode node in NetworkNodes)
                {
                    SimParameter parameter = node.Content.Component.Parameters.Where(p => p.Name == "Lastprofil").FirstOrDefault();
                    if (parameter != null)
                    {
                        List<string> nodeColumn = new List<string>();
                        SimMultiValueBigTable.SimMultiValueBigTablePointer MVpointer = parameter.MultiValuePointer as SimMultiValueBigTable.SimMultiValueBigTablePointer;
                        if (MVpointer != null && MVpointer.ValueField != null)
                        {
                            if (MVpointer.ValueField.MVType == MultiValueType.TABLE)
                            {
                                SimMultiValueBigTable table = MVpointer.ValueField as SimMultiValueBigTable;

                                for (int i = 0; i < table.Count(1); i++)
                                {
                                    nodeColumn.Add(table.ColumnHeaders[i].Name);
                                    for (int j = 0; j < table.Count(0); j++)
                                    {
                                        var value = table[j, i];
                                        nodeColumn.Add(value.ToString(CultureInfo.InvariantCulture));
                                    }
                                }
                                row_header_column.Clear();
                                row_header_column.Add("Timestamp");
                                table.RowHeaders.ToList().ForEach(hr => row_header_column.Add(hr.Name));
                                column.Add(nodeColumn);

                            }
                        }
                    }
                }
                columns.AddRange(column);
            }

            //Order the sequence of the columns, later this should be deleted 
            columns.Sort((a, b) => a[0].CompareTo(b[0]));

            //Transpose the table to get the right format for the CSVExporter 
            List<List<string>> columnsTrans = new List<List<string>>();

            if (columns.Count != 0)
            {
                for (int i = 0; i < columns.FirstOrDefault().Count; i++)
                {
                    List<string> row = new List<string>();

                    var j = i;
                    if (i == row_header_column.Count)
                    {
                        return columnsTrans;
                    }
                    row.Add(row_header_column[j]);

                    foreach (var col in columns)
                    {
                        row.Add(col[i]);
                    }
                    columnsTrans.Add(row);
                }
                return columnsTrans;

            }

            return columnsTrans;
        }






        /// <summary>
        ///  Exports electrical nodes with aggreagated LoadProfiles
        /// </summary>	
        public static void ExportElectricalLoad(SimFlowNetwork network, string fileName, bool showDialog, string folderPath)
        {

            FileInfo info = new FileInfo(fileName);


            //this one returns the exportable columns and rows 
            List<List<string>> exportableString = ExportElectrical(network);

            CSVExporter exporter = new CSVExporter(exportableString.FirstOrDefault().Count);
            exporter.CSV_DELIMITER = ";";
            exporter.AddMultipleRecords(exportableString);
            // save to file
            exporter.WriteFile(folderPath + "/" + info.Name + ".csv");
        }




        private static List<List<string>> ExportElectrical(SimFlowNetwork network)
        {

            List<List<SimFlowNetworkNode>> nodes = GetNodes(network);
            List<List<string>> columns = new List<List<string>>();
            List<string> row_header_column = new List<string>();

            //for each network or subnetwork in the flow network 
            foreach (List<SimFlowNetworkNode> NetworkNodes in nodes)
            {
                List<List<double>> column = new List<List<double>>();
                List<double> colToExport = new List<double>();

                colToExport.Add(NetworkNodes.Where(n => n.Edges_In.Count == 0).FirstOrDefault().ID.LocalId);

                foreach (SimFlowNetworkNode node in NetworkNodes)
                {
                    SimParameter parameter = node.Content.Component.Parameters.Where(p => p.Name == "Lastprofil").FirstOrDefault();

                    if (parameter != null)
                    {
                        List<double> nodeColumn = new List<double>();
                        SimMultiValueBigTable.SimMultiValueBigTablePointer MVpointer = parameter.MultiValuePointer as SimMultiValueBigTable.SimMultiValueBigTablePointer;
                        if (MVpointer != null && MVpointer.ValueField != null)
                        {
                            if (MVpointer.ValueField.MVType == MultiValueType.TABLE)
                            {
                                SimMultiValueBigTable table = MVpointer.ValueField as SimMultiValueBigTable;

                                //Getting the weight factor (in this exporter the "Anteil" value for each node will be that) 

                                SimFlowNetworkElement element = node.Edges_In.Where(e => e.Content.Component.Parameters.Any(p => p.Name == "Anteil")).FirstOrDefault();
                                //Getting the parameters 
                                var parameterValues = element.Content.InstanceParameterValuesPersistent;

                                for (int i = 0; i < table.Count(1); i++)
                                {
                                    for (int j = 0; j < table.Count(0); j++)
                                    {
                                        var value = table[j, i] * parameterValues.First(pv => pv.Key.Name == "Anteil").Value;
                                        nodeColumn.Add(value);
                                    }
                                }

                                row_header_column.Clear();
                                table.RowHeaders.ToList().ForEach(hr => row_header_column.Add(hr.Name));
                                column.Add(nodeColumn);
                            }
                        }
                    }
                }

                //Aggregate the columns for a subnetwork (or network) 
                if (column.Count > 0)
                {
                    for (int i = 0; i < column.FirstOrDefault().Count; i++)
                    {
                        double value = 0;
                        foreach (var nC in column)
                        {
                            value = value + nC[i];
                        }
                        colToExport.Add(value);
                    }
                }
                if (colToExport.Count > 1)
                {

                    List<string> stringCols = new List<string>();
                    colToExport.ForEach(c => stringCols.Add(c.ToString(CultureInfo.InvariantCulture)));
                    columns.Add(stringCols);
                }

            }

            //Transpose the table to get the right format for the CSVExporter 
            List<List<string>> columnsTrans = new List<List<string>>();
            if (columns.Count < 1)
                return columnsTrans;

            for (int i = 0; i < columns.FirstOrDefault().Count; i++)
            {
                List<string> row = new List<string>();
                if (i == 0)
                {
                    row.Add("Timestamp");
                }
                else
                {
                    row.Add(row_header_column[i - 1]);

                }
                foreach (var col in columns)
                {

                    row.Add(col[i]);

                }
                columnsTrans.Add(row);
            }

            return columnsTrans;
        }


        /// <summary>
        /// Gets the nodes which were involved in the aggregation
        /// </summary>
        public static List<SimFlowNetworkNode> GetSummarizedNodes(SimFlowNetwork network, string parameterName)
        {
            List<SimFlowNetworkNode> summarizedNodes = new List<SimFlowNetworkNode>();
            List<SimFlowNetworkNode> nodesWithContent = new List<SimFlowNetworkNode>();

            foreach (var item in GetNodes(network))
            {
                nodesWithContent.AddRange(item.Where(n => n.Content != null));
                summarizedNodes.AddRange(nodesWithContent.Where(n => n.Content.Component.Parameters.Any(p => p.Name == parameterName)).ToList());
            }
            return summarizedNodes;
        }

        private static List<List<SimFlowNetworkNode>> GetNodes(SimFlowNetwork network)
        {
            List<List<SimFlowNetworkNode>> nodes = new List<List<SimFlowNetworkNode>>();
            List<SimFlowNetworkNode> currentNode = new List<SimFlowNetworkNode>();

            List<Tuple<SimFlowNetworkNode, SimMultiValue>> nodesAndBigTable = new List<Tuple<SimFlowNetworkNode, SimMultiValue>>();

            foreach (var node in network.ContainedNodes)
            {
                currentNode.Add(node.Value);
                try
                {
                    nodesAndBigTable.Add(new Tuple<SimFlowNetworkNode, SimMultiValue>(node.Value, node.Value.Content.Component.Parameters
                        .Where(p => p.MultiValuePointer is SimMultiValuePointer)
                        .FirstOrDefault().MultiValuePointer.ValueField));
                }
                catch (Exception)
                {
                }

            }
            nodes.Add(currentNode);

            if (network.ContainedFlowNetworks != null)
            {
                foreach (var subNetwork in network.ContainedFlowNetworks)
                {
                    nodes.AddRange(GetNodes(subNetwork.Value));
                }
            }
            return nodes;
        }
    }



}
