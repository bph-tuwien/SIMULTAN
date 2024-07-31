using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ProjectIOTests : BaseProjectTest
    {
        private static readonly FileInfo v16tov17Project = new FileInfo(@"./ProjectUpgradeV16toV17.simultan");

        #region Version Specific Translations

        [TestMethod]
        public void RemoveAutoGenComponentsV16toV17_Volume()
        {
            LoadProject(v16tov17Project);
            var root = projectData.Components.First(x => x.Name == "Root");

            //Volume component
            var volumeComp = root.Components.First(x => x.Component.Name == "Volume").Component;
            Assert.AreEqual(SimInstanceType.Entity3D, volumeComp.InstanceType);
            Assert.AreEqual(3, volumeComp.Components.Count); //All autogen children should be removed

            var face30 = volumeComp.Components.First(x => x.Component.Name == "Face 30").Component;
            Assert.IsFalse(face30.IsAutomaticallyGenerated);
            Assert.AreEqual(SimInstanceType.None, face30.InstanceType);
            Assert.AreEqual(1, face30.Parameters.Count);
            Assert.AreEqual(0, face30.Components.Count);
            Assert.AreEqual(0, face30.ReferencedComponents.Count);
            Assert.AreEqual(0, face30.Instances.Count);

            //Check Face 33, that's kept alive by a subcomponent
            var face33 = volumeComp.Components.First(x => x.Component.Name == "Face 33").Component;
            Assert.IsFalse(face33.IsAutomaticallyGenerated);
            Assert.AreEqual(SimInstanceType.None, face33.InstanceType);
            Assert.AreEqual(0, face33.Parameters.Count);
            Assert.AreEqual(1, face33.Components.Count);
            Assert.AreEqual(0, face33.ReferencedComponents.Count);
            Assert.AreEqual(0, face33.Instances.Count);

            //Check Face 25, that's kept alive by a parameter in a hole component
            var face25 = volumeComp.Components.First(x => x.Component.Name == "Face 25").Component;
            Assert.IsFalse(face25.IsAutomaticallyGenerated);
            Assert.AreEqual(SimInstanceType.None, face25.InstanceType);
            Assert.AreEqual(0, face25.Parameters.Count);
            Assert.AreEqual(1, face25.Components.Count);
            Assert.AreEqual(1, face25.Components[0].Component.Parameters.Count);
            Assert.AreEqual(0, face25.ReferencedComponents.Count);
            Assert.AreEqual(0, face25.Instances.Count);
        }

        [TestMethod]
        public void RemoveAutoGenComponentsV16toV17_Face()
        {
            LoadProject(v16tov17Project);
            var root = projectData.Components.First(x => x.Name == "Root");

            var faceComp = root.Components.First(x => x.Component.Name == "Face").Component;

            Assert.AreEqual(3, faceComp.Parameters.Count);

            Assert.IsTrue(faceComp.Parameters.Any(x => x.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key ==
                ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_IN));
            Assert.IsTrue(faceComp.Parameters.Any(x => x.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key ==
                ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_OUT));
            Assert.IsTrue(faceComp.Parameters.Any(x => x.NameTaxonomyEntry.Text == "Keeper"));
        }

        [TestMethod]
        public void RemoveAutoGenComponentsV16toV17_Edge()
        {
            LoadProject(v16tov17Project);
            var root = projectData.Components.First(x => x.Name == "Root");

            var faceComp = root.Components.First(x => x.Component.Name == "Edge").Component;

            Assert.AreEqual(1, faceComp.Parameters.Count);
            Assert.IsTrue(faceComp.Parameters.Any(x => x.NameTaxonomyEntry.Text == "Keeper"));
        }

        [TestMethod]
        public void RemoveAutoGenComponentsV16toV17_Vertex()
        {
            LoadProject(v16tov17Project);
            var root = projectData.Components.First(x => x.Name == "Root");

            var faceComp = root.Components.First(x => x.Component.Name == "Vertex").Component;

            Assert.AreEqual(1, faceComp.Parameters.Count);
            Assert.IsTrue(faceComp.Parameters.Any(x => x.NameTaxonomyEntry.Text == "Keeper"));
        }

        #endregion
    }
}
