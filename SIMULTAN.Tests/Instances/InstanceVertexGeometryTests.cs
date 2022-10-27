using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Instances
{
    [TestClass]
    public class InstanceVertexGeometryTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\GeometryInstanceTestsProject.simultan");

        #region Add

        [TestMethod]
        public void AddVertexInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 1");
            var vertexA = gm.Geometry.Vertices.First(f => f.Name == "Vertex A");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, vertexA);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(SimInstanceType.AttributesPoint, inst1.InstanceType);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(vertexA.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);

            //Second association
            projectData.ComponentGeometryExchange.Associate(comp, vertexB);

            Assert.AreEqual(2, comp.Instances.Count);

            var inst2 = comp.Instances.First(x => x != inst1);
            Assert.AreEqual(comp, inst2.Component);
            Assert.AreEqual(SimInstanceType.AttributesPoint, inst2.InstanceType);
            Assert.AreEqual(true, inst2.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst2.State.ConnectionState);
            Assert.AreEqual(1, inst2.Placements.Count);

            geomPlacement = inst2.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(vertexB.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        /// <summary>
        /// Add an instance for multiple faces at once.
        /// </summary>
        [TestMethod]
        public void AddVertexsInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 1");
            var vertexA = gm.Geometry.Vertices.First(f => f.Name == "Vertex A");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");

            Assert.AreEqual(0, comp.Instances.Count);

            var vertexList = new List<Vertex>();
            vertexList.Add(vertexA);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, vertexList);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(SimInstanceType.AttributesPoint, inst1.InstanceType);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(vertexA.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);

            // add second one
            vertexList.Clear();
            vertexList.Add(vertexB);
            projectData.ComponentGeometryExchange.Associate(comp, vertexList);

            var inst2 = comp.Instances.First(x => x != inst1);
            Assert.AreEqual(comp, inst2.Component);
            Assert.AreEqual(SimInstanceType.AttributesPoint, inst2.InstanceType);
            Assert.AreEqual(true, inst2.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst2.State.ConnectionState);
            Assert.AreEqual(1, inst2.Placements.Count);

            geomPlacement = inst2.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(vertexB.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        [TestMethod]
        public void AddVertexInstanceParameters()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 1");
            var vertexA = gm.Geometry.Vertices.First(f => f.Name == "Vertex A");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");

            Assert.AreEqual(0, comp.Parameters.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, vertexA);

            Assert.AreEqual(1, comp.Parameters.Count);

            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_COUNT));

            Assert.AreNotEqual(null, nrtotalParam);

            Assert.AreEqual(1, nrtotalParam.ValueCurrent);

            //Add second association
            projectData.ComponentGeometryExchange.Associate(comp, vertexB);
            Assert.AreEqual(2, nrtotalParam.ValueCurrent);
        }

        [TestMethod]
        public void AddVertexInstancePath()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 1");
            var vertexA = gm.Geometry.Vertices.First(f => f.Name == "Vertex A");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, vertexA);

            //No path for this type of instance
            var instance = comp.Instances[0];
            Assert.AreEqual(1, instance.InstancePath.Count);
            AssertUtil.AssertDoubleEqual(0.0, instance.InstancePathLength);
            Assert.AreEqual(vertexA.Position, instance.InstancePath[0]);
        }

        [TestMethod]
        public void VertexAssociateAgain()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");
            var inst = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == vertexB.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(inst);
            Assert.AreEqual(1, comp.Instances.Count);

            projectData.ComponentGeometryExchange.Associate(comp, vertexB);

            Assert.AreEqual(1, comp.Instances.Count);
            Assert.AreEqual(inst, comp.Instances[0]);
        }

        #endregion

        #region Changes

        [TestMethod]
        public void MissingVertexWithClone()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");

            var newGM = gm.Geometry.Clone();
            var vertexB = newGM.Vertices.First(f => f.Name == "Vertex B");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == vertexB.Id && pg.FileId == resource.Key));
            var placement = instance.Placements.OfType<SimInstancePlacementGeometry>().First();

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), comp.InstanceState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);

            vertexB.RemoveFromModel();
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

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == vertexB.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instance);
            Assert.AreEqual(1, comp.Instances.Count);

            var gmCopy = gm.Geometry.Clone();

            gm.Geometry = gmCopy;

            var vertexNew = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");
            var instanceNew = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == vertexB.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instanceNew);
            Assert.AreEqual(instance, instanceNew);
            Assert.IsNotNull(vertexNew);
            Assert.AreEqual(1, comp.Instances.Count);
        }

        [TestMethod]
        public void ReplaceGeometryModelVertexDelete()
        {
            LoadProject(testProject, "arch", "arch");
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == vertexB.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.IsNotNull(instance);
            Assert.IsNotNull(geomPlacement);
            Assert.AreEqual(1, comp.Instances.Count);

            var gmCopy = gm.Geometry.Clone();

            gm.Geometry.Vertices.Remove(vertexB);

            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, geomPlacement.State);

            gm.Geometry = gmCopy;

            var verterxNew = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");
            var instanceNew = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == vertexB.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instanceNew);
            Assert.AreEqual(instance, instanceNew);
            Assert.IsNotNull(verterxNew);
            Assert.AreEqual(1, comp.Instances.Count);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        [TestMethod]
        public void InstancePathGeometryChanged()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");
            var inst = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == vertexB.Id && pg.FileId == resource.Key));

            var instPath = inst.InstancePath[0];

            vertexB.Position = new Point3D(1.0, 1.0, 1.0);

            Assert.AreNotEqual(instPath, inst.InstancePath[0]);
            Assert.AreEqual(new Point3D(1.0, 1.0, 1.0), inst.InstancePath[0]);
        }

        #endregion

        #region Remove

        [TestMethod]
        public void RemoveVertexInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");

            Assert.AreEqual(1, comp.Instances.Count);
            Assert.IsTrue(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == vertexB.Id)));

            projectData.ComponentGeometryExchange.Disassociate(comp, vertexB);

            Assert.AreEqual(0, comp.Instances.Count);
            Assert.IsFalse(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == vertexB.Id)));
        }

        [TestMethod]
        public void RemoveVertexInstanceParameters()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");

            Assert.AreEqual(1, comp.Instances.Count);
            Assert.IsTrue(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == vertexB.Id)));

            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_COUNT));

            projectData.ComponentGeometryExchange.Disassociate(comp, vertexB);

            Assert.AreEqual(0, comp.Instances.Count);
            Assert.IsFalse(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == vertexB.Id)));
            AssertUtil.AssertDoubleEqual(0.0, nrtotalParam.ValueCurrent);
        }

        [TestMethod]
        public void RemoveVertexState()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexA = gm.Geometry.Vertices.First(f => f.Name == "Vertex A");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");
            projectData.ComponentGeometryExchange.Associate(comp, vertexA);

            vertexB.RemoveFromModel();

            Assert.AreEqual(2, comp.Instances.Count);
            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, comp.InstanceState.ConnectionState);
            Assert.AreEqual(true, comp.InstanceState.IsRealized);

            var inst = comp.Instances.First(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp &&
                gp.FileId == resource.Key && gp.GeometryId == vertexB.Id));
            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, inst.State.ConnectionState);
            Assert.AreEqual(true, inst.State.IsRealized);
            Assert.AreEqual(1, inst.Placements.Count);

            var pl = (SimInstancePlacementGeometry)inst.Placements[0];
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, pl.State);
            Assert.AreEqual(true, pl.IsValid);
        }

        [TestMethod]
        public void RemoveVertexParameters()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexA = gm.Geometry.Vertices.First(f => f.Name == "Vertex A");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");
            projectData.ComponentGeometryExchange.Associate(comp, vertexA);

            vertexB.RemoveFromModel();

            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_COUNT));
            AssertUtil.AssertDoubleEqual(2.0, nrtotalParam.ValueCurrent);
        }

        [TestMethod]
        public void RemoveVertexInstancePath()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Vertex 2");
            var vertexB = gm.Geometry.Vertices.First(f => f.Name == "Vertex B");

            vertexB.RemoveFromModel();

            Assert.AreEqual(0, comp.Instances[0].InstancePath.Count);
        }

        #endregion
    }
}
