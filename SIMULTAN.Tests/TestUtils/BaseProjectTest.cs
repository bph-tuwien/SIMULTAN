using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Projects;
using SIMULTAN.Utils;
using System.IO;

namespace SIMULTAN.Tests.TestUtils
{
    public class BaseProjectTest
    {
        protected HierarchicalProject project = null;
        protected ExtendedProjectData projectData = null;
        protected IServicesProvider sp = null;

        public void LoadProject(FileInfo projectFile)
        {
            (this.project, this.projectData, this.sp) = ProjectUtils.LoadTestData(projectFile);
        }

        public void LoadProject(FileInfo projectFile, string userName, string password)
        {
            (this.project, this.projectData, this.sp) = ProjectUtils.LoadTestData(projectFile, userName, password);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ProjectUtils.CleanupTestData(ref this.project, ref this.projectData);
            this.sp = null;
        }
    }
}
