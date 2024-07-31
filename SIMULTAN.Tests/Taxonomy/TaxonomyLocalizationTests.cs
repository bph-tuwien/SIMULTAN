using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SIMULTAN.Tests.Taxonomy
{
    [TestClass]
    public class TaxonomyLocalizationTests
    {
        private class NotifyChangedHelper
        {
            public int ChangedCount = 0;

            public void OnChanged(object sender, EventArgs args)
            {
                Assert.IsTrue(sender is SimTaxonomyLocalization);
                ChangedCount++;
            }
        }

        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { new SimTaxonomyLocalization(null); });
        }

        [TestMethod]
        public void AddLanguageTest()
        {
            SimTaxonomyEntry taxEntry = new SimTaxonomyEntry("demokey");

            Assert.AreEqual(0, taxEntry.Localization.Entries.Count);

            taxEntry.Localization.AddLanguage(CultureInfo.InvariantCulture);

            Assert.AreEqual(1, taxEntry.Localization.Entries.Count);
            Assert.IsTrue(taxEntry.Localization.Entries.ContainsKey(CultureInfo.InvariantCulture));

            var entry = taxEntry.Localization.Entries[CultureInfo.InvariantCulture];
            Assert.AreEqual(CultureInfo.InvariantCulture, entry.Culture);
            Assert.AreEqual("", entry.Name);
            Assert.AreEqual("", entry.Description);

            var culture = CultureInfo.GetCultureInfo("en");
            taxEntry.Localization.AddLanguage(culture);
            Assert.AreEqual(2, taxEntry.Localization.Entries.Count);

            entry = taxEntry.Localization.Entries[culture];
            Assert.AreEqual(culture, entry.Culture);
            Assert.AreEqual("", entry.Name);
            Assert.AreEqual("", entry.Description);
        }

        [TestMethod]
        public void AddDuplicateLanguageTest()
        {
            SimTaxonomyEntry taxEntry = new SimTaxonomyEntry("demokey");

            taxEntry.Localization.AddLanguage(CultureInfo.InvariantCulture);
            // should not do anything
            taxEntry.Localization.AddLanguage(CultureInfo.InvariantCulture);
            Assert.AreEqual(1, taxEntry.Localization.Entries.Count);
        }

        [TestMethod]
        public void UpdateLanguageTest()
        {
            SimTaxonomyEntry taxEntry = new SimTaxonomyEntry("demokey");
            var culture = CultureInfo.InvariantCulture;

            taxEntry.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(culture, "TestName", "TestDescription"));
            var entry = taxEntry.Localization.Entries[culture];

            Assert.AreEqual(culture, entry.Culture);
            Assert.AreEqual("TestName", entry.Name);
            Assert.AreEqual("TestDescription", entry.Description);
        }

        [TestMethod]
        public void RemoveLanguageTest()
        {
            SimTaxonomyEntry taxEntry = new SimTaxonomyEntry("demokey");
            var culture = CultureInfo.InvariantCulture;
            var de = CultureInfo.GetCultureInfo("de");
            var en = CultureInfo.GetCultureInfo("en");
            taxEntry.Localization.AddLanguage(culture);
            taxEntry.Localization.AddLanguage(de);
            taxEntry.Localization.AddLanguage(en);

            Assert.AreEqual(3, taxEntry.Localization.Entries.Count);

            taxEntry.Localization.RemoveLanguage(de);

            Assert.AreEqual(2, taxEntry.Localization.Entries.Count);
            Assert.IsTrue(taxEntry.Localization.Entries.ContainsKey(culture));
            Assert.IsTrue(taxEntry.Localization.Entries.ContainsKey(en));

            taxEntry.Localization.RemoveLanguage(culture);
            taxEntry.Localization.RemoveLanguage(en);
            Assert.AreEqual(0, taxEntry.Localization.Entries.Count);
        }


        [TestMethod]
        public void IsIdenticalTest()
        {
            SimTaxonomyEntry taxEntry1 = new SimTaxonomyEntry("entry1");
            SimTaxonomyEntry taxEntry2 = new SimTaxonomyEntry("entry2");

            // empty should be identiceal
            Assert.IsTrue(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsTrue(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));

            var de = CultureInfo.GetCultureInfo("de");
            var en = CultureInfo.GetCultureInfo("en");

            taxEntry1.Localization.AddLanguage(de);
            taxEntry1.Localization.AddLanguage(en);

            // languages differ
            Assert.IsFalse(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsFalse(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));

            taxEntry2.Localization.AddLanguage(de);
            Assert.IsFalse(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsFalse(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));

            taxEntry2.Localization.AddLanguage(en);
            Assert.IsTrue(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsTrue(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));

            // different translations
            taxEntry1.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(de, "de name", "de description"));
            taxEntry1.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(en, "en name", "en description"));
            Assert.IsFalse(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsFalse(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));

            taxEntry2.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(de, "de name", "de description"));
            Assert.IsFalse(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsFalse(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));

            taxEntry2.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(en, "en name", "en description"));
            Assert.IsTrue(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsTrue(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));

            // name different
            taxEntry2.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(en, "en name 2", "en description"));
            Assert.IsFalse(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsFalse(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));

            // description different
            taxEntry2.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(en, "en name", "en description 2"));
            Assert.IsFalse(taxEntry1.Localization.IsIdenticalTo(taxEntry2.Localization));
            Assert.IsFalse(taxEntry2.Localization.IsIdenticalTo(taxEntry1.Localization));
        }

        [TestMethod]
        public void LocalizeTest()
        {
            SimTaxonomyEntry taxEntry = new SimTaxonomyEntry("key");
            var invariant = CultureInfo.InvariantCulture;
            var de = CultureInfo.GetCultureInfo("de");
            var deAt = CultureInfo.GetCultureInfo("de-at");
            var deDe = CultureInfo.GetCultureInfo("de-de");
            var deCh = CultureInfo.GetCultureInfo("de-ch");
            var en = CultureInfo.GetCultureInfo("en");
            var enGb = CultureInfo.GetCultureInfo("en-gb");
            var enUs = CultureInfo.GetCultureInfo("en-us");
            var jp = CultureInfo.GetCultureInfo("jp");

            var invariantEntry = new SimTaxonomyLocalizationEntry(invariant, "name inv", "description inv");
            var deEntry = new SimTaxonomyLocalizationEntry(de, "name de", "description de");
            var deAtEntry = new SimTaxonomyLocalizationEntry(deAt, "name deAt", "description deAt");
            var deDeEntry = new SimTaxonomyLocalizationEntry(deDe, "name deDe", "description deDe");
            var enEntry = new SimTaxonomyLocalizationEntry(en, "name en", "description en");
            var enGbEntry = new SimTaxonomyLocalizationEntry(enGb, "name enGb", "description enGb");

            taxEntry.Localization.AddLanguage(invariant);
            taxEntry.Localization.AddLanguage(de);
            taxEntry.Localization.AddLanguage(deAt);
            taxEntry.Localization.AddLanguage(deDe);
            taxEntry.Localization.AddLanguage(en);
            taxEntry.Localization.AddLanguage(enGb);

            taxEntry.Localization.SetLanguage(invariantEntry);
            taxEntry.Localization.SetLanguage(deEntry);
            taxEntry.Localization.SetLanguage(deAtEntry);
            taxEntry.Localization.SetLanguage(deDeEntry);
            taxEntry.Localization.SetLanguage(enEntry);
            taxEntry.Localization.SetLanguage(enGbEntry);

            Assert.AreEqual(invariantEntry, taxEntry.Localization.Localize());
            Assert.AreEqual(invariantEntry, taxEntry.Localization.Localize(invariant));
            Assert.AreEqual(invariantEntry, taxEntry.Localization.Localize(jp)); // not found should return invariant
            Assert.AreEqual(deEntry, taxEntry.Localization.Localize(de));
            Assert.AreEqual(deEntry, taxEntry.Localization.Localize(deCh)); // should return parent localization (de-ch -> de)
            Assert.AreEqual(deAtEntry, taxEntry.Localization.Localize(deAt));
            Assert.AreEqual(deDeEntry, taxEntry.Localization.Localize(deDe));
            Assert.AreEqual(enEntry, taxEntry.Localization.Localize(en));
            Assert.AreEqual(enEntry, taxEntry.Localization.Localize(enUs));
            Assert.AreEqual(enGbEntry, taxEntry.Localization.Localize(enGb));

            taxEntry.Localization.RemoveLanguage(invariant);

            Assert.AreEqual(taxEntry.Localization.Entries.First().Value, taxEntry.Localization.Localize(jp)); // not found should return first if no invariant
        }

        [TestMethod]
        public void NotifyChangedTest()
        {
            NotifyChangedHelper helper = new NotifyChangedHelper();

            SimTaxonomyEntry taxEntry = new SimTaxonomyEntry("key");

            var culture = CultureInfo.InvariantCulture;
            taxEntry.Localization.Changed += helper.OnChanged;

            Assert.AreEqual(0, helper.ChangedCount);

            taxEntry.Localization.AddLanguage(culture);
            Assert.AreEqual(1, helper.ChangedCount);

            taxEntry.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(culture, "TestName", "TestDescription"));
            Assert.AreEqual(2, helper.ChangedCount);

            taxEntry.Localization.RemoveLanguage(culture);
            Assert.AreEqual(3, helper.ChangedCount);
        }
    }
}
