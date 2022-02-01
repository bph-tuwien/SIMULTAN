using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Projects
{
    /// <summary>
    /// Holds a project graph.
    /// </summary>
    public class HierarchicProjectMetaData
    {
        #region PROPERTIES
        /// <summary>
        /// The project's global unique identifier...
        /// </summary>
        public Guid ProjectId { get; }
        /// <summary>
        /// The paths to the child project files.
        /// </summary>
        public Dictionary<Guid, string> ChildProjects { get; }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes a HierarchicalProjectMetaData.
        /// </summary>
        public HierarchicProjectMetaData()
        {
            this.ProjectId = Guid.NewGuid();
            this.ChildProjects = new Dictionary<Guid, string>();
        }

        /// <summary>
        /// Initializes a HierarchicalProjectMetaData. Copies the original child projects, but generates a new id
        /// </summary>
        /// <param name="original">The original project</param>
        public HierarchicProjectMetaData(HierarchicProjectMetaData original)
        {
            this.ProjectId = Guid.NewGuid();
            this.ChildProjects = new Dictionary<Guid, string>(original.ChildProjects);
        }

        /// <summary>
        /// Initializes a HierarchicalProjectMetaData.
        /// </summary>
        /// <param name="_project_id">The id of this project</param>
        /// <param name="_children">A list of child projects</param>
        public HierarchicProjectMetaData(Guid _project_id, Dictionary<Guid, string> _children)
        {
            this.ProjectId = _project_id;
            this.ChildProjects = new Dictionary<Guid, string>(_children);
        }

        #endregion



        #region METHODS: Children

        /// <summary>
        /// Adds a child project.
        /// </summary>
        /// <param name="_project_id">the child project's unique id</param>
        /// <param name="_project_folder">the project folder</param>
        /// <param name="_child_project_folder">the child project folder</param>
        public void AddChildProject(Guid _project_id, DirectoryInfo _project_folder, DirectoryInfo _child_project_folder)
        {
            // calculate the relative path
            string relative_path_to_child_project = FileSystemNavigation.GetRelativePath(_project_folder.FullName, _child_project_folder.FullName);
            relative_path_to_child_project = relative_path_to_child_project.Replace("~", string.Empty);
            // save
            if (this.ChildProjects.ContainsKey(_project_id))
                this.ChildProjects[_project_id] = relative_path_to_child_project;
            else
                this.ChildProjects.Add(_project_id, relative_path_to_child_project);
        }

        /// <summary>
        /// Removes the child project with the given id.
        /// </summary>
        /// <param name="_project_id">the id of the child project to be removed</param>
        public void RemoveChildProject(Guid _project_id)
        {
            this.ChildProjects.Remove(_project_id);
        }

        #endregion

        #region METHODS: ToString

        /// <summary>
        /// Serialization method.
        /// </summary>
        /// <param name="_sb">the serialized content carrier</param>
        protected void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.PROJECT_METADATA);                        // PROJECT_METADATA

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // specifics
            _sb.AppendLine(((int)ProjectSaveCode.PROJECT_ID).ToString());
            _sb.AppendLine(this.ProjectId.ToString());

            // children
            _sb.AppendLine(((int)ProjectSaveCode.NR_OF_CHILD_PROJECTS).ToString());
            _sb.AppendLine(this.ChildProjects.Count.ToString());

            foreach (var child in this.ChildProjects)
            {
                _sb.AppendLine(((int)ProjectSaveCode.CHILD_PROJECT_ID).ToString());
                _sb.AppendLine(child.Key.ToString());

                _sb.AppendLine(((int)ProjectSaveCode.CHILD_PROJECT_REL_PATH).ToString());
                _sb.AppendLine(child.Value);
            }
        }

        /// <summary>
        /// Serializes the data into a single file.
        /// </summary>
        /// <returns>the string builder containing the result of the serialization</returns>
        public StringBuilder ExportAsSingleFile()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.META_SECTION);

            this.AddToExport(ref sb);

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // FINALIZE FILE
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.EOF);

            return sb;
        }

        #endregion
    }
}
