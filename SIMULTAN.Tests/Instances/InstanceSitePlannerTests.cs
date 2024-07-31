using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Instances
{
    [TestClass]
    public class InstanceSitePlannerTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./SiteplannerTest.simultan");

        #region Add/Remove Instance

        [TestMethod]
        public void AddSitePlannerInstance()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test1.simgeo");
            var comp = projectData.Components.First(x => x.Name == "Building2");

            Assert.AreEqual(0, comp.Instances.Count);

            projectData.ComponentGeometryExchange.Associate(comp, building);

            Assert.AreEqual(1, comp.Instances.Count);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), comp.Instances[0].State);
            Assert.AreEqual(1, comp.Instances[0].Placements.Count);

            var pl = comp.Instances[0].Placements[0] as SimInstancePlacementGeometry;
            Assert.IsNotNull(pl);
            Assert.AreEqual(spProject.SitePlannerFile.Key, pl.FileId);
            Assert.AreEqual(building.ID, pl.GeometryId);
        }

        [TestMethod]
        public void AddSitePlannerInstanceParameters()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test1.simgeo");
            var comp = projectData.Components.First(x => x.Name == "Building2");

            Assert.IsFalse(comp.Parameters.Any(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_LABEL_SOURCE)));

            projectData.ComponentGeometryExchange.Associate(comp, building);

            Assert.IsTrue(comp.Parameters.Any(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_PARAM_TO_GEOMETRY)));
            var p = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_PARAM_TO_GEOMETRY));
            Assert.AreEqual(SimParameterInstancePropagation.PropagateIfInstance, p.InstancePropagationMode);
            Assert.AreEqual(1.0, p.Value);
        }

        [TestMethod]
        public void AddSitePlannerInstanceEvents()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test1.simgeo");
            var comp = projectData.Components.First(x => x.Name == "Building2");

            List<List<SitePlannerBuilding>> modifiedBuildings = new List<List<SitePlannerBuilding>>();
            projectData.ComponentGeometryExchange.BuildingAssociationChanged += (s, e) => modifiedBuildings.Add(e.ToList());

            projectData.ComponentGeometryExchange.Associate(comp, building);

            Assert.AreEqual(1, modifiedBuildings.Count);
            Assert.AreEqual(1, modifiedBuildings[0].Count);
            Assert.AreEqual(building, modifiedBuildings[0][0]);
        }

        [TestMethod]
        public void AddSitePlannerInstanceMissingTarget()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test1.simgeo");
            var comp = projectData.Components.First(x => x.Name == "Building2");

            var inst = new SimComponentInstance(SimInstanceType.BuiltStructure, spProject.SitePlannerFile.Key, 9999);
            comp.Instances.Add(inst);

            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound), inst.State);
            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, ((SimInstancePlacementGeometry)inst.Placements.First()).State);
        }


        [TestMethod]
        public void RemoveSitePlannerInstance()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test2.simgeo");
            var comp = projectData.Components.First(x => x.Name == "Building");

            Assert.AreEqual(1, projectData.ComponentGeometryExchange.GetPlacements(building).Count());

            projectData.ComponentGeometryExchange.Disassociate(comp, building);

            Assert.AreEqual(0, comp.Instances.Count);
        }

        private WeakReference RemoveSitePlannerInstanceMemoryLeak_Action()
        {
            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test2.simgeo");
            var comp = projectData.Components.First(x => x.Name == "Building");

            Assert.AreEqual(1, projectData.ComponentGeometryExchange.GetPlacements(building).Count());

            WeakReference instanceRef = new WeakReference(projectData.ComponentGeometryExchange.GetPlacements(building).First());
            projectData.ComponentGeometryExchange.Disassociate(comp, building);

            return instanceRef;
        }
        [TestMethod]
        public void RemoveSitePlannerInstanceMemoryLeak()
        {
            LoadProject(testProject);

            var instanceRef = RemoveSitePlannerInstanceMemoryLeak_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(instanceRef.IsAlive);
        }

        [TestMethod]
        public void RemoveSitePlannerInstanceEvents()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test2.simgeo");
            var comp = projectData.Components.First(x => x.Name == "Building");

            Assert.AreEqual(1, projectData.ComponentGeometryExchange.GetPlacements(building).Count());

            List<List<SitePlannerBuilding>> modifiedBuildings = new List<List<SitePlannerBuilding>>();
            projectData.ComponentGeometryExchange.BuildingAssociationChanged += (s, e) => modifiedBuildings.Add(e.ToList());

            projectData.ComponentGeometryExchange.Disassociate(comp, building);

            Assert.AreEqual(1, modifiedBuildings.Count);
            Assert.AreEqual(1, modifiedBuildings[0].Count);
            Assert.AreEqual(building, modifiedBuildings[0][0]);
        }

        #endregion

        #region Add/Remove Building

        [TestMethod]
        public void BuildingRemoved()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test2.simgeo");

            var placement = projectData.ComponentGeometryExchange.GetPlacements(building).First();

            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), placement.Instance.State);

            spProject.Buildings.Remove(building);

            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, placement.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound), placement.Instance.State);
        }

        [TestMethod]
        public void MissingBuildingAdded()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test2.simgeo");

            var placement = projectData.ComponentGeometryExchange.GetPlacements(building).First();

            spProject.Buildings.Remove(building);

            Assert.AreEqual(SimInstancePlacementState.InstanceTargetMissing, placement.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound), placement.Instance.State);

            spProject.Buildings.Add(building);

            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), placement.Instance.State);
        }

        #endregion

        #region Parameter Changes

        [TestMethod]
        public void GeometryParameterChangedEvents()
        {
            LoadProject(testProject);

            var spProject = projectData.SitePlannerManager.SitePlannerProjects.First();
            var building = spProject.Buildings.First(x => x.GeometryModelRes.ResourceFile.Name == "test2.simgeo");

            List<SitePlannerBuilding> eventData = new List<SitePlannerBuilding>();
            projectData.ComponentGeometryExchange.BuildingComponentParamaterChanged += (s, e) => eventData.Add(e);

            var comp = projectData.Components.First(x => x.Name == "Building");
            var param = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_PARAM_TO_GEOMETRY));

            Assert.AreEqual(0, eventData.Count);

            //Test if event is sent
            param.Value = 1.0;

            Assert.AreEqual(1, eventData.Count);
            Assert.AreEqual(building, eventData[0]);

            //Make sure that event is only sent when the value has actually changed
            param.Value = 1.0;

            Assert.AreEqual(1, eventData.Count);
            Assert.AreEqual(building, eventData[0]);
        }

        #endregion

        #region Add/Remove Siteplanner Projects

        [TestMethod]
        public void AddSiteplannerProject()
        {
            LoadProject(testProject);

            var resource = project.AddEmptySitePlannerResource(project.ProjectUnpackFolder, "newempty", "newempty {0}");
            var sp = projectData.SitePlannerManager.SitePlannerProjects.FirstOrDefault(x => x.SitePlannerFile == resource);
            Assert.IsNotNull(sp);

            Assert.IsTrue(projectData.ComponentGeometryExchange.IsConnected(sp));
        }

        [TestMethod]
        public void RemoveSiteplannerProject()
        {
            LoadProject(testProject);

            var sp = projectData.SitePlannerManager.SitePlannerProjects.First();

            project.DeleteResource(sp.SitePlannerFile);

            Assert.IsFalse(projectData.ComponentGeometryExchange.IsConnected(sp));
        }

        #endregion
    }
}
