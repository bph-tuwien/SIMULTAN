using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Exceptions;
using SIMULTAN.Projects;
using SIMULTAN.Utils;
using System;
using System.IO;

namespace SIMULTAN.Tests.TestUtils
{
    public class BaseProjectTest
    {
        protected HierarchicalProject project = null;
        protected ExtendedProjectData projectData = null;
        protected IServicesProvider sp = null;

        private FileInfo projectFile = null;

        private FileInfo CopyTempFile(FileInfo projectFile)
        {
            // copy simultan file to a temp guid file so they don't collide on parallel test runs
            var guid = Guid.NewGuid();
            var newFile = new FileInfo(Path.Combine(projectFile.DirectoryName, guid.ToString() + ".simultan"));
            projectFile.CopyTo(newFile.FullName);
            return newFile;
        }

        public void LoadProject(FileInfo projectFile)
        {
            this.projectFile = CopyTempFile(projectFile);
            (this.project, this.projectData, this.sp) = ProjectUtils.LoadTestData(this.projectFile);
        }

        public void LoadProject(FileInfo projectFile, string userName, string password)
        {
            this.projectFile = CopyTempFile(projectFile);
            (this.project, this.projectData, this.sp) = ProjectUtils.LoadTestData(this.projectFile, userName, password);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ProjectUtils.CleanupTestData(ref this.project, ref this.projectData);
            this.sp = null;
            this.projectFile?.Delete();
        }
    }
}
