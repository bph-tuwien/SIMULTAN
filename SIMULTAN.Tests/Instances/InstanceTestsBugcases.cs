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
    /// <summary>
    /// Contains unit tests that should actually work but don't
    /// This has to be rechecked when the GeometryExchange gets reworked
    /// </summary>
    //[TestClass]
    public class InstanceTestsBugcases : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\GeometryInstanceTestsProject.simultan");

        [TestMethod]
        public void VolumeAddFace()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

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
        public void VolumeHoleRemovedEmpty()
        {
            LoadProject(testProject);
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

            //Remove hole
            gm.Geometry.StartBatchOperation();

            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeLoop = frontFace.Holes.First(x => x.Faces.Count == 1);
            frontFace.Holes.Remove(holeLoop);

            holeLoop.RemoveFromModel();

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
            projectData.Components.EnableAsyncUpdates = false;
            projectData.GeometryCommunicator.EnableAsyncUpdates = false;
            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Room");

            //Add new association
            var volumeComp = projectData.Components.First(x => x.Name == "Room1");
            projectData.GeometryCommunicator.Associate(volumeComp, volume);

            //Remove hole
            gm.Geometry.StartBatchOperation();

            var frontFace = gm.Geometry.Faces.First(x => x.Name == "FrontWall");
            var holeLoop = frontFace.Holes.First(x => x.Faces.Count > 1);
            frontFace.Holes.Remove(holeLoop);

            holeLoop.RemoveFromModel();
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
            Assert.Fail();
        }
        [TestMethod]
        public void VolumeHoleConvertedToEdgeLoop()
        {
            Assert.Fail();
        }
    }
}
