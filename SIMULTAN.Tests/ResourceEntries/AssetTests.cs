using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.ResourceEntries
{
    [TestClass]
    public class AssetTests : BaseProjectTest
    {
        private static readonly FileInfo cleanupTestsProject = new FileInfo(@"./CleanupTests.simultan");

        [TestMethod]
        public void AddAsset()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = comp.AddAsset(res, "abc");

            Assert.AreEqual(1, comp.ReferencedAssets.Count);
            Assert.IsTrue(comp.ReferencedAssets.Contains(ass));
            Assert.AreEqual(res, ass.Resource);
            Assert.AreEqual(res.Key, ass.ResourceKey);
            Assert.AreEqual("abc", ass.ContainedObjectId);

            Assert.AreEqual(1, projectData.AssetManager.Assets.Count);
            Assert.IsTrue(projectData.AssetManager.Assets.ContainsKey(res.Key));
            Assert.AreEqual(1, projectData.AssetManager.Assets[res.Key].Count);
            Assert.IsTrue(projectData.AssetManager.Assets[res.Key].Contains(ass));
        }

        [TestMethod]
        public void RemoveAssetOnRootComponentDelete()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = comp.AddAsset(res, "abc");

            projectData.Components.Remove(comp);

            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);
        }



        [TestMethod]
        public void RemoveAssetOnRootComponentClear()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = comp.AddAsset(res, "abc");

            projectData.Components.Clear();

            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);
        }

        [TestMethod]
        public void RemoveAssetOnChildComponentDelete()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var tax = projectData.Taxonomies.First().Entries.First();

            var child = new SimComponent();
            child.Slots.Add(new SimTaxonomyEntryReference(tax));
            var childEntry = new SimChildComponentEntry(new SimSlot(tax, ""), child);
            comp.Components.Add(childEntry);

            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = child.AddAsset(res, "abc");

            comp.Components.Remove(childEntry);

            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);
        }

        [TestMethod]
        public void RemoveAssetOnChildComponentTreeDelete()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var tax = projectData.Taxonomies.First().Entries.First();

            var child = new SimComponent();
            child.Slots.Add(new SimTaxonomyEntryReference(tax));
            var childEntry = new SimChildComponentEntry(new SimSlot(tax, ""), child);
            comp.Components.Add(childEntry);

            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = child.AddAsset(res, "abc");

            projectData.Components.Remove(comp);

            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);
        }

        [TestMethod]
        public void RemoveAssetOnChildComponentClear()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var tax = projectData.Taxonomies.First().Entries.First();

            var child = new SimComponent();
            child.Slots.Add(new SimTaxonomyEntryReference(tax));
            var childEntry = new SimChildComponentEntry(new SimSlot(tax, ""), child);
            comp.Components.Add(childEntry);

            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = child.AddAsset(res, "abc");

            comp.Components.Clear();

            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);
        }

        [TestMethod]
        public void RemoveAssetOnChildComponentUnset()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var tax = projectData.Taxonomies.First().Entries.First();

            var child = new SimComponent();
            child.Slots.Add(new SimTaxonomyEntryReference(tax));
            var childEntry = new SimChildComponentEntry(new SimSlot(tax, ""), child);
            comp.Components.Add(childEntry);

            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = child.AddAsset(res, "abc");

            childEntry.Component = null;

            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);
        }

        [TestMethod]
        public void RestoreAssetAfterRootComponentDelete()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = comp.AddAsset(res, "abc");

            projectData.Components.Remove(comp);
            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);

            comp.Id = SimId.Empty;
            projectData.Components.Add(comp);
            Assert.AreEqual(1, projectData.AssetManager.Assets.Count);
            Assert.IsTrue(projectData.AssetManager.Assets.ContainsKey(res.Key));
            Assert.AreEqual(1, projectData.AssetManager.Assets[res.Key].Count);
            Assert.IsTrue(projectData.AssetManager.Assets[res.Key].Contains(ass));
        }

        [TestMethod]
        public void RestoreAssetAfterChildComponentDelete()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var tax = projectData.Taxonomies.First().Entries.First();

            var child = new SimComponent();
            child.Slots.Add(new SimTaxonomyEntryReference(tax));
            var childEntry = new SimChildComponentEntry(new SimSlot(tax, ""), child);
            comp.Components.Add(childEntry);

            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = child.AddAsset(res, "abc");

            comp.Components.Remove(childEntry);

            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);

            childEntry.Component.Id = SimId.Empty;
            comp.Components.Add(childEntry);

            Assert.AreEqual(1, projectData.AssetManager.Assets.Count);
            Assert.IsTrue(projectData.AssetManager.Assets.ContainsKey(res.Key));
            Assert.AreEqual(1, projectData.AssetManager.Assets[res.Key].Count);
            Assert.IsTrue(projectData.AssetManager.Assets[res.Key].Contains(ass));
        }

        [TestMethod]
        public void RestoreAssetAfterChildComponentTreeDelete()
        {
            LoadProject(cleanupTestsProject);

            //Add asset
            var comp = projectData.Components.First();
            var tax = projectData.Taxonomies.First().Entries.First();

            var child = new SimComponent();
            child.Slots.Add(new SimTaxonomyEntryReference(tax));
            var childEntry = new SimChildComponentEntry(new SimSlot(tax, ""), child);
            comp.Components.Add(childEntry);

            var res = projectData.AssetManager.Resources.OfType<ResourceFileEntry>().First();
            var ass = child.AddAsset(res, "abc");

            projectData.Components.Remove(comp);

            Assert.AreEqual(0, projectData.AssetManager.Assets.Count);

            comp.Id = SimId.Empty;
            childEntry.Component.Id = SimId.Empty;
            projectData.Components.Add(comp);

            Assert.AreEqual(1, projectData.AssetManager.Assets.Count);
            Assert.IsTrue(projectData.AssetManager.Assets.ContainsKey(res.Key));
            Assert.AreEqual(1, projectData.AssetManager.Assets[res.Key].Count);
            Assert.IsTrue(projectData.AssetManager.Assets[res.Key].Contains(ass));
        }
    }
}
