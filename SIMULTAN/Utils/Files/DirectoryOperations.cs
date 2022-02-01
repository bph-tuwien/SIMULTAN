using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Files
{
    /// <summary>
    /// Contains methods for querying and manipulating directories.
    /// </summary>
    public static class DirectoryOperations
    {
        /// <summary>
        /// Copies a directory and its contents.
        /// </summary>
        /// <param name="_source">the source directory</param>
        /// <param name="_target">the target directory</param>
        /// <param name="_recursive">if true, the copy includes all sub-directories and files</param>
        /// <remarks>source: "https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories"</remarks>
        public static void DirectoryCopy(string _source, string _target, bool _recursive)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo sDir = new DirectoryInfo(_source);

            if (!sDir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + _source);

            DirectoryInfo[] source_subDirs = sDir.GetDirectories();
            // if the target directory doesn't exist, create it.
            if (!Directory.Exists(_target))
                Directory.CreateDirectory(_target);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = sDir.GetFiles();
            foreach (FileInfo source_file in files)
            {
                string target_file_path = Path.Combine(_target, source_file.Name);
                File.Copy(source_file.FullName, target_file_path, true);
            }

            // if copying subdirectories, copy them and their contents to new location.
            if (_recursive)
            {
                foreach (DirectoryInfo subdir in source_subDirs)
                {
                    string target_dir_path = Path.Combine(_target, subdir.Name);
                    DirectoryCopy(subdir.FullName, target_dir_path, _recursive);
                }
            }
        }
    }
}
