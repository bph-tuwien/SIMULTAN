using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Excel;
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
    public class ComponentDXFExcelMappingTests
    {
        [TestMethod]
        public void WriteExcelMapping()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            var mapping = new ExcelComponentMapping(new List<long> { 5566, 9876 }, "ExcelTool", "ExcelRule", 1);
            
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteExcelComponentMapping(mapping, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteExcelMapping, exportedString);
        }

        [TestMethod]
        public void ReadMappingV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExcelComponentMapping mapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ExcelMappingV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                mapping = ComponentDxfIOComponents.ExcelComponentMappingElement.Parse(reader, info);
            }

            Assert.IsNotNull(mapping);
            Assert.AreEqual("ExcelTool", mapping.ToolName);
            Assert.AreEqual("ExcelRule", mapping.RuleName);
            Assert.AreEqual(1, mapping.RuleIndexInTool);

            var path = mapping.Path.ToList();
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(5566, path[0]);
            Assert.AreEqual(9876, path[1]);
        }

        [TestMethod]
        public void ReadMappingV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExcelComponentMapping mapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ExcelMappingV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                mapping = ComponentDxfIOComponents.ExcelComponentMappingElement.Parse(reader, info);
            }

            Assert.IsNotNull(mapping);
            Assert.AreEqual("ExcelTool", mapping.ToolName);
            Assert.AreEqual("ExcelRule", mapping.RuleName);
            Assert.AreEqual(1, mapping.RuleIndexInTool);

            var path = mapping.Path.ToList();
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(5566, path[0]);
            Assert.AreEqual(9876, path[1]);
        }

        [TestMethod]
        public void ReadMappingV8()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExcelComponentMapping mapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ExcelMappingV8)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 8;

                reader.Read();

                mapping = ComponentDxfIOComponents.ExcelComponentMappingElement.Parse(reader, info);
            }

            Assert.IsNotNull(mapping);
            Assert.AreEqual("ExcelTool", mapping.ToolName);
            Assert.AreEqual("ExcelRule", mapping.RuleName);
            Assert.AreEqual(1, mapping.RuleIndexInTool);

            var path = mapping.Path.ToList();
            Assert.AreEqual(2, path.Count);
            Assert.AreEqual(1, path[0]);
            Assert.AreEqual(2, path[1]);
        }
    }
}
