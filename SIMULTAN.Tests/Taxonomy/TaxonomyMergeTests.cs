using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.Projects;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TaxonomyUtils = SIMULTAN.Tests.TestUtils.TaxonomyUtils;

namespace SIMULTAN.Tests.Taxonomy;

[TestClass]
public class TaxonomyMergeTests : BaseProjectTest
{
    private static readonly FileInfo emptyProject = new FileInfo(@"./EmptyProject.simultan");


    private static string[] locales = { "", "en", "de" };
    private static string[] localesAlt = { "ja", "zh-Hans" };


    private SimTaxonomy GetTestTaxonomy(int i, string tag, IEnumerable<string> locales)
    {
        LoadProject(emptyProject);

        var tax = TaxonomyUtils.GenerateTaxonomy(i, tag, locales);

        projectData.Taxonomies.Add(tax);

        return tax;
    }

    private (SimTaxonomyCollection, SimTaxonomy) GenTempTaxonomyCollection(int i, string tag, IEnumerable<string> locales)
    {
        var tmpProjectData = new ExtendedProjectData(projectData.SynchronizationContext, projectData.DispatcherTimerFactory);
        var tax = TaxonomyUtils.GenerateTaxonomy(i, tag, locales);
        tmpProjectData.Taxonomies.Add(tax);
        return (tmpProjectData.Taxonomies, tax);
    }

    #region Taxonomy Merging

    [TestMethod]
    public void MergeEmpty()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);

        Assert.AreEqual(0, tax.Entries.Count());
        Assert.AreEqual(0, mergetax.Entries.Count());

        tax.MergeWith(mergetax);

        Assert.AreEqual(0, tax.Entries.Count());
        Assert.AreEqual(0, mergetax.Entries.Count());
    }

    [TestMethod]
    public void MergeSingleExisting()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        tax.Entries.Add(TaxonomyUtils.GenerateEntry(0, "A", locales));
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        mergetax.Entries.Add(TaxonomyUtils.GenerateEntry(0, "A", locales));

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());

        tax.MergeWith(mergetax);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
    }

    [TestMethod]
    public void MergeMultipleExisting()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        tax.Entries.Add(TaxonomyUtils.GenerateEntry(0, "A", locales));
        tax.Entries.Add(TaxonomyUtils.GenerateEntry(1, "A", locales));
        tax.Entries.Add(TaxonomyUtils.GenerateEntry(2, "A", locales));
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        mergetax.Entries.Add(TaxonomyUtils.GenerateEntry(2, "A", locales));
        mergetax.Entries.Add(TaxonomyUtils.GenerateEntry(1, "A", locales));
        mergetax.Entries.Add(TaxonomyUtils.GenerateEntry(0, "A", locales));

        Assert.AreEqual(3, tax.Entries.Count());
        Assert.AreEqual(3, mergetax.Entries.Count());

        tax.MergeWith(mergetax);

        Assert.AreEqual(3, tax.Entries.Count());
        Assert.AreEqual(3, mergetax.Entries.Count());
    }

    [TestMethod]
    public void MergeHierarchyExisting()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        e0.Children.Add(e1);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        e1.Children.Add(e2);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var m0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        mergetax.Entries.Add(m0);
        var m1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        m0.Children.Add(m1);
        var m2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        m1.Children.Add(m2);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(1, e1.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(1, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());

        tax.MergeWith(mergetax);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(1, e1.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(1, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());
    }

    [TestMethod]
    public void MergeHierarchyUpdateLoc()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        e0.Children.Add(e1);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        e1.Children.Add(e2);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "B", locales);
        var m0 = TaxonomyUtils.GenerateEntry(0, "B", locales);
        mergetax.Entries.Add(m0);
        var m1 = TaxonomyUtils.GenerateEntry(1, "B", locales);
        m0.Children.Add(m1);
        var m2 = TaxonomyUtils.GenerateEntry(2, "B", locales);
        m1.Children.Add(m2);

        TaxonomyUtils.AssertLocalization(tax, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(e0, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(e1, 1, "A", locales);
        TaxonomyUtils.AssertLocalization(e2, 2, "A", locales);
        TaxonomyUtils.AssertLocalization(mergetax, 0, "B", locales);
        TaxonomyUtils.AssertLocalization(m0, 0, "B", locales);
        TaxonomyUtils.AssertLocalization(m1, 1, "B", locales);
        TaxonomyUtils.AssertLocalization(m2, 2, "B", locales);

        tax.MergeWith(mergetax);

        TaxonomyUtils.AssertLocalization(tax, 0, "B", locales);
        TaxonomyUtils.AssertLocalization(e0, 0, "B", locales);
        TaxonomyUtils.AssertLocalization(e1, 1, "B", locales);
        TaxonomyUtils.AssertLocalization(e2, 2, "B", locales);
        TaxonomyUtils.AssertLocalization(mergetax, 0, "B", locales);
        TaxonomyUtils.AssertLocalization(m0, 0, "B", locales);
        TaxonomyUtils.AssertLocalization(m1, 1, "B", locales);
        TaxonomyUtils.AssertLocalization(m2, 2, "B", locales);
    }
    [TestMethod]
    public void MergeDifferentCulture()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        e0.Children.Add(e1);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        e1.Children.Add(e2);

        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", localesAlt);
        var m0 = TaxonomyUtils.GenerateEntry(0, "B", localesAlt);
        mergetax.Entries.Add(m0);
        var m1 = TaxonomyUtils.GenerateEntry(1, "B", localesAlt);
        m0.Children.Add(m1);
        var m2 = TaxonomyUtils.GenerateEntry(2, "B", localesAlt);
        m1.Children.Add(m2);

        TaxonomyUtils.AssertLocalization(tax, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(e0, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(e1, 1, "A", locales);
        TaxonomyUtils.AssertLocalization(e2, 2, "A", locales);
        TaxonomyUtils.AssertLocalization(m0, 0, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m1, 1, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m2, 2, "B", localesAlt);

        tax.MergeWith(mergetax);

        // first tax got additional languages
        TaxonomyUtils.AssertLocalization(tax, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(tax, 0, "A", localesAlt);
        TaxonomyUtils.AssertLocalization(e0, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(e1, 1, "A", locales);
        TaxonomyUtils.AssertLocalization(e2, 2, "A", locales);
        TaxonomyUtils.AssertLocalization(e0, 0, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(e1, 1, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(e2, 2, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m0, 0, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m1, 1, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m2, 2, "B", localesAlt);
    }
    [TestMethod]
    public void MergeDifferentCultureDelete()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        e0.Children.Add(e1);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        e1.Children.Add(e2);

        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", localesAlt);
        var m0 = TaxonomyUtils.GenerateEntry(0, "B", localesAlt);
        mergetax.Entries.Add(m0);
        var m1 = TaxonomyUtils.GenerateEntry(1, "B", localesAlt);
        m0.Children.Add(m1);
        var m2 = TaxonomyUtils.GenerateEntry(2, "B", localesAlt);
        m1.Children.Add(m2);

        TaxonomyUtils.AssertLocalization(tax, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(e0, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(e1, 1, "A", locales);
        TaxonomyUtils.AssertLocalization(e2, 2, "A", locales);
        TaxonomyUtils.AssertLocalization(m0, 0, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m1, 1, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m2, 2, "B", localesAlt);

        tax.MergeWith(mergetax, true);

        TaxonomyUtils.AssertLocalization(tax, 0, "A", localesAlt);
        foreach (var loc in locales)
        {
            var culture = new CultureInfo(loc);
            Assert.IsFalse(tax.Languages.Contains(culture));
            Assert.IsFalse(e0.Localization.Entries.ContainsKey(culture));
            Assert.IsFalse(e1.Localization.Entries.ContainsKey(culture));
            Assert.IsFalse(e2.Localization.Entries.ContainsKey(culture));
        }
        TaxonomyUtils.AssertLocalization(e0, 0, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(e1, 1, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(e2, 2, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m0, 0, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m1, 1, "B", localesAlt);
        TaxonomyUtils.AssertLocalization(m2, 2, "B", localesAlt);
    }

    [TestMethod]
    public void MergeHierarchyAdded()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var m0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        mergetax.Entries.Add(m0);
        var m1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        m0.Children.Add(m1);
        var m2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        m1.Children.Add(m2);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(0, e0.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(1, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());

        tax.MergeWith(mergetax);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(1, e0.Children[0].Children.Count());
        Assert.AreEqual(0, e0.Children[0].Children[0].Children.Count());
        Assert.AreEqual(m1.Key, e0.Children[0].Key);
        Assert.AreEqual(m2.Key, e0.Children[0].Children[0].Key);

        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(1, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());
    }

    [TestMethod]
    public void MergeReorderToHierarchy()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        tax.Entries.Add(e1);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        tax.Entries.Add(e2);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var m0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        mergetax.Entries.Add(m0);
        var m1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        m0.Children.Add(m1);
        var m2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        m1.Children.Add(m2);

        Assert.AreEqual(3, tax.Entries.Count());
        Assert.AreEqual(0, e0.Children.Count());
        Assert.AreEqual(0, e1.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(1, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());

        tax.MergeWith(mergetax);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(1, e1.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());

        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(1, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());
    }
    [TestMethod]
    public void MergeReorderFlat()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        e0.Children.Add(e1);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        e1.Children.Add(e2);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var m0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        mergetax.Entries.Add(m0);
        var m1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        mergetax.Entries.Add(m1);
        var m2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        mergetax.Entries.Add(m2);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(1, e1.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());
        Assert.AreEqual(3, mergetax.Entries.Count());
        Assert.AreEqual(0, m0.Children.Count());
        Assert.AreEqual(0, m1.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());

        tax.MergeWith(mergetax);

        Assert.AreEqual(3, tax.Entries.Count());
        Assert.AreEqual(0, e0.Children.Count());
        Assert.AreEqual(0, e1.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());

        Assert.AreEqual(3, mergetax.Entries.Count());
        Assert.AreEqual(0, m0.Children.Count());
        Assert.AreEqual(0, m1.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());
    }
    [TestMethod]
    public void MergeReorderInverse()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        e0.Children.Add(e1);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        e1.Children.Add(e2);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var m0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        var m1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        var m2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        mergetax.Entries.Add(m2);
        m2.Children.Add(m1);
        m1.Children.Add(m0);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(1, e1.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(0, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());
        Assert.AreEqual(1, m2.Children.Count());

        Assert.AreEqual(e0.Key, tax.Entries[0].Key);
        Assert.AreEqual(e1.Key, e0.Children[0].Key);
        Assert.AreEqual(e2.Key, e1.Children[0].Key);
        Assert.AreEqual(m2.Key, mergetax.Entries[0].Key);
        Assert.AreEqual(m1.Key, m2.Children[0].Key);
        Assert.AreEqual(m0.Key, m1.Children[0].Key);

        tax.MergeWith(mergetax);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(0, e0.Children.Count());
        Assert.AreEqual(1, e1.Children.Count());
        Assert.AreEqual(1, e2.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(0, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());
        Assert.AreEqual(1, m2.Children.Count());

        Assert.AreEqual(e2.Key, tax.Entries[0].Key);
        Assert.AreEqual(e1.Key, e2.Children[0].Key);
        Assert.AreEqual(e0.Key, e1.Children[0].Key);
        Assert.AreEqual(m2.Key, mergetax.Entries[0].Key);
        Assert.AreEqual(m1.Key, m2.Children[0].Key);
        Assert.AreEqual(m0.Key, m1.Children[0].Key);
    }
    [TestMethod]
    public void MergeReorderNew()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax.Entries.Add(e0);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var m0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        var m1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        mergetax.Entries.Add(m1);
        m1.Children.Add(m0);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(0, e0.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(0, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());

        Assert.AreEqual(e0.Key, tax.Entries[0].Key);
        Assert.AreEqual(m1.Key, mergetax.Entries[0].Key);
        Assert.AreEqual(m0.Key, m1.Children[0].Key);

        tax.MergeWith(mergetax);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, tax.Entries[0].Children.Count());
        Assert.AreEqual(0, tax.Entries[0].Children[0].Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(0, m0.Children.Count());
        Assert.AreEqual(1, m1.Children.Count());

        Assert.AreEqual(m1.Key, tax.Entries[0].Key);
        Assert.AreEqual(m0.Key, tax.Entries[0].Children[0].Key);
        Assert.AreEqual(m1.Key, mergetax.Entries[0].Key);
        Assert.AreEqual(m0.Key, m1.Children[0].Key);
    }

    [TestMethod]
    public void MergeReorderRemove()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        tax.Entries.Add(e0);
        e0.Children.Add(e1);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var m1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        mergetax.Entries.Add(m1);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(0, e1.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(0, m1.Children.Count());

        Assert.AreEqual(e0.Key, tax.Entries[0].Key);
        Assert.AreEqual(e1.Key, e0.Children[0].Key);
        Assert.AreEqual(m1.Key, mergetax.Entries[0].Key);

        tax.MergeWith(mergetax, true);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(0, e1.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(0, m1.Children.Count());

        Assert.AreEqual(e1.Key, tax.Entries[0].Key);
        Assert.AreEqual(m1.Key, mergetax.Entries[0].Key);
    }
    [TestMethod]
    public void MergeRemoveChildEntry()
    {
        var tax = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        tax.Entries.Add(e0);
        e0.Children.Add(e1);
        e1.Children.Add(e2);
        var mergetax = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        var m0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        var m2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        mergetax.Entries.Add(m0);
        m0.Children.Add(m2);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(1, e1.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(1, m0.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());

        Assert.AreEqual(e0.Key, tax.Entries[0].Key);
        Assert.AreEqual(e1.Key, e0.Children[0].Key);
        Assert.AreEqual(e2.Key, e1.Children[0].Key);
        Assert.AreEqual(m0.Key, mergetax.Entries[0].Key);
        Assert.AreEqual(m2.Key, m0.Children[0].Key);

        tax.MergeWith(mergetax, true);

        Assert.AreEqual(1, tax.Entries.Count());
        Assert.AreEqual(1, e0.Children.Count());
        Assert.AreEqual(0, e2.Children.Count());
        Assert.AreEqual(1, mergetax.Entries.Count());
        Assert.AreEqual(1, m0.Children.Count());
        Assert.AreEqual(0, m2.Children.Count());

        Assert.AreEqual(e0.Key, tax.Entries[0].Key);
        Assert.AreEqual(e2.Key, e0.Children[0].Key);
        Assert.AreEqual(m0.Key, mergetax.Entries[0].Key);
        Assert.AreEqual(m2.Key, m0.Children[0].Key);
    }

    #endregion

    #region Taxonomy Collection Merging

    [TestMethod]
    public void CollectionMergeIdentical()
    {
        var tax1 = GetTestTaxonomy(0, "A", locales);
        var e0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        var e1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        var e2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        tax1.Entries.Add(e0);
        e0.Children.Add(e1);
        tax1.Entries.Add(e2);
        var (tmpColl, tmpTax1) = GenTempTaxonomyCollection(0, "A", locales);
        var t0 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        var t1 = TaxonomyUtils.GenerateEntry(1, "A", locales);
        var t2 = TaxonomyUtils.GenerateEntry(2, "A", locales);
        tmpTax1.Entries.Add(t0);
        t0.Children.Add(t1);
        tmpTax1.Entries.Add(t2);


        Assert.AreEqual(1, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(1, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.IsTrue(tax1.IsIdentical(tmpTax1));

        var result = projectData.Taxonomies.Merge(tmpColl, out var conflicts, false, false);

        Assert.AreEqual(1, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(1, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.IsTrue(tax1.IsIdentical(tmpTax1));
        Assert.AreEqual(0, conflicts.Count);
        Assert.AreEqual(1, result.Count);
    }

    [TestMethod]
    public void CollectionMergeAdded()
    {
        var tax1 = GetTestTaxonomy(0, "A", locales);
        var e1 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax1.Entries.Add(e1);
        var (tmpColl, tmpTax1) = GenTempTaxonomyCollection(1, "A", locales);
        var t1 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tmpTax1.Entries.Add(t1);
        var tmpTax2 = TaxonomyUtils.GenerateTaxonomy(2, "A", locales);
        var w1 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tmpTax2.Entries.Add(w1);
        tmpColl.Add(tmpTax2);

        Assert.AreEqual(1, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(1, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(1, tmpColl.Count(x => x.Key == tmpTax2.Key));
        Assert.AreEqual(1, tax1.Entries.Count);
        Assert.AreEqual(1, tmpTax1.Entries.Count);
        Assert.AreEqual(1, tmpTax2.Entries.Count);

        var result = projectData.Taxonomies.Merge(tmpColl, out var conflicts, false, false);

        Assert.AreEqual(1, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(1, projectData.Taxonomies.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(1, projectData.Taxonomies.Count(x => x.Key == tmpTax2.Key));
        Assert.AreEqual(0, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(0, tmpColl.Count(x => x.Key == tmpTax2.Key));
        Assert.AreEqual(1, tax1.Entries.Count);
        Assert.AreEqual(1, tmpTax1.Entries.Count);
        Assert.AreEqual(1, tmpTax2.Entries.Count);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, conflicts.Count);
    }

    [TestMethod]
    public void CollectionMergeUpdate()
    {
        var tax1 = GetTestTaxonomy(0, "A", locales);
        var e1 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax1.Entries.Add(e1);
        var (tmpColl, tmpTax1) = GenTempTaxonomyCollection(0, "A", locales);
        var t1 = TaxonomyUtils.GenerateEntry(0, "B", locales);
        tmpTax1.Entries.Add(t1);

        Assert.AreEqual(1, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(1, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(1, tax1.Entries.Count);
        Assert.AreEqual(1, tmpTax1.Entries.Count);
        TaxonomyUtils.AssertLocalization(e1, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(t1, 0, "B", locales);

        var result = projectData.Taxonomies.Merge(tmpColl, out var conflicts, false, false);

        Assert.AreEqual(1, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(0, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(1, tax1.Entries.Count);
        Assert.AreEqual(1, tmpTax1.Entries.Count);
        TaxonomyUtils.AssertLocalization(e1, 0, "B", locales);
        TaxonomyUtils.AssertLocalization(t1, 0, "B", locales);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, conflicts.Count);
    }

    [TestMethod]
    public void CollectionMergeConflict()
    {
        var tax1 = GetTestTaxonomy(0, "A", locales);
        var e1 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax1.Entries.Add(e1);
        var tax2 = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        projectData.Taxonomies.Add(tax2);
        var (tmpColl, tmpTax1) = GenTempTaxonomyCollection(0, "A", locales);
        var t1 = TaxonomyUtils.GenerateEntry(0, "B", locales);
        tmpTax1.Entries.Add(t1);

        Assert.AreEqual(2, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(1, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(1, tax1.Entries.Count);
        Assert.AreEqual(0, tax2.Entries.Count);
        Assert.AreEqual(1, tmpTax1.Entries.Count);
        TaxonomyUtils.AssertLocalization(e1, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(t1, 0, "B", locales);

        var result = projectData.Taxonomies.Merge(tmpColl, out var conflicts, false, false);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(2, conflicts.Count);
        Assert.IsTrue(conflicts.Contains((tmpTax1, tax1)));
        Assert.IsTrue(conflicts.Contains((tmpTax1, tax2)));
        TaxonomyUtils.AssertLocalization(e1, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(t1, 0, "B", locales);

        Assert.AreEqual(2, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(1, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(1, tax1.Entries.Count);
        Assert.AreEqual(0, tax2.Entries.Count);
        Assert.AreEqual(1, tmpTax1.Entries.Count);
    }

    [TestMethod]
    public void CollectionMergeConflictForce()
    {
        var tax1 = GetTestTaxonomy(0, "A", locales);
        var e1 = TaxonomyUtils.GenerateEntry(0, "A", locales);
        tax1.Entries.Add(e1);
        var tax2 = TaxonomyUtils.GenerateTaxonomy(0, "A", locales);
        projectData.Taxonomies.Add(tax2);
        var (tmpColl, tmpTax1) = GenTempTaxonomyCollection(0, "A", locales);
        var t1 = TaxonomyUtils.GenerateEntry(0, "B", locales);
        tmpTax1.Entries.Add(t1);

        Assert.AreEqual(2, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(1, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(1, tax1.Entries.Count);
        Assert.AreEqual(0, tax2.Entries.Count);
        Assert.AreEqual(1, tmpTax1.Entries.Count);
        TaxonomyUtils.AssertLocalization(e1, 0, "A", locales);
        TaxonomyUtils.AssertLocalization(t1, 0, "B", locales);

        var result = projectData.Taxonomies.Merge(tmpColl, out var conflicts, false, true);

        Assert.AreEqual(0, result.Count);
        Assert.AreEqual(0, conflicts.Count);
        TaxonomyUtils.AssertLocalization(e1, 0, "B", locales);
        TaxonomyUtils.AssertLocalization(t1, 0, "B", locales);

        Assert.AreEqual(2, projectData.Taxonomies.Count(x => x.Key == tax1.Key));
        Assert.AreEqual(0, tmpColl.Count(x => x.Key == tmpTax1.Key));
        Assert.AreEqual(1, tax1.Entries.Count);
        Assert.AreEqual(0, tax2.Entries.Count);
        Assert.AreEqual(1, tmpTax1.Entries.Count);
    }

    #endregion

}
