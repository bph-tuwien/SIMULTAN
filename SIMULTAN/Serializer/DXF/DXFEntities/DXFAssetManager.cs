using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper class for asset path entries
    /// </summary>
    internal class DXFAssetManager : DXFEntityContainer
    {
        #region CLASS MEMBERS

        private string dxf_WorkingDirectory;
        private List<string> dxf_paths_to_resource_files;
        private int dxf_nr_paths_to_resource_files;
        private Dictionary<int, Tuple<SimUserRole, string, string, int, bool>> dxf_resource_files;
        private int dxf_nr_resource_files;

        private bool dxf_saved_resources_as_objects;

        private int dxf_current_rf_key;
        private string dxf_current_rf_file_name;
        private string dxf_current_rf_full_path;
        private SimUserRole dxf_current_rf_user;
        private bool dxf_current_is_contained;

        private int dxf_current_nr_of_properties_expected;
        private int dxf_current_nr_of_properties_read;

        #endregion

        public DXFAssetManager()
        {
            this.dxf_WorkingDirectory = string.Empty;
            this.dxf_paths_to_resource_files = new List<string>();
            this.dxf_nr_paths_to_resource_files = 0;
            this.dxf_resource_files = new Dictionary<int, Tuple<SimUserRole, string, string, int, bool>>();
            this.dxf_nr_resource_files = 0;

            this.dxf_saved_resources_as_objects = false;

            this.dxf_current_rf_key = -1;
            this.dxf_current_rf_file_name = string.Empty;
            this.dxf_current_rf_full_path = string.Empty;
            this.dxf_current_rf_user = SimUserRole.ADMINISTRATOR;
            this.dxf_current_is_contained = false;

            this.dxf_current_nr_of_properties_expected = 3;
            this.dxf_current_nr_of_properties_read = 0;
        }

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)AssetSaveCode.WORKING_DIR:
                    this.dxf_WorkingDirectory = this.Decoder.FValue;
                    break;
                case (int)AssetSaveCode.WORKING_PATHS:
                    this.dxf_nr_paths_to_resource_files = this.Decoder.IntValue();
                    break;
                case (int)AssetSaveCode.APATH_COLLECTION:
                    this.dxf_nr_resource_files = this.Decoder.IntValue();
                    break;
                case (int)AssetSaveCode.APATHS_AS_OBJECTS:
                    this.dxf_saved_resources_as_objects = this.Decoder.FValue == "1";
                    break;
                case (int)AssetSaveCode.APATH_USER:
                    if (this.dxf_nr_resource_files > this.dxf_resource_files.Count)
                    {
                        this.dxf_current_rf_user = ComponentUtils.StringToComponentManagerType(this.Decoder.FValue);
                        this.dxf_current_nr_of_properties_read++;
                        this.SetCurrent();
                    }
                    break;
                case (int)AssetSaveCode.APATH_KEY:
                    if (this.dxf_nr_resource_files > this.dxf_resource_files.Count)
                    {
                        this.dxf_current_rf_key = this.Decoder.IntValue();
                        this.dxf_current_nr_of_properties_read++;
                        this.SetCurrent();
                    }
                    break;
                case (int)AssetSaveCode.APATH_ISCONTAINED:
                    if (this.dxf_nr_resource_files > this.dxf_resource_files.Count)
                    {
                        this.dxf_current_nr_of_properties_expected = 4;
                        this.dxf_current_is_contained = this.Decoder.IntValue() == 1;
                        this.dxf_current_nr_of_properties_read++;
                        this.SetCurrent();
                    }
                    break;
                case (int)AssetSaveCode.APATH_REL_PATH:
                    if (this.dxf_nr_resource_files > this.dxf_resource_files.Count)
                    {
                        this.dxf_current_rf_file_name = this.Decoder.FValue;
                        this.dxf_current_nr_of_properties_read++;
                        this.SetCurrent();
                    }
                    break;
                case (int)AssetSaveCode.APATH_FULL_PATH:
                    if (this.dxf_nr_resource_files > this.dxf_resource_files.Count)
                    {
                        this.dxf_current_nr_of_properties_expected = 5;
                        this.dxf_current_rf_full_path = this.Decoder.FValue;
                        this.dxf_current_nr_of_properties_read++;
                        this.SetCurrent();
                    }
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_paths_to_resource_files > this.dxf_paths_to_resource_files.Count)
                    {
                        this.dxf_paths_to_resource_files.Add(this.Decoder.FValue);
                        this.Decoder.ProjectData.AssetManager.PathsToResourceFiles.Add(this.Decoder.FValue); // moved from OnLoaded
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        private void SetCurrent()
        {
            if (this.dxf_current_nr_of_properties_read == this.dxf_current_nr_of_properties_expected)
            {
                this.dxf_resource_files.Add(this.dxf_current_rf_key, Tuple.Create(this.dxf_current_rf_user, this.dxf_current_rf_file_name, this.dxf_current_rf_full_path, this.dxf_current_rf_key, this.dxf_current_is_contained));
                this.dxf_current_rf_key = -1;
                this.dxf_current_rf_file_name = string.Empty;
                this.dxf_current_rf_full_path = string.Empty;
                this.dxf_current_rf_user = SimUserRole.ADMINISTRATOR;
                this.dxf_current_is_contained = false;

                this.dxf_current_nr_of_properties_expected = 3;
                this.dxf_current_nr_of_properties_read = 0;
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
                    DXFGeometricAsset sgA = sE as DXFGeometricAsset;
                    if (sgA != null)
                    {
                        this.Decoder.ProjectData.AssetManager.AddParsedGeometricAsset(sgA.dxf_referencing_component_ids, sgA.dxf_PathCodeToAsset, sgA.dxf_ContainedObjectId);
                        add_successful &= true;
                    }
                    DXFDocumentAsset sdA = sE as DXFDocumentAsset;
                    if (sdA != null)
                    {
                        this.Decoder.ProjectData.AssetManager.AddParsedDocumentAsset(sdA.dxf_referencing_component_ids, sdA.dxf_PathCodeToAsset, sdA.dxf_ContainedObjectId);
                        add_successful &= true;
                    }
                }
            }
            //Console.WriteLine("Asset Manager added SOMETHING");
            return add_successful;
        }

        #endregion

        #region OVERRIDES: Post-Processing
        /// <inheritdoc/>
        internal override void OnLoaded()
        {
            base.OnLoaded();

            // reset the working directory as the saved one, only if the currently set one is invalid
            if (!Directory.Exists(this.Decoder.ProjectData.AssetManager.WorkingDirectory))
                this.Decoder.ProjectData.AssetManager.WorkingDirectory = this.dxf_WorkingDirectory;

            // this is necessary only if the assets manager was saved using the old serialization method (before 3.1.2020)
            if (!this.dxf_saved_resources_as_objects)
            {
                foreach (var entry in this.dxf_resource_files)
                {
                    string path = (entry.Value.Item3 == AssetManager.PATH_NOT_FOUND) ? null : entry.Value.Item3;
                    this.Decoder.ProjectData.AssetManager.AddParsedUndifferentiatedResourceEntry(entry.Value.Item1, entry.Value.Item2, path, entry.Value.Item4, entry.Value.Item5);
                }
                this.Decoder.ProjectData.AssetManager.OrganizeResourceFileEntries();
            }
            else
            {
                // sync with lookup
                this.Decoder.ProjectData.AssetManager.SyncLookupAfterLoading();
            }
        }

        #endregion
    }
}
