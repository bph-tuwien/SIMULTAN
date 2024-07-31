using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.ValueMappings
{
    [TestClass]
    public class SimValueMappingCollectionTests : BaseProjectTest
    {
        private static readonly FileInfo emptyProject = new FileInfo(@"./EmptyProject.simultan");

        [TestMethod]
        public void AddValueMapping()
        {
            LoadProject(emptyProject, "bph", "bph");

            var mapping = SimValueMappingTests.CreateMapping();

            Assert.AreEqual(SimId.Empty, mapping.Id);
            Assert.AreEqual(null, mapping.Factory);

            projectData.ValueMappings.Add(mapping);

            Assert.AreNotEqual(0, mapping.LocalID);
            Assert.AreEqual(project, mapping.Id.Location);
            Assert.IsTrue(projectData.ValueMappings.Contains(mapping));
            Assert.AreEqual(projectData.ValueMappings, mapping.Factory);

            Assert.AreEqual(mapping, projectData.IdGenerator.GetById<SimValueMapping>(new SimId(project, mapping.Id.LocalId)));
        }

        [TestMethod]
        public void AddValueMappingTwice()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            var mapping = SimValueMappingTests.CreateMapping();
            projectData.ValueMappings.Add(mapping);

            //Test
            Assert.ThrowsException<ArgumentException>(() => { projectData.ValueMappings.Add(mapping); });
        }

        [TestMethod]
        public void AddValueMappingExceptions()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Exception
            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueMappings.Add(null); });

            //Add with existing id
            var mapping = SimValueMappingTests.CreateMapping();
            mapping.Id = new SimId(99);

            Assert.ThrowsException<NotSupportedException>(() => { projectData.ValueMappings.Add(mapping); });
        }

        [TestMethod]
        public void RemoveValueMapping()
        {
            LoadProject(emptyProject);

            //Setup
            var mapping = SimValueMappingTests.CreateMapping();

            projectData.ValueMappings.Add(mapping);

            //Actual Test

            var localId = mapping.Id.LocalId;

            projectData.ValueMappings.Remove(mapping);

            Assert.IsFalse(projectData.ValueMappings.Contains(mapping));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimValueMapping>(new SimId(project, localId)));
            Assert.AreEqual(null, mapping.Factory);

            Assert.AreEqual(localId, mapping.Id.LocalId);
            Assert.AreEqual(null, mapping.Id.Location);
        }

        [TestMethod]
        public void ReplaceValueMapping()
        {
            LoadProject(emptyProject);

            //Setup
            var mapping = SimValueMappingTests.CreateMapping();
            var mapping2 = SimValueMappingTests.CreateMapping();

            projectData.ValueMappings.Add(mapping);

            var localId = mapping.Id.LocalId;

            //Exception
            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueMappings[0] = null; });

            //Test
            projectData.ValueMappings[0] = mapping2;

            //Old
            Assert.IsFalse(projectData.ValueMappings.Contains(mapping));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimValueMapping>(new SimId(project, localId)));
            Assert.AreEqual(null, mapping.Factory);
            Assert.AreEqual(localId, mapping.Id.LocalId);
            Assert.AreEqual(null, mapping.Id.Location);

            //New
            Assert.IsTrue(projectData.ValueMappings.Contains(mapping2));
            Assert.AreEqual(mapping2, projectData.IdGenerator.GetById<SimValueMapping>(new SimId(project, mapping2.LocalID)));
            Assert.AreEqual(projectData.ValueMappings, mapping2.Factory);
            Assert.AreNotEqual(0, mapping2.Id.LocalId);
            Assert.AreEqual(project, mapping2.Id.Location);
        }

        [TestMethod]
        public void ClearValueMappings()
        {
            LoadProject(emptyProject);

            //Setup
            SimValueMapping[] mappings = new SimValueMapping[]
            {
                SimValueMappingTests.CreateMapping(),
                SimValueMappingTests.CreateMapping(),
                SimValueMappingTests.CreateMapping()
            };

            projectData.ValueMappings.AddRange(mappings);
            var ids = mappings.ToDictionary(x => x, x => x.Id.LocalId);

            //Test
            projectData.ValueMappings.Clear();

            //Old
            foreach (var mapping in mappings)
            {
                Assert.AreNotEqual(0, ids[mapping]);
                Assert.IsFalse(projectData.ValueMappings.Contains(mapping));
                Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, ids[mapping])));
                Assert.AreEqual(null, mapping.Factory);
                Assert.AreEqual(ids[mapping], mapping.Id.LocalId);
                Assert.AreEqual(null, mapping.Id.Location);
            }
        }
    }
}
