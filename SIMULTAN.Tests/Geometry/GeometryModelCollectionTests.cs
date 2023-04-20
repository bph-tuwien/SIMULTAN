using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry
{
    [TestClass]
    public class GeometryModelCollectionTests : BaseProjectTest
    {
        private static readonly FileInfo instanceProject = new FileInfo(@".\InstanceTestsProject.simultan");
        private static readonly FileInfo migrationProject = new FileInfo(@".\LegacyParentMigrationTest.simultan");

        #region Simple Files

        [TestMethod]
        public void Add()
        {
            LoadProject(instanceProject);

            var resource = (ResourceFileEntry)projectData.AssetManager.Resources.FirstOrDefault(x => x.Name == "Network.simgeo");

            var errors = new List<SimGeoIOError>();
            var model = SimGeoIO.Load(resource, projectData, errors);

            Assert.AreEqual(0, projectData.GeometryModels.Count());

            projectData.GeometryModels.AddGeometryModel(model);

            Assert.AreEqual(1, projectData.GeometryModels.Count());
            Assert.AreEqual(model, projectData.GeometryModels.First());
        }

        [TestMethod]
        public void Remove()
        {
            LoadProject(instanceProject);

            var resource = (ResourceFileEntry)projectData.AssetManager.Resources.FirstOrDefault(x => x.Name == "Network.simgeo");
            var errors = new List<SimGeoIOError>();
            var gm = SimGeoIO.Load(resource, projectData, errors);
            projectData.GeometryModels.AddGeometryModel(gm);

            projectData.GeometryModels.RemoveGeometryModel(gm);

            Assert.AreEqual(0, projectData.GeometryModels.Count());
        }

        private WeakReference RemoveMemoryLeak_Action()
        {
            var resource = (ResourceFileEntry)projectData.AssetManager.Resources.FirstOrDefault(x => x.Name == "Network.simgeo");
            var errors = new List<SimGeoIOError>();
            var gm = SimGeoIO.Load(resource, projectData, errors);
            projectData.GeometryModels.AddGeometryModel(gm);
            WeakReference gmRef = new WeakReference(gm);

            return gmRef;
        }
        private void RemoveMemoryLeak_Action2(WeakReference gmRef)
        {
            projectData.GeometryModels.RemoveGeometryModel((GeometryModel)gmRef.Target);
        }
        [TestMethod]
        public void RemoveMemoryLeak()
        {
            LoadProject(instanceProject);

            var gmRef = RemoveMemoryLeak_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(gmRef.IsAlive);

            RemoveMemoryLeak_Action2(gmRef);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(gmRef.IsAlive);
        }

        #endregion

        #region Linked Models

        [TestMethod]
        public void AddWithLinked()
        {
            LoadProject(instanceProject);

            var resource = (ResourceFileEntry)projectData.AssetManager.Resources.FirstOrDefault(x => x.Name == "Building.simgeo");

            var errors = new List<SimGeoIOError>();
            var model = SimGeoIO.Load(resource, projectData, errors);

            Assert.AreEqual(0, projectData.GeometryModels.Count());

            projectData.GeometryModels.AddGeometryModel(model);

            Assert.AreEqual(2, projectData.GeometryModels.Count());
            Assert.IsTrue(projectData.GeometryModels.Contains(model));
            Assert.IsTrue(projectData.GeometryModels.Contains(model.LinkedModels[0]));
        }

        private (WeakReference gmRef, WeakReference childGmRef) RemoveWithLinked_Action()
        {
            var resource = (ResourceFileEntry)projectData.AssetManager.Resources.FirstOrDefault(x => x.Name == "Building.simgeo");

            var errors = new List<SimGeoIOError>();
            var gm = SimGeoIO.Load(resource, projectData, errors);
            projectData.GeometryModels.AddGeometryModel(gm);
            WeakReference gmRef = new WeakReference(gm);
            WeakReference childGmRef = new WeakReference(gm.LinkedModels[0]);

            return (gmRef, childGmRef);
        }
        private void RemoveWithLink_Action2(WeakReference gmRef)
        {
            projectData.GeometryModels.RemoveGeometryModel((GeometryModel)gmRef.Target);
        }

        [TestMethod]
        public void RemoveWithLinked()
        {
            LoadProject(instanceProject);

            (var gmRef, var childGmRef) = RemoveWithLinked_Action();

            Assert.IsTrue(gmRef.IsAlive);
            Assert.IsTrue(childGmRef.IsAlive);

            RemoveWithLink_Action2(gmRef);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.AreEqual(0, projectData.GeometryModels.Count());
            Assert.IsFalse(gmRef.IsAlive);
            Assert.IsFalse(childGmRef.IsAlive);
        }

        #endregion

        /// <summary>
        /// At SimGeo version 12 the BaseGeometry parents were replaced with <see cref="SimGeometryRelation"/>s.
        /// This tests if the old parent were correctly migrated to relations.
        /// </summary>
        [TestMethod]
        public void LegacyParentMigrationTest()
        {
            LoadProject(migrationProject);

            // load parent first so migration works
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("parent.simgeo", projectData, sp);
            (var gm1, var resource1) = ProjectUtils.LoadGeometry("child.simgeo", projectData, sp);

            var vertex1 = gm1.Geometry.Vertices[0];
            var vertex2 = gm1.Geometry.Vertices[1];
            var vol = gm2.Geometry.Volumes[0];

            Assert.AreEqual(2, projectData.GeometryRelations.Count);

            var relations = projectData.GeometryRelations.GetRelationsOf(vertex1).ToList();
            Assert.AreEqual(1, relations.Count);
            Assert.IsFalse(relations[0].IsAutogenerated);
            Assert.AreEqual(projectData.Owner.GlobalID, relations[0].Source.ProjectId);
            Assert.AreEqual(resource1.Key, relations[0].Source.FileId);
            Assert.AreEqual(vertex1.Id, relations[0].Source.BaseGeometryId);
            Assert.AreEqual(projectData.Owner.GlobalID, relations[0].Target.ProjectId);
            Assert.AreEqual(resource2.Key, relations[0].Target.FileId);
            Assert.AreEqual(vol.Id, relations[0].Target.BaseGeometryId);

            relations = projectData.GeometryRelations.GetRelationsOf(vertex2).ToList();
            Assert.AreEqual(1, relations.Count);
            Assert.IsFalse(relations[0].IsAutogenerated);
            Assert.AreEqual(projectData.Owner.GlobalID, relations[0].Source.ProjectId);
            Assert.AreEqual(resource1.Key, relations[0].Source.FileId);
            Assert.AreEqual(vertex2.Id, relations[0].Source.BaseGeometryId);
            Assert.AreEqual(projectData.Owner.GlobalID, relations[0].Target.ProjectId);
            Assert.AreEqual(resource2.Key, relations[0].Target.FileId);
            Assert.AreEqual(vol.Id, relations[0].Target.BaseGeometryId);
        }
    }
}
