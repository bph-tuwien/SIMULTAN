using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
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
using System.Windows.Media;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFPublicTests
    {
        private DirectoryInfo workingDirectory;

        public static ExtendedProjectData CreateTestData(DirectoryInfo workingDirectory)
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;

            projectData.Components.StartLoading();

            var c1 = new SimComponent()
            {
                Id = new Data.SimId(Guid.Empty, 131),
                Name = "C1",
                Description = "",
                IsAutomaticallyGenerated = false,
                CurrentSlot = new SimSlotBase(SimDefaultSlots.Cost),
                ComponentColor = Color.FromArgb(230, 100, 10, 20),
                InstanceType = SimInstanceType.AttributesFace,
                Visibility = SimComponentVisibility.AlwaysVisible,
                SortingType = SimComponentContentSorting.ByName,
            };

            var c2 = new SimComponent()
            {
                Id = new Data.SimId(Guid.Empty, 132),
                Name = "C2",
                Description = "",
                IsAutomaticallyGenerated = false,
                CurrentSlot = new SimSlotBase(SimDefaultSlots.Cost),
                ComponentColor = Color.FromArgb(230, 100, 10, 20),
                InstanceType = SimInstanceType.AttributesFace,
                Visibility = SimComponentVisibility.VisibleInProject,
                SortingType = SimComponentContentSorting.ByName,
            };
            c1.Components.Add(new SimChildComponentEntry(new SimSlot(c2.CurrentSlot, "1"), c2));

            var c3 = new SimComponent()
            {
                Id = new Data.SimId(Guid.Empty, 133),
                Name = "C3",
                Description = "",
                IsAutomaticallyGenerated = false,
                CurrentSlot = new SimSlotBase(SimDefaultSlots.Cost),
                ComponentColor = Color.FromArgb(230, 100, 10, 20),
                InstanceType = SimInstanceType.AttributesFace,
                Visibility = SimComponentVisibility.VisibleInProject,
                SortingType = SimComponentContentSorting.ByName,
            };

            projectData.Components.Add(c1);
            projectData.Components.Add(c3);

            ResourceDirectoryEntry f1 = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F1", false, 1, false)
            {
                Visibility = SimComponentVisibility.AlwaysVisible
            };
            projectData.AssetManager.AddResourceEntry(f1);

            ResourceDirectoryEntry f2 = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F1\\F2", false, 2, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(f2);

            ResourceDirectoryEntry f3 = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F3", false, 5, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(f3);

            ResourceDirectoryEntry f4 = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F4", false, 7, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(f4);

            ResourceDirectoryEntry f5 = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F5", false, 9, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(f5);

            ContainedResourceFileEntry d1 = new ContainedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F1\\F2\\D1.txt", false, 3, false)
            {
                Visibility = SimComponentVisibility.AlwaysVisible
            };
            projectData.AssetManager.AddResourceEntry(d1, f2);

            ContainedResourceFileEntry d2 = new ContainedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F1\\D2.txt", false, 4, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(d2, f2);

            ContainedResourceFileEntry d3 = new ContainedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F3\\D3.txt", false, 6, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(d3, f3);

            ContainedResourceFileEntry d4 = new ContainedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F4\\D4.txt", false, 8, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(d4, f4);

            ContainedResourceFileEntry d5 = new ContainedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "F5\\D5.txt", false, 10, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(d5, f5);


            projectData.AssetManager.CreateDocumentAsset(c2, d4, "ABC");
            projectData.AssetManager.CreateDocumentAsset(c3, d5, "ABC");

            projectData.Components.EndLoading();

            return projectData;
        }

        public static DirectoryInfo SetupTestData()
        {
            var workingDirectory = new DirectoryInfo("./TestWorkingDirectory");
            workingDirectory.Create();

            var f1 = workingDirectory.CreateSubdirectory("F1");
            var f2 = f1.CreateSubdirectory("F2");
            var f3 = workingDirectory.CreateSubdirectory("F3");
            var f4 = workingDirectory.CreateSubdirectory("F4");
            var f5 = workingDirectory.CreateSubdirectory("F5");

            File.Create("./TestWorkingDirectory/F1/F2/D1.txt").Dispose();
            File.Create("./TestWorkingDirectory/F1/D2.txt").Dispose();
            File.Create("./TestWorkingDirectory/F3/D3.txt").Dispose();
            File.Create("./TestWorkingDirectory/F4/D4.txt").Dispose();
            File.Create("./TestWorkingDirectory/F5/D5.txt").Dispose();

            return workingDirectory;
        }

        public static void CleanupTestData(DirectoryInfo workingDirectory)
        {
            if (workingDirectory != null && workingDirectory.Exists)
                workingDirectory.Delete(true);
        }

        [TestInitialize]
        public void Setup()
        {
            workingDirectory = SetupTestData();
        }

        [TestCleanup]
        public void Cleanup()
        {
            CleanupTestData(workingDirectory);
            workingDirectory = null;
        }


        [TestMethod]
        public void WritePublicComponentFile()
        {
            ExtendedProjectData data = CreateTestData(workingDirectory);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIO.WritePublic(writer, data);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WritePublic, exportedString);
        }

        [TestMethod]
        public void ReadPublicComponentFileV12()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXFPV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ComponentDxfIO.ReadPublic(reader, info);
            }

            Assert.AreEqual(1, projectData.Components.Count);
            Assert.AreEqual(2, projectData.AssetManager.Resources.Count);
            Assert.AreEqual(1, projectData.AssetManager.Assets.Count);
        }
    }
}
