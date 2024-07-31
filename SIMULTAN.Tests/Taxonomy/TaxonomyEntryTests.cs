using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Taxonomy
{
    [TestClass]
    public class TaxonomyEntryTests
    {
        [TestMethod]
        public void Ctor()
        {
            var id = new SimId(Guid.NewGuid(), 2233);
            var culture = CultureInfo.GetCultureInfo("de-AT");

            var entry = new SimTaxonomyEntry("key");
            Assert.AreEqual(SimId.Empty, entry.Id);
            Assert.AreEqual("key", entry.Key);
            Assert.AreEqual(null, entry.Parent);
            Assert.AreEqual(null, entry.Taxonomy);
            Assert.AreEqual(0, entry.Children.Count);
            Assert.AreEqual(0, entry.Localization.Entries.Count);

            entry = new SimTaxonomyEntry(id, "key");
            Assert.AreEqual(id, entry.Id);
            Assert.AreEqual("key", entry.Key);
            Assert.AreEqual(null, entry.Parent);
            Assert.AreEqual(null, entry.Taxonomy);
            Assert.AreEqual(0, entry.Children.Count);
            Assert.AreEqual(0, entry.Localization.Entries.Count);

            entry = new SimTaxonomyEntry("key", "name", "desc", culture);
            Assert.AreEqual(SimId.Empty, entry.Id);
            Assert.AreEqual("key", entry.Key);
            Assert.AreEqual(null, entry.Parent);
            Assert.AreEqual(null, entry.Taxonomy);
            Assert.AreEqual(0, entry.Children.Count);
            Assert.AreEqual(1, entry.Localization.Entries.Count);
            Assert.IsTrue(entry.Localization.Entries.ContainsKey(culture));
            Assert.AreEqual("name", entry.Localization.Entries[culture].Name);
            Assert.AreEqual("desc", entry.Localization.Entries[culture].Description);
            Assert.AreEqual(culture, entry.Localization.Entries[culture].Culture);

            entry = new SimTaxonomyEntry(id, "key", "name", "desc", culture);
            Assert.AreEqual(id, entry.Id);
            Assert.AreEqual("key", entry.Key);
            Assert.AreEqual(null, entry.Parent);
            Assert.AreEqual(null, entry.Taxonomy);
            Assert.AreEqual(0, entry.Children.Count);
            Assert.AreEqual(1, entry.Localization.Entries.Count);
            Assert.IsTrue(entry.Localization.Entries.ContainsKey(culture));
            Assert.AreEqual("name", entry.Localization.Entries[culture].Name);
            Assert.AreEqual("desc", entry.Localization.Entries[culture].Description);
            Assert.AreEqual(culture, entry.Localization.Entries[culture].Culture);
        }

        [TestMethod]
        public void CtorException()
        {
            //Key null
            Assert.ThrowsException<ArgumentException>(() => { var entry = new SimTaxonomyEntry(null); });
            Assert.ThrowsException<ArgumentException>(() => { var entry = new SimTaxonomyEntry(new SimId(Guid.NewGuid(), 22), null); });
            Assert.ThrowsException<ArgumentException>(() => { var entry = new SimTaxonomyEntry(null, "name"); });
            Assert.ThrowsException<ArgumentException>(() => { var entry = new SimTaxonomyEntry(new SimId(Guid.NewGuid(), 22), null, "name"); });

            //Key empty
            Assert.ThrowsException<ArgumentException>(() => { var entry = new SimTaxonomyEntry(""); });
            Assert.ThrowsException<ArgumentException>(() => { var entry = new SimTaxonomyEntry(new SimId(Guid.NewGuid(), 22), ""); });
            Assert.ThrowsException<ArgumentException>(() => { var entry = new SimTaxonomyEntry("", "name"); });
            Assert.ThrowsException<ArgumentException>(() => { var entry = new SimTaxonomyEntry(new SimId(Guid.NewGuid(), 22), "", "name"); });
        }

        #region Properties

        [TestMethod]
        public void PropertyKey()
        {
            var entry = new SimTaxonomyEntry("key");

            Assert.ThrowsException<ArgumentException>(() => { entry.Key = null; });
            Assert.ThrowsException<ArgumentException>(() => { entry.Key = ""; });

            entry.Key = "key2";
            Assert.AreEqual("key2", entry.Key);
        }

        [TestMethod]
        public void PropertyKeyChanged()
        {
            var entry = new SimTaxonomyEntry("key");
            PropertyChangedEventCounter cc = new PropertyChangedEventCounter(entry);

            entry.Key = "key2";
            Assert.AreEqual(1, cc.PropertyChangedArgs.Count);
            Assert.AreEqual(nameof(entry.Key), cc.PropertyChangedArgs[0]);
        }

        #endregion
    }
}
