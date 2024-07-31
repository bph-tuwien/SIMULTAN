using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Serializer.CODXF
{
    /// <summary>
    /// Provides methods for serializing component, network, asset, resource and user-defined component list data into component files
    /// </summary>
    public static class ComponentDxfIO
    {
        //Used to load Excel files which do not have a version section. This should be removed in the future
        internal static ulong LastParsedFileVersion { get; private set; } = 0;

        #region Section Syntax

        /// <summary>
        /// An old section used for parameter visualization grids
        /// </summary>
        public static DXFSkipSectionParserElement ImportantSection = new DXFSkipSectionParserElement(ParamStructTypes.IMPORTANT_SECTION);

        #endregion

        #region Public Component DXF

        /// <summary>
        /// Exports the public components, assets and resources.
        /// </summary>
        /// 
        /// Public Components are all components where the <see cref="SimComponent.Visibility"/> of the root component is set
        /// to <see cref="SimComponentVisibility.AlwaysVisible"/>
        /// Public Assets are all assets which are referenced by a Public Component.
        /// 
        /// Public Resources are all resources that either are public themselves (<see cref="ResourceEntry.Visibility"/> 
        /// equals <see cref="SimComponentVisibility.AlwaysVisible"/>, or that are referenced by a Public Asset.
        /// In addition, all <see cref="ResourceDirectoryEntry"/> are public if they contain at least one public resource.
        /// 
        /// <param name="file">The target file</param>
        /// <param name="projectData">The project data to serialize</param>
        public static void WritePublic(FileInfo file, ProjectData projectData)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(fs))
                {
                    WritePublic(writer, projectData);
                }
            }
        }
        /// <summary>
        /// Exports the public components, assets and resources.
        /// </summary>
        /// 
        /// Public Components are all components where the <see cref="SimComponent.Visibility"/> of the root component is set
        /// to <see cref="SimComponentVisibility.AlwaysVisible"/>
        /// Public Assets are all assets which are referenced by a Public Component.
        /// 
        /// Public Resources are all resources that either are public themselves (<see cref="ResourceEntry.Visibility"/> 
        /// equals <see cref="SimComponentVisibility.AlwaysVisible"/>, or that are referenced by a Public Asset.
        /// In addition, all <see cref="ResourceDirectoryEntry"/> are public if they contain at least one public resource.
        /// 
        /// <param name="writer">DXFStreamWriter into which the data should be serialized</param>
        /// <param name="projectData">The project data to serialize</param>
        public static void WritePublic(DXFStreamWriter writer, ProjectData projectData)
        {
            //File header
            writer.WriteVersionSection();

            //Data
            HashSet<ResourceEntry> publicResources = new HashSet<ResourceEntry>();
            List<Asset> publicAssets = new List<Asset>();
            foreach (var res in projectData.AssetManager.Resources)
                GetPublicResources(res, false, publicResources);
            GetPublicAssets(projectData.AssetManager, publicAssets, publicResources);

            ComponentDxfIOResources.WriteAssetsSection(projectData.AssetManager.Resources,
                publicAssets,
                x => publicResources.Contains(x),
                writer);
            ComponentDxfIOComponents.WriteComponentSection(
                projectData.Components.Where(x => x.Visibility == SimComponentVisibility.AlwaysVisible),
                writer);

            //EOF
            writer.WriteEOF();
        }

        /// <summary>
        /// Reads the data from a public component file
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <param name="parserInfo">Info for the parser</param>
        public static void ReadPublic(FileInfo file, DXFParserInfo parserInfo)
        {
            using (FileStream stream = file.OpenRead())
            {
                if (stream.Length == 0)
                    return;

                using (DXFStreamReader reader = new DXFStreamReader(stream))
                {
                    ReadPublic(reader, parserInfo);
                }
            }
        }
        /// <summary>
        /// Reads the data from a public component file
        /// </summary>
        /// <param name="reader">The DXFStream from which the data should be read</param>
        /// <param name="parserInfo">Info for the parser</param>
        public static void ReadPublic(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            //Version section
            if (CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }

            LastParsedFileVersion = parserInfo.FileVersion;

            //Data section
            ComponentDxfIOResources.ReadAssetSection(reader, parserInfo);
            ComponentDxfIOComponents.ReadComponentSection(reader, parserInfo);

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.ProjectData.Components.RestoreReferences(parserInfo.ProjectData.NetworkManager.GetAllNetworkElements());
            parserInfo.ProjectData.AssetManager.ReleaseTmpParseRecord();
        }

        #endregion

        #region Component DXF

        /// <summary>
        /// Exports all components, assets, resources, networks and user-defined component lists
        /// </summary>
        /// <param name="file">The target file</param>
        /// <param name="projectData">The data to export</param>
        public static void Write(FileInfo file, ProjectData projectData)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(fs))
                {
                    Write(writer, projectData);
                }
            }
        }
        /// <summary>
        /// Exports all components, assets, resources, networks and user-defined component lists
        /// </summary>
        /// <param name="writer">DXFStreamWriter into which the data should be serialized</param>
        /// <param name="projectData">The data to export</param>
        public static void Write(DXFStreamWriter writer, ProjectData projectData)
        {
            //File header
            writer.WriteVersionSection();

            //Data
            ComponentDxfIOResources.WriteAssetsSection(projectData.AssetManager.Resources,
                projectData.AssetManager.Assets.Values.SelectMany(x => x),
                x => true,
                writer);
            ComponentDxfIOComponents.WriteComponentSection(projectData.Components, writer);
            ComponentDxfIOUserLists.WriteUserListsSection(projectData.UserComponentLists, writer);
            ComponentDxfIONetworks.WriteNetworkSection(projectData.NetworkManager.NetworkRecord, writer);
            ComponentDxfIOSimNetworks.WriteNetworkSection(projectData.SimNetworks, writer);
            ComponentDxfIOValueMappings.WriteValueMappingSection(projectData.ValueMappings, writer);

            //EOF
            writer.WriteEOF();
        }

        /// <summary>
        /// Reads the data from a full component file
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <param name="parserInfo">Info for the parser</param>
        public static void Read(FileInfo file, DXFParserInfo parserInfo)
        {
            using (FileStream stream = file.OpenRead())
            {
                if (stream.Length == 0)
                    return;

                using (DXFStreamReader reader = new DXFStreamReader(stream))
                {
                    Read(reader, parserInfo);
                }
            }
        }
        /// <summary>
        /// Reads the data from a full component file
        /// </summary>
        /// <param name="reader">The DXFStreamReader to read from</param>
        /// <param name="parserInfo">Info for the parser</param>
        public static void Read(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            //Version section
            if (CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }

            LastParsedFileVersion = parserInfo.FileVersion;

            //Data section
            ComponentDxfIOResources.ReadAssetSection(reader, parserInfo);
            ComponentDxfIOComponents.ReadComponentSection(reader, parserInfo);

            if (parserInfo.FileVersion > 8)
            {
                ComponentDxfIOUserLists.ReadUserListsSection(reader, parserInfo);
            }
            else if (parserInfo.FileVersion >= 6 && parserInfo.FileVersion <= 8)
            {
                reader.Peek(); //0 SECTION
                (int key, var sectionName) = reader.Peek();
                if (key == 2 && sectionName == ParamStructTypes.ENTITY_SECTION)
                {
                    ComponentDxfIOUserLists.ReadUserListsSection(reader, parserInfo);
                }
            }

            ComponentDxfIONetworks.ReadNetworkSection(reader, parserInfo);

            if (parserInfo.FileVersion >= 12)
            {
                ComponentDxfIOSimNetworks.ReadNetworkSection(reader, parserInfo);
            }

            if (parserInfo.FileVersion <= 3)
            {
                ImportantSection.Skip(reader, parserInfo, true); //Some version 3 contain it, some don't
            }

            if (parserInfo.FileVersion >= 13)
            {
                ComponentDxfIOValueMappings.ReadValueMappingSection(reader, parserInfo);
            }

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.ProjectData.Components.RestoreReferences(parserInfo.ProjectData.NetworkManager.GetAllNetworkElements());
            parserInfo.ProjectData.AssetManager.ReleaseTmpParseRecord();

            if (parserInfo.FileVersion >= 12)
            {
                ComponentDxfIOSimNetworks.SubscribeToEvents(reader, parserInfo);
            }
            parserInfo.FinishLog();
        }


        #endregion

        #region Component Library

        /// <summary>
        /// Writes components and networks into a component library file
        /// </summary>
        /// <param name="file">The target file</param>
        /// <param name="components">The root components to be exported. All children are also included</param>
        /// <param name="networks">The networks to be exported</param>
        public static void WriteLibrary(FileInfo file, IEnumerable<SimComponent> components,
            IEnumerable<SimFlowNetwork> networks)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(fs))
                {
                    WriteLibrary(writer, components, networks);
                }
            }
        }

        internal static void WriteLibrary(DXFStreamWriter writer, IEnumerable<SimComponent> components,
            IEnumerable<SimFlowNetwork> networks)
        {
            //File header
            writer.WriteVersionSection();

            ComponentDxfIOComponents.WriteComponentSection(components, writer);
            ComponentDxfIONetworks.WriteNetworkSection(networks, writer);

            //EOF
            writer.WriteEOF();
        }

        /// <summary>
        /// Reads a component library
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <param name="parserInfo">Info for the parser</param>
        public static void ReadLibrary(FileInfo file, DXFParserInfo parserInfo)
        {
            using (FileStream stream = file.OpenRead())
            {
                if (stream.Length == 0)
                    return;

                using (DXFStreamReader reader = new DXFStreamReader(stream))
                {
                    ReadLibrary(reader, parserInfo);
                }
            }
        }
        /// <summary>
        /// Reads a component library
        /// </summary>
        /// <param name="reader">The DXFStreamReader to read from</param>
        /// <param name="parserInfo">Info for the parser</param>
        public static void ReadLibrary(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            //Version section
            if (CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }

            //Data section
            ComponentDxfIOComponents.ReadComponentSection(reader, parserInfo);
            ComponentDxfIONetworks.ReadNetworkSection(reader, parserInfo);

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.ProjectData.Components.RestoreReferences(parserInfo.ProjectData.NetworkManager.GetAllNetworkElements());
            parserInfo.ProjectData.AssetManager.ReleaseTmpParseRecord();
        }

        #endregion

        /// <summary>
        /// Collects all public resources (but not the ones referenced by assets) into a HashSet
        /// </summary>
        /// <param name="entry">The root entry to investigate</param>
        /// <param name="parentIsPublic">When set to True, all resources are exported no matter there visibility</param>
        /// <param name="publicResources">The HashSet to which public resources are added</param>
        /// <returns></returns>
        internal static bool GetPublicResources(ResourceEntry entry, bool parentIsPublic, HashSet<ResourceEntry> publicResources)
        {
            bool isPublic = entry.Visibility == SimComponentVisibility.AlwaysVisible || parentIsPublic;
            bool isAnyChildPublic = false;

            if (entry is ResourceDirectoryEntry dir)
            {
                foreach (var child in dir.Children)
                    isAnyChildPublic |= GetPublicResources(child, isPublic, publicResources);
            }

            if (isPublic || isAnyChildPublic)
            {
                publicResources.Add(entry);
                return true;
            }

            return false;
        }
        /// <summary>
        /// Collects all public assets and there referenced resources
        /// </summary>
        /// <param name="assetManager">The AssetManager that should be investigated</param>
        /// <param name="publicAssets">A list to which all public assets are added</param>
        /// <param name="publicResources">A HashSet to which all referenced resources are added</param>
        internal static void GetPublicAssets(AssetManager assetManager, List<Asset> publicAssets, HashSet<ResourceEntry> publicResources)
        {
            foreach (var assetEntry in assetManager.Assets)
            {
                bool anyAssetPublic = false;

                foreach (var asset in assetEntry.Value)
                {
                    foreach (var componentId in asset.ReferencingComponentIds)
                    {
                        var component = assetManager.ProjectData.IdGenerator.GetById<SimComponent>(
                            new SimId(assetManager.ProjectData.Components.CalledFromLocation.GlobalID, componentId));
                        if (component != null)
                        {
                            if (ComponentWalker.GetParents(component).Any(x => x.Visibility == SimComponentVisibility.AlwaysVisible))
                            {
                                publicAssets.Add(asset);
                                anyAssetPublic = true;
                                break;
                            }
                        }
                    }
                }

                if (anyAssetPublic)
                {
                    var resource = assetManager.GetResource(assetEntry.Key);
                    publicResources.Add(resource);

                    while (resource.Parent != null)
                    {
                        resource = resource.Parent;
                        if (resource != null)
                            publicResources.Add(resource);
                    }
                }
            }
        }
    }
}
