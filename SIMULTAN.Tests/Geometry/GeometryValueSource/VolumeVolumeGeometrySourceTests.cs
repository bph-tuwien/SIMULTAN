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
    public class VolumeVolumeGeometrySourceTests : BaseProjectTest
    {
        private static readonly FileInfo geometrySourceProject = new FileInfo(@"./GeometryValueSourceTests.simultan");

        #region Attach/Open

        [TestMethod]
        public void AddSource()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            Assert.IsTrue(double.IsNegativeInfinity(param.Value));

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            Assert.IsTrue(double.IsNegativeInfinity(param.Value));
            Assert.IsTrue(double.IsNegativeInfinity((double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]));

            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
        }
        [TestMethod]
        public void OpenGeometry()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void OpenedGeometry()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change geometry

        [TestMethod]
        public void GeometryChanged()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontLeftTop").Position += new SimVector3D(0, 5, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(0, 5, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackLeftTop").Position += new SimVector3D(0, 5, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 5, 0);

            AssertUtil.AssertDoubleEqual(1000.0, param.Value);
            AssertUtil.AssertDoubleEqual(1000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void BatchGeometryChanged()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            gm.Geometry.StartBatchOperation();
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontLeftTop").Position += new SimVector3D(0, 5, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(0, 5, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackLeftTop").Position += new SimVector3D(0, 5, 0);
            gm.Geometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 5, 0);
            gm.Geometry.EndBatchOperation();

            AssertUtil.AssertDoubleEqual(1000.0, param.Value);
            AssertUtil.AssertDoubleEqual(1000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void VolumeRemoved()
        {
            LoadProject(geometrySourceProject);

            var volComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = volComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var volume = gm.Geometry.Volumes.FirstOrDefault(x => x.Name == "Upper Volume");

            volume.RemoveFromModel();

            //Check
            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)volComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void VolumeMissingAdded()
        {
            LoadProject(geometrySourceProject);

            var volComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = volComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            //Change instance to an not existing one
            ((SimInstancePlacementGeometry)volComp.Instances[0].Placements[0]).GeometryId = 9999;

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);
            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            //Add volume
            Volume v = new Volume(9999, gm.Geometry.Layers.First(), "",
                gm.Geometry.Faces.Where(x => x.Name.StartsWith("NewVolumeFace")));

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)volComp.Instances[0].InstanceParameterValuesPersistent[param]);

        }
        [TestMethod]
        public void VolumeReadd()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var vol = gm.Geometry.Volumes.First(x => x.Name == "Upper Volume");

            vol.RemoveFromModel();

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            vol.AddToModel();

            //Check
            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        [TestMethod]
        public void GeometryReplaced()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            copyGeometry.Vertices.First(x => x.Name == "Vertex - FrontLeftTop").Position += new SimVector3D(0, 5, 0);
            copyGeometry.Vertices.First(x => x.Name == "Vertex - FrontRightTop").Position += new SimVector3D(0, 5, 0);
            copyGeometry.Vertices.First(x => x.Name == "Vertex - BackLeftTop").Position += new SimVector3D(0, 5, 0);
            copyGeometry.Vertices.First(x => x.Name == "Vertex - BackRightTop").Position += new SimVector3D(0, 5, 0);

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(1000.0, param.Value);
            AssertUtil.AssertDoubleEqual(1000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedFaceRemoved()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            var vol = copyGeometry.Volumes.First(x => x.Name == "Upper Volume");

            vol.RemoveFromModel();

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedMissingFaceAdded()
        {
            LoadProject(geometrySourceProject);

            var volComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = volComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            //Modify instance to point to missing
            ((SimInstancePlacementGeometry)volComp.Instances[0].Placements[0]).GeometryId = 9999;

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();

            //Add face
            Volume v = new Volume(9999, copyGeometry.Layers.First(), "",
                copyGeometry.Faces.Where(x => x.Name.StartsWith("NewVolumeFace")));

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);

            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)volComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void GeometryReplacedFaceReAdded()
        {
            LoadProject(geometrySourceProject);

            var volComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = volComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var copyGeometry = gm.Geometry.Clone();
            var volume = copyGeometry.Volumes.FirstOrDefault(x => x.Name == "Upper Volume");

            volume.RemoveFromModel();
            gm.Geometry = copyGeometry;

            AssertUtil.AssertDoubleEqual(double.NaN, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)volComp.Instances[0].InstanceParameterValuesPersistent[param]);

            volume.AddToModel();

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)volComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change instances/placements

        [TestMethod]
        public void AddInstance()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add instance
            var bottomVolume = gm.Geometry.Volumes.First(x => x.Name == "Bottom Volume");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.Entity3D, gm.File.Key, bottomVolume.Id));

            //Check
            AssertUtil.AssertDoubleEqual(4000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void RemoveInstance()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add second instance
            var bottomVolume = gm.Geometry.Volumes.First(x => x.Name == "Bottom Volume");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.Entity3D, gm.File.Key, bottomVolume.Id));

            AssertUtil.AssertDoubleEqual(4000.0, param.Value);

            //Remove instance
            faceComp.Instances.RemoveAt(0);

            //Check
            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void RemovePlacement()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");
            var param = faceComp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            //Add second instance
            var bottomVolume = gm.Geometry.Volumes.First(x => x.Name == "Bottom Volume");
            faceComp.Instances.Add(new SimComponentInstance(SimInstanceType.Entity3D, gm.File.Key, bottomVolume.Id));

            //Remove instance
            faceComp.Instances[0].Placements.RemoveAt(0);

            //Check
            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(double.NaN, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[1].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Change parameter/component

        [TestMethod]
        public void ParameterAdded()
        {
            LoadProject(geometrySourceProject);

            var faceComp = this.projectData.Components.First(x => x.Name == "Volume");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var param = new SimDoubleParameter("New Param", "", -1.0);
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            faceComp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)faceComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }
        [TestMethod]
        public void ComponentAdded()
        {
            LoadProject(geometrySourceProject);

            var volComp = this.projectData.Components.First(x => x.Name == "Volume");

            (var gm, var resource) = ProjectUtils.LoadGeometry("Geometry.simgeo", projectData, sp);

            var newComp = new SimComponent()
            {
                InstanceType = SimInstanceType.Entity3D,
            };
            newComp.Slots.Add(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined)));

            var param = new SimDoubleParameter("New Param", "", -1.0);
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            newComp.Parameters.Add(param);

            var volume = gm.Geometry.Volumes.First(x => x.Name == "Upper Volume");
            newComp.Instances.Add(new SimComponentInstance(SimInstanceType.Entity3D, gm.File.Key, volume.Id));

            volComp.Components.Add(new SimChildComponentEntry(new SimSlot(new SimTaxonomyEntryReference(newComp.Slots[0]), ""), newComp));

            AssertUtil.AssertDoubleEqual(2000.0, param.Value);
            AssertUtil.AssertDoubleEqual(2000.0, (double)newComp.Instances[0].InstanceParameterValuesPersistent[param]);
        }

        #endregion

        #region Memory Leak Checks

        private static WeakReference MemoryLeakTestSourceRemoved_Action(SimDoubleParameter param)
        {
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

            WeakReference ptrRef = new WeakReference(param.ValueSource);
            return ptrRef;
        }
        [TestMethod]
        public void MemoryLeakTestSourceRemoved()
        {
            LoadProject(geometrySourceProject);

            var comp = projectData.Components.First(x => x.Name == "Volume");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "GeometryParam");

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
            var comp = projectData.Components.First(x => x.Name == "Volume");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "GeometryParam");

            //Add parameter source
            param.ValueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.VolumeVolume);

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
    }
}
