using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFResourceTests : ComponentDXFTestsBase
    {
        #region Section

        [TestMethod]
        public void WriteAssetsSection()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = CreateTestData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOResources.WriteAssetsSection(projectData.AssetManager.Resources,
                        projectData.AssetManager.Assets.Values.SelectMany(x => x), x => true,
                        writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_WriteAssetsSection, exportedString);
        }

        [TestMethod]
        public void ReadAssetsSectionV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            List<AssetManager> assetManager = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_AssetsSectionV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                assetManager = ComponentDxfIOResources.AssetsSectionElement.Parse(reader, info);
            }

            Assert.AreEqual(1, assetManager.Count);
            CheckAssetManager(assetManager.First());
        }

        [TestMethod]
        public void ReadAssetsSectionV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            List<AssetManager> assetManager = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_AssetsSectionV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                assetManager = ComponentDxfIOResources.AssetsSectionElement.Parse(reader, info);
            }

            Assert.AreEqual(1, assetManager.Count);
            CheckAssetManager(assetManager.First());
        }

        #endregion

        #region AssetManager

        internal static void CheckAssetManager(AssetManager assetManager)
        {
            Assert.IsNotNull(assetManager);
            Assert.AreEqual(1, assetManager.Resources.Count);

            var rootDir = assetManager.Resources[0] as ResourceDirectoryEntry;
            Assert.IsNotNull(rootDir);
            Assert.AreEqual(2, rootDir.Children.Count);

            var childDir1 = rootDir.Children[0] as ResourceDirectoryEntry;
            Assert.IsNotNull(childDir1);
            Assert.AreEqual(2, childDir1.Children.Count);

            var containedFile = childDir1.Children[0] as ContainedResourceFileEntry;
            Assert.IsNotNull(containedFile);

            var linkedFile = childDir1.Children[1] as LinkedResourceFileEntry;
            Assert.IsNotNull(linkedFile);

            Assert.IsTrue(assetManager.Assets.ContainsKey(linkedFile.Key));
            Assert.AreEqual(1, assetManager.Assets[linkedFile.Key].Count);
            var docAsset = assetManager.Assets[linkedFile.Key][0] as DocumentAsset;
            Assert.IsNotNull(docAsset);
            Assert.AreEqual(linkedFile, docAsset.Resource);

            Assert.IsTrue(assetManager.Assets.ContainsKey(containedFile.Key));
            Assert.AreEqual(1, assetManager.Assets[containedFile.Key].Count);
            var geoAsset = assetManager.Assets[containedFile.Key][0] as GeometricAsset;
            Assert.IsNotNull(geoAsset);
            Assert.AreEqual(containedFile, geoAsset.Resource);

            var childDir2 = rootDir.Children[1] as ResourceDirectoryEntry;
            Assert.IsNotNull(childDir2);
            Assert.AreEqual(0, childDir2.Children.Count);
        }

        [TestMethod]
        public void WriteAssetManager()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = CreateTestData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOResources.WriteAssetManager(projectData.AssetManager.Resources, x => true,
                        projectData.AssetManager.Assets.Values.SelectMany(x => x), writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_WriteAssetManager, exportedString);
        }

        [TestMethod]
        public void ReadAssetManagerV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            AssetManager assetManager = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_AssetManagerV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                assetManager = ComponentDxfIOResources.AssetManagerEntityElement.Parse(reader, info);
            }

            CheckAssetManager(assetManager);
        }

        [TestMethod]
        public void ReadAssetManagerV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            AssetManager assetManager = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_AssetManagerV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                assetManager = ComponentDxfIOResources.AssetManagerEntityElement.Parse(reader, info);
            }

            CheckAssetManager(assetManager);
        }

        #endregion


        #region ResourceDirectory

        [TestMethod]
        public void WriteResourceDirectoryEntrySimple()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = "C:\\";

            ResourceDirectoryEntry rootDirectory = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "RootFolder", false, 3, false);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOResources.WriteResourceEntry(rootDirectory, writer, x => true);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteResourceDirectory_Simple, exportedString);
        }

        [TestMethod]
        public void WriteResourceDirectoryEntryMultiple()
        {
            ExtendedProjectData projectData = CreateTestData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOResources.WriteResourceEntry(projectData.AssetManager.Resources[0] as ResourceDirectoryEntry, writer,
                        x => true);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteResourceDirectory_Multiple, exportedString);
        }

        [TestMethod]
        public void ReadResourceDirectoryV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ResourceDirectoryEntry directory = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ResourceDirectoryV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                directory = ComponentDxfIOResources.ResourceDirectoryEntityElement.Parse(reader, info) as ResourceDirectoryEntry;
            }

            Assert.IsNotNull(directory);
            Assert.AreEqual("RootFolder", directory.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, directory.UserWithWritingAccess);
            Assert.AreEqual(3, directory.Key);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, directory.Visibility);

            Assert.AreEqual(2, directory.Children.Count);

            var childFolder1 = directory.Children[0] as ResourceDirectoryEntry;
            Assert.IsNotNull(childFolder1);
            Assert.AreEqual("RootFolder\\ChildFolder1", childFolder1.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.ARCHITECTURE, childFolder1.UserWithWritingAccess);
            Assert.AreEqual(4, childFolder1.Key);
            Assert.AreEqual(SimComponentVisibility.VisibleInProject, childFolder1.Visibility);

            var childFolder2 = directory.Children[1] as ResourceDirectoryEntry;
            Assert.IsNotNull(childFolder2);
            Assert.AreEqual("RootFolder\\ChildFolder2", childFolder2.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.ARCHITECTURE, childFolder2.UserWithWritingAccess);
            Assert.AreEqual(5, childFolder2.Key);
            Assert.AreEqual(SimComponentVisibility.VisibleInProject, childFolder2.Visibility);

            Assert.AreEqual(2, childFolder1.Children.Count);
            var containedFile1 = childFolder1.Children[0] as ContainedResourceFileEntry;
            Assert.IsNotNull(containedFile1);
            Assert.AreEqual("RootFolder\\ChildFolder1\\MyContainedFile.txt", containedFile1.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, containedFile1.UserWithWritingAccess);
            Assert.AreEqual(11, containedFile1.Key);
            Assert.AreEqual(true, containedFile1.Exists);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, containedFile1.Visibility);

            var linkedFile1 = childFolder1.Children[1] as LinkedResourceFileEntry;
            Assert.IsNotNull(linkedFile1);
            Assert.AreEqual("MyLinkedFile.txt", linkedFile1.CurrentRelativePath);
            Assert.AreEqual(linkDirectory.FullName + "\\MyLinkedFile.txt", linkedFile1.CurrentFullPath);
            Assert.AreEqual(SimUserRole.BUILDING_DEVELOPER, linkedFile1.UserWithWritingAccess);
            Assert.AreEqual(12, linkedFile1.Key);
            Assert.AreEqual(true, linkedFile1.Exists);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, linkedFile1.Visibility);
        }

        [TestMethod]
        public void ReadResourceDirectoryV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ResourceDirectoryEntry directory = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ResourceDirectoryV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                directory = ComponentDxfIOResources.ResourceDirectoryEntityElement.Parse(reader, info) as ResourceDirectoryEntry;
            }

            Assert.IsNotNull(directory);
            Assert.AreEqual("RootFolder", directory.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, directory.UserWithWritingAccess);
            Assert.AreEqual(3, directory.Key);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, directory.Visibility);

            Assert.AreEqual(2, directory.Children.Count);

            var childFolder1 = directory.Children[0] as ResourceDirectoryEntry;
            Assert.IsNotNull(childFolder1);
            Assert.AreEqual("RootFolder\\ChildFolder1", childFolder1.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.ARCHITECTURE, childFolder1.UserWithWritingAccess);
            Assert.AreEqual(4, childFolder1.Key);
            Assert.AreEqual(SimComponentVisibility.VisibleInProject, childFolder1.Visibility);

            var childFolder2 = directory.Children[1] as ResourceDirectoryEntry;
            Assert.IsNotNull(childFolder2);
            Assert.AreEqual("RootFolder\\ChildFolder2", childFolder2.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.ARCHITECTURE, childFolder2.UserWithWritingAccess);
            Assert.AreEqual(5, childFolder2.Key);
            Assert.AreEqual(SimComponentVisibility.VisibleInProject, childFolder2.Visibility);

            Assert.AreEqual(2, childFolder1.Children.Count);
            var containedFile1 = childFolder1.Children[0] as ContainedResourceFileEntry;
            Assert.IsNotNull(containedFile1);
            Assert.AreEqual("RootFolder\\ChildFolder1\\MyContainedFile.txt", containedFile1.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, containedFile1.UserWithWritingAccess);
            Assert.AreEqual(11, containedFile1.Key);
            Assert.AreEqual(true, containedFile1.Exists);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, containedFile1.Visibility);

            var linkedFile1 = childFolder1.Children[1] as LinkedResourceFileEntry;
            Assert.IsNotNull(linkedFile1);
            Assert.AreEqual("MyLinkedFile.txt", linkedFile1.CurrentRelativePath);
            Assert.AreEqual(linkDirectory.FullName + "\\MyLinkedFile.txt", linkedFile1.CurrentFullPath);
            Assert.AreEqual(SimUserRole.BUILDING_DEVELOPER, linkedFile1.UserWithWritingAccess);
            Assert.AreEqual(12, linkedFile1.Key);
            Assert.AreEqual(true, linkedFile1.Exists);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, linkedFile1.Visibility);
        }

        #endregion

        #region ContainedResourceFile

        [TestMethod]
        public void WriteContainedResourceFileEntry()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = "C:\\";

            ContainedResourceFileEntry file = new ContainedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_DEVELOPER, "MyContainedFile.txt", false, 11, false);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOResources.WriteContainedResourceFileEntry(file, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteContainedResourceFile, exportedString);
        }

        [TestMethod]
        public void ReadContainedResourceFileV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ContainedResourceFileEntry file = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ContainedFileV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                file = ComponentDxfIOResources.ContainedResourceFileEntityElement.Parse(reader, info) as ContainedResourceFileEntry;
            }

            Assert.IsNotNull(file);
            Assert.AreEqual("MyContainedFile.txt", file.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, file.UserWithWritingAccess);
            Assert.AreEqual(11, file.Key);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, file.Visibility);

        }

        [TestMethod]
        public void ReadContainedResourceFileV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ContainedResourceFileEntry file = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ContainedFileV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                file = ComponentDxfIOResources.ContainedResourceFileEntityElement.Parse(reader, info) as ContainedResourceFileEntry;
            }

            Assert.IsNotNull(file);
            Assert.AreEqual("MyContainedFile.txt", file.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, file.UserWithWritingAccess);
            Assert.AreEqual(11, file.Key);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, file.Visibility);

        }

        #endregion

        #region LinkedResourceFile

        [TestMethod]
        public void WriteLinkedResourceFileEntry()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = "C:\\";

            LinkedResourceFileEntry file = new LinkedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_DEVELOPER, "MyLinkedFile.txt", false, 12);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOResources.WriteLinkedResourceFileEntry(file, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteLinkedResourceFile, exportedString);
        }

        [TestMethod]
        public void ReadLinkedResourceFileV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = new DirectoryInfo("./TestWorkingDirectory").FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            LinkedResourceFileEntry file = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_LinkedFileV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                file = ComponentDxfIOResources.LinkedResourceFileEntityElement.Parse(reader, info) as LinkedResourceFileEntry;
            }

            Assert.IsNotNull(file);
            Assert.AreEqual("MyLinkedFile.txt", file.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.BUILDING_DEVELOPER, file.UserWithWritingAccess);
            Assert.AreEqual(12, file.Key);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, file.Visibility);

        }

        [TestMethod]
        public void ReadLinkedResourceFileV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = new DirectoryInfo("./TestWorkingDirectory").FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            LinkedResourceFileEntry file = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_LinkedFileV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                file = ComponentDxfIOResources.LinkedResourceFileEntityElement.Parse(reader, info) as LinkedResourceFileEntry;
            }

            Assert.IsNotNull(file);
            Assert.AreEqual("MyLinkedFile.txt", file.CurrentRelativePath);
            Assert.AreEqual(SimUserRole.BUILDING_DEVELOPER, file.UserWithWritingAccess);
            Assert.AreEqual(12, file.Key);
            Assert.AreEqual(SimComponentVisibility.AlwaysVisible, file.Visibility);

        }

        #endregion

        #region Assets

        [TestMethod]
        public void WriteDocumentAsset()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = "C:\\";

            var asset = new DocumentAsset(projectData.AssetManager, 5566, 11, "2");

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOResources.WriteDocumentAsset(asset, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteDocumentAsset, exportedString);
        }

        [TestMethod]
        public void WriteGeometryAsset()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = "C:\\";

            var asset = new GeometricAsset(projectData.AssetManager, 5566, 12, "3");

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOResources.WriteGeometricAsset(asset, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteGeometricAsset, exportedString);
        }

        [TestMethod]
        public void ReadDocumentAssetV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = new DirectoryInfo("./TestWorkingDirectory").FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            DocumentAsset asset = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_DocumentAssetV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                asset = ComponentDxfIOResources.DocumentAssetEntityElement.Parse(reader, info) as DocumentAsset;
            }

            Assert.IsNotNull(asset);
            Assert.AreEqual(11, asset.ResourceKey);
            Assert.AreEqual("2", asset.ContainedObjectId);
            Assert.AreEqual(1, asset.ReferencingComponentIds.Count);
            Assert.AreEqual(5566, asset.ReferencingComponentIds[0]);

        }

        [TestMethod]
        public void ReadGeometricAssetV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = new DirectoryInfo("./TestWorkingDirectory").FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            GeometricAsset asset = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_GeometricAssetV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                asset = ComponentDxfIOResources.GeometricAssetEntityElement.Parse(reader, info) as GeometricAsset;
            }

            Assert.IsNotNull(asset);
            Assert.AreEqual(12, asset.ResourceKey);
            Assert.AreEqual("3", asset.ContainedObjectId);
            Assert.AreEqual(1, asset.ReferencingComponentIds.Count);
            Assert.AreEqual(5566, asset.ReferencingComponentIds[0]);

        }

        [TestMethod]
        public void ReadDocumentAssetV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = new DirectoryInfo("./TestWorkingDirectory").FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            DocumentAsset asset = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_DocumentAssetV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                asset = ComponentDxfIOResources.DocumentAssetEntityElement.Parse(reader, info) as DocumentAsset;
            }

            Assert.IsNotNull(asset);
            Assert.AreEqual(11, asset.ResourceKey);
            Assert.AreEqual("2", asset.ContainedObjectId);
            Assert.AreEqual(1, asset.ReferencingComponentIds.Count);
            Assert.AreEqual(5566, asset.ReferencingComponentIds[0]);

        }

        [TestMethod]
        public void ReadGeometricAssetV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.AssetManager.WorkingDirectory = new DirectoryInfo("./TestWorkingDirectory").FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            GeometricAsset asset = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_GeometricAssetV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                asset = ComponentDxfIOResources.GeometricAssetEntityElement.Parse(reader, info) as GeometricAsset;
            }

            Assert.IsNotNull(asset);
            Assert.AreEqual(12, asset.ResourceKey);
            Assert.AreEqual("3", asset.ContainedObjectId);
            Assert.AreEqual(1, asset.ReferencingComponentIds.Count);
            Assert.AreEqual(5566, asset.ReferencingComponentIds[0]);

        }

        #endregion
    }
}
