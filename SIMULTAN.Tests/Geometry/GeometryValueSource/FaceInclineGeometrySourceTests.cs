using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.IO;
using System.Linq;


namespace SIMULTAN.Tests.Geometry.GeometryValueSource
{
    [TestClass]
    public class FaceInclineGeometrySourceTests : BaseProjectTest
    {
        private static readonly FileInfo geometrySourceProject = new FileInfo(@"./GeometryValueSourceTests.simultan");

        #region Attach/Open

        [TestMethod]
        public void AddSource()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            Assert.IsTrue(double.IsNegativeInfinity(param.Value));

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            Assert.IsTrue(double.IsNegativeInfinity(param.Value));
            Assert.IsTrue(double.IsNegativeInfinity((double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]));

            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
        }
        [TestMethod]
        public void OpenGeometry()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            AssertUtil.AssertDoubleEqual(90.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void OpenedGeometry()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            AssertUtil.AssertDoubleEqual(90.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void OpenedGeometryMultiple()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var rightFace = gm.Geometry.Faces.First(x => x.Name == "Right Face");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesFace, gm.File.Key, rightFace.Id));

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change geometry

        [TestMethod]
        public void GeometryChanged()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 10, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(0, 10, 0);

            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(45.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void BatchGeometryChanged()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            gm.Geometry.StartBatchOperation();
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 10, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(0, 10, 0);
            gm.Geometry.EndBatchOperation();

            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(45.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void FaceRemoved()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var face = gm.Geometry.Faces.FirstOrDefault(x => x.Name == "Top Face");

            face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
            face.RemoveFromModel();

            //Check
            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void FaceMissingAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            var newInstance = new SimComponentInstance(SimInstanceType.AttributesFace, 0, 9999);
            faceComp.Instances.Add(newInstance);
            newInstance.InstanceParameterValuesPersistent[param] = 90.0;

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            //Add face
            var faceBoundary = new EdgeLoop(gm.Geometry.Layers.First(), "NewFace Boundary",
                gm.Geometry.Edges.Where(x => x.Name.StartsWith("NewFaceEdge")));
            var face = new Face(9999, faceBoundary.Layer, "NewFace", faceBoundary);

            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);

        }
        [TestMethod]
        public void FaceReadd()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var face = gm.Geometry.Faces.FirstOrDefault(x => x.Name == "Top Face");

            face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
            face.RemoveFromModel();

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            face.AddToModel();

            //Check
            AssertUtil.AssertDoubleEqual(90.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void GeometryReplaced()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            copyGeometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 10, 0);
            copyGeometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(0, 10, 0);

            AssertUtil.AssertDoubleEqual(90.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(45.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedFaceRemoved()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            var face = copyGeometry.Faces.FirstOrDefault(x => x.Name == "Top Face");

            face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
            face.RemoveFromModel();

            AssertUtil.AssertDoubleEqual(90.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedMissingFaceAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

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

            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedFaceReAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            var face = copyGeometry.Faces.FirstOrDefault(x => x.Name == "Top Face");

            face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
            face.RemoveFromModel();
            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);

            face.AddToModel();

            AssertUtil.AssertDoubleEqual(90.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change instances/placements

        [TestMethod]
        public void AddInstance()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add instance
            var rightFace = gm.Geometry.Faces.First(x => x.Name == "Right Face");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesFace, gm.File.Key, rightFace.Id));

            //Check
            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void RemoveInstance()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add additional instance, cause otherwise testing doesn't make sense
            var newInst = new SimComponentInstance(SimInstanceType.AttributesFace, 99, 9999);
            faceComp.Instances.Add(newInst);
            newInst.InstanceParameterValuesPersistent[param] = 45.0;

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Remove instance
            faceComp.Instances.RemoveAt(0);

            //Check
            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(45.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void RemovePlacement()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add additional instance, cause otherwise testing doesn't make sense
            var newInst = new SimComponentInstance(SimInstanceType.AttributesFace, 99, 9999);
            faceComp.Instances.Add(newInst);
            newInst.InstanceParameterValuesPersistent[param] = 45.0;

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Remove instance
            faceComp.Instances[0].Placements.RemoveAt(0);

            //Check
            AssertUtil.AssertDoubleEqual(45.0, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(45.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change parameter/component

        [TestMethod]
        public void ParameterAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var param = new SimDoubleParameter("New Param", "", -1.0);
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            faceComp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(90.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void ComponentAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Face 2");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var newComp = new SimComponent()
            {
                InstanceType = SimInstanceType.AttributesFace,
            };
            newComp.Slots.Add(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined)));
            var param = new SimDoubleParameter("New Param", "", -1.0);
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            newComp.Parameters.Add(param);

            var rightFace = gm.Geometry.Faces.First(x => x.Name == "Top Face");
            newComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesFace, gm.File.Key, rightFace.Id));

            faceComp.Components.Add(new SimChildComponentEntry(new SimSlot(new SimTaxonomyEntryReference(newComp.Slots[0]), ""), newComp));

            AssertUtil.AssertDoubleEqual(90.0, param.Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)newComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Memory Leaks

        private WeakReference MemoryLeakTestSourceRemoved_Action()
        {
            var comp = projectData.Components.First(x => x.Name == "Face 2");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

            WeakReference ptrRef = new WeakReference(param.ValueSource);
            param.ValueSource = null;

            return ptrRef;
        }
        [TestMethod]
        public void MemoryLeakTestSourceRemoved()
        {
            LoadProject(geometrySourceProject);

            var ptrRef = MemoryLeakTestSourceRemoved_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        private WeakReference MemoryLeakTestParameterRemoved_Action()
        {
            var comp = projectData.Components.First(x => x.Name == "Face 2");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);

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

        #region Filter

        private (SimTaxonomyEntry geo1, SimTaxonomyEntry geo2, SimTaxonomyEntry both) GetFilterTags()
        {
            var tax = projectData.Taxonomies.GetTaxonomyByKeyOrName("filter");
            var geo1 = tax.GetTaxonomyEntryByKey("geo1");
            var geo2 = tax.GetTaxonomyEntryByKey("geo2");
            var both = tax.GetTaxonomyEntryByKey("both");
            return (geo1, geo2, both);
        }

        [TestMethod]
        public void FilterGeometry_Attach()
        {
            LoadProject(geometrySourceProject);
            var tags = GetFilterTags();

            var faceComp = this.projectData.Components.First(x => x.Name == "Face Filter");
            var param = faceComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry_2.simgeo", projectData, sp);

            //Add parameter source
            var source = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);
            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo1));
            ((SimDoubleParameter)param).ValueSource = source;

            AssertUtil.AssertDoubleEqual(90.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
        }

        [TestMethod]
        public void FilterGeometry_SourceTagsChanged()
        {
            LoadProject(geometrySourceProject);
            var tags = GetFilterTags();

            var faceComp = this.projectData.Components.First(x => x.Name == "Face Filter");
            var param = faceComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry_2.simgeo", projectData, sp);

            //Add parameter source
            var source = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);
            ((SimDoubleParameter)param).ValueSource = source;

            AssertUtil.AssertDoubleEqual(76.717474411461012, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(63.43494882292201, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);

            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo1));

            AssertUtil.AssertDoubleEqual(90.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));

            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo2));

            AssertUtil.AssertDoubleEqual(0.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));

            source.FilterTags.RemoveWhere(x => x.Target == tags.geo1);
            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.both));

            AssertUtil.AssertDoubleEqual(63.43494882292201, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(63.43494882292201, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void FilterGeometry_ResourceTagsChanged()
        {
            LoadProject(geometrySourceProject);
            var tags = GetFilterTags();

            var faceComp = this.projectData.Components.First(x => x.Name == "Face Filter");
            var param = faceComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry_2.simgeo", projectData, sp);

            //Add parameter source
            var source = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);
            ((SimDoubleParameter)param).ValueSource = source;
            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo1));

            AssertUtil.AssertDoubleEqual(90.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));

            resource.Tags.RemoveAt(0);

            AssertUtil.AssertDoubleEqual(0.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));

            resource2.Tags.Add(new SimTaxonomyEntryReference(tags.geo1));

            AssertUtil.AssertDoubleEqual(63.43494882292201, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(63.43494882292201, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void FilterGeometry_NewInstance()
        {
            LoadProject(geometrySourceProject);
            var tags = GetFilterTags();

            var faceComp = this.projectData.Components.First(x => x.Name == "Face Filter");
            var param = faceComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry_2.simgeo", projectData, sp);
            (var gm3, var resource3) = ProjectUtils.LoadGeometry("Geometry_3.simgeo", projectData, sp);

            //Add parameter source
            var source = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceIncline);
            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo1));
            ((SimDoubleParameter)param).ValueSource = source;

            AssertUtil.AssertDoubleEqual(90.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));

            //Add instance
            var bottomFace = gm3.Geometry.Faces.First(x => x.Name == "Face 20 (Copy)");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesFace, gm3.File.Key, bottomFace.Id));

            AssertUtil.AssertDoubleEqual(2.8552965687497789, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(90.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(-84.289406862500442, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
        }

        #endregion
        //TODO: Unittests for multiple geometry placements (can be done after multigeometry is implemented)
    }
}
