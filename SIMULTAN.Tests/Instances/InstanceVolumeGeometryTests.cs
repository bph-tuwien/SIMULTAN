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
            projectData.Components.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.GeometryCommunicator.Associate(comp, volume);

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
        public void AddVolumeInstanceSubComponents()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            Assert.AreEqual(0, comp.Components.Count);

            //Add new association
            projectData.GeometryCommunicator.Associate(comp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.GeometryCommunicator.Associate(floorComp, floorFace);


            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

            var wallComp = volumeComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;

            CheckParameter(wallComp, "A", 46.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "AᴍᴀX", 48.52, "-", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "AᴍɪN", 43.04, "-", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "b", 10.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "BᴍᴀX", 10.1, "-", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "BᴍɪN", 9.8, "-", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "h", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "HᴍᴀX", 5.2, "-", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "HᴍɪN", 4.8, "-", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "Kᴅᴀ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "Kꜰᴀ", 0.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
        }

        [TestMethod]
        public void AddVolumeInstanceSubHoleParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.GeometryCommunicator.Associate(floorComp, floorFace);


            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

            var wallComp = volumeComp.Components.First(c => c.Component != null && c.Component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == resource.Key && pg.GeometryId == frontFace.Id))).Component;

            //Hole with face
            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Window1"))?.Component;

                CheckParameter(holeComp, "A", 2.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍᴀX", 2.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍɪN", 2.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "b", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍᴀX", 2.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍɪN", 2.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "h", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍᴀX", 1.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍɪN", 1.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "Kᴅᴀ", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "Kꜰᴀ", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            }

            //Empty hole
            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Edge Loop")).Component;

                CheckParameter(holeComp, "A", 2.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍᴀX", 2.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍɪN", 2.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "b", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍᴀX", 2.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍɪN", 2.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "h", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍᴀX", 1.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍɪN", 1.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "Kᴅᴀ", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "Kꜰᴀ", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            }
        }

        [TestMethod]
        public void AddVolumeInstanceSubVolumeParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.GeometryCommunicator.Associate(floorComp, floorFace);

            //Add new association
            projectData.GeometryCommunicator.Associate(roomComp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(roomComp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(roomComp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(roomComp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var roomComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(roomComp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var room2Comp = projectData.Components.First(x => x.Name == "Room2");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(room1Comp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(room1Comp, volume);

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
        public void VolumeChangedSubVolumeParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.GeometryCommunicator.Associate(floorComp, floorFace);

            //Add new association
            projectData.GeometryCommunicator.Associate(room1Comp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.GeometryCommunicator.Associate(floorComp, floorFace);

            //Add new association
            projectData.GeometryCommunicator.Associate(room1Comp, volume);

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
            CheckParameter(wallComp, "AᴍᴀX", 71.02, "-", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "AᴍɪN", 64.44, "-", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "b", 12.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "BᴍᴀX", 12.1, "-", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "BᴍɪN", 11.8, "-", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "h", 6.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "HᴍᴀX", 6.2, "-", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "HᴍɪN", 5.8, "-", SimInfoFlow.Input, SimCategory.Geometry);

            CheckParameter(wallComp, "Kᴅᴀ", 5.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            CheckParameter(wallComp, "Kꜰᴀ", -1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
        }
        [TestMethod]
        public void VolumeChangedSubHoleParameters()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.GeometryCommunicator.Associate(floorComp, floorFace);


            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

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
                CheckParameter(holeComp, "AᴍᴀX", 3.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍɪN", 3.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "b", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍᴀX", 3.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍɪN", 3.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "h", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍᴀX", 1.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍɪN", 1.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "Kᴅᴀ", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "Kꜰᴀ", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            }

            //Empty hole
            {
                var holeComp = wallComp.Components.First(x => x.Component != null && x.Component.Name.StartsWith("Edge Loop")).Component;

                CheckParameter(holeComp, "A", 3.0, "m²", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍᴀX", 3.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "AᴍɪN", 3.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "b", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍᴀX", 3.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "BᴍɪN", 3.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "h", 1.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍᴀX", 1.0, "-", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "HᴍɪN", 1.0, "-", SimInfoFlow.Input, SimCategory.Geometry);

                CheckParameter(holeComp, "Kᴅᴀ", 3.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
                CheckParameter(holeComp, "Kꜰᴀ", 2.0, "m", SimInfoFlow.Input, SimCategory.Geometry);
            }
        }
        [TestMethod]
        public void VolumeChangedFacePath()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var room1Comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.GeometryCommunicator.Associate(floorComp, floorFace);

            //Add new association
            projectData.GeometryCommunicator.Associate(room1Comp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add floor to get different min/max heights
            var floorFace = gm.Geometry.Faces.First(x => x.Name == "Floor");
            var floorComp = projectData.Components.First(x => x.Name == "Floor");
            projectData.GeometryCommunicator.Associate(floorComp, floorFace);


            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

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

        [TestMethod]
        public void VolumeHoleAddedEmpty()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

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

        #endregion

        #region RemoveInstance

        [TestMethod]
        public void RemoveVolumeInstance()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var comp = projectData.Components.First(x => x.Name == "Room1");
            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            Assert.AreEqual(0, comp.Instances.Count);

            //Add new association
            projectData.GeometryCommunicator.Associate(comp, volume);
            Assert.AreNotEqual(0, comp.Components.Count);

            projectData.GeometryCommunicator.DisAssociate(comp, volume);
            Assert.AreEqual(0, comp.Components.Count);
            Assert.AreEqual(0, comp.Instances.Count);
            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.Ok), comp.InstanceState);
            Assert.AreEqual(0, comp.ReferencedComponents.Count);

        }

        #endregion
    }
}
