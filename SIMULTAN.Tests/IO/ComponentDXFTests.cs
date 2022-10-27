using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
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
using System.Windows;

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

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(projectData, guid, otherGuid, instanceNode);
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

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(projectData, guid, otherGuid, instanceNode);
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

            //Resources
            ComponentDXFResourceTests.CheckAssetManager(projectData.AssetManager);
            ComponentDXFNetworkTests.CheckNetworks(projectData, guid);

            var instanceNode = projectData.NetworkManager.NetworkRecord[0].ContainedNodes[2];
            ComponentDXFComponentTests.CheckComponents(projectData, guid, otherGuid, instanceNode);
            ComponentDXFUserComponentListTests.CheckUserLists(projectData);

            Assert.AreEqual(0, projectData.ValueMappings.Count);
        }

        #endregion
    }
}
