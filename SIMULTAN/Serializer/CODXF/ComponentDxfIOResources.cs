using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.CODXF
{
    /// <summary>
    /// Provides methods for serializing resources and assets to a component file
    /// </summary>
    public static class ComponentDxfIOResources
    {
        #region Syntax Resources

        /// <summary>
        /// Syntax for a <see cref="ContainedResourceFileEntry"/>
        /// </summary>
        internal static DXFEntityParserElementBase<ResourceEntry> ContainedResourceFileEntityElement =
            new DXFComplexEntityParserElement<ResourceEntry>(
                new DXFEntityParserElement<ResourceEntry>(ParamStructTypes.RESOURCE_FILE,
                    (data, info) => ParseContainedResourceFileEntry(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<SimUserRole>(ResourceSaveCode.RESOURCE_USER),
                        new DXFSingleEntryParserElement<int>(ResourceSaveCode.RESOURCE_KEY),
                        new DXFSingleEntryParserElement<string>(ResourceSaveCode.RESOURCE_RELATIVE_PATH),
                        new DXFSingleEntryParserElement<SimComponentVisibility>(ResourceSaveCode.RESOURCE_VISIBILITY),
                    }));

        /// <summary>
        /// Syntax for a <see cref="LinkedResourceFileEntry"/>
        /// </summary>
        internal static DXFEntityParserElementBase<ResourceEntry> LinkedResourceFileEntityElement =
            new DXFComplexEntityParserElement<ResourceEntry>(
                new DXFEntityParserElement<ResourceEntry>(ParamStructTypes.RESOURCE_LINK,
                    (data, info) => ParseLinkedResourceFileEntry(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<SimUserRole>(ResourceSaveCode.RESOURCE_USER),
                        new DXFSingleEntryParserElement<int>(ResourceSaveCode.RESOURCE_KEY),
                        new DXFSingleEntryParserElement<string>(ResourceSaveCode.RESOURCE_RELATIVE_PATH),
                        new DXFSingleEntryParserElement<SimComponentVisibility>(ResourceSaveCode.RESOURCE_VISIBILITY),
                    }));


        /// <summary>
        /// Syntax for a <see cref="ResourceDirectoryEntry"/>
        /// </summary>
        internal static DXFEntityParserElementBase<ResourceEntry> ResourceDirectoryEntityElement =
            new DXFComplexEntityParserElement<ResourceEntry>(
                new DXFEntityParserElement<ResourceEntry>(ParamStructTypes.RESOURCE_DIR,
                    (data, info) => ParseResourceDirectoryEntry(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<SimUserRole>(ResourceSaveCode.RESOURCE_USER),
                        new DXFSingleEntryParserElement<int>(ResourceSaveCode.RESOURCE_KEY),
                        new DXFSingleEntryParserElement<string>(ResourceSaveCode.RESOURCE_RELATIVE_PATH),
                        new DXFSingleEntryParserElement<SimComponentVisibility>(ResourceSaveCode.RESOURCE_VISIBILITY),
                        new DXFEntitySequenceEntryParserElement<ResourceEntry>(ResourceSaveCode.RESOURCE_CHILDREN,
                            new DXFEntityParserElementBase<ResourceEntry>[]
                            {
                                new DXFRecursiveEntityParserElement<ResourceEntry>(ParamStructTypes.RESOURCE_DIR, "ResourceDirectory"),
                                ContainedResourceFileEntityElement,
                                LinkedResourceFileEntityElement
                            })
                    }))
            {
                Identifier = "ResourceDirectory"
            };

        #endregion

        #region Syntax Assets

        /// <summary>
        /// Syntax for a <see cref="DocumentAsset"/>
        /// </summary>
        internal static DXFEntityParserElementBase<Asset> DocumentAssetEntityElement =
            new DXFEntityParserElement<Asset>(ParamStructTypes.ASSET_DOCU,
                (data, info) => ParseDocumentAsset(data, info),
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<int>(AssetSaveCode.RESOURCE_KEY),
                    new DXFSingleEntryParserElement<string>(AssetSaveCode.CONTENT),
                    new DXFStructArrayEntryParserElement<SimId>(AssetSaveCode.REFERENCE_COL, ParseAssetComponentId,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<long>(AssetSaveCode.REFERENCE_LOCALID),
                            new DXFSingleEntryParserElement<Guid>(AssetSaveCode.REFERENCE_GLOBALID) { MinVersion = 12 }
                        })
                });

        /// <summary>
        /// Syntax for a <see cref="GeometricAsset"/>
        /// </summary>
        internal static DXFEntityParserElementBase<Asset> GeometricAssetEntityElement =
            new DXFEntityParserElement<Asset>(ParamStructTypes.ASSET_GEOM,
                (data, info) => ParseGeometricAsset(data, info),
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<int>(AssetSaveCode.RESOURCE_KEY),
                    new DXFSingleEntryParserElement<string>(AssetSaveCode.CONTENT),
                    new DXFStructArrayEntryParserElement<SimId>(AssetSaveCode.REFERENCE_COL, ParseAssetComponentId,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<long>(AssetSaveCode.REFERENCE_LOCALID),
                            new DXFSingleEntryParserElement<Guid>(AssetSaveCode.REFERENCE_GLOBALID) { MinVersion = 12 }
                        })
                });

        #endregion

        #region Syntax AssetManager

        /// <summary>
        /// Syntax for an <see cref="AssetManager"/>
        /// </summary>
        internal static DXFEntityParserElementBase<AssetManager> AssetManagerEntityElement =
            new DXFComplexEntityParserElement<AssetManager>(
                new DXFEntityParserElement<AssetManager>(ParamStructTypes.ASSET_MANAGER,
                    (data, info) => ParseAssetManager(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFEntitySequenceEntryParserElement<ResourceEntry>(AssetSaveCode.APATH_COLLECTION,
                            new DXFEntityParserElementBase<ResourceEntry>[]
                            {
                                ResourceDirectoryEntityElement,
                                ContainedResourceFileEntityElement,
                                LinkedResourceFileEntityElement
                            }),
                        new DXFEntitySequenceEntryParserElement<Asset>(AssetSaveCode.ASSET_COLLECTION,
                            new DXFEntityParserElementBase<Asset>[]
                            {
                                DocumentAssetEntityElement,
                                GeometricAssetEntityElement
                            })                        
                    }));

        /// <summary>
        /// Syntax for a asset section
        /// </summary>
        internal static DXFSectionParserElement<AssetManager> AssetsSectionElement =
            new DXFSectionParserElement<AssetManager>(ParamStructTypes.ASSET_SECTION,
                new DXFEntityParserElementBase<AssetManager>[]
                {
                    AssetManagerEntityElement
                });

        #endregion

        /// <summary>
        /// Writes a asset section to the DXF stream
        /// </summary>
        /// <param name="resources">The resources to serialize</param>
        /// <param name="assets">The assets to serialize</param>
        /// <param name="resourceFilter">Filter to decide which resources should be serialized. Call for root elements as well as for sub resources</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteAssetsSection(IEnumerable<ResourceEntry> resources,
            IEnumerable<Asset> assets, Predicate<ResourceEntry> resourceFilter, DXFStreamWriter writer)
        {
            writer.StartSection(ParamStructTypes.ASSET_SECTION);

            WriteAssetManager(resources, resourceFilter, assets, writer);

            writer.EndSection();
        }

        /// <summary>
        /// Reads a asset/resource section. The results are stored in <see cref="DXFParserInfo.ProjectData"/>
        /// </summary>
        /// <param name="reader">The DXF reader to read from</param>
        /// <param name="info">Info for the parser</param>
        internal static void ReadAssetSection(DXFStreamReader reader, DXFParserInfo info)
        {
            AssetsSectionElement.Parse(reader, info);
        }


        #region AssetManager

        /// <summary>
        /// Writes an asset manager entity to the DXF stream
        /// </summary>
        /// <param name="resources">The resources to serialize</param>
        /// <param name="assets">The assets to serialize</param>
        /// <param name="resourceFilter">Filter to decide which resources should be serialized. Call for root elements as well as for sub resources</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteAssetManager(IEnumerable<ResourceEntry> resources,
            Predicate<ResourceEntry> resourceFilter,
            IEnumerable<Asset> assets, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.ASSET_MANAGER);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(AssetManager));

            writer.WriteEntitySequence(AssetSaveCode.APATH_COLLECTION, resources.Where(x => resourceFilter(x)),
                (i, iwriter) => WriteResourceEntry(i, iwriter, resourceFilter));
            writer.WriteEntitySequence(AssetSaveCode.ASSET_COLLECTION, assets, WriteAsset);

            writer.EndComplexEntity();
        }

        private static AssetManager ParseAssetManager(DXFParserResultSet data, DXFParserInfo info)
        {
            var resources = data.Get<ResourceEntry[]>(AssetSaveCode.APATH_COLLECTION, new ResourceEntry[] { });
            var assets = data.Get<Asset[]>(AssetSaveCode.ASSET_COLLECTION, new Asset[] { });

            foreach (var res in resources)
                info.ProjectData.AssetManager.AddParsedResource(res);

            info.ProjectData.AssetManager.SyncLookupAfterLoading();

            return info.ProjectData.AssetManager;
        }

        #endregion

        #region ResourceEntry

        /// <summary>
        /// Writes a resource entry to the DXF stream. This method calls either <see cref="WriteResourceDirectoryEntry"/>, 
        /// <see cref="WriteContainedResourceFileEntry"/>
        /// or <see cref="WriteLinkedResourceFileEntry"/>
        /// </summary>
        /// <param name="resource">The instance to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        /// <param name="exportFilter">Filters which sub resources are serialized</param>
        internal static void WriteResourceEntry(ResourceEntry resource, DXFStreamWriter writer, Predicate<ResourceEntry> exportFilter)
        {
            if (resource is ResourceDirectoryEntry resDir)
                WriteResourceDirectoryEntry(resDir, writer, exportFilter);
            else if (resource is ContainedResourceFileEntry containedFile)
                WriteContainedResourceFileEntry(containedFile, writer);
            else if (resource is LinkedResourceFileEntry linkedFile)
                WriteLinkedResourceFileEntry(linkedFile, writer);
        }

        private static void WriteResourceEntryCommon(ResourceEntry resource, DXFStreamWriter writer)
        {
            writer.Write(ResourceSaveCode.RESOURCE_USER, resource.UserWithWritingAccess);
            writer.Write(ResourceSaveCode.RESOURCE_KEY, resource.Key);
            writer.Write(ResourceSaveCode.RESOURCE_RELATIVE_PATH, resource.CurrentRelativePath);
            writer.Write(ResourceSaveCode.RESOURCE_VISIBILITY, resource.Visibility);
        }

        #endregion

        #region ResourceDirectoryEntry

        /// <summary>
        /// Writes an resource directory to the DXF stream
        /// </summary>
        /// <param name="directory">The resource directory to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        /// <param name="exportFilter">Filters which sub resources are serialized</param>
        internal static void WriteResourceDirectoryEntry(ResourceDirectoryEntry directory, DXFStreamWriter writer,
            Predicate<ResourceEntry> exportFilter)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.RESOURCE_DIR);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(ResourceDirectoryEntry));

            WriteResourceEntryCommon(directory, writer);

            //Children
            writer.WriteEntitySequence(ResourceSaveCode.RESOURCE_CHILDREN, directory.Children.Where(x => exportFilter(x)),
                (i, iwriter) => WriteResourceEntry(i, iwriter, exportFilter));

            writer.EndComplexEntity();
        }

        private static ResourceDirectoryEntry ParseResourceDirectoryEntry(DXFParserResultSet data, DXFParserInfo info)
        {
            var user = data.Get<SimUserRole>(ResourceSaveCode.RESOURCE_USER, SimUserRole.ADMINISTRATOR);
            var key = data.Get<int>(ResourceSaveCode.RESOURCE_KEY, -1);
            var relPath = data.Get<string>(ResourceSaveCode.RESOURCE_RELATIVE_PATH, AssetManager.PATH_NOT_FOUND);
            var visibility = data.Get<SimComponentVisibility>(ResourceSaveCode.RESOURCE_VISIBILITY, SimComponentVisibility.AlwaysVisible);
            var children = data.Get<ResourceEntry[]>(ResourceSaveCode.RESOURCE_CHILDREN, new ResourceEntry[0]);

            var dir = info.ProjectData.AssetManager.ParseResourceDirectoryEntry(user, relPath, key, visibility);

            dir.Children.SuppressNotification = true;
            foreach (var child in children)
            {
                child.Parent = dir;
                dir.Children.Add(child);
            }
            dir.Children.SuppressNotification = false;

            return dir;
        }

        #endregion

        #region ContainedResourceFileEntry

        /// <summary>
        /// Writes a <see cref="ContainedResourceFileEntry"/> to the DXF stream
        /// </summary>
        /// <param name="fileEntry">The contained resource file to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteContainedResourceFileEntry(ContainedResourceFileEntry fileEntry, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.RESOURCE_FILE);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(ContainedResourceFileEntry));

            WriteResourceEntryCommon(fileEntry, writer);

            writer.EndComplexEntity();
        }

        private static ContainedResourceFileEntry ParseContainedResourceFileEntry(DXFParserResultSet data, DXFParserInfo info)
        {
            var user = data.Get<SimUserRole>(ResourceSaveCode.RESOURCE_USER, SimUserRole.ADMINISTRATOR);
            var key = data.Get<int>(ResourceSaveCode.RESOURCE_KEY, -1);
            var relPath = data.Get<string>(ResourceSaveCode.RESOURCE_RELATIVE_PATH, AssetManager.PATH_NOT_FOUND);
            var visibility = data.Get<SimComponentVisibility>(ResourceSaveCode.RESOURCE_VISIBILITY, SimComponentVisibility.AlwaysVisible);

            var file = info.ProjectData.AssetManager.ParseContainedResourceFileEntry(user, relPath, key, visibility);
            return file;
        }

        #endregion

        #region ContainedResourceFileEntry

        /// <summary>
        /// Writes a <see cref="LinkedResourceFileEntry"/> to the DXF stream
        /// </summary>
        /// <param name="linkedFile">The linked resource file to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteLinkedResourceFileEntry(LinkedResourceFileEntry linkedFile, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.RESOURCE_LINK);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(LinkedResourceFileEntry));

            WriteResourceEntryCommon(linkedFile, writer);

            writer.EndComplexEntity();
        }

        private static LinkedResourceFileEntry ParseLinkedResourceFileEntry(DXFParserResultSet data, DXFParserInfo info)
        {
            var user = data.Get<SimUserRole>(ResourceSaveCode.RESOURCE_USER, SimUserRole.ADMINISTRATOR);
            var key = data.Get<int>(ResourceSaveCode.RESOURCE_KEY, -1);
            var relPath = data.Get<string>(ResourceSaveCode.RESOURCE_RELATIVE_PATH, AssetManager.PATH_NOT_FOUND);
            var visibility = data.Get<SimComponentVisibility>(ResourceSaveCode.RESOURCE_VISIBILITY, SimComponentVisibility.AlwaysVisible);

            var file = info.ProjectData.AssetManager.ParseLinkedResourceFileEntry(user, relPath, key, visibility);
            return file;
        }

        #endregion

        #region Asset

        /// <summary>
        /// Writes an asset to the DXF stream. This method calls either <see cref="WriteDocumentAsset(DocumentAsset, DXFStreamWriter)"/>
        /// or <see cref="WriteGeometricAsset(GeometricAsset, DXFStreamWriter)"/>
        /// </summary>
        /// <param name="asset">The asset to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteAsset(Asset asset, DXFStreamWriter writer)
        {
            if (asset is DocumentAsset da)
                WriteDocumentAsset(da, writer);
            else if (asset is GeometricAsset ga)
                WriteGeometricAsset(ga, writer);
        }

        private static void WriteAssetCommon(Asset asset, string entityName, Type type, DXFStreamWriter writer)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, entityName);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, type);

            writer.Write(AssetSaveCode.RESOURCE_KEY, asset.ResourceKey);
            writer.Write(AssetSaveCode.CONTENT, asset.ContainedObjectId);
            writer.WriteArray(AssetSaveCode.REFERENCE_COL, asset.ReferencingComponentIds, (id, iwriter) =>
            {
                iwriter.Write(AssetSaveCode.REFERENCE_LOCALID, id);
                iwriter.Write(AssetSaveCode.REFERENCE_GLOBALID, Guid.Empty);
            });
        }

        /// <summary>
        /// Writes a document asset to the DXF stream
        /// </summary>
        /// <param name="asset">The asset to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteDocumentAsset(DocumentAsset asset, DXFStreamWriter writer)
        {
            WriteAssetCommon(asset, ParamStructTypes.ASSET_DOCU, typeof(DocumentAsset), writer);
        }
        /// <summary>
        /// Writes a geometric asset to the DXF stream
        /// </summary>
        /// <param name="asset">The asset to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteGeometricAsset(GeometricAsset asset, DXFStreamWriter writer)
        {
            WriteAssetCommon(asset, ParamStructTypes.ASSET_GEOM, typeof(GeometricAsset), writer);
        }

        private static DocumentAsset ParseDocumentAsset(DXFParserResultSet data, DXFParserInfo info)
        {
            var key = data.Get<int>(AssetSaveCode.RESOURCE_KEY, 0);
            var content = data.Get<string>(AssetSaveCode.CONTENT, string.Empty);
            var componentIds = data.Get<SimId[]>(AssetSaveCode.REFERENCE_COL, new SimId[] { });

            try
            {
                return info.ProjectData.AssetManager.AddParsedDocumentAsset(componentIds.Select(x => x.LocalId), key, content);
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load DocumentAsset with ResourceKey={0}\nException: {2}\nStackTrace:\n{3}",
                    key, e.Message, e.StackTrace
                    ));
            }

            return null;
        }

        private static GeometricAsset ParseGeometricAsset(DXFParserResultSet data, DXFParserInfo info)
        {
            var key = data.Get<int>(AssetSaveCode.RESOURCE_KEY, 0);
            var content = data.Get<string>(AssetSaveCode.CONTENT, string.Empty);
            var componentIds = data.Get<SimId[]>(AssetSaveCode.REFERENCE_COL, new SimId[] { });

            try
            {
                return info.ProjectData.AssetManager.AddParsedGeometricAsset(componentIds.Select(x => x.LocalId), key, content);
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load GeometricAsset with ResourceKey={0}\nException: {2}\nStackTrace:\n{3}",
                    key, e.Message, e.StackTrace
                    ));
            }

            return null;
        }

        private static SimId ParseAssetComponentId(DXFParserResultSet data, DXFParserInfo info)
        {
            return data.GetSimId(AssetSaveCode.REFERENCE_GLOBALID, AssetSaveCode.REFERENCE_LOCALID, info.GlobalId);
        }

        #endregion
    }
}
