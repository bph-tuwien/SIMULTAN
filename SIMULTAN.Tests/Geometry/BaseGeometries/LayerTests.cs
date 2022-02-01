using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class LayerTests
    {
        [TestMethod]
        public void Ctor()
        {
            var gm = new GeometryModelData(new DummyComponentGeometryExchange(new FileInfo("./dummy.geosim")));

            Assert.ThrowsException<ArgumentNullException>(() => { var l0 = new Layer(null, ""); });
            Assert.ThrowsException<ArgumentNullException>(() => { var l0 = new Layer(gm, null); });

            var l1 = new Layer(gm, "asdf");
            gm.Layers.Add(l1);

            Assert.AreEqual(gm, l1.Model);
            Assert.AreEqual("asdf", l1.Name);
            Assert.AreEqual(null, l1.Parent);
            Assert.AreEqual(1, gm.Layers.Count);
            Assert.AreEqual(l1, gm.Layers[0]);
        }

        [TestMethod]
        public void AddToSublayer()
        {
            var gm = new GeometryModelData(new DummyComponentGeometryExchange(new FileInfo("./dummy.geosim")));
            var l1 = new Layer(gm, "asdf");
            gm.Layers.Add(l1);

            var l2 = new Layer(gm, "asdf2");
            l1.Layers.Add(l2);

            Assert.AreEqual(l1, l2.Parent);
            Assert.AreEqual(l2, l1.Layers[0]);
        }

        [TestMethod]
        public void RemoveSublayer()
        {
            var gm = new GeometryModelData(new DummyComponentGeometryExchange(new FileInfo("./dummy.geosim")));
            var l1 = new Layer(gm, "asdf");
            gm.Layers.Add(l1);

            var l2 = new Layer(gm, "asdf2");
            l1.Layers.Add(l2);

            l1.Layers.RemoveAt(0);
            Assert.AreEqual(null, l2.Parent);
            Assert.AreEqual(0, l1.Layers.Count);
        }

        [TestMethod]
        public void GeometryRemoved()
        {
            var gm = new GeometryModelData(new DummyComponentGeometryExchange(new FileInfo("./dummy.geosim")));
            var l1 = new Layer(gm, "asdf");
            gm.Layers.Add(l1);

            var v = new Vertex(l1, "", new Point3D(0, 0, 0));
            Assert.AreEqual(1, l1.Elements.Count);

            v.RemoveFromModel();
            Assert.AreEqual(0, l1.Elements.Count);
        }

        [TestMethod]
        public void IsVisible()
        {
            var gm = new GeometryModelData(new DummyComponentGeometryExchange(new FileInfo("./dummy.geosim")));
            var l1 = new Layer(gm, "asdf");
            var l1event = new PropertyChangedEventData(l1);
            gm.Layers.Add(l1);

            var l2 = new Layer(gm, "asdf2");
            var l2event = new PropertyChangedEventData(l2);
            l1.Layers.Add(l2);

            Assert.IsTrue(l1.IsActuallyVisible);
            Assert.IsTrue(l2.IsActuallyVisible);

            l1event.Reset();
            l2event.Reset();
            l2.IsVisible = false;
            Assert.IsFalse(l2.IsActuallyVisible);
            Assert.AreEqual(0, l1event.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Layer.IsVisible), l2event.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l2event.PropertyChangedData[1]);

            l1event.Reset();
            l2event.Reset();
            l2.IsVisible = false;
            Assert.IsFalse(l2.IsActuallyVisible);
            Assert.AreEqual(0, l1event.PropertyChangedData.Count);
            Assert.AreEqual(0, l2event.PropertyChangedData.Count);

            l2.IsVisible = true;
            Assert.IsTrue(l2.IsActuallyVisible);
            Assert.AreEqual(0, l1event.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Layer.IsVisible), l2event.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l2event.PropertyChangedData[1]);

            //Parent layer isvisible
            l1event.Reset();
            l2event.Reset();
            l1.IsVisible = false;
            Assert.IsFalse(l1.IsActuallyVisible);
            Assert.IsFalse(l2.IsActuallyVisible);
            Assert.AreEqual(nameof(Layer.IsVisible), l1event.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l1event.PropertyChangedData[1]);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l2event.PropertyChangedData[0]);

            l1.IsVisible = true;
            Assert.IsTrue(l1.IsActuallyVisible);
            Assert.IsTrue(l2.IsActuallyVisible);
            Assert.AreEqual(nameof(Layer.IsVisible), l1event.PropertyChangedData[2]);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l1event.PropertyChangedData[3]);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l2event.PropertyChangedData[1]);

            //GM IsVisible
            l1event.Reset();
            l2event.Reset();
            gm.IsVisible = false;
            Assert.IsFalse(l1.IsActuallyVisible);
            Assert.IsFalse(l2.IsActuallyVisible);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l1event.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l2event.PropertyChangedData[0]);

            gm.IsVisible = true;
            Assert.IsTrue(l1.IsActuallyVisible);
            Assert.IsTrue(l2.IsActuallyVisible);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l1event.PropertyChangedData[1]);
            Assert.AreEqual(nameof(Layer.IsActuallyVisible), l2event.PropertyChangedData[1]);
        }
    }
}
