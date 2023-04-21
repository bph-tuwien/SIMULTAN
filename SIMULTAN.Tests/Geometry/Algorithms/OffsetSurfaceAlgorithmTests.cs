using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class OffsetSurfaceAlgorithmTests : BaseProjectTest
    {
        private static readonly FileInfo offsetProject = new FileInfo(@".\OffsetGeometryProject.simultan");

        [TestMethod]
        public void ConvertToModelCube()
        {
            LoadProject(offsetProject);
            var model = ProjectUtils.LoadGeometry("Cube.simgeo", projectData, sp);

            List<PFace> interiorFaces = model.geometryModel.Geometry.Volumes.First().Faces.ToList();
            List<OffsetFace> interiorOffsetFaces = interiorFaces.Select(x => model.geometryModel.Geometry.OffsetModel.Faces[(x.Face, x.Orientation)]).ToList();

            //Action
            var offsetModel = OffsetSurfaceAlgorithms.ConvertToModel(interiorOffsetFaces, Matrix3D.Identity);

            //Check
            Assert.AreEqual(8, offsetModel.Vertices.Count);
            Assert.AreEqual(12, offsetModel.Edges.Count);
            Assert.AreEqual(6, offsetModel.EdgeLoops.Count);
            Assert.AreEqual(6, offsetModel.Faces.Count);
            Assert.AreEqual(0, offsetModel.Volumes.Count);

            foreach (var v in offsetModel.Vertices)
                Assert.AreEqual(3, v.Edges.Count);

            foreach (var of in interiorOffsetFaces)
            {
                var matchingFace = offsetModel.Faces.FirstOrDefault(x =>
                    x.Boundary.Edges.Select(b => b.StartVertex).ToList().All(v => of.Boundary.Any(ofv => (ofv - v.Position).Length <= 0.001))
                );
                Assert.AreNotEqual(null, matchingFace);
            }
        }

        [TestMethod]
        public void ConvertToModelStep()
        {
            LoadProject(offsetProject);
            var model = ProjectUtils.LoadGeometry("FloorStep.simgeo", projectData, sp);

            List<OffsetFace> interiorOffsetFaces = model.geometryModel.Geometry.OffsetModel.Faces.Where(x => x.Key.Item2 == GeometricOrientation.Forward)
                .Select(x => x.Value).ToList();

            //Action
            var offsetModel = OffsetSurfaceAlgorithms.ConvertToModel(interiorOffsetFaces, Matrix3D.Identity);

            //Check
            Assert.AreEqual(8, offsetModel.Vertices.Count);
            Assert.AreEqual(8, offsetModel.Edges.Count);
            Assert.AreEqual(2, offsetModel.Faces.Count);
        }

        [TestMethod]
        public void ConvertToModelStepWithTransform()
        {
            LoadProject(offsetProject);
            var model = ProjectUtils.LoadGeometry("FloorStep.simgeo", projectData, sp);

            List<OffsetFace> interiorOffsetFaces = model.geometryModel.Geometry.OffsetModel.Faces.Where(x => x.Key.Item2 == GeometricOrientation.Forward)
                .Select(x => x.Value).ToList();

            //Action
            Matrix3D transform = Matrix3D.Identity;
            transform.Scale(new Vector3D(1, 0, 1));
            var offsetModel = OffsetSurfaceAlgorithms.ConvertToModel(interiorOffsetFaces, transform);

            //Check
            Assert.AreEqual(6, offsetModel.Vertices.Count);
            Assert.AreEqual(7, offsetModel.Edges.Count);
            Assert.AreEqual(2, offsetModel.Faces.Count);
        }
    }
}
