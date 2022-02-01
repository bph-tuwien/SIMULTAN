using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Files
{
    /// <summary>
    /// Handles different types of paths to targets on the local file system.
    /// </summary>
    public static class FileSystemNavigation
    {

        #region RELATIVE & ABSOLUTE
        /// <summary>
        /// Calculates the relative from a directory to a file.
        /// Throws exception, if any of the paths is not rooted. If the paths do not share the same root,
        /// the absolute path of 'toPath' is returned.
        /// </summary>
        /// <param name="fromPath">the base from which to calculate the relative path</param>
        /// <param name="toPath">the directory or file path for which to calculate the relative path</param>
        /// <returns></returns>
        public static string GetRelativePath(string fromPath, string toPath)
        {
            // 1. check if both paths are rooted
            if (!Path.IsPathRooted(fromPath) || !Path.IsPathRooted(toPath))
                throw new ArgumentException("Cannot calculate relative path between non-rooted paths!");

            // 2. check if the path root is the same
            string fromRoot = Path.GetPathRoot(fromPath);
            string toRoot = Path.GetPathRoot(toPath);
            if (!string.Equals(fromRoot, toRoot, StringComparison.InvariantCultureIgnoreCase))
            {
                // relative path cannot be calculated, return the absolute path
                return toPath;
            }

            // 3. extract the common parts
            string fromPath_wo_Root = fromPath.Substring(fromRoot.Length);
            string fPath = fromPath_wo_Root.Replace('/', Path.DirectorySeparatorChar);

            string toPath_wo_Root = toPath.Substring(toRoot.Length);
            string tPath = toPath_wo_Root.Replace('/', Path.DirectorySeparatorChar);

            string[] fPath_parts = fPath.Split(Path.DirectorySeparatorChar);
            string[] tPath_parts = tPath.Split(Path.DirectorySeparatorChar);

            int counter_common = 0;
            for (int i = 0; i < fPath_parts.Length && i < tPath_parts.Length; i++)
            {
                if (string.Equals(fPath_parts[i], tPath_parts[i], StringComparison.InvariantCultureIgnoreCase))
                    counter_common++;
                else
                    break;
            }

            // 4. stitch the relative path together
            string relPath = string.Empty;
            for (int i = counter_common; i < fPath_parts.Length; i++)
            {
                relPath += ".." + Path.DirectorySeparatorChar;
            }
            for (int i = counter_common; i < tPath_parts.Length; i++)
            {
                relPath = Path.Combine(relPath, tPath_parts[i]);
            }
            return relPath;
        }

        /// <summary>
        /// Determines if the path is rooted and absolute.
        /// </summary>
        /// <param name="path">the path to be evaluated</param>
        /// <returns>the result of the check</returns>
        public static bool IsPathFullyQualified(string path)
        {
            // 1. check path is rooted
            if (!Path.IsPathRooted(path))
                return false;

            // 2. get the path's root
            string root = Path.GetPathRoot(path);
            if (root != null && root.Length > 0)
            {
                if (root.StartsWith(Path.DirectorySeparatorChar.ToString()) || root.StartsWith(Path.AltDirectorySeparatorChar.ToString()))
                    return true;
                else
                {
                    DriveInfo[] dis = DriveInfo.GetDrives();
                    foreach (var d in dis)
                    {
                        if (string.Equals(d.Name, root, StringComparison.InvariantCultureIgnoreCase))
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves a relative path relative to the given directory.
        /// </summary>
        /// <param name="fromPath">the full path of the directory from which to resolve the relative path</param>
        /// <param name="toPath">the relative path itself</param>
        /// <param name="overlapPossible">if true, the relative path can have overlaps with the full path</param>
        /// <returns>the full path corresponding to 'toPath'</returns>
        public static string ReconstructFullPath(string fromPath, string toPath, bool overlapPossible)
        {
            // 1. if the toPath is rooted, it is resolable w/o the fromPath
            if (Path.IsPathRooted(toPath))
            {
                // TODO
                return toPath;
            }

            // 1. check if the from path is rooted
            if (!Path.IsPathRooted(fromPath))
                throw new ArgumentException("Cannot calculate the relative path from a non-rooted path!");

            // 2. remove the root of the fromPath
            string fromRoot = Path.GetPathRoot(fromPath);
            string fromPath_wo_Root = fromPath.Substring(fromRoot.Length);

            // 3. extract the common parts
            string fPath = fromPath_wo_Root.Replace('/', Path.DirectorySeparatorChar);
            string tPath = toPath.Replace('/', Path.DirectorySeparatorChar);

            string[] fPath_parts = fPath.Split(Path.DirectorySeparatorChar);
            string[] tPath_parts = tPath.Split(Path.DirectorySeparatorChar);

            // 4. find the overlaps
            int best_fit_start = fPath_parts.Length;
            if (overlapPossible && !(toPath.StartsWith(Path.DirectorySeparatorChar.ToString()) || toPath.StartsWith(Path.AltDirectorySeparatorChar.ToString()) || toPath.StartsWith(".")))
            {
                Regex proper_fit = new Regex(@"^[-2]*[1]+$");
                for (int shift = 0; shift < fPath_parts.Length; shift++)
                {
                    int[] sfit = Enumerable.Repeat(-1, fPath_parts.Length).ToArray();
                    for (int f = 0; f + shift < fPath_parts.Length; f++)
                    {
                        for (int i = 0; i < shift; i++)
                        {
                            sfit[i] = -2;
                        }
                        if (f < tPath_parts.Length)
                            sfit[f + shift] = string.Equals(fPath_parts[f + shift], tPath_parts[f], StringComparison.InvariantCultureIgnoreCase) ? 1 : 0;
                        else
                            sfit[f + shift] = -2;
                    }
                    string sfit_str = (sfit.Length == 1) ? sfit[0].ToString() : sfit.Select(x => x.ToString()).Aggregate((x, y) => x + y);
                    //Console.WriteLine("fit: {0}", sfit_str);
                    if (proper_fit.IsMatch(sfit_str))
                    {
                        //Console.WriteLine("MATCH for shift {0}", shift);
                        best_fit_start = shift;
                        break;
                    }
                }
            }
            else
            {
                for (int t = 0; t < tPath_parts.Length; t++)
                {
                    if (tPath_parts[t] == "..")
                        best_fit_start--;
                    else
                        break;
                }
            }

            // stitch the paths together
            List<string> full_parts = new List<string> { fromRoot };
            for (int i = 0; i < best_fit_start; i++)
            {
                full_parts.Add(fPath_parts[i]);
            }
            for (int t = 0; t < tPath_parts.Length; t++)
            {
                if (tPath_parts[t] != "." && tPath_parts[t] != "..")
                    full_parts.Add(tPath_parts[t]);
            }

            string final_path = string.Empty;
            foreach (string s in full_parts)
            {
                final_path = Path.Combine(final_path, s);
            }
            if (toPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                final_path += Path.DirectorySeparatorChar.ToString();
            else if (toPath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                final_path += Path.AltDirectorySeparatorChar.ToString();

            return final_path;
        }

        /// <summary>
        /// Checks if the child directory is a true subdirectory of the parent directory.
        /// </summary>
        /// <param name="fullPathParent">the absolute path to the potential parent</param>
        /// <param name="fullPathChild">the absolute path to the potential child</param>
        /// <param name="directoriesExist">if true, the check is perfomed on the file system</param>
        /// <returns>true if the child is a subdirectory of the parent</returns>
        public static bool IsSubdirectoryOf(string fullPathParent, string fullPathChild, bool directoriesExist = true)
        {
            if (directoriesExist)
            {
                if (!Directory.Exists(fullPathParent) || !Directory.Exists(fullPathChild))
                    throw new ArgumentException("Both directory paths must be valid!");
            }

            DirectoryInfo diP = new DirectoryInfo(fullPathParent);
            DirectoryInfo diC = new DirectoryInfo(fullPathChild);
            while (diC.Parent != null)
            {
                if (string.Equals(diC.Parent.FullName, diP.FullName, StringComparison.InvariantCultureIgnoreCase))
                    return true;
                diC = diC.Parent;
            }

            return false;

        }

        /// <summary>
        /// Checks if the given paths fulfill the following condition: 
        /// if both paths are folders, the child is in the parent or the same as the parent;
        /// if the child is a file, its directory is in the parent or the same as the parent.
        /// </summary>
        /// <param name="fullPathParent">the absolute path to the potential parent</param>
        /// <param name="fullPathChildFileOrFolder">the absolute path to the potential child</param>
        /// <param name="elementsExist">if true, the check is perfomed on the file system</param>
        /// <returns></returns>
        public static bool IsContainedIn(string fullPathParent, string fullPathChildFileOrFolder, bool elementsExist = true)
        {
            // assume child is a folder
            bool contained_as_folder = IsSubdirectoryOf(fullPathParent, fullPathChildFileOrFolder, elementsExist)
                                        || string.Equals(fullPathParent, fullPathChildFileOrFolder, StringComparison.InvariantCultureIgnoreCase);
            bool contained_as_file = false;
            if (!contained_as_folder)
            {
                // try child as a file
                if (elementsExist && !File.Exists(fullPathChildFileOrFolder))
                    throw new ArgumentException("Both paths must be valid!");
                FileInfo child = new FileInfo(fullPathChildFileOrFolder);
                DirectoryInfo dChild = child.Directory;
                contained_as_file = IsSubdirectoryOf(fullPathParent, dChild.FullName, elementsExist)
                                        || string.Equals(fullPathParent, dChild.FullName, StringComparison.InvariantCultureIgnoreCase);
            }

            return contained_as_folder || contained_as_file;
        }

        /// <summary>
        /// Tests the given paths to determine the actual absolute and relative paths.
        /// </summary>
        /// <param name="fullPath">the candidate for an absolute path</param>
        /// <param name="relPath">the candidate for the relative path</param>
        /// <param name="invalidPathString">an invalid string that indicates an invalid path</param>
        /// <returns>feedback and corrected paths</returns>
        public static (string actualFullPath, string actualRelPath, bool fullIsValid, bool relIsValid,
            bool pathsMatch, bool pathsExist) CheckPaths(string fullPath, string relPath, string invalidPathString)
        {
            string actual_rel_path = relPath;
            string actual_full_path = fullPath;
            bool paths_match = false;
            bool paths_exist = false;

            // 1. determine actual relative and full paths
            bool rel_is_absolute = FileSystemNavigation.IsPathFullyQualified(relPath);
            bool full_unknown = string.IsNullOrEmpty(fullPath) || (fullPath == invalidPathString);
            if (rel_is_absolute && full_unknown)
            {
                actual_rel_path = string.Empty;
                actual_full_path = relPath;
            }
            bool full_valid = !string.IsNullOrEmpty(actual_full_path) && (actual_full_path != invalidPathString);
            bool rel_valid = !string.IsNullOrEmpty(actual_rel_path) && (actual_rel_path != invalidPathString);

            // 2. check if the paths match
            if (!string.IsNullOrEmpty(actual_full_path) && actual_full_path != invalidPathString)
            {
                string reconstructed_full_path = ReconstructFullPath(actual_full_path, actual_rel_path, true);
                paths_match = string.Equals(reconstructed_full_path, actual_full_path, StringComparison.InvariantCultureIgnoreCase);
            }

            // 3. check if the paths exist
            if (full_valid)
                paths_exist = File.Exists(actual_full_path) || Directory.Exists(actual_full_path);

            return (actual_full_path, actual_rel_path, full_valid, rel_valid, paths_match, paths_exist);
        }

        #endregion

        #region PATH ANALYSIS

        /// <summary>
        /// Extracts the shortest or longest path containing the given symbol sequence. If the sequence 
        /// could not be found, it returns an empty string.
        /// </summary>
        /// <param name="fullPath">the path to be analysed</param>
        /// <param name="queryText">the text to look for</param>
        /// <param name="shortestPath">if true returns the shortest path; otherwise - the longest path</param>
        /// <returns>the found path</returns>
        public static string GetSubPathContaining(string fullPath, string queryText, bool shortestPath)
        {
            if (string.IsNullOrEmpty(fullPath))
                throw new ArgumentException("The path cannot be empty or Null!");

            if (string.IsNullOrEmpty(queryText))
                throw new ArgumentException("The symbol sequence to look for cannot be empty or Null!");

            // extract the components of the path
            string pathRoot = Path.GetPathRoot(fullPath);
            string path_wo_Root = fullPath.Substring(pathRoot.Length);
            string path = path_wo_Root.Replace('/', Path.DirectorySeparatorChar);
            string[] path_parts = path.Split(Path.DirectorySeparatorChar);

            if (shortestPath)
            {
                string found = pathRoot;
                if (found.Contains(queryText))
                    return found;

                for (int i = 0; i < path_parts.Length; i++)
                {
                    if (found.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        found += path_parts[i];
                    else
                        found += Path.DirectorySeparatorChar.ToString() + path_parts[i];

                    if (path_parts[i].Contains(queryText))
                        return found;
                }
            }
            else
            {
                int index = -1;
                for (int i = path_parts.Length - 1; i >= 0; i--)
                {
                    if (path_parts[i].Contains(queryText))
                    {
                        index = i;
                        break;
                    }
                }
                if (index >= 0)
                {
                    string found = pathRoot;
                    for (int n = 0; n <= index; n++)
                    {
                        if (found.EndsWith(Path.DirectorySeparatorChar.ToString()))
                            found += path_parts[n];
                        else
                            found += Path.DirectorySeparatorChar.ToString() + path_parts[n];
                    }
                    return found;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Extracts the file name of any path of any quality.
        /// </summary>
        /// <param name="path">the path</param>
        /// <returns>the found file name</returns>
        public static string ExtractNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            int last_ind_1 = path.LastIndexOf(Path.DirectorySeparatorChar);
            int last_ind_2 = path.LastIndexOf(Path.AltDirectorySeparatorChar);
            int last_ind = Math.Max(last_ind_1, last_ind_2);
            if (last_ind < 0 || last_ind >= path.Length - 2)
                return path;

            return path.Substring(last_ind + 1);
        }

        #endregion

        /// <summary>
        /// Checks whether a file is locked by trying to open it in exclusive read mode
        /// </summary>
        /// <param name="file">The file to check</param>
        /// <returns>True when the file is already in use. False otherwise.</returns>
        public static bool IsFileLocked(FileInfo file)
        {
            if (!file.Exists)
                throw new ArgumentException("File does not exist");

            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }
    }
}
