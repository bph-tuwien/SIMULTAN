using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SIMULTAN.Tests.ResourceEntries
{
    /// <summary>
    /// Tests for Resource Entries
    /// </summary>
    [TestClass]
    public class ResourceEntryTests
    {

        private WeakReference ResourceEntryTagsMemoryLeakTest_Action()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var workdir = Path.Combine(Directory.GetCurrentDirectory(), "TestWorkDir");
            Directory.CreateDirectory(workdir);
            projectData.AssetManager.WorkingDirectory = workdir;

            ContainedResourceFileEntry file = new ContainedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_DEVELOPER, "MyContainedFile.txt", false, 11, false);

            // get some tax entry to test tags
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_AGGREGATION_OPERATION);
            file.Tags.Add(new Data.Taxonomy.SimTaxonomyEntryReference(taxEntry));

            WeakReference weakRef = new WeakReference(file);

            Assert.IsTrue(weakRef.IsAlive);

            // keep the file alive
            Debug.WriteLine(file.Tags[0].Target.Localization.Localize().Name);

            return weakRef;
        }
        [TestMethod]
        public void ResourceEntryTagsMemoryLeakTest()
        {
            var weakRef = ResourceEntryTagsMemoryLeakTest_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(weakRef.IsAlive);
        }
    }
}
