using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ParameterStructure.Tests
{
    [TestClass]
    public class UnitTestSetup
    {
        [AssemblyInitialize]
        public static void AssemblyStartup(TestContext context)
        {
            var executionDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);

            foreach (var folder in Directory.GetDirectories(executionDir, "~*", SearchOption.TopDirectoryOnly))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
