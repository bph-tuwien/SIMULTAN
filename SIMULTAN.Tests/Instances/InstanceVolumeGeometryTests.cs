using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Utils;
using SIMULTAN.Utils;
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
    public class InstanceVolumeGeometryTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\GeometryInstanceTestsProject.simultan");


        private static void CheckParameter(SimComponent comp, string name, double value, string unit,
            SimInfoFlow propagation, SimCategory category)
        {
            var param = comp.Parameters.FirstOrDefault(x => x.Name == name);
            Assert.IsNotNull(param);
            AssertUtil.AssertDoubleEqual(value, param.ValueCurrent);
            Assert.AreEqual(unit, param.Unit);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual(propagation, param.Propagation);
            Assert.AreEqual(category, param.Category);
            Assert.IsNull(param.MultiValuePointer);
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
        public void AddVolumeInstanceSubComponents()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            Assert.AreEqual(0, comp.Components.Count);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(comp, volume);

            //Check number of subcomponents
            Assert.AreEqual(7, comp.Components.Count);

            //Check volume subcomponent
            {
                var subComp = comp.Components.FirstOrDefault(x => x.Component != null && x.Component.InstanceType == SimInstanceType.GeometricVolume)?.Component;
                Assert.IsNotNull(subComp);
                Assert.IsTrue(subComp.Name.StartsWith(volume.Name));
                Assert.AreEqual(1, subComp.Instances.Count);

                var placement = (SimInstancePlacementGeometry)subComp.Instances[0].Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                Assert.IsNotNull(placement);
                Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), subComp.Instances[0].State);
                Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
                Assert.AreEqual(resource.Key, placement.FileId);
                Assert.AreEqual(volume.Id, placement.GeometryId);
                Assert.AreEqual(0, placement.RelatedIds.Count);
            }

            //Check all faces
            foreach (var face in volume.Faces.Select(x => x.Face))
            {
                var subComp = comp.Components.FirstOrDefault(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == face.Id)))?.Component;

                Assert.IsNotNull(subComp);
                Assert.IsTrue(subComp.Name.StartsWith(face.Name));
                Assert.AreEqual(1, subComp.Instances.Count);
                Assert.AreEqual(SimInstanceType.GeometricSurface, subComp.InstanceType);

                var placement = (SimInstancePlacementGeometry)subComp.Instances[0].Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                Assert.IsNotNull(placement);
                Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), subComp.Instances[0].State);
                Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
                Assert.AreEqual(resource.Key, placement.FileId);
                Assert.AreEqual(face.Id, placement.GeometryId);
                Assert.AreEqual(1, placement.RelatedIds.Count);
                Assert.AreEqual(face.Boundary.Id, placement.RelatedIds[0]);
            }

            //Holes
            var wallFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var wallComp = comp.Components.FirstOrDefault(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == wallFace.Id)))?.Component;

            //Hole with face
            {
                var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");
                var holeComp = wallComp.Components.FirstOrDefault(c => c.Component != null && c.Component.Instances.Any(
                        inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == holeFace.Id)))?.Component;

                Assert.IsNotNull(holeComp);
                Assert.IsTrue(holeComp.Name.StartsWith(holeFace.Name));
                Assert.AreEqual(1, holeComp.Instances.Count);
                Assert.AreEqual(SimInstanceType.GeometricSurface, holeComp.InstanceType);

                var placement = (SimInstancePlacementGeometry)holeComp.Instances[0].Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                Assert.IsNotNull(placement);
                Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), holeComp.Instances[0].State);
                Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
                Assert.AreEqual(resource.Key, placement.FileId);
                Assert.AreEqual(holeFace.Id, placement.GeometryId);
                Assert.AreEqual(1, placement.RelatedIds.Count);
                Assert.AreEqual(holeFace.Boundary.Id, placement.RelatedIds[0]);
            }

            //Empty hole
            {
                var holeLoop = wallFace.Holes.First(x => x.Faces.Count == 1);
                var holeComp = wallComp.Components.FirstOrDefault(c => c.Component != null && c.Component.Instances.Any(
                        inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == holeLoop.Id)))?.Component;

                Assert.IsNotNull(holeComp);
                Assert.IsTrue(holeComp.Name.StartsWith(holeLoop.Name));
                Assert.AreEqual(1, holeComp.Instances.Count);
                Assert.AreEqual(SimInstanceType.GeometricSurface, holeComp.InstanceType);

                var placement = (SimInstancePlacementGeometry)holeComp.Instances[0].Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                Assert.IsNotNull(placement);
                Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), holeComp.Instances[0].State);
                Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
                Assert.AreEqual(resource.Key, placement.FileId);
                Assert.AreEqual(holeLoop.Id, placement.GeometryId);
                Assert.AreEqual(1, placement.RelatedIds.Count);
                Assert.AreEqual(wallFace.Id, placement.RelatedIds[0]);
            }

        }

        [TestMethod]
        public void AddVolumeInstanceSubFaceParameters()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.ComponentGeometryExchange.Associate(floorComp, floorFace);


            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            var wallComp = volumeComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;

            CheckParameter(wallComp, "A", 46.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "AᴍᴀX", 48.52, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "AᴍɪN", 43.04, "m²", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "b", 10.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "BᴍᴀX", 10.1, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "BᴍɪN", 9.8, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "h", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "HᴍᴀX", 5.2, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "HᴍɪN", 4.8, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "Kᴅᴀ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "Kꜰᴀ", 0.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
        }

        [TestMethod]
        public void AddVolumeInstanceSubHoleParameters()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.ComponentGeometryExchange.Associate(floorComp, floorFace);


            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            var wallComp = volumeComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;

            //Hole with face
            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Window1"))?.Component;

                CheckParameter(holeComp, "A", 2.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍᴀX", 2.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍɪN", 2.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "b", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍᴀX", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍɪN", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "h", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍᴀX", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍɪN", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "Kᴅᴀ", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "Kꜰᴀ", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            }

            //Empty hole
            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Edge Loop")).Component;

                CheckParameter(holeComp, "A", 2.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍᴀX", 2.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍɪN", 2.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "b", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍᴀX", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍɪN", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "h", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍᴀX", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍɪN", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "Kᴅᴀ", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "Kꜰᴀ", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            }
        }

        [TestMethod]
        public void AddVolumeInstanceSubVolumeParameters()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.ComponentGeometryExchange.Associate(floorComp, floorFace);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(roomComp, volume);

            var volumeComp = roomComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == volume.Id))).Component;

            //Check parameters

            CheckParameter(volumeComp, "Aᴀ", 50.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Aᴃɢꜰ", double.NaN, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Aɴꜰ", double.NaN, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Aɴɢꜰ", 47.04, "m²", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(volumeComp, "Hᴀ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Hʟɪᴄʜᴛ", 4.8, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Hʀᴏʜ", 4.8, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(volumeComp, "Kᴅᴀ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kᴅᴜᴋ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kᴅᴜᴋʀ", 0.2, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kꜰᴀ", 0.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kꜰᴏᴋ", 0.2, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kꜰᴏᴋʀ", double.NaN, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(volumeComp, "Lᴘᴇʀ", 30.2, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(volumeComp, "Vᴀ", 250.0, "m³", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Vᴃʀɪ", 262.59979, "m³", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Vɴʀɪ", 225.79234, "m³", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Vɴʀɪɴꜰ", 225.79234, "m³", SimInfoFlow.Input, SimCategory.Geometry);
        }

        [TestMethod]
        public void AddVolumeInstanceAssets()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(roomComp, volume);

            Assert.AreEqual(1, roomComp.ReferencedAssets.Count);
            Assert.AreEqual(resource, roomComp.ReferencedAssets[0].Resource);
            Assert.AreEqual(volume.Id.ToString(), roomComp.ReferencedAssets[0].ContainedObjectId);

            var subComp = roomComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == volume.Id))).Component;

            Assert.AreEqual(1, subComp.ReferencedAssets.Count);
            Assert.AreEqual(resource, subComp.ReferencedAssets[0].Resource);
            Assert.AreEqual(volume.Id.ToString(), subComp.ReferencedAssets[0].ContainedObjectId);

            foreach (var face in volume.Faces.Select(x => x.Face))
            {
                subComp = roomComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == face.Id))).Component;

                Assert.AreEqual(1, subComp.ReferencedAssets.Count);
                Assert.AreEqual(resource, subComp.ReferencedAssets[0].Resource);
                Assert.AreEqual(face.Id.ToString(), subComp.ReferencedAssets[0].ContainedObjectId);
            }

            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var faceComp = roomComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;

            //Hole with face
            {
                var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");
                var holeComp = faceComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                        inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == holeFace.Id))).Component;

                Assert.AreEqual(1, holeComp.ReferencedAssets.Count);
                Assert.AreEqual(resource, holeComp.ReferencedAssets[0].Resource);
                Assert.AreEqual(holeFace.Id.ToString(), holeComp.ReferencedAssets[0].ContainedObjectId);
            }
            //Empty hole
            {
                var holeLoop = frontFace.Holes.First(x => x.Faces.Count == 1);
                var holeComp = faceComp.Components.FirstOrDefault(c => c.Component != null && c.Component.Instances.Any(
                        inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == holeLoop.Id))).Component;

                Assert.AreEqual(1, holeComp.ReferencedAssets.Count);
                Assert.AreEqual(resource, holeComp.ReferencedAssets[0].Resource);
                Assert.AreEqual(holeLoop.Id.ToString(), holeComp.ReferencedAssets[0].ContainedObjectId);
            }
        }

        [TestMethod]
        public void AddVolumeInstanceSubFacePath()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(roomComp, volume);

            //Face
            var frontface = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var faceComponent = roomComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontface.Id))).Component;
            var instance = faceComponent.Instances[0];

            AssertUtil.ContainEqualValuesDifferentStart(new Point3D[] {
                new Point3D(0, 0, 0),
                new Point3D(10, 0, 0),
                new Point3D(10, 5, 0),
                new Point3D(0, 5, 0)
                }, instance.InstancePath);

        }

        [TestMethod]
        public void AddVolumeInstanceSubHolePath()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(roomComp, volume);

            //Face
            var frontface = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var faceComponent = roomComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontface.Id))).Component;

            {
                var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");
                var holeComp = faceComponent.Components.First(c => c.Component != null && c.Component.Instances.Any(
                        inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == holeFace.Id))).Component;

                var instance = holeComp.Instances[0];

                AssertUtil.ContainEqualValuesDifferentStart(new Point3D[] {
                new Point3D(2, 2, 0),
                new Point3D(4, 2, 0),
                new Point3D(4, 3, 0),
                new Point3D(2, 3, 0)
                }, instance.InstancePath);
            }
            {
                var holeLoop = frontface.Holes.First(x => x.Faces.Count == 1);
                var holeComp = faceComponent.Components.First(c => c.Component != null && c.Component.Instances.Any(
                        inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == holeLoop.Id))).Component;

                var instance = holeComp.Instances[0];

                AssertUtil.ContainEqualValuesDifferentStart(new Point3D[] {
                new Point3D(5, 2, 0),
                new Point3D(7, 2, 0),
                new Point3D(7, 3, 0),
                new Point3D(5, 3, 0)
                }, instance.InstancePath);
            }
        }

        [TestMethod]
        public void AddVolumeInstanceSubVolumePath()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(roomComp, volume);

            //Room
            var instance = roomComp.Instances[0];
            Assert.AreEqual(0, instance.InstancePath.Count);

            //Volume
            var volumeComp = roomComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == volume.Id))).Component;
            instance = volumeComp.Instances[0];
            Assert.AreEqual(0, instance.InstancePath.Count);
        }

        [TestMethod]
        public void AddVolumeInstanceNeighborReferences()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var room2Comp = projectData.Components.First(x => x.Name == "Room2");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(room1Comp, volume);

            Assert.AreEqual(1, room1Comp.ReferencedComponents.Count);
            Assert.AreEqual(room2Comp, room1Comp.ReferencedComponents.First().Target);

            Assert.AreEqual(1, room2Comp.ReferencedComponents.Count);
            Assert.AreEqual(room1Comp, room2Comp.ReferencedComponents.First().Target);

            //Connecting face
            var wallComp = room1Comp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("BackWall")).Component;
            Assert.IsTrue(wallComp.ReferencedComponents.Any(x => x.Target == room2Comp));
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

        #region Geometry Changes

        [TestMethod]
        public void VolumeAddFace()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            //Remove face from volume
            gm.Geometry.StartBatchOperation();

            var leftFace = volume.Faces.RemoveFirst(x => x.Face.Name == "LeftWall");

            gm.Geometry.EndBatchOperation();

            Assert.AreEqual(5, volumeComp.Components
                .Where(x => x.Component != null && x.Component.InstanceType == SimInstanceType.GeometricSurface).Count());
        }

        private void ReplaceFace(GeometryModelData gm)
        {
            var volume = gm.Volumes.First(x => x.Name == "Room");
            var topFace = gm.Faces.First(x => x.Name == "Ceiling");
            var bottomFace = gm.Faces.First(x => x.Name == "Floor");

            var vtb = gm.Vertices.First(x => x.Id == 26);
            var vtf = gm.Vertices.First(x => x.Id == 14);
            var vbb = gm.Vertices.First(x => x.Id == 6);
            var vbf = gm.Vertices.First(x => x.Id == 1);

            gm.StartBatchOperation();

            topFace.Boundary.Edges.RemoveFirst(x => x.Edge.Id == 31);
            bottomFace.Boundary.Edges.RemoveFirst(x => x.Edge.Id == 9);
            volume.Faces.RemoveFirst(x => x.Face.Id == 33);

            //Create new edges, vertices
            var vertexTop = new Vertex(volume.Layer, "", new Point3D(0, 5, -2.5));
            var vertexBottom = new Vertex(volume.Layer, "", new Point3D(0, 0, -2.5));

            var eTop1 = new Edge(volume.Layer, "", new Vertex[] { vtb, vertexTop });
            var eTop2 = new Edge(volume.Layer, "", new Vertex[] { vertexTop, vtf });

            var eBottom1 = new Edge(volume.Layer, "", new Vertex[] { vbb, vertexBottom });
            var eBottom2 = new Edge(volume.Layer, "", new Vertex[] { vertexBottom, vbf });

            var vEdge = new Edge(volume.Layer, "", new Vertex[] { vertexTop, vertexBottom });

            //Replace edges
            topFace.Boundary.Edges.Add(new PEdge(eTop1, GeometricOrientation.Undefined, topFace.Boundary));
            topFace.Boundary.Edges.Add(new PEdge(eTop2, GeometricOrientation.Undefined, topFace.Boundary));

            bottomFace.Boundary.Edges.Add(new PEdge(eBottom1, GeometricOrientation.Undefined, bottomFace.Boundary));
            bottomFace.Boundary.Edges.Add(new PEdge(eBottom2, GeometricOrientation.Undefined, bottomFace.Boundary));

            //Create new face
            var newLoop = new EdgeLoop(volume.Layer, "", new Edge[] { eTop1, vEdge, eBottom1, gm.Edges.First(x => x.Id == 27) });
            var newFace = new Face(volume.Layer, "", newLoop, GeometricOrientation.Forward, null);
            volume.Faces.Add(new PFace(newFace, volume, GeometricOrientation.Undefined));

            newLoop = new EdgeLoop(volume.Layer, "", new Edge[] { eTop2, gm.Edges.First(x => x.Id == 15), eBottom2, vEdge });
            newFace = new Face(volume.Layer, "", newLoop, GeometricOrientation.Forward, null);
            volume.Faces.Add(new PFace(newFace, volume, GeometricOrientation.Undefined));

            gm.EndBatchOperation();
        }
        [TestMethod]
        public void VolumeTopologyChanged()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            //Replace one face
            ReplaceFace(gm.Geometry);

            //Check result
            Assert.AreEqual(7, volumeComp.Components.Where(x => x.Component != null && x.Component.InstanceType == SimInstanceType.GeometricSurface).Count());

            foreach (var face in volume.Faces.Select(x => x.Face))
            {
                var faceComp = volumeComp.Components.FirstOrDefault(x => x.Component != null && x.Component.Instances.Count == 1 &&
                x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.FileId == resource.Key && gp.GeometryId == face.Id))
                    ?.Component;
                Assert.IsNotNull(faceComp);
            }

        }

        [TestMethod]
        public void VolumeChangedSubVolumeParameters()
        {
            LoadProject(testProject, "arch", "arch");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            // Disable access checking as a workaround to check permissions for arch user
            using (AccessCheckingDisabler.Disable(floorComp.Factory))
            {
                projectData.ComponentGeometryExchange.Associate(floorComp, floorFace);
                //Add new association
                projectData.ComponentGeometryExchange.Associate(room1Comp, volume);
            }

            //Move right Wall by two meters and floor by -1 meter
            var rightWall = gm.Geometry.Faces.First(x => x.Name == "RightWall");
            var floorWall = gm.Geometry.Faces.First(x => x.Name == "Floor");
            gm.Geometry.StartBatchOperation();
            foreach (var vertex in rightWall.Boundary.Edges.Select(e => e.StartVertex))
                vertex.Position += new Vector3D(2, 0, 0);
            foreach (var vertex in floorWall.Boundary.Edges.Select(e => e.StartVertex))
                vertex.Position += new Vector3D(0, -1, 0);
            gm.Geometry.EndBatchOperation();

            //Check parameters
            var volumeComp = room1Comp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == volume.Id))).Component;

            CheckParameter(volumeComp, "Aᴀ", 60.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Aᴃɢꜰ", double.NaN, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Aɴꜰ", double.NaN, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Aɴɢꜰ", 56.64, "m²", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(volumeComp, "Hᴀ", 6.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Hʟɪᴄʜᴛ", 5.8, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Hʀᴏʜ", 5.8, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(volumeComp, "Kᴅᴀ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kᴅᴜᴋ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kᴅᴜᴋʀ", -0.8, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kꜰᴀ", -1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kꜰᴏᴋ", -0.8, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Kꜰᴏᴋʀ", double.NaN, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(volumeComp, "Lᴘᴇʀ", 33.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(volumeComp, "Vᴀ", 360.0, "m³", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Vᴃʀɪ", 374.32076, "m³", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Vɴʀɪ", 328.51246, "m³", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(volumeComp, "Vɴʀɪɴꜰ", 328.51246, "m³", SimInfoFlow.Input, SimCategory.Geometry);
        }
        [TestMethod]
        public void VolumeChangedSubFaceParameters()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.ComponentGeometryExchange.Associate(floorComp, floorFace);

            //Add new association
            projectData.ComponentGeometryExchange.Associate(room1Comp, volume);

            //Move right Wall by two meters and floor by -1 meter
            var rightWall = gm.Geometry.Faces.First(x => x.Name == "RightWall");
            var floorWall = gm.Geometry.Faces.First(x => x.Name == "Floor");
            gm.Geometry.StartBatchOperation();
            foreach (var vertex in rightWall.Boundary.Edges.Select(e => e.StartVertex))
                vertex.Position += new Vector3D(2, 0, 0);
            foreach (var vertex in floorWall.Boundary.Edges.Select(e => e.StartVertex))
                vertex.Position += new Vector3D(0, -1, 0);
            gm.Geometry.EndBatchOperation();

            //Check results
            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var wallComp = room1Comp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;

            CheckParameter(wallComp, "A", 68.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "AᴍᴀX", 71.02, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "AᴍɪN", 64.44, "m²", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "b", 12.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "BᴍᴀX", 12.1, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "BᴍɪN", 11.8, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "h", 6.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "HᴍᴀX", 6.2, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "HᴍɪN", 5.8, "m", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "Kᴅᴀ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "Kꜰᴀ", -1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
        }
        [TestMethod]
        public void VolumeChangedSubHoleParameters()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.ComponentGeometryExchange.Associate(floorComp, floorFace);


            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            var wallComp = volumeComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;

            gm.Geometry.StartBatchOperation();

            gm.Geometry.Vertices.First(x => x.Id == 44).Position -= new Vector3D(1, 0, 0);
            gm.Geometry.Vertices.First(x => x.Id == 38).Position -= new Vector3D(1, 0, 0);

            gm.Geometry.Vertices.First(x => x.Id == 37).Position += new Vector3D(1, 0, 0);
            gm.Geometry.Vertices.First(x => x.Id == 10).Position += new Vector3D(1, 0, 0);

            gm.Geometry.EndBatchOperation();

            //Hole with face
            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Window1")).Component;

                CheckParameter(holeComp, "A", 3.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍᴀX", 3.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍɪN", 3.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "b", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍᴀX", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍɪN", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "h", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍᴀX", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍɪN", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "Kᴅᴀ", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "Kꜰᴀ", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            }

            //Empty hole
            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Edge Loop")).Component;

                CheckParameter(holeComp, "A", 3.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍᴀX", 3.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍɪN", 3.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "b", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍᴀX", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍɪN", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "h", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍᴀX", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍɪN", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "Kᴅᴀ", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "Kꜰᴀ", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            }
        }
        [TestMethod]
        public void VolumeChangedFacePath()
        {
            LoadProject(testProject, "arch", "arch");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            using (AccessCheckingDisabler.Disable(floorComp.Factory))
            {
                projectData.ComponentGeometryExchange.Associate(floorComp, floorFace);

                //Add new association
                projectData.ComponentGeometryExchange.Associate(room1Comp, volume);
            }

            //Move right Wall by two meters and floor by -1 meter
            var rightWall = gm.Geometry.Faces.First(x => x.Name == "RightWall");
            var floorWall = gm.Geometry.Faces.First(x => x.Name == "Floor");
            gm.Geometry.StartBatchOperation();
            foreach (var vertex in rightWall.Boundary.Edges.Select(e => e.StartVertex))
                vertex.Position += new Vector3D(2, 0, 0);
            foreach (var vertex in floorWall.Boundary.Edges.Select(e => e.StartVertex))
                vertex.Position += new Vector3D(0, -1, 0);
            gm.Geometry.EndBatchOperation();

            //Check path
            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var wallComp = room1Comp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;
            AssertUtil.ContainEqualValuesDifferentStart(new Point3D[]
            {
                new Point3D(0, -1, 0),
                new Point3D(12, -1, 0),
                new Point3D(12, 5, 0),
                new Point3D(0, 5, 0)
            }, wallComp.Instances[0].InstancePath);
        }
        [TestMethod]
        public void VolumeChangedHolePath()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.ComponentGeometryExchange.Associate(floorComp, floorFace);


            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            var wallComp = volumeComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;

            gm.Geometry.StartBatchOperation();

            gm.Geometry.Vertices.First(x => x.Id == 44).Position -= new Vector3D(1, 0, 0);
            gm.Geometry.Vertices.First(x => x.Id == 38).Position -= new Vector3D(1, 0, 0);

            gm.Geometry.Vertices.First(x => x.Id == 37).Position += new Vector3D(1, 0, 0);
            gm.Geometry.Vertices.First(x => x.Id == 10).Position += new Vector3D(1, 0, 0);

            gm.Geometry.EndBatchOperation();

            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Window1")).Component;
                AssertUtil.ContainEqualValuesDifferentStart(new Point3D[]
                {
                    new Point3D(1, 2, 0),
                    new Point3D(4, 2, 0),
                    new Point3D(4, 3, 0),
                    new Point3D(1, 3, 0),
                }, holeComp.Instances[0].InstancePath);
            }

            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Edge Loop")).Component;
                AssertUtil.ContainEqualValuesDifferentStart(new Point3D[]
                {
                    new Point3D(5, 2, 0),
                    new Point3D(8, 2, 0),
                    new Point3D(8, 3, 0),
                    new Point3D(5, 3, 0),
                }, holeComp.Instances[0].InstancePath);
            }
        }

        #endregion

        #region Geometry Name Changes

        [TestMethod]
        public void VolumeFaceNameChanged()
        {
            LoadProject(testProject, "arch", "arch");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            var volumeComp = projectData.Components.First(x => x.Name == "Room2");

            var face = gm.Geometry.Faces.First(x => x.Name == "Surface 91");
            var faceSubComp = volumeComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == face.Id))).Component;
            Assert.AreEqual(face.Name, faceSubComp.Name);

            face.Name = "asdfFace";
            Assert.AreEqual(face.Name, faceSubComp.Name);
        }

        [TestMethod]
        public void VolumeFaceNameReplaced()
        {
            LoadProject(testProject, "arch", "arch");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            var volumeComp = projectData.Components.First(x => x.Name == "Room2");
            var oldFace = gm.Geometry.Faces.First(x => x.Name == "Surface 91");

            var faceSubComp = volumeComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == oldFace.Id)))
                .Component;
            Assert.AreEqual(oldFace.Name, faceSubComp.Name);

            var newGM = gm.Geometry.Clone();
            var newFace = newGM.Faces.First(x => x.Name == "Surface 91");
            newFace.Name = "asdfFace";
            Assert.AreEqual(oldFace.Name, faceSubComp.Name);

            gm.Geometry = newGM;
            Assert.AreEqual(newFace.Name, faceSubComp.Name);
        }

        [TestMethod]
        public void VolumeVolumeNameChanged()
        {
            LoadProject(testProject, "arch", "arch");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            var volumeComp = projectData.Components.First(x => x.Name == "Room2");

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Volume 92");
            var volumeSubComp = volumeComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == volume.Id)))
                .Component;
            Assert.AreEqual(volume.Name, volumeSubComp.Name);

            volume.Name = "asdfFace";
            Assert.AreEqual(volume.Name, volumeSubComp.Name);
        }

        [TestMethod]
        public void VolumeVolumeNameReplaced()
        {
            LoadProject(testProject, "arch", "arch");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            var volumeComp = projectData.Components.First(x => x.Name == "Room2");
            var oldVolume = gm.Geometry.Volumes.First(x => x.Name == "Volume 92");

            var volumeSubComp = volumeComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == oldVolume.Id)))
                .Component;
            Assert.AreEqual(oldVolume.Name, volumeSubComp.Name);

            var newGM = gm.Geometry.Clone();
            var newVolume = newGM.Volumes.First(x => x.Name == "Volume 92");
            newVolume.Name = "asdfVolume";
            Assert.AreEqual(oldVolume.Name, volumeSubComp.Name);

            gm.Geometry = newGM;
            Assert.AreEqual(newVolume.Name, volumeSubComp.Name);
        }

        [TestMethod]
        public void VolumeHoleEmptyNameChanged()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var frontWall = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var hole = frontWall.Holes.First(x => x.Faces.Count == 1);

            var volumeComp = projectData.Components.First(x => x.Name == "Room1");

            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            var frontFaceComp = volumeComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == frontWall.Id)))
                .Component;
            var holeComp = frontFaceComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == hole.Id)))
                .Component;

            Assert.AreEqual(hole.Name, holeComp.Name);

            hole.Name = "asdfHole";
            Assert.AreEqual(hole.Name, holeComp.Name);
        }

        [TestMethod]
        public void VolumeHoleEmptyNameReplaced()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");

            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            var frontWall = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var hole = frontWall.Holes.First(x => x.Faces.Count == 1);

            var frontFaceComp = volumeComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == frontWall.Id)))
                .Component;
            var holeComp = frontFaceComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == hole.Id)))
                .Component;

            Assert.AreEqual(hole.Name, holeComp.Name);

            var newGM = gm.Geometry.Clone();
            frontWall = newGM.Faces.First(x => x.Name == "FrontWall");
            var newHole = frontWall.Holes.First(x => x.Faces.Count == 1);

            newHole.Name = "asdfHole";
            Assert.AreEqual(hole.Name, holeComp.Name);

            gm.Geometry = newGM;
            Assert.AreEqual(newHole.Name, holeComp.Name);
        }

        [TestMethod]
        public void VolumeHoleFilledNameChanged()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var frontWall = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var hole = frontWall.Holes.First(x => x.Faces.Count == 2);
            var holeFace = hole.Faces.First(x => x != frontWall);

            var volumeComp = projectData.Components.First(x => x.Name == "Room1");

            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            var frontFaceComp = volumeComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == frontWall.Id)))
                .Component;
            var holeComp = frontFaceComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == holeFace.Id)))
                .Component;

            Assert.AreEqual(holeFace.Name, holeComp.Name);

            holeFace.Name = "asdfHole";
            Assert.AreEqual(holeFace.Name, holeComp.Name);
        }

        [TestMethod]
        public void VolumeHoleFilledNameReplaced()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");

            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            var frontWall = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var hole = frontWall.Holes.First(x => x.Faces.Count == 2);
            var holeFace = hole.Faces.First(x => x != frontWall);

            var frontFaceComp = volumeComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == frontWall.Id)))
                .Component;
            var holeComp = frontFaceComp.Components.First(
                x => x.Component.Instances.Any(i => i.Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == holeFace.Id)))
                .Component;

            Assert.AreEqual(holeFace.Name, holeComp.Name);

            var newGM = gm.Geometry.Clone();
            frontWall = newGM.Faces.First(x => x.Name == "FrontWall");
            hole = frontWall.Holes.First(x => x.Faces.Count == 2);
            var newHoleFace = hole.Faces.First(x => x != frontWall);

            newHoleFace.Name = "asdfHole";
            Assert.AreEqual(holeFace.Name, holeComp.Name);

            gm.Geometry = newGM;
            Assert.AreEqual(newHoleFace.Name, holeComp.Name);
        }

        #endregion

        #region Holes

        [TestMethod]
        public void VolumeHoleAddedEmpty()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            gm.Geometry.StartBatchOperation();

            var holeVertices = new Vertex[]
            {
                new Vertex(volume.Layer, "", new Point3D(0, 1, -1)),
                new Vertex(volume.Layer, "", new Point3D(0, 3, -1)),
                new Vertex(volume.Layer, "", new Point3D(0, 3, -3)),
                new Vertex(volume.Layer, "", new Point3D(0, 1, -3)),
            };
            var holeEdges = new Edge[]
            {
                new Edge(volume.Layer, "", new Vertex[]{ holeVertices[0], holeVertices[1] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeVertices[1], holeVertices[2] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeVertices[2], holeVertices[3] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeVertices[3], holeVertices[0] }),
            };
            var holeLoop = new EdgeLoop(volume.Layer, "", holeEdges);

            var leftWall = gm.Geometry.Faces.First(x => x.Name == "LeftWall");
            leftWall.Holes.Add(holeLoop);

            gm.Geometry.EndBatchOperation();

            var leftWallComp = volumeComp.Components.First(x => x.Component != null && x.Component.Instances.Count == 1 &&
                x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == leftWall.Id))
                .Component;
            Assert.AreEqual(1, leftWallComp.Components.Count);

            var holeComp = leftWallComp.Components.First().Component;
            Assert.AreEqual(1, holeComp.Instances.Count);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), holeComp.Instances[0].State);

            var geometryPlacement = (SimInstancePlacementGeometry)holeComp.Instances[0].Placements.First(x => x is SimInstancePlacementGeometry);
            Assert.IsNotNull(geometryPlacement);
            Assert.AreEqual(resource.Key, geometryPlacement.FileId);
            Assert.AreEqual(holeLoop.Id, geometryPlacement.GeometryId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geometryPlacement.State);

            //Parameter check
            AssertUtil.AssertDoubleEqual(21.0, leftWallComp.Parameters.First(x => x.Name == "A").ValueCurrent);
        }
        [TestMethod]
        public void VolumeHoleAddedFilled()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            gm.Geometry.StartBatchOperation();

            var holeVertices = new Vertex[]
            {
                new Vertex(volume.Layer, "", new Point3D(0, 1, -1)),
                new Vertex(volume.Layer, "", new Point3D(0, 3, -1)),
                new Vertex(volume.Layer, "", new Point3D(0, 3, -3)),
                new Vertex(volume.Layer, "", new Point3D(0, 1, -3)),
            };
            var holeEdges = new Edge[]
            {
                new Edge(volume.Layer, "", new Vertex[]{ holeVertices[0], holeVertices[1] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeVertices[1], holeVertices[2] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeVertices[2], holeVertices[3] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeVertices[3], holeVertices[0] }),
            };
            var holeLoop = new EdgeLoop(volume.Layer, "", holeEdges);
            var holeFace = new Face(volume.Layer, "", holeLoop, GeometricOrientation.Forward);

            var leftWall = gm.Geometry.Faces.First(x => x.Name == "LeftWall");
            leftWall.Holes.Add(holeLoop);

            gm.Geometry.EndBatchOperation();

            var leftWallComp = volumeComp.Components.First(x => x.Component != null && x.Component.Instances.Count == 1 &&
                x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == leftWall.Id))
                .Component;
            Assert.AreEqual(1, leftWallComp.Components.Count);

            var holeComp = leftWallComp.Components.First().Component;
            Assert.AreEqual(1, holeComp.Instances.Count);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), holeComp.Instances[0].State);

            var geometryPlacement = (SimInstancePlacementGeometry)holeComp.Instances[0].Placements.First(x => x is SimInstancePlacementGeometry);
            Assert.IsNotNull(geometryPlacement);
            Assert.AreEqual(resource.Key, geometryPlacement.FileId);
            Assert.AreEqual(holeFace.Id, geometryPlacement.GeometryId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geometryPlacement.State);

            //Parameter check
            AssertUtil.AssertDoubleEqual(21.0, leftWallComp.Parameters.First(x => x.Name == "A").ValueCurrent);
        }
        [TestMethod]
        public void VolumeHoleRemovedEmpty()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            //Remove hole
            gm.Geometry.StartBatchOperation();

            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeLoop = frontFace.Holes.First(x => x.Faces.Count == 1);
            frontFace.Holes.Remove(holeLoop);

            gm.Geometry.EndBatchOperation();

            var wallComp = volumeComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id)))?.Component;

            Assert.AreEqual(1, wallComp.Components.Count);

            //Check parameters
            AssertUtil.AssertDoubleEqual(48.0, wallComp.Parameters.First(x => x.Name == "A").ValueCurrent);
        }
        [TestMethod]
        public void VolumeHoleRemovedFilled()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComp, volume);

            //Remove hole
            gm.Geometry.StartBatchOperation();

            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeLoop = frontFace.Holes.First(x => x.Faces.Count > 1);
            frontFace.Holes.Remove(holeLoop);

            holeLoop.Faces.First(x => x.Boundary == holeLoop).RemoveFromModel();

            gm.Geometry.EndBatchOperation();

            var wallComp = volumeComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id)))?.Component;

            Assert.AreEqual(1, wallComp.Components.Count);

            //Check parameters
            AssertUtil.AssertDoubleEqual(48.0, wallComp.Parameters.First(x => x.Name == "A").ValueCurrent);
        }


        [TestMethod]
        public void VolumeHoleConvertedToFace()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var face = (Face)gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volume = face.PFaces[0].Volume;
            var holeLoop = face.Holes.First(x => x.Faces.Count == 1);

            var volumeComponent = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComponent, volume);

            var faceComponent = volumeComponent.Components.SelectMany(
                c => c.Component.Instances.SelectMany(i => i.Placements.OfType<SimInstancePlacementGeometry>()))
                .First(x => x.FileId == face.ModelGeometry.Model.File.Key && x.GeometryId == face.Id).Instance.Component;

            var attrib2DSubComponents = faceComponent.Components.Where(x => x.Component.InstanceType == SimInstanceType.GeometricSurface)
                .Select(x => x.Component).ToList();
            Assert.AreEqual(2, attrib2DSubComponents.Count);

            //Add face to hole
            Face holeFace = new Face(holeLoop.Layer, "HoleFace", holeLoop, GeometricOrientation.Forward);

            //Check if edge loop component is gone and face is there
            var loopComponentExists = attrib2DSubComponents.SelectMany(
                c => c.Instances.SelectMany(i => i.Placements.OfType<SimInstancePlacementGeometry>()))
                .Any(x => x.FileId == holeLoop.ModelGeometry.Model.File.Key && x.GeometryId == holeLoop.Id);
            Assert.IsFalse(loopComponentExists);

            var holeFaceComponent = attrib2DSubComponents.SelectMany(
                c => c.Instances.SelectMany(i => i.Placements.OfType<SimInstancePlacementGeometry>()))
                .First(x => x.FileId == holeFace.ModelGeometry.Model.File.Key && x.GeometryId == holeFace.Id).Instance.Component;
            Assert.IsNotNull(holeFace.Name, holeFaceComponent.Name);
            Assert.AreEqual(SimInstanceType.GeometricSurface, holeFaceComponent.InstanceType);
        }
        [TestMethod]
        public void VolumeHoleConvertedToEdgeLoop()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var face = (Face)gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volume = face.PFaces[0].Volume;
            var holeLoop = face.Holes.First(x => x.Faces.Count == 2);
            var holeFace = holeLoop.Faces.First(x => x != face);

            var volumeComponent = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComponent, volume);

            var faceComponent = volumeComponent.Components.SelectMany(
                c => c.Component.Instances.SelectMany(i => i.Placements.OfType<SimInstancePlacementGeometry>()))
                .First(x => x.FileId == face.ModelGeometry.Model.File.Key && x.GeometryId == face.Id).Instance.Component;

            var attrib2DSubComponents = faceComponent.Components.Where(x => x.Component.InstanceType == SimInstanceType.GeometricSurface)
                .Select(x => x.Component).ToList();
            Assert.AreEqual(2, attrib2DSubComponents.Count);

            //Delete face from hole
            holeFace.RemoveFromModel();

            //Check if edge loop component is gone and face is there
            var holeFaceComponent = attrib2DSubComponents.SelectMany(
                c => c.Instances.SelectMany(i => i.Placements.OfType<SimInstancePlacementGeometry>()))
                .Any(x => x.FileId == holeFace.ModelGeometry.Model.File.Key && x.GeometryId == holeFace.Id);
            Assert.IsFalse(holeFaceComponent);

            var loopComponentExists = attrib2DSubComponents.SelectMany(
                c => c.Instances.SelectMany(i => i.Placements.OfType<SimInstancePlacementGeometry>()))
                .First(x => x.FileId == holeLoop.ModelGeometry.Model.File.Key && x.GeometryId == holeLoop.Id).Instance.Component;
            Assert.IsNotNull(holeLoop.Name, loopComponentExists.Name);
            Assert.AreEqual(SimInstanceType.GeometricSurface, loopComponentExists.InstanceType);
        }


        [TestMethod]
        public void VolumeHoleInHoleAddEmpty()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Associate
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var volumeComponent = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComponent, volume);

            //Find hole face
            var wallFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");

            var wallComponent = volumeComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == wallFace.Id).Component;
            var holeFaceComponent = wallComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == holeFace.Id).Component;

            Assert.AreEqual(0, holeFaceComponent.Components.Count);

            //Add hole to hole
            gm.Geometry.StartBatchOperation();

            Vertex[] holeholeVertices = new Vertex[]
            {
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.75, 0)),
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.75, 0)),
            };
            Edge[] holeholeEdges = new Edge[]
            {
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[0], holeholeVertices[1] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[1], holeholeVertices[2] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[2], holeholeVertices[3] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[3], holeholeVertices[0] }),
            };
            EdgeLoop holeholeLoop = new EdgeLoop(volume.Layer, "HoleHole", holeholeEdges);

            holeFace.Holes.Add(holeholeLoop);

            gm.Geometry.EndBatchOperation();

            //Check results
            var holeholeComponent = holeFaceComponent.Components.First().Component;
            Assert.AreEqual(1, holeholeComponent.Instances.Count);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), holeholeComponent.Instances[0].State);

            var geometryPlacement = (SimInstancePlacementGeometry)holeholeComponent.Instances[0].Placements.First(x => x is SimInstancePlacementGeometry);
            Assert.IsNotNull(geometryPlacement);
            Assert.AreEqual(resource.Key, geometryPlacement.FileId);
            Assert.AreEqual(holeholeLoop.Id, geometryPlacement.GeometryId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geometryPlacement.State);

            //Parameter check
            AssertUtil.AssertDoubleEqual(0.25, holeholeComponent.Parameters.First(x => x.Name == "A").ValueCurrent);
        }
        [TestMethod]
        public void VolumeHoleInHoleAddFilled()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Associate
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var volumeComponent = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComponent, volume);

            //Find hole face
            var wallFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");

            var wallComponent = volumeComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == wallFace.Id).Component;
            var holeFaceComponent = wallComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == holeFace.Id).Component;

            Assert.AreEqual(0, holeFaceComponent.Components.Count);

            //Add hole to hole
            gm.Geometry.StartBatchOperation();

            Vertex[] holeholeVertices = new Vertex[]
            {
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.75, 0)),
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.75, 0)),
            };
            Edge[] holeholeEdges = new Edge[]
            {
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[0], holeholeVertices[1] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[1], holeholeVertices[2] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[2], holeholeVertices[3] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[3], holeholeVertices[0] }),
            };
            EdgeLoop holeholeLoop = new EdgeLoop(volume.Layer, "HoleHole", holeholeEdges);
            Face holeholeFace = new Face(volume.Layer, "HoleHoleFace", holeholeLoop, GeometricOrientation.Forward);

            holeFace.Holes.Add(holeholeLoop);

            gm.Geometry.EndBatchOperation();

            //Check results
            var holeholeComponent = holeFaceComponent.Components.First().Component;
            Assert.AreEqual(1, holeholeComponent.Instances.Count);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), holeholeComponent.Instances[0].State);

            var geometryPlacement = (SimInstancePlacementGeometry)holeholeComponent.Instances[0].Placements.First(x => x is SimInstancePlacementGeometry);
            Assert.IsNotNull(geometryPlacement);
            Assert.AreEqual(resource.Key, geometryPlacement.FileId);
            Assert.AreEqual(holeholeFace.Id, geometryPlacement.GeometryId);
            Assert.AreEqual(SimInstancePlacementState.Valid, geometryPlacement.State);

            //Parameter check
            AssertUtil.AssertDoubleEqual(0.25, holeholeComponent.Parameters.First(x => x.Name == "A").ValueCurrent);
        }

        [TestMethod]
        public void VolumeHoleInHoleRemoveEmpty()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Associate
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var volumeComponent = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComponent, volume);

            //Find hole face
            var wallFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");

            var wallComponent = volumeComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == wallFace.Id).Component;
            var holeFaceComponent = wallComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == holeFace.Id).Component;

            Assert.AreEqual(0, holeFaceComponent.Components.Count);

            //Add hole to hole
            gm.Geometry.StartBatchOperation();

            Vertex[] holeholeVertices = new Vertex[]
            {
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.75, 0)),
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.75, 0)),
            };
            Edge[] holeholeEdges = new Edge[]
            {
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[0], holeholeVertices[1] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[1], holeholeVertices[2] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[2], holeholeVertices[3] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[3], holeholeVertices[0] }),
            };
            EdgeLoop holeholeLoop = new EdgeLoop(volume.Layer, "HoleHole", holeholeEdges);

            holeFace.Holes.Add(holeholeLoop);

            gm.Geometry.EndBatchOperation();

            //Check results
            holeFace.Holes.Clear();

            Assert.AreEqual(0, holeFaceComponent.Components.Count);
        }
        [TestMethod]
        public void VolumeHoleInHoleRemoveFilled()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Associate
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var volumeComponent = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComponent, volume);

            //Find hole face
            var wallFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");

            var wallComponent = volumeComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == wallFace.Id).Component;
            var holeFaceComponent = wallComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == holeFace.Id).Component;

            Assert.AreEqual(0, holeFaceComponent.Components.Count);

            //Add hole to hole
            gm.Geometry.StartBatchOperation();

            Vertex[] holeholeVertices = new Vertex[]
            {
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.75, 0)),
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.75, 0)),
            };
            Edge[] holeholeEdges = new Edge[]
            {
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[0], holeholeVertices[1] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[1], holeholeVertices[2] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[2], holeholeVertices[3] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[3], holeholeVertices[0] }),
            };
            EdgeLoop holeholeLoop = new EdgeLoop(volume.Layer, "HoleHole", holeholeEdges);
            Face holeholeFace = new Face(volume.Layer, "HoleHoleFace", holeholeLoop, GeometricOrientation.Forward);

            holeFace.Holes.Add(holeholeLoop);

            gm.Geometry.EndBatchOperation();

            //Check results
            holeFace.Holes.Clear();

            Assert.AreEqual(0, holeFaceComponent.Components.Count);
        }

        [TestMethod]
        public void VolumeHoleInHoleConvertToFace()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Associate
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var volumeComponent = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComponent, volume);

            //Find hole face
            var wallFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");

            var wallComponent = volumeComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == wallFace.Id).Component;
            var holeFaceComponent = wallComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == holeFace.Id).Component;

            Assert.AreEqual(0, holeFaceComponent.Components.Count);

            //Setup
            gm.Geometry.StartBatchOperation();

            Vertex[] holeholeVertices = new Vertex[]
            {
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.75, 0)),
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.75, 0)),
            };
            Edge[] holeholeEdges = new Edge[]
            {
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[0], holeholeVertices[1] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[1], holeholeVertices[2] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[2], holeholeVertices[3] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[3], holeholeVertices[0] }),
            };
            EdgeLoop holeholeLoop = new EdgeLoop(volume.Layer, "HoleHole", holeholeEdges);

            holeFace.Holes.Add(holeholeLoop);

            gm.Geometry.EndBatchOperation();

            //Find components
            var holeholeComponent = holeFaceComponent.Components.First().Component;
            var geometryPlacement = (SimInstancePlacementGeometry)holeholeComponent.Instances[0].Placements.First(x => x is SimInstancePlacementGeometry);
            Assert.AreEqual(1, holeholeComponent.Instances.Count);
            Assert.AreEqual(resource.Key, geometryPlacement.FileId);
            Assert.AreEqual(holeholeLoop.Id, geometryPlacement.GeometryId);

            //Operation to test
            Face holeholeFace = new Face(volume.Layer, "HoleHoleFace", holeholeLoop, GeometricOrientation.Forward);

            //Check results
            geometryPlacement = (SimInstancePlacementGeometry)holeholeComponent.Instances[0].Placements.First(x => x is SimInstancePlacementGeometry);
            Assert.AreEqual(1, holeholeComponent.Instances.Count);
            Assert.AreEqual(resource.Key, geometryPlacement.FileId);
            Assert.AreEqual(holeholeFace.Id, geometryPlacement.GeometryId);
        }
        [TestMethod]
        public void VolumeHoleInHoleConvertToEdgeLoop()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Associate
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");
            var volumeComponent = projectData.Components.First(x => x.Name == "Room1");
            projectData.ComponentGeometryExchange.Associate(volumeComponent, volume);

            //Find hole face
            var wallFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeFace = gm.Geometry.Faces.First(x => x.Name == "Window1");

            var wallComponent = volumeComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == wallFace.Id).Component;
            var holeFaceComponent = wallComponent.Components.First(x => x.Component.Instances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                gp.GeometryId == holeFace.Id).Component;

            Assert.AreEqual(0, holeFaceComponent.Components.Count);

            //Setup
            gm.Geometry.StartBatchOperation();

            Vertex[] holeholeVertices = new Vertex[]
            {
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.5, 0)),
                new Vertex(volume.Layer, "", new Point3D(3.5, 2.75, 0)),
                new Vertex(volume.Layer, "", new Point3D(2.5, 2.75, 0)),
            };
            Edge[] holeholeEdges = new Edge[]
            {
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[0], holeholeVertices[1] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[1], holeholeVertices[2] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[2], holeholeVertices[3] }),
                new Edge(volume.Layer, "", new Vertex[]{ holeholeVertices[3], holeholeVertices[0] }),
            };
            EdgeLoop holeholeLoop = new EdgeLoop(volume.Layer, "HoleHole", holeholeEdges);
            Face holeholeFace = new Face(volume.Layer, "HoleHoleFace", holeholeLoop, GeometricOrientation.Forward);

            holeFace.Holes.Add(holeholeLoop);

            gm.Geometry.EndBatchOperation();

            //Find components
            var holeholeComponent = holeFaceComponent.Components.First().Component;
            var geometryPlacement = (SimInstancePlacementGeometry)holeholeComponent.Instances[0].Placements.First(x => x is SimInstancePlacementGeometry);
            Assert.AreEqual(1, holeholeComponent.Instances.Count);
            Assert.AreEqual(resource.Key, geometryPlacement.FileId);
            Assert.AreEqual(holeholeFace.Id, geometryPlacement.GeometryId);

            //Operation to test
            holeholeFace.RemoveFromModel();

            //Check results
            geometryPlacement = (SimInstancePlacementGeometry)holeholeComponent.Instances[0].Placements.First(x => x is SimInstancePlacementGeometry);
            Assert.AreEqual(1, holeholeComponent.Instances.Count);
            Assert.AreEqual(resource.Key, geometryPlacement.FileId);
            Assert.AreEqual(holeholeLoop.Id, geometryPlacement.GeometryId);
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
            Assert.AreNotEqual(0, comp.Components.Count);

            projectData.ComponentGeometryExchange.Disassociate(comp, volume);
            Assert.AreEqual(0, comp.Components.Count);
            Assert.AreEqual(0, comp.Instances.Count);
            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.Ok), comp.InstanceState);
            Assert.AreEqual(0, comp.ReferencedComponents.Count);
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
