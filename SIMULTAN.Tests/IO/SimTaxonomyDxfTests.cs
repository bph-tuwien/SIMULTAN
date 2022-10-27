using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.TXDXF;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data;
using System.IO;
using SIMULTAN.Projects;
using System.Text;
using SIMULTAN.Tests.Utils;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.Properties;
using System.Linq;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class SimTaxonomyDxfTests 
    {
        private static string Taxonomy1Key = "TaxKey 1";
        private static string Taxonomy1Name = "Taxonomy 1";
        private static long Taxonomy1LocalID = 1;
        private static string Taxonomy2Key = "TaxKey 2";
        private static string Taxonomy2Name = "Taxonomy 2";
        private static long Taxonomy2LocalID = 2;
        private static string TaxonomyEntry1Key = "Key 1";
        private static string TaxonomyEntry1Name = "Entry 1";
        private static long TaxonomyEntry1LocalID = 3;
        private static string TaxonomyEntry2Key = "Key 2";
        private static string TaxonomyEntry2Name = "Entry 2";
        private static long TaxonomyEntry2LocalID = 4;
        private static string TaxonomyEntry3Key = "Key 3";
        private static string TaxonomyEntry3Name = "Entry 3";
        private static long TaxonomyEntry3LocalID = 5;
        private static string Description = "Description";

        [TestMethod]
        public void WriteEmptyTaxonomy()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            var tax = new SimTaxonomy(Taxonomy1Key, Taxonomy1Name, Description) { IsReadonly = true};
            projectData.Taxonomies.Add(tax);
            var tax2 = new SimTaxonomy(Taxonomy2Key, Taxonomy2Name, null);
            projectData.Taxonomies.Add(tax2);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimTaxonomyDxfIO.Write(writer, new SimTaxonomy[] { tax, tax2}, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_TXDXF_Empty, exportedString);
        }

        [TestMethod]
        public void ReadEmptyTaxonomy()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            Assert.AreEqual(0, projectData.Taxonomies.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_TXDXF_Empty)))
            {
                SimTaxonomyDxfIO.Read(reader, new DXFParserInfo(projectData.Taxonomies.CalledFromLocation.GlobalID, projectData));
            }

            Assert.AreEqual(2, projectData.Taxonomies.Count);

            var tax = projectData.Taxonomies[0];
            Assert.AreEqual(Taxonomy1Key, tax.Key);
            Assert.AreEqual(Taxonomy1Name, tax.Name);
            Assert.AreEqual(true, tax.IsReadonly);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy1LocalID, tax.LocalID);
            tax = projectData.Taxonomies[1];
            Assert.AreEqual(Taxonomy2Key, tax.Key);
            Assert.AreEqual(Taxonomy2Name, tax.Name);
            Assert.AreEqual(false, tax.IsReadonly);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy2LocalID, tax.LocalID);
        }

        [TestMethod]
        public void WriteTaxonomyWithEntries()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            var tax = new SimTaxonomy(Taxonomy1Key, Taxonomy1Name, Description);
            projectData.Taxonomies.Add(tax);
            var tax2 = new SimTaxonomy(Taxonomy2Key, Taxonomy2Name, null);
            projectData.Taxonomies.Add(tax2);

            var entry1 = new SimTaxonomyEntry() { Key = TaxonomyEntry1Key, Name = TaxonomyEntry1Name };
            var entry2 = new SimTaxonomyEntry() { Key = TaxonomyEntry2Key, Name = TaxonomyEntry2Name, Description = Description };
            var entry3 = new SimTaxonomyEntry() { Key = TaxonomyEntry3Key, Name = TaxonomyEntry3Name };

            entry1.Children.Add(entry2);
            entry1.Children.Add(entry3);
            tax.Entries.Add(entry1);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimTaxonomyDxfIO.Write(writer, new SimTaxonomy[] { tax, tax2}, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_TXDXF_Entries, exportedString);
        }

        [TestMethod]
        public void ReadTaxonomyWithEntries()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_TXDXF_Entries)))
            {
                SimTaxonomyDxfIO.Read(reader, new DXFParserInfo(projectData.Taxonomies.CalledFromLocation.GlobalID, projectData));
            }

            Assert.AreEqual(2, projectData.Taxonomies.Count);

            var tax = projectData.Taxonomies[0];
            Assert.AreEqual(Taxonomy1Key, tax.Key);
            Assert.AreEqual(Taxonomy1Name, tax.Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy1LocalID, tax.LocalID);

            Assert.AreEqual(1, tax.Entries.Count);
            var entry = tax.Entries[0];
            Assert.AreEqual(TaxonomyEntry1Key, entry.Key);
            Assert.AreEqual(TaxonomyEntry1Name, entry.Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry.GlobalID);
            Assert.AreEqual(TaxonomyEntry1LocalID, entry.LocalID);

            Assert.AreEqual(2, entry.Children.Count);
            var entry2 = entry.Children[0];
            Assert.AreEqual(TaxonomyEntry2Key, entry2.Key);
            Assert.AreEqual(TaxonomyEntry2Name, entry2.Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry2.GlobalID);
            Assert.AreEqual(TaxonomyEntry2LocalID, entry2.LocalID);
            Assert.AreEqual(0, entry2.Children.Count);
            entry2 = entry.Children[1];
            Assert.AreEqual(TaxonomyEntry3Key, entry2.Key);
            Assert.AreEqual(TaxonomyEntry3Name, entry2.Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry2.GlobalID);
            Assert.AreEqual(TaxonomyEntry3LocalID, entry2.LocalID);
            Assert.AreEqual(0, entry2.Children.Count);

            tax = projectData.Taxonomies[1];
            Assert.AreEqual(Taxonomy2Key, tax.Key);
            Assert.AreEqual(Taxonomy2Name, tax.Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy2LocalID, tax.LocalID);
        }
    }
}
