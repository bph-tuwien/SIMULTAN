using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Serializer.JSON;
using SIMULTAN.Tests.TestUtils;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Taxonomy;

[TestClass]
public class TaxonomyJSONTests : BaseProjectTest
{
    private static readonly FileInfo emptyProject = new FileInfo(@"./EmptyProject.simultan");

    private static readonly List<string> locales = new() { "", "en", "de" };
    private static readonly List<string> localesAlt = new() { "en", "de" };
    private void Load()
    {
        LoadProject(emptyProject);
    }

    private void AssertTranslations(SimTaxonomySerializable entry, IEnumerable<SimTaxonomyLocalizationEntry> loc)
    {
        foreach (var translation in loc)
        {
            if (translation.Culture.Name == "")
            {
                Assert.AreEqual(translation.Name, entry.Name);
                Assert.AreEqual(translation.Description, entry.Description);
            }
            else
            {
                var l = entry.Localization.Find(x => x.CultureCode == translation.Culture.Name);
                Assert.IsNotNull(l);
                Assert.AreEqual(translation.Name, l.Name);
                Assert.AreEqual(translation.Description, l.Description);
            }
        }
    }
    private void AssertTranslations(SimTaxonomyEntrySerializable entry, IEnumerable<SimTaxonomyLocalizationEntry> loc)
    {
        foreach (var translation in loc)
        {
            if (translation.Culture.Name == "")
            {
                Assert.AreEqual(translation.Name, entry.Name);
                Assert.AreEqual(translation.Description, entry.Description);
            }
            else
            {
                var l = entry.Localization.Find(x => x.CultureCode == translation.Culture.Name);
                Assert.IsNotNull(l);
                Assert.AreEqual(translation.Name, l.Name);
                Assert.AreEqual(translation.Description, l.Description);
            }
        }
    }

    [TestMethod]
    public void TaxonomyToSerializableEmptyTest()
    {
        Load();
        var loc = new string[] { "" };
        var tax = TaxonomyUtils.GenerateTaxonomy(0, "A", loc);
        projectData.Taxonomies.Add(tax);

        var ser = new SimTaxonomySerializable(tax);
        Assert.AreEqual(tax.Key, ser.Key);
        var translations = TaxonomyUtils.GenTaxonomyLoc(0, "A", loc).ToList();
        Assert.AreEqual(translations[0].Culture.Name, "");
        Assert.AreEqual(translations[0].Name, ser.Name);
        Assert.AreEqual(translations[0].Description, ser.Description);
        Assert.IsNull(ser.Localization);
        Assert.IsNull(ser.Children);
    }

    [TestMethod]
    public void TaxonomyEntryToSerializableEmptyTest()
    {
        Load();
        var loc = new string[] { "" };
        var entry = TaxonomyUtils.GenerateEntry(0, "A", loc);

        var ser = new SimTaxonomyEntrySerializable(entry);
        Assert.AreEqual(entry.Key, ser.Key);
        var translations = TaxonomyUtils.GenEntryLoc(0, "A", loc).ToList();
        Assert.AreEqual(translations[0].Culture.Name, "");
        Assert.AreEqual(translations[0].Name, ser.Name);
        Assert.AreEqual(translations[0].Description, ser.Description);
        Assert.IsNull(ser.Localization);
        Assert.IsNull(ser.Children);
    }

    [TestMethod]
    public void TaxonomyToSerializableTest()
    {
        Load();
        var tax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var e1 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        var e2 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        var e3 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        tax.Entries.Add(e1);
        e1.Children.Add(e2);
        tax.Entries.Add(e3);
        projectData.Taxonomies.Add(tax);

        var ser = new SimTaxonomySerializable(tax);

        Assert.AreEqual(tax.Key, ser.Key);

        var translations = TaxonomyUtils.GenTaxonomyLoc(0, "A", locales);
        Assert.AreEqual(locales.Count, ser.SupportedLanguages.Count);
        AssertTranslations(ser, translations);
        var se1 = ser.Children.Find(x => x.Key == e1.Key);
        Assert.IsNotNull(se1);
        AssertTranslations(se1, TaxonomyUtils.GenEntryLoc(0, "A", locales));
        var se2 = se1.Children.Find(x => x.Key == e2.Key);
        Assert.IsNotNull(se2);
        AssertTranslations(se2, TaxonomyUtils.GenEntryLoc(1, "A", locales));
        var se3 = ser.Children.Find(x => x.Key == e3.Key);
        Assert.IsNotNull(se3);
        AssertTranslations(se3, TaxonomyUtils.GenEntryLoc(2, "A", locales));
    }

    [TestMethod]
    public void SerializableToTaxonomyTest()
    {
        Load();
        var tax = TaxonomyUtils.GenerateTaxonomy(0, "A", localesAlt);
        var e1 = TaxonomyUtils.GenerateEntry(0, "A", localesAlt);
        var e2 = TaxonomyUtils.GenerateEntry(1, "A", localesAlt);
        var e3 = TaxonomyUtils.GenerateEntry(2, "A", localesAlt);
        tax.Entries.Add(e1);
        e1.Children.Add(e2);
        tax.Entries.Add(e3);

        var inv = tax.Localization.Localize();
        var en = tax.Localization.Localize(new CultureInfo("en"));
        var de = tax.Localization.Localize(new CultureInfo("de"));
        var ser = new SimTaxonomySerializable()
        {
            Key = tax.Key,
            Name = inv.Name,
            Description = inv.Description,
            SupportedLanguages = localesAlt.ToList(),
            Localization = new()
            {
                new SimTaxonomyLocalizationSerializable(en),
                new SimTaxonomyLocalizationSerializable(de),
            }
        };
        inv = e1.Localization.Localize();
        en = e1.Localization.Localize(new CultureInfo("en"));
        de = e1.Localization.Localize(new CultureInfo("de"));
        var se1 = new SimTaxonomyEntrySerializable()
        {
            Key = e1.Key,
            Name = inv.Name,
            Description = inv.Description,
            Localization = new()
            {
                new SimTaxonomyLocalizationSerializable(en),
                new SimTaxonomyLocalizationSerializable(de),
            }
        };
        inv = e2.Localization.Localize();
        en = e2.Localization.Localize(new CultureInfo("en"));
        de = e2.Localization.Localize(new CultureInfo("de"));
        var se2 = new SimTaxonomyEntrySerializable()
        {
            Key = e2.Key,
            Name = inv.Name,
            Description = inv.Description,
            Localization = new()
            {
                new SimTaxonomyLocalizationSerializable(en),
                new SimTaxonomyLocalizationSerializable(de),
            }
        };
        inv = e3.Localization.Localize();
        en = e3.Localization.Localize(new CultureInfo("en"));
        de = e3.Localization.Localize(new CultureInfo("de"));
        var se3 = new SimTaxonomyEntrySerializable()
        {
            Key = e3.Key,
            Name = inv.Name,
            Description = inv.Description,
            Localization = new()
            {
                new SimTaxonomyLocalizationSerializable(en),
                new SimTaxonomyLocalizationSerializable(de),
            }
        };

        ser.Children = new();
        ser.Children.Add(se1);
        se1.Children = new();
        se1.Children.Add(se2);
        ser.Children.Add(se3);

        var converted = ser.ToSimTaxonomy();
        projectData.Taxonomies.Add(converted);

        Assert.AreEqual(ser.Key, converted.Key);
        foreach (var loc in localesAlt)
            Assert.IsTrue(converted.Languages.Contains(new CultureInfo(loc)));
        TaxonomyUtils.AssertLocalization(converted, 0, "A", localesAlt);
        var ce1 = converted.GetTaxonomyEntryByKey(se1.Key);
        Assert.IsNotNull(ce1);
        TaxonomyUtils.AssertLocalization(ce1, 0, "A", localesAlt);
        ce1 = converted.GetTaxonomyEntryByKey(se2.Key);
        Assert.IsNotNull(ce1);
        TaxonomyUtils.AssertLocalization(ce1, 1, "A", localesAlt);
        ce1 = converted.GetTaxonomyEntryByKey(se3.Key);
        Assert.IsNotNull(ce1);
        TaxonomyUtils.AssertLocalization(ce1, 2, "A", localesAlt);
    }
}
