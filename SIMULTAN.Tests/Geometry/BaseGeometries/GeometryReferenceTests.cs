using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Projects;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class GeometryReferenceTests
    {
        [TestMethod]
        public void Ctor()
        {
            ServicesProvider sp = new ServicesProvider();
            ProjectData projectData = new ExtendedProjectData();

            var gm = GeometryModelHelper.EmptyModel();
            ShapeGenerator.GenerateCube(gm.layer, new Point3D(0, 0, 0), new Point3D(1, 1, 1));

            Assert.ThrowsException<ArgumentNullException>(() => { var ref0 = new GeometryReference(null, null); });
            Assert.ThrowsException<ArgumentException>(() => { var ref0 = new GeometryReference(Guid.NewGuid(), ulong.MaxValue, "", null, null); });
            Assert.ThrowsException<ArgumentNullException>(() => { var ref0 = new GeometryReference(Guid.NewGuid(), 1, "", null, null); });

            var v = gm.model.Geometry.Vertices[0];

            var ref2 = new GeometryReference(v, projectData.GeometryModels);
            Assert.AreEqual(v.Id, ref2.GeometryID);
            Assert.AreEqual(gm.model.Id, ref2.ModelID);
            Assert.AreEqual(v.Name, ref2.Name);
            Assert.AreEqual(v, ref2.Target);
            Assert.AreEqual(true, ref2.IsLoaded);

            var ref3 = new GeometryReference(v.ModelGeometry.Model.Id, v.Id, "asdf", null, projectData.GeometryModels);
            Assert.AreEqual(v.Id, ref3.GeometryID);
            Assert.AreEqual(gm.model.Id, ref3.ModelID);
            Assert.AreEqual("asdf", ref3.Name);
            Assert.AreEqual(null, ref3.Target);
            Assert.AreEqual(false, ref3.IsLoaded);

            var ref4 = new GeometryReference(v.ModelGeometry.Model.Id, v.Id, "asdf", v, projectData.GeometryModels);
            Assert.AreEqual(v.Id, ref4.GeometryID);
            Assert.AreEqual(gm.model.Id, ref4.ModelID);
            Assert.AreEqual(v.Name, ref4.Name);
            Assert.AreEqual(v, ref4.Target);
            Assert.AreEqual(true, ref4.IsLoaded);

            projectData.GeometryModels.AddGeometryModel(gm.model);

            var ref5 = new GeometryReference(v.ModelGeometry.Model.Id, v.Id, "asdf", v, projectData.GeometryModels);
            Assert.AreEqual(v.Id, ref5.GeometryID);
            Assert.AreEqual(gm.model.Id, ref5.ModelID);
            Assert.AreEqual(v.Name, ref5.Name);
            Assert.AreEqual(v, ref5.Target);
            Assert.AreEqual(true, ref5.IsLoaded);

            var ref6 = new GeometryReference(v.ModelGeometry.Model.Id, v.Id, "asdf", null, projectData.GeometryModels);
            Assert.AreEqual(v.Id, ref6.GeometryID);
            Assert.AreEqual(gm.model.Id, ref6.ModelID);
            Assert.AreEqual(v.Name, ref6.Name);
            Assert.AreEqual(v, ref6.Target);
            Assert.AreEqual(true, ref6.IsLoaded);
        }

        [TestMethod]
        public void Unload()
        {
            ServicesProvider sp = new ServicesProvider();
            ProjectData projectData = new ExtendedProjectData();

            var gm = GeometryModelHelper.EmptyModel();
            ShapeGenerator.GenerateCube(gm.layer, new Point3D(0, 0, 0), new Point3D(1, 1, 1));
            projectData.GeometryModels.AddGeometryModel(gm.model);

            var v = gm.model.Geometry.Vertices[0];
            var ref0 = new GeometryReference(v, projectData.GeometryModels);
            var ref0event = new PropertyChangedEventData(ref0);

            projectData.GeometryModels.RemoveGeometryModel(gm.model);

            Assert.AreEqual(v.Id, ref0.GeometryID);
            Assert.AreEqual(gm.model.Id, ref0.ModelID);
            Assert.AreEqual(v.Name, ref0.Name);
            Assert.AreEqual(null, ref0.Target);
            Assert.AreEqual(false, ref0.IsLoaded);

            Assert.AreEqual(3, ref0event.PropertyChangedData.Count);
            Assert.IsTrue(ref0event.PropertyChangedData.Contains(nameof(GeometryReference.Name)));
            Assert.IsTrue(ref0event.PropertyChangedData.Contains(nameof(GeometryReference.Target)));
            Assert.IsTrue(ref0event.PropertyChangedData.Contains(nameof(GeometryReference.IsLoaded)));
        }

        [TestMethod]
        public void Load()
        {
            ServicesProvider sp = new ServicesProvider();
            ProjectData projectData = new ExtendedProjectData();

            var gm = GeometryModelHelper.EmptyModel();
            ShapeGenerator.GenerateCube(gm.layer, new Point3D(0, 0, 0), new Point3D(1, 1, 1));

            var v = gm.model.Geometry.Vertices[0];
            var ref0 = new GeometryReference(v.ModelGeometry.Model.Id, v.Id, "", null, projectData.GeometryModels);
            var ref0event = new PropertyChangedEventData(ref0);

            projectData.GeometryModels.AddGeometryModel(gm.model);

            Assert.AreEqual(v.Id, ref0.GeometryID);
            Assert.AreEqual(gm.model.Id, ref0.ModelID);
            Assert.AreEqual(v.Name, ref0.Name);
            Assert.AreEqual(v, ref0.Target);
            Assert.AreEqual(true, ref0.IsLoaded);

            Assert.AreEqual(3, ref0event.PropertyChangedData.Count);
            Assert.IsTrue(ref0event.PropertyChangedData.Contains(nameof(GeometryReference.Name)));
            Assert.IsTrue(ref0event.PropertyChangedData.Contains(nameof(GeometryReference.Target)));
            Assert.IsTrue(ref0event.PropertyChangedData.Contains(nameof(GeometryReference.IsLoaded)));
        }
    }
}
