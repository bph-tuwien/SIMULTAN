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
    public class EdgeLengthGeometrySourceTests : BaseProjectTest
    {
        private static readonly FileInfo geometrySourceProject = new FileInfo(@"./GeometryValueSourceTests.simultan");

        #region Attach/Open

        [TestMethod]
        public void AddSource()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            Assert.IsTrue(double.IsNegativeInfinity(param.Value));

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            Assert.IsTrue(double.IsNegativeInfinity(param.Value));
            Assert.IsTrue(double.IsNegativeInfinity((double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            Assert.IsTrue(double.IsNegativeInfinity((double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]));

            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
        }
        [TestMethod]
        public void OpenGeometry()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            AssertUtil.AssertDoubleEqual(30.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void OpenedGeometry()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            AssertUtil.AssertDoubleEqual(30.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change geometry

        [TestMethod]
        public void GeometryChanged()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 0, -5);

            AssertUtil.AssertDoubleEqual(35.0, param.Value);
            AssertUtil.AssertDoubleEqual(25.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void BatchGeometryChanged()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            gm.Geometry.StartBatchOperation();
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 0, -5);
            gm.Geometry.EndBatchOperation();

            AssertUtil.AssertDoubleEqual(35.0, param.Value);
            AssertUtil.AssertDoubleEqual(25.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void FaceRemoved()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var edge = gm.Geometry.Edges.FirstOrDefault(x => x.Name == "Edge 1");

            foreach (var pedge in edge.PEdges.ToList())
            {
                if (pedge.Parent is EdgeLoop el)
                {
                    foreach (var face in el.Faces.ToList())
                    {
                        face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
                        face.RemoveFromModel();
                    }
                }
            }
            edge.RemoveFromModel();

            //Check
            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void FaceMissingAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            var newInstance = new SimComponentInstance(SimInstanceType.AttributesEdge, 0, 9999);
            faceComp.Instances.Add(newInstance);
            newInstance.InstanceParameterValuesPersistent[param] = 0.0;

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            //Add edge
            var v1 = gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackRightTop");
            var v2 = gm.Geometry.Vertices.First(x => x.Name == "Vertex - B3 - FrontRightTop");
            var newEdge = new Edge(9999, v1.Layer, "", new Vertex[] { v1, v2 });

            AssertUtil.AssertDoubleEqual(60.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(30.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);

        }
        [TestMethod]
        public void FaceReadd()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var edge = gm.Geometry.Edges.FirstOrDefault(x => x.Name == "Edge 1");

            foreach (var pedge in edge.PEdges.ToList())
            {
                if (pedge.Parent is EdgeLoop el)
                {
                    foreach (var face in el.Faces.ToList())
                    {
                        face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
                        face.RemoveFromModel();
                    }
                }
            }
            edge.RemoveFromModel();

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            edge.AddToModel();

            //Check
            AssertUtil.AssertDoubleEqual(30.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void GeometryReplaced()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            copyGeometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 0, -5);

            AssertUtil.AssertDoubleEqual(30.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(35.0, param.Value);
            AssertUtil.AssertDoubleEqual(25.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedFaceRemoved()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            var edge = copyGeometry.Edges.FirstOrDefault(x => x.Name == "Edge 1");

            foreach (var pedge in edge.PEdges.ToList())
            {
                if (pedge.Parent is EdgeLoop el)
                {
                    foreach (var face in el.Faces.ToList())
                    {
                        face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
                        face.RemoveFromModel();
                    }
                }
            }
            edge.RemoveFromModel();

            AssertUtil.AssertDoubleEqual(30.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedMissingFaceAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            var newInstance = new SimComponentInstance(SimInstanceType.AttributesEdge, 0, 9999);
            faceComp.Instances.Add(newInstance);
            newInstance.InstanceParameterValuesPersistent[param] = 0.0;

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();

            //Add edge
            var v1 = copyGeometry.Vertices.First(x => x.Name == "Vertex - BackRightTop");
            var v2 = copyGeometry.Vertices.First(x => x.Name == "Vertex - B3 - FrontRightTop");
            var newEdge = new Edge(9999, v1.Layer, "", new Vertex[] { v1, v2 });

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(60.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(30.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedFaceReAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            var edge = copyGeometry.Edges.FirstOrDefault(x => x.Name == "Edge 1");

            foreach (var pedge in edge.PEdges.ToList())
            {
                if (pedge.Parent is EdgeLoop el)
                {
                    foreach (var face in el.Faces.ToList())
                    {
                        face.PFaces.ToList().ForEach(x => x.Volume.RemoveFromModel());
                        face.RemoveFromModel();
                    }
                }
            }
            edge.RemoveFromModel();
            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);

            edge.AddToModel();

            AssertUtil.AssertDoubleEqual(30.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change instances/placements

        [TestMethod]
        public void AddInstance()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add instance
            var rightFace = gm.Geometry.Edges.First(x => x.Name == "Edge 2");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesEdge, gm.File.Key, rightFace.Id));

            //Check
            AssertUtil.AssertDoubleEqual(50.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void RemoveInstance()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Remove instance
            faceComp.Instances.RemoveAt(0);

            //Check
            AssertUtil.AssertDoubleEqual(10.0, param.Value);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void RemovePlacement()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Remove instance
            faceComp.Instances[1].Placements.RemoveAt(0);

            //Check
            AssertUtil.AssertDoubleEqual(20.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change parameter/component

        [TestMethod]
        public void ParameterAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var param = new SimDoubleParameter("New Param", "", -1.0);
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            faceComp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(30.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void ComponentAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var newComp = new SimComponent()
            {
                InstanceType = SimInstanceType.AttributesEdge,
            };
            newComp.Slots.Add(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined)));
            var param = new SimDoubleParameter("New Param", "", -1.0);
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            newComp.Parameters.Add(param);

            var edge2 = gm.Geometry.Edges.First(x => x.Name == "Edge 2");
            newComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesEdge, gm.File.Key, edge2.Id));

            faceComp.Components.Add(new SimChildComponentEntry(new SimSlot(new SimTaxonomyEntryReference(newComp.Slots[0]), ""), newComp));

            AssertUtil.AssertDoubleEqual(20.0, param.Value);
            AssertUtil.AssertDoubleEqual(20.0, (double)newComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Memory Leak Checks

        private static WeakReference MemoryLeakTestSourceRemoved_Action(SimDoubleParameter param)
        {
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

            WeakReference ptrRef = new WeakReference(param.ValueSource);
            return ptrRef;
        }
        [TestMethod]
        public void MemoryLeakTestSourceRemoved()
        {
            LoadProject(geometrySourceProject);

            var comp = projectData.Components.First(x => x.Name == "Edge");
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
            var comp = projectData.Components.First(x => x.Name == "Edge");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);

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

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge Filter");
            var param = faceComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry_2.simgeo", projectData, sp);

            //Add parameter source
            var source = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);
            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo1));
            ((SimDoubleParameter)param).ValueSource = source;

            AssertUtil.AssertDoubleEqual(30.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[3].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void FilterGeometry_SourceTagsChanged()
        {
            LoadProject(geometrySourceProject);
            var tags = GetFilterTags();

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge Filter");
            var param = faceComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry_2.simgeo", projectData, sp);

            //Add parameter source
            var source = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);
            ((SimDoubleParameter)param).ValueSource = source;

            AssertUtil.AssertDoubleEqual(90.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(40.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[3].InstanceParameterValuesPersistent[param]);

            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo1));

            AssertUtil.AssertDoubleEqual(30.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[3].InstanceParameterValuesPersistent[param]);

            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo2));

            AssertUtil.AssertDoubleEqual(0.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[2].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[3].InstanceParameterValuesPersistent[param]));

            source.FilterTags.RemoveWhere(x => x.Target == tags.geo1);
            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.both));

            AssertUtil.AssertDoubleEqual(60.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(40.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[2].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[3].InstanceParameterValuesPersistent[param]));
        }

        [TestMethod]
        public void FilterGeometry_ResourceTagsChanged()
        {
            LoadProject(geometrySourceProject);
            var tags = GetFilterTags();

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge Filter");
            var param = faceComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry_2.simgeo", projectData, sp);

            //Add parameter source
            var source = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);
            ((SimDoubleParameter)param).ValueSource = source;
            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo1));

            AssertUtil.AssertDoubleEqual(30.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[3].InstanceParameterValuesPersistent[param]);

            resource.Tags.RemoveAt(0);

            AssertUtil.AssertDoubleEqual(0.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[2].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[3].InstanceParameterValuesPersistent[param]));

            resource2.Tags.Add(new SimTaxonomyEntryReference(tags.geo1));

            AssertUtil.AssertDoubleEqual(60.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(40.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[2].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[3].InstanceParameterValuesPersistent[param]));
        }

        [TestMethod]
        public void FilterGeometry_NewInstance()
        {
            LoadProject(geometrySourceProject);
            var tags = GetFilterTags();

            var faceComp = this.projectData.Components.First(x => x.Name == "Edge Filter");
            var param = faceComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry_2.simgeo", projectData, sp);
            (var gm3, var resource3) = ProjectUtils.LoadGeometry("Geometry_3.simgeo", projectData, sp);

            //Add parameter source
            var source = new SimGeometryParameterSource(SimGeometrySourceProperty.EdgeLength);
            source.FilterTags.Add(new SimTaxonomyEntryReference(tags.geo1));
            ((SimDoubleParameter)param).ValueSource = source;

            AssertUtil.AssertDoubleEqual(30.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[3].InstanceParameterValuesPersistent[param]);

            //Add instance
            var rightFace = gm3.Geometry.Edges.First(x => x.Name == "Edge 2");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesEdge, gm3.File.Key, rightFace.Id));

            AssertUtil.AssertDoubleEqual(110.0, ((SimDoubleParameter)param).Value);
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[0].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(0.0, Convert.ToDouble(faceComp.Instances[1].InstanceParameterValuesPersistent[param]));
            AssertUtil.AssertDoubleEqual(20.0, (double)faceComp.Instances[2].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(10.0, (double)faceComp.Instances[3].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(80.0, (double)faceComp.Instances[4].InstanceParameterValuesPersistent[param]);
        }

        #endregion
    }
}
