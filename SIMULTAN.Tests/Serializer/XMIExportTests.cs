using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.XMI;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Serializer
{
    [TestClass]
    public class XMIExportTests : BaseProjectTest
    {
        private static readonly FileInfo exportProject = new FileInfo(@"./XMIExportTestProject.simultan");

        [TestMethod]
        public void ExportEmpty()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.Parse("30665bc9-b1f2-4204-adb1-c3e68e60c1c7")));
            string exportedString = null;

            using (StringWriterWithEncoding sw = new StringWriterWithEncoding(Encoding.UTF8)) 
            {
                XMIExporter.Export(projectData, sw);
                exportedString = sw.ToString();
            }

            AssertUtil.AreEqualMultiline(Resources.XMISerializer_Empty, exportedString);
        }

        [TestMethod]
        public void ExportTestProject()
        {
            LoadProject(exportProject, "admin", "admin");

            string exportedString = null;
            using (StringWriterWithEncoding sw = new StringWriterWithEncoding(Encoding.UTF8))
            {
                XMIExporter.Export(projectData, sw);
                exportedString = sw.ToString();
            }

            AssertUtil.AreEqualMultiline(Resources.XMISerializer_TestProject, exportedString);
        }

        [TestMethod]
        public void ExportSingle()
        {
            LoadProject(exportProject, "admin", "admin");

            string exportedString = null;
            using (StringWriterWithEncoding sw = new StringWriterWithEncoding(Encoding.UTF8))
            {
                XMIExporter.Export(projectData, projectData.SimNetworks.Where(x => x.Name == "SimNetwork"), sw);
                exportedString = sw.ToString();
            }

            AssertUtil.AreEqualMultiline(Resources.XMISerializer_ExportSingle, exportedString);
        }
    }
}
