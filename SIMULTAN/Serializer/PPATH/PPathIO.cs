using SIMULTAN.Data.Assets;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.PPATH
{
    /// <summary>
    /// Provides methods for serializing information about which files needs to be unpacked
    /// </summary>
    public static class PPathIO
    {
        /// <summary>
        /// Writes the path of all public assets/resources to a file
        /// </summary>
        /// <param name="file">The target file</param>
        /// <param name="projectData">The project data from which the public resources should be taken</param>
        public static void Write(FileInfo file, ProjectData projectData)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fs))
                {
                    Write(writer, projectData);
                }
            }
        }

        internal static void Write(StreamWriter writer, ProjectData projectData)
        {
            var publicResources = new HashSet<ResourceEntry>();
            var publicAssets = new List<Asset>();
            foreach (var res in projectData.AssetManager.Resources)
                ComponentDxfIO.GetPublicResources(res, false, publicResources);
            ComponentDxfIO.GetPublicAssets(projectData.AssetManager, publicAssets, publicResources);

            foreach (var resource in publicResources)
            {
                if (resource is ResourceDirectoryEntry)
                    writer.WriteLine(FileSystemNavigation.SanitizeWritePath(resource.CurrentRelativePath) + "/");
                else if (resource is ResourceFileEntry)
                    writer.WriteLine(FileSystemNavigation.SanitizeWritePath(resource.CurrentRelativePath));
            }
        }

        /// <summary>
        /// Reads a list of public resources pathes from a ppath file
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <returns>A list of public resources in the project</returns>
        public static List<string> Read(FileInfo file)
        {
            using (FileStream stream = file.OpenRead())
            {
                if (stream.Length == 0)
                    return new List<string>();

                using (StreamReader reader = new StreamReader(stream))
                {
                    return Read(reader);
                }
            }
        }

        internal static List<string> Read(StreamReader file)
        {
            List<string> result = new List<string>();

            string line;
            while ((line = file.ReadLine()) != null)
            {
                if (!string.IsNullOrEmpty(line))
                {
                    result.Add(FileSystemNavigation.SanitizePath(line));
                }
            }

            return result;
        }

    }
}
