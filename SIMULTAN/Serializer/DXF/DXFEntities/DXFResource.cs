using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// Wrapper class for a serialized resource entry.
    /// </summary>
    internal abstract class DXFResourceEntry : DXFEntityContainer
    {
        #region CLASS MEMBERS

        /// <summary>
        /// Corresponds to <see cref="ResourceEntry.UserWithWritingAccess"/>
        /// </summary>
        protected SimUserRole dxf_UserWithWritingAccess;
        /// <summary>
        /// Corresponds to <see cref="ResourceEntry.Key"/>
        /// </summary>
        protected int dxf_Key;
        /// <summary>
        /// Corresponds to <see cref="ResourceEntry.Name"/>
        /// </summary>
        protected string dxf_Name;
        /// <summary>
        /// Corresponds to <see cref="ResourceEntry.CurrentRelativePath"/>
        /// </summary>
        protected string dxf_CurrentRelativePath;
        /// <summary>
        /// Corresponds to the filed current_anchor_of_relative_path in ResourceEntry.
        /// </summary>
        protected string dxf_current_anchor_of_relative_path;
        /// <summary>
        /// Corresponds to <see cref="ResourceEntry.CurrentFullPath"/>
        /// </summary>
        protected string dxf_CurrentFullPath;

        /// <summary>
        /// Signifies if at saving time the property <see cref="ResourceEntry.Parent"/> was set.
        /// </summary>
        protected bool dxf_HasParent;

        /// <summary>
        /// The visibility of the resource within the project and among projects.
        /// </summary>
        protected SimComponentVisibility dxf_Visibility;

        /// <summary>
        /// Holds the actual parsed entity.
        /// </summary>
        internal ResourceEntry dxf_parsed;

        #endregion

        /// <summary>
        /// Initializes an instance with default values.
        /// </summary>
        protected DXFResourceEntry()
        {
            this.dxf_UserWithWritingAccess = SimUserRole.ADMINISTRATOR;
            this.dxf_Key = -1; // invalid
            this.dxf_Name = string.Empty;
            this.dxf_CurrentRelativePath = AssetManager.PATH_NOT_FOUND;
            this.dxf_current_anchor_of_relative_path = AssetManager.PATH_NOT_FOUND;
            this.dxf_CurrentFullPath = AssetManager.PATH_NOT_FOUND;

            this.dxf_HasParent = false;
            this.dxf_Visibility = SimComponentVisibility.VisibleInProject;
        }

        #region OVERRIDES: Read Property
        /// <inheritdoc/>
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ResourceSaveCode.RESOURCE_USER:
                    this.dxf_UserWithWritingAccess = ComponentUtils.StringToComponentManagerType(this.Decoder.FValue);
                    break;
                case (int)ResourceSaveCode.RESOURCE_KEY:
                    this.dxf_Key = this.Decoder.IntValue();
                    break;
                case (int)ResourceSaveCode.RESOURCE_NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    //Console.WriteLine("name: {0}", this.dxf_Name);
                    break;
                case (int)ResourceSaveCode.RESOURCE_RELATIVE:
                    this.dxf_CurrentRelativePath = this.Decoder.FValue;
                    break;
                case (int)ResourceSaveCode.RESOURCE_ANCHOR:
                    this.dxf_current_anchor_of_relative_path = this.Decoder.FValue;
                    break;
                case (int)ResourceSaveCode.RESOURCE_FULL:
                    this.dxf_CurrentFullPath = this.Decoder.FValue;
                    break;
                case (int)ResourceSaveCode.RESOURCE_HAS_PARENT:
                    this.dxf_HasParent = this.Decoder.FValue == "1";
                    break;
                case (int)ResourceSaveCode.RESOURCE_VISIBILITY:
                    bool success = Enum.TryParse(this.Decoder.FValue, out SimComponentVisibility vis);
                    if (success)
                        this.dxf_Visibility = vis;
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion
    }

    /// <summary>
    /// Wrapper class for a serialized resource directory.
    /// </summary>
    internal class DXFResourceDirectoryEntry : DXFResourceEntry
    {
        private List<ResourceEntry> dxf_Children;
        private int dxf_nr_Children;
        /// <inheritdoc/>
        public DXFResourceDirectoryEntry()
        {
            this.dxf_Children = new List<ResourceEntry>();
            this.dxf_nr_Children = 0;
        }

        #region OVERRIDES: Read Property
        /// <inheritdoc/>
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ResourceSaveCode.RESOURCE_CHILDREN:
                    this.dxf_nr_Children = this.Decoder.IntValue();
                    break;
                default:
                    // DXFResourceEntry: RESOURCE_USER, RESOURCE_KEY, RESOURCE_NAME, RESOURCE_RELATIVE, RESOURCE_ANCHOR, RESOURCE_FULL, RESOURCE_HAS_PARENT
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Adding Entities
        /// <inheritdoc/>
        internal override bool AddEntity(DXFEntity _e)
        {
            // handle depending on type
            if (_e == null) return false;
            bool add_successful = false;

            DXFComponentSubContainer container = _e as DXFComponentSubContainer;
            if (container != null)
            {
                add_successful = true;
                foreach (DXFEntity sE in container.EC_Entities)
                {
                    DXFResourceEntry entry = sE as DXFResourceEntry;
                    if (entry != null && this.dxf_nr_Children > this.dxf_Children.Count)
                    {
                        // take the parsed entry
                        this.dxf_Children.Add(entry.dxf_parsed);
                        add_successful &= true;
                        //Console.WriteLine("Resource Directory {0} added child {1}", this.dxf_Name, entry.dxf_parsed.Name);
                    }
                }
            }
            return add_successful;
        }

        #endregion

        #region OVERRIDES: Post-Processing
        /// <inheritdoc/>
        internal override void OnLoaded()
        {
            base.OnLoaded();

            this.dxf_parsed = this.Decoder.ProjectData.AssetManager.AddParsedResourceDirectoryEntry(this.dxf_UserWithWritingAccess, this.dxf_CurrentRelativePath, this.dxf_CurrentFullPath, this.dxf_Key, !this.dxf_HasParent, this.Decoder.CheckForResourceExistence);
            if (this.dxf_parsed != null && this.dxf_parsed is ResourceDirectoryEntry)
            {
                (this.dxf_parsed as ResourceDirectoryEntry).Children.SuppressNotification = true;
                foreach (var child in this.dxf_Children)
                {
                    child.Parent = this.dxf_parsed;
                    (this.dxf_parsed as ResourceDirectoryEntry).Children.Add(child);
                }
                (this.dxf_parsed as ResourceDirectoryEntry).Children.SuppressNotification = false;
                this.dxf_parsed.Visibility = this.dxf_Visibility;
            }
        }

        #endregion

    }

    /// <summary>
    /// Wrapper class for all types of serialized resource files.
    /// </summary>
    internal class DXFContainedResourceFileEntry : DXFResourceEntry
    {
        private bool dxf_FileIsNotReallyContained;

        /// <inheritdoc/>
        public DXFContainedResourceFileEntry()
        {
            this.dxf_FileIsNotReallyContained = false;
        }

        #region OVERRIDES: Read Property
        /// <inheritdoc/>
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ResourceSaveCode.RESOURCE_PROBLEM:
                    this.dxf_FileIsNotReallyContained = this.Decoder.FValue == "1";
                    break;
                default:
                    // DXFResourceEntry: RESOURCE_USER, RESOURCE_KEY, RESOURCE_NAME, RESOURCE_RELATIVE, RESOURCE_ANCHOR, RESOURCE_FULL, RESOURCE_HAS_PARENT
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }
        #endregion

        #region OVERRIDES: Post-Processing
        /// <inheritdoc/>
        internal override void OnLoaded()
        {
            base.OnLoaded();

            var test_parse = this.Decoder.ProjectData.AssetManager.AddParsedContainedResourceFileEntry(this.dxf_UserWithWritingAccess, this.dxf_CurrentRelativePath, this.dxf_CurrentFullPath, this.dxf_Key, !this.dxf_HasParent, this.Decoder.CheckForResourceExistence);
            if (test_parse.parsed != null)
                this.dxf_parsed = test_parse.parsed;
            else
                this.dxf_parsed = test_parse.alternative;
            if (this.dxf_parsed != null)
                this.dxf_parsed.Visibility = this.dxf_Visibility;
            //Console.WriteLine("Resource File Contained done: {0}", this.dxf_Name);
        }

        #endregion

    }

    /// <summary>
    /// Wrapper class for a linked resource file entry.
    /// </summary>
    internal class DXFLinkedResourceFileEntry : DXFResourceEntry
    {
        private bool dxf_MoreThanOneValidPathDetected;

        /// <inheritdoc/>
        public DXFLinkedResourceFileEntry()
        {
            this.dxf_MoreThanOneValidPathDetected = false;
        }

        #region OVERRIDES: Read Property
        /// <inheritdoc/>
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ResourceSaveCode.RESOURCE_PROBLEM:
                    this.dxf_MoreThanOneValidPathDetected = this.Decoder.FValue == "1";
                    break;
                default:
                    // DXFResourceEntry: RESOURCE_USER, RESOURCE_KEY, RESOURCE_NAME, RESOURCE_RELATIVE, RESOURCE_ANCHOR, RESOURCE_FULL, RESOURCE_HAS_PARENT
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }
        #endregion

        #region OVERRIDES: Post-Processing
        /// <inheritdoc/>
        internal override void OnLoaded()
        {
            base.OnLoaded();

            var test_parse = this.Decoder.ProjectData.AssetManager.AddParsedLinkedResourceFileEntry(this.dxf_UserWithWritingAccess, this.dxf_CurrentRelativePath, this.dxf_CurrentFullPath, this.dxf_Key, !this.dxf_HasParent, this.Decoder.CheckForResourceExistence);
            if (test_parse.parsed != null)
                this.dxf_parsed = test_parse.parsed;
            else
                this.dxf_parsed = test_parse.alternative;
            if (this.dxf_parsed != null)
                this.dxf_parsed.Visibility = this.dxf_Visibility;
            //Console.WriteLine("Resource File Linked done: {0}", this.dxf_Name);
        }

        #endregion
    }
}
