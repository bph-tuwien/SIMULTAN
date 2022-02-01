using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Serializer.Projects
{
    /// <summary>
    /// Contains methods for importing and exporting parts of a project.
    /// </summary>
    public static class ProjectExportImport
    {
        /// <summary>
        /// Saves the given components and their respective referenced components and values.
        /// The name of the values file is derived from the given component file name.
        /// </summary>
        /// <param name="_file">the zip file that contains both the components and the values</param>
        /// <param name="projectData">The project's data</param>
        /// <param name="_components">the selected components for saving</param>
        public static void ExportComponentLibrary(FileInfo _file, ExtendedProjectData projectData, IEnumerable<SimComponent> _components)
        {
            if (_file == null)
                throw new ArgumentNullException(nameof(_file));
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (_components == null)
                throw new ArgumentNullException(nameof(_components));

            if (string.IsNullOrEmpty(_file.Extension))
                throw new ArgumentException("The file has no valid extension!");

            List<SimComponent> input_roots = ComponentStructure.FindMinimalForestOf(_components);

            // gather all relevant components
            (var all_components, var all_networks) = GetReferencedComponentsAndNetworks(_components);
            // gather all relevant values
            var all_values = GetReferencedMultiValues(all_components);

            // extract the file names
            string path_to_CompFile = Path.Combine(_file.Directory.FullName, "ComponentExport_Components" + ParamStructFileExtensions.FILE_EXT_COMPONENTS);
            string path_to_MVFile = Path.Combine(_file.Directory.FullName, "ComponentExport_Values" + ParamStructFileExtensions.FILE_EXT_MULTIVALUES);
            List<FileInfo> files_to_pack = new List<FileInfo>();

            // export the values
            if (all_values.Count() > 0)
            {
                StringBuilder export_v = SimMultiValueCollection.ExportSome(all_values, true);
                string content_v = export_v.ToString();
                using (FileStream fs = File.Create(path_to_MVFile))
                {
                    byte[] content_B = Encoding.UTF8.GetBytes(content_v);
                    fs.Write(content_B, 0, content_B.Length);
                }
                files_to_pack.Add(new FileInfo(path_to_MVFile));
            }

            // export the components
            StringBuilder export_c = projectData.Components.ExportSome(all_components, all_networks, true);
            string content_c = export_c.ToString();
            using (FileStream fs = File.Create(path_to_CompFile))
            {
                byte[] content_B = Encoding.UTF8.GetBytes(content_c);
                fs.Write(content_B, 0, content_B.Length);
            }
            files_to_pack.Add(new FileInfo(path_to_CompFile));

            // put both files in a Zip archive and delete them from the file system
            ZipUtils.CreateArchiveFrom(_file, new List<DirectoryInfo>(), files_to_pack, _file.DirectoryName);
            foreach (var f in files_to_pack)
            {
                f.Delete();
            }
        }

        /// <summary>
        /// Merges a component file (and a related values file) with the given project.
        /// </summary>
        /// <param name="_project">the project in which we are merging</param>
        /// <param name="_archive_file">the zip file from a previous export</param>
        public static SimComponent ImportComponentLibrary(HierarchicalProject _project, FileInfo _archive_file)
        {
            if (_project == null)
                throw new ArgumentNullException(nameof(_project));
            if (!File.Exists(_archive_file.FullName))
                throw new ArgumentException("The given file does not exist!");

            // 0. unpack the archive
            var files_to_import = ZipUtils.UnpackArchive(_archive_file, _archive_file.Directory);
            FileInfo vfile = files_to_import.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES));
            FileInfo cfile = files_to_import.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS));

            // 1a. reconstruct the values file
            ExtendedProjectData mergeData = new ExtendedProjectData();
            if (vfile != null)
            {
                // 1b. load the values to a clean factory                
                DXFDecoder dxf_decoder_V = new DXFDecoder(mergeData, DXFDecoderMode.MultiValue);
                dxf_decoder_V.LoadFromFile(vfile.FullName);

                // 1c. add the import description to their name
                foreach (SimMultiValue mv in mergeData.ValueManager)
                {
                    mv.Name += String.Format(" ({0})", _archive_file.FullName);
                }
            }

            SimComponent import_parent = null;

            // 2a. load the components and networks
            if (cfile != null)
            {
                DXFDecoder dxf_decoder_C = new DXFDecoder(mergeData, DXFDecoderMode.Components);
                dxf_decoder_C.LoadFromFile(cfile.FullName);
                mergeData.Components.RestoreReferences(mergeData.NetworkManager.GetAllNetworkElements());

                // 2b. restore interdependencies
                mergeData.AssetManager.ReleaseTmpParseRecord();
                mergeData.Components.RemoveAllAssets();
                mergeData.NetworkManager.RemoveReferencesToGeometryWithinRecord();

                // 2c. put all imported components into a new parent component to make the import recognizable
                List<SimComponent> to_transfer = new List<SimComponent>(mergeData.Components);

                import_parent = new SimComponent(_project.AllProjectDataManagers.UsersManager.CurrentUser.Role);
                import_parent.CurrentSlot = new SimSlotBase(ComponentUtils.COMP_SLOT_IMPORT);

                foreach (SimComponent c in to_transfer)
                {
                    // define the slot in the parent
                    var new_slot = import_parent.Components.FindAvailableSlot(c.CurrentSlot);
                    import_parent.Components.Add(new SimChildComponentEntry(new_slot, c));
                }

                // 2d. put a description into the imported networks to make them recognizable
                foreach (SimFlowNetwork nw in mergeData.NetworkManager.NetworkRecord)
                {
                    nw.Description += String.Format(" ({0})", cfile.FullName);
                }

                // 3a. merge the values with the existing
                _project.AllProjectDataManagers.ValueManager.Merge(mergeData.ValueManager);

                // 3b. merge component and network records
                _project.AllProjectDataManagers.Components.Merge(new SimComponent[] { import_parent });
                _project.AllProjectDataManagers.NetworkManager.AddToRecord(mergeData.NetworkManager.NetworkRecord.ToList());
            }

            // delete the unpacked files
            if (vfile != null)
                vfile.Delete();
            if (cfile != null)
                cfile.Delete();

            return import_parent;
        }


        /// <summary>
        /// Exports the given mltivalues to a file.
        /// </summary>
        /// <param name="_file">the file</param>
        /// <param name="_value_factory">the value factory of the selected values</param>
        /// <param name="_values">the values to export</param>
        public static void ExportMultiValueLibrary(FileInfo _file, SimMultiValueCollection _value_factory, IEnumerable<SimMultiValue> _values)
        {
            if (string.IsNullOrEmpty(_file.Extension))
                throw new ArgumentException("The file has no valid extension!");
            if (_values == null)
                throw new ArgumentNullException(nameof(_values));

            // export the values
            if (_values.Count() > 0)
            {
                StringBuilder export_v = SimMultiValueCollection.ExportSome(_values, true);
                string content_v = export_v.ToString();
                using (FileStream fs = File.Create(_file.FullName))
                {
                    byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content_v);
                    fs.Write(content_B, 0, content_B.Length);
                }
            }
        }

        /// <summary>
        /// Merges the values contained in the given file with the ones in the project.
        /// </summary>
        /// <param name="_project">the project in which to merge</param>
        /// <param name="_value_file">the file containing the values to merge</param>
        public static void ImportMultiValueLibrary(HierarchicalProject _project, FileInfo _value_file)
        {
            if (_project == null)
                throw new ArgumentNullException(nameof(_project));
            if (!File.Exists(_value_file.FullName))
                throw new ArgumentException("The given file does not exist!");

            // 1a. reconstruct the values file
            var projectData = new ExtendedProjectData();
            Dictionary<long, long> value_merge_record = new Dictionary<long, long>();
            if (File.Exists(_value_file.FullName))
            {
                // 1b. load the values to a clean factory                
                DXFDecoder dxf_decoder_V = new DXFDecoder(projectData, DXFDecoderMode.MultiValue);
                dxf_decoder_V.LoadFromFile(_value_file.FullName);

                // 1c. add the import description to their name
                foreach (SimMultiValue mv in projectData.ValueManager)
                {
                    mv.Name += String.Format(" ({0})", _value_file.FullName);
                }
            }

            // 3a. merge the values with the existing
            value_merge_record = _project.AllProjectDataManagers.ValueManager.Merge(projectData.ValueManager);
        }


        #region Helper Methods Components

        internal static (IEnumerable<SimComponent> components, IEnumerable<SimFlowNetwork> networks) GetReferencedComponentsAndNetworks(
            IEnumerable<SimComponent> _components)
        {
            if (_components == null)
                throw new ArgumentNullException(nameof(_components));

            HashSet<SimComponent> allComponents = new HashSet<SimComponent>(_components);
            HashSet<SimFlowNetwork> allNetworks = new HashSet<SimFlowNetwork>();

            List<SimComponent> addedComponents = new List<SimComponent>(_components);
            List<SimFlowNetwork> addedNetworks = new List<SimFlowNetwork>();

            //Find all affected components/networks regardless if parent-childs are included
            while (addedComponents.Count > 0)
            {
                //Find next level of references
                var foundComponents = GetAllReferencedComponents(addedComponents);
                var foundFlowNetworks = GetAllRelevantNetworks(addedComponents);
                addedComponents.Clear();

                foreach (var comp in foundComponents)
                {
                    if (!allComponents.Contains(comp))
                    {
                        allComponents.Add(comp);
                        addedComponents.Add(comp);
                    }
                }

                //Find networks
                addedNetworks.Clear();

                foreach (var nw in foundFlowNetworks)
                {
                    if (!allNetworks.Contains(nw))
                    {
                        allNetworks.Add(nw);
                        addedNetworks.Add(nw);
                    }
                }

                //Find network content
                foreach (var nw in addedNetworks)
                {
                    var nwComponents = nw.GetAllContent();
                    foreach (var nwComp in nwComponents)
                    {
                        if (!allComponents.Contains(nwComp))
                        {
                            allComponents.Add(nwComp);
                            addedComponents.Add(nwComp);
                        }
                    }
                }
            }

            //Remove children when parent is included too
            List<SimComponent> resultComponents = new List<SimComponent>();
            foreach (var comp in allComponents)
            {
                if (!ComponentWalker.GetParents(comp, false).Any(p => allComponents.Contains(p)))
                    resultComponents.Add(comp);
            }

            List<SimFlowNetwork> resultNetworks = new List<SimFlowNetwork>();
            foreach (var nw in allNetworks)
            {
                if (!GetParentNetworks(nw).Any(p => allNetworks.Contains(p)))
                    resultNetworks.Add(nw);
            }

            return (resultComponents, resultNetworks);
        }


        /// <summary>
        /// Merges the input components with all those referenced by them (reference of reference and circular reference included).
        /// All input components have to belong to the calling factory.
        /// </summary>
        /// <param name="_components">the selected components</param>
        /// <returns>all components</returns>
        internal static HashSet<SimComponent> GetAllReferencedComponents(IEnumerable<SimComponent> _components)
        {
            if (_components == null)
                throw new ArgumentNullException(nameof(_components));

            HashSet<SimComponent> result = new HashSet<SimComponent>(_components);

            //Gather the referenced components
            foreach (SimComponent c in _components)
            {
                FindAllReferences(c, result);
            }

            return result;
        }

        private static void FindAllReferences(SimComponent component, HashSet<SimComponent> result)
        {
            GetAllRefCompsOfRefComps(component, result);

            foreach (var subComponent in component.Components)
                if (subComponent.Component != null)
                    FindAllReferences(subComponent.Component, result);
        }

        /// <summary>
        /// Returns a list of all components the calling component references - directly or via 
        /// references of its referenced components. Robust even if circular referencing present.
        /// </summary>
        /// <param name="component">the calling component</param>
        /// <param name="result">the references found so far</param>
        private static void GetAllRefCompsOfRefComps(SimComponent component, HashSet<SimComponent> result)
        {
            foreach (var entry in component.ReferencedComponents)
            {
                SimComponent rComp = entry.Target;

                if (rComp != null && !result.Contains(rComp))
                {
                    result.Add(rComp);
                    GetAllRefCompsOfRefComps(rComp, result);
                }
            }
        }

        /// <summary>
        /// Extracts the distinct networks relating to the given components.
        /// </summary>
        /// <param name="components">the selected components</param>
        /// <returns>all found networks</returns>
        private static HashSet<SimFlowNetwork> GetAllRelevantNetworks(IEnumerable<SimComponent> components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            HashSet<SimFlowNetwork> result = new HashSet<SimFlowNetwork>();

            foreach (var comp in components)
                GetAllRelevantNetworks(comp, result);

            return result;
        }

        private static void GetAllRelevantNetworks(SimComponent component, HashSet<SimFlowNetwork> results)
        {
            foreach (var nwPlacements in component.Instances.SelectMany(x => x.Placements.OfType<SimInstancePlacementNetwork>()))
            {
                if (!results.Contains(nwPlacements.NetworkElement.Network))
                    results.Add(nwPlacements.NetworkElement.Network);
            }

            foreach (var child in component.Components.Where(x => x.Component != null))
                GetAllRelevantNetworks(child.Component, results);
        }

        private static IEnumerable<SimFlowNetwork> GetParentNetworks(SimFlowNetwork network)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));

            if (network.Parent != null)
            {
                yield return network.ParentNetwork;
                network = network.ParentNetwork;
            }
        }

        #endregion

        #region Helper Methods MultiValues

        /// <summary>
        /// Retrieves all value fields referenced by any of the parameters in the given components.
        /// </summary>
        /// <param name="components">The components</param>
        /// <returns>A collection of distinct value fields; can be empty but not Null</returns>
        internal static IEnumerable<SimMultiValue> GetReferencedMultiValues(IEnumerable<SimComponent> components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            HashSet<SimMultiValue> values = new HashSet<SimMultiValue>();
            foreach (SimComponent c in components)
            {
                GetReferencedMultiValues(c, values);
            }

            return values;
        }

        private static void GetReferencedMultiValues(SimComponent _comp, HashSet<SimMultiValue> result)
        {
            foreach (var param in ComponentWalker.GetFlatParameters(_comp))
            {
                if (param.MultiValuePointer != null && !result.Contains(param.MultiValuePointer.ValueField))
                    result.Add(param.MultiValuePointer.ValueField);
            }
        }

        #endregion
    }
}
