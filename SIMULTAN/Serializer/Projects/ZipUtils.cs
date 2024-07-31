using Assimp.Unmanaged;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SIMULTAN.Serializer.Projects
{
    /// <summary>
    /// Contains functionality for handling Zip archives.
    /// </summary>
    internal static class ZipUtils
    {
        /// <summary>
        /// Unpacks a given archive to an existing directory.
        /// </summary>
        /// <param name="_archive">the archive file</param>
        /// <param name="_unpacked_archive_dir">the directory to hold the unpacked files</param>
        /// <returns>a collection of all unpacked files</returns>
        internal static IEnumerable<FileInfo> UnpackArchive(FileInfo _archive, DirectoryInfo _unpacked_archive_dir)
        {
            // 0. check if any of the archive entries duplicate the files present in the target dir
            using (ZipArchive zip = ZipFile.Open(_archive.FullName, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    string target_file_path = Path.Combine(_unpacked_archive_dir.FullName, entry.Name);
                    if (File.Exists(target_file_path))
                        File.Delete(target_file_path);
                }
            }

            // 1. extract
            ZipFile.ExtractToDirectory(_archive.FullName, _unpacked_archive_dir.FullName);

            // 2. gather extracted files
            FileInfo[] extracted_files = _unpacked_archive_dir.GetFiles();

            // done
            return extracted_files;
        }

        /// <summary>
        /// Unpacks a given archive to a the given directory. Only the files with the given extensions are unpacked.
        /// </summary>
        /// <param name="_archive">the archive file</param>
        /// <param name="_extensions">the file extensions to unpack</param>
        /// <param name="_unpacked_archive_dir">the directory holding the unpacked files</param>
        /// <returns>a collection of all unpacked files</returns>
        internal static IEnumerable<FileInfo> PartialUnpackArchive(FileInfo _archive, HashSet<string> _extensions, DirectoryInfo _unpacked_archive_dir)
        {
            // 1. extract the files
            using (ZipArchive zip = ZipFile.Open(_archive.FullName, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    string entry_ext = Path.GetExtension(entry.FullName);
                    if (_extensions.Contains(entry_ext))
                    {
                        string target_file_path = Path.Combine(_unpacked_archive_dir.FullName, entry.Name);
                        entry.ExtractToFile(target_file_path, true);
                    }
                }
            }

            // 2. gather the extracted files
            FileInfo[] extracted_files = _unpacked_archive_dir.GetFiles();

            // done
            return extracted_files;
        }

        /// <summary>
        /// Unpacks only the given paths from the archive into the given directory.
        /// </summary>
        /// <param name="_archive">the archive file</param>
        /// <param name="_paths">the paths to find and unpack</param>
        /// <param name="_unpacked_archive_dir">the directory holding the unpacked files and directories</param>
        /// <returns>>a collection of all unpacked files and directories</returns>
        internal static (IEnumerable<FileInfo> files, IEnumerable<DirectoryInfo> dirs) PartialUnpackPaths(FileInfo _archive, IEnumerable<string> _paths, DirectoryInfo _unpacked_archive_dir)
        {
            if (_paths == null)
                throw new ArgumentNullException(nameof(_paths));

            var files = new List<FileInfo>();
            var dirs = new List<DirectoryInfo>();

            using (ZipArchive zip = ZipFile.Open(_archive.FullName, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    var sanitizedFullName = FileSystemNavigation.SanitizePath(entry.FullName);
                    if (_paths.Any(x => sanitizedFullName.EndsWith(x)))
                    {
                        // check if directory or file
                        bool is_dir = (sanitizedFullName.EndsWith(Path.DirectorySeparatorChar.ToString()) || sanitizedFullName.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                                        && string.IsNullOrEmpty(entry.Name);

                        if (is_dir)
                        {
                            string target_dir_path = Path.Combine(_unpacked_archive_dir.FullName, sanitizedFullName);
                            if (!Directory.Exists(target_dir_path))
                                Directory.CreateDirectory(target_dir_path);
                            dirs.Add(new DirectoryInfo(target_dir_path));
                        }
                        else
                        {
                            string target_file_path = Path.Combine(_unpacked_archive_dir.FullName, sanitizedFullName);
                            string target_folder_path = Path.GetDirectoryName(target_file_path);

                            //Create folder if not existing
                            if (!Directory.Exists(target_folder_path))
                                Directory.CreateDirectory(target_folder_path);

                            //Unpack if file
                            if (!string.IsNullOrEmpty(entry.Name))
                            {
                                entry.ExtractToFile(target_file_path, true);
                            }
                            files.Add(new FileInfo(target_file_path));
                        }
                    }
                }
            }

            return (files, dirs);
        }

        /// <summary>
        /// Unpacks a given archive to a the given directory. Only the files with extensions not contained in _extensions_to_skip are unpacked.
        /// </summary>
        /// <param name="_archive">the archive file</param>
        /// <param name="_extensions_to_skip">the file extensions to skip when unpacking</param>
        /// <param name="_unpacked_archive_dir">the directory holding the unpacked files</param>
        /// <returns>a collection of all unpacked files</returns>
        internal static IEnumerable<FileSystemInfo> PartialUnpackArchiveSkip(FileInfo _archive, HashSet<string> _extensions_to_skip, DirectoryInfo _unpacked_archive_dir)
        {
            // 1. extract the files


            //var encoding = Encoding.GetEncoding("IBM437");

            //using (FileStream stream = new FileStream(_archive.FullName, FileMode.OpenOrCreate))
            //{
            //    using (ZipArchive zip = new ZipArchive(stream, ZipArchiveMode.Update, false, encoding))
            using (ZipArchive zip = ZipFile.Open(_archive.FullName, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    var entry_ext = Path.GetExtension(entry.FullName);
                    if (string.IsNullOrEmpty(entry_ext) || !_extensions_to_skip.Contains(entry_ext))
                    {
                        string target_file_path = Path.Combine(_unpacked_archive_dir.FullName, entry.FullName);
                        string target_folder_path = Path.GetDirectoryName(target_file_path);

                        //Create folder if not exist
                        if (!Directory.Exists(target_folder_path))
                            Directory.CreateDirectory(target_folder_path);

                        //Unpack if file
                        if (!string.IsNullOrEmpty(entry.Name))
                        {
                            entry.ExtractToFile(target_file_path, true);
                        }
                    }
                }
            }
            //}

            // 2. gather the extracted files
            //FileInfo[] extracted_files = _unpacked_archive_dir.GetFiles();
            //DirectoryInfo[] extracted_dirs = _unpacked_archive_dir.GetDirectories();
            FileSystemInfo[] extracted_all = _unpacked_archive_dir.GetFileSystemInfos();

            // done
            return extracted_all;
        }

        /// <summary>
        /// Creates a Zip Archive from the given files.
        /// </summary>
        /// <param name="_archive">the archive file</param>
        /// <param name="_dirs">the archive directories containing the archive files</param>
        /// <param name="_files">the files to zip</param>
        /// <param name="_root_dir">the root directory of all files and directories to pack</param>
        internal static void CreateArchiveFrom(FileInfo _archive, IEnumerable<DirectoryInfo> _dirs, IEnumerable<FileInfo> _files, string _root_dir)
        {
            if (_files == null) return;
            if (_files.Count() == 0) return;

            if (File.Exists(_archive.FullName))
                File.Delete(_archive.FullName);
            using (ZipArchive archive = ZipFile.Open(_archive.FullName, ZipArchiveMode.Create))
            {
                //List<string> dir_entries = new List<string>();
                foreach (DirectoryInfo di in _dirs)
                {
                    //ZipUtils.CreateZipEntryFromDirectoryRecursive(archive, di, _root_dir, ref dir_entries);

                    string dpath = FileSystemNavigation.GetRelativePath(_root_dir, di.FullName);
                    if (!dpath.EndsWith(Path.DirectorySeparatorChar.ToString()) && !dpath.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                        dpath += Path.DirectorySeparatorChar;
                    archive.CreateEntry(dpath);
                }
                foreach (FileInfo fi in _files)
                {
                    string fpath = FileSystemNavigation.GetRelativePath(_root_dir, fi.FullName);
                    archive.CreateEntryFromFile(fi.FullName, fpath);
                }
            }
        }

        /// <summary>
        /// Opens an existing Zip Archive and replaces only the entries corresponding to the 
        /// given files.
        /// </summary>
        /// <param name="_archive_file">the existing archive file</param>
        /// <param name="_files">the files for entry update</param>
        /// <param name="_root_dir">the root directory of all files and directories to pack</param>
        internal static void UpdateArchiveFrom(FileInfo _archive_file, IEnumerable<FileInfo> _files, string _root_dir)
        {
            using (FileStream fs = File.Open(_archive_file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (BufferedStream bs = new BufferedStream(fs))
                {
                    using (ZipArchive archive = new ZipArchive(bs, ZipArchiveMode.Update))
                    {
                        foreach (FileInfo fi in _files)
                        {
                            string fpath = FileSystemNavigation.GetRelativePath(_root_dir, fi.FullName);

                            // replace the corresponding Zip entry in the archive
                            ZipArchiveEntry zip_entry = archive.GetEntry(fpath);
                            if (zip_entry != null)
                                zip_entry.Delete();
                            archive.CreateEntryFromFile(fi.FullName, fpath);
                        }
                    }
                }
            }
        }
    }
}
