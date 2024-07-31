using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Taxonomy
{
    [TestClass]
    public class TaxonomyLanguageTests
    {
        [TestMethod]
        public void AddLanguage()
        {
            var deatCulture = new CultureInfo("de-at");

            SimTaxonomy taxonomy = new SimTaxonomy();
            Assert.IsFalse(taxonomy.Languages.Contains(deatCulture));

            taxonomy.Languages.Add(deatCulture);

            Assert.IsTrue(taxonomy.Languages.Contains(deatCulture));
        }
        [TestMethod]
        public void AddDuplicateLanguage()
        {
            var deatCulture = new CultureInfo("de-at");

            SimTaxonomy taxonomy = new SimTaxonomy();
            taxonomy.Languages.Add(deatCulture);

            Assert.ThrowsException<ArgumentException>(() => { taxonomy.Languages.Add(deatCulture); });
        }
        [TestMethod]
        public void AddLanguageTaxonomy()
        {
            var deatCulture = new CultureInfo("de-at");

            SimTaxonomy taxonomy = new SimTaxonomy();
            Assert.IsFalse(taxonomy.Localization.Entries.ContainsKey(deatCulture));

            taxonomy.Languages.Add(deatCulture);

            Assert.IsTrue(taxonomy.Localization.Entries.ContainsKey(deatCulture));
        }
        [TestMethod]
        public void AddLanguageEntry()
        {
            var deatCulture = new CultureInfo("de-at");

            SimTaxonomy taxonomy = new SimTaxonomy();
            SimTaxonomyEntry entry = new SimTaxonomyEntry("demokey");
            taxonomy.Entries.Add(entry);

            Assert.IsFalse(entry.Localization.Entries.ContainsKey(deatCulture));

            taxonomy.Languages.Add(deatCulture);

            Assert.IsTrue(entry.Localization.Entries.ContainsKey(deatCulture));
        }
        [TestMethod]
        public void AddLanguageBeforeEntry()
        {
            var deatCulture = new CultureInfo("de-at");

            SimTaxonomy taxonomy = new SimTaxonomy();
            taxonomy.Languages.Add(deatCulture);

            SimTaxonomyEntry entry = new SimTaxonomyEntry("demokey");
            taxonomy.Entries.Add(entry);
            Assert.IsTrue(entry.Localization.Entries.ContainsKey(deatCulture));
        }


        [TestMethod]
        public void RemoveLanguage()
        {
            var deatCulture = new CultureInfo("de-at");

            SimTaxonomy taxonomy = new SimTaxonomy();
            taxonomy.Languages.Add(deatCulture);

            taxonomy.Languages.Remove(deatCulture);
            Assert.IsFalse(taxonomy.Languages.Contains(deatCulture));
        }
        [TestMethod]
        public void RemoveLanguageTaxonomy()
        {
            var deatCulture = new CultureInfo("de-at");

            SimTaxonomy taxonomy = new SimTaxonomy();
            taxonomy.Languages.Add(deatCulture);

            taxonomy.Languages.Remove(deatCulture);
            Assert.IsFalse(taxonomy.Localization.Entries.ContainsKey(deatCulture));
        }
        [TestMethod]
        public void RemoveLanguageEntry()
        {
            var deatCulture = new CultureInfo("de-at");

            SimTaxonomy taxonomy = new SimTaxonomy();
            SimTaxonomyEntry entry = new SimTaxonomyEntry("demokey");
            taxonomy.Entries.Add(entry);

            taxonomy.Languages.Add(deatCulture);
            taxonomy.Languages.Remove(deatCulture);

            Assert.IsFalse(entry.Localization.Entries.ContainsKey(deatCulture));
        }


        [TestMethod]
        public void ClearLanguage()
        {
            var deatCulture = new CultureInfo("de-at");
            var enCulture = new CultureInfo("en");

            SimTaxonomy taxonomy = new SimTaxonomy();
            taxonomy.Languages.Add(deatCulture);
            taxonomy.Languages.Add(enCulture);

            Assert.AreEqual(2, taxonomy.Languages.Count);

            taxonomy.Languages.Clear();

            Assert.AreEqual(0, taxonomy.Languages.Count);
        }
        [TestMethod]
        public void ClearLanguageTaxonomy()
        {
            var deatCulture = new CultureInfo("de-at");
            var enCulture = new CultureInfo("en");

            SimTaxonomy taxonomy = new SimTaxonomy();
            taxonomy.Languages.Add(deatCulture);
            taxonomy.Languages.Add(enCulture);

            Assert.AreEqual(2, taxonomy.Localization.Entries.Keys.Count);

            taxonomy.Languages.Clear();

            Assert.AreEqual(0, taxonomy.Localization.Entries.Keys.Count);
        }
        [TestMethod]
        public void ClearLanguageEntry()
        {
            var deatCulture = new CultureInfo("de-at");
            var enCulture = new CultureInfo("en");

            SimTaxonomy taxonomy = new SimTaxonomy();
            SimTaxonomyEntry entry = new SimTaxonomyEntry("demokey");
            taxonomy.Entries.Add(entry);

            taxonomy.Languages.Add(deatCulture);
            taxonomy.Languages.Add(enCulture);

            Assert.AreEqual(2, entry.Localization.Entries.Keys.Count);

            taxonomy.Languages.Clear();

            Assert.AreEqual(0, entry.Localization.Entries.Keys.Count);
        }


        [TestMethod]
        public void AddEntryWithWrongLanguage()
        {
            var deatCulture = new CultureInfo("de-at");
            var enCulture = new CultureInfo("en");

            SimTaxonomy taxonomy = new SimTaxonomy();
            taxonomy.Languages.Add(deatCulture);

            SimTaxonomyEntry entry = new SimTaxonomyEntry("demokey");
            entry.Localization.SetLanguage(enCulture, "enname", "endescr");

            Assert.ThrowsException<NotSupportedException>(() => { taxonomy.Entries.Add(entry); });
        }
        [TestMethod]
        public void AddChildEntryWithWrongLanguage()
        {
            var deatCulture = new CultureInfo("de-at");
            var enCulture = new CultureInfo("en");

            SimTaxonomy taxonomy = new SimTaxonomy();
            SimTaxonomyEntry entry = new SimTaxonomyEntry("demokey");
            taxonomy.Entries.Add(entry);

            taxonomy.Languages.Add(deatCulture);

            SimTaxonomyEntry entry2 = new SimTaxonomyEntry("childkey");
            entry2.Localization.SetLanguage(enCulture, "enname", "endescr");

            Assert.ThrowsException<NotSupportedException>(() => { entry.Children.Add(entry2); });
        }

        [TestMethod]
        public void SetEntryWithWrongLanguage()
        {
            var deatCulture = new CultureInfo("de-at");
            var enCulture = new CultureInfo("en");

            SimTaxonomy taxonomy = new SimTaxonomy();
            var oldEntry = new SimTaxonomyEntry("key");
            taxonomy.Entries.Add(oldEntry);
            taxonomy.Languages.Add(deatCulture);

            SimTaxonomyEntry entry = new SimTaxonomyEntry("demokey");
            entry.Localization.SetLanguage(enCulture, "enname", "endescr");

            Assert.ThrowsException<NotSupportedException>(() => { taxonomy.Entries[0] = entry; });
        }
        [TestMethod]
        public void SetChildEntryWithWrongLanguage()
        {
            var deatCulture = new CultureInfo("de-at");
            var enCulture = new CultureInfo("en");

            SimTaxonomy taxonomy = new SimTaxonomy();
            SimTaxonomyEntry entry = new SimTaxonomyEntry("demokey");
            taxonomy.Entries.Add(entry);
            SimTaxonomyEntry oldEntry2 = new SimTaxonomyEntry("oldkey");
            entry.Children.Add(oldEntry2);

            taxonomy.Languages.Add(deatCulture);

            SimTaxonomyEntry entry2 = new SimTaxonomyEntry("childkey");
            entry2.Localization.SetLanguage(enCulture, "enname", "endescr");

            Assert.ThrowsException<NotSupportedException>(() => { entry.Children[0] = entry2; });
        }
    }
}
