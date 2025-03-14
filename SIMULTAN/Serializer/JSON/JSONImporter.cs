using MathNet.Numerics.LinearAlgebra;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.JSON.Serializables;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON
{
    public class JSONImporter
    {
        /// <summary>
        /// Imports a taxonomy JSON file into the project
        /// </summary>
        /// <param name="projectData">The project data</param>
        /// <param name="file">The JSON file</param>
        /// <exception cref="ArgumentException">If the file does not exist</exception>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static void ImportTaxonomy(ExtendedProjectData projectData, FileInfo file)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!file.Exists)
                throw new ArgumentException("File does not exist");
            var content = File.ReadAllText(file.FullName);
            ImportTaxonomy(projectData, content);
        }

        /// <summary>
        /// Imports a taxonomy JSON string into the project
        /// </summary>
        /// <param name="projectData">The project data</param>
        /// <param name="json">The JSON string</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static void ImportTaxonomy(ExtendedProjectData projectData, string json)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (json == null)
                throw new ArgumentNullException(nameof(json));
            var jsonTaxonomies = JsonSerializer.Deserialize<SimTaxonomySerializable[]>(json);
            ImportTaxonomy(projectData, jsonTaxonomies);
        }

        /// <summary>
        /// Imports a taxonomy JSON serializables into the project
        /// </summary>
        /// <param name="projectData">The project data</param>
        /// <param name="jsonTaxonomies">The JSON taxonomies</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static void ImportTaxonomy(ExtendedProjectData projectData, IEnumerable<SimTaxonomySerializable> jsonTaxonomies)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (jsonTaxonomies == null)
                throw new ArgumentNullException(nameof(jsonTaxonomies));
            var taxonomies = jsonTaxonomies.Select(x => x.ToSimTaxonomy());
            var tmpProjectData = new ExtendedProjectData(projectData.SynchronizationContext, projectData.DispatcherTimerFactory);
            tmpProjectData.Taxonomies.AddRange(taxonomies);
            projectData.Taxonomies.Import(tmpProjectData.Taxonomies);
        }

        /// <summary>
        /// Imports and merges the Taxonomies with the existing ones.
        /// For details see <see cref="SimTaxonomyCollection.Merge(SimTaxonomyCollection, out List{ValueTuple{SimTaxonomy, SimTaxonomy}}, bool, bool)"/>
        /// </summary>
        /// <param name="projectData">The project data</param>
        /// <param name="file">The file to import</param>
        /// <param name="conflicts">Conflicts that may have been detected while merging</param>
        /// <param name="deleteMissing">If missing entries should be removed</param>
        /// <param name="force">If merging should be forced even if conflicts were detected (uses first match)</param>
        /// <exception cref="ArgumentException">If the file could not be found</exception>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static void ImportAndMergeTaxonomy(ExtendedProjectData projectData, FileInfo file,
            out List<(SimTaxonomy other, SimTaxonomy existing)> conflicts, bool deleteMissing = false, bool force = false)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!file.Exists)
                throw new ArgumentException("File does not exist");

            var json = File.ReadAllText(file.FullName);
            ImportAndMergeTaxonomy(projectData, json, out conflicts, deleteMissing, force);
        }

        /// <summary>
        /// Imports and merges the Taxonomies with the existing ones.
        /// For details see <see cref="SimTaxonomyCollection.Merge(SimTaxonomyCollection, out List{ValueTuple{SimTaxonomy, SimTaxonomy}}, bool, bool)"/>
        /// </summary>
        /// <param name="projectData">The project data</param>
        /// <param name="json">The json to import</param>
        /// <param name="conflicts">Conflicts that may have been detected while merging</param>
        /// <param name="deleteMissing">If missing entries should be removed</param>
        /// <param name="force">If merging should be forced even if conflicts were detected (uses first match)</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static void ImportAndMergeTaxonomy(ExtendedProjectData projectData, string json,
            out List<(SimTaxonomy other, SimTaxonomy existing)> conflicts, bool deleteMissing = false, bool force = false)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            var jsonTaxonomies = JsonSerializer.Deserialize<SimTaxonomySerializable[]>(json);
            ImportAndMergeTaxonomy(projectData, jsonTaxonomies, out conflicts, deleteMissing, force);
        }

        /// <summary>
        /// Imports and merges the Taxonomies with the existing ones.
        /// For details see <see cref="SimTaxonomyCollection.Merge(SimTaxonomyCollection, out List{ValueTuple{SimTaxonomy, SimTaxonomy}}, bool, bool)"/>
        /// </summary>
        /// <param name="projectData">The project data</param>
        /// <param name="jsonTaxonomies">The JSON taxonomies to import</param>
        /// <param name="conflicts">Conflicts that may have been detected while merging</param>
        /// <param name="deleteMissing">If missing entries should be removed</param>
        /// <param name="force">If merging should be forced even if conflicts were detected (uses first match)</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static void ImportAndMergeTaxonomy(ExtendedProjectData projectData, IEnumerable<SimTaxonomySerializable> jsonTaxonomies,
            out List<(SimTaxonomy other, SimTaxonomy existing)> conflicts, bool deleteMissing = false, bool force = false)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (jsonTaxonomies == null)
                throw new ArgumentNullException(nameof(jsonTaxonomies));
            var taxonomies = jsonTaxonomies.Select(x => x.ToSimTaxonomy());
            var tmpProjectData = new ExtendedProjectData(projectData.SynchronizationContext, projectData.DispatcherTimerFactory);
            tmpProjectData.Taxonomies.AddRange(taxonomies);
            projectData.Taxonomies.Merge(tmpProjectData.Taxonomies, out conflicts, deleteMissing, force);
        }

        /// <summary>
        /// Imports and merges the Taxonomies with the target taxonomy.
        /// For details see <see cref="SimTaxonomy.MergeWith(SimTaxonomy, bool)"/>
        /// </summary>
        /// <param name="targetTaxonomy">The taxonomy to merge into</param>
        /// <param name="jsonTaxonomies">The JSON taxonomies to import</param>
        /// <param name="deleteMissing">If missing entries should be removed</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public static bool ImportAndMergeTaxonomy(SimTaxonomy targetTaxonomy, IEnumerable<SimTaxonomySerializable> jsonTaxonomies, bool deleteMissing = false)
        {
            if (targetTaxonomy == null)
                throw new ArgumentNullException(nameof(targetTaxonomy));
            if (jsonTaxonomies == null)
                throw new ArgumentNullException(nameof(jsonTaxonomies));
            var jsonTaxonomy = jsonTaxonomies.FirstOrDefault(x => x.Key == targetTaxonomy.Key);
            if (jsonTaxonomy == null)
                return false;
            var other = jsonTaxonomy.ToSimTaxonomy();
            targetTaxonomy.MergeWith(other, deleteMissing);
            return true;
        }

        /// <summary>
        /// Imports and merges the Taxonomies with the target taxonomy.
        /// For details see <see cref="SimTaxonomy.MergeWith(SimTaxonomy, bool)"/>
        /// </summary>
        /// <param name="targetTaxonomy">The taxonomy to merge into</param>
        /// <param name="json">The JSON to import</param>
        /// <param name="deleteMissing">If missing entries should be removed</param>
        public static bool ImportAndMergeTaxonomy(SimTaxonomy targetTaxonomy, string json, bool deleteMissing = false)
        {
            var jsonTaxonomies = JsonSerializer.Deserialize<SimTaxonomySerializable[]>(json);
            return ImportAndMergeTaxonomy(targetTaxonomy, jsonTaxonomies, deleteMissing);
        }

        /// <summary>
        /// Imports and merges the Taxonomies with the target taxonomy.
        /// For details see <see cref="SimTaxonomy.MergeWith(SimTaxonomy, bool)"/>
        /// </summary>
        /// <param name="targetTaxonomy">The taxonomy to merge into</param>
        /// <param name="file">The file to import</param>
        /// <param name="deleteMissing">If missing entries should be removed</param>
        public static bool ImportAndMergeTaxonomy(SimTaxonomy targetTaxonomy, FileInfo file, bool deleteMissing = false)
        {
            if (!file.Exists)
                throw new ArgumentException("File does not exist");

            var json = File.ReadAllText(file.FullName);

            return ImportAndMergeTaxonomy(targetTaxonomy, json, deleteMissing);
        }


    }
}
