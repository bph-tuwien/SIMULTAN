using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Class containing helper functions for the JSON export
    /// </summary>
    public static class JSONExportHelpers
    {
        /// <summary>
        /// Finds the root network of a SimNetworkElement
        /// </summary>
        public static SimNetwork GetRootNetwork(BaseSimNetworkElement element)
        {
            if (element is SimNetwork nw)
            {
                if (nw.ParentNetwork.ParentNetwork == null)
                {
                    return nw.ParentNetwork;
                }
                else
                {
                    return GetRootNetwork(nw.ParentNetwork);
                }
            }
            else if (element is SimNetworkBlock block)
            {
                if (block.ParentNetwork.ParentNetwork == null)
                {
                    return block.ParentNetwork;
                }
                else
                {
                    return GetRootNetwork(block.ParentNetwork);
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Exporter to create JSON
    /// </summary>
    public static class JSONExporter
    {

        /// <summary>
        /// Exports the project data into a JSON file
        /// </summary>
        /// <param name="projectData">The project to export</param>
        /// <param name="fileToSave">The file to save the .json to</param>
        public static void Export(ProjectData projectData, FileInfo fileToSave)
        {
            if (fileToSave.Extension != ".json")
                throw new ArgumentException("not a .json file");

            var serializableProjectData = new ProjectSerializable(projectData);

            var options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };

            //open file stream
            using (StreamWriter file = File.CreateText(fileToSave.FullName))
            {
                var json = JsonSerializer.Serialize(serializableProjectData, options);
                file.Write(json);
            }
        }

        /// <summary>
        /// Exports the selected components and networks
        /// </summary>
        /// <param name="projectData">The project to export</param>
        /// <param name="networks">Networks to export</param>
        /// <param name="fileToSave">The file to save the .json to</param>
        public static void ExportNetworks(ProjectData projectData, List<SimNetwork> networks, FileInfo fileToSave)
        {
            var serializableNetworks = new List<SimNetworkSerializable>();
            var serializableComponents = new List<SimComponentSerializable>();
            var componentsToExport = new List<SimComponent>();

            foreach (var network in networks)
            {
                serializableNetworks.Add(new SimNetworkSerializable(network));
                var components = SimNetworkSerializable.GetComponentInstances(network);
                foreach (var item in components)
                {
                    if (!componentsToExport.Contains(item))
                    {
                        componentsToExport.Add(item);
                    }
                }
            }
            foreach (var item in componentsToExport)
            {
                serializableComponents.Add(new SimComponentSerializable(item));
            }

            var serializableProjectData = new ProjectSerializable(projectData, serializableComponents, serializableNetworks);
            //open file stream
            using (StreamWriter file = File.CreateText(fileToSave.FullName))
            {
                var json = JsonSerializer.Serialize(serializableProjectData, new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                });
                file.Write(json);
            }
        }

        /// <summary>
        /// Exports taxonomies to a JSON file
        /// </summary>
        /// <param name="taxonomies">The taxonomies export</param>
        /// <param name="file">The file to export to</param>
        public static void ExportTaxonomy(IEnumerable<SimTaxonomy> taxonomies, FileInfo file)
        {
            using var fileStream = file.Open(FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(fileStream);
            writer.Write(ExportTaxonomy(taxonomies));
        }

        /// <summary>
        /// Exports taxonomies to a JSON string
        /// </summary>
        /// <param name="taxonomies">The taxonomies export</param>
        /// <returns>The taxonomy JSON</returns>
        public static string ExportTaxonomy(IEnumerable<SimTaxonomy> taxonomies)
        {
            return ExportTaxonomy(taxonomies.Select(x => new SimTaxonomySerializable(x)));
        }

        /// <summary>
        /// Exports taxonomies to a JSON string
        /// </summary>
        /// <param name="taxonomies">The taxonomies export</param>
        /// <returns>The taxonomy JSON</returns>
        public static string ExportTaxonomy(IEnumerable<SimTaxonomySerializable> taxonomies)
        {
            return JsonSerializer.Serialize(taxonomies, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });
        }
    }
}
