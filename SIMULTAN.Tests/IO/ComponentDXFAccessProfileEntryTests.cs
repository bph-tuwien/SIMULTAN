using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFAccessProfileEntryTests
    {
        [TestMethod]
        public void WriteAccessProfileEntry()
        {
            SimAccessProfileEntry entry = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE,
                SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize,
                new DateTime(2021, 05, 05, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2021, 06, 05, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2021, 06, 07, 0, 0, 0, DateTimeKind.Utc));

            var asdfasdf = new DateTime(2021, 06, 07, 0, 0, 0, DateTimeKind.Utc).ToString(new DateTimeFormatInfo());

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteProfileEntry(entry, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteAccessEntry, exportedString);
        }

        [TestMethod]
        public void ParseAccessProfileEntryV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            SimAccessProfileEntry entry = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_AccessEntryV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                entry = ComponentDxfIOComponents.ProfileEntryElement.Parse(reader, info);
            }

            Assert.IsNotNull(entry);
            Assert.AreEqual(SimUserRole.ARCHITECTURE, entry.Role);
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize, entry.Access);
            Assert.AreEqual(new DateTime(2021, 05, 05, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessWrite);
            Assert.AreEqual(new DateTime(2021, 06, 05, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessSupervize);
            Assert.AreEqual(new DateTime(2021, 06, 07, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessRelease);
        }

        [TestMethod]
        public void ParseAccessProfileEntryV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            SimAccessProfileEntry entry = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_AccessEntryV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                entry = ComponentDxfIOComponents.ProfileEntryElement.Parse(reader, info);
            }

            Assert.IsNotNull(entry);
            Assert.AreEqual(SimUserRole.ARCHITECTURE, entry.Role);
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize, entry.Access);
            Assert.AreEqual(new DateTime(2021, 05, 05, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessWrite);
            Assert.AreEqual(new DateTime(2021, 06, 05, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessSupervize);
            Assert.AreEqual(new DateTime(2021, 06, 07, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessRelease);
        }

        [TestMethod]
        public void ParseAccessProfileEntryVminus1()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            SimAccessProfileEntry entry = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_AccessEntryV_1)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 0;

                reader.Read();

                entry = ComponentDxfIOComponents.ProfileEntryElement.Parse(reader, info);
            }

            Assert.IsNotNull(entry);
            Assert.AreEqual(SimUserRole.ARCHITECTURE, entry.Role);
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize, entry.Access);
            Assert.AreEqual(new DateTime(2021, 05, 05, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessWrite);
            Assert.AreEqual(new DateTime(2021, 06, 05, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessSupervize);
            Assert.AreEqual(new DateTime(2021, 06, 07, 0, 0, 0, DateTimeKind.Utc), entry.LastAccessRelease);
        }
    }
}
