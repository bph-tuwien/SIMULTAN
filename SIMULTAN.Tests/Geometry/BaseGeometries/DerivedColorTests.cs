using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class DerivedColorTests
    {
        [TestMethod]
        public void Ctor()
        {
            var data = GeometryModelHelper.EmptyModel();
            data.layer.Color = new DerivedColor(SimColors.Orange);
            Vertex v = new Vertex(data.layer, "v", new SimPoint3D(0, 0, 0));

            Assert.ThrowsException<ArgumentNullException>(() => { DerivedColor c0 = new DerivedColor(null); }); //source null

            DerivedColor c1 = new DerivedColor(SimColors.Pink);
            Assert.AreEqual(SimColors.Pink, c1.LocalColor);
            Assert.AreEqual(SimColors.Pink, c1.ParentColor);
            Assert.AreEqual(SimColors.Pink, c1.Color);
            Assert.AreEqual(false, c1.IsFromParent);

            DerivedColor c2 = new DerivedColor(SimColors.Pink, true);
            v.Color = c2;
            Assert.AreEqual(SimColors.Pink, c2.LocalColor);
            Assert.AreEqual(data.layer, c2.Parent);
            Assert.AreEqual(SimColors.Orange, c2.ParentColor);
            Assert.AreEqual(SimColors.Orange, c2.Color);
            Assert.AreEqual(true, c2.IsFromParent);

            DerivedColor c3 = new DerivedColor(c2);
            v.Color = c3;
            Assert.AreEqual(SimColors.Pink, c3.LocalColor);
            Assert.AreEqual(data.layer, c3.Parent);
            Assert.AreEqual(SimColors.Orange, c3.ParentColor);
            Assert.AreEqual(SimColors.Orange, c3.Color);
            Assert.AreEqual(true, c3.IsFromParent);
        }

        [TestMethod]
        public void LocalColor()
        {
            var data = GeometryModelHelper.EmptyModel();
            data.layer.Color = new DerivedColor(SimColors.Orange);

            DerivedColor c1 = new DerivedColor(SimColors.Pink, true);
            Vertex v = new Vertex(data.layer, "v", new SimPoint3D(0, 0, 0)) { Color = c1 };

            PropertyChangedEventData c1event = new PropertyChangedEventData(c1);

            Assert.AreEqual(SimColors.Pink, c1.LocalColor);
            Assert.AreEqual(data.layer, c1.Parent);
            Assert.AreEqual(SimColors.Orange, c1.ParentColor);
            Assert.AreEqual(SimColors.Orange, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(0, c1event.PropertyChangedData.Count);

            c1.Color = SimColors.Green;
            Assert.AreEqual(SimColors.Green, c1.LocalColor);
            Assert.AreEqual(data.layer, c1.Parent);
            Assert.AreEqual(SimColors.Orange, c1.ParentColor);
            Assert.AreEqual(SimColors.Green, c1.Color);
            Assert.AreEqual(false, c1.IsFromParent);
            Assert.AreEqual(1, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[0]);
        }

        [TestMethod]
        public void IsFromParent()
        {
            var data = GeometryModelHelper.EmptyModel();
            data.layer.Color = new DerivedColor(SimColors.Orange);

            DerivedColor c1 = new DerivedColor(SimColors.Pink, true);
            Vertex v = new Vertex(data.layer, "v", new SimPoint3D(0, 0, 0)) { Color = c1 };
            PropertyChangedEventData c1event = new PropertyChangedEventData(c1);

            Assert.AreEqual(SimColors.Pink, c1.LocalColor);
            Assert.AreEqual(data.layer, c1.Parent);
            Assert.AreEqual(SimColors.Orange, c1.ParentColor);
            Assert.AreEqual(SimColors.Orange, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(0, c1event.PropertyChangedData.Count);

            c1.IsFromParent = false;
            Assert.AreEqual(SimColors.Pink, c1.LocalColor);
            Assert.AreEqual(data.layer, c1.Parent);
            Assert.AreEqual(SimColors.Orange, c1.ParentColor);
            Assert.AreEqual(SimColors.Pink, c1.Color);
            Assert.AreEqual(false, c1.IsFromParent);
            Assert.AreEqual(1, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[0]);

            c1.IsFromParent = true;
            Assert.AreEqual(SimColors.Pink, c1.LocalColor);
            Assert.AreEqual(data.layer, c1.Parent);
            Assert.AreEqual(SimColors.Orange, c1.ParentColor);
            Assert.AreEqual(SimColors.Orange, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(2, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[1]);
        }

        [TestMethod]
        public void ParentColorChanged()
        {
            var data = GeometryModelHelper.EmptyModel();
            data.layer.Color = new DerivedColor(SimColors.Orange);

            DerivedColor c1 = new DerivedColor(SimColors.Pink, true);
            Vertex v = new Vertex(data.layer, "v", new SimPoint3D(0, 0, 0)) { Color = c1 };
            PropertyChangedEventData c1event = new PropertyChangedEventData(c1);

            data.layer.Color = new DerivedColor(SimColors.Gold);
            Assert.AreEqual(SimColors.Pink, c1.LocalColor);
            Assert.AreEqual(data.layer, c1.Parent);
            Assert.AreEqual(SimColors.Gold, c1.ParentColor);
            Assert.AreEqual(SimColors.Gold, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(1, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[0]);
        }

        [TestMethod]
        public void ParentColorColorChanged()
        {
            var data = GeometryModelHelper.EmptyModel();
            data.layer.Color = new DerivedColor(SimColors.Orange);

            DerivedColor c1 = new DerivedColor(SimColors.Pink, true);
            Vertex v = new Vertex(data.layer, "v", new SimPoint3D(0, 0, 0)) { Color = c1 };
            PropertyChangedEventData c1event = new PropertyChangedEventData(c1);

            data.layer.Color.Color = SimColors.Gold;
            Assert.AreEqual(SimColors.Pink, c1.LocalColor);
            Assert.AreEqual(data.layer, c1.Parent);
            Assert.AreEqual(SimColors.Gold, c1.ParentColor);
            Assert.AreEqual(SimColors.Gold, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(1, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[0]);
        }
    }
}
