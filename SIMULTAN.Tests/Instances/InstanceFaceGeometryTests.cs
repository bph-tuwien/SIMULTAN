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
        private static readonly FileInfo migrationTestProject = new FileInfo(@".\InstancePropagationMigrationTest_v10.simultan");

        #region Add

        [TestMethod]
        public void AddFaceInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall");
            var leftFace = gm.Geometry.Faces.First(f => f.Name == "LeftWall");
            var floorFace = gm.Geometry.Faces.First(f => f.Name == "Floor");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, leftFace);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(SimInstanceType.AttributesFace, inst1.InstanceType);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(leftFace.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);

            //Second association
            projectData.ComponentGeometryExchange.Associate(comp, floorFace);

            Assert.AreEqual(2, comp.Instances.Count);

            var inst2 = comp.Instances.First(x => x != inst1);
            Assert.AreEqual(comp, inst2.Component);
            Assert.AreEqual(SimInstanceType.AttributesFace, inst2.InstanceType);
            Assert.AreEqual(true, inst2.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst2.State.ConnectionState);
            Assert.AreEqual(1, inst2.Placements.Count);

            geomPlacement = inst2.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(floorFace.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        /// <summary>
        /// Add an instance for multiple faces at once.
        /// </summary>
        [TestMethod]
        public void AddFacesInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall");
            var leftFace = gm.Geometry.Faces.First(f => f.Name == "LeftWall");
            var floorFace = gm.Geometry.Faces.First(f => f.Name == "Floor");

            Assert.AreEqual(0, comp.Instances.Count);

            var faceList = new List<Face>();
            faceList.Add(leftFace);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, faceList);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(SimInstanceType.AttributesFace, inst1.InstanceType);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(leftFace.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);

            // add second one
            faceList.Clear();
            faceList.Add(floorFace);
            projectData.ComponentGeometryExchange.Associate(comp, faceList);

            var inst2 = comp.Instances.First(x => x != inst1);
            Assert.AreEqual(comp, inst2.Component);
            Assert.AreEqual(SimInstanceType.AttributesFace, inst2.InstanceType);
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

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall");
            var leftFace = gm.Geometry.Faces.First(f => f.Name == "LeftWall");
            var floorFace = gm.Geometry.Faces.First(f => f.Name == "Floor");

            Assert.AreEqual(0, comp.Parameters.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, leftFace);

            Assert.AreEqual(2, comp.Parameters.Count);

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            Assert.AreNotEqual(null, aParam);
            Assert.AreNotEqual(null, nrtotalParam);

            Assert.AreEqual(1, nrtotalParam.ValueCurrent);
            AssertUtil.AssertDoubleEqual(25.0, aParam.ValueCurrent);

            //Add second association
            projectData.ComponentGeometryExchange.Associate(comp, floorFace);
            Assert.AreEqual(2, nrtotalParam.ValueCurrent);
            AssertUtil.AssertDoubleEqual(75.0, aParam.ValueCurrent);
        }

        [TestMethod]
        public void AddFaceInstancePath()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall");
            var leftFace = gm.Geometry.Faces.First(f => f.Name == "LeftWall");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, leftFace);

            //No path for this type of instance
            var instance = comp.Instances[0];
            Assert.AreEqual(0, instance.InstancePath.Count);
            AssertUtil.AssertDoubleEqual(0.0, instance.InstancePathLength);
        }

        [TestMethod]
        public void FaceAssociateAgain()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");

            var inst = comp.Instances.First(c => c.Placements.Any(p => p is SimInstancePlacementGeometry geo && geo.GeometryId == face.Id));

            Assert.IsNotNull(inst);
            Assert.AreEqual(4, comp.Instances.Count);

            projectData.ComponentGeometryExchange.Associate(comp, face);

            var newinst = comp.Instances.First(c => c.Placements.Any(p => p is SimInstancePlacementGeometry geo && geo.GeometryId == face.Id));
            Assert.AreEqual(4, comp.Instances.Count);
            Assert.AreEqual(inst, newinst);
        }

        #endregion

        #region Changes

        [TestMethod]
        public void FaceChangedParameters()
        {
            LoadProject(testProject, "arch", "arch");

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
            LoadProject(testProject, "arch", "arch");

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
            LoadProject(testProject, "arch", "arch");

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
        public void MissingFaceWithClone()
        {
            LoadProject(testProject);
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
            var newFace = new Face(face.Id, el.Layer, "", el, GeometricOrientation.Forward);

            gm.Geometry = newGM;
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), faceComp.InstanceState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
        }

        [TestMethod]
        public void ReplaceGeometryModel()
        {
            LoadProject(testProject, "arch", "arch");
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instance);
            Assert.AreEqual(4, comp.Instances.Count);
            AssertUtil.AssertDoubleEqual(188.0, aParam.ValueCurrent);

            var gmCopy = gm.Geometry.Clone();
            var vertex = gmCopy.Vertices.First(f => f.Id == 2);

            vertex.Position = new Point3D(11.0, 0.0, 0.0);

            gm.Geometry = gmCopy;

            var faceNew = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instanceNew = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instanceNew);
            Assert.AreEqual(instance, instanceNew);
            Assert.IsNotNull(faceNew);
            Assert.AreEqual(4, comp.Instances.Count);

            AssertUtil.AssertDoubleEqual(193, aParam.ValueCurrent);
        }

        [TestMethod]
        public void ReplaceGeometryModelFaceDelete()
        {
            LoadProject(testProject, "arch", "arch");
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.IsNotNull(instance);
            Assert.IsNotNull(geomPlacement);
            Assert.AreEqual(4, comp.Instances.Count);
            AssertUtil.AssertDoubleEqual(188.0, aParam.ValueCurrent);

            var gmCopy = gm.Geometry.Clone();
            var vertex = gmCopy.Vertices.First(f => f.Id == 2);

            gm.Geometry.Faces.Remove(face);

            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, geomPlacement.State);
            AssertUtil.AssertDoubleEqual(100.0, aParam.ValueCurrent);

            gm.Geometry = gmCopy;

            var faceNew = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instanceNew = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instanceNew);
            Assert.AreEqual(instance, instanceNew);
            Assert.IsNotNull(faceNew);
            Assert.AreEqual(4, comp.Instances.Count);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
            AssertUtil.AssertDoubleEqual(188, aParam.ValueCurrent);

            vertex.Position = new Point3D(11.0, 0.0, 0.0);

            AssertUtil.AssertDoubleEqual(193, aParam.ValueCurrent);
        }

        #endregion

        #region Remove

        [TestMethod]
        public void RemoveFaceInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var rightFace = gm.Geometry.Faces.First(f => f.Name == "RightWall");

            Assert.AreEqual(4, comp.Instances.Count);
            Assert.IsTrue(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));

            projectData.ComponentGeometryExchange.Disassociate(comp, rightFace);

            Assert.AreEqual(3, comp.Instances.Count);
            Assert.IsFalse(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));
        }

        [TestMethod]
        public void RemoveFaceInstanceParameters()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var rightFace = gm.Geometry.Faces.First(f => f.Name == "RightWall");

            Assert.AreEqual(4, comp.Instances.Count);
            Assert.IsTrue(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            projectData.ComponentGeometryExchange.Disassociate(comp, rightFace);

            Assert.AreEqual(3, comp.Instances.Count);
            Assert.IsFalse(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));
            AssertUtil.AssertDoubleEqual(163.0, aParam.ValueCurrent);
            AssertUtil.AssertDoubleEqual(3.0, nrtotalParam.ValueCurrent);
        }

        [TestMethod]
        public void RemoveLastFaceInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Floor");
            var face = gm.Geometry.Faces.First(f => f.Name == "SmallFloor");
            var doutParam = comp.Parameters.FirstOrDefault(x => x.Name == "Δdout");
            var dinParam = comp.Parameters.FirstOrDefault(x => x.Name == "Δdin");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            //Remove last instance
            projectData.ComponentGeometryExchange.Disassociate(comp, face);

            Assert.AreEqual(0, comp.Instances.Count);

            Assert.AreEqual(0, aParam.ValueCurrent);
            Assert.AreEqual(0, nrtotalParam.ValueCurrent);
        }

        [TestMethod]
        public void RemoveFaceState()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var face = gm.Geometry.Faces.First(f => f.Name == "RightWall");
            var faceInst = comp.Instances.First(x => x.Placements.Any(p => p is SimInstancePlacementGeometry gp &&
                gp.FileId == resource.Key && gp.GeometryId == face.Id));

            face.RemoveFromModel();

            Assert.AreEqual(4, comp.Instances.Count);
            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, comp.InstanceState.ConnectionState);
            Assert.AreEqual(true, comp.InstanceState.IsRealized);

            foreach (var inst in comp.Instances.Where(x => x != faceInst))
            {
                Assert.AreEqual(SimInstanceConnectionState.Ok, inst.State.ConnectionState);
                Assert.AreEqual(true, inst.State.IsRealized);
            }

            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, faceInst.State.ConnectionState);
            Assert.AreEqual(true, faceInst.State.IsRealized);
            Assert.AreEqual(1, faceInst.Placements.Count);
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, faceInst.Placements[0].State);
        }

        [TestMethod]
        public void RemoveFaceParameters()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var face = gm.Geometry.Faces.First(f => f.Name == "RightWall");
            var faceInst = comp.Instances.First(x => x.Placements.Any(p => p is SimInstancePlacementGeometry gp &&
                gp.FileId == resource.Key && gp.GeometryId == face.Id));

            face.RemoveFromModel();

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            Assert.AreNotEqual(null, aParam);
            Assert.AreNotEqual(null, nrtotalParam);

            Assert.AreEqual(4, nrtotalParam.ValueCurrent);
            AssertUtil.AssertDoubleEqual(163.0, aParam.ValueCurrent);
        }

        [TestMethod]
        public void RemoveFaceInstancePath()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var face = gm.Geometry.Faces.First(f => f.Name == "RightWall");
            var faceInst = comp.Instances.First(x => x.Placements.Any(p => p is SimInstancePlacementGeometry gp &&
                gp.FileId == resource.Key && gp.GeometryId == face.Id));

            face.RemoveFromModel();

            Assert.AreEqual(0, faceInst.InstancePath.Count);
        }

        #endregion

        #region Manuel Instance Manipulation

        [TestMethod]
        public void ManualCreate()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");

            Assert.IsNotNull(face);

            var comp = new SimComponent()
            {
                Name = "NewComp",
                InstanceType = SimInstanceType.AttributesFace
            };
            projectData.Components.Add(comp);

            var instance = new SimComponentInstance(SimInstanceType.AttributesFace);

            comp.Instances.Add(instance);

            var placement = new SimInstancePlacementGeometry(resource.Key, face.Id);

            instance.Placements.Add(placement);

            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
            Assert.IsTrue(instance.State.IsRealized);
            Assert.IsTrue(comp.InstanceState.IsRealized);

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            Assert.IsNotNull(aParam);
            AssertUtil.AssertDoubleEqual(88.0, aParam.ValueCurrent);
            Assert.IsNotNull(nrtotalParam);
            AssertUtil.AssertDoubleEqual(1, nrtotalParam.ValueCurrent);
        }

        [TestMethod]
        public void ManualCreate2()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");

            Assert.IsNotNull(face);

            var comp = new SimComponent()
            {
                Name = "NewComp",
                InstanceType = SimInstanceType.AttributesFace
            };
            projectData.Components.Add(comp);

            var instance = new SimComponentInstance(SimInstanceType.AttributesFace);

            var placement = new SimInstancePlacementGeometry(resource.Key, face.Id);

            instance.Placements.Add(placement);

            comp.Instances.Add(instance);

            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            Assert.IsNotNull(aParam);
            AssertUtil.AssertDoubleEqual(88.0, aParam.ValueCurrent);
            Assert.IsNotNull(nrtotalParam);
            AssertUtil.AssertDoubleEqual(1, nrtotalParam.ValueCurrent);
        }

        [TestMethod]
        public void ManualCreate3()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");

            Assert.IsNotNull(face);

            var comp = new SimComponent()
            {
                Name = "NewComp",
                InstanceType = SimInstanceType.AttributesFace
            };
            var instance = new SimComponentInstance(SimInstanceType.AttributesFace);
            var placement = new SimInstancePlacementGeometry(resource.Key, face.Id);

            instance.Placements.Add(placement);
            comp.Instances.Add(instance);
            projectData.Components.Add(comp);

            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            Assert.IsNotNull(aParam);
            AssertUtil.AssertDoubleEqual(88.0, aParam.ValueCurrent);
            Assert.IsNotNull(nrtotalParam);
            AssertUtil.AssertDoubleEqual(1, nrtotalParam.ValueCurrent);
        }
        
        /// <summary>
        /// Create instance to unopened geometry. Should not create parameters.
        /// </summary>
        [TestMethod]
        public void ManualCreateUnopened()
        {
            LoadProject(testProject);

            var resourceKey = 2;
            ulong faceId = 21;

            var comp = new SimComponent()
            {
                Name = "NewComp",
                InstanceType = SimInstanceType.AttributesFace
            };
            projectData.Components.Add(comp);

            var instance = new SimComponentInstance(SimInstanceType.AttributesFace);

            comp.Instances.Add(instance);

            var placement = new SimInstancePlacementGeometry(resourceKey, faceId);

            instance.Placements.Add(placement);

            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
            Assert.IsTrue(instance.State.IsRealized);
            Assert.IsTrue(comp.InstanceState.IsRealized);

            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var nrtotalParam = comp.Parameters.FirstOrDefault(x => x.Name == "NRᴛᴏᴛᴀʟ");

            Assert.IsNull(aParam);
            Assert.IsNull(nrtotalParam);
        }

        [TestMethod]
        public void ManualRemovePlacement()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            instance.Placements.Remove(geomPlacement);

            Assert.AreEqual(100.0, aParam.ValueCurrent);
            var found = projectData.ComponentGeometryExchange.GetComponents(face).Contains(comp);
            Assert.IsFalse(found);
            Assert.IsFalse(instance.State.IsRealized);
            Assert.IsTrue(comp.InstanceState.IsRealized);

            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
        }

        [TestMethod]
        public void ManualRemovePlacement2()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            comp.Instances.Remove(instance);

            Assert.AreEqual(100.0, aParam.ValueCurrent);
            var found = projectData.ComponentGeometryExchange.GetComponents(face).Contains(comp);
            Assert.IsFalse(found);
            Assert.IsTrue(comp.InstanceState.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
        }

        [TestMethod]
        public void ManualRemovePlacement3()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            projectData.Components.Remove(comp);

            var found = projectData.ComponentGeometryExchange.GetComponents(face).Contains(comp);
            Assert.IsFalse(found);
        }
         
        /// <summary>
        /// Manaually remove instances of an unopened geometry file
        /// </summary>
        [TestMethod]
        public void ManualRemoveUnopenedInstances()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instances = comp.Instances.Where(x => x.Placements.Any(y => y is SimInstancePlacementGeometry pg && pg.FileId != resource.Key)).ToList();

            Assert.IsNotNull(instances);
            Assert.IsTrue(instances.Count > 0);

            foreach(var inst in instances)
            {
                comp.Instances.Remove(inst);
            }

            AssertUtil.AssertDoubleEqual(188.0, aParam.ValueCurrent);
            var found = projectData.ComponentGeometryExchange.GetComponents(face).Contains(comp);
            Assert.IsTrue(found);
        }

        [TestMethod]
        public void ManualClearPlacement()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            instance.Placements.Clear();

            Assert.AreEqual(100.0, aParam.ValueCurrent);
            var found = projectData.ComponentGeometryExchange.GetComponents(face).Contains(comp);
            Assert.IsFalse(found);
            Assert.IsFalse(instance.State.IsRealized);
            Assert.IsTrue(comp.InstanceState.IsRealized);

            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
        }

        [TestMethod]
        public void ManualClearPlacement2()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            comp.Instances.Clear();

            // only value of open geometry models should change
            Assert.AreEqual(100.0, aParam.ValueCurrent);
            var found = projectData.ComponentGeometryExchange.GetComponents(face).Contains(comp);
            Assert.IsFalse(found);
            Assert.IsFalse(comp.InstanceState.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
        }

        [TestMethod]
        public void ManualClearPlacement3()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var aParam = comp.Parameters.FirstOrDefault(x => x.Name == "A");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            projectData.Components.Clear();

            var found = projectData.ComponentGeometryExchange.GetComponents(face).Contains(comp);
            Assert.IsFalse(found);
        }

        #endregion

        #region Parameter Changes

        [TestMethod]
        public void DinParameterchanged()
        {
            LoadProject(testProject, "arch", "arch");
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var param = comp.Parameters.First(x => x.Name == "Δdin");

            List<List<BaseGeometry>> eventData = new List<List<BaseGeometry>>();
            projectData.ComponentGeometryExchange.GeometryInvalidated += (s, e) => eventData.Add(e.ToList());

            //Set parameter
            param.ValueCurrent = 1.0;

            //Check notifications
            Assert.AreEqual(1, eventData.Count);
            Assert.AreEqual(3, eventData[0].Count);
            Assert.IsTrue(eventData[0].Any(x => x.Id == 25));
            Assert.IsTrue(eventData[0].Any(x => x.Id == 30));
            Assert.IsTrue(eventData[0].Any(x => x.Id == 65));
        }

        [TestMethod]
        public void DoutParameterchanged()
        {
            LoadProject(testProject, "arch", "arch");
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var param = comp.Parameters.First(x => x.Name == "Δdout");

            List<List<BaseGeometry>> eventData = new List<List<BaseGeometry>>();
            projectData.ComponentGeometryExchange.GeometryInvalidated += (s, e) => eventData.Add(e.ToList());

            //Set parameter
            param.ValueCurrent = 1.0;

            //Check notifications
            Assert.AreEqual(1, eventData.Count);
            Assert.AreEqual(3, eventData[0].Count);
            Assert.IsTrue(eventData[0].Any(x => x.Id == 25));
            Assert.IsTrue(eventData[0].Any(x => x.Id == 30));
            Assert.IsTrue(eventData[0].Any(x => x.Id == 65));
        }

        [TestMethod]
        public void DinInstanceParameterchanged()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var param = comp.Parameters.First(x => x.Name == "Δdin");

            List<List<BaseGeometry>> eventData = new List<List<BaseGeometry>>();
            projectData.ComponentGeometryExchange.GeometryInvalidated += (s, e) => eventData.Add(e.ToList());

            //Instance Parameter set
            var instance = comp.Instances.First();
            var pl = (SimInstancePlacementGeometry)instance.Placements.First(x => x is SimInstancePlacementGeometry);
            instance.InstanceParameterValuesPersistent[param] = 1.0;

            //Check notifications
            Assert.AreEqual(1, eventData.Count);
            Assert.AreEqual(1, eventData[0].Count);
            Assert.IsTrue(eventData[0].Any(x => x.Id == pl.GeometryId));
        }

        [TestMethod]
        public void DoutInstanceParameterchanged()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var param = comp.Parameters.First(x => x.Name == "Δdout");

            List<List<BaseGeometry>> eventData = new List<List<BaseGeometry>>();
            projectData.ComponentGeometryExchange.GeometryInvalidated += (s, e) => eventData.Add(e.ToList());

            //Instance Parameter set
            var instance = comp.Instances.First();
            var pl = (SimInstancePlacementGeometry)instance.Placements.First(x => x is SimInstancePlacementGeometry);
            instance.InstanceParameterValuesPersistent[param] = 1.0;

            //Check notifications
            Assert.AreEqual(1, eventData.Count);
            Assert.AreEqual(1, eventData[0].Count);
            Assert.IsTrue(eventData[0].Any(x => x.Id == pl.GeometryId));
        }

        [TestMethod]
        public void DinDoutInstancePropagationParameterchanged()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var param = comp.Parameters.First(x => x.Name == "Δdin");

            List<List<BaseGeometry>> eventData = new List<List<BaseGeometry>>();
            projectData.ComponentGeometryExchange.GeometryInvalidated += (s, e) => eventData.Add(e.ToList());

            //Set propagation to false
            // Todo: replace with component propagate parameter
            comp.Instances.ToList().ForEach(x => x.PropagateParameterChanges = false);
            Assert.AreEqual(0, eventData.Count);

            //Instance Parameter set (no change since no propagation)
            var instance = comp.Instances.First();
            var pl = (SimInstancePlacementGeometry)instance.Placements.First(x => x is SimInstancePlacementGeometry);
            param.ValueCurrent = 1.0;
            Assert.AreEqual(1, eventData.Count);

            //Set propagation to true -> event since din is newly propagated
            comp.Instances.ToList().ForEach(x => x.PropagateParameterChanges = true);
            Assert.AreEqual(4, eventData.Count); // +2 (din/dout) for each 
            Assert.AreEqual(1, eventData.Skip(1).Count(x => x[0].Id == 25));
            Assert.AreEqual(1, eventData.Skip(1).Count(x => x[0].Id == 30));
            Assert.AreEqual(1, eventData.Skip(1).Count(x => x[0].Id == 65));
        }

        [TestMethod]
        public void PropagateParameterChangesMigrationTest()
        {
            LoadProject(migrationTestProject);

            var propOff = projectData.Components.First(x => x.Name == "Propagation Off");
            var propOn = projectData.Components.First(x => x.Name == "Propagation On");

            Assert.IsNotNull(propOff);
            Assert.IsNotNull(propOn);
            Assert.AreEqual(1, propOff.Instances.Count);
            Assert.AreEqual(1, propOn.Instances.Count);
            Assert.AreEqual(false, propOff.Parameters.Any(x => x.Name == ReservedParameters.RP_INST_PROPAGATE));
            Assert.AreEqual(false, propOn.Parameters.Any(x => x.Name == ReservedParameters.RP_INST_PROPAGATE));
            Assert.AreEqual(false, propOff.Instances[0].PropagateParameterChanges);
            Assert.AreEqual(true, propOn.Instances[0].PropagateParameterChanges);
        }

        #endregion
    }
}
