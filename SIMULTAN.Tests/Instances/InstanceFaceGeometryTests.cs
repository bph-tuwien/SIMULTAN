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
    public class InstanceFaceGeometryTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\GeometryInstanceTestsProject.simultan");

        [TestMethod]
        public void AddFaceInstance()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall");
            var leftFace = gm.Geometry.Faces.First(f => f.Name == "LeftWall");
            var floorFace = gm.Geometry.Faces.First(f => f.Name == "Floor");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.GeometryCommunicator.Associate(comp, leftFace);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(SimInstanceType.Attributes2D, inst1.InstanceType);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(leftFace.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);

            //Second association
            projectData.GeometryCommunicator.Associate(comp, floorFace);

            Assert.AreEqual(2, comp.Instances.Count);

            var inst2 = comp.Instances.First(x => x != inst1);
            Assert.AreEqual(comp, inst2.Component);
            Assert.AreEqual(SimInstanceType.Attributes2D, inst2.InstanceType);
            Assert.AreEqual(true, inst2.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst2.State.ConnectionState);
            Assert.AreEqual(1, inst2.Placements.Count);

            geomPlacement = inst2.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(floorFace.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        [TestMethod]
        public void AddFaceInstanceParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall");
            var leftFace = gm.Geometry.Faces.First(f => f.Name == "LeftWall");
            var floorFace = gm.Geometry.Faces.First(f => f.Name == "Floor");

            Assert.AreEqual(1, comp.Parameters.Count);

            //Add new association
            projectData.GeometryCommunicator.Associate(comp, leftFace);

            Assert.AreEqual(5, comp.Parameters.Count);

            var doutParam = comp.Parameters.FirstOrDefault(x => x.Name == "Δdout");
            var dinParam = comp.Parameters.FirstOrDefault(x => x.Name == "Δdin");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            Assert.AreNotEqual(null, doutParam);
            Assert.AreNotEqual(null, dinParam);
            Assert.AreNotEqual(null, aParam);
            Assert.AreNotEqual(null, nrtotalParam);

            Assert.AreEqual(1, nrtotalParam.ValueCurrent);
            AssertUtil.AssertDoubleEqual(25.0, aParam.ValueCurrent);

            //Add second association
            projectData.GeometryCommunicator.Associate(comp, floorFace);
            Assert.AreEqual(2, nrtotalParam.ValueCurrent);
            AssertUtil.AssertDoubleEqual(75.0, aParam.ValueCurrent);
        }

        [TestMethod]
        public void AddFaceInstancePath()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall");
            var leftFace = gm.Geometry.Faces.First(f => f.Name == "LeftWall");

            //Add new association
            projectData.GeometryCommunicator.Associate(comp, leftFace);

            //No path for this type of instance
            var instance = comp.Instances[0];
            Assert.AreEqual(0, instance.InstancePath.Count);
            AssertUtil.AssertDoubleEqual(0.0, instance.InstancePathLength);
        }

        [TestMethod]
        public void FaceChangedParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var vertex = gm.Geometry.Vertices.First(f => f.Name == "Vertex_TopRight");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");

            AssertUtil.AssertDoubleEqual(188.0, aParam.ValueCurrent);

            vertex.Position = new Point3D(20.0, 10.0, 0.0);

            AssertUtil.AssertDoubleEqual(200.5, aParam.ValueCurrent);
        }

        [TestMethod]
        public void FaceChangedBatchParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var vertex1 = gm.Geometry.Vertices.First(f => f.Name == "Vertex_TopRight");
            var vertex2 = gm.Geometry.Vertices.First(f => f.Name == "Vertex_TopLeft");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");

            AssertUtil.AssertDoubleEqual(188.0, aParam.ValueCurrent);

            gm.Geometry.StartBatchOperation();

            vertex1.Position = new Point3D(20.0, 10.0, 0.0);
            vertex2.Position = new Point3D(15.0, 10.0, 0.0);

            gm.Geometry.EndBatchOperation();

            AssertUtil.AssertDoubleEqual(213.0, aParam.ValueCurrent);
        }

        [TestMethod]
        public void FaceTopologyChangedParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var vertex1 = gm.Geometry.Vertices.First(f => f.Name == "Vertex_TopRight");
            var vertex2 = gm.Geometry.Vertices.First(f => f.Name == "Vertex_TopLeft");
            var edgeOld = gm.Geometry.Edges.First(e => e.Name == "Edge_Top");
            var face = gm.Geometry.Faces.First(f => f.Name == "Face_StandaloneRect");

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var inst = comp.Instances.First(
                x => x.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));

            AssertUtil.AssertDoubleEqual(188.0, aParam.ValueCurrent);

            gm.Geometry.StartBatchOperation();

            //Remove old edge
            var pedge = face.Boundary.Edges.First(pe => pe.Edge == edgeOld);
            face.Boundary.Edges.Remove(pedge);
            edgeOld.RemoveFromModel();

            //Add two new edges
            var newVertex = new Vertex(face.Layer, "", new Point3D(17.5, 7.5, 0.0));
            var newEdge1 = new Edge(face.Layer, "", new Vertex[] { vertex1, newVertex });
            var newEdge2 = new Edge(face.Layer, "", new Vertex[] { vertex2, newVertex });
            face.Boundary.Edges.Add(new PEdge(newEdge1, GeometricOrientation.Undefined, face.Boundary));
            face.Boundary.Edges.Add(new PEdge(newEdge2, GeometricOrientation.Undefined, face.Boundary));

            gm.Geometry.EndBatchOperation();

            AssertUtil.AssertDoubleEqual(31.25, inst.InstanceParameterValuesPersistent[aParam]);
            AssertUtil.AssertDoubleEqual(194.25, aParam.ValueCurrent);
        }

        [TestMethod]
        public void RemoveFaceInstance()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var rightFace = gm.Geometry.Faces.First(f => f.Name == "RightWall");

            Assert.AreEqual(4, comp.Instances.Count);
            Assert.IsTrue(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));

            projectData.GeometryCommunicator.DisAssociate(comp, rightFace);

            Assert.AreEqual(3, comp.Instances.Count);
            Assert.IsFalse(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));
        }

        [TestMethod]
        public void RemoveFaceInstanceParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var rightFace = gm.Geometry.Faces.First(f => f.Name == "RightWall");

            Assert.AreEqual(4, comp.Instances.Count);
            Assert.IsTrue(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            projectData.GeometryCommunicator.DisAssociate(comp, rightFace);

            Assert.AreEqual(3, comp.Instances.Count);
            Assert.IsFalse(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));
            AssertUtil.AssertDoubleEqual(163.0, aParam.ValueCurrent);
            AssertUtil.AssertDoubleEqual(3.0, nrtotalParam.ValueCurrent);
        }

        [TestMethod]
        public void RemoveLastFaceInstance()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Floor");
            var face = gm.Geometry.Faces.First(f => f.Name == "SmallFloor");
            var doutParam = comp.Parameters.FirstOrDefault(x => x.Name == "Δdout");
            var dinParam = comp.Parameters.FirstOrDefault(x => x.Name == "Δdin");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            //Remove last instance
            projectData.GeometryCommunicator.DisAssociate(comp, face);

            Assert.AreEqual(0, comp.Instances.Count);

            Assert.AreEqual(0, aParam.ValueCurrent);
            Assert.AreEqual(0, nrtotalParam.ValueCurrent);
        }


        [TestMethod]
        public void MissingFaceWithClone()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var faceComp = projectData.Components.First(x => x.Name == "Wall 2");

            var newGM = gm.Geometry.Clone();
            var face = newGM.Faces.First(x => x.Name == "Face_StandaloneRect");
            var instance = faceComp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var placement = instance.Placements.OfType<SimInstancePlacementGeometry>().First();
            var faceBoundary = face.Boundary;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), faceComp.InstanceState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);

            face.RemoveFromModel();
            gm.Geometry = newGM;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound), faceComp.InstanceState);
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, placement.State);

            newGM = newGM.Clone();
            var el = newGM.EdgeLoops.First(x => x.Id == faceBoundary.Id);
            var newFace = new Face(el.Layer, "", el, GeometricOrientation.Forward);

            gm.Geometry = newGM;
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), faceComp.InstanceState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
        }
    }
}
