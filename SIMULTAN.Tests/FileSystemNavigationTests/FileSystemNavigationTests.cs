using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace SIMULTAN.Tests.FileSystemNavigationTests
{
    [TestClass]
    public class FileSystemNavigationTests
    {
        [TestMethod]
        public void TestIsSubDirectoryMethod()
        {
            var workingDir = Directory.GetCurrentDirectory();
            var parent = Directory.GetParent(workingDir);
            string parentPath = parent.FullName;
            string childPath = workingDir;
            Assert.IsTrue(SIMULTAN.Utils.Files.FileSystemNavigation.IsSubdirectoryOf(parentPath, childPath, false));
        }
    }
}
