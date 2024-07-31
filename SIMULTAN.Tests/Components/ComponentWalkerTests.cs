using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class ComponentWalkerTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./ComponentWalkerTest.simultan");

        [TestMethod]
        public void BreathFirstTraversalManyRoot()
        {
            LoadProject(testProject);

            var result = ComponentWalker.BreadthFirstTraversalMany(projectData.Components, x => x.Parameters, x => x.Name.StartsWith("A")).ToList();

            Assert.AreEqual(8, result.Count);
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "x"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "x2"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "x3"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "x4"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "x5"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "xx4"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "xx5"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "xx7"));
        }
        [TestMethod]
        public void BreathFirstTraversalManyComponent()
        {
            LoadProject(testProject);

            var result = ComponentWalker.BreadthFirstTraversalMany(projectData.Components.First(x => x.Name == "A3"), 
                x => x.Parameters, x => x.Name.StartsWith("A")).ToList();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "xx4"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "xx5"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "xx7"));
        }
        [TestMethod]
        public void BreathFirstTraversalManySubComponents()
        {
            LoadProject(testProject);

            var result = ComponentWalker.BreadthFirstTraversalMany(projectData.Components.First(x => x.Name == "A3").Components,
                x => x.Parameters, x => x.Name.StartsWith("A")).ToList();

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "xx4"));
            Assert.IsTrue(result.Any(x => x.NameTaxonomyEntry.Text == "xx5"));
        }

        [TestMethod]
        public void BreadthFirstTraversalRoot()
        {
            LoadProject(testProject);

            var result = ComponentWalker.BreadthFirstTraversal(projectData.Components, x => x.Name, x => x.Name.StartsWith("A")).ToList();

            Assert.AreEqual(10, result.Count);
            Assert.IsTrue(result.Contains("A"));
            Assert.IsTrue(result.Contains("A2"));
            Assert.IsTrue(result.Contains("AA2"));
            Assert.IsTrue(result.Contains("A3"));
            Assert.IsTrue(result.Contains("AB3"));
            Assert.IsTrue(result.Contains("AB4"));
            Assert.IsTrue(result.Contains("AAB3"));
            Assert.IsTrue(result.Contains("AB"));
            Assert.IsTrue(result.Contains("AAB"));
            Assert.IsTrue(result.Contains("AB2"));
        }
        [TestMethod]
        public void BreadthFirstTraversalComponent()
        {
            LoadProject(testProject);

            var result = ComponentWalker.BreadthFirstTraversal(projectData.Components.First(x => x.Name == "A3"),
                x => x.Name, x => x.Name.StartsWith("A")).ToList();

            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("A3"));
            Assert.IsTrue(result.Contains("AB3"));
            Assert.IsTrue(result.Contains("AB4"));
            Assert.IsTrue(result.Contains("AAB3"));
        }
        [TestMethod]
        public void BreadthFirstTraversalSubComponent()
        {
            LoadProject(testProject);

            var result = ComponentWalker.BreadthFirstTraversal(projectData.Components.First(x => x.Name == "A3").Components,
                x => x.Name, x => x.Name.StartsWith("A")).ToList();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("AB3"));
            Assert.IsTrue(result.Contains("AB4"));
            Assert.IsTrue(result.Contains("AAB3"));
        }


        [TestMethod]
        public void FirstOrDefaultRoot()
        {
            LoadProject(testProject);

            var result = ComponentWalker.FirstOrDefault(projectData.Components, x => x.Name == "AB4");
            Assert.AreEqual("AB4", result.Name);

            result = ComponentWalker.FirstOrDefault(projectData.Components, x => x.Name == "U");
            Assert.IsNull(result);
        }
        [TestMethod]
        public void FirstOrDefaultComponent()
        {
            LoadProject(testProject);

            var result = ComponentWalker.FirstOrDefault(projectData.Components.First(x => x.Name == "A3"),
                x => x.Name.StartsWith("A") && x.Name.EndsWith("3"));
            Assert.AreEqual("A3", result.Name);

            result = ComponentWalker.FirstOrDefault(projectData.Components.First(x => x.Name == "A3"), x => x.Name == "U");
            Assert.IsNull(result);
        }
        [TestMethod]
        public void FirstOrDefaultSubComponent()
        {
            LoadProject(testProject);

            var result = ComponentWalker.FirstOrDefault(projectData.Components.First(x => x.Name == "A3").Components,
                x => x.Name.StartsWith("A") && x.Name.EndsWith("3"));
            Assert.AreEqual("AB3", result.Name);

            result = ComponentWalker.FirstOrDefault(projectData.Components.First(x => x.Name == "A3"), x => x.Name == "U");
            Assert.IsNull(result);
        }
    }
}
