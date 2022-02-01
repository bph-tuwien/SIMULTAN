using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.StyleChecks
{
    [TestClass]
    public class StyleChecks
    {
        [TestMethod]
        public void NamespaceChecks()
        {
            var ass = Assembly.Load("SIMULTAN");

            var types = ass.GetTypes();

            foreach (var type in types)
            {
                if (type.IsPublic)
                {
                    var namespaceSplit = type.Namespace.Split('.');

                    Assert.AreEqual("SIMULTAN", namespaceSplit[0],
                        string.Format("Top level namespace has to be SIMULTAN. Wrong type: {0}", type.FullName));
                    Assert.IsTrue(namespaceSplit.Length <= 3,
                        string.Format("There may be only three namespace levels. Wrong type: {0}", type.FullName));
                }
            }
        }
    }
}
