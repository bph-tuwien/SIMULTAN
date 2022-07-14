using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFUserComponentListTests : ComponentDXFTestsBase
    {
        public static void CheckUserLists(ProjectData projectData)
        {
            Assert.AreEqual(3, projectData.UserComponentLists.Count);

            var list1 = projectData.UserComponentLists[0];
            Assert.AreEqual("demolist", list1.Name);
            Assert.AreEqual(2, list1.RootComponents.Count);
            Assert.AreEqual(projectData.Components[0], list1.RootComponents[0]);
            Assert.AreEqual(projectData.Components[0].Components[0].Component, list1.RootComponents[1]);

            var list2 = projectData.UserComponentLists[1];
            Assert.AreEqual("demo list 2", list2.Name);
            Assert.AreEqual(0, list2.RootComponents.Count);

            var list3 = projectData.UserComponentLists[2];
            Assert.AreEqual("demolist3", list3.Name);
            Assert.AreEqual(1, list3.RootComponents.Count);
            Assert.AreEqual(projectData.Components[0].Components[0].Component, list3.RootComponents[0]);
        }



        [TestMethod]
        public void WriteUserComponentList()
        {
            ExtendedProjectData projectData = CreateTestData();

            var list = new SimUserComponentList("demolist", new SimComponent[]
            {
                projectData.Components[0],
                projectData.Components[0].Components[0].Component
            });
            projectData.UserComponentLists.Add(list);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOUserLists.WriteUserList(list, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_WriteUserComponentList, exportedString);
        }

        [TestMethod]
        public void WriteUserComponentListSection()
        {
            ExtendedProjectData projectData = CreateTestData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOUserLists.WriteUserListsSection(projectData.UserComponentLists, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_WriteUserComponentLists, exportedString);
        }


        [TestMethod]
        public void ReadUserComponentListV12()
        {
            ExtendedProjectData projectData = CreateTestData(out var guid);

            SimUserComponentList userList = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_UserComponentListV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                userList = ComponentDxfIOUserLists.UserListEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(userList);
            Assert.AreEqual("demolist", userList.Name);
            Assert.AreEqual(2, userList.RootComponents.Count);
            Assert.AreEqual(projectData.Components[0], userList.RootComponents[0]);
            Assert.AreEqual(projectData.Components[0].Components[0].Component, userList.RootComponents[1]);
        }

        [TestMethod]
        public void ReadUserComponentListV11()
        {
            ExtendedProjectData projectData = CreateTestData(out var guid);

            SimUserComponentList userList = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_UserComponentListV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                userList = ComponentDxfIOUserLists.UserListEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(userList);
            Assert.AreEqual("demolist", userList.Name);
            Assert.AreEqual(2, userList.RootComponents.Count);
            Assert.AreEqual(projectData.Components[0], userList.RootComponents[0]);
            Assert.AreEqual(projectData.Components[0].Components[0].Component, userList.RootComponents[1]);
        }

        [TestMethod]
        public void ReadUserComponentListSectionV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;

            base.CreateNetworkTestData(projectData);
            base.CreateComponentTestData(projectData, guid, otherGuid);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_UserComponentListsV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                ComponentDxfIOUserLists.ReadUserListsSection(reader, info);
            }

            CheckUserLists(projectData);
        }

        [TestMethod]
        public void ReadUserComponentListSectionV11()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;

            base.CreateNetworkTestData(projectData);
            base.CreateComponentTestData(projectData, guid, otherGuid);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_UserComponentListsV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                ComponentDxfIOUserLists.ReadUserListsSection(reader, info);
            }

            CheckUserLists(projectData);
        }
    }
}
