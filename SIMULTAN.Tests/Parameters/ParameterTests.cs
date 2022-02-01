using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Users;
using SIMULTAN.Tests.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Parameters
{
    [TestClass]
    public class ParameterTests : BaseProjectTest
    {
        private static readonly FileInfo parameterProject = new FileInfo(@".\ParameterTestsProject.simultan");
        private static readonly FileInfo calculationProject = new FileInfo(@".\CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@".\ComponentAccessTestsProject.simultan");
        private static readonly FileInfo parameterAccessProject = new FileInfo(@".\AccessTestsProject.simultan");

        internal void CheckParameter(SimParameter parameter, string name, string unit, double value, double min, double max, SimParameterOperations op)
        {
            Assert.AreEqual(name, parameter.Name);
            Assert.AreEqual(unit, parameter.Unit);
            AssertUtil.AssertDoubleEqual(value, parameter.ValueCurrent);
            AssertUtil.AssertDoubleEqual(min, parameter.ValueMin);
            AssertUtil.AssertDoubleEqual(max, parameter.ValueMax);
            Assert.AreEqual(op, parameter.AllowedOperations);
        }

        [TestMethod]
        public void Ctor()
        {
            var parameter = new SimParameter("param", "unit", 0.5, SimParameterOperations.EditName);
            CheckParameter(parameter, "param", "unit", 0.5, double.NegativeInfinity, double.PositiveInfinity, SimParameterOperations.EditName);

            parameter = new SimParameter("param2", "unit2", 0.5, -5, 5, SimParameterOperations.EditName | SimParameterOperations.EditValue);
            CheckParameter(parameter, "param2", "unit2", 0.5, -5, 5, SimParameterOperations.EditName | SimParameterOperations.EditValue);
        }

        [TestMethod]
        public void CtorParsing()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_A");
            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(table, 1, 2);

            //Without pointer
            var param = new SimParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99.5, -2, 99.7, "textval",
                null, SimParameterOperations.Move);

            CheckParameter(param, "name", "unit", 99.5, -2, 99.7, SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual("textval", param.TextValue);
            Assert.AreEqual(null, param.MultiValuePointer);


            //With pointer
            param = new SimParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99.5, -2, 99.7, "textval",
                ptr, SimParameterOperations.Move);

            CheckParameter(param, "name", "unit", 6.0, -2, 99.7, SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual("textval", param.TextValue);

            var paramPtr = (SimMultiValueBigTable.SimMultiValueBigTablePointer)param.MultiValuePointer;
            Assert.AreEqual(table, paramPtr.ValueField);
            Assert.AreEqual(1, paramPtr.Row);
            Assert.AreEqual(2, paramPtr.Column);
        }

        [TestMethod]
        public void Clone()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_A");
            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(table, 1, 2);

            var paramSource = new SimParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99.5, -2, 99.7, "textval",
                ptr, SimParameterOperations.Move);
            projectData.Components.First().Parameters.Add(paramSource);
            Assert.AreNotEqual(null, paramSource.Component);


            var param = paramSource.Clone();

            CheckParameter(param, "name", "unit", 6.0, -2, 99.7, SimParameterOperations.Move);
            Assert.AreEqual(0, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual("textval", param.TextValue);

            var paramPtr = (SimMultiValueBigTable.SimMultiValueBigTablePointer)param.MultiValuePointer;
            Assert.AreEqual(table, paramPtr.ValueField);
            Assert.AreEqual(1, paramPtr.Row);
            Assert.AreEqual(2, paramPtr.Column);

            Assert.AreEqual(null, param.Component);
        }

        #region Properties

        [TestMethod]
        public void PropertyAllowedOperations()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.AllowedOperations), SimParameterOperations.All);
        }

        [TestMethod]
        public void PropertyCategory()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescription()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyName()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.Name), "randomName");
        }

        [TestMethod]
        public void PropertyPropagation()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationMode()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValue()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.TextValue), "someText");
        }

        [TestMethod]
        public void PropertyUnit()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrent()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.ValueCurrent), 11.1);
        }

        [TestMethod]
        public void PropertyValueMin()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.ValueMin), -20.4);
        }

        [TestMethod]
        public void PropertyValueMax()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.ValueMax), 20.6);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGenerated()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointer()
        {
            LoadProject(parameterAccessProject);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimParameter.MultiValuePointer), table.CreateNewPointer(),
                new System.Collections.Generic.List<string> { nameof(SimParameter.MultiValuePointer), nameof(SimParameter.ValueCurrent) });
        }

        #endregion

        #region Property Access

        private void CheckParameterPropertyAccess<T>(string prop, T value)
        {
            LoadProject(accessProject, "bph", "bph");
            var bphParameter = projectData.Components.First(x => x.Name == "BPHRoot").Parameters.First(x => x.Name == "BPHParameter");
            var archParameter = projectData.Components.First(x => x.Name == "ArchRoot").Parameters.First(x => x.Name == "ArchParameter");

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, prop, value);
        }

        [TestMethod]
        public void PropertyAllowedOperationsAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.Name), "randomName");
        }

        [TestMethod]
        public void PropertyPropagationAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.TextValue), "someText");
        }

        [TestMethod]
        public void PropertyUnitAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrentAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.ValueCurrent), 11.1);
        }

        [TestMethod]
        public void PropertyValueMinAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.ValueMin), -20.4);
        }

        [TestMethod]
        public void PropertyValueMaxAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.ValueMax), 20.6);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedAccess()
        {
            CheckParameterPropertyAccess(nameof(SimParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerAccess()
        {
            LoadProject(parameterAccessProject, "bph", "bph");
            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var archParameter = archComp.Parameters.First(x => x.Name == "Parameter1");
            var bphParameter = bphComp.Parameters.First(x => x.Name == "Parameter2");

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, nameof(SimParameter.MultiValuePointer), table.CreateNewPointer());
        }

        #endregion

        #region Property Changes

        private void CheckParameterPropertyChanges<T>(string prop, T value)
        {
            //Setup
            LoadProject(accessProject, "bph", "bph");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHRoot");
            var bphParameter = bphComponent.Parameters.First(x => x.Name == "BPHParameter");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, prop, value, SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }


        [TestMethod]
        public void PropertyAllowedOperationsChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.Name), "randomName");
        }

        [TestMethod]
        public void PropertyPropagationChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.TextValue), "someText");
        }

        [TestMethod]
        public void PropertyUnitChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrentChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.ValueCurrent), 11.1);
        }

        [TestMethod]
        public void PropertyValueMinChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.ValueMin), -20.4);
        }

        [TestMethod]
        public void PropertyValueMaxChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.ValueMax), 20.6);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedChanges()
        {
            CheckParameterPropertyChanges(nameof(SimParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerChanges()
        {
            //Setup
            LoadProject(parameterAccessProject, "bph", "bph");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHComp");
            var bphParameter = bphComponent.Parameters.First(x => x.Name == "Parameter2");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, nameof(SimParameter.MultiValuePointer), table.CreateNewPointer(),
                SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }

        #endregion


        [TestMethod]
        public void HasSameCurrentValue()
        {
            var param = new SimParameter("p1", "unit", 1.0, SimParameterOperations.None);

            Assert.IsTrue(param.HasSameCurrentValue(1.0));
            Assert.IsFalse(param.HasSameCurrentValue(1.1));
            Assert.IsFalse(param.HasSameCurrentValue(double.NaN));

            param.ValueCurrent = double.NaN;
            Assert.IsFalse(param.HasSameCurrentValue(1.0));
            Assert.IsTrue(param.HasSameCurrentValue(double.NaN));
        }

        [TestMethod]
        public void StateNan()
        {
            SimParameter p = new SimParameter("name", "unit", double.NaN);
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueNaN));

            p.ValueCurrent = 1.0;
            Assert.IsFalse(p.State.HasFlag(SimParameterState.ValueNaN));

            p.ValueCurrent = double.NaN;
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueNaN));
        }

        [TestMethod]
        public void StateOutOfRange()
        {
            SimParameter p = new SimParameter("name", "unit", 50.0, 0.0, 2.0);
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueOutOfRange));

            p.ValueCurrent = 1.0;
            Assert.IsFalse(p.State.HasFlag(SimParameterState.ValueOutOfRange));

            p.ValueCurrent = -2.0;
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueOutOfRange));
        }

        [TestMethod]
        public void StateRefNotFound()
        {
            LoadProject(parameterProject);
            projectData.Components.EnableAsyncUpdates = false;

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimParameter("B", "unit", 99.9)
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.Name = "A";
            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            var reference = comp.ReferencedComponents.First();
            var refSlot = reference.Slot;

            reference.Target = null;
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            reference.Target = projectData.Components.First(x => x.Name == "ReferenceSource");
            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.Name = "B";
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.Propagation = SimInfoFlow.Output;
            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));
        }

        [TestMethod]
        public void StateHidesReference()
        {
            LoadProject(parameterProject);
            projectData.Components.EnableAsyncUpdates = false;

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimParameter("A", "unit", 99.9)
            {
                Propagation = SimInfoFlow.Output
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.HidesReference));

            param.Name = "B";
            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            param.Name = "A";
            Assert.IsTrue(param.State.HasFlag(SimParameterState.HidesReference));

            var reference = comp.ReferencedComponents.First();
            var refSlot = reference.Slot;
            reference.Target = null;
            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            reference.Target = projectData.Components.First(x => x.Name == "ReferenceSource");
            Assert.IsTrue(param.State.HasFlag(SimParameterState.HidesReference));

            param.Propagation = SimInfoFlow.FromReference;
            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));
        }

        [TestMethod]
        public void GetReferencingCalculations()
        {
            LoadProject(calculationProject);

            var param = new SimParameter("p", "u", 1.0);
            Assert.ThrowsException<InvalidOperationException>(() => { param.GetReferencingCalculations(); });

            var c2 = projectData.Components.First(x => x.Name == "ReferenceUsingCalcParent")
                .Components.First(x => x.Component != null && x.Component.Name == "ReferenceUsingCalc").Component;

            var calc = c2.Calculations.First();

            param = c2.Parameters.First(x => x.Name == "noref");
            var calcs = param.GetReferencingCalculations();
            Assert.AreEqual(1, calcs.Count);
            Assert.AreEqual(calc, calcs[0]);

            param = c2.Parameters.First(x => x.Name == "out");
            calcs = param.GetReferencingCalculations();
            Assert.AreEqual(1, calcs.Count);
            Assert.AreEqual(calc, calcs[0]);
        }

        [TestMethod]
        public void HasAccess()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "NotEmpty");
            var param = comp.Parameters.First(x => x.Name == "a");

            Assert.IsTrue(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "admin"), SimComponentAccessPrivilege.Read));
            Assert.IsTrue(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "arch"), SimComponentAccessPrivilege.Read));
            Assert.IsFalse(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "guest"), SimComponentAccessPrivilege.Read));

            Assert.IsTrue(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "admin"), SimComponentAccessPrivilege.Write));
            Assert.IsFalse(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "arch"), SimComponentAccessPrivilege.Write));
            Assert.IsFalse(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "guest"), SimComponentAccessPrivilege.Write));
        }

        [TestMethod]
        public void GetReferencingParameter()
        {
            LoadProject(parameterProject);

            var param = new SimParameter("B", "u", 1.0);
            Assert.ThrowsException<InvalidOperationException>(() => { param.GetReferencedParameter(); });

            var refTarget = projectData.Components.First(x => x.Name == "ReferenceSource").Parameters.First(x => x.Name == "A");

            var refComp = projectData.Components.First(x => x.Name == "RefParent")
                .Components.First(x => x.Component != null && x.Component.Name == "RefChild").Component;
            refComp.Parameters.Add(param);

            var target = param.GetReferencedParameter();
            Assert.AreEqual(param, target);

            param.Propagation = SimInfoFlow.FromReference;
            target = param.GetReferencedParameter();
            Assert.AreEqual(null, target);

            param.Name = "A";
            target = param.GetReferencedParameter();
            Assert.AreEqual(refTarget, target);
        }

        [TestMethod]
        public void NewParameterReferencing()
        {
            LoadProject(parameterProject);
            projectData.Components.EnableAsyncUpdates = false;

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimParameter("A", "unit", 99.9)
            {
                Propagation = SimInfoFlow.FromReference
            };

            AssertUtil.AssertDoubleEqual(99.9, param.ValueCurrent);

            comp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(55.0, param.ValueCurrent);
        }

        [TestMethod]
        public void ParameterChangeToReferencing()
        {
            LoadProject(parameterProject);
            projectData.Components.EnableAsyncUpdates = false;

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimParameter("A", "unit", 99.9)
            {
                Propagation = SimInfoFlow.Input
            };

            AssertUtil.AssertDoubleEqual(99.9, param.ValueCurrent);

            comp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(99.9, param.ValueCurrent);

            param.Propagation = SimInfoFlow.FromReference;

            AssertUtil.AssertDoubleEqual(55.0, param.ValueCurrent);
        }
    }
}
