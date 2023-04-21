using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.GRDXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class GeometryRelationsFileMappingDXFTests
    {

        private (SimTaxonomy, SimTaxonomyEntry) Setup(ExtendedProjectData projectData)
        {
            var taxonomy = new SimTaxonomy("Test");
            var taxEntry = new SimTaxonomyEntry("test", "test");
            taxonomy.Entries.Add(taxEntry);
            projectData.Taxonomies.Add(taxonomy);
            return (taxonomy, taxEntry);
        }

        #region File Mapping

        [TestMethod]
        public void WriteEmpty()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    GeometryRelationsFileMappingDxfIO.Write(writer, new GeometryRelationsFileMapping[] { });
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_GRFMDXF_WriteEmpty, exportedString);
        }

        [TestMethod]
        public void ReadEmpty()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            List<GeometryRelationsFileMapping> mappings = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_GRFMDXF_ReadEmptyV20)))
            {
                mappings = GeometryRelationsFileMappingDxfIO.Read(reader, new DXFParserInfo(projectData.GeometryRelations.CalledFromLocation.GlobalID, projectData));
            }

            Assert.IsNotNull(mappings);
            Assert.AreEqual(0, mappings.Count);
        }

        [TestMethod]
        public void WriteMappings()
        {
            var mappings = new List<GeometryRelationsFileMapping>
            {
                new GeometryRelationsFileMapping(1, "test"),
                new GeometryRelationsFileMapping(2, "test2"),
                new GeometryRelationsFileMapping(3, "test3")
            };

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    GeometryRelationsFileMappingDxfIO.Write(writer, mappings);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_GRFMDXF_WriteMappings, exportedString);
        }

        [TestMethod]
        public void ReadMappings()
        {
            var projectData = new ExtendedProjectData();
            var globalId = Guid.Parse("0a060b64-94a5-4d04-8acc-52844be9629c");
            projectData.SetCallingLocation(new DummyReferenceLocation(globalId));

            List<GeometryRelationsFileMapping> mappings = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_GRFMDXF_ReadMappingsV20)))
            {
                mappings = GeometryRelationsFileMappingDxfIO.Read(reader, new DXFParserInfo(projectData.GeometryRelations.CalledFromLocation.GlobalID, projectData));
            }

            Assert.IsNotNull(mappings);
            Assert.AreEqual(3, mappings.Count);

            Assert.IsTrue(mappings.Any(x => x.FileId == 1 && x.FilePath == "test"));
            Assert.IsTrue(mappings.Any(x => x.FileId == 2 && x.FilePath == "test2"));
            Assert.IsTrue(mappings.Any(x => x.FileId == 3 && x.FilePath == "test3"));
        }

        #endregion
    }
}
