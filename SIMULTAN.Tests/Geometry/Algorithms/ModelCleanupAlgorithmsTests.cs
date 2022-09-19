using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Utils;
using SIMULTAN.Utils;
using SIMULTAN.Utils.UndoRedo;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class ModelCleanupAlgorithmsTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\ModelCleanupAlgorithmsTestProject.simultan");

        private void DoReAssociation(GeometryModelData newGeom, GeometryModel model, List<(BaseGeometry, SimComponent)> toReassociate, HashSet<SimInstancePlacementGeometry> toDeassociate)
        {
            var undoItem = new GroupUndoItem();
            toDeassociate.ForEach(x => undoItem.Items.Add(new RemovePlacementUndoItem(x)));
            undoItem.Items.Add(new ModelCompleteStateUndoItem(newGeom, model));
            toReassociate.ForEach(x => undoItem.Items.Add(new AddPlacementUndoItem(x.Item1, x.Item2)));
            undoItem.Execute();
        }

        [TestMethod]
        public void TestVertexDuplicateRemovalRestoresComponentAssociations()
        {
            LoadProject(testProject);
            var exchange = projectData.ComponentGeometryExchange;
            AABBGrid vertexGrid = null;
            AABBGrid edgeGrid = null;
            (var gm, var resource) = ProjectUtils.LoadGeometry("duplicate_vertices.simgeo", projectData, sp);

            var geo = gm.Geometry;
            var geoCopy = geo.Clone();

            Assert.AreEqual(8, geoCopy.Vertices.Count);
            Assert.AreEqual(4, geoCopy.Edges.Count);

            var ov1 = geo.Vertices.FirstOrDefault(x => x.Name == "v1");
            var ov2 = geo.Vertices.FirstOrDefault(x => x.Name == "v2");
            var ov3 = geo.Vertices.FirstOrDefault(x => x.Name == "v3");
            var ov4 = geo.Vertices.FirstOrDefault(x => x.Name == "v4");
            var c1 = exchange.GetComponents(ov1).ToList()[0];
            var c2 = exchange.GetComponents(ov2).ToList()[0];
            var c3 = exchange.GetComponents(ov3).ToList()[0];
            var v1 = geoCopy.Vertices.FirstOrDefault(x => x.Name == "v1");
            var v2 = geoCopy.Vertices.FirstOrDefault(x => x.Name == "v2");
            var v3 = geoCopy.Vertices.FirstOrDefault(x => x.Name == "v3");
            var v4 = geoCopy.Vertices.FirstOrDefault(x => x.Name == "v4");
            Assert.IsTrue(c1.Name == "v1");
            Assert.IsTrue(c2.Name == "v2");
            Assert.IsTrue(c3.Name == "v3");
            Assert.AreEqual(0, exchange.GetComponents(ov4).ToList().Count);

            var mergeTracker = new ModelCleanupAlgorithms.MergeTracker<Vertex>();
            var removedCount = ModelCleanupAlgorithms.RemoveDuplicateVertices(geoCopy, 0.1, ref vertexGrid, ref edgeGrid, mergeTracker);
            var mergedVertices = mergeTracker.AsList();

            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(1, mergedVertices.Count);
            Assert.IsTrue(geoCopy.Vertices.Contains(mergedVertices[0].Item1));
            foreach (var rv in mergedVertices[0].Item2)
            {
                if (rv != mergedVertices[0].Item1)
                {
                    Assert.IsFalse(geoCopy.Vertices.Contains(rv));
                }
            }

            var geometryToReassign = new List<(BaseGeometry, SimComponent)>();
            var geometryToUnassign = new HashSet<SimInstancePlacementGeometry>();

            ModelCleanupAlgorithms.ReassignComponentsAfterMerge(mergedVertices, geo.Vertices, exchange, geometryToReassign, geometryToUnassign);

            // check if un/reassignments are correct
            Assert.AreEqual(2, geometryToReassign.Count);
            Assert.AreEqual(2, geometryToUnassign.Count);

            Assert.IsTrue(geometryToReassign.All(x => x.Item1 == ov1));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c2));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c3));

            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == ov2.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == ov3.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c2));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c3));

            // do the un/reassignment using the undo items
            DoReAssociation(geoCopy, gm, geometryToReassign, geometryToUnassign);

            var vc1 = exchange.GetComponents(v1);
            var vc2 = exchange.GetComponents(v2);
            var vc3 = exchange.GetComponents(v3);
            var vc4 = exchange.GetComponents(v4);
            Assert.AreEqual(3, vc1.Count());
            Assert.IsTrue(vc1.Contains(c1));
            Assert.IsTrue(vc1.Contains(c2));
            Assert.IsTrue(vc1.Contains(c3));
            Assert.AreEqual(0, vc2.Count());
            Assert.AreEqual(0, vc3.Count());
            Assert.AreEqual(0, vc4.Count());
        }

        [TestMethod]
        public void TestEdgeDuplicateRemovalRestoresComponentAssociations()
        {
            LoadProject(testProject);
            var exchange = projectData.ComponentGeometryExchange;
            AABBGrid vertexGrid = null;
            AABBGrid edgeGrid = null;
            (var gm, var resource) = ProjectUtils.LoadGeometry("duplicate_edges.simgeo", projectData, sp);

            var geo = gm.Geometry;
            var geoCopy = geo.Clone();

            Assert.AreEqual(8, geoCopy.Vertices.Count);
            Assert.AreEqual(4, geoCopy.Edges.Count);

            var oe1 = geo.Edges.FirstOrDefault(x => x.Name == "e1");
            var oe2 = geo.Edges.FirstOrDefault(x => x.Name == "e2");
            var oe3 = geo.Edges.FirstOrDefault(x => x.Name == "e3");
            var oe4 = geo.Edges.FirstOrDefault(x => x.Name == "e4");
            var c1 = exchange.GetComponents(oe1).ToList()[0];
            var c2 = exchange.GetComponents(oe2).ToList()[0];
            var c3 = exchange.GetComponents(oe3).ToList()[0];
            var e1 = geoCopy.Edges.FirstOrDefault(x => x.Name == "e1");
            var e2 = geoCopy.Edges.FirstOrDefault(x => x.Name == "e2");
            var e3 = geoCopy.Edges.FirstOrDefault(x => x.Name == "e3");
            var e4 = geoCopy.Edges.FirstOrDefault(x => x.Name == "e4");
            Assert.IsTrue(c1.Name == "e1");
            Assert.IsTrue(c2.Name == "e2");
            Assert.IsTrue(c3.Name == "e3");
            Assert.AreEqual(0, exchange.GetComponents(oe4).ToList().Count);

            var mergeTracker = new ModelCleanupAlgorithms.MergeTracker<Edge>();

            ModelCleanupAlgorithms.RemoveDuplicateVertices(geoCopy,0.1, ref vertexGrid, ref edgeGrid);
            var removedCount = ModelCleanupAlgorithms.RemoveDuplicateEdges(geoCopy, ref edgeGrid,mergeTracker);
            var merged = mergeTracker.AsList();

            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(1, merged.Count);
            Assert.IsTrue(geoCopy.Edges.Contains(merged[0].Item1));
            foreach (var r in merged[0].Item2)
            {
                if (r != merged[0].Item1)
                {
                    Assert.IsFalse(geoCopy.Edges.Contains(r));
                }
            }

            var geometryToReassign = new List<(BaseGeometry, SimComponent)>();
            var geometryToUnassign = new HashSet<SimInstancePlacementGeometry>();

            ModelCleanupAlgorithms.ReassignComponentsAfterMerge(merged, geo.Edges, exchange, geometryToReassign, geometryToUnassign);

            // check if un/reassignments are correct
            Assert.AreEqual(2, geometryToReassign.Count);
            Assert.AreEqual(2, geometryToUnassign.Count);

            Assert.IsTrue(geometryToReassign.All(x => x.Item1 == oe1));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c2));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c3));

            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == oe2.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == oe3.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c2));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c3));

            // do the un/reassignment
            DoReAssociation(geoCopy, gm, geometryToReassign, geometryToUnassign);

            var ec1 = exchange.GetComponents(e1);
            var ec2 = exchange.GetComponents(e2);
            var ec3 = exchange.GetComponents(e3);
            var ec4 = exchange.GetComponents(e4);
            Assert.AreEqual(3, ec1.Count());
            Assert.IsTrue(ec1.Contains(c1));
            Assert.IsTrue(ec1.Contains(c2));
            Assert.IsTrue(ec1.Contains(c3));
            Assert.AreEqual(0, ec2.Count());
            Assert.AreEqual(0, ec3.Count());
            Assert.AreEqual(0, ec4.Count());
        }

        [TestMethod]
        public void TestFaceDuplicateRemovalRestoresComponentAssociations()
        {
            LoadProject(testProject);
            var exchange = projectData.ComponentGeometryExchange;
            AABBGrid vertexGrid = null;
            AABBGrid edgeGrid = null;
            AABBGrid faceGrid = null;
            (var gm, var resource) = ProjectUtils.LoadGeometry("duplicate_faces.simgeo", projectData, sp);

            var geo = gm.Geometry;
            var geoCopy = geo.Clone();

            Assert.AreEqual(16, geoCopy.Vertices.Count);
            Assert.AreEqual(16, geoCopy.Edges.Count);
            Assert.AreEqual(4, geoCopy.Faces.Count);

            var of1 = geo.Faces.FirstOrDefault(x => x.Name == "f1");
            var of2 = geo.Faces.FirstOrDefault(x => x.Name == "f2");
            var of3 = geo.Faces.FirstOrDefault(x => x.Name == "f3");
            var of4 = geo.Faces.FirstOrDefault(x => x.Name == "f4");
            var c1 = exchange.GetComponents(of1).ToList()[0];
            var c2 = exchange.GetComponents(of2).ToList()[0];
            var c3 = exchange.GetComponents(of3).ToList()[0];
            var f1 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f1");
            var f2 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f2");
            var f3 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f3");
            var f4 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f4");
            Assert.IsTrue(c1.Name == "f1");
            Assert.IsTrue(c2.Name == "f2");
            Assert.IsTrue(c3.Name == "f3");
            Assert.AreEqual(0, exchange.GetComponents(of4).ToList().Count);

            var faceTracker = new ModelCleanupAlgorithms.MergeTracker<Face>();

            int count = 0;
            int removedCount = 0;
            bool changed = true;

            while (changed)
            {
                count +=ModelCleanupAlgorithms.RemoveDuplicateVertices(geoCopy, 0.1, ref vertexGrid, ref edgeGrid);
                count += ModelCleanupAlgorithms.RemoveDuplicateEdges(geoCopy, ref edgeGrid);
                var cc = ModelCleanupAlgorithms.RemoveDuplicateFaces(geoCopy, ref faceGrid, faceTracker);
                count += cc;
                removedCount += cc;

                changed = count > 0;
                count = 0;
            }

            var merged = faceTracker.AsList();
            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(1, merged.Count);
            Assert.IsTrue(geoCopy.Faces.Contains(merged[0].Item1));
            foreach (var r in merged[0].Item2)
            {
                if (r != merged[0].Item1)
                {
                    Assert.IsFalse(geoCopy.Faces.Contains(r));
                }
            }

            var geometryToReassign = new List<(BaseGeometry, SimComponent)>();
            var geometryToUnassign = new HashSet<SimInstancePlacementGeometry>();

            ModelCleanupAlgorithms.ReassignComponentsAfterMerge(merged, geo.Faces, exchange, geometryToReassign, geometryToUnassign);

            // check if un/reassignments are correct
            Assert.AreEqual(2, geometryToReassign.Count);
            Assert.AreEqual(2, geometryToUnassign.Count);

            Assert.IsTrue(geometryToReassign.All(x => x.Item1 == of1));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c2));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c3));

            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == of2.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == of3.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c2));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c3));

            // do the un/reassignment
            // first replace geometry
            DoReAssociation(geoCopy, gm, geometryToReassign, geometryToUnassign);

            var ec1 = exchange.GetComponents(f1);
            var ec2 = exchange.GetComponents(f2);
            var ec3 = exchange.GetComponents(f3);
            var ec4 = exchange.GetComponents(f4);
            Assert.AreEqual(3, ec1.Count());
            Assert.IsTrue(ec1.Contains(c1));
            Assert.IsTrue(ec1.Contains(c2));
            Assert.IsTrue(ec1.Contains(c3));
            Assert.AreEqual(0, ec2.Count());
            Assert.AreEqual(0, ec3.Count());
            Assert.AreEqual(0, ec4.Count());
        }

        [TestMethod]
        public void TestHoleDuplicateRemovalRestoresComponentAssociations()
        {
            LoadProject(testProject);
            var exchange = projectData.ComponentGeometryExchange;
            AABBGrid vertexGrid = null;
            AABBGrid edgeGrid = null;
            AABBGrid faceGrid = null;
            (var gm, var resource) = ProjectUtils.LoadGeometry("duplicate_hole.simgeo", projectData, sp);

            var geo = gm.Geometry;
            var geoCopy = geo.Clone();

            Assert.AreEqual(12, geoCopy.Vertices.Count);
            Assert.AreEqual(12, geoCopy.Edges.Count);
            Assert.AreEqual(3, geoCopy.Faces.Count);
            Assert.AreEqual(2, geoCopy.Faces.First(x => x.Id == 18).Holes.Count);

            var of1 = geo.Faces.FirstOrDefault(x => x.Name == "f1");
            var of2 = geo.Faces.FirstOrDefault(x => x.Name == "f2");
            var of3 = geo.Faces.FirstOrDefault(x => x.Name == "f3");
            var c2 = exchange.GetComponents(of2).ToList()[0];
            var c3 = exchange.GetComponents(of3).ToList()[0];
            var f1 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f1");
            var f2 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f2");
            var f3 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f3");
            Assert.AreEqual(0, exchange.GetComponents(of1).ToList().Count);
            Assert.IsTrue(c2.Name == "fh1");
            Assert.IsTrue(c3.Name == "fh2");

            var faceTracker = new ModelCleanupAlgorithms.MergeTracker<Face>();

            int count = 0;
            int removedCount = 0;
            bool changed = true;

            while (changed)
            {
                count +=ModelCleanupAlgorithms.RemoveDuplicateVertices(geoCopy, 0.1, ref vertexGrid, ref edgeGrid);
                count += ModelCleanupAlgorithms.RemoveDuplicateEdges(geoCopy, ref edgeGrid);

                changed = count > 0;
                count = 0;
            }
            removedCount = ModelCleanupAlgorithms.RemoveDuplicateFaces(geoCopy, ref faceGrid, faceTracker);

            var merged = faceTracker.AsList();
            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(1, merged.Count);
            Assert.IsTrue(geoCopy.Faces.Contains(merged[0].Item1));
            foreach (var r in merged[0].Item2)
            {
                if (r != merged[0].Item1)
                {
                    Assert.IsFalse(geoCopy.Faces.Contains(r));
                }
            }

            var geometryToReassign = new List<(BaseGeometry, SimComponent)>();
            var geometryToUnassign = new HashSet<SimInstancePlacementGeometry>();

            ModelCleanupAlgorithms.ReassignComponentsAfterMerge(merged, geo.Faces, exchange, geometryToReassign, geometryToUnassign);

            // check if un/reassignments are correct
            Assert.AreEqual(1, geometryToReassign.Count);
            Assert.AreEqual(1, geometryToUnassign.Count);

            Assert.IsTrue(geometryToReassign.All(x => x.Item1 == of2));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c3));

            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == of3.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c3));

            // do the un/reassignment
            // first replace geometry
            DoReAssociation(geoCopy, gm, geometryToReassign, geometryToUnassign);

            var ec1 = exchange.GetComponents(f1);
            var ec2 = exchange.GetComponents(f2);
            Assert.AreEqual(0, ec1.Count());
            Assert.AreEqual(2, ec2.Count());
            Assert.IsTrue(ec2.Contains(c2));
            Assert.IsTrue(ec2.Contains(c3));
        }

        [TestMethod]
        public void TestVolumeDuplicateRemovalRestoresComponentAssociations()
        {
            LoadProject(testProject);
            var exchange = projectData.ComponentGeometryExchange;
            AABBGrid vertexGrid = null;
            AABBGrid edgeGrid = null;
            AABBGrid faceGrid = null;
            AABBGrid volumeGrid = null;
            (var gm, var resource) = ProjectUtils.LoadGeometry("duplicate_volumes.simgeo", projectData, sp);

            var geo = gm.Geometry;
            var geoCopy = geo.Clone();

            Assert.AreEqual(32, geoCopy.Vertices.Count);
            Assert.AreEqual(48, geoCopy.Edges.Count);
            Assert.AreEqual(24, geoCopy.Faces.Count);
            Assert.AreEqual(4, geoCopy.Volumes.Count);

            var ov1 = geo.Volumes.FirstOrDefault(x => x.Name == "v1");
            var ov2 = geo.Volumes.FirstOrDefault(x => x.Name == "v2");
            var ov3 = geo.Volumes.FirstOrDefault(x => x.Name == "v3");
            var ov4 = geo.Volumes.FirstOrDefault(x => x.Name == "v4");
            var c1 = exchange.GetComponents(ov1).ToList()[0];
            var c2 = exchange.GetComponents(ov2).ToList()[0];
            var c3 = exchange.GetComponents(ov3).ToList()[0];
            var v1 = geoCopy.Volumes.FirstOrDefault(x => x.Name == "v1");
            var v2 = geoCopy.Volumes.FirstOrDefault(x => x.Name == "v2");
            var v3 = geoCopy.Volumes.FirstOrDefault(x => x.Name == "v3");
            var v4 = geoCopy.Volumes.FirstOrDefault(x => x.Name == "v4");
            Assert.IsTrue(c1.Name == "v1");
            Assert.IsTrue(c2.Name == "v2");
            Assert.IsTrue(c3.Name == "v3");
            Assert.AreEqual(0, exchange.GetComponents(ov4).ToList().Count);

            var mergeTracker = new ModelCleanupAlgorithms.MergeTracker<Volume>();

            int count = 0;
            int removedCount = 0;
            bool changed = true;

            while (changed)
            {
                count +=ModelCleanupAlgorithms.RemoveDuplicateVertices(geoCopy, 0.1, ref vertexGrid, ref edgeGrid);
                count += ModelCleanupAlgorithms.RemoveDuplicateEdges(geoCopy, ref edgeGrid);
                count += ModelCleanupAlgorithms.RemoveDuplicateFaces(geoCopy, ref faceGrid);
                var cc = ModelCleanupAlgorithms.RemoveDuplicateVolumes(geoCopy, ref volumeGrid,mergeTracker);
                count += cc;
                removedCount += cc;

                changed = count > 0;
                count = 0;
            }

            var merged = mergeTracker.AsList();
            Assert.AreEqual(3, removedCount);
            Assert.AreEqual(1, merged.Count);
            Assert.IsTrue(geoCopy.Volumes.Contains(merged[0].Item1));
            foreach (var r in merged[0].Item2)
            {
                if (r != merged[0].Item1)
                {
                    Assert.IsFalse(geoCopy.Volumes.Contains(r));
                }
            }

            var geometryToReassign = new List<(BaseGeometry, SimComponent)>();
            var geometryToUnassign = new HashSet<SimInstancePlacementGeometry>();

            ModelCleanupAlgorithms.ReassignComponentsAfterMerge(merged, geo.Volumes, exchange, geometryToReassign, geometryToUnassign);

            // check if un/reassignments are correct
            Assert.AreEqual(2, geometryToReassign.Count);
            Assert.AreEqual(2, geometryToUnassign.Count);

            Assert.IsTrue(geometryToReassign.All(x => x.Item1 == ov1));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c2));
            Assert.IsTrue(geometryToReassign.Any(x => x.Item2 == c3));

            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == ov2.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == ov3.Id));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c2));
            Assert.IsTrue(geometryToUnassign.Any(x => x.Instance.Component == c3));

            // do the un/reassignment
            // first replace geometry
            DoReAssociation(geoCopy, gm, geometryToReassign, geometryToUnassign);

            var ec1 = exchange.GetComponents(v1).Where(x=> x.IsAutomaticallyGenerated == false);
            var ec2 = exchange.GetComponents(v2).Where(x=> x.IsAutomaticallyGenerated == false);
            var ec3 = exchange.GetComponents(v3).Where(x=> x.IsAutomaticallyGenerated == false);
            var ec4 = exchange.GetComponents(v4).Where(x=> x.IsAutomaticallyGenerated == false);
            Assert.AreEqual(3, ec1.Count());
            Assert.IsTrue(ec1.Contains(c1));
            Assert.IsTrue(ec1.Contains(c2));
            Assert.IsTrue(ec1.Contains(c3));
            Assert.AreEqual(0, ec2.Count());
            Assert.AreEqual(0, ec3.Count());
            Assert.AreEqual(0, ec4.Count());
        }

        [TestMethod]
        public void TestEdgeEdgeSplitRestoresComponentAssociations()
        {
            LoadProject(testProject);
            var exchange = projectData.ComponentGeometryExchange;
            AABBGrid edgeGrid = null;
            (var gm, var resource) = ProjectUtils.LoadGeometry("edge_edge_split.simgeo", projectData, sp);

            var geo = gm.Geometry;
            var geoCopy = geo.Clone();

            Assert.AreEqual(4, geoCopy.Vertices.Count);
            Assert.AreEqual(2, geoCopy.Edges.Count);

            var oe1 = geo.Edges.FirstOrDefault(x => x.Name == "e1");
            var oe2 = geo.Edges.FirstOrDefault(x => x.Name == "e2");
            var c1 = exchange.GetComponents(oe1).ToList()[0];
            var c2 = exchange.GetComponents(oe2).ToList()[0];
            var e1 = geoCopy.Edges.FirstOrDefault(x => x.Name == "v1");
            var e2 = geoCopy.Edges.FirstOrDefault(x => x.Name == "v2");
            Assert.IsTrue(c1.Name == "ee1");
            Assert.IsTrue(c2.Name == "ee2");

            var replacementTracker = new ModelCleanupAlgorithms.ReplacementTracker<Edge>();


            var splitCount = ModelCleanupAlgorithms.SplitEdgeEdgeIntersections(geoCopy, 0.1, ref edgeGrid, replacementTracker);

            var replaced = replacementTracker.GetReplacements();
            Assert.AreEqual(1, splitCount);
            Assert.AreEqual(2, replaced.Count);
            for (int i = 0; i < replaced.Count; i++)
            {
                Assert.IsFalse(geoCopy.Edges.Contains(replaced[i].oldGeom));
                foreach (var r in replaced[i].newGeom)
                {
                    Assert.IsTrue(geoCopy.Edges.Contains(r));
                }
            }
            var geometryToReassign = new List<(BaseGeometry geom, SimComponent comp)>();
            var geometryToUnassign = new HashSet<SimInstancePlacementGeometry>();

            ModelCleanupAlgorithms.ReassignComponentsAfterReplacement(replaced, geo.Edges, exchange, geometryToReassign, geometryToUnassign);

            // check if un/reassignments are correct
            Assert.AreEqual(4, geometryToReassign.Count);
            Assert.AreEqual(2, geometryToUnassign.Count);

            Assert.AreEqual(2, geometryToReassign.Count(x => x.comp == c1));
            Assert.AreEqual(2, geometryToReassign.Count(x => x.comp == c2));

            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == oe1.Id && x.Instance.Component == c1));
            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == oe2.Id && x.Instance.Component == c2));

            // do the un/reassignment
            // first replace geometry
            DoReAssociation(geoCopy, gm, geometryToReassign, geometryToUnassign);

            var replacing = replaced.First(x => x.oldGeom.Id == oe1.Id).newGeom;
            Assert.AreEqual(2, replacing.Count);
            foreach (var r in replacing)
            {
                var ec = exchange.GetComponents(r);
                Assert.IsTrue(ec.All(x => x == c1));
            }
            replacing = replaced.First(x => x.oldGeom.Id == oe2.Id).newGeom;
            Assert.AreEqual(2, replacing.Count);
            foreach (var r in replacing)
            {
                var ec = exchange.GetComponents(r);
                Assert.IsTrue(ec.All(x => x == c2));
            }
        }

        [TestMethod]
        public void TestEdgeVerexSplitRestoresComponentAssociations()
        {
            LoadProject(testProject);
            var exchange = projectData.ComponentGeometryExchange;
            AABBGrid vertexGrid = null;
            AABBGrid edgeGrid = null;
            (var gm, var resource) = ProjectUtils.LoadGeometry("edge_vertex_split.simgeo", projectData, sp);

            var geo = gm.Geometry;
            var geoCopy = geo.Clone();

            Assert.AreEqual(4, geoCopy.Vertices.Count);
            Assert.AreEqual(2, geoCopy.Edges.Count);

            var oe1 = geo.Edges.FirstOrDefault(x => x.Name == "e1");
            var oe2 = geo.Edges.FirstOrDefault(x => x.Name == "e2");
            var c1 = exchange.GetComponents(oe1).ToList()[0];
            var c2 = exchange.GetComponents(oe2).ToList()[0];
            var e1 = geoCopy.Edges.FirstOrDefault(x => x.Name == "v1");
            var e2 = geoCopy.Edges.FirstOrDefault(x => x.Name == "v2");
            Assert.IsTrue(c1.Name == "ev1");
            Assert.IsTrue(c2.Name == "ev2");

            var replacementTracker = new ModelCleanupAlgorithms.ReplacementTracker<Edge>();

            var splitCount = ModelCleanupAlgorithms.SplitEdgeVertexIntersections(geoCopy, 0.1, ref vertexGrid, ref edgeGrid, replacementTracker);

            var replaced = replacementTracker.GetReplacements();
            Assert.AreEqual(1, splitCount);
            Assert.AreEqual(1, replaced.Count);
            Assert.IsFalse(geoCopy.Edges.Contains(replaced[0].oldGeom));
            foreach (var r in replaced[0].newGeom)
            {
                Assert.IsTrue(geoCopy.Edges.Contains(r));
            }

            var geometryToReassign = new List<(BaseGeometry geom, SimComponent comp)>();
            var geometryToUnassign = new HashSet<SimInstancePlacementGeometry>();

            ModelCleanupAlgorithms.ReassignComponentsAfterReplacement(replaced, geo.Edges, exchange, geometryToReassign, geometryToUnassign);

            // check if un/reassignments are correct
            Assert.AreEqual(2, geometryToReassign.Count);
            Assert.AreEqual(1, geometryToUnassign.Count);

            Assert.AreEqual(2, geometryToReassign.Count(x => x.comp == c1));
            Assert.AreEqual(0, geometryToReassign.Count(x => x.comp == c2));

            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == oe1.Id && x.Instance.Component == c1));
            Assert.IsFalse(geometryToUnassign.Any(x => x.GeometryId == oe2.Id && x.Instance.Component == c2));

            // do the un/reassignment
            // first replace geometry
            DoReAssociation(geoCopy, gm, geometryToReassign, geometryToUnassign);

            var replacing = replaced.First(x => x.oldGeom.Id == oe1.Id).newGeom;
            Assert.AreEqual(2, replacing.Count);
            foreach (var r in replacing)
            {
                var ec = exchange.GetComponents(r);
                Assert.IsTrue(ec.All(x => x == c1));
            }
        }
        
        [TestMethod]
        public void TestFaceFaceSplitRestoresComponentAssociations()
        {
            LoadProject(testProject);
            var exchange = projectData.ComponentGeometryExchange;
            AABBGrid vertexGrid = null;
            AABBGrid edgeGrid = null;
            AABBGrid faceGrid = null;
            (var gm, var resource) = ProjectUtils.LoadGeometry("face_face_split.simgeo", projectData, sp);

            var geo = gm.Geometry;
            var geoCopy = geo.Clone();

            Assert.AreEqual(8, geoCopy.Vertices.Count);
            Assert.AreEqual(8, geoCopy.Edges.Count);
            Assert.AreEqual(2, geoCopy.Faces.Count);

            var of1 = geo.Faces.FirstOrDefault(x => x.Name == "f1");
            var of2 = geo.Faces.FirstOrDefault(x => x.Name == "f2");
            var c1 = exchange.GetComponents(of1).ToList()[0];
            var c2 = exchange.GetComponents(of2).ToList()[0];
            var e1 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f1");
            var e2 = geoCopy.Faces.FirstOrDefault(x => x.Name == "f2");
            Assert.IsTrue(c1.Name == "ff1");
            Assert.IsTrue(c2.Name == "ff2");

            var mergeTracker = new ModelCleanupAlgorithms.MergeTracker<Face>();
            var edgeTracker = new ModelCleanupAlgorithms.ReplacementTracker<Edge>();
            var replacementTracker = new ModelCleanupAlgorithms.ReplacementTracker<Face>();

            int count = 0;
            bool changed = true;

            while (changed)
            {
                count += ModelCleanupAlgorithms.SplitEdgeEdgeIntersections(geoCopy, 0.1, ref edgeGrid, edgeTracker);
                count += ModelCleanupAlgorithms.RemoveDuplicateFaces(geoCopy, ref faceGrid);

                changed = count > 0;
                count = 0;
            }

            var splitResult = ModelCleanupAlgorithms.SplitFaces(geoCopy, 0.1, ref vertexGrid, ref faceGrid,"error", "{0} ({1})", replacementTracker);

            var replaced = replacementTracker.GetReplacements();
            Assert.IsTrue(splitResult.success);
            Assert.AreEqual(2, replaced.Count);
            for (int i = 0; i < replaced.Count; i++)
            {
                Assert.IsFalse(geoCopy.Faces.Contains(replaced[i].oldGeom));
                foreach (var r in replaced[i].newGeom)
                {
                    Assert.IsTrue(geoCopy.Faces.Contains(r));
                }
            }

            var mergedCount = ModelCleanupAlgorithms.RemoveDuplicateFaces(geoCopy, ref faceGrid, mergeTracker);

            var merged = mergeTracker.AsList();
            Assert.AreEqual(1, mergedCount);

            var geometryToReassign = new List<(BaseGeometry geom, SimComponent comp)>();
            var geometryToUnassign = new HashSet<SimInstancePlacementGeometry>();

            ModelCleanupAlgorithms.ReassignComponentsAfterReplacement(edgeTracker.GetReplacements(), geo.Edges, exchange, geometryToReassign, geometryToUnassign);
            ModelCleanupAlgorithms.ReassignComponentsAfterReplacement(replaced, geo.Faces, exchange, geometryToReassign, geometryToUnassign);
            ModelCleanupAlgorithms.ReassignComponentsAfterMerge(merged, geo.Faces, exchange, geometryToReassign, geometryToUnassign, replacementTracker);

            // check if un/reassignments are correct
            Assert.AreEqual(4, geometryToReassign.Count);
            Assert.AreEqual(2, geometryToUnassign.Count);

            Assert.AreEqual(2, geometryToReassign.Count(x => x.comp == c1));
            Assert.AreEqual(2, geometryToReassign.Count(x => x.comp == c2));

            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == of1.Id && x.Instance.Component == c1));
            Assert.IsTrue(geometryToUnassign.Any(x => x.GeometryId == of2.Id && x.Instance.Component == c2));

            // do the un/reassignment
            // first replace geometry
            DoReAssociation(geoCopy, gm, geometryToReassign, geometryToUnassign);

            geo = gm.Geometry;
            Assert.AreEqual(10, geo.Vertices.Count);
            Assert.AreEqual(12, geo.Edges.Count);
            Assert.AreEqual(3, geo.Faces.Count);

            foreach (var face in geo.Faces)
            {
                var comps = exchange.GetComponents(face).ToList();
                if(comps.Count == 1)
                {
                    Assert.IsTrue(comps[0] == c1 || comps[0] == c2);
                }
                else if(comps.Count == 2)
                {
                    Assert.IsTrue((comps[0] == c1 && comps[1] == c2) || (comps[0] == c2 && comps[1] == c1));
                }
                else
                {
                    Assert.Fail();
                }
            }
        }
    }
}
