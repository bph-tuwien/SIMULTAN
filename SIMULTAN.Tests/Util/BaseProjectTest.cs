using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Projects;
using SIMULTAN.UI.Services;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Utils
{
    public class BaseProjectTest
    {
        protected HierarchicalProject project = null;
        protected ExtendedProjectData projectData = null;
        protected IServicesProvider sp = null;

        public void LoadProject(FileInfo projectFile)
        {
            (project, projectData, sp) = ProjectUtils.LoadTestData(projectFile);
        }

        public void LoadProject(FileInfo projectFile, string userName, string password)
        {
            (project, projectData, sp) = ProjectUtils.LoadTestData(projectFile, userName, password);
        }

        [TestCleanup]
        public void Cleanup()
        {
            ProjectUtils.CleanupTestData(ref project, ref projectData);
            sp = null;
        }
    }
}
