using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFTests : ComponentDXFTestsBase
    {
        #region Private Files

        [TestMethod]
        public void WriteComponentFile()
        {
            ExtendedProjectData data = CreateTestData();

            string exportedString = null;
            var entries = data.Taxonomies.SelectMany(t => t.Entries);
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIO.Write(writer, data);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_Write, exportedString);
        }

        [TestMethod]
        public void ReadComponentFileV27()
        {
            Guid guid = Guid.Parse("98478ed1-d3f4-4873-95b6-412e5e23aac4");
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);


            var tax = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            tax.Entries.Add(baseTaxonomyEntry);
            projectData.Taxonomies.Add(tax);


            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFV27)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 27;
                ComponentDxfIO.Read(reader, info);
            }

            projectData.Components.RestoreDefaultTaxonomyReferences();

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(9, projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);

            Assert.AreEqual(0, projectData.ValueMappings.Count);
        }

        [TestMethod]
        public void ReadComponentFileV26()
        {
            Guid guid = Guid.Parse("98478ed1-d3f4-4873-95b6-412e5e23aac4");
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);


            var tax = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            tax.Entries.Add(baseTaxonomyEntry);
            projectData.Taxonomies.Add(tax);


            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFV26)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 26;
                ComponentDxfIO.Read(reader, info);
            }

            projectData.Components.RestoreDefaultTaxonomyReferences();

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(9, projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);

            Assert.AreEqual(0, projectData.ValueMappings.Count);
        }

        [TestMethod]
        public void ReadComponentFileV25()
        {
            Guid guid = Guid.Parse("98478ed1-d3f4-4873-95b6-412e5e23aac4");
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);


            var tax = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            tax.Entries.Add(baseTaxonomyEntry);
            projectData.Taxonomies.Add(tax);


            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFV25)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ComponentDxfIO.Read(reader, info);
            }

            projectData.Components.RestoreDefaultTaxonomyReferences();

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(9, projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);

            Assert.AreEqual(0, projectData.ValueMappings.Count);
        }

        [TestMethod]
        public void ReadComponentFileV21()
        {
            Guid guid = Guid.Parse("98478ed1-d3f4-4873-95b6-412e5e23aac4");
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);


            var tax = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            tax.Entries.Add(baseTaxonomyEntry);
            projectData.Taxonomies.Add(tax);


            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ComponentDxfIO.Read(reader, info);
            }

            projectData.Components.RestoreDefaultTaxonomyReferences();

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(9, projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);

            Assert.AreEqual(0, projectData.ValueMappings.Count);
        }

        [TestMethod]
        public void ReadComponentFileV19()
        {
            Guid guid = Guid.Parse("98478ed1-d3f4-4873-95b6-412e5e23aac4");
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);


            var tax = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            tax.Entries.Add(baseTaxonomyEntry);
            projectData.Taxonomies.Add(tax);


            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFV19)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ComponentDxfIO.Read(reader, info);
            }

            projectData.Components.RestoreDefaultTaxonomyReferences();

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(9, projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);

            Assert.AreEqual(0, projectData.ValueMappings.Count);
        }

        [TestMethod]
        public void ReadComponentFileV13()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);
            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFV13)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ComponentDxfIO.Read(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(2, projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);
            ComponentDXFSimNetworkTests.CheckTestData(projectData);
            ComponentDXFValueMappingTests.CheckTestData(projectData);
        }

        [TestMethod]
        public void ReadComponentFileV12()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ComponentDxfIO.Read(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(2, projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);
            ComponentDXFSimNetworkTests.CheckTestData(projectData);

            Assert.AreEqual(0, projectData.ValueMappings.Count);
        }

        [TestMethod]
        public void ReadComponentFileV11()
        {
            Guid guid = Guid.Parse("98478ed1-d3f4-4873-95b6-412e5e23aac4");
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;
            projectData.AssetManager.PathsToResourceFiles.Add(linkDirectory.FullName);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ComponentDxfIO.Read(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(2, projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);

            Assert.AreEqual(0, projectData.ValueMappings.Count);
        }

        #endregion
    }
}
