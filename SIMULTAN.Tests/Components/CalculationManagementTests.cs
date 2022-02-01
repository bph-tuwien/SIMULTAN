using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class CalculationManagementTests : BaseProjectTest
    {
        private static readonly FileInfo calculationProject = new FileInfo(@".\CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@".\AccessTestsProject.simultan");

        [TestMethod]
        public void AddCalculation()
        {
            LoadProject(calculationProject);
            var comp = projectData.Components.First(x => x.Name == "NoCalculation");

            var calc = new SimCalculation("a+b", "New Calculation", null, new Dictionary<string, SimParameter> { { "out1", null } });
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Thread.Sleep(5);
            comp.Calculations.Add(calc);
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.IsTrue(comp.Calculations.Contains(calc));
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            Assert.AreEqual(comp, calc.Component);
            Assert.AreEqual(projectData.Components, calc.Factory);
            Assert.AreEqual(project, calc.Id.Location);
            Assert.AreNotEqual(0, calc.Id.LocalId);

            Assert.AreEqual(calc, projectData.IdGenerator.GetById<SimCalculation>(calc.Id));
        }

        [TestMethod]
        public void AddCalculationExceptions()
        {
            LoadProject(calculationProject);
            var comp = projectData.Components.First(x => x.Name == "NoCalculation");

            //Add null
            Assert.ThrowsException<ArgumentNullException>(() => { comp.Calculations.Add(null); });

            //Add twice
            var calc = new SimCalculation("a+b", "New Calculation", null, new Dictionary<string, SimParameter> { { "out1", null } });
            comp.Calculations.Add(calc);

            Assert.ThrowsException<ArgumentException>(() => { comp.Calculations.Add(calc); });
        }

        [TestMethod]
        public void RemoveCalculation()
        {
            LoadProject(calculationProject);
            var comp = projectData.Components.First(x => x.Name == "WithCalc");
            var calc = comp.Calculations.First();
            var calcId = calc.Id.LocalId;
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Thread.Sleep(5);
            comp.Calculations.Remove(calc);
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.IsFalse(comp.Calculations.Contains(calc));
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            Assert.AreEqual(null, calc.Component);
            Assert.AreEqual(null, calc.Factory);
            Assert.AreEqual(null, calc.Id.Location);
            Assert.AreEqual(calcId, calc.Id.LocalId);

            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimCalculation>(calc.Id));
        }

        [TestMethod]
        public void ClearCalculation()
        {
            LoadProject(calculationProject);
            var comp = projectData.Components.First(x => x.Name == "WithCalc");
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            var oldCalcs = comp.Calculations.ToArray();

            Thread.Sleep(5);
            comp.Calculations.Clear();
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.AreEqual(0, comp.Calculations.Count);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            foreach (var calc in oldCalcs)
            {
                Assert.AreEqual(null, calc.Component);
                Assert.AreEqual(null, calc.Factory);
                Assert.AreEqual(null, calc.Id.Location);
                Assert.AreEqual(null, projectData.IdGenerator.GetById<SimCalculation>(calc.Id));
            }


        }

        [TestMethod]
        public void ReplaceCalculation()
        {
            LoadProject(calculationProject);
            var comp = projectData.Components.First(x => x.Name == "WithCalc");
            var calc = comp.Calculations.First();
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            var newCalc = new SimCalculation("a+b", "New Calculation", null, new Dictionary<string, SimParameter> { { "out1", null } });

            Thread.Sleep(5);
            comp.Calculations[0] = newCalc;

            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsFalse(comp.Calculations.Contains(calc));
            Assert.IsTrue(comp.Calculations.Contains(newCalc));
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            Assert.AreEqual(null, calc.Component);
            Assert.AreEqual(null, calc.Factory);
            Assert.AreEqual(null, calc.Id.Location);

            Assert.AreEqual(comp, newCalc.Component);
            Assert.AreEqual(projectData.Components, newCalc.Factory);
            Assert.AreEqual(project, newCalc.Id.Location);
            Assert.AreNotEqual(0, newCalc.Id.LocalId);

            Assert.AreEqual(newCalc, projectData.IdGenerator.GetById<SimCalculation>(newCalc.Id));
        }

        [TestMethod]
        public void CalculationPropertyWriteAccess()
        {
            LoadProject(calculationProject);
            var comp = projectData.Components.First(x => x.Name == "NoCalculation");
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "New Calc", null, new Dictionary<string, SimParameter> { { "out1", null } });
            comp.Calculations.Add(calc);

            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Thread.Sleep(5);
            calc.Expression = "a * b";
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;

            Thread.Sleep(5);
            calc.Name = "other";
            write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;

            Thread.Sleep(5);
            calc.IterationCount = 5;
            write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;

            Thread.Sleep(5);
            calc.OverrideResult = !calc.OverrideResult;
            write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;

            Thread.Sleep(5);
            calc.Description = "text";
            write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;

            Thread.Sleep(5);
            calc.ResultAggregation = SimResultAggregationMethod.Separate;
            write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;

            Thread.Sleep(5);
            calc.InputParams["a"] = demoParams["param1"];
            write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;

            Thread.Sleep(5);
            calc.ReturnParams["out1"] = demoParams["out"];
            write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;

            Thread.Sleep(5);
            calc.ReturnParams.Add("out2", null);
            write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            lastWrite = write;
        }

        [TestMethod]
        public void MemoryLeak()
        {
            LoadProject(new FileInfo(".\\UnitTestProject.simultan"));

            var sourceComponent = projectData.Components.FirstOrDefault(x => x.Name == "Vector Calculation");
            WeakReference calcRef = new WeakReference(sourceComponent.Calculations.First());

            Assert.IsTrue(calcRef.IsAlive);

            sourceComponent.Calculations.Remove((SimCalculation)calcRef.Target);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(calcRef.IsAlive);
        }


        [TestMethod]
        public void AccessTestsAdd()
        {
            LoadProject(accessProject, "bph", "bph");

            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");

            var calc = new SimCalculation("a+b", "New Calc", null, new Dictionary<string, SimParameter> { { "out1", null } });

            //Add
            Assert.ThrowsException<AccessDeniedException>(() => { archComp.Calculations.Add(calc); });
            Assert.AreEqual(1, archComp.Calculations.Count);

            bphComp.Calculations.Add(calc);
            Assert.AreEqual(2, bphComp.Calculations.Count);
        }

        [TestMethod]
        public void AccessTestsRemove()
        {
            LoadProject(accessProject, "bph", "bph");

            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");

            var calc = new SimCalculation("a+b", "New Calc", null, new Dictionary<string, SimParameter> { { "out1", null } });
            bphComp.Calculations.Add(calc);

            //Remove
            Assert.ThrowsException<AccessDeniedException>(() => { archComp.Parameters.RemoveAt(1); });
            bphComp.Calculations.Remove(calc);
            Assert.AreEqual(1, bphComp.Calculations.Count);
        }


        [TestMethod]
        public void InputParameterDeleted()
        {
            LoadProject(calculationProject);

            var parentComp = projectData.Components.First(x => x.Name == "WithSubCalc");
            var childComp = parentComp.Components.First(x => x.Component.Name == "Sub").Component;
            var calc = parentComp.Calculations.First(x => x.Name == "calculation");

            var aParam = parentComp.Parameters.First(x => x.Name == "a");
            var bParam = childComp.Parameters.First(x => x.Name == "b");

            //Input
            parentComp.Parameters.Remove(aParam);
            Assert.AreEqual(null, calc.InputParams["a"]);
            Assert.AreEqual(bParam, calc.InputParams["b"]);

            childComp.Parameters.Remove(bParam);
            Assert.AreEqual(null, calc.InputParams["a"]);
            Assert.AreEqual(null, calc.InputParams["b"]);
        }

        [TestMethod]
        public void ReturnParameterDeleted()
        {
            LoadProject(calculationProject);

            var parentComp = projectData.Components.First(x => x.Name == "WithSubCalc");
            var childComp = parentComp.Components.First(x => x.Component.Name == "Sub").Component;
            var calc = parentComp.Calculations.First(x => x.Name == "calculation");

            var outParam = parentComp.Parameters.First(x => x.Name == "out");
            var out2Param = childComp.Parameters.First(x => x.Name == "out2");

            //Output
            parentComp.Parameters.Remove(outParam);
            Assert.AreEqual(null, calc.ReturnParams["out1"]);
            Assert.AreEqual(out2Param, calc.ReturnParams["out01"]);

            childComp.Parameters.Remove(out2Param);
            Assert.AreEqual(null, calc.ReturnParams["out1"]);
            Assert.AreEqual(null, calc.ReturnParams["out01"]);
        }

        [TestMethod]
        public void InputParameterComponentDeleted()
        {
            LoadProject(calculationProject);

            var parentComp = projectData.Components.First(x => x.Name == "WithSubCalc");
            var childComp = parentComp.Components.First(x => x.Component.Name == "Sub");
            var calc = parentComp.Calculations.First(x => x.Name == "calculation");

            var aParam = parentComp.Parameters.First(x => x.Name == "a");
            var bParam = childComp.Component.Parameters.First(x => x.Name == "b");

            //Input
            parentComp.Components.Remove(childComp);
            Assert.AreEqual(aParam, calc.InputParams["a"]);
            Assert.AreEqual(null, calc.InputParams["b"]);

            //Deleting the component which contains the calculations does not modify it
            projectData.Components.Remove(parentComp);
            Assert.AreEqual(aParam, calc.InputParams["a"]);
            Assert.AreEqual(null, calc.InputParams["b"]);
        }

        [TestMethod]
        public void ReturnParameterComponentDeleted()
        {
            LoadProject(calculationProject);

            var parentComp = projectData.Components.First(x => x.Name == "WithSubCalc");
            var childComp = parentComp.Components.First(x => x.Component.Name == "Sub");
            var calc = parentComp.Calculations.First(x => x.Name == "calculation");

            var outParam = parentComp.Parameters.First(x => x.Name == "out");
            var out2Param = childComp.Component.Parameters.First(x => x.Name == "out2");

            //Output
            parentComp.Components.Remove(childComp);
            Assert.AreEqual(outParam, calc.ReturnParams["out1"]);
            Assert.AreEqual(null, calc.ReturnParams["out01"]);

            //Deleting the component which contains the calculations does not modify it
            projectData.Components.Remove(parentComp);
            Assert.AreEqual(outParam, calc.ReturnParams["out1"]);
            Assert.AreEqual(null, calc.ReturnParams["out01"]);
        }
    }
}
