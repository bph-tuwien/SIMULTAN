using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Serializer.CSV;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Serializer.SpecializedExporters
{

    [Obsolete("Needs to be removed or moved to a plugin")]
    public static class NetworkToCSVExporter
    {


        /// <summary>
        /// This is an export function for Cooling system. only works for a specific case: cooling system
        /// </summary>
        public static void Export(SimFlowNetwork data, bool exportUnits, bool showDialog, string folderPath)
        {
            FileInfo info = new FileInfo(data.Name);
            ExportNodes(info, data, exportUnits, showDialog, folderPath);
            ExportEdges(info, data, exportUnits, showDialog, folderPath);
            ExportChillers(info, data, exportUnits, showDialog, folderPath);
        }




        /// <summary>
        /// This is an export function for Cooling system. only works for a specific case: cooling system
        /// </summary>
        public static void ExportElectricarGrid(SimFlowNetwork data, bool exportUnits, bool showDialog, string folderPath)
        {
            FileInfo info = new FileInfo(data.Name);

            ExportElectricNodes(info, data, exportUnits, showDialog, folderPath);
            ExportElectricEdges(info, data, exportUnits, showDialog, folderPath);


        }

        private static void ExportElectricEdges(FileInfo target, SimFlowNetwork data, bool exportUnits, bool showDialog, string folderPath)
        {
            // Configure save file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                OverwritePrompt = true,
                FileName = "edges", // Default file name
                DefaultExt = ".csv", // Default file extension
                Filter = "csv files|*.csv" // Filter files by extension
            };


            // Show save file dialog box
            Nullable<bool> result = new Nullable<bool>();
            if (showDialog == true)
                result = dlg.ShowDialog();
            else
            {
                result = false;
                dlg.FileName = folderPath + "/" + "edges" + ".csv";
            }



            // Process save file dialog box results
            if (result.HasValue && result == true || showDialog == false)
            {
                CSVExporter exporter = new CSVExporter(12);
                exporter.CSV_DELIMITER = ";";
                List<List<string>> row_list = new List<List<string>>();

                UnicodeEncoding unicode = new UnicodeEncoding();


                List<string> header_row = new List<string>()
                {
                    "ID", "Benennung", "Typ",
                    "von", "bis",
                    "von_Benennung","bis_Benennung",
                    "Nennquerschnitt_mm2",
                    "Laenge_m",  "Kabeltyp_1_Cu_2_Al","Zustand",
                    "Anteil",
                };

                row_list.Add(header_row);
                row_list.AddRange(GetElectricalNetworkEdges(target, data, exportUnits));


                exporter.AddMultipleRecords(row_list);
                exporter.WriteFile(dlg.FileName);
            }
        }

        private static void ExportElectricNodes(FileInfo target, SimFlowNetwork data, bool exportUnits, bool showDialog, string folderPath)
        {
            // Configure save file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                OverwritePrompt = true,
                FileName = "vertices", // Default file name
                DefaultExt = ".csv", // Default file extension
                Filter = "csv files|*.csv" // Filter files by extension
            };



            // Show save file dialog box
            Nullable<bool> result = new Nullable<bool>();
            if (showDialog == true)
            {
                result = dlg.ShowDialog();
            }

            else
            {
                result = false;
                dlg.FileName = folderPath + "/" + "vertices" + ".csv";
            }



            // Process save file dialog box results
            if (result.HasValue && result == true || showDialog == false)
            {

                CSVExporter exporter = new CSVExporter(8);
                exporter.CSV_DELIMITER = ";";
                List<List<string>> row_list = new List<List<string>>();
                List<string> header_row = new List<string>() { "ID", "Benennung", "Typ", "x", "y", "z", "Nennleistung_VA", "Einspeisung" };
                row_list.Add(header_row);
                row_list.AddRange(GetElectricNetworkNodes(target, data, exportUnits));



                exporter.AddMultipleRecords(row_list);
                exporter.WriteFile(dlg.FileName);

            }

        }



        private static void ExportEdges(FileInfo target, SimFlowNetwork data, bool exportUnits, bool showDialog, string folderPath)
        {
            // Configure save file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                OverwritePrompt = true,
                FileName = "edges", // Default file name
                DefaultExt = ".csv", // Default file extension
                Filter = "csv files|*.csv" // Filter files by extension
            };


            // Show save file dialog box
            Nullable<bool> result = new Nullable<bool>();
            if (showDialog == true)
                result = dlg.ShowDialog();
            else
            {
                result = false;
                dlg.FileName = folderPath + "/" + "edges" + ".csv";
            }



            // Process save file dialog box results
            if (result.HasValue && result == true || showDialog == false)
            {
                CSVExporter exporter = new CSVExporter(20);
                exporter.CSV_DELIMITER = ";";
                List<List<string>> row_list = new List<List<string>>();

                UnicodeEncoding unicode = new UnicodeEncoding();


                List<string> header_row = new List<string>()
                {
                    "ID", "Benennung", "Typ",
                    "von", "bis",
                    "von_Benennung","bis_Benennung",
                    "Laenge_m",
                    "d_i",  "s_1","s_2",
                    "Lambda_1", "Lambda_2", "Alpha_a",
                    "Alpha_i","n_k", "Beta_k",  "v_k", "Zeta",
                    "Anteil",
                };

                row_list.Add(header_row);
                row_list.AddRange(GetNetworkEdges(target, data, exportUnits));


                exporter.AddMultipleRecords(row_list);
                exporter.WriteFile(dlg.FileName);
            }

        }




        private static List<List<string>> GetNetworkEdges(FileInfo target, SimFlowNetwork data, bool exportUnits)
        {
            List<List<string>> row_list = new List<List<string>>();
            //Get all  the edges in the network, even the ones inisde SubNetworks 
            if (data.ContainedEdges != null)
            {
                foreach (var item in data.ContainedEdges)
                {
                    SimFlowNetworkEdge edge = item.Value;
                    row_list.Add(GetEdge(target, edge, exportUnits));
                }
            }
            if (data.ContainedFlowNetworks != null)
            {
                foreach (var item in data.ContainedFlowNetworks)
                {
                    new List<List<string>>();
                    SimFlowNetwork network = item.Value;
                    List<List<string>> subNW_row_list = GetNetworkEdges(target, network, exportUnits);
                    row_list.AddRange(subNW_row_list);
                }
            }

            return row_list;
        }


        /// <summary>
        /// Exports the electrical grid´s network´s edges
        /// </summary>
        private static List<List<string>> GetElectricalNetworkEdges(FileInfo target, SimFlowNetwork data, bool exportUnits)
        {
            List<List<string>> row_list = new List<List<string>>();
            //Get all  the edges in the network, even the ones inisde SubNetworks 
            List<SimFlowNetworkNode> summarizedNodes = NetworkDependentAggregatedValueFieldExporter.GetSummarizedNodes(data, "Lastprofil");

            if (data.ContainedEdges != null)
            {
                foreach (var item in data.ContainedEdges)
                {
                    if (!summarizedNodes.Contains(item.Value.End) && !summarizedNodes.Contains(item.Value.Start))
                    {
                        SimFlowNetworkEdge edge = item.Value;
                        row_list.Add(GetElectricalGridEdge(data, target, edge, exportUnits));
                    }

                }
            }
            if (data.ContainedFlowNetworks != null)
            {
                foreach (var item in data.ContainedFlowNetworks)
                {
                    new List<List<string>>();
                    SimFlowNetwork network = item.Value;
                    List<List<string>> subNW_row_list = GetElectricalNetworkEdges(target, network, exportUnits);
                    row_list.AddRange(subNW_row_list);
                }
            }

            return row_list;
        }

        /// <summary>
        /// Gets one particular edge (Electrical Grid)
        /// </summary>
        public static List<string> GetElectricalGridEdge(SimFlowNetwork network, FileInfo target, SimFlowNetworkEdge edge, bool exportUnits)
        {
            List<string> row = new List<string>();
            SimComponentInstance geomRelationship = edge.GetUpdatedInstance(false);

            row.Add(edge.ID.ToString());

            string name = edge.Name;
            //trimming the name (in the case it contains an "_" underline, then only the prefix (before the sign) should be used for the name) 
            if (name.Contains("_"))
            {
                int index = name.IndexOf("_");
                name = name.Substring(0, index);
            }
            row.Add(name);
            row.Add(edge.Content.Name);

            //Adding reference t the starting node (or flowNetwork) 
            string sName = "";
            if (edge.Start is SimFlowNetwork)
            {
                SimFlowNetwork FnEdgeStart = edge.Start as SimFlowNetwork;
                row.Add(FnEdgeStart.ConnectionToParentEntryNode.ID.ToString());

                sName = FnEdgeStart.ContainedNodes.Where(n => n.Value.ID.LocalId == FnEdgeStart.NodeStart_ID).FirstOrDefault().Value.Name;
                if (sName.Contains("_"))
                {
                    int index = sName.IndexOf("_");
                    sName = sName.Substring(0, index);
                }

            }
            else if (edge.Start is SimFlowNetworkNode)
            {
                row.Add(edge.Start.ID.ToString());

                sName = edge.Start.Name;
                if (sName.Contains("_"))
                {
                    int index = sName.IndexOf("_");
                    sName = sName.Substring(0, index);
                }
            }



            string eName = "";
            //Adding reference t the ending node (or flowNetwork) 
            if (edge.End is SimFlowNetwork)
            {
                SimFlowNetwork FnEdgeEnd = edge.End as SimFlowNetwork;
                row.Add(FnEdgeEnd.ConnectionToParentEntryNode.ID.ToString());

                eName = edge.End.Name;
                //FnEdgeEnd.ContainedNodes.Where(n => n.Value.ID == FnEdgeEnd.NodeStart_ID).FirstOrDefault().Value.Name;
                //trimming the name (in the case it contains an "_" underline, then only the prefix (before the sign) should be used for the name) 
                if (eName.Contains("_"))
                {
                    int index = eName.IndexOf("_");
                    eName = eName.Substring(0, index);
                }
            }
            else if (edge.End is SimFlowNetworkNode)
            {
                row.Add(edge.End.ID.ToString());

                eName = edge.End.Name;

                //trimming the name (in the case it contains an "_" underline, then only the prefix (before the sign) should be used for the name) 
                if (eName.Contains("_"))
                {
                    int index = eName.IndexOf("_");
                    eName = eName.Substring(0, index);
                }
            }

            row.Add(sName);
            row.Add(eName);

            var parameterValues = edge.Content.InstanceParameterValuesPersistent;

            row.Add(parameterValues.FirstOrDefault(p => p.Key.Name == "Nennquerschnitt").Value.ToString((CultureInfo.InvariantCulture)));

            if (geomRelationship != null)
            {
                row.Add(geomRelationship.InstancePathLength.ToString((CultureInfo.InvariantCulture)));
            }
            else
            {
                row.Add("");
            }
            //Getting the parameters 

            row.Add(parameterValues.FirstOrDefault(p => p.Key.Name == "Leitungsmaterial").Value.ToString((CultureInfo.InvariantCulture)));
            row.Add(parameterValues.FirstOrDefault(p => p.Key.Name == "Zustand").Value.ToString((CultureInfo.InvariantCulture)));
            row.Add(parameterValues.FirstOrDefault(p => p.Key.Name == "Anteil").Value.ToString((CultureInfo.InvariantCulture)));

            return row;
        }

        /// <summary>
        /// Gets one particular edge (Cooling system)
        /// </summary>

        private static List<string> GetEdge(FileInfo target, SimFlowNetworkEdge edge, bool exportUnits)
        {
            List<string> row = new List<string>();

            SimComponentInstance geomRelationship = edge.GetUpdatedInstance(false);

            row.Add(edge.ID.ToString());

            string name = edge.Name;
            //trimming the name (in the case it contains an "_" underline, then only the prefix (before the sign) should be used for the name) 
            if (name.Contains("_"))
            {
                int index = name.IndexOf("_");
                name = name.Substring(0, index);
            }

            row.Add(name);

            //Type
            row.Add(edge.Content.Name);

            //Adding reference t the starting node (or flowNetwork) 
            string sName = "";
            if (edge.Start is SimFlowNetwork)
            {
                SimFlowNetwork FnEdgeStart = edge.Start as SimFlowNetwork;
                row.Add(FnEdgeStart.ConnectionToParentEntryNode.ID.ToString());

                sName = FnEdgeStart.ContainedNodes.Where(n => n.Value.ID.LocalId == FnEdgeStart.NodeStart_ID).FirstOrDefault().Value.Name;
                if (sName.Contains("_"))
                {
                    int index = sName.IndexOf("_");
                    sName = sName.Substring(0, index);
                }

            }
            else if (edge.Start is SimFlowNetworkNode)
            {
                row.Add(edge.Start.ID.ToString());

                sName = edge.Start.Name;
                if (sName.Contains("_"))
                {
                    int index = sName.IndexOf("_");
                    sName = sName.Substring(0, index);
                }
            }

            string eName = "";
            //Adding reference t the ending node (or flowNetwork) 
            if (edge.End is SimFlowNetwork)
            {
                SimFlowNetwork FnEdgeEnd = edge.End as SimFlowNetwork;
                row.Add(FnEdgeEnd.ConnectionToParentEntryNode.ID.ToString());
                eName = FnEdgeEnd.ContainedNodes.Where(n => n.Value.ID.LocalId == FnEdgeEnd.NodeStart_ID).FirstOrDefault().Value.Name;
                //trimming the name (in the case it contains an "_" underline, then only the prefix (before the sign) should be used for the name) 
                if (eName.Contains("_"))
                {
                    int index = eName.IndexOf("_");
                    eName = eName.Substring(0, index);
                }
            }
            else if (edge.End is SimFlowNetworkNode)
            {
                row.Add(edge.End.ID.ToString());

                eName = edge.End.Name;

                //trimming the name (in the case it contains an "_" underline, then only the prefix (before the sign) should be used for the name) 
                if (eName.Contains("_"))
                {
                    int index = eName.IndexOf("_");
                    eName = eName.Substring(0, index);
                }
            }

            //This code switches the start switches the start and end node´s name. have to be removed later 2020.02.21
            //Also switches the two whenever has a content name Anschluss. Probably because of the same reason as noted in february but something must have chaged in the project (naming..)  2020.05.11

            if (edge.Content.Component.Parameters.Any(p => p.Name == "Anteil") || edge.Content.Name == "Anschluss")
            {
                row.Add(eName);
                row.Add(sName);
            }
            else
            {
                row.Add(sName);
                row.Add(eName);
            }

            if (!edge.Content.Component.Parameters.Any(p => p.Name == "Anteil"))
            {
                if (geomRelationship != null)
                {
                    row.Add(geomRelationship.InstancePathLength.ToString((CultureInfo.InvariantCulture)));
                }
                else
                {
                    row.Add("");
                }
                //Getting the parameters 
                var parameterValues = edge.Content.InstanceParameterValuesPersistent;
                row.Add(parameterValues.First(p => p.Key.Name == "d_i").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "s_1").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "s_2").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "Lambda_1").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "Lambda_2").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "Alpha_a").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "Alpha_i").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "n_k").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "Beta_k").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "v_k").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add(parameterValues.First(p => p.Key.Name == "Zeta").Value.ToString((CultureInfo.InvariantCulture)));
                row.Add("");

            }
            else
            {
                row.AddRange(InpuitBlank(12));
                row.Add("1");
            }


            return row;
        }




















        private static void ExportChillers(FileInfo target, SimFlowNetwork data, bool exportUnits, bool showDialog, string folderPath)
        {

            // Configure save file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                OverwritePrompt = true,
                FileName = "chillers", // Default file name
                DefaultExt = ".csv", // Default file extension
                Filter = "csv files|*.csv" // Filter files by extension
            };


            // Show save file dialog box
            Nullable<bool> result = new Nullable<bool>();
            if (showDialog == true)
            {
                result = dlg.ShowDialog();
            }

            else
            {
                result = false;
                dlg.FileName = folderPath + "/" + "chillers" + ".csv";
            }

            // Process save file dialog box results
            if (result.HasValue && result == true || showDialog == false)
            {
                List<List<string>> row_list = new List<List<string>>();
                List<string> header_row = new List<string>();

                List<SimFlowNetworkNode> chillerNodes = new List<SimFlowNetworkNode>();


                header_row.Add("");
                List<SimFlowNetworkNode> nodes = data.GetNestedNodes();
                foreach (var node in nodes)
                {
                    if (node.Content.Name == "Kältezentrale")
                    {
                        var name = node.Name;
                        chillerNodes.Add(node);

                        if (node.Name.Contains("_"))
                        {
                            int index = node.Name.IndexOf("_");
                            name = node.Name.Substring(0, index);
                        }

                        header_row.Add(name);
                    }
                }
                row_list.Add(header_row);


                CSVExporter exporter = new CSVExporter(header_row.Count);
                exporter.CSV_DELIMITER = ";";


                //ordering the Parameters....
                List<string> orderedParameters = new List<string>();
                var parameters = chillerNodes.FirstOrDefault().Content.Component.Parameters;

                orderedParameters.Add("Z_SP");
                orderedParameters.Add("Z_EP");
                orderedParameters.Add("DW_max");
                orderedParameters.Add("GW_o");
                orderedParameters.Add("GW_u");
                orderedParameters.Add("kap_Limit");
                orderedParameters.Add("kap");


                foreach (var nodeParameter in orderedParameters)
                {
                    List<string> row = new List<string>();
                    row.Add(nodeParameter);
                    foreach (var node in chillerNodes)
                    {
                        row.Add(node.Content.InstanceParameterValuesPersistent.First(p => p.Key.Name == nodeParameter)
                            .Value.ToString(CultureInfo.InvariantCulture));
                    }
                    row_list.Add(row);
                }




                exporter.AddMultipleRecords(row_list);
                exporter.WriteFile(dlg.FileName);
            }

        }


        private static void ExportNodes(FileInfo target, SimFlowNetwork data, bool exportUnits, bool showDialog, string folderPath)
        {
            // Configure save file dialog box
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                OverwritePrompt = true,
                FileName = "vertices", // Default file name
                DefaultExt = ".csv", // Default file extension
                Filter = "csv files|*.csv" // Filter files by extension
            };
            // Show save file dialog box
            Nullable<bool> result = new Nullable<bool>();
            if (showDialog == true)
            {
                result = dlg.ShowDialog();
            }
            else
            {
                result = false;
                dlg.FileName = folderPath + "/" + "vertices" + ".csv";
            }

            // Process save file dialog box results
            if (result.HasValue && result == true || showDialog == false)
            {

                CSVExporter exporter = new CSVExporter(6);
                exporter.CSV_DELIMITER = ";";
                List<List<string>> row_list = new List<List<string>>();
                List<string> header_row = new List<string>() { "ID", "Benennung", "Typ", "x_m", "y_m", "z_m" };
                row_list.Add(header_row);
                List<List<string>> list = GetNetworkNodes(target, data, exportUnits);
                List<List<string>> input_list = new List<List<string>>();
                //sort the list: 
                input_list.AddRange(list.Where(o => o[1].Contains("E")).OrderBy(o => o[1]).ToList());
                input_list.AddRange(list.Where(o => o[1].Contains("V")).OrderBy(o => o[1]).ToList());
                input_list.AddRange(list.Where(o => o[1].Contains("K")).OrderBy(o => o[1]).ToList());
                input_list.AddRange(list.Where(o => o[1].Contains("L")).OrderBy(o => o[1]).ToList());

                row_list.AddRange(input_list);
                exporter.AddMultipleRecords(row_list);
                exporter.WriteFile(dlg.FileName);
            }
        }


        private static List<List<string>> GetElectricNetworkNodes(FileInfo target, SimFlowNetwork data, bool exportUnits)
        {
            List<List<string>> rowList = new List<List<string>>();
            List<SimFlowNetworkNode> summarizedNodes = NetworkDependentAggregatedValueFieldExporter.GetSummarizedNodes(data, "Lastprofil");
            SimFlowNetwork parentNetwork = data;
            if (data.ContainedNodes != null)
            {
                foreach (var item in data.ContainedNodes)
                {
                    if (!summarizedNodes.Contains(item.Value))
                    {
                        SimFlowNetworkNode node = item.Value;
                        rowList.Add(GetElectricNode(parentNetwork, target, node, exportUnits));
                    }
                }
            }


            if (data.ContainedFlowNetworks != null)
            {
                foreach (var item in data.ContainedFlowNetworks)
                {
                    new List<List<string>>();
                    SimFlowNetwork network = item.Value;
                    List<List<string>> subNW_row_list = GetElectricNetworkNodes(target, network, exportUnits);
                    rowList.AddRange(subNW_row_list);
                }
            }
            return rowList;
        }




        private static List<string> GetElectricNode(SimFlowNetwork parentNetwork, FileInfo target, SimFlowNetworkNode node, bool exportUnits)
        {

            string name = node.Name;
            //trimming the name (in the case it contains an "_" underline, then only the prefix (before the sign) should be used for the name) 

            if (name.Contains("_"))
            {
                int index = name.IndexOf("_");
                name = name.Substring(0, index);
            }

            SimComponentInstance geomRelationship = node.GetUpdatedInstance(true);

            List<string> row = new List<string>();
            row.Add(node.ID.ToString());


            if (parentNetwork.NodeStart_ID == node.ID.LocalId)
            {
                name = parentNetwork.Name;
            }
            row.Add(name);
            row.Add(node.Content.Name);

            if (geomRelationship != null && geomRelationship.InstancePath.Count > 0)
            {
                Point3D position = geomRelationship.InstancePath[0];

                row.Add(position.X.ToString(CultureInfo.InvariantCulture));
                row.Add(position.Y.ToString(CultureInfo.InvariantCulture));
                row.Add(position.Z.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                row.Add("");
                row.Add("");
                row.Add("");
            }

            var parameterValues = node.Content.InstanceParameterValuesPersistent;
            row.Add(parameterValues.First(p => p.Key.Name == "Nennleistung").Value.ToString((CultureInfo.InvariantCulture)));
            row.Add(parameterValues.First(p => p.Key.Name == "Einspeisung").Value.ToString((CultureInfo.InvariantCulture)));

            return row;
        }


        private static List<List<string>> GetNetworkNodes(FileInfo target, SimFlowNetwork data, bool exportUnits)
        {
            List<List<string>> row_list = new List<List<string>>();

            if (data.ContainedNodes != null)
            {
                List<List<SimFlowNetworkNode>> ordered_nodes = new List<List<SimFlowNetworkNode>>();
                List<SimFlowNetworkNode> nodes_E = new List<SimFlowNetworkNode>();
                List<SimFlowNetworkNode> nodes_V = new List<SimFlowNetworkNode>();
                List<SimFlowNetworkNode> nodes_K = new List<SimFlowNetworkNode>();
                List<SimFlowNetworkNode> nodes_L = new List<SimFlowNetworkNode>();

                foreach (var item in data.ContainedNodes)
                {
                    SimFlowNetworkNode node = item.Value;
                    row_list.Add(GetNode(target, node, exportUnits));
                }
            }


            if (data.ContainedFlowNetworks != null)
            {
                foreach (var item in data.ContainedFlowNetworks)
                {
                    new List<List<string>>();
                    SimFlowNetwork network = item.Value;
                    List<List<string>> subNW_row_list = GetNetworkNodes(target, network, exportUnits);
                    row_list.AddRange(subNW_row_list);
                }
            }
            return row_list;
        }





        private static List<string> GetNode(FileInfo target, SimFlowNetworkNode node, bool exportUnits)
        {


            string name = node.Name;
            //trimming the name (in the case it contains an "_" underline, then only the prefix (before the sign) should be used for the name) 

            if (name.Contains("_"))
            {
                int index = name.IndexOf("_");
                name = name.Substring(0, index);
            }

            SimComponentInstance geomRelationship = node.Content;

            List<string> row = new List<string>();
            row.Add(node.ID.ToString());
            row.Add(name);

            // If the content holds a load profile it should be Type "LastProfile" --> this is not quite the best way to do it. 
            if (node.Content.Component.Parameters.Any(n => n.Name == "Lastprofil"))
            {
                row.Add("Lastprofil");
            }
            else
            {
                row.Add(node.Content.Name);
            }


            if (geomRelationship != null && geomRelationship.InstancePath.Count > 0)
            {
                var position = geomRelationship.InstancePath[0];

                row.Add(position.X.ToString(CultureInfo.InvariantCulture));
                row.Add(position.Y.ToString(CultureInfo.InvariantCulture));
                row.Add(position.Z.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                row.Add("");
                row.Add("");
                row.Add("");
            }


            return row;
        }




        #region Helper for the CSV exporter

        private static List<string> InpuitBlank(int index)
        {

            List<string> row = new List<string>();
            for (int i = 0; i < index; i++)
            {
                row.Add("");
            }
            return row;

        }

        #endregion


    }

}


