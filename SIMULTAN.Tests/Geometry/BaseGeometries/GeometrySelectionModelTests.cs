using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class GeometrySelectionModelTests
    {
        Vertex[] TestData(Layer layer)
        {
            Vertex[] v = new Vertex[]
            {
                new Vertex(layer, "", new SimPoint3D(0,0,0)),
                new Vertex(layer, "", new SimPoint3D(1,2,3)),
                new Vertex(layer, "", new SimPoint3D(2,4,6)),
                new Vertex(layer, "", new SimPoint3D(3,6,9)),
                new Vertex(layer, "", new SimPoint3D(4,8,12)),
                new Vertex(layer, "", new SimPoint3D(5,10,15)),
            };

            return v;
        }

        [TestMethod]
        public void Ctor()
        {
            var data = GeometryModelHelper.EmptyModel();
            var models = new ObservableCollection<GeometryModel>() { data.model };

            Assert.ThrowsException<ArgumentNullException>(() => { var gsm0 = new GeometrySelectionModel(null); });

            var gsm1 = new GeometrySelectionModel(models);
            Assert.AreEqual(0, gsm1.SelectedGeometry.Count);
            Assert.AreEqual(null, gsm1.ActiveGeometry);
        }

        [TestMethod]
        public void SelectSingle()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var gsm1EventData = new SelectionModelEventData(gsm1);

            gsm1.Select(testdata[0], false);
            Assert.AreEqual(testdata[0], gsm1.SelectedGeometry.ElementAt(0));
            Assert.AreEqual(testdata[0], gsm1.ActiveGeometry);
            Assert.AreEqual(testdata[0], gsm1EventData.SelectionChangedEventData.ElementAt(0).Added.ElementAt(0));
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(0).Removed);
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(0).Reason);
            Assert.AreEqual(null, gsm1EventData.ActiveChangedEventData.ElementAt(0).OldValue);
            Assert.AreEqual(testdata[0], gsm1EventData.ActiveChangedEventData.ElementAt(0).NewValue);

            gsm1.Select(testdata[1], false);
            Assert.AreEqual(testdata[1], gsm1.SelectedGeometry.ElementAt(1));
            Assert.AreEqual(testdata[1], gsm1.ActiveGeometry);
            Assert.AreEqual(testdata[1], gsm1EventData.SelectionChangedEventData.ElementAt(1).Added.ElementAt(0));
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(1).Removed);
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(1).Reason);
            Assert.AreEqual(testdata[0], gsm1EventData.ActiveChangedEventData.ElementAt(1).OldValue);
            Assert.AreEqual(testdata[1], gsm1EventData.ActiveChangedEventData.ElementAt(1).NewValue);

            gsm1.Select(testdata[2], true);
            Assert.AreEqual(testdata[2], gsm1.SelectedGeometry.ElementAt(0));
            Assert.AreEqual(testdata[2], gsm1.ActiveGeometry);
            Assert.AreEqual(testdata[2], gsm1EventData.SelectionChangedEventData.ElementAt(2).Added.ElementAt(0));
            Assert.AreEqual(2, gsm1EventData.SelectionChangedEventData.ElementAt(2).Removed.Count());
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(2).Reason);
            Assert.AreEqual(testdata[1], gsm1EventData.ActiveChangedEventData.ElementAt(2).OldValue);
            Assert.AreEqual(testdata[2], gsm1EventData.ActiveChangedEventData.ElementAt(2).NewValue);
        }

        [TestMethod]
        public void SelectMultiple()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var gsm1EventData = new SelectionModelEventData(gsm1);

            Assert.ThrowsException<ArgumentNullException>(() => { gsm1.Select((IEnumerable<BaseGeometry>)null, false); });

            gsm1.Select(new BaseGeometry[] { testdata[0], testdata[1] }, false);
            Assert.AreEqual(2, gsm1.SelectedGeometry.Count);
            Assert.IsTrue(gsm1.SelectedGeometry.Contains(testdata[0]));
            Assert.IsTrue(gsm1.SelectedGeometry.Contains(testdata[1]));
            Assert.AreEqual(testdata[0], gsm1.ActiveGeometry);
            Assert.AreEqual(2, gsm1EventData.SelectionChangedEventData.ElementAt(0).Added.Count());
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(0).Added.Contains(testdata[0]));
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(0).Added.Contains(testdata[1]));
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(0).Removed);
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(0).Reason);
            Assert.AreEqual(null, gsm1EventData.ActiveChangedEventData.ElementAt(0).OldValue);
            Assert.AreEqual(testdata[0], gsm1EventData.ActiveChangedEventData.ElementAt(0).NewValue);

            gsm1.Select(new BaseGeometry[] { testdata[2], testdata[3] }, false);
            Assert.AreEqual(4, gsm1.SelectedGeometry.Count);
            Assert.IsTrue(gsm1.SelectedGeometry.Contains(testdata[2]));
            Assert.IsTrue(gsm1.SelectedGeometry.Contains(testdata[3]));
            Assert.AreEqual(testdata[2], gsm1.ActiveGeometry);
            Assert.AreEqual(2, gsm1EventData.SelectionChangedEventData.ElementAt(1).Added.Count());
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(1).Added.Contains(testdata[2]));
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(1).Added.Contains(testdata[3]));
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(1).Removed);
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(1).Reason);
            Assert.AreEqual(testdata[0], gsm1EventData.ActiveChangedEventData.ElementAt(1).OldValue);
            Assert.AreEqual(testdata[2], gsm1EventData.ActiveChangedEventData.ElementAt(1).NewValue);

            gsm1.Select(new BaseGeometry[] { testdata[4], testdata[5] }, true);
            Assert.AreEqual(2, gsm1.SelectedGeometry.Count);
            Assert.IsTrue(gsm1.SelectedGeometry.Contains(testdata[4]));
            Assert.IsTrue(gsm1.SelectedGeometry.Contains(testdata[5]));
            Assert.AreEqual(testdata[4], gsm1.ActiveGeometry);
            Assert.AreEqual(2, gsm1EventData.SelectionChangedEventData.ElementAt(2).Added.Count());
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(2).Added.Contains(testdata[4]));
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(2).Added.Contains(testdata[5]));
            Assert.AreEqual(4, gsm1EventData.SelectionChangedEventData.ElementAt(2).Removed.Count());
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(2).Removed.Contains(testdata[0]));
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(2).Removed.Contains(testdata[1]));
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(2).Removed.Contains(testdata[2]));
            Assert.IsTrue(gsm1EventData.SelectionChangedEventData.ElementAt(2).Removed.Contains(testdata[3]));
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(2).Reason);
            Assert.AreEqual(testdata[2], gsm1EventData.ActiveChangedEventData.ElementAt(2).OldValue);
            Assert.AreEqual(testdata[4], gsm1EventData.ActiveChangedEventData.ElementAt(2).NewValue);
        }

        [TestMethod]
        public void SelectModel()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var data2 = GeometryModelHelper.EmptyModel();
            var testdata2 = TestData(data2.layer);
            var data3 = GeometryModelHelper.EmptyModel();
            var testdata3 = TestData(data3.layer);

            var models = new ObservableCollection<GeometryModel>()
            {
                data.model,
                data2.model,
                data3.model,
            };

            GeometrySelectionModel gsm0 = new GeometrySelectionModel(models);
            var gsm0EventData = new SelectionModelEventData(gsm0);

            gsm0.Select(data.model.Geometry, false);
            Assert.AreEqual(6, gsm0.SelectedGeometry.Count);
            Assert.IsNotNull(gsm0.ActiveGeometry);
            Assert.AreEqual(6, gsm0EventData.SelectionChangedEventData.ElementAt(0).Added.Count());
            Assert.AreEqual(null, gsm0EventData.SelectionChangedEventData.ElementAt(0).Removed);
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm0EventData.SelectionChangedEventData.ElementAt(0).Reason);
            Assert.IsNull(gsm0EventData.ActiveChangedEventData.ElementAt(0).OldValue);
            Assert.IsNotNull(gsm0EventData.ActiveChangedEventData.ElementAt(0).NewValue);

            gsm0.Select(data2.model.Geometry, false);
            Assert.AreEqual(12, gsm0.SelectedGeometry.Count);
            Assert.AreEqual(data2.model.Geometry, gsm0.ActiveGeometry.ModelGeometry);
            Assert.AreEqual(6, gsm0EventData.SelectionChangedEventData.ElementAt(1).Added.Count());
            Assert.AreEqual(null, gsm0EventData.SelectionChangedEventData.ElementAt(1).Removed);
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm0EventData.SelectionChangedEventData.ElementAt(1).Reason);
            Assert.AreEqual(data.model.Geometry, gsm0EventData.ActiveChangedEventData.ElementAt(1).OldValue.ModelGeometry);
            Assert.AreEqual(data2.model.Geometry, gsm0EventData.ActiveChangedEventData.ElementAt(1).NewValue.ModelGeometry);

            gsm0.Select(data3.model.Geometry, true);
            Assert.AreEqual(6, gsm0.SelectedGeometry.Count);
            Assert.AreEqual(data3.model.Geometry, gsm0.ActiveGeometry.ModelGeometry);
            Assert.AreEqual(6, gsm0EventData.SelectionChangedEventData.ElementAt(2).Added.Count());
            Assert.AreEqual(12, gsm0EventData.SelectionChangedEventData.ElementAt(2).Removed.Count());
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm0EventData.SelectionChangedEventData.ElementAt(2).Reason);
            Assert.AreEqual(data2.model.Geometry, gsm0EventData.ActiveChangedEventData.ElementAt(2).OldValue.ModelGeometry);
            Assert.AreEqual(data3.model.Geometry, gsm0EventData.ActiveChangedEventData.ElementAt(2).NewValue.ModelGeometry);
        }

        [TestMethod]
        public void ModelCollectionChanged()
        {
            var data = GeometryModelHelper.EmptyModel();
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var eventData = new SelectionModelEventData(gsm1);

            var data2 = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data2.layer);
            gsm1.Select(testdata[0], false);
            models.Add(data2.model);

            Assert.AreEqual(1, gsm1.SelectedGeometry.Count);
            Assert.AreEqual(1, eventData.SelectionChangedEventData.Count);
            Assert.AreEqual(1, eventData.ActiveChangedEventData.Count);

            testdata[0].RemoveFromModel();
            Assert.AreEqual(0, gsm1.SelectedGeometry.Count);
            Assert.AreEqual(2, eventData.SelectionChangedEventData.Count);
            Assert.AreEqual(2, eventData.ActiveChangedEventData.Count);

            gsm1.Select(testdata[1], false);
            eventData.Reset();

            models.Remove(data.model);
            Assert.AreEqual(1, gsm1.SelectedGeometry.Count);
            Assert.AreEqual(0, eventData.SelectionChangedEventData.Count);
            Assert.AreEqual(0, eventData.ActiveChangedEventData.Count);

            models.Remove(data2.model);
            Assert.AreEqual(0, gsm1.SelectedGeometry.Count);
            Assert.AreEqual(1, eventData.SelectionChangedEventData.Count);
            Assert.AreEqual(1, eventData.ActiveChangedEventData.Count);
        }

        [TestMethod]
        public void Clear()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var gsm1EventData = new SelectionModelEventData(gsm1);

            gsm1.Select(new BaseGeometry[] { testdata[0], testdata[1] }, false);
            gsm1EventData.Reset();

            gsm1.Clear();
            Assert.AreEqual(0, gsm1.SelectedGeometry.Count);
            Assert.AreEqual(null, gsm1.ActiveGeometry);
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(0).Added);
            Assert.AreEqual(2, gsm1EventData.SelectionChangedEventData.ElementAt(0).Removed.Count());
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(0).Reason);
            Assert.AreEqual(testdata[0], gsm1EventData.ActiveChangedEventData.ElementAt(0).OldValue);
            Assert.AreEqual(null, gsm1EventData.ActiveChangedEventData.ElementAt(0).NewValue);
        }

        [TestMethod]
        public void DeselectSingle()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var gsm1EventData = new SelectionModelEventData(gsm1);

            gsm1.Select(new BaseGeometry[] { testdata[0], testdata[1], testdata[2], testdata[3] }, false);
            gsm1EventData.Reset();

            gsm1.Deselect(testdata[1]);
            Assert.AreEqual(3, gsm1.SelectedGeometry.Count);
            Assert.IsNotNull(gsm1.ActiveGeometry);
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(0).Added);
            Assert.AreEqual(1, gsm1EventData.SelectionChangedEventData.ElementAt(0).Removed.Count());
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(0).Reason);
            Assert.AreEqual(0, gsm1EventData.ActiveChangedEventData.Count);

            var act = gsm1.ActiveGeometry;
            gsm1.Deselect(act);
            Assert.AreEqual(2, gsm1.SelectedGeometry.Count);
            Assert.IsNotNull(gsm1.ActiveGeometry);
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(1).Added);
            Assert.AreEqual(1, gsm1EventData.SelectionChangedEventData.ElementAt(1).Removed.Count());
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(1).Reason);
            Assert.AreEqual(act, gsm1EventData.ActiveChangedEventData.ElementAt(0).OldValue);
            Assert.IsNotNull(gsm1EventData.ActiveChangedEventData.ElementAt(0).NewValue);
        }

        [TestMethod]
        public void DeselectModel()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var gsm1EventData = new SelectionModelEventData(gsm1);

            gsm1.Select(new BaseGeometry[] { testdata[0], testdata[1], testdata[2], testdata[3] }, false);
            gsm1EventData.Reset();

            gsm1.Deselect(data.model.Geometry);
            Assert.AreEqual(0, gsm1.SelectedGeometry.Count);
            Assert.IsNull(gsm1.ActiveGeometry);
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(0).Added);
            Assert.AreEqual(4, gsm1EventData.SelectionChangedEventData.ElementAt(0).Removed.Count());
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(0).Reason);
            Assert.AreEqual(1, gsm1EventData.ActiveChangedEventData.Count);
        }

        [TestMethod]
        public void ToggleSelection()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var gsm1EventData = new SelectionModelEventData(gsm1);

            gsm1.Select(new BaseGeometry[] { testdata[0], testdata[1], testdata[2], testdata[3] }, false);
            gsm1EventData.Reset();

            gsm1.ToggleSelection(testdata[1]);
            Assert.AreEqual(3, gsm1.SelectedGeometry.Count);
            Assert.IsNotNull(gsm1.ActiveGeometry);
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(0).Added);
            Assert.AreEqual(1, gsm1EventData.SelectionChangedEventData.ElementAt(0).Removed.Count());
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(0).Reason);
            Assert.AreEqual(0, gsm1EventData.ActiveChangedEventData.Count);

            var act = gsm1.ActiveGeometry;
            gsm1.ToggleSelection(act);
            Assert.AreEqual(2, gsm1.SelectedGeometry.Count);
            Assert.IsNotNull(gsm1.ActiveGeometry);
            Assert.AreEqual(null, gsm1EventData.SelectionChangedEventData.ElementAt(1).Added);
            Assert.AreEqual(1, gsm1EventData.SelectionChangedEventData.ElementAt(1).Removed.Count());
            Assert.AreEqual(GeometrySelectionModel.SelectionChangedReason.User, gsm1EventData.SelectionChangedEventData.ElementAt(1).Reason);
            Assert.AreEqual(1, gsm1EventData.ActiveChangedEventData.Count);

            gsm1EventData.Reset();
            gsm1.ToggleSelection(testdata[1]);
            Assert.AreEqual(3, gsm1.SelectedGeometry.Count);
            Assert.IsNotNull(gsm1.ActiveGeometry);
            Assert.AreEqual(1, gsm1EventData.SelectionChangedEventData.Count);
            Assert.AreEqual(1, gsm1EventData.ActiveChangedEventData.Count);
        }

        [TestMethod]
        public void MakeConsistent()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var gsm1EventData = new SelectionModelEventData(gsm1);

            gsm1.Select(new BaseGeometry[] { testdata[0], testdata[1], testdata[2], testdata[3] }, false);
            gsm1EventData.Reset();

            data.model.Geometry.StartBatchOperation();
            testdata[1].RemoveFromModel();
            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(3, gsm1.SelectedGeometry.Count);
        }

        [TestMethod]
        public void IsSelectedSingle()
        {
            var data = GeometryModelHelper.EmptyModel();
            var models = new ObservableCollection<GeometryModel>() { data.model };

            ShapeGenerator.GenerateCube(data.layer, new SimPoint3D(0, 0, 0), new SimPoint3D(1, 1, 1));
            var p1 = new ProxyGeometry(data.layer, "", data.model.Geometry.Vertices[0]);
            var pl1 = new Polyline(data.layer, "", new Edge[] { data.model.Geometry.Edges[0], data.model.Geometry.Edges[1] });

            var gsm1 = new GeometrySelectionModel(models);

            gsm1.Select(data.model.Geometry.Vertices[0], false);
            gsm1.Select(data.model.Geometry.EdgeLoops[0], false);
            gsm1.Select(data.model.Geometry.Edges[0], false);
            gsm1.Select(data.model.Geometry.Faces[0], false);
            gsm1.Select(data.model.Geometry.Polylines[0], false);
            gsm1.Select(data.model.Geometry.ProxyGeometries[0], false);
            gsm1.Select(data.model.Geometry.Volumes[0], false);

            Assert.IsTrue(gsm1.IsSelected(data.model.Geometry.Vertices[0]));
            Assert.IsTrue(gsm1.IsSelected(data.model.Geometry.Edges[0]));
            Assert.IsTrue(gsm1.IsSelected(data.model.Geometry.EdgeLoops[0]));
            Assert.IsTrue(gsm1.IsSelected(data.model.Geometry.Faces[0]));
            Assert.IsTrue(gsm1.IsSelected(data.model.Geometry.Volumes[0]));
            Assert.IsTrue(gsm1.IsSelected(data.model.Geometry.Polylines[0]));
            Assert.IsTrue(gsm1.IsSelected(data.model.Geometry.Volumes[0]));

            gsm1.Clear();
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry.Vertices[0]));
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry.Edges[0]));
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry.EdgeLoops[0]));
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry.Faces[0]));
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry.Volumes[0]));
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry.Polylines[0]));
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry.Volumes[0]));
        }

        [TestMethod]
        public void IsSelectedModel()
        {
            var data = GeometryModelHelper.EmptyModel();
            var models = new ObservableCollection<GeometryModel>() { data.model };

            ShapeGenerator.GenerateCube(data.layer, new SimPoint3D(0, 0, 0), new SimPoint3D(1, 1, 1));
            var p1 = new ProxyGeometry(data.layer, "", data.model.Geometry.Vertices[0]);
            var pl1 = new Polyline(data.layer, "", new Edge[] { data.model.Geometry.Edges[0], data.model.Geometry.Edges[1] });

            var gsm1 = new GeometrySelectionModel(models);

            gsm1.Select(data.model.Geometry.Vertices[0], false);
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry));

            gsm1.Select(data.model.Geometry, false);
            Assert.IsTrue(gsm1.IsSelected(data.model.Geometry));

            gsm1.Clear();
            Assert.IsFalse(gsm1.IsSelected(data.model.Geometry));
        }

        [TestMethod]
        public void ReplaceModel()
        {
            var data = GeometryModelHelper.EmptyModel();
            var testdata = TestData(data.layer);
            var data2 = GeometryModelHelper.EmptyModelData();
            var testdata2 = TestData(data2.layer);

            var models = new ObservableCollection<GeometryModel>() { data.model };

            GeometrySelectionModel gsm0 = new GeometrySelectionModel(models);
            gsm0.Select(data.model.Geometry, false);

            foreach (var v in testdata2)
                Assert.IsFalse(gsm0.IsSelected(v));

            data.model.Geometry = data2.modelData;

            foreach (var v in testdata2)
                Assert.IsTrue(gsm0.IsSelected(v));
        }

        [TestMethod]
        public void PermissionDenied()
        {
            var data = GeometryModelHelper.EmptyModel();
            data.model.Permissions = OperationPermission.None;
            var testdata = TestData(data.layer);
            var models = new ObservableCollection<GeometryModel>() { data.model };

            var gsm1 = new GeometrySelectionModel(models);
            var gsm1EventData = new SelectionModelEventData(gsm1);

            gsm1.Select(testdata[0], false);
            Assert.AreEqual(0, gsm1.SelectedGeometry.Count);
        }
    }
}
