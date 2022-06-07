using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Tests.Utils;
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
            WeakReference gmRef = new WeakReference(gm);
            gm = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(gmRef.IsAlive);

            projectData.GeometryModels.RemoveGeometryModel((GeometryModel)gmRef.Target);

            Assert.AreEqual(0, projectData.GeometryModels.Count());

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

        [TestMethod]
        public void RemoveWithLinked()
        {
            LoadProject(instanceProject);

            var resource = (ResourceFileEntry)projectData.AssetManager.Resources.FirstOrDefault(x => x.Name == "Building.simgeo");

            var errors = new List<SimGeoIOError>();
            var gm = SimGeoIO.Load(resource, projectData, errors);
            projectData.GeometryModels.AddGeometryModel(gm);
            WeakReference gmRef = new WeakReference(gm);
            WeakReference childGmRef = new WeakReference(gm.LinkedModels[0]);
            gm = null;

            Assert.IsTrue(gmRef.IsAlive);
            Assert.IsTrue(childGmRef.IsAlive);

            projectData.GeometryModels.RemoveGeometryModel((GeometryModel)gmRef.Target);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.AreEqual(0, projectData.GeometryModels.Count());
            Assert.IsFalse(gmRef.IsAlive);
            Assert.IsFalse(childGmRef.IsAlive);
        }

        #endregion
    }
}
