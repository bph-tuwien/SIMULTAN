using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.PPATH;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class PPathTests
    {
        private DirectoryInfo workingDirectory;

        [TestInitialize]
        public void Setup()
        {
            workingDirectory = ComponentDXFPublicTests.SetupTestData();
        }

        [TestCleanup]
        public void Cleanup()
        {
            ComponentDXFPublicTests.CleanupTestData(workingDirectory);
            workingDirectory = null;
        }

        [TestMethod]
        public void WritePPath()
        {
            ExtendedProjectData data = ComponentDXFPublicTests.CreateTestData(workingDirectory);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, true))
                {
                    PPathIO.Write(writer, data);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.PPathSerializer_Write, exportedString);
        }

        [TestMethod]
        public void ReadPPath()
        {
            string[] expected = new string[]
            {
                FileSystemNavigation.SanitizePath(@"F1\F2\D1.txt"),
                FileSystemNavigation.SanitizePath(@"F1\F2\"),
                FileSystemNavigation.SanitizePath(@"F1\D2.txt"),
                FileSystemNavigation.SanitizePath(@"F1\"),
                FileSystemNavigation.SanitizePath(@"F4\D4.txt"),
                FileSystemNavigation.SanitizePath(@"F4\")
            };

            List<string> result = null;
            using (StreamReader reader = new StreamReader(StringStream.Create(Resources.PPathSerializer_Read)))
            {
                result = PPathIO.Read(reader);
            }

            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.Count);

            foreach (var exp in expected)
                Assert.IsTrue(result.Contains(exp));
        }
    }
}
