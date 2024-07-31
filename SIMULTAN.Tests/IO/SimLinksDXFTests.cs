using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.SIMLINKS;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class SimLinksDXFTests
    {
        [TestMethod]
        public void Write()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            //Fix machine hash
            MultiLinkManager.MachineHashGenerator = new DummyMachineHashGenerator();

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            MultiLink link1 = new MultiLink(@"C:\something");
            MultiLink link2 = new MultiLink(@"D:\something");

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimLinksDxfIO.Write(new MultiLink[] { link1, link2 }, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_SIMLINKS_Write, exportedString);
        }

        [TestMethod]
        public void ReadV29()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            List<MultiLink> links = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SIMLINKS_ReadV29)))
            {
                var info = new DXFParserInfo(guid, projectData);
                links = SimLinksDxfIO.Read(reader, info);
            }

            Assert.IsNotNull(links);
            Assert.AreEqual(2, links.Count);

            var link = links[0];
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("42BE0F32C1C2CDC58646B483E18C864978D668C64687FF16C8149951721AA191", link.Representations.First().Key);
            Assert.AreEqual(@"C:\something", link.Representations.First().Value);

            link = links[1];
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("42BE0F32C1C2CDC58646B483E18C864978D668C64687FF16C8149951721AA191", link.Representations.First().Key);
            Assert.AreEqual(@"D:\something", link.Representations.First().Value);
        }
        [TestMethod]
        public void ReadV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            List<MultiLink> links = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SIMLINKS_ReadV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                links = SimLinksDxfIO.Read(reader, info);
            }

            Assert.IsNotNull(links);
            Assert.AreEqual(2, links.Count);

            var link = links[0];
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("1136910687", link.Representations.First().Key);
            Assert.AreEqual(@"C:\something", link.Representations.First().Value);

            link = links[1];
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("1136910687", link.Representations.First().Key);
            Assert.AreEqual(@"D:\something", link.Representations.First().Value);
        }

        [TestMethod]
        public void ReadV11()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            List<MultiLink> links = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SIMLINKS_ReadV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                links = SimLinksDxfIO.Read(reader, info, false);
            }

            Assert.IsNotNull(links);
            Assert.AreEqual(2, links.Count);

            var link = links[0];
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("1136910687", link.Representations.First().Key);
            Assert.AreEqual(@"C:\something", link.Representations.First().Value);

            link = links[1];
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("1136910687", link.Representations.First().Key);
            Assert.AreEqual(@"D:\something", link.Representations.First().Value);
        }


        [TestMethod]
        public void WriteLinkSection()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            //Fix machine hash
            MultiLinkManager.MachineHashGenerator = new DummyMachineHashGenerator();

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            MultiLink link1 = new MultiLink(@"C:\something");
            MultiLink link2 = new MultiLink(@"D:\something");

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimLinksDxfIO.WriteMultiLinkSection(new MultiLink[] { link1, link2 }, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_SIMLINKS_WriteLinkSection, exportedString);
        }

        [TestMethod]
        public void ReadLinkSectionV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            List<MultiLink> links = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SIMLINKS_ReadLinkSectionV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                links = SimLinksDxfIO.MultiLinkSectionEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(links);
            Assert.AreEqual(2, links.Count);

            var link = links[0];
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("1136910687", link.Representations.First().Key);
            Assert.AreEqual(@"C:\something", link.Representations.First().Value);

            link = links[1];
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("1136910687", link.Representations.First().Key);
            Assert.AreEqual(@"D:\something", link.Representations.First().Value);
        }


        [TestMethod]
        public void WriteLink()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            //Fix machine hash
            MultiLinkManager.MachineHashGenerator = new DummyMachineHashGenerator();

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            MultiLink link = new MultiLink(@"C:\something");

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimLinksDxfIO.WriteMultiLink(link, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_SIMLINKS_WriteLink, exportedString);
        }

        [TestMethod]
        public void ReadPortV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            MultiLink link = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SIMLINKS_ReadLinkV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                link = SimLinksDxfIO.LinkEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(link);
            Assert.AreEqual(1, link.Representations.Count);
            Assert.AreEqual("1136910687", link.Representations.First().Key);
            Assert.AreEqual(@"C:\something", link.Representations.First().Value);
        }
    }
}
