using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class DerivedColorTests
    {
        [TestMethod]
        public void Ctor()
        {
            var data = GeometryModelHelper.EmptyModel();
            Vertex parent = new Vertex(data.layer, "", new Point3D(0, 0, 0)) { Color = new DerivedColor(Colors.Orange) };

            Assert.ThrowsException<ArgumentNullException>(() => { DerivedColor c0 = new DerivedColor(Colors.Pink, null, "asdf"); }); //parent null
            Assert.ThrowsException<ArgumentNullException>(() => { DerivedColor c0 = new DerivedColor(Colors.Pink, parent, null); }); //name null
            Assert.ThrowsException<ArgumentException>(() => { DerivedColor c0 = new DerivedColor(Colors.Pink, parent, "asdf"); }); //prop does not exist
            Assert.ThrowsException<ArgumentException>(() => { DerivedColor c0 = new DerivedColor(Colors.Pink, parent, "Position"); }); //prop wrong type

            Assert.ThrowsException<ArgumentNullException>(() => { DerivedColor c0 = new DerivedColor(null); }); //source null

            DerivedColor c1 = new DerivedColor(Colors.Pink);
            Assert.AreEqual(Colors.Pink, c1.LocalColor);
            Assert.AreEqual(Colors.Pink, c1.ParentColor);
            Assert.AreEqual(Colors.Pink, c1.Color);
            Assert.AreEqual(false, c1.IsFromParent);

            DerivedColor c2 = new DerivedColor(Colors.Pink, parent, "Color");
            Assert.AreEqual(Colors.Pink, c2.LocalColor);
            Assert.AreEqual(parent, c2.Parent);
            Assert.AreEqual("Color", c2.PropertyName);
            Assert.AreEqual(Colors.Orange, c2.ParentColor);
            Assert.AreEqual(Colors.Orange, c2.Color);
            Assert.AreEqual(true, c2.IsFromParent);

            DerivedColor c3 = new DerivedColor(c2);
            Assert.AreEqual(Colors.Pink, c3.LocalColor);
            Assert.AreEqual(parent, c3.Parent);
            Assert.AreEqual("Color", c3.PropertyName);
            Assert.AreEqual(Colors.Orange, c3.ParentColor);
            Assert.AreEqual(Colors.Orange, c3.Color);
            Assert.AreEqual(true, c3.IsFromParent);
        }

        [TestMethod]
        public void LocalColor()
        {
            var data = GeometryModelHelper.EmptyModel();
            Vertex parent = new Vertex(data.layer, "", new Point3D(0, 0, 0)) { Color = new DerivedColor(Colors.Orange) };

            DerivedColor c1 = new DerivedColor(Colors.Pink, parent, "Color");
            PropertyChangedEventData c1event = new PropertyChangedEventData(c1);

            Assert.AreEqual(Colors.Pink, c1.LocalColor);
            Assert.AreEqual(parent, c1.Parent);
            Assert.AreEqual("Color", c1.PropertyName);
            Assert.AreEqual(Colors.Orange, c1.ParentColor);
            Assert.AreEqual(Colors.Orange, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(0, c1event.PropertyChangedData.Count);

            c1.Color = Colors.Green;
            Assert.AreEqual(Colors.Green, c1.LocalColor);
            Assert.AreEqual(parent, c1.Parent);
            Assert.AreEqual("Color", c1.PropertyName);
            Assert.AreEqual(Colors.Orange, c1.ParentColor);
            Assert.AreEqual(Colors.Green, c1.Color);
            Assert.AreEqual(false, c1.IsFromParent);
            Assert.AreEqual(1, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[0]);
        }

        [TestMethod]
        public void IsFromParent()
        {
            var data = GeometryModelHelper.EmptyModel();
            Vertex parent = new Vertex(data.layer, "", new Point3D(0, 0, 0)) { Color = new DerivedColor(Colors.Orange) };

            DerivedColor c1 = new DerivedColor(Colors.Pink, parent, "Color");
            PropertyChangedEventData c1event = new PropertyChangedEventData(c1);

            Assert.AreEqual(Colors.Pink, c1.LocalColor);
            Assert.AreEqual(parent, c1.Parent);
            Assert.AreEqual("Color", c1.PropertyName);
            Assert.AreEqual(Colors.Orange, c1.ParentColor);
            Assert.AreEqual(Colors.Orange, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(0, c1event.PropertyChangedData.Count);

            c1.IsFromParent = false;
            Assert.AreEqual(Colors.Pink, c1.LocalColor);
            Assert.AreEqual(parent, c1.Parent);
            Assert.AreEqual("Color", c1.PropertyName);
            Assert.AreEqual(Colors.Orange, c1.ParentColor);
            Assert.AreEqual(Colors.Pink, c1.Color);
            Assert.AreEqual(false, c1.IsFromParent);
            Assert.AreEqual(1, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[0]);

            c1.IsFromParent = true;
            Assert.AreEqual(Colors.Pink, c1.LocalColor);
            Assert.AreEqual(parent, c1.Parent);
            Assert.AreEqual("Color", c1.PropertyName);
            Assert.AreEqual(Colors.Orange, c1.ParentColor);
            Assert.AreEqual(Colors.Orange, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(2, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[1]);
        }

        [TestMethod]
        public void ParentColorChanged()
        {
            var data = GeometryModelHelper.EmptyModel();
            Vertex parent = new Vertex(data.layer, "", new Point3D(0, 0, 0)) { Color = new DerivedColor(Colors.Orange) };

            DerivedColor c1 = new DerivedColor(Colors.Pink, parent, "Color");
            PropertyChangedEventData c1event = new PropertyChangedEventData(c1);

            parent.Color = new DerivedColor(Colors.Gold);
            Assert.AreEqual(Colors.Pink, c1.LocalColor);
            Assert.AreEqual(parent, c1.Parent);
            Assert.AreEqual("Color", c1.PropertyName);
            Assert.AreEqual(Colors.Gold, c1.ParentColor);
            Assert.AreEqual(Colors.Gold, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(1, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[0]);
        }

        [TestMethod]
        public void ParentColorColorChanged()
        {
            var data = GeometryModelHelper.EmptyModel();
            Vertex parent = new Vertex(data.layer, "", new Point3D(0, 0, 0)) { Color = new DerivedColor(Colors.Orange) };

            DerivedColor c1 = new DerivedColor(Colors.Pink, parent, "Color");
            PropertyChangedEventData c1event = new PropertyChangedEventData(c1);

            parent.Color.Color = Colors.Gold;
            Assert.AreEqual(Colors.Pink, c1.LocalColor);
            Assert.AreEqual(parent, c1.Parent);
            Assert.AreEqual("Color", c1.PropertyName);
            Assert.AreEqual(Colors.Gold, c1.ParentColor);
            Assert.AreEqual(Colors.Gold, c1.Color);
            Assert.AreEqual(true, c1.IsFromParent);
            Assert.AreEqual(1, c1event.PropertyChangedData.Count);
            Assert.AreEqual("Color", c1event.PropertyChangedData[0]);
        }
    }
}
