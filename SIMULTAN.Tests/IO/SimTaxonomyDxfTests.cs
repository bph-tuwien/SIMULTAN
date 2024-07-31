using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.TXDXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using SIMULTAN.Utils.Streams;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class SimTaxonomyDxfTests
    {
        private static CultureInfo de = CultureInfo.GetCultureInfo("de");

        private static string Taxonomy1Key = "TaxKey 1";
        private static string Taxonomy1Name = "Taxonomy 1";
        private static string Taxonomy1NameDe = "Taxonomy 1 de";
        private static long Taxonomy1LocalID = 1;
        private static string Taxonomy2Key = "TaxKey 2";
        private static string Taxonomy2Name = "Taxonomy 2";
        private static string Taxonomy2NameDe = "Taxonomy 2 de";
        private static long Taxonomy2LocalID = 2;
        private static string TaxonomyEntry1Key = "Key 1";
        private static string TaxonomyEntry1Name = "Entry 1";
        private static long TaxonomyEntry1LocalID = 3;
        private static string TaxonomyEntry2Key = "Key 2";
        private static string TaxonomyEntry2Name = "Entry 2";
        private static string TaxonomyEntry2NameDe = "Entry 2 de";
        private static long TaxonomyEntry2LocalID = 4;
        private static string TaxonomyEntry3Key = "Key 3";
        private static string TaxonomyEntry3Name = "Entry 3";
        private static string TaxonomyEntry3NameDe = "Entry 3 de";
        private static long TaxonomyEntry3LocalID = 5;
        private static string Description = "Description";
        private static string DescriptionDe = "Description de";

        [TestMethod]
        public void WriteEmptyTaxonomy()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            var tax = new SimTaxonomy(Taxonomy1Key, Taxonomy1Name, Description) { IsReadonly = true };
            projectData.Taxonomies.Add(tax);
            var tax2 = new SimTaxonomy(Taxonomy2Key, Taxonomy2Name, "");
            tax2.Languages.Add(de);
            tax2.Localization.SetLanguage(de, Taxonomy2NameDe, DescriptionDe);
            projectData.Taxonomies.Add(tax2);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimTaxonomyDxfIO.Write(writer, new SimTaxonomy[] { tax, tax2 }, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_TXDXF_WriteEmpty, exportedString);
        }

        [TestMethod]
        public void ReadEmptyTaxonomyV23()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            Assert.AreEqual(0, projectData.Taxonomies.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_TXDXF_ReadEmptyV23)))
            {
                SimTaxonomyDxfIO.Read(reader, new DXFParserInfo(projectData.Taxonomies.CalledFromLocation.GlobalID, projectData));
            }

            Assert.AreEqual(2, projectData.Taxonomies.Count);

            var tax = projectData.Taxonomies[0];
            Assert.AreEqual(Taxonomy1Key, tax.Key);
            Assert.AreEqual(Taxonomy1Name, tax.Localization.Localize().Name);
            Assert.AreEqual(1, tax.Languages.Count);
            Assert.IsTrue(tax.Languages.Contains(CultureInfo.InvariantCulture));
            Assert.AreEqual(1, tax.Localization.Entries.Count);
            var entry = tax.Localization.Entries[CultureInfo.InvariantCulture];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, Taxonomy1Name, Description), entry);
            Assert.AreEqual(true, tax.IsReadonly);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy1LocalID, tax.LocalID);
            Assert.AreEqual(tax, projectData.IdGenerator.GetById<SimTaxonomy>(tax.Id));
            tax = projectData.Taxonomies[1];
            Assert.AreEqual(Taxonomy2Key, tax.Key);
            Assert.AreEqual(Taxonomy2Name, tax.Localization.Localize().Name);
            Assert.AreEqual(2, tax.Languages.Count);
            Assert.IsTrue(tax.Languages.Contains(CultureInfo.InvariantCulture));
            Assert.IsTrue(tax.Languages.Contains(de));
            Assert.AreEqual(2, tax.Localization.Entries.Count);
            entry = tax.Localization.Entries[CultureInfo.InvariantCulture];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, Taxonomy2Name, ""), entry);
            entry = tax.Localization.Entries[de];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(de, Taxonomy2NameDe, DescriptionDe), entry);
            Assert.AreEqual(false, tax.IsReadonly);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy2LocalID, tax.LocalID);
            Assert.AreEqual(tax, projectData.IdGenerator.GetById<SimTaxonomy>(tax.Id));
        }
        [TestMethod]
        public void ReadEmptyTaxonomyV18()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            Assert.AreEqual(0, projectData.Taxonomies.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_TXDXF_ReadEmptyV18)))
            {
                SimTaxonomyDxfIO.Read(reader, new DXFParserInfo(projectData.Taxonomies.CalledFromLocation.GlobalID, projectData));
            }

            Assert.AreEqual(2, projectData.Taxonomies.Count);

            var tax = projectData.Taxonomies[0];
            Assert.AreEqual(Taxonomy1Key, tax.Key);
            Assert.AreEqual(Taxonomy1Name, tax.Localization.Localize().Name);
            Assert.AreEqual(true, tax.IsReadonly);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy1LocalID, tax.LocalID);
            Assert.AreEqual(tax, projectData.IdGenerator.GetById<SimTaxonomy>(tax.Id));
            tax = projectData.Taxonomies[1];
            Assert.AreEqual(Taxonomy2Key, tax.Key);
            Assert.AreEqual(Taxonomy2Name, tax.Localization.Localize().Name);
            Assert.AreEqual(false, tax.IsReadonly);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy2LocalID, tax.LocalID);
            Assert.AreEqual(tax, projectData.IdGenerator.GetById<SimTaxonomy>(tax.Id));
        }

        [TestMethod]
        public void WriteTaxonomyWithEntries()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            var tax = new SimTaxonomy(Taxonomy1Key, Taxonomy1Name, Description);
            projectData.Taxonomies.Add(tax);
            tax.Languages.Add(de);
            tax.Localization.SetLanguage(de, Taxonomy1NameDe, DescriptionDe);
            var tax2 = new SimTaxonomy(Taxonomy2Key, Taxonomy2Name, "");
            projectData.Taxonomies.Add(tax2);

            var entry1 = new SimTaxonomyEntry(TaxonomyEntry1Key, TaxonomyEntry1Name);
            var entry2 = new SimTaxonomyEntry(TaxonomyEntry2Key, TaxonomyEntry2Name, Description);
            var entry3 = new SimTaxonomyEntry(TaxonomyEntry3Key, TaxonomyEntry3Name);

            entry1.Children.Add(entry2);
            entry1.Children.Add(entry3);
            tax.Entries.Add(entry1);

            entry2.Localization.SetLanguage(de, TaxonomyEntry2NameDe, DescriptionDe);
            entry3.Localization.SetLanguage(de, TaxonomyEntry3NameDe, DescriptionDe);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimTaxonomyDxfIO.Write(writer, new SimTaxonomy[] { tax, tax2 }, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_TXDXF_WriteEntries, exportedString);
        }

        [TestMethod]
        public void ReadTaxonomyWithEntriesV23()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_TXDXF_ReadEntriesV23)))
            {
                SimTaxonomyDxfIO.Read(reader, new DXFParserInfo(projectData.Taxonomies.CalledFromLocation.GlobalID, projectData));
            }

            Assert.AreEqual(2, projectData.Taxonomies.Count);

            var tax = projectData.Taxonomies[0];
            Assert.AreEqual(Taxonomy1Key, tax.Key);
            Assert.AreEqual(Taxonomy1Name, tax.Localization.Localize().Name);
            Assert.AreEqual(2, tax.Languages.Count);
            Assert.IsTrue(tax.Languages.Contains(CultureInfo.InvariantCulture));
            Assert.IsTrue(tax.Languages.Contains(de));
            Assert.AreEqual(2, tax.Localization.Entries.Count);
            var loc = tax.Localization.Entries[CultureInfo.InvariantCulture];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, Taxonomy1Name, Description), loc);
            loc = tax.Localization.Entries[de];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(de, Taxonomy1NameDe, DescriptionDe), loc);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy1LocalID, tax.LocalID);
            Assert.AreEqual(tax, projectData.IdGenerator.GetById<SimTaxonomy>(tax.Id));

            Assert.AreEqual(1, tax.Entries.Count);
            var entry = tax.Entries[0];
            Assert.AreEqual(TaxonomyEntry1Key, entry.Key);
            Assert.AreEqual(TaxonomyEntry1Name, entry.Localization.Localize().Name);
            loc = entry.Localization.Entries[CultureInfo.InvariantCulture];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, TaxonomyEntry1Name, ""), loc);
            loc = entry.Localization.Entries[de];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(de, "", ""), loc);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry.GlobalID);
            Assert.AreEqual(TaxonomyEntry1LocalID, entry.LocalID);
            Assert.AreEqual(entry, projectData.IdGenerator.GetById<SimTaxonomyEntry>(entry.Id));

            Assert.AreEqual(2, entry.Children.Count);
            var entry2 = entry.Children[0];
            Assert.AreEqual(TaxonomyEntry2Key, entry2.Key);
            Assert.AreEqual(TaxonomyEntry2Name, entry2.Localization.Localize().Name);
            loc = entry2.Localization.Entries[CultureInfo.InvariantCulture];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, TaxonomyEntry2Name, Description), loc);
            loc = entry2.Localization.Entries[de];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(de, TaxonomyEntry2NameDe, DescriptionDe), loc);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry2.GlobalID);
            Assert.AreEqual(TaxonomyEntry2LocalID, entry2.LocalID);
            Assert.AreEqual(entry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(entry2.Id));
            Assert.AreEqual(0, entry2.Children.Count);
            entry2 = entry.Children[1];
            Assert.AreEqual(TaxonomyEntry3Key, entry2.Key);
            Assert.AreEqual(TaxonomyEntry3Name, entry2.Localization.Localize().Name);
            loc = entry2.Localization.Entries[CultureInfo.InvariantCulture];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, TaxonomyEntry3Name, ""), loc);
            loc = entry2.Localization.Entries[de];
            Assert.AreEqual(new SimTaxonomyLocalizationEntry(de, TaxonomyEntry3NameDe, DescriptionDe), loc);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry2.GlobalID);
            Assert.AreEqual(TaxonomyEntry3LocalID, entry2.LocalID);
            Assert.AreEqual(0, entry2.Children.Count);
            Assert.AreEqual(entry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(entry2.Id));

            tax = projectData.Taxonomies[1];
            Assert.AreEqual(Taxonomy2Key, tax.Key);
            Assert.AreEqual(Taxonomy2Name, tax.Localization.Localize().Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy2LocalID, tax.LocalID);
            Assert.AreEqual(tax, projectData.IdGenerator.GetById<SimTaxonomy>(tax.Id));
        }
        [TestMethod]
        public void ReadTaxonomyWithEntriesV18()
        {
            var projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(Guid.NewGuid()));

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_TXDXF_ReadEntriesV18)))
            {
                SimTaxonomyDxfIO.Read(reader, new DXFParserInfo(projectData.Taxonomies.CalledFromLocation.GlobalID, projectData));
            }

            Assert.AreEqual(2, projectData.Taxonomies.Count);

            var tax = projectData.Taxonomies[0];
            Assert.AreEqual(Taxonomy1Key, tax.Key);
            Assert.AreEqual(Taxonomy1Name, tax.Localization.Localize().Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy1LocalID, tax.LocalID);
            Assert.AreEqual(tax, projectData.IdGenerator.GetById<SimTaxonomy>(tax.Id));

            Assert.AreEqual(1, tax.Entries.Count);
            var entry = tax.Entries[0];
            Assert.AreEqual(TaxonomyEntry1Key, entry.Key);
            Assert.AreEqual(TaxonomyEntry1Name, entry.Localization.Localize().Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry.GlobalID);
            Assert.AreEqual(TaxonomyEntry1LocalID, entry.LocalID);
            Assert.AreEqual(entry, projectData.IdGenerator.GetById<SimTaxonomyEntry>(entry.Id));

            Assert.AreEqual(2, entry.Children.Count);
            var entry2 = entry.Children[0];
            Assert.AreEqual(TaxonomyEntry2Key, entry2.Key);
            Assert.AreEqual(TaxonomyEntry2Name, entry2.Localization.Localize().Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry2.GlobalID);
            Assert.AreEqual(TaxonomyEntry2LocalID, entry2.LocalID);
            Assert.AreEqual(entry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(entry2.Id));
            Assert.AreEqual(0, entry2.Children.Count);
            entry2 = entry.Children[1];
            Assert.AreEqual(TaxonomyEntry3Key, entry2.Key);
            Assert.AreEqual(TaxonomyEntry3Name, entry2.Localization.Localize().Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, entry2.GlobalID);
            Assert.AreEqual(TaxonomyEntry3LocalID, entry2.LocalID);
            Assert.AreEqual(0, entry2.Children.Count);
            Assert.AreEqual(entry2, projectData.IdGenerator.GetById<SimTaxonomyEntry>(entry2.Id));

            tax = projectData.Taxonomies[1];
            Assert.AreEqual(Taxonomy2Key, tax.Key);
            Assert.AreEqual(Taxonomy2Name, tax.Localization.Localize().Name);
            Assert.AreEqual(projectData.Taxonomies.CalledFromLocation.GlobalID, tax.GlobalID);
            Assert.AreEqual(Taxonomy2LocalID, tax.LocalID);
            Assert.AreEqual(tax, projectData.IdGenerator.GetById<SimTaxonomy>(tax.Id));
        }
    }
}
