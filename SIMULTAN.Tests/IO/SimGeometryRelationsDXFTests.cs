using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.GRDXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class SimGeometryRelationsDXFTests
    {

        private (SimTaxonomy, SimTaxonomyEntry) Setup(ExtendedProjectData projectData)
        {
            var taxonomy = new SimTaxonomy("Test");
            var taxEntry = new SimTaxonomyEntry("test", "test");
            taxonomy.Entries.Add(taxEntry);
            projectData.Taxonomies.Add(taxonomy);
            return (taxonomy, taxEntry);
        }

        #region Geometry Relations

        [TestMethod]
        public void WriteEmpty()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimGeometryRelationsDxfIO.Write(writer, projectData.GeometryRelations);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_GRDXF_WriteEmpty, exportedString);
        }

        [TestMethod]
        public void ReadEmptyV20()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            Assert.AreEqual(0, projectData.GeometryRelations.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_GRDXF_ReadEmptyV20)))
            {
                SimGeometryRelationsDxfIO.Read(reader, new DXFParserInfo(projectData.GeometryRelations.CalledFromLocation.GlobalID, projectData));
            }

            // should not have loaded anything cause empty
            Assert.AreEqual(0, projectData.GeometryRelations.Count);
        }

        [TestMethod]
        public void WriteRelations()
        {
            var projectData = new ExtendedProjectData();
            var globalId = Guid.Parse("0a060b64-94a5-4d04-8acc-52844be9629c");
            projectData.SetCallingLocation(new DummyReferenceLocation(globalId));

            (var taxonomy, var taxEntry) = Setup(projectData);

            var rel1 = new SimGeometryRelation(null,
                new SimBaseGeometryReference(globalId, 1, 33),
                new SimBaseGeometryReference(globalId, 1, 44),
                false);
            var rel2 = new SimGeometryRelation(null,
                new SimBaseGeometryReference(globalId, 1, 33),
                new SimBaseGeometryReference(globalId, 1, 55),
                true);
            var rel3 = new SimGeometryRelation(new SimTaxonomyEntryReference(taxEntry),
                new SimBaseGeometryReference(globalId, 2, 44),
                new SimBaseGeometryReference(globalId, 2, 66),
                false);

            projectData.GeometryRelations.Add(rel1);
            projectData.GeometryRelations.Add(rel2);
            projectData.GeometryRelations.Add(rel3);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimGeometryRelationsDxfIO.Write(writer, projectData.GeometryRelations);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_GRDXF_WriteRelations, exportedString);
        }

        [TestMethod]
        public void ReadRelationsV20()
        {
            var projectData = new ExtendedProjectData();
            var globalId = Guid.Parse("0a060b64-94a5-4d04-8acc-52844be9629c");
            projectData.SetCallingLocation(new DummyReferenceLocation(globalId));
            (_, var taxEntry) = Setup(projectData);

            Assert.AreEqual(0, projectData.GeometryRelations.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_GRDXF_ReadRelationsV20)))
            {
                SimGeometryRelationsDxfIO.Read(reader, new DXFParserInfo(projectData.GeometryRelations.CalledFromLocation.GlobalID, projectData));
            }

            Assert.AreEqual(3, projectData.GeometryRelations.Count);

            var rel = projectData.GeometryRelations[0];
            Assert.AreNotEqual(0, rel.Id.LocalId);
            Assert.AreEqual(globalId, rel.Id.GlobalId);
            Assert.AreEqual(projectData.GeometryRelations, rel.Factory);
            Assert.IsNull(rel.RelationType);
            Assert.IsFalse(rel.IsAutogenerated);
            Assert.AreEqual(globalId, rel.Source.ProjectId);
            Assert.AreEqual(33ul, rel.Source.BaseGeometryId);
            Assert.AreEqual(1, rel.Source.FileId);
            Assert.AreEqual(globalId, rel.Target.ProjectId);
            Assert.AreEqual(44ul, rel.Target.BaseGeometryId);
            Assert.AreEqual(1, rel.Target.FileId);
            Assert.AreEqual(rel, projectData.IdGenerator.GetById<SimGeometryRelation>(rel.Id));

            rel = projectData.GeometryRelations[1];
            Assert.AreNotEqual(0, rel.Id.LocalId);
            Assert.AreEqual(globalId, rel.Id.GlobalId);
            Assert.AreEqual(projectData.GeometryRelations, rel.Factory);
            Assert.IsNull(rel.RelationType);
            Assert.IsTrue(rel.IsAutogenerated);
            Assert.AreEqual(globalId, rel.Source.ProjectId);
            Assert.AreEqual(33ul, rel.Source.BaseGeometryId);
            Assert.AreEqual(1, rel.Source.FileId);
            Assert.AreEqual(globalId, rel.Target.ProjectId);
            Assert.AreEqual(55ul, rel.Target.BaseGeometryId);
            Assert.AreEqual(1, rel.Target.FileId);
            Assert.AreEqual(rel, projectData.IdGenerator.GetById<SimGeometryRelation>(rel.Id));

            rel = projectData.GeometryRelations[2];
            Assert.AreNotEqual(0, rel.Id.LocalId);
            Assert.AreEqual(globalId, rel.Id.GlobalId);
            Assert.AreEqual(projectData.GeometryRelations, rel.Factory);
            Assert.IsNotNull(rel.RelationType);
            Assert.AreEqual(taxEntry, rel.RelationType.Target);
            Assert.IsFalse(rel.IsAutogenerated);
            Assert.AreEqual(globalId, rel.Source.ProjectId);
            Assert.AreEqual(44ul, rel.Source.BaseGeometryId);
            Assert.AreEqual(2, rel.Source.FileId);
            Assert.AreEqual(globalId, rel.Target.ProjectId);
            Assert.AreEqual(66ul, rel.Target.BaseGeometryId);
            Assert.AreEqual(2, rel.Target.FileId);
            Assert.AreEqual(rel, projectData.IdGenerator.GetById<SimGeometryRelation>(rel.Id));
        }
        #endregion
    }
}
