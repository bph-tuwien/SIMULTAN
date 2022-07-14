using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.METADXF;
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
    public class MetaDxfIOTests
    {
        [TestMethod]
        public void Write()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            HierarchicProjectMetaData metaData = new HierarchicProjectMetaData(otherGuid, new Dictionary<Guid, string> { });

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    MetaDxfIO.Write(writer, metaData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_METADXF_Write, exportedString);
        }

        [TestMethod]
        public void ReadV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            HierarchicProjectMetaData metaData = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_METADXF_ReadV12)))
            {
                var info = new DXFParserInfo(guid, projectData);

                metaData = MetaDxfIO.Read(reader, info);
            }

            Assert.IsNotNull(metaData);
            Assert.AreEqual(otherGuid, metaData.ProjectId);
            Assert.AreEqual(0, metaData.ChildProjects.Count);
        }

        [TestMethod]
        public void ReadV11()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            HierarchicProjectMetaData metaData = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_METADXF_ReadV11)))
            {
                var info = new DXFParserInfo(guid, projectData);

                metaData = MetaDxfIO.Read(reader, info);
            }

            Assert.IsNotNull(metaData);
            Assert.AreEqual(otherGuid, metaData.ProjectId);
            Assert.AreEqual(0, metaData.ChildProjects.Count);
        }

        [TestMethod]
        public void WriteMetaData()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            HierarchicProjectMetaData metaData = new HierarchicProjectMetaData(otherGuid, new Dictionary<Guid, string> { });

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    MetaDxfIO.WriteMetaData(writer, metaData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_METADXF_WriteMetaData, exportedString);
        }

        [TestMethod]
        public void ReadMetaDataV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            HierarchicProjectMetaData metaData = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_METADXF_ReadMetaDataV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                metaData = MetaDxfIO.MetaDataEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(metaData);
            Assert.AreEqual(otherGuid, metaData.ProjectId);
            Assert.AreEqual(0, metaData.ChildProjects.Count);
        }
    }
}
