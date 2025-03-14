using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.TXDXF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.TestUtils
{
    public static class TaxonomyUtils
    {
        private static readonly string TaxKey = "taxkey {0}";
        private static readonly string TaxName = "{1} tax {0} name {2}";
        private static readonly string TaxDesc = "{1} tax {0} desc {2}";

        private static readonly string EntryKey = "entrykey {0}";
        private static readonly string EntryName = "{1} entry {0} name {2}";
        private static readonly string EntryDesc = "{1} entry {0} desc {2}";

        /// <summary>
        /// Loads and returns the default taxonomies without loading a whole project.
        /// </summary>
        /// <returns>The default taxonomies</returns>
        public static SimTaxonomyCollection GetDefaultTaxonomies()
        {
            var assembly = Assembly.GetAssembly(typeof(SimTaxonomy));
            using (var default_tax_stream = assembly.GetManifestResourceStream("SIMULTAN.Data.Taxonomy.Default.default_taxonomies.txdxf"))
            {
                var dummyProjectData = new ExtendedProjectData();
                dummyProjectData.SetCallingLocation(new DummyReferenceLocation(Guid.Empty));
                var tmpParserInfo = new DXFParserInfo(Guid.Empty, dummyProjectData);
                SimTaxonomyDxfIO.Import(new DXFStreamReader(default_tax_stream), tmpParserInfo);
                return dummyProjectData.Taxonomies;
            }
        }

        public static void LoadDefaultTaxonomies(ExtendedProjectData projectData)
        {
            var assembly = Assembly.GetAssembly(typeof(SimTaxonomy));
            using (var default_tax_stream = assembly.GetManifestResourceStream("SIMULTAN.Data.Taxonomy.Default.default_taxonomies.txdxf"))
            {
                var tmpParserInfo = new DXFParserInfo(projectData.Taxonomies.CalledFromLocation.GlobalID, projectData);
                SimTaxonomyDxfIO.Import(new DXFStreamReader(default_tax_stream), tmpParserInfo);
            }
        }

        /// <summary>
        /// Quickly gets a single default slot taxonomy entry, only use when you do not need more than a single one.
        /// Otherwise get the taxonomies and then the entries manually.
        /// </summary>
        /// <param name="key">The slot taxonomy entry key</param>
        /// <returns>The taxonomy entry for the slot</returns>
        internal static SimTaxonomyEntry GetDefaultSlot(string key)
        {
            var taxonomies = GetDefaultTaxonomies();
            return taxonomies.GetDefaultSlot(key);
        }
        public static void AssertLocalization(SimTaxonomyEntry entry, int i, string tag, IEnumerable<string> locales)
        {
            foreach (var loc in GenEntryLoc(i, tag, locales))
            {
                Assert.IsTrue(entry.Localization.Entries.ContainsKey(loc.Culture));
                var l = entry.Localization.Entries[loc.Culture];
                Assert.AreEqual(loc, l);
            }
        }

        public static void AssertLocalization(SimTaxonomy tax, int i, string tag, IEnumerable<string> locales)
        {
            foreach (var loc in GenTaxonomyLoc(i, tag, locales))
            {
                Assert.IsTrue(tax.Languages.Contains(loc.Culture));
                Assert.IsTrue(tax.Localization.Entries.ContainsKey(loc.Culture));
                var l = tax.Localization.Entries[loc.Culture];
                Assert.AreEqual(loc, l);
            }
        }

        public static IEnumerable<SimTaxonomyLocalizationEntry> GenTaxonomyLoc(int i, string tag, IEnumerable<string> locales)
        {
            foreach (var loc in locales)
            {
                yield return new SimTaxonomyLocalizationEntry(new CultureInfo(loc),
                    string.Format(TaxName, i, loc, tag),
                    string.Format(TaxDesc, i, loc, tag));
            }
        }
        public static IEnumerable<SimTaxonomyLocalizationEntry> GenEntryLoc(int i, string tag, IEnumerable<string> locales)
        {
            foreach (var loc in locales)
            {
                yield return new SimTaxonomyLocalizationEntry(new CultureInfo(loc),
                    string.Format(EntryName, i, loc, tag),
                    string.Format(EntryDesc, i, loc, tag));
            }
        }

        public static SimTaxonomyEntry GenerateEntry(int i, string tag, IEnumerable<string> locales)
        {
            var entry = new SimTaxonomyEntry(string.Format(EntryKey, i));
            foreach (var loc in GenEntryLoc(i, tag, locales))
            {
                entry.Localization.AddLanguage(loc.Culture);
                entry.Localization.SetLanguage(loc);
            }
            return entry;
        }
        public static SimTaxonomy GenerateTaxonomy(int i, string tag, IEnumerable<string> locales)
        {
            var tax = new SimTaxonomy() { Key = string.Format(TaxKey, i) };
            foreach (var loc in GenTaxonomyLoc(i, tag, locales))
            {
                if (!tax.Languages.Contains(loc.Culture))
                    tax.Languages.Add(loc.Culture);
                tax.Localization.AddLanguage(loc.Culture);
                tax.Localization.SetLanguage(loc);
            }

            return tax;
        }
    }
}
