using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.TestUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Instances
{
    [TestClass]
    public class InstanceVolumeGeometryTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\GeometryInstanceTestsProject.simultan");


        private static void CheckParameter(SimComponent comp, string name, double value, string unit,
            SimInfoFlow propagation, SimCategory category)
        {
            var taxkey = ReservedParameterKeys.NameToKeyLookup[name];
            var param = comp.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(x => x.HasReservedTaxonomyEntry(taxkey));
            Assert.IsNotNull(param);
            AssertUtil.AssertDoubleEqual(value, param.Value);
            Assert.AreEqual(unit, param.Unit);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual(propagation, param.Propagation);
            Assert.AreEqual(category, param.Category);
            Assert.IsNull(param.ValueSource);
        }

        #region New Instance

        [TestMethod]
        public void AddVolumeInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, volume);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(SimInstanceType.Entity3D, inst1.InstanceType);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(volume.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        [TestMethod]
        public void AddVolumeInstanceAlreadyConnected()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, volume);

            Assert.AreEqual(1, comp.Instances.Count);

            var inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(SimInstanceType.Entity3D, inst1.InstanceType);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            var geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(volume.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);

            // associate again, should not change anything
            projectData.ComponentGeometryExchange.Associate(comp, volume);

            Assert.AreEqual(1, comp.Instances.Count);

            inst1 = comp.Instances[0];
            Assert.AreEqual(comp, inst1.Component);
            Assert.AreEqual(SimInstanceType.Entity3D, inst1.InstanceType);
            Assert.AreEqual(true, inst1.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, inst1.State.ConnectionState);
            Assert.AreEqual(1, inst1.Placements.Count);

            geomPlacement = inst1.Placements[0] as SimInstancePlacementGeometry;
            Assert.AreNotEqual(null, geomPlacement);
            Assert.AreEqual(volume.Id, geomPlacement.GeometryId);
            Assert.AreEqual(resource.Key, geomPlacement.FileId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geomPlacement.State);
        }

        [TestMethod]
        public void AddVolumeInstanceMaterialReferences()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(room1Comp, volume);

            var wallComp = projectData.Components.First(x => x.Name == "Wall 2");

            List<Face> facesWithMaterial = new List<Face> {
                gm.Geometry.Faces.First(x => x.Name == "BackWall"),
                gm.Geometry.Faces.First(x => x.Name == "RightWall")
            };

            foreach (var subComp in room1Comp.Components.Where(x => x.Component != null && x.Component.InstanceType == SimInstanceType.GeometricSurface))
            {
                var geometricPlacement = (SimInstancePlacementGeometry)subComp.Component.Instances.First().Placements.First(x => x is SimInstancePlacementGeometry);
                if (geometricPlacement.FileId == resource.Key && facesWithMaterial.Any(x => x.Id == geometricPlacement.GeometryId))
                {
                    Assert.IsTrue(subComp.Component.ReferencedComponents.Any(x => x.Target == wallComp));
                }
                else
                    Assert.IsFalse(subComp.Component.ReferencedComponents.Any(x => x.Target == wallComp));
            }
        }

        #endregion

        #region Remove

        [TestMethod]
        public void RemoveVolumeInstance()
        {
            LoadProject(testProject);
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, volume);
            Assert.AreNotEqual(0, comp.Instances.Count);

            projectData.ComponentGeometryExchange.Disassociate(comp, volume);
            Assert.AreEqual(0, comp.Instances.Count);
            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.Ok), comp.InstanceState);
        }

        [TestMethod]
        public void RemovedVolumeState()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            //Remove volume
            volume.RemoveFromModel();

            //Check state
            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, volumeComp.InstanceState.ConnectionState);
            Assert.AreEqual(true, volumeComp.InstanceState.IsRealized);
            Assert.AreEqual(1, volumeComp.Instances.Count);

            var instance = volumeComp.Instances[0];
            Assert.AreEqual(SimInstanceConnectionState.GeometryNotFound, instance.State.ConnectionState);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(1, instance.Placements.Count);

            var placement = (SimInstancePlacementGeometry)instance.Placements[0];
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, placement.State);
            Assert.AreEqual(true, placement.IsValid);
        }

        [TestMethod]
        public void RemoveVolumeSubComponents()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            //Remove volume
            volume.RemoveFromModel();

            //Check subcomponents
            Assert.IsTrue(volumeComp.Components.All(x =>
                x.Component.InstanceType != SimInstanceType.GeometricVolume &&
                x.Component.InstanceType != SimInstanceType.GeometricSurface));
        }

        [TestMethod]
        public void RemoveVolumeNeightborReferences()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var room2Comp = projectData.Components.First(x => x.Name == "Room2");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(room1Comp, volume);

            volume.RemoveFromModel();

            Assert.AreEqual(0, room1Comp.ReferencedComponents.Count);
            Assert.AreEqual(0, room2Comp.ReferencedComponents.Count);
        }

        #endregion
    }
}
