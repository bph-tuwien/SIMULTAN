using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Taxonomy
{


    [TestClass]
    public class TaxonomyTests : BaseProjectTest
    {
        private class IsBeingDeletedEventCounter
        {
            internal class DeleteCounterReference
            {
                public SimTaxonomyEntryReference reference;
                public IsBeingDeletedEventCounter counter;

                public DeleteCounterReference(SimTaxonomyEntryReference reference, IsBeingDeletedEventCounter counter)
                {
                    this.reference = reference;
                    this.counter = counter;
                }

                public void OnIsBeingDeleted(SimTaxonomyEntry caller)
                {
                    counter.OnIsBeingDeleted(reference.Target);
                }
            }

            public int Count => count;
            private int count = 0;
            public bool WasCalled
            {
                get => count > 0;
            }
            public object LastCaller { get => Callers.Count > 0 ? Callers[Callers.Count - 1] : null; }
            public List<object> Callers { get; private set; }

            public IsBeingDeletedEventCounter()
            {
                Callers = new List<object>();
            }

            public void OnIsBeingDeleted(object sender)
            {
                count++;
                Callers.Add(sender);
            }

            public void OnDeleteAction()
            {
                OnIsBeingDeleted(null);
            }

            public void Reset()
            {
                count = 0;
                Callers.Clear();
            }
        }

        private static readonly FileInfo emptyProject = new FileInfo(@".\EmptyProject.simultan");

        private static string TaxonomyKey = "TestTaxonomyKey";
        private static string TaxonomyName = "TestTaxonomy";
        private static string TaxonomyDescription = "TestTaxonomy";

        private static string TaxonomyEntryKey = "TestTaxonomyEntryKey";
        private static string TaxonomyEntryName = "TestTaxonomyEntry";
        private static string TaxonomyEntryDescription = "TestTaxonomyEntry";
        private static string TaxonomyEntryKey2 = "TestTaxonomyEntryKey2";
        private static string TaxonomyEntryName2 = "TestTaxonomyEntry2";
        private static string TaxonomyEntryDescription2 = "TestTaxonomyEntry2";
        private static string TaxonomyEntryKey3 = "TestTaxonomyEntryKey3";
        private static string TaxonomyEntryName3 = "TestTaxonomyEntry3";
        private static string TaxonomyEntryDescription3 = "TestTaxonomyEntry3";

        private void OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen(SimTaxonomyEntry caller)
        {
            Assert.Fail();
        }

        /// <summary>
        /// Check if all reserved parameter taxonomies can be found
        /// </summary>
        [TestMethod]
        public void DefaultParameterTaxonomiesCorrectlyLoadedTest()
        {
            LoadProject(emptyProject);

            ReservedParameterKeys.NameToKeyLookup.Values.ForEach(x =>
            {
                if (projectData.Taxonomies.GetReservedParameter(x) == null)
                {
                    Assert.Fail();
                }
            });
        }

        /// <summary>
        /// Check if all reserved slot taxonomies can be found
        /// </summary>
        [TestMethod]
        public void DefaultSlotTaxonomiesCorrectlyLoadedTest()
        {
            LoadProject(emptyProject);

            SimDefaultSlotKeys.AllSlots.ForEach(x =>
            {
                if (projectData.Taxonomies.GetDefaultSlot(x) == null)
                {
                    Assert.Fail();
                }
            });
        }

        /// <summary>
        /// Check if all reserved slot taxonomies can be found via the lookup
        /// </summary>
        [TestMethod]
        public void DefaultSlotLookupTaxonomiesCorrectlyLoadedTest()
        {
            LoadProject(emptyProject);

            SimDefaultSlots.AllSlots.ForEach(x =>
            {
                var key = SimDefaultSlotKeys.BaseToKeyLookup[x];
                if (projectData.Taxonomies.GetDefaultSlot(key) == null)
                {
                    Assert.Fail();
                }
            });
        }

        /// <summary>
        /// Adding a Taxonomy should give it an Id and set its Factory
        /// </summary>
        [TestMethod]
        public void AddTaxonomyTest()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);

            Assert.AreEqual(SimId.Empty, taxonomy.Id);
            Assert.IsNull(taxonomy.Factory);
            Assert.IsNotNull(taxonomy.Entries);
            Assert.AreEqual(0, taxonomy.Entries.Count);
            // we have two because of the default taxonomies
            Assert.AreEqual(2, projectData.Taxonomies.Count);

            projectData.Taxonomies.Add(taxonomy);

            Assert.AreEqual(3, projectData.Taxonomies.Count);
            Assert.IsTrue(projectData.Taxonomies.Contains(taxonomy));

            Assert.AreEqual(project, taxonomy.Id.Location);
            Assert.AreNotEqual(0, taxonomy.Id.LocalId);
            Assert.AreEqual(taxonomy, projectData.IdGenerator.GetById<SimTaxonomy>(taxonomy.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomy.Factory);
            Assert.AreEqual(0, taxonomy.Entries.Count);
        }

        /// <summary>
        /// Adding an SimTaxonomyEntry to a Taxonomy that was already added to a Factory (is active) should
        /// give it a proper ID and set its Factory and Taxonomy. Also should update the Parent entry if 
        /// it is a entry sub-entry.
        /// </summary>
        [TestMethod]
        public void AddTaxonomyEntryToActiveTaxonomy()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);
            projectData.Taxonomies.Add(taxonomy);

            var taxonomyEntry = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName, TaxonomyEntryDescription);
            var reference = new SimTaxonomyEntryReference(taxonomyEntry);
            taxonomyEntry.AddDeleteReference(reference, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);

            Assert.AreEqual(SimId.Empty, taxonomyEntry.Id);
            Assert.IsNull(taxonomyEntry.Factory);
            Assert.IsNotNull(taxonomyEntry.Children);
            Assert.IsNull(taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);
            Assert.AreEqual(0, taxonomyEntry.Children.Count);
            Assert.AreEqual(0, taxonomy.Entries.Count);
            Assert.IsFalse(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            taxonomy.Entries.Add(taxonomyEntry);

            Assert.AreEqual(1, taxonomy.Entries.Count);
            Assert.IsTrue(taxonomy.Entries.Contains(taxonomyEntry));

            Assert.AreEqual(project, taxonomyEntry.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry.Id.LocalId);
            Assert.AreEqual(taxonomyEntry, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);
            Assert.AreEqual(0, taxonomyEntry.Children.Count);
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            var taxonomyEntry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2, TaxonomyEntryDescription2);
            var reference2 = new SimTaxonomyEntryReference(taxonomyEntry2);
            taxonomyEntry2.AddDeleteReference(reference2, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);
            taxonomyEntry.Children.Add(taxonomyEntry2);

            Assert.AreEqual(1, taxonomy.Entries.Count);
            Assert.AreEqual(1, taxonomyEntry.Children.Count);
            Assert.IsTrue(taxonomyEntry.Children.Contains(taxonomyEntry2));

            Assert.AreEqual(project, taxonomyEntry2.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(taxonomyEntry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);
            Assert.AreEqual(0, taxonomyEntry2.Children.Count);
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey2));

            taxonomyEntry.RemoveDeleteReference(reference);
            taxonomyEntry2.RemoveDeleteReference(reference2);
        }

        /// <summary>
        /// Adding a whole SimTaxonomyEntry subtree to a Taxonomy that was already added to a Factory (is active) should
        /// give it a proper ID and set its Factory and Taxonomy. Also should update the Parent entry if 
        /// it is a entry sub-entry.
        /// </summary>
        [TestMethod]
        public void AddTaxonomyEntrySubtreeToActiveTaxonomy()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);
            projectData.Taxonomies.Add(taxonomy);

            var taxonomyEntry = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName, TaxonomyEntryDescription);
            var reference = new SimTaxonomyEntryReference(taxonomyEntry);
            taxonomyEntry.AddDeleteReference(reference, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);

            Assert.AreEqual(SimId.Empty, taxonomyEntry.Id);
            Assert.IsNull(taxonomyEntry.Factory);
            Assert.IsNotNull(taxonomyEntry.Children);
            Assert.IsNull(taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);
            Assert.AreEqual(0, taxonomyEntry.Children.Count);
            Assert.AreEqual(0, taxonomy.Entries.Count);
            Assert.IsFalse(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            var taxonomyEntry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2, TaxonomyEntryDescription2);
            var reference2 = new SimTaxonomyEntryReference(taxonomyEntry2);
            taxonomyEntry2.AddDeleteReference(reference2, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);
            taxonomyEntry.Children.Add(taxonomyEntry2);

            Assert.AreEqual(0, taxonomy.Entries.Count);
            Assert.AreEqual(1, taxonomyEntry.Children.Count);
            Assert.IsTrue(taxonomyEntry.Children.Contains(taxonomyEntry2));

            Assert.AreEqual(SimId.Empty, taxonomyEntry2.Id);
            Assert.IsNull(taxonomyEntry2.Factory);
            Assert.IsNotNull(taxonomyEntry2.Children);
            Assert.IsNull(taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);
            Assert.AreEqual(0, taxonomyEntry2.Children.Count);
            Assert.IsFalse(taxonomy.IsKeyInUse(TaxonomyEntryKey2));

            // add subtree
            taxonomy.Entries.Add(taxonomyEntry);

            Assert.AreEqual(1, taxonomy.Entries.Count);
            Assert.IsTrue(taxonomy.Entries.Contains(taxonomyEntry));

            Assert.AreEqual(project, taxonomyEntry.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry.Id.LocalId);
            Assert.AreEqual(taxonomyEntry, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);
            Assert.AreEqual(1, taxonomyEntry.Children.Count);
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            Assert.AreEqual(project, taxonomyEntry2.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(taxonomyEntry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);
            Assert.AreEqual(0, taxonomyEntry2.Children.Count);
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey2));

            taxonomyEntry.RemoveDeleteReference(reference);
            taxonomyEntry2.RemoveDeleteReference(reference2);
        }

        /// <summary>
        /// Adding SimTaxonomyEntries to inactive Taxonomies (not added to a Factory yet) should not give them new Ids
        /// and shouldn't update the Factory.
        /// But Parents should still be updated to reflect the hierarchy.
        /// Finally adding the Taxonomy to a Factory should updated the Ids of the whole hierarchy, also sets the Factories.
        /// </summary>
        [TestMethod]
        public void AddTaxonomyEntryToInactiveTaxonomy()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);

            var taxonomyEntry = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName, TaxonomyEntryDescription);
            var reference = new SimTaxonomyEntryReference(taxonomyEntry);
            taxonomyEntry.AddDeleteReference(reference, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);

            Assert.AreEqual(SimId.Empty, taxonomyEntry.Id);
            Assert.IsNull(taxonomyEntry.Factory);
            Assert.IsNotNull(taxonomyEntry.Children);
            Assert.IsNull(taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);
            Assert.AreEqual(0, taxonomyEntry.Children.Count);
            Assert.AreEqual(0, taxonomy.Entries.Count);
            Assert.IsFalse(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            taxonomy.Entries.Add(taxonomyEntry);

            Assert.AreEqual(1, taxonomy.Entries.Count);
            Assert.IsTrue(taxonomy.Entries.Contains(taxonomyEntry));

            Assert.AreEqual(SimId.Empty, taxonomyEntry.Id);
            Assert.AreEqual(taxonomy, taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);
            Assert.IsNull(taxonomyEntry.Factory);
            Assert.AreEqual(0, taxonomyEntry.Children.Count);
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            var taxonomyEntry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2, TaxonomyEntryDescription2);
            var reference2 = new SimTaxonomyEntryReference(taxonomyEntry2);
            taxonomyEntry2.AddDeleteReference(reference2, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);
            taxonomyEntry.Children.Add(taxonomyEntry2);

            Assert.AreEqual(1, taxonomy.Entries.Count);
            Assert.AreEqual(1, taxonomyEntry.Children.Count);
            Assert.IsTrue(taxonomyEntry.Children.Contains(taxonomyEntry2));

            Assert.AreEqual(SimId.Empty, taxonomyEntry2.Id);
            Assert.AreEqual(taxonomy, taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);
            Assert.IsNull(taxonomyEntry2.Factory);
            Assert.AreEqual(0, taxonomyEntry2.Children.Count);
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey2));

            // add to factory
            projectData.Taxonomies.Add(taxonomy);

            // now all the ids should be set, also the factories
            Assert.AreEqual(project, taxonomy.Id.Location);
            Assert.AreNotEqual(0, taxonomy.Id.LocalId);
            Assert.AreEqual(taxonomy, projectData.IdGenerator.GetById<SimTaxonomy>(taxonomy.Id));

            Assert.AreEqual(project, taxonomyEntry.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry.Id.LocalId);
            Assert.AreEqual(taxonomyEntry, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry.Factory);

            Assert.AreEqual(project, taxonomyEntry2.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(taxonomyEntry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);

            taxonomyEntry.RemoveDeleteReference(reference);
            taxonomyEntry2.RemoveDeleteReference(reference2);
        }

        [TestMethod]
        public void GetTaxonomyEntryByKeyTest()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);
            projectData.Taxonomies.Add(taxonomy);

            Assert.IsNull(taxonomy.GetTaxonomyEntryByKey(TaxonomyEntryKey));
            Assert.IsNull(taxonomy.GetTaxonomyEntryByKey(TaxonomyEntryKey2));

            var taxonomyEntry = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName, TaxonomyEntryDescription);
            taxonomy.Entries.Add(taxonomyEntry);

            var taxonomyEntry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2, TaxonomyEntryDescription2);
            taxonomyEntry.Children.Add(taxonomyEntry2);

            Assert.AreEqual(taxonomyEntry, taxonomy.GetTaxonomyEntryByKey(TaxonomyEntryKey));
            Assert.AreEqual(taxonomyEntry2, taxonomy.GetTaxonomyEntryByKey(TaxonomyEntryKey2));
        }

        /// <summary>
        /// Removing a Taxonomy from the factory should reset its Id and Factory.
        /// </summary>
        [TestMethod]
        public void RemoveTaxonomyTest()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);
            projectData.Taxonomies.Add(taxonomy);

            // check if properly added
            Assert.AreEqual(3, projectData.Taxonomies.Count);
            Assert.IsTrue(projectData.Taxonomies.Contains(taxonomy));

            Assert.AreEqual(project, taxonomy.Id.Location);
            Assert.AreNotEqual(0, taxonomy.Id.LocalId);
            Assert.AreEqual(taxonomy, projectData.IdGenerator.GetById<SimTaxonomy>(taxonomy.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomy.Factory);
            Assert.AreEqual(0, taxonomy.Entries.Count);

            var localId = taxonomy.Id.LocalId;

            projectData.Taxonomies.Remove(taxonomy);

            Assert.AreEqual(2, projectData.Taxonomies.Count);

            Assert.AreEqual(localId, taxonomy.Id.LocalId);
            Assert.IsNull(taxonomy.Id.Location);
            Assert.IsNull(projectData.IdGenerator.GetById<SimTaxonomy>(taxonomy.Id));
            Assert.IsNull(taxonomy.Factory);
        }

        [TestMethod]
        public void RemoveTaxonomyEntryFromActiveTaxonomy()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);
            projectData.Taxonomies.Add(taxonomy);

            var deleteCounter = new IsBeingDeletedEventCounter();
            var taxonomyEntry = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName, TaxonomyEntryDescription);

            taxonomy.Entries.Add(taxonomyEntry);

            Assert.AreEqual(projectData.Project.GlobalID, taxonomyEntry.Id.GlobalId);
            Assert.AreNotEqual(0, taxonomyEntry.Id.LocalId);
            Assert.AreEqual(taxonomyEntry, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);

            var taxonomyEntry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2, TaxonomyEntryDescription2);
            taxonomyEntry.Children.Add(taxonomyEntry2);

            Assert.AreEqual(projectData.Project.GlobalID, taxonomyEntry2.Id.GlobalId);
            Assert.AreNotEqual(0, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(taxonomyEntry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);

            // Start removing
            var reference = new SimTaxonomyEntryReference(taxonomyEntry);
            var countRef = new IsBeingDeletedEventCounter.DeleteCounterReference(reference, deleteCounter);
            taxonomyEntry.AddDeleteReference(reference, countRef.OnIsBeingDeleted);
            var reference2 = new SimTaxonomyEntryReference(taxonomyEntry2);
            var countRef2 = new IsBeingDeletedEventCounter.DeleteCounterReference(reference2, deleteCounter);
            taxonomyEntry2.AddDeleteReference(reference2, countRef2.OnIsBeingDeleted);

            var localId = taxonomyEntry2.Id.LocalId;
            taxonomyEntry.Children.Remove(taxonomyEntry2);

            Assert.AreEqual(localId, taxonomyEntry2.Id.LocalId);
            Assert.IsNull(taxonomyEntry2.Id.Location);
            Assert.IsNull(projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.IsNull(taxonomyEntry2.Factory);
            Assert.IsNull(taxonomyEntry2.Taxonomy);
            Assert.IsNull(taxonomyEntry2.Parent);
            Assert.IsFalse(taxonomy.IsKeyInUse(TaxonomyEntryKey2));

            Assert.AreEqual(1, deleteCounter.Count);
            Assert.AreEqual(taxonomyEntry2, deleteCounter.LastCaller);
            deleteCounter.Reset();

            localId = taxonomyEntry.Id.LocalId;
            taxonomy.Entries.Remove(taxonomyEntry);

            Assert.AreEqual(localId, taxonomyEntry.Id.LocalId);
            Assert.IsNull(taxonomyEntry.Id.Location);
            Assert.IsNull(projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry.Id));
            Assert.IsNull(taxonomyEntry.Factory);
            Assert.IsNull(taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);
            Assert.IsFalse(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            Assert.AreEqual(1, deleteCounter.Count);
            Assert.AreEqual(taxonomyEntry, deleteCounter.LastCaller);

            taxonomyEntry.RemoveDeleteReference(reference);
            taxonomyEntry2.RemoveDeleteReference(reference2);
        }

        [TestMethod]
        public void RemoveTaxonomyEntrySubtreeFromActiveTaxonomy()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);
            projectData.Taxonomies.Add(taxonomy);

            var taxonomyEntry = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName, TaxonomyEntryDescription);

            taxonomy.Entries.Add(taxonomyEntry);

            Assert.AreEqual(projectData.Project.GlobalID, taxonomyEntry.Id.GlobalId);
            Assert.AreNotEqual(0, taxonomyEntry.Id.LocalId);
            Assert.AreEqual(taxonomyEntry, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);

            var taxonomyEntry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2, TaxonomyEntryDescription2);
            taxonomyEntry.Children.Add(taxonomyEntry2);

            Assert.AreEqual(projectData.Project.GlobalID, taxonomyEntry2.Id.GlobalId);
            Assert.AreNotEqual(0, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(taxonomyEntry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);

            var deleteCounter = new IsBeingDeletedEventCounter();
            var reference = new SimTaxonomyEntryReference(taxonomyEntry);
            var countRef = new IsBeingDeletedEventCounter.DeleteCounterReference(reference, deleteCounter);
            taxonomyEntry.AddDeleteReference(reference, countRef.OnIsBeingDeleted);
            var reference2 = new SimTaxonomyEntryReference(taxonomyEntry2);
            var countRef2 = new IsBeingDeletedEventCounter.DeleteCounterReference(reference2, deleteCounter);
            taxonomyEntry2.AddDeleteReference(reference2, countRef2.OnIsBeingDeleted);

            var localId = taxonomyEntry.Id.LocalId;
            var localId2 = taxonomyEntry2.Id.LocalId;
            taxonomy.Entries.Remove(taxonomyEntry);

            Assert.AreEqual(localId, taxonomyEntry.Id.LocalId);
            Assert.IsNull(taxonomyEntry.Id.Location);
            Assert.IsNull(projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry.Id));
            Assert.IsNull(taxonomyEntry.Factory);
            Assert.IsNull(taxonomyEntry.Taxonomy);
            Assert.IsNull(taxonomyEntry.Parent);
            Assert.IsFalse(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            Assert.AreEqual(localId2, taxonomyEntry2.Id.LocalId);
            Assert.IsNull(taxonomyEntry2.Id.Location);
            Assert.IsNull(projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.IsNull(taxonomyEntry2.Factory);
            Assert.IsNull(taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);
            Assert.IsFalse(taxonomy.IsKeyInUse(TaxonomyEntryKey2));

            Assert.AreEqual(2, deleteCounter.Count);
            Assert.IsTrue(deleteCounter.Callers.Contains(taxonomyEntry));
            Assert.IsTrue(deleteCounter.Callers.Contains(taxonomyEntry2));

            taxonomyEntry.RemoveDeleteReference(reference);
            taxonomyEntry2.RemoveDeleteReference(reference2);
        }

        /// <summary>
        /// Moving an entry inside a taxonomy should retain its Id.
        /// </summary>
        [TestMethod]
        public void MoveTaxonomyEntryInActiveTaxonomy()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);
            projectData.Taxonomies.Add(taxonomy);

            var taxonomyEntry = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName, TaxonomyEntryDescription);
            var reference = new SimTaxonomyEntryReference(taxonomyEntry);
            taxonomyEntry.AddDeleteReference(reference, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);
            taxonomy.Entries.Add(taxonomyEntry);

            var taxonomyEntry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2, TaxonomyEntryDescription2);
            var reference2 = new SimTaxonomyEntryReference(taxonomyEntry2);
            taxonomyEntry2.AddDeleteReference(reference2, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);
            taxonomyEntry.Children.Add(taxonomyEntry2);

            Assert.AreEqual(1, taxonomy.Entries.Count);
            Assert.AreEqual(1, taxonomyEntry.Children.Count);
            Assert.IsTrue(taxonomyEntry.Children.Contains(taxonomyEntry2));

            Assert.AreEqual(project, taxonomyEntry2.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(taxonomyEntry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);
            Assert.AreEqual(0, taxonomyEntry2.Children.Count);

            SimId id = taxonomyEntry2.Id;

            // only add it to new location, will be automatically removed from parent collection
            taxonomy.Entries.Add(taxonomyEntry2);

            Assert.AreEqual(0, taxonomyEntry2.Children.Count);
            Assert.AreEqual(2, taxonomy.Entries.Count);

            Assert.AreEqual(id.GlobalId, taxonomyEntry2.Id.GlobalId);
            Assert.AreEqual(id.LocalId, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(id.Location, taxonomyEntry2.Id.Location);
            Assert.IsNull(taxonomyEntry2.Parent);
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);

            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey));
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey2));

            taxonomyEntry.RemoveDeleteReference(reference);
            taxonomyEntry2.RemoveDeleteReference(reference2);
        }

        /// <summary>
        /// Moving an entry inside a taxonomy should retain its Id.
        /// </summary>
        [TestMethod]
        public void MoveTaxonomyEntrySubtreeInActiveTaxonomy()
        {
            LoadProject(emptyProject);
            var taxonomy = new SimTaxonomy(TaxonomyKey, TaxonomyName, TaxonomyDescription);
            projectData.Taxonomies.Add(taxonomy);

            var taxonomyEntry = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName, TaxonomyEntryDescription);
            var reference = new SimTaxonomyEntryReference(taxonomyEntry);
            taxonomyEntry.AddDeleteReference(reference, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);
            taxonomy.Entries.Add(taxonomyEntry);

            var taxonomyEntry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2, TaxonomyEntryDescription2);
            var reference2 = new SimTaxonomyEntryReference(taxonomyEntry2);
            taxonomyEntry2.AddDeleteReference(reference2, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);
            taxonomyEntry.Children.Add(taxonomyEntry2);

            var taxonomyEntry3 = new SimTaxonomyEntry(TaxonomyEntryKey3, TaxonomyEntryName3, TaxonomyEntryDescription3);
            var reference3 = new SimTaxonomyEntryReference(taxonomyEntry3);
            taxonomyEntry3.AddDeleteReference(reference3, OnTaxonomyEntryIsBeingDeleted_ShouldNotHappen);
            taxonomyEntry2.Children.Add(taxonomyEntry3);

            Assert.AreEqual(1, taxonomy.Entries.Count);
            Assert.AreEqual(1, taxonomyEntry.Children.Count);
            Assert.IsTrue(taxonomyEntry.Children.Contains(taxonomyEntry2));

            Assert.AreEqual(project, taxonomyEntry2.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(taxonomyEntry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry2.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry2.Taxonomy);
            Assert.AreEqual(taxonomyEntry, taxonomyEntry2.Parent);
            Assert.AreEqual(1, taxonomyEntry2.Children.Count);

            Assert.IsTrue(taxonomyEntry2.Children.Contains(taxonomyEntry3));
            Assert.AreEqual(project, taxonomyEntry3.Id.Location);
            Assert.AreNotEqual(0, taxonomyEntry3.Id.LocalId);
            Assert.AreEqual(taxonomyEntry3, projectData.IdGenerator.GetById<SimTaxonomyEntry>(taxonomyEntry3.Id));
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry3.Factory);
            Assert.AreEqual(taxonomy, taxonomyEntry3.Taxonomy);
            Assert.AreEqual(taxonomyEntry2, taxonomyEntry3.Parent);
            Assert.AreEqual(0, taxonomyEntry3.Children.Count);

            SimId id2 = taxonomyEntry2.Id;
            SimId id3 = taxonomyEntry3.Id;

            taxonomy.Entries.Add(taxonomyEntry2);

            Assert.AreEqual(0, taxonomyEntry.Children.Count);
            Assert.AreEqual(1, taxonomyEntry2.Children.Count);
            Assert.AreEqual(2, taxonomy.Entries.Count);

            Assert.AreEqual(id2.GlobalId, taxonomyEntry2.Id.GlobalId);
            Assert.AreEqual(id2.LocalId, taxonomyEntry2.Id.LocalId);
            Assert.AreEqual(id2.Location, taxonomyEntry2.Id.Location);
            Assert.IsNull(taxonomyEntry2.Parent);
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry2.Factory);

            Assert.AreEqual(id3.GlobalId, taxonomyEntry3.Id.GlobalId);
            Assert.AreEqual(id3.LocalId, taxonomyEntry3.Id.LocalId);
            Assert.AreEqual(id3.Location, taxonomyEntry3.Id.Location);
            Assert.AreEqual(taxonomyEntry2, taxonomyEntry3.Parent);
            Assert.AreEqual(projectData.Taxonomies, taxonomyEntry3.Factory);

            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey));
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey2));
            Assert.IsTrue(taxonomy.IsKeyInUse(TaxonomyEntryKey));

            taxonomyEntry.RemoveDeleteReference(reference);
            taxonomyEntry2.RemoveDeleteReference(reference2);
            taxonomyEntry3.RemoveDeleteReference(reference3);
        }

        [TestMethod]
        public void EntryReferenceAlwaysNeedsTarget()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new SimTaxonomyEntryReference((SimTaxonomyEntry)null));
        }

        private (WeakReference, WeakReference, WeakReference, WeakReference) DeleterMemoryLeakTest_Action()
        {
            var taxonomy = new SimTaxonomy(TaxonomyName);
            var entry1 = new SimTaxonomyEntry(TaxonomyEntryKey, TaxonomyEntryName);
            var entry2 = new SimTaxonomyEntry(TaxonomyEntryKey2, TaxonomyEntryName2);
            var entry3 = new SimTaxonomyEntry(TaxonomyEntryKey3, TaxonomyEntryName3);
            taxonomy.Entries.Add(entry1);
            entry1.Children.Add(entry2);
            entry2.Children.Add(entry3);
            projectData.Taxonomies.Add(taxonomy);

            var ref1 = new SimTaxonomyEntryReference(entry1);
            var ref2 = new SimTaxonomyEntryReference(entry2);
            var ref3 = new SimTaxonomyEntryReference(entry3);
            // Add some delete actions cause they are saved in the entries
            ref1.SetDeleteAction((SimTaxonomyEntry caller) => { Console.WriteLine(caller.Name); });
            ref2.SetDeleteAction((SimTaxonomyEntry caller) => { Console.WriteLine(caller.Name); });
            ref3.SetDeleteAction((SimTaxonomyEntry caller) => { Console.WriteLine(caller.Name); });

            var wref1 = new WeakReference(entry1);
            var wref2 = new WeakReference(entry2);
            var wref3 = new WeakReference(entry3);
            var wrefT = new WeakReference(taxonomy);
            return (wref1, wref2, wref3, wrefT);
        }
        private void DeleterMemoryLeakTest_RemoveEntry2()
        {
            var taxonomy = projectData.Taxonomies.First(x => x.Name == TaxonomyName);
            var entry1 = taxonomy.GetTaxonomyEntryByKey(TaxonomyEntryKey);
            var entry2 = taxonomy.GetTaxonomyEntryByKey(TaxonomyEntryKey2);
            entry1.Children.Remove(entry2);
        }
        private void DeleterMemoryLeakTest_RemoveTaxonomy()
        {
            var taxonomy = projectData.Taxonomies.First(x => x.Name == TaxonomyName);
            projectData.Taxonomies.Remove(taxonomy);
        }
        [TestMethod]
        public void DeleterMemoryLeakTest()
        {
            LoadProject(emptyProject);

            var (wref1, wref2, wref3, wrefT) = DeleterMemoryLeakTest_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(wref1.IsAlive);
            Assert.IsTrue(wref2.IsAlive);
            Assert.IsTrue(wref3.IsAlive);
            Assert.IsTrue(wrefT.IsAlive);

            DeleterMemoryLeakTest_RemoveEntry2();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(wref1.IsAlive);
            Assert.IsFalse(wref2.IsAlive);
            Assert.IsFalse(wref3.IsAlive);
            Assert.IsTrue(wrefT.IsAlive);

            DeleterMemoryLeakTest_RemoveTaxonomy();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(wref1.IsAlive);
            Assert.IsFalse(wref2.IsAlive);
            Assert.IsFalse(wref3.IsAlive);
            Assert.IsFalse(wrefT.IsAlive);
        }

    }
}
