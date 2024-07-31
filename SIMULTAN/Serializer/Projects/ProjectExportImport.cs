using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.GRDXF;
using SIMULTAN.Serializer.MVDXF;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Serializer.TXDXF;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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
        /// <param name="_file">The zip file that contains both the components and the values</param>
        /// <param name="projectData">The project's data</param>
        /// <param name="_components">The selected components for saving</param>
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
            // gather all relevant taxonomies
            var all_taxonomies = GetReferencedTaxonomies(all_components);

            var packDir = new DirectoryInfo(Path.Combine(_file.Directory.FullName, "~ComponentExport"));
            (_, var newName) = AdmissibilityQueries.DirectoryNameIsAdmissible(packDir, x => !Directory.Exists(x), "{0}{1}");
            packDir = new DirectoryInfo(newName);
            packDir.Create();

            // extract the file names
            string path_to_CompFile = Path.Combine(packDir.FullName, "ComponentExport_Components" + ParamStructFileExtensions.FILE_EXT_COMPONENTS);
            string path_to_MVFile = Path.Combine(packDir.FullName, "ComponentExport_Values" + ParamStructFileExtensions.FILE_EXT_MULTIVALUES);
            string path_to_TXFile = Path.Combine(packDir.FullName, "ComponentExport_Taxonomies" + ParamStructFileExtensions.FILE_EXT_TAXONOMY);
            List<FileInfo> files_to_pack = new List<FileInfo>();

            // export the values
            if (all_values.Any())
            {
                var mvFile = new FileInfo(path_to_MVFile);
                MultiValueDxfIO.Write(mvFile, all_values);
                files_to_pack.Add(mvFile);
            }

            // export taxonomies
            if (all_taxonomies.Any())
            {
                var txFile = new FileInfo(path_to_TXFile);
                SimTaxonomyDxfIO.Write(txFile, all_taxonomies, projectData);
                files_to_pack.Add(txFile);
            }

            // export the components
            var compFile = new FileInfo(path_to_CompFile);
            ComponentDxfIO.WriteLibrary(compFile, all_components, all_networks);
            files_to_pack.Add(compFile);

            // put both files in a Zip archive and delete them from the file system
            ZipUtils.CreateArchiveFrom(_file, new List<DirectoryInfo>(), files_to_pack, packDir.FullName);
            // clear pack dir
            packDir.Delete(true);
        }

        /// <summary>
        /// Gets the replacing taxonomy entry for a source entry.
        /// </summary>
        /// <param name="sourceEntry">The source entry of the import.</param>
        /// <param name="existingTaxonomies">The existing taxonomies.</param>
        /// <param name="existingEntry">The existing entry if one is found.</param>
        /// <returns>True if an existing entry was found.</returns>
        private static bool GetReplacingTaxonomyEntry(SimTaxonomyEntry sourceEntry, Dictionary<SimTaxonomy, SimTaxonomy> existingTaxonomies, out SimTaxonomyEntry existingEntry)
        {
            if (existingTaxonomies.TryGetValue(sourceEntry.Taxonomy, out var tax))
            {
                var replaceEntry = tax.GetTaxonomyEntryByKey(sourceEntry.Key);
                if (replaceEntry != null)
                {
                    existingEntry = replaceEntry;
                    return true;
                }
                existingEntry = null;
                return false;
            }
            existingEntry = null;
            return false;
        }

        /// <summary>
        /// Merges a component file (and a related values file) with the given project.
        /// </summary>
        /// <param name="_project">The project in which we are merging</param>
        /// <param name="_archive_file">The zip file from a previous export</param>
        public static SimComponent ImportComponentLibrary(HierarchicalProject _project, FileInfo _archive_file)
        {
            if (_project == null)
                throw new ArgumentNullException(nameof(_project));
            if (!File.Exists(_archive_file.FullName))
                throw new ArgumentException("The given file does not exist!");

            // 0. unpack the archive
            var unpackDir = new DirectoryInfo(Path.Combine(_archive_file.Directory.FullName, "~ComponentImport"));
            (_, var newName) = AdmissibilityQueries.DirectoryNameIsAdmissible(unpackDir, x => !Directory.Exists(x), "{0}{1}");
            unpackDir = new DirectoryInfo(newName);
            unpackDir.Create();
            var files_to_import = ZipUtils.UnpackArchive(_archive_file, unpackDir);
            FileInfo tfile = files_to_import.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_TAXONOMY));
            FileInfo vfile = files_to_import.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES));
            FileInfo cfile = files_to_import.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS));

            ExtendedProjectData mergeData = new ExtendedProjectData(_project.AllProjectDataManagers.SynchronizationContext, _project.AllProjectDataManagers.DispatcherTimerFactory);

            // load the taxonomies
            if (tfile != null)
            {
                DXFParserInfo info = new DXFParserInfo(Guid.Empty, mergeData);
                mergeData.Taxonomies.SetCallingLocation(new DummyReferenceLocation(info.GlobalId));
                SimTaxonomyDxfIO.Read(tfile, info);
            }

            // 1a. reconstruct the values file
            if (vfile != null)
            {
                // 1b. load the values to a clean factory
                DXFParserInfo info = new DXFParserInfo(Guid.Empty, mergeData);
                MultiValueDxfIO.Read(vfile, info);

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
                ComponentDxfIO.ReadLibrary(cfile, new DXFParserInfo(Guid.Empty, mergeData));

                // 2b. restore interdependencies
                mergeData.Components.RemoveAllAssets();
                mergeData.NetworkManager.RemoveReferencesToGeometryWithinRecord();
                // if the import has no default taxonomies, load them first
                if (!mergeData.Taxonomies.Any())
                {
                    HierarchicalProject.LoadDefaultTaxonomies(mergeData);
                    mergeData.RestoreDefaultTaxonomyReferences();
                }

                // 2c. put all imported components into a new parent component to make the import recognizable
                List<SimComponent> to_transfer = new List<SimComponent>(mergeData.Components);

                import_parent = new SimComponent(_project.AllProjectDataManagers.UsersManager.CurrentUser.Role);
                var importTax = _project.AllProjectDataManagers.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Import);
                import_parent.Slots.Add(new SimTaxonomyEntryReference(importTax));

                foreach (SimComponent c in to_transfer)
                {
                    // define the slot in the parent
                    var new_slot = import_parent.Components.FindAvailableSlot(c.Slots[0].Target);
                    import_parent.Components.Add(new SimChildComponentEntry(new_slot, c));
                }

                // 2d. put a description into the imported networks to make them recognizable
                foreach (SimFlowNetwork nw in mergeData.NetworkManager.NetworkRecord)
                {
                    nw.Description += String.Format(" ({0})", cfile.FullName);
                }

                // merge taxonomies
                var existingTaxonomies = _project.AllProjectDataManagers.Taxonomies.Merge(mergeData.Taxonomies);

                // 3a. merge the values with the existing
                _project.AllProjectDataManagers.ValueManager.Merge(mergeData.ValueManager);

                // 3b. merge component and network records
                _project.AllProjectDataManagers.Components.Merge(new SimComponent[] { import_parent });
                _project.AllProjectDataManagers.NetworkManager.AddToRecord(mergeData.NetworkManager.NetworkRecord.ToList());

                // replace all taxonomy entries that already existed
                ComponentWalker.ForeachComponent(import_parent, (component) =>
                {
                    for (int i = 0; i < component.Slots.Count; i++)
                    {
                        var item = component.Slots[i];
                        if (GetReplacingTaxonomyEntry(item.Target, existingTaxonomies, out var replaceEntry))
                        {
                            component.Slots.Insert(i + 1, new SimTaxonomyEntryReference(replaceEntry));
                            component.Slots.RemoveAt(i);
                        }
                    }

                    foreach (var child in component.Components)
                    {
                        if (GetReplacingTaxonomyEntry(child.Slot.SlotBase.Target, existingTaxonomies, out var replaceEntry))
                        {
                            child.Slot = new SimSlot(new SimTaxonomyEntryReference(replaceEntry), child.Slot.SlotExtension);
                        }
                    }

                    foreach (var reference in component.ReferencedComponents)
                    {
                        if (GetReplacingTaxonomyEntry(reference.Slot.SlotBase.Target, existingTaxonomies, out var replaceEntry))
                        {
                            reference.Slot = new SimSlot(new SimTaxonomyEntryReference(replaceEntry), reference.Slot.SlotExtension);
                        }
                    }

                    foreach (var param in component.Parameters)
                    {
                        if (param.NameTaxonomyEntry.HasTaxonomyEntry)
                        {
                            if (GetReplacingTaxonomyEntry(param.NameTaxonomyEntry.TaxonomyEntryReference.Target, existingTaxonomies, out var replaceEntry))
                            {
                                param.NameTaxonomyEntry = new SimTaxonomyEntryOrString(replaceEntry);
                            }
                        }
                    }
                });

                // ToDo: remove, only for testing
                /*
                ComponentWalker.ForeachComponent(import_parent, (component) =>
                {
                    Debug.Assert(component.Slots.All(x => x.Target.Factory != null &&
                        x.Target.Factory == _project.AllProjectDataManagers.Taxonomies));
                    Debug.Assert(component.Components.All(x => x.Slot.SlotBase.Target.Factory != null &&
                        x.Slot.SlotBase.Target.Factory == _project.AllProjectDataManagers.Taxonomies));
                    Debug.Assert(component.ReferencedComponents.All(x => x.Slot.SlotBase.Target.Factory != null &&
                        x.Slot.SlotBase.Target.Factory == _project.AllProjectDataManagers.Taxonomies));
                    Debug.Assert(component.Parameters.All(x => !x.NameTaxonomyEntry.HasTaxonomyEntry ||
                        x.NameTaxonomyEntry.TaxonomyEntryReference.Target.Factory != null &&
                        x.NameTaxonomyEntry.TaxonomyEntryReference.Target.Factory == _project.AllProjectDataManagers.Taxonomies));
                });
                */
            }

            // delete the unpacked files
            unpackDir.Delete(true);

            return import_parent;
        }


        /// <summary>
        /// Exports the given MultiValues to a file.
        /// </summary>
        /// <param name="_file">The file</param>
        /// <param name="_value_factory">The value factory of the selected values</param>
        /// <param name="_values">The values to export</param>
        public static void ExportMultiValueLibrary(FileInfo _file, SimMultiValueCollection _value_factory, IEnumerable<SimMultiValue> _values)
        {
            if (string.IsNullOrEmpty(_file.Extension))
                throw new ArgumentException("The file has no valid extension!");
            if (_values == null)
                throw new ArgumentNullException(nameof(_values));

            // export the values
            if (_values.Count() > 0)
            {
                MultiValueDxfIO.Write(_file, _values);
            }
        }

        /// <summary>
        /// Merges the values contained in the given file with the ones in the project.
        /// </summary>
        /// <param name="_project">The project in which to merge</param>
        /// <param name="_value_file">The file containing the values to merge</param>
        public static void ImportMultiValueLibrary(HierarchicalProject _project, FileInfo _value_file)
        {
            if (_project == null)
                throw new ArgumentNullException(nameof(_project));
            if (!File.Exists(_value_file.FullName))
                throw new ArgumentException("The given file does not exist!");

            // 1a. reconstruct the values file
            var projectData = new ExtendedProjectData(_project.AllProjectDataManagers.SynchronizationContext, _project.AllProjectDataManagers.DispatcherTimerFactory);
            if (_value_file.Exists)
            {
                // 1b. load the values to a clean factory                
                MultiValueDxfIO.Read(_value_file, new DXFParserInfo(_project.GlobalID, projectData));

                // 1c. add the import description to their name
                foreach (SimMultiValue mv in projectData.ValueManager)
                {
                    mv.Name += String.Format(" ({0})", _value_file.FullName);
                }
            }

            // 3a. merge the values with the existing
            _project.AllProjectDataManagers.ValueManager.Merge(projectData.ValueManager);
        }

        /// <summary>
        /// Saves the given geometry files (.simgeo), their <see cref="SimGeometryRelation"/>s and <see cref="SimTaxonomy"/>.
        /// Also writes the associated <see cref="GeometryRelationsFileMapping"/> for all relevant geometry files.
        /// </summary>
        /// <param name="file">The zip file that contains both the components and the values</param>
        /// <param name="projectData">The project's data</param>
        /// <param name="geometryFiles">The selected geometry files for saving</param>
        public static void ExportGeometryWithRelations(FileInfo file, ExtendedProjectData projectData, IEnumerable<ResourceEntry> geometryFiles)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (geometryFiles == null)
                throw new ArgumentNullException(nameof(geometryFiles));
            if (!geometryFiles.Any())
                throw new ArgumentException("No geometry files provided");
            if (geometryFiles.Any(x => x.CurrentFullPath == AssetManager.PATH_NOT_FOUND))
                throw new ArgumentException("Some of the geometry files paths are not found");
            if (geometryFiles.Any(x => !x.CurrentFullPath.EndsWith(ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL)))
                throw new ArgumentException("Files contain non geometry files");

            if (string.IsNullOrEmpty(file.Extension))
                throw new ArgumentException("The file has no valid extension!");

            var resKeys = geometryFiles.Select(x => x.Key).ToHashSet();
            // only export relations that have source and target in the selected files
            var relations = projectData.GeometryRelations.Where(x => resKeys.Contains(x.Source.FileId) && resKeys.Contains(x.Target.FileId));
            // all referenced taxonomies of the relations
            var taxonomies = relations.Where(x => x.RelationType != null).Select(x => x.RelationType.Target.Taxonomy).Distinct();

            // extract the file names
            var packDir = new DirectoryInfo(Path.Combine(file.Directory.FullName, "~GeometryExport"));
            (_, var newName) = AdmissibilityQueries.DirectoryNameIsAdmissible(packDir, x => !Directory.Exists(x), "{0}{1}");
            packDir = new DirectoryInfo(newName);
            packDir.Create();
            string path_to_GRFile = Path.Combine(packDir.FullName, "GeometryExport_Relations" + ParamStructFileExtensions.FILE_EXT_GEOMETRY_RELATIONS);
            string path_to_TXFile = Path.Combine(packDir.FullName, "GeometryExport_Taxonomies" + ParamStructFileExtensions.FILE_EXT_TAXONOMY);
            string path_to_GRFMFile = Path.Combine(packDir.FullName, "GeometryExport_RelationsMappings" + ParamStructFileExtensions.FILE_EXT_GEOMETRY_RELATIONS_FILE_MAPPINGS);
            var files_to_pack = new List<FileInfo>();
            var mappings = new List<GeometryRelationsFileMapping>();

            // copy geometry files to pack them, no additional export necessary
            foreach (var geoFile in geometryFiles)
            {
                var fi = new FileInfo(geoFile.CurrentFullPath);
                mappings.Add(new GeometryRelationsFileMapping(geoFile.Key, fi.Name));
                if (fi.Exists)
                {
                    var dest = Path.Combine(packDir.FullName, fi.Name);
                    fi.CopyTo(dest);
                    files_to_pack.Add(new FileInfo(dest));
                }
                else
                {
                    throw new Exception("Geometry file does not exist");
                }
            }

            // export relations
            if (relations.Any())
            {
                var grFile = new FileInfo(path_to_GRFile);
                SimGeometryRelationsDxfIO.Write(grFile, relations);
                files_to_pack.Add(grFile);
            }

            // export mappings
            if (mappings.Any())
            {
                var grfmFile = new FileInfo(path_to_GRFMFile);
                GeometryRelationsFileMappingDxfIO.Write(grfmFile, mappings);
                files_to_pack.Add(grfmFile);
            }

            // export taxonomies
            if (taxonomies.Any())
            {
                var txFile = new FileInfo(path_to_TXFile);
                SimTaxonomyDxfIO.Write(txFile, taxonomies, projectData);
                files_to_pack.Add(txFile);
            }

            // put both files in a Zip archive and delete them from the file system
            ZipUtils.CreateArchiveFrom(file, new List<DirectoryInfo>(), files_to_pack, packDir.FullName);
            // clean up files
            packDir.Delete(true);
        }

        /// <summary>
        /// Imports a geometry with relations archive and merges it with the existing project.
        /// </summary>
        /// <param name="_project">The project in which we are merging</param>
        /// <param name="_archive_file">The zip file from a previous export</param>
        /// <param name="_target_directory">The target directory in the project</param>
        /// <param name="_nameCollisionFormat">The name collision format, needs to format places</param>
        /// <returns>A list of potential errors that occurred during the import migration.</returns>
        public static List<SimGeoIOError> ImportGeometryWithRelations(HierarchicalProject _project, FileInfo _archive_file, DirectoryInfo _target_directory,
            string _nameCollisionFormat)
        {
            if (_project == null)
                throw new ArgumentNullException(nameof(_project));
            if (!File.Exists(_archive_file.FullName))
                throw new ArgumentException("The given file does not exist!");
            if (!(_project.ProjectUnpackFolder.FullName == _target_directory.FullName ||
                FileSystemNavigation.IsSubdirectoryOf(_project.ProjectUnpackFolder.FullName, _target_directory.FullName, false)))
                throw new ArgumentException("Target folder must be inside the project unpack folder");

            // 0. unpack the archive
            var unpackDir = new DirectoryInfo(Path.Combine(_archive_file.Directory.FullName, "~GeometryImport"));
            (_, var newName) = AdmissibilityQueries.DirectoryNameIsAdmissible(unpackDir, x => !Directory.Exists(x), "{0}{1}");
            unpackDir = new DirectoryInfo(newName);
            unpackDir.Create();
            // unpack and get files
            var files_to_import = ZipUtils.UnpackArchive(_archive_file, unpackDir);
            FileInfo tfile = files_to_import.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_TAXONOMY));
            FileInfo grfile = files_to_import.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_RELATIONS));
            FileInfo grfmfile = files_to_import.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_RELATIONS_FILE_MAPPINGS));
            var simgeoFiles = files_to_import.Where(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL));

            ExtendedProjectData mergeData = new ExtendedProjectData(_project.AllProjectDataManagers.SynchronizationContext, _project.AllProjectDataManagers.DispatcherTimerFactory);

            // load the taxonomies
            if (tfile != null)
            {
                DXFParserInfo info = new DXFParserInfo(Guid.Empty, mergeData);
                mergeData.Taxonomies.SetCallingLocation(new DummyReferenceLocation(info.GlobalId));
                SimTaxonomyDxfIO.Read(tfile, info);
            }

            // import mappings (old file Ids to filename)
            var mappings = new List<GeometryRelationsFileMapping>();
            if (grfmfile != null)
            {
                DXFParserInfo info = new DXFParserInfo(Guid.Empty, mergeData);
                mappings = GeometryRelationsFileMappingDxfIO.Read(grfmfile, info);
            }
            var mappingsLookup = mappings.ToDictionary(x => x.FileId, x => x.FilePath);

            // import geometry relations
            if (grfile != null)
            {
                DXFParserInfo info = new DXFParserInfo(Guid.Empty, mergeData);
                mergeData.GeometryRelations.SetCallingLocation(new DummyReferenceLocation(info.GlobalId));
                SimGeometryRelationsDxfIO.Read(grfile, info);
            }

            // import geometry files as new resources
            var resourceLookup = new Dictionary<string, ResourceEntry>();
            foreach (var geofile in simgeoFiles)
            {
                var resource = _project.CopyResourceAsContainedFileEntry(geofile, _target_directory, _nameCollisionFormat);
                resourceLookup.Add(geofile.Name, resource);
            }

            // generate lookup for changed file ids (old exported file ids -> new resource file ids)
            var idLookup = new Dictionary<int, int>();
            foreach (var idmap in mappingsLookup)
            {
                var resEntry = resourceLookup[idmap.Value];
                idLookup.Add(idmap.Key, resEntry.Key);
            }

            // Merge the taxonomies
            var existingTaxonomies = _project.AllProjectDataManagers.Taxonomies.Merge(mergeData.Taxonomies);

            // migrate the geometry relations from the import to the existing project
            foreach (var relation in mergeData.GeometryRelations)
            {
                // find filename in mappings by the id
                var sourcePath = new FileInfo(mappingsLookup[relation.Source.FileId]);
                var targetPath = new FileInfo(mappingsLookup[relation.Target.FileId]);
                // find the newly created resource with the same name (name is from the import before it as put into the project)
                var sourceResource = resourceLookup[sourcePath.Name];
                var targetResource = resourceLookup[targetPath.Name];

                // recreate source/target
                var sourceRef = new SimBaseGeometryReference(_project.GlobalID, sourceResource.Key, relation.Source.BaseGeometryId);
                var targetRef = new SimBaseGeometryReference(_project.GlobalID, targetResource.Key, relation.Target.BaseGeometryId);

                SimTaxonomyEntryReference relationType = null;
                // migrate relation type if needed
                if (relation.RelationType != null)
                {
                    // check if already existed
                    if (GetReplacingTaxonomyEntry(relation.RelationType.Target, existingTaxonomies, out var replaceEntry))
                    {
                        relationType = new SimTaxonomyEntryReference(replaceEntry);
                    }
                    // otherwise use it directly as the taxonomy entry was already migrated
                    else
                    {
                        relationType = new SimTaxonomyEntryReference(relation.RelationType.Target);
                    }
                }

                // create new migrated relation
                var newRelation = new SimGeometryRelation(relationType, sourceRef, targetRef);
                _project.AllProjectDataManagers.GeometryRelations.Add(newRelation);
            }

            // migrate all unpacked files to update their linked file ids, also upgrades older files to use ids instead of file names
            var resourcePathLookup = resourceLookup.ToDictionary(x => x.Key, x => x.Value.Name);
            var errors = new List<SimGeoIOError>();
            var resourceFiles = resourceLookup.Values.OfType<ResourceFileEntry>();
            SimGeoIO.MigrateAfterImport(resourceFiles, _project.AllProjectDataManagers, errors, idLookup, resourcePathLookup);

            // unload the models again cause they get loaded on migration
            foreach (var res in resourceLookup.Values)
            {
                var rfe = (ResourceFileEntry)res;
                if (_project.AllProjectDataManagers.GeometryModels.TryGetGeometryModel(rfe, out var geoModel))
                {
                    _project.AllProjectDataManagers.GeometryModels.RemoveGeometryModel(geoModel);
                }
            }

            // delete unpack dir and unpacked files
            unpackDir.Delete(true);

            return errors;
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
        /// <param name="_components">The selected components</param>
        /// <returns>All components</returns>
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
        /// <param name="component">The calling component</param>
        /// <param name="result">The references found so far</param>
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
        /// <param name="components">The selected components</param>
        /// <returns>All found networks</returns>
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
                if (!(param is SimEnumParameter) && param.ValueSource != null && param.ValueSource is SimMultiValueParameterSource mvP &&
                    !result.Contains(mvP.ValueField))
                {
                    result.Add(mvP.ValueField);
                }
            }
        }

        /// <summary>
        /// Gets the referenced taxonomies of the component tree.
        /// </summary>
        /// <param name="components">The root components.</param>
        internal static IEnumerable<SimTaxonomy> GetReferencedTaxonomies(IEnumerable<SimComponent> components)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            HashSet<SimTaxonomy> values = new HashSet<SimTaxonomy>();
            foreach (var c in components)
            {
                GetReferencedTaxonomies(c, values);
            }

            return values;
        }

        /// <summary>
        /// Gets the referenced taxonomies of the component tree.
        /// </summary>
        /// <param name="rootComponent">The root component.</param>
        /// <param name="result">The referenced taxonomies.</param>
        private static void GetReferencedTaxonomies(SimComponent rootComponent, HashSet<SimTaxonomy> result)
        {
            // Check whole component tree
            ComponentWalker.ForeachComponent(rootComponent, component =>
            {
                foreach (var slot in component.Slots)
                {
                    if (component.ParentContainer == null && !result.Contains(slot.Target.Taxonomy))
                    {
                        result.Add(slot.Target.Taxonomy);
                    }
                }
                // also check the child entry slots cause they could have no Target component
                foreach (var child in component.Components)
                {
                    if (!result.Contains(child.Slot.SlotBase.Target.Taxonomy))
                        result.Add(child.Slot.SlotBase.Target.Taxonomy);
                }

                // check component reference slots
                foreach (var compref in component.ReferencedComponents)
                {
                    if (!result.Contains(compref.Slot.SlotBase.Target.Taxonomy))
                        result.Add(compref.Slot.SlotBase.Target.Taxonomy);
                }

                // check parameters
                foreach (var param in component.Parameters)
                {
                    if (param.NameTaxonomyEntry.HasTaxonomyEntry)
                    {
                        var tax = param.NameTaxonomyEntry.TaxonomyEntryReference.Target.Taxonomy;
                        if (!result.Contains(tax))
                            result.Add(tax);
                    }
                }
            });
        }

        #endregion
    }
}
