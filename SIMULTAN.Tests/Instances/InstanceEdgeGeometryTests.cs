using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SIMULTAN.Tests.Instances
{
    [TestClass]
    public class InstanceEdgeGeometryTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./GeometryInstanceTestsProject.simultan");

        #region Add

        [TestMethod]
        public void AddEdgeInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Edge 1");
            var edgeA = gm.Geometry.Edges.First(f => f.Name == "Edge A");
            var edgeB = gm.Geometry.Edges.First(f => f.Name == "Edge B");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, edgeA);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(edgeA.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);

            //Second association
            projectData.ComponentGeometryExchange.Associate(comp, edgeB);

            Assert.AreEqual(2, comp.Instances.Count);

            var inst2 = comp.Instances.First(x => x != inst1);
            Assert.AreEqual(comp, inst2.Component);
            Assert.AreEqual(true, inst2.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst2.State.ConnectionState);
            Assert.AreEqual(1, inst2.Placements.Count);

            geomPlacement = inst2.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(edgeB.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        /// <summary>
        /// Add an instance for multiple faces at once.
        /// </summary>
        [TestMethod]
        public void AddEdgesInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Edge 1");
            var edgeA = gm.Geometry.Edges.First(f => f.Name == "Edge A");
            var edgeB = gm.Geometry.Edges.First(f => f.Name == "Edge B");

            Assert.AreEqual(0, comp.Instances.Count);

            var edgeList = new List<Edge>();
            edgeList.Add(edgeA);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, edgeList);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(edgeA.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);

            // add second one
            edgeList.Clear();
            edgeList.Add(edgeB);
            projectData.ComponentGeometryExchange.Associate(comp, edgeList);

            var inst2 = comp.Instances.First(x => x != inst1);
            Assert.AreEqual(comp, inst2.Component);
            Assert.AreEqual(true, inst2.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst2.State.ConnectionState);
            Assert.AreEqual(1, inst2.Placements.Count);

            geomPlacement = inst2.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(edgeB.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        [TestMethod]
        public void EdgeAssociateAgain()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Edge 2");
            var edgeB = gm.Geometry.Edges.First(f => f.Name == "Edge B");
            var inst = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == edgeB.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(inst);
            Assert.AreEqual(1, comp.Instances.Count);

            projectData.ComponentGeometryExchange.Associate(comp, edgeB);

            Assert.AreEqual(1, comp.Instances.Count);
            Assert.AreEqual(inst, comp.Instances[0]);
        }

        #endregion

        #region Changes

        [TestMethod]
        public void MissingEdgeWithClone()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Edge 2");

            var newGM = gm.Geometry.Clone();
            var edgeB = newGM.Edges.First(f => f.Name == "Edge B");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == edgeB.Id && pg.FileId == resource.Key));
            var placement = instance.Placements.OfType<SimInstancePlacementGeometry>().First();

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), comp.InstanceState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);

            edgeB.RemoveFromModel();
            gm.Geometry = newGM;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound), comp.InstanceState);
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, placement.State);

            newGM = newGM.Clone();

            gm.Geometry = newGM;
            // should still be Ok
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), comp.InstanceState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
        }

        [TestMethod]
        public void ReplaceGeometryModel()
        {
            LoadProject(testProject, "arch", "arch");
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Edge 2");
            var edgeB = gm.Geometry.Edges.First(f => f.Name == "Edge B");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == edgeB.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instance);
            Assert.AreEqual(1, comp.Instances.Count);

            var gmCopy = gm.Geometry.Clone();
            var vertex = gmCopy.Vertices.First(f => f.Name == "Vertex C");

            vertex.Position = new SimPoint3D(10.0, 0.0, 10.0);

            gm.Geometry = gmCopy;

            var edgeNew = gm.Geometry.Edges.First(f => f.Name == "Edge B");
            var instanceNew = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == edgeB.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instanceNew);
            Assert.AreEqual(instance, instanceNew);
            Assert.IsNotNull(edgeNew);
            Assert.AreEqual(1, comp.Instances.Count);
        }

        [TestMethod]
        public void ReplaceGeometryModelEdgeDelete()
        {
            LoadProject(testProject, "arch", "arch");
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Edge 2");
            var edgeB = gm.Geometry.Edges.First(f => f.Name == "Edge B");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == edgeB.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.IsNotNull(instance);
            Assert.IsNotNull(geomPlacement);
            Assert.AreEqual(1, comp.Instances.Count);

            var gmCopy = gm.Geometry.Clone();
            var vertex = gmCopy.Vertices.First(f => f.Name == "Vertex C");

            gm.Geometry.Edges.Remove(edgeB);

            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, geomPlacement.State);

            gm.Geometry = gmCopy;

            var edgeNew = gm.Geometry.Edges.First(f => f.Name == "Edge B");
            var instanceNew = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == edgeB.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instanceNew);
            Assert.AreEqual(instance, instanceNew);
            Assert.IsNotNull(edgeNew);
            Assert.AreEqual(1, comp.Instances.Count);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        #endregion

        #region Remove

        [TestMethod]
        public void RemoveEdgeInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Edge 2");
            var edgeB = gm.Geometry.Edges.First(f => f.Name == "Edge B");

            Assert.AreEqual(1, comp.Instances.Count);
            Assert.IsTrue(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == edgeB.Id)));

            projectData.ComponentGeometryExchange.Disassociate(comp, edgeB);

            Assert.AreEqual(0, comp.Instances.Count);
            Assert.IsFalse(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == edgeB.Id)));
        }

        [TestMethod]
        public void RemoveEdgeInstanceState()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Edge 2");
            var edgeB = gm.Geometry.Edges.First(f => f.Name == "Edge B");

            edgeB.RemoveFromModel();

            Assert.AreEqual(1, comp.Instances.Count);
            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, comp.InstanceState.ConnectionState);
            Assert.AreEqual(true, comp.InstanceState.IsRealized);

            var inst = comp.Instances[0];
            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, inst.State.ConnectionState);
            Assert.AreEqual(true, inst.State.IsRealized);
            Assert.AreEqual(1, inst.Placements.Count);

            var pl = (SimInstancePlacementGeometry)inst.Placements[0];
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, pl.State);
            Assert.AreEqual(true, pl.IsValid);
        }

        #endregion
    }
}
