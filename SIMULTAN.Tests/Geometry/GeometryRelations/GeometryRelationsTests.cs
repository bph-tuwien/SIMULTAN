using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Geometry.GeometryRelations
{
    [TestClass]
    public class GeometryRelationsTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./GeometryRelationsTest.simultan");

        private SimTaxonomy geoRelTax;
        private SimTaxonomyEntry generalTaxEntry;
        private SimTaxonomyEntry parentTaxEntry;

        private void Setup()
        {
            geoRelTax = projectData.Taxonomies.GetTaxonomyByKeyOrName("georel");
            generalTaxEntry = geoRelTax.GetTaxonomyEntryByKey("general");
            parentTaxEntry = geoRelTax.GetTaxonomyEntryByKey("is parent");
            Assert.IsNotNull(geoRelTax);
            Assert.IsNotNull(generalTaxEntry);
            Assert.IsNotNull(parentTaxEntry);
        }

        [TestMethod]
        public void AddRelationTest()
        {
            LoadProject(testProject);
            Setup();

            (var gm1, var resource1) = ProjectUtils.LoadGeometry("Geometry 1.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry 2.simgeo", projectData, sp);

            var basegeom1 = gm1.Geometry.Edges.First(x => x.Name == "Edge 3");
            var basegeom2 = gm2.Geometry.Edges.First(x => x.Name == "Edge 3");

            var relation = new SimGeometryRelation(generalTaxEntry,
                projectData.Owner.GlobalID, resource1.Key, basegeom1.Id,
                projectData.Owner.GlobalID, resource2.Key, basegeom2.Id);

            Assert.AreEqual(SimId.Empty, relation.Id);
            Assert.IsNull(relation.Factory);

            Assert.AreEqual(projectData.Owner.GlobalID, relation.Source.ProjectId);
            Assert.AreEqual(projectData.Owner.GlobalID, relation.Target.ProjectId);
            Assert.AreEqual(resource1.Key, relation.Source.FileId);
            Assert.AreEqual(resource2.Key, relation.Target.FileId);
            Assert.AreEqual(basegeom1.Id, relation.Source.BaseGeometryId);
            Assert.AreEqual(basegeom2.Id, relation.Target.BaseGeometryId);

            projectData.GeometryRelations.Add(relation);

            Assert.AreEqual(project, relation.Id.Location);
            Assert.AreNotEqual(0, relation.Id.LocalId);
            Assert.AreEqual(relation, projectData.IdGenerator.GetById<SimGeometryRelation>(relation.Id));
            Assert.AreEqual(projectData.GeometryRelations, relation.Factory);
        }

        [TestMethod]
        public void RemoveRelationTest()
        {
            LoadProject(testProject);
            Setup();

            (var gm1, var resource1) = ProjectUtils.LoadGeometry("Geometry 1.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry 2.simgeo", projectData, sp);

            var basegeom1 = gm1.Geometry.Edges.First(x => x.Name == "Edge 3");
            var basegeom2 = gm2.Geometry.Edges.First(x => x.Name == "Edge 3");

            var relation = new SimGeometryRelation(generalTaxEntry, project.GlobalID, basegeom1, project.GlobalID, basegeom2);

            projectData.GeometryRelations.Add(relation);

            Assert.AreEqual(project, relation.Id.Location);
            Assert.AreNotEqual(0, relation.Id.LocalId);
            Assert.AreEqual(relation, projectData.IdGenerator.GetById<SimGeometryRelation>(relation.Id));
            Assert.AreEqual(projectData.GeometryRelations, relation.Factory);

            projectData.GeometryRelations.Remove(relation);

            Assert.AreEqual(project.GlobalID, relation.Id.GlobalId);
            Assert.AreNotEqual(0, relation.Id.LocalId);
            Assert.IsNull(projectData.IdGenerator.GetById<SimGeometryRelation>(relation.Id));
            Assert.IsNull(relation.Factory);
        }

        [TestMethod]
        public void ReplaceRelationTest()
        {
            LoadProject(testProject);
            Setup();

            (var gm1, var resource1) = ProjectUtils.LoadGeometry("Geometry 1.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry 2.simgeo", projectData, sp);

            var basegeom1 = gm1.Geometry.Edges.First(x => x.Name == "Edge 3");
            var basegeom2 = gm2.Geometry.Edges.First(x => x.Name == "Edge 3");

            var relation = new SimGeometryRelation(generalTaxEntry,
                projectData.Owner.GlobalID, resource1.Key, basegeom1.Id,
                projectData.Owner.GlobalID, resource2.Key, basegeom2.Id);

            Assert.AreEqual(SimId.Empty, relation.Id);
            Assert.IsNull(relation.Factory);

            Assert.AreEqual(projectData.Owner.GlobalID, relation.Source.ProjectId);
            Assert.AreEqual(projectData.Owner.GlobalID, relation.Target.ProjectId);
            Assert.AreEqual(resource1.Key, relation.Source.FileId);
            Assert.AreEqual(resource2.Key, relation.Target.FileId);
            Assert.AreEqual(basegeom1.Id, relation.Source.BaseGeometryId);
            Assert.AreEqual(basegeom2.Id, relation.Target.BaseGeometryId);

            projectData.GeometryRelations.Add(relation);

            Assert.AreEqual(project, relation.Id.Location);
            Assert.AreNotEqual(0, relation.Id.LocalId);
            Assert.AreEqual(relation, projectData.IdGenerator.GetById<SimGeometryRelation>(relation.Id));
            Assert.AreEqual(projectData.GeometryRelations, relation.Factory);

            var relation2 = new SimGeometryRelation(generalTaxEntry,
                projectData.Owner.GlobalID, resource2.Key, basegeom2.Id,
                projectData.Owner.GlobalID, resource1.Key, basegeom1.Id);

            var index = projectData.GeometryRelations.IndexOf(relation);
            projectData.GeometryRelations[index] = relation2;

            Assert.AreEqual(project, relation2.Id.Location);
            Assert.AreNotEqual(0, relation2.Id.LocalId);
            Assert.AreEqual(relation2, projectData.IdGenerator.GetById<SimGeometryRelation>(relation2.Id));
            Assert.AreEqual(projectData.GeometryRelations, relation2.Factory);

            Assert.IsNull(relation.Id.Location);
            Assert.AreNotEqual(0, relation.Id.LocalId);
            Assert.IsNull(projectData.IdGenerator.GetById<SimGeometryRelation>(relation.Id));
            Assert.IsNull(relation.Factory);
        }

        [TestMethod]
        public void RemoveRelationTypeEntryTest()
        {
            LoadProject(testProject);
            Setup();

            (var gm1, var resource1) = ProjectUtils.LoadGeometry("Geometry 1.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("Geometry 2.simgeo", projectData, sp);

            var basegeom1 = gm1.Geometry.Edges.First(x => x.Name == "Edge 3");
            var basegeom2 = gm2.Geometry.Edges.First(x => x.Name == "Edge 3");

            var relation = new SimGeometryRelation(generalTaxEntry, project.GlobalID, basegeom1, project.GlobalID, basegeom2);

            projectData.GeometryRelations.Add(relation);

            Assert.AreEqual(generalTaxEntry, relation.RelationType.Target);

            geoRelTax.Entries.Remove(generalTaxEntry);

            Assert.IsNull(relation.RelationType);
        }

        [TestMethod]
        public void GetRelationsTest()
        {
            LoadProject(testProject);
            Setup();

            (var gm1, var resource1) = ProjectUtils.LoadGeometry("child.simgeo", projectData, sp);
            (var gm2, var resource2) = ProjectUtils.LoadGeometry("parent.simgeo", projectData, sp);


            var volume = gm2.Geometry.Volumes.First(x => x.Name == "Volume 41");

            var relations = projectData.GeometryRelations.GetRelationsOf(volume).ToList();

            Assert.AreEqual(2, relations.Count);

            var rel1 = relations[0];
            var rel2 = relations[1];
            Assert.AreEqual(parentTaxEntry, rel1.RelationType.Target);
            Assert.AreEqual(parentTaxEntry, rel2.RelationType.Target);
            Assert.AreEqual(resource2.Key, rel1.Target.FileId);
            Assert.AreEqual(volume.Id, rel1.Target.BaseGeometryId);
            Assert.AreEqual(resource2.Key, rel1.Target.FileId);
            Assert.AreEqual(volume.Id, rel1.Target.BaseGeometryId);
            Assert.AreEqual(resource1.Key, rel1.Source.FileId);
            Assert.AreEqual(resource1.Key, rel1.Source.FileId);

            relations = projectData.GeometryRelations.GetRelationsFrom(volume).ToList();
            Assert.AreEqual(0, relations.Count);
            relations = projectData.GeometryRelations.GetRelationsTo(volume).ToList();
            Assert.AreEqual(2, relations.Count);

            relations = projectData.GeometryRelations.GetRelationsOf(gm2).ToList();
            Assert.AreEqual(3, relations.Count);

            relations = projectData.GeometryRelations.GetRelationsOf(gm1).ToList();
            Assert.AreEqual(2, relations.Count);
        }

        [TestMethod]
        public void NotifyChangedTest()
        {
            LoadProject(testProject);
            Setup();

            Assert.IsFalse(projectData.GeometryRelations.HasChanges);

            var lastAccessTime = projectData.GeometryRelations.LastChange;

            var rel = projectData.GeometryRelations[0];

            rel.IsAutogenerated = true;
            Assert.IsTrue(projectData.GeometryRelations.HasChanges);
            Assert.IsTrue(lastAccessTime <= projectData.GeometryRelations.LastChange);
            Assert.IsTrue(DateTime.Now >= projectData.GeometryRelations.LastChange);
            lastAccessTime = projectData.GeometryRelations.LastChange;
            projectData.GeometryRelations.ResetChanges();
            Assert.IsFalse(projectData.GeometryRelations.HasChanges);

            rel.RelationType = new SimTaxonomyEntryReference(this.generalTaxEntry);
            Assert.IsTrue(projectData.GeometryRelations.HasChanges);
            Assert.IsTrue(lastAccessTime <= projectData.GeometryRelations.LastChange);
            Assert.IsTrue(DateTime.Now >= projectData.GeometryRelations.LastChange);
            lastAccessTime = projectData.GeometryRelations.LastChange;
            projectData.GeometryRelations.ResetChanges();

            rel.Source = new SimBaseGeometryReference(rel.Source.ProjectId, rel.Source.FileId, 1337);
            Assert.IsTrue(projectData.GeometryRelations.HasChanges);
            Assert.IsTrue(lastAccessTime <= projectData.GeometryRelations.LastChange);
            Assert.IsTrue(DateTime.Now >= projectData.GeometryRelations.LastChange);
            lastAccessTime = projectData.GeometryRelations.LastChange;
            projectData.GeometryRelations.ResetChanges();

            rel.Target = new SimBaseGeometryReference(rel.Target.ProjectId, rel.Target.FileId, 1337);
            Assert.IsTrue(projectData.GeometryRelations.HasChanges);
            Assert.IsTrue(lastAccessTime <= projectData.GeometryRelations.LastChange);
            Assert.IsTrue(DateTime.Now >= projectData.GeometryRelations.LastChange);
        }

    }
}
