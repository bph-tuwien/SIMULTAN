using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.TestUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace SIMULTAN.Tests.Instances
{
    [TestClass]
    public class InstanceFaceGeometryTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./GeometryInstanceTestsProject.simultan");
        private static readonly FileInfo migrationTestProject = new FileInfo(@"./InstancePropagationMigrationTest_v10.simultan");

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
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instance);
            Assert.AreEqual(4, comp.Instances.Count);

            var gmCopy = gm.Geometry.Clone();
            var vertex = gmCopy.Vertices.First(f => f.Id == 2);

            vertex.Position = new SimPoint3D(11.0, 0.0, 0.0);

            gm.Geometry = gmCopy;

            var faceNew = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instanceNew = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));

            Assert.IsNotNull(instanceNew);
            Assert.AreEqual(instance, instanceNew);
            Assert.IsNotNull(faceNew);
            Assert.AreEqual(4, comp.Instances.Count);
        }

        [TestMethod]
        public void ReplaceGeometryModelFaceDelete()
        {
            LoadProject(testProject, "arch", "arch");
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.IsNotNull(instance);
            Assert.IsNotNull(geomPlacement);
            Assert.AreEqual(4, comp.Instances.Count);

            var gmCopy = gm.Geometry.Clone();
            var vertex = gmCopy.Vertices.First(f => f.Id == 2);

            gm.Geometry.Faces.Remove(face);

            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, geomPlacement.State);

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

            projectData.ComponentGeometryExchange.Disassociate(comp, rightFace);

            Assert.AreEqual(3, comp.Instances.Count);
            Assert.IsFalse(comp.Instances.Any(i => i.Placements.Any(pl => pl is SimInstancePlacementGeometry gp && gp.GeometryId == rightFace.Id)));
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

            var instance = new SimComponentInstance();

            comp.Instances.Add(instance);

            var placement = new SimInstancePlacementGeometry(resource.Key, face.Id, SimInstanceType.AttributesFace);

            instance.Placements.Add(placement);

            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
            Assert.IsTrue(instance.State.IsRealized);
            Assert.IsTrue(comp.InstanceState.IsRealized);
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

            var instance = new SimComponentInstance();

            var placement = new SimInstancePlacementGeometry(resource.Key, face.Id, SimInstanceType.AttributesFace);

            instance.Placements.Add(placement);

            comp.Instances.Add(instance);

            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
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
            var instance = new SimComponentInstance();
            var placement = new SimInstancePlacementGeometry(resource.Key, face.Id, SimInstanceType.AttributesFace);

            instance.Placements.Add(placement);
            comp.Instances.Add(instance);
            projectData.Components.Add(comp);

            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
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

            var instance = new SimComponentInstance();

            comp.Instances.Add(instance);

            var placement = new SimInstancePlacementGeometry(resourceKey, faceId, SimInstanceType.AttributesFace);

            instance.Placements.Add(placement);

            Assert.AreEqual(SimInstanceConnectionState.Ok, comp.InstanceState.ConnectionState);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
            Assert.IsTrue(instance.State.IsRealized);
            Assert.IsTrue(comp.InstanceState.IsRealized);
        }

        [TestMethod]
        public void ManualRemovePlacement()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            instance.Placements.Remove(geomPlacement);

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
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            comp.Instances.Remove(instance);

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
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instances = comp.Instances.Where(x => x.Placements.Any(y => y is SimInstancePlacementGeometry pg && pg.FileId != resource.Key)).ToList();

            Assert.IsNotNull(instances);
            Assert.IsTrue(instances.Count > 0);

            foreach (var inst in instances)
            {
                comp.Instances.Remove(inst);
            }

            var found = projectData.ComponentGeometryExchange.GetComponents(face).Contains(comp);
            Assert.IsTrue(found);
        }

        [TestMethod]
        public void ManualClearPlacement()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry2.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Wall 2");
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            instance.Placements.Clear();

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
            var face = gm.Geometry.Faces.First(f => f.Name == "Surface 21");
            var instance = comp.Instances.First(i =>
                i.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == face.Id && pg.FileId == resource.Key));
            var geomPlacement = instance.Placements[0] as SimInstancePlacementGeometry;

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);

            comp.Instances.Clear();

            // only value of open geometry models should change
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
            var param = comp.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_IN));

            List<List<BaseGeometry>> eventData = new List<List<BaseGeometry>>();
            projectData.ComponentGeometryExchange.GeometryInvalidated += (s, e) => eventData.Add(e.ToList());

            //Set parameter
            param.Value = 1.0;

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
            var param = comp.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_OUT));

            List<List<BaseGeometry>> eventData = new List<List<BaseGeometry>>();
            projectData.ComponentGeometryExchange.GeometryInvalidated += (s, e) => eventData.Add(e.ToList());

            //Set parameter
            param.Value = 1.0;

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
            var param = comp.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_IN));

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
            var param = comp.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_OUT));

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
            var param = comp.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_IN));

            List<List<BaseGeometry>> eventData = new List<List<BaseGeometry>>();
            projectData.ComponentGeometryExchange.GeometryInvalidated += (s, e) => eventData.Add(e.ToList());

            //Set propagation to false
            comp.Instances.ToList().ForEach(x => x.PropagateParameterChanges = false);
            Assert.AreEqual(0, eventData.Count);

            //Instance Parameter set (no change since no propagation)
            var instance = comp.Instances.First();
            var pl = (SimInstancePlacementGeometry)instance.Placements.First(x => x is SimInstancePlacementGeometry);
            param.Value = 1.0;
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
            Assert.AreEqual(false, propOff.Parameters.Any(x => x.NameTaxonomyEntry.Text == ReservedParameters.RP_INST_PROPAGATE));
            Assert.AreEqual(false, propOn.Parameters.Any(x => x.NameTaxonomyEntry.Text == ReservedParameters.RP_INST_PROPAGATE));
            Assert.AreEqual(false, propOff.Instances[0].PropagateParameterChanges);
            Assert.AreEqual(true, propOn.Instances[0].PropagateParameterChanges);
        }

        #endregion
    }
}
