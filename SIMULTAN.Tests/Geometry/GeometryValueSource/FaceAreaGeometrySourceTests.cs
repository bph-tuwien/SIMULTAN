using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;


namespace SIMULTAN.Tests.Geometry.GeometryValueSource
{
    [TestClass]
    public class FaceAreaGeometrySourceTests : BaseProjectTest
    {
        private static readonly FileInfo geometrySourceProject = new FileInfo(@"./GeometryValueSourceTests.simultan");

        #region Attach/Open

        [TestMethod]
        public void AddSource()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            Assert.IsTrue(double.IsNegativeInfinity(param.Value));

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            Assert.IsTrue(double.IsNegativeInfinity(param.Value));
            Assert.IsTrue(double.IsNegativeInfinity((double)(double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            Assert.IsTrue(double.IsNegativeInfinity((double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]));

            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
        }
        [TestMethod]
        public void OpenGeometry()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            AssertUtil.AssertDoubleEqual(164.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void OpenedGeometry()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            AssertUtil.AssertDoubleEqual(164.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change geometry

        [TestMethod]
        public void GeometryChanged()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontRightBottom").Position += new SimVector3D(10, 0, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(10, 0, 0);

            AssertUtil.AssertDoubleEqual(264.0, param.Value);
            AssertUtil.AssertDoubleEqual(164.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void BatchGeometryChanged()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            gm.Geometry.StartBatchOperation();
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontRightBottom").Position += new SimVector3D(10, 0, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(10, 0, 0);
            gm.Geometry.EndBatchOperation();

            AssertUtil.AssertDoubleEqual(264.0, param.Value);
            AssertUtil.AssertDoubleEqual(164.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void FaceRemoved()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var face = gm.Geometry.Faces.FirstOrDefault(x => x.Name == "Front Face");

            face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
            face.RemoveFromModel();

            //Check
            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void FaceMissingAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            var newInstance = new SimComponentInstance(SimInstanceType.AttributesFace, 0, 9999);
            faceComp.Instances.Add(newInstance);
            newInstance.InstanceParameterValuesPersistent[param] = 0.0;

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            //Add face
            var faceBoundary = new EdgeLoop(gm.Geometry.Layers.First(), "NewFace Boundary",
                gm.Geometry.Edges.Where(x => x.Name.StartsWith("NewFaceEdge")));
            var face = new Face(9999, faceBoundary.Layer, "NewFace", faceBoundary);

            AssertUtil.AssertDoubleEqual(264.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);

        }
        [TestMethod]
        public void FaceReadd()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var face = gm.Geometry.Faces.FirstOrDefault(x => x.Name == "Front Face");

            face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
            face.RemoveFromModel();

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            face.AddToModel();

            //Check
            AssertUtil.AssertDoubleEqual(164.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void GeometryReplaced()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            copyGeometry.Vertices.First(x => x.Name == "Vertex - FrontRightBottom").Position += new SimVector3D(10, 0, 0);
            copyGeometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(10, 0, 0);

            AssertUtil.AssertDoubleEqual(164.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(264.0, param.Value);
            AssertUtil.AssertDoubleEqual(164.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedFaceRemoved()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            var face = copyGeometry.Faces.FirstOrDefault(x => x.Name == "Front Face");

            face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
            face.RemoveFromModel();

            AssertUtil.AssertDoubleEqual(164.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedMissingFaceAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            var newInstance = new SimComponentInstance(SimInstanceType.AttributesFace, 0, 9999);
            faceComp.Instances.Add(newInstance);
            newInstance.InstanceParameterValuesPersistent[param] = 0.0;

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();

            //Add face
            var faceBoundary = new EdgeLoop(copyGeometry.Layers.First(), "NewFace Boundary",
                gm.Geometry.Edges.Where(x => x.Name.StartsWith("NewFaceEdge")));
            var face = new Face(9999, faceBoundary.Layer, "NewFace", faceBoundary);

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(264.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedFaceReAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            var face = copyGeometry.Faces.FirstOrDefault(x => x.Name == "Front Face");

            face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
            face.RemoveFromModel();
            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);

            face.AddToModel();

            AssertUtil.AssertDoubleEqual(164.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change instances/placements

        [TestMethod]
        public void AddInstance()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add instance
            var rightFace = gm.Geometry.Faces.First(x => x.Name == "Right Face");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesFace, gm.File.Key, rightFace.Id));

            //Check
            AssertUtil.AssertDoubleEqual(364.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(200.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void RemoveInstance()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Remove instance
            faceComp.Instances.RemoveAt(1);

            //Check
            AssertUtil.AssertDoubleEqual(64.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void RemovePlacement()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Remove instance
            faceComp.Instances[1].Placements.RemoveAt(0);

            //Check
            AssertUtil.AssertDoubleEqual(64.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change parameter/component

        [TestMethod]
        public void ParameterAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var param = new SimDoubleParameter("New Param", "", -1.0);
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            faceComp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(164.0, param.Value);
            AssertUtil.AssertDoubleEqual(64.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(100.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void ComponentAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var newComp = new SimComponent()
            {
                InstanceType = SimInstanceType.AttributesFace,
            };
            newComp.Slots.Add(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined)));
            var param = new SimDoubleParameter("New Param", "", -1.0);
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            newComp.Parameters.Add(param);

            var rightFace = gm.Geometry.Faces.First(x => x.Name == "Right Face");
            newComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesFace, gm.File.Key, rightFace.Id));

            faceComp.Components.Add(new SimChildComponentEntry(new SimSlot(new SimTaxonomyEntryReference(newComp.Slots[0]), ""), newComp));

            AssertUtil.AssertDoubleEqual(200.0, param.Value);
            AssertUtil.AssertDoubleEqual(200.0, (double)newComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Memory Leak Checks

        private static WeakReference MemoryLeakTestSourceRemoved_Action(SimDoubleParameter param)
        {
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            WeakReference ptrRef = new WeakReference(param.ValueSource);
            return ptrRef;
        }
        [TestMethod]
        public void MemoryLeakTestSourceRemoved()
        {
            LoadProject(geometrySourceProject);

            var comp = projectData.Components.First(x => x.Name == "Face");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            var ptrRef = MemoryLeakTestSourceRemoved_Action(param);

            param.ValueSource = null;
            param = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        private WeakReference MemoryLeakTestParameterRemoved_Action()
        {
            var comp = projectData.Components.First(x => x.Name == "Face");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);

            WeakReference ptrRef = new WeakReference(param.ValueSource);
            comp.Parameters.Remove(param);

            return ptrRef;
        }
        [TestMethod]
        public void MemoryLeakTestParameterRemoved()
        {
            LoadProject(geometrySourceProject);

            var ptrRef = MemoryLeakTestParameterRemoved_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        #endregion

        //TODO: Unittests for multiple geometry placements (can be done after multigeometry is implemented)
    }
}
