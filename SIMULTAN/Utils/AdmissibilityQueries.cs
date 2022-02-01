using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Provides methods for checking the admissibility of various names of files or properties
    /// by applying user-defined predicates for admissibility testing.
    /// </summary>
    public static class AdmissibilityQueries
    {
        /// <summary>
        /// Checks if the given file name is admissible by applying the given predicate. If the name
        /// is inadmissible, returns an alternative name.
        /// </summary>
        /// <param name="new_file">the proposed file</param>
        /// <param name="isAdmissible">the predicate checking for admissibility</param>
        /// <param name="collisionFormat">The format of the new filename. Used with a string.Format call.
        /// The format is only used when the original name of the file is not admissible.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>if the file is admissible, and an alternative if not</returns>
        public static (bool admissible, string alternative_name) FileNameIsAdmissible(FileInfo new_file, Predicate<string> isAdmissible,
            string collisionFormat)
        {
            if (collisionFormat == null)
                throw new ArgumentNullException(nameof(collisionFormat));

            string alternative_name = new_file.FullName;
            DirectoryInfo parent = new_file.Directory;
            int counter = 0;
            bool admissible = true;
            while (!isAdmissible(alternative_name))
            {
                admissible = false;
                counter++;
                alternative_name =
                    Path.Combine(parent.FullName,
                    string.Format(collisionFormat,
                        Path.GetFileNameWithoutExtension(new_file.Name),
                        counter
                        ) + new_file.Extension
                    );
            }
            return (admissible, alternative_name);
        }

        /// <summary>
        /// Checks if the given directory name is admissible by applying the given predicate. If the name
        /// is inadmissible, returns an alternative name.
        /// </summary>
        /// <param name="new_dir">the proposed directory</param>
        /// <param name="isAdmissible">the predicate checking for admissibility</param>
        /// <param name="collisionFormat">The string format used to generate new names in case the original name is not admissible
        /// Arguments:
        ///   {0}: The original name
        ///   {1}: A running counter
        /// </param>
        /// <returns>if the directory is admissible, and an alternative if not</returns>
        public static (bool admissible, string alternative_name) DirectoryNameIsAdmissible(DirectoryInfo new_dir, Predicate<string> isAdmissible,
            string collisionFormat)
        {
            if (collisionFormat == null)
                throw new ArgumentNullException(nameof(collisionFormat));

            string alternative_name = new_dir.FullName;
            int counter = 0;
            bool admissible = true;
            while (!isAdmissible(alternative_name))
            {
                admissible = false;
                counter++;
                alternative_name =
                    string.Format(
                        collisionFormat,
                        new_dir.FullName,
                        counter
                        );
            }
            return (admissible, alternative_name);
        }

        /// <summary>
        /// Checks if the given name is admissible by checking via the given predicate.
        /// If the name is inadmissible, return an alternative name.
        /// </summary>
        /// <param name="name">the proposed name</param>
        /// <param name="isAdmissible">the predicate checking for admissibility</param>
        /// <param name="collisionNameFormat">
        /// Format used to create a new name when the initial name is not available.
        /// 
        /// Arguments:
        ///  {0}: The original name
        ///  {1}: A running counter
        /// </param>
        /// <returns>if the name is admissible, and an alternative name if not</returns>
        public static (bool admissible, string alternative_name) PropertyNameIsAdmissible(string name, Predicate<string> isAdmissible,
            string collisionNameFormat)
        {
            string alternative_name = name;
            bool admissible = true;
            int counter = 0;
            while (!isAdmissible(alternative_name))
            {
                admissible = false;
                counter++;
                alternative_name = string.Format(collisionNameFormat, name, counter);
            }

            return (admissible, alternative_name);
        }

        /// <summary>
        /// Finds a valid name while copying objects
        /// </summary>
        /// <param name="name">The proposed name</param>
        /// <param name="isUsed">A predicate that returns True when the name is already in use</param>
        /// <param name="copyFormat">
        /// Format to create the copy name.
        /// Arguments:
        ///  {0} The original name
        /// </param>
        /// <param name="copyCollisionFormat">
        /// Format used to create a new name when the initial name format did not return an available name.
        /// Arguments:
        ///  {0}: The original name
        ///  {1}: A running counter
        /// </param>
        /// <returns>A valid name</returns>
        public static string FindCopyName(string name, Predicate<string> isUsed,
            string copyFormat, string copyCollisionFormat)
        {
            string newName = string.Format(copyFormat, name);
            int newNameCounter = 1;

            while (isUsed(newName))
            {
                newName = string.Format(copyCollisionFormat, name, newNameCounter);
                ++newNameCounter;
            }

            return newName;
        }
    }
}
