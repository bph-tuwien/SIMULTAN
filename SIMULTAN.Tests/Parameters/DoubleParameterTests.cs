using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using System;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Parameters
{
    [TestClass]
    public class DoubleParameterTests : BaseProjectTest
    {
        private static readonly FileInfo parameterProject = new FileInfo(@"./ParameterTestsProject.simultan");
        private static readonly FileInfo calculationProject = new FileInfo(@"./CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@"./ComponentAccessTestsProject.simultan");
        private static readonly FileInfo parameterAccessProject = new FileInfo(@"./AccessTestsProject.simultan");

        internal void CheckParameter(SimDoubleParameter parameter, string name, string unit, double value, double min, double max, SimParameterOperations op)
        {
            Assert.AreEqual(name, parameter.NameTaxonomyEntry.Text);
            Assert.AreEqual(unit, parameter.Unit);
            AssertUtil.AssertDoubleEqual(value, parameter.Value);
            AssertUtil.AssertDoubleEqual(min, parameter.ValueMin);
            AssertUtil.AssertDoubleEqual(max, parameter.ValueMax);
            Assert.AreEqual(op, parameter.AllowedOperations);
        }

        [TestMethod]
        public void Ctor()
        {
            var parameter = new SimDoubleParameter("param", "unit", 0.5, SimParameterOperations.EditName);
            CheckParameter(parameter, "param", "unit", 0.5, double.NegativeInfinity, double.PositiveInfinity, SimParameterOperations.EditName);

            parameter = new SimDoubleParameter("param2", "unit2", 0.5, -5, 5, SimParameterOperations.EditName | SimParameterOperations.EditValue);
            CheckParameter(parameter, "param2", "unit2", 0.5, -5, 5, SimParameterOperations.EditName | SimParameterOperations.EditValue);
        }

        [TestMethod]
        public void CtorParsing()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_A");
            var ptr = new SimMultiValueBigTableParameterSource(table, 1, 2);

            //Without pointer
            var param = new SimDoubleParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99.5, -2, 99.7, "textval",
                null, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", "unit", 99.5, -2, 99.7, SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual("textval", param.Description);
            Assert.AreEqual(null, param.ValueSource);


            //With pointer
            param = new SimDoubleParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99.5, -2, 99.7, "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", "unit", 6, -2, 99.7, SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual("textval", param.Description);

            var paramPtr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            Assert.AreEqual(table, paramPtr.ValueField);
            Assert.AreEqual(1, paramPtr.Row);
            Assert.AreEqual(2, paramPtr.Column);
        }

        [TestMethod]
        public void Clone()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_A");
            var ptr = new SimMultiValueBigTableParameterSource(table, 1, 2);

            var paramSource = new SimDoubleParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99.5, -2, 99.7, "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);
            projectData.Components.StartLoading();
            projectData.Components.First().Parameters.Add(paramSource);
            projectData.Components.EndLoading();
            Assert.AreNotEqual(null, paramSource.Component);


            var param = paramSource.Clone() as SimDoubleParameter;

            CheckParameter(param, "name", "unit", 6.0, -2, 99.7, SimParameterOperations.Move);
            Assert.AreEqual(0, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(false, param.IsAutomaticallyGenerated); //Isn't cloned
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual("textval", param.Description);

            var paramPtr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            Assert.AreEqual(table, paramPtr.ValueField);
            Assert.AreEqual(1, paramPtr.Row);
            Assert.AreEqual(2, paramPtr.Column);

            Assert.AreEqual(null, param.Component);
        }

        #region Properties

        [TestMethod]
        public void PropertyAllowedOperations()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.AllowedOperations), SimParameterOperations.All);
        }

        [TestMethod]
        public void PropertyCategory()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescription()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyName()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"), new System.Collections.Generic.List<string> { nameof(SimDoubleParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyNameTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry), new System.Collections.Generic.List<string> { nameof(SimDoubleParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyPropagation()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationMode()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValue()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyUnit()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrent()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.Value), 11.1);
        }

        [TestMethod]
        public void PropertyValueMin()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.ValueMin), -20.4);
        }

        [TestMethod]
        public void PropertyValueMax()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.ValueMax), 20.6);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGenerated()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointer()
        {
            LoadProject(parameterAccessProject);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleParameter.ValueSource), table.CreateNewPointer(),
                new System.Collections.Generic.List<string> { nameof(SimDoubleParameter.ValueSource), nameof(SimDoubleParameter.Value) });
        }

        #endregion

        #region Property Access

        private void CheckParameterPropertyAccess<T>(string prop, T value)
        {
            LoadProject(accessProject, "bph", "bph");
            var bphParameter = projectData.Components.First(x => x.Name == "BPHRoot").Parameters.First(x => x.NameTaxonomyEntry.Text == "BPHParameter") as SimDoubleParameter;
            var archParameter = projectData.Components.First(x => x.Name == "ArchRoot").Parameters.First(x => x.NameTaxonomyEntry.Text == "ArchParameter") as SimDoubleParameter;

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, prop, value);
        }

        [TestMethod]
        public void PropertyAllowedOperationsAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyPropagationAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyUnitAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrentAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.Value), 11.1);
        }

        [TestMethod]
        public void PropertyValueMinAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.ValueMin), -20.4);
        }

        [TestMethod]
        public void PropertyValueMaxAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.ValueMax), 20.6);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerAccess()
        {
            LoadProject(parameterAccessProject, "bph", "bph");
            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var archParameter = archComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter1") as SimDoubleParameter;
            var bphParameter = bphComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter2") as SimDoubleParameter;

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, nameof(SimDoubleParameter.ValueSource), table.CreateNewPointer());
        }

        #endregion

        #region Property Changes

        private void CheckParameterPropertyChanges<T>(string prop, T value)
        {
            //Setup
            LoadProject(accessProject, "bph", "bph");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHRoot");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Text == "BPHParameter");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, prop, value, SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }


        [TestMethod]
        public void PropertyAllowedOperationsChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyNameChangesTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry));
        }

        [TestMethod]
        public void PropertyPropagationChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyUnitChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrentChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.Value), 11.1);
        }

        [TestMethod]
        public void PropertyValueMinChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.ValueMin), -20.4);
        }

        [TestMethod]
        public void PropertyValueMaxChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.ValueMax), 20.6);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerChanges()
        {
            //Setup
            LoadProject(parameterAccessProject, "bph", "bph");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHComp");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter2");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, nameof(SimDoubleParameter.ValueSource), table.CreateNewPointer(),
                SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }

        #endregion


        [TestMethod]
        public void HasSameCurrentValue()
        {
            var param = new SimDoubleParameter("p1", "unit", 1.0, SimParameterOperations.None);

            Assert.IsTrue(param.IsSameValue(1.0, 1.0));
            Assert.IsFalse(param.IsSameValue(1.0, 1.1));
            Assert.IsFalse(param.IsSameValue(1.0, double.NaN));

            Assert.IsFalse(param.IsSameValue(double.NaN, 1.0));
            Assert.IsTrue(param.IsSameValue(double.NaN, double.NaN));
        }

        [TestMethod]
        public void StateNan()
        {
            SimDoubleParameter p = new SimDoubleParameter("name", "unit", double.NaN);
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueNaN));

            p.Value = 1.0;
            Assert.IsFalse(p.State.HasFlag(SimParameterState.ValueNaN));

            p.Value = double.NaN;
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueNaN));
        }

        [TestMethod]
        public void StateOutOfRange()
        {
            SimDoubleParameter p = new SimDoubleParameter("name", "unit", 50.0, 0.0, 2.0);
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueOutOfRange));

            p.Value = 1.0;
            Assert.IsFalse(p.State.HasFlag(SimParameterState.ValueOutOfRange));

            p.Value = -2.0;
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueOutOfRange));
        }


        [TestMethod]
        public void CheckBaseParamValue()
        {
            var parameter = new SimDoubleParameter("name", "unit", 50.0, 0, 2) as SimBaseParameter;
            Assert.IsNotNull(parameter.Value);
            Assert.IsTrue(parameter.Value is double);
            Assert.AreEqual(parameter.Value, 50.0);
        }



        [TestMethod]
        public void StateRefNotFound()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimDoubleParameter("B", "unit", 99.9)
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.NameTaxonomyEntry = new Data.Taxonomy.SimTaxonomyEntryOrString("A");
            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            var reference = comp.ReferencedComponents.First();
            var refSlot = reference.Slot;

            reference.Target = null;
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            reference.Target = projectData.Components.First(x => x.Name == "ReferenceSource");
            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.NameTaxonomyEntry = new Data.Taxonomy.SimTaxonomyEntryOrString("B");
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.Propagation = SimInfoFlow.Output;
            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));
        }

        [TestMethod]
        public void StateHidesReference()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimDoubleParameter("A", "unit", 99.9)
            {
                Propagation = SimInfoFlow.Output
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("B");
            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("A");
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

            var param = new SimDoubleParameter("p", "u", 1.0);
            Assert.AreEqual(0, param.ReferencingCalculations.Count);

            var c2 = projectData.Components.First(x => x.Name == "ReferenceUsingCalcParent")
                .Components.First(x => x.Component != null && x.Component.Name == "ReferenceUsingCalc").Component;

            var calc = c2.Calculations.First();

            param = c2.Parameters.First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "noref") as SimDoubleParameter;
            var calcs = param.ReferencingCalculations;
            Assert.AreEqual(1, calcs.Count);
            Assert.AreEqual(calc, calcs[0]);

            param = c2.Parameters.First(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "out") as SimDoubleParameter;
            calcs = param.ReferencingCalculations;
            Assert.AreEqual(1, calcs.Count);
            Assert.AreEqual(calc, calcs[0]);
        }

        [TestMethod]
        public void HasAccess()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "NotEmpty");
            var param = comp.Parameters.First(x => x.NameTaxonomyEntry.Text == "a");

            Assert.IsTrue(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "admin"), SimComponentAccessPrivilege.Read));
            Assert.IsTrue(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "arch"), SimComponentAccessPrivilege.Read));
            Assert.IsFalse(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "guest"), SimComponentAccessPrivilege.Read));

            Assert.IsTrue(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "admin"), SimComponentAccessPrivilege.Write));
            Assert.IsFalse(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "arch"), SimComponentAccessPrivilege.Write));
            Assert.IsFalse(param.HasAccess(projectData.UsersManager.Users.First(x => x.Name == "guest"), SimComponentAccessPrivilege.Write));
        }

        [TestMethod]
        public void GetReferencedParameter()
        {
            LoadProject(parameterProject);

            var param = new SimDoubleParameter("B", "u", 1.0);
            Assert.ThrowsException<InvalidOperationException>(() => { param.GetReferencedParameter(); });

            var refTarget = projectData.Components.First(x => x.Name == "ReferenceSource").Parameters.First(x => x.NameTaxonomyEntry.Text == "A");

            var refComp = projectData.Components.First(x => x.Name == "RefParent")
                .Components.First(x => x.Component != null && x.Component.Name == "RefChild").Component;
            refComp.Parameters.Add(param);

            var target = param.GetReferencedParameter();
            Assert.AreEqual(param, target);

            param.Propagation = SimInfoFlow.FromReference;
            target = param.GetReferencedParameter();
            Assert.AreEqual(null, target);

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("A");
            target = param.GetReferencedParameter();
            Assert.AreEqual(refTarget, target);
        }

        [TestMethod]
        public void NewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimDoubleParameter("A", "unit", 99.9)
            {
                Propagation = SimInfoFlow.FromReference
            };

            AssertUtil.AssertDoubleEqual(99.9, param.Value);

            comp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(55.0, param.Value);
        }


        [TestMethod]
        public void WrongNewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimDoubleParameter("Bool_A", "unit", 99.9)
            {
                Propagation = SimInfoFlow.FromReference
            };

            AssertUtil.AssertDoubleEqual(99.9, param.Value);

            comp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(99.9, param.Value);
        }




        [TestMethod]
        public void ParameterChangeToReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimDoubleParameter("A", "unit", 99.9)
            {
                Propagation = SimInfoFlow.Input
            };

            AssertUtil.AssertDoubleEqual(99.9, param.Value);

            comp.Parameters.Add(param);

            AssertUtil.AssertDoubleEqual(99.9, param.Value);

            param.Propagation = SimInfoFlow.FromReference;

            AssertUtil.AssertDoubleEqual(55.0, param.Value);
        }

        /// <summary>
        /// A Parameter should preserve it's previous name if the taxonomy entry gets deleted
        /// </summary>
        [TestMethod]
        public void KeepNameOnTaxonomyEntryDelete()
        {
            LoadProject(parameterProject);

            // first create a taxonomy and entry
            var taxonomy = new SimTaxonomy("TestTaxonomy");
            projectData.Taxonomies.Add(taxonomy);

            var entryName = "TestEntry";
            var entryKey = "key";
            var taxEntry = new SimTaxonomyEntry(entryKey, entryName);
            taxonomy.Entries.Add(taxEntry);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "Empty");
            Assert.IsNotNull(comp);

            var parameter = new SimDoubleParameter(taxEntry, "", 1);
            Assert.AreEqual(entryKey, parameter.NameTaxonomyEntry.TextOrKey);
            Assert.AreEqual(taxEntry, parameter.NameTaxonomyEntry.TaxonomyEntryReference.Target);
            comp.Parameters.Add(parameter);

            // now deleting the taxonomy entry should keep the name of the parameter but remove the entry reference
            taxonomy.Entries.Remove(taxEntry);

            Assert.AreEqual(entryKey, parameter.NameTaxonomyEntry.TextOrKey);
            Assert.IsFalse(parameter.NameTaxonomyEntry.HasTaxonomyEntry);
            Assert.IsFalse(parameter.NameTaxonomyEntry.HasTaxonomyEntryReference);
        }

        private WeakReference KeepNameOnTaxonomyEntryDeleteMemoryLeak_Action()
        {

            // first create a taxonomy and entry
            var taxonomy = new SimTaxonomy("TestTaxonomy");
            projectData.Taxonomies.Add(taxonomy);

            var entryName = "TestEntry";
            var taxEntry = new SimTaxonomyEntry("key", entryName);
            taxonomy.Entries.Add(taxEntry);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "Empty");

            var parameter = new SimDoubleParameter(taxEntry, "", 1);
            comp.Parameters.Add(parameter);

            var wref = new WeakReference(taxEntry);

            Assert.IsTrue(wref.IsAlive);

            // now deleting the taxonomy entry should keep the name of the parameter but remove the entry reference
            taxonomy.Entries.Remove(taxEntry);
            return wref;
        }
        [TestMethod]
        public void KeepNameOnTaxonomyEntryDeleteMemoryLeak()
        {
            LoadProject(parameterProject);

            var wref = KeepNameOnTaxonomyEntryDeleteMemoryLeak_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(wref.IsAlive);
        }

        private WeakReference DeleteParamterWithTaxEntryMemoryLeak_Action()
        {

            // first create a taxonomy and entry
            var taxonomy = new SimTaxonomy("TestTaxonomy");
            projectData.Taxonomies.Add(taxonomy);

            var entryName = "TestEntry";
            var taxEntry = new SimTaxonomyEntry("key", entryName);
            taxonomy.Entries.Add(taxEntry);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "Empty");

            var parameter = new SimDoubleParameter(taxEntry, "", 1);
            comp.Parameters.Add(parameter);

            var wref = new WeakReference(parameter);

            Assert.IsTrue(wref.IsAlive);

            // Tax entry should not keep the parameter alive
            comp.Parameters.Remove(parameter);
            return wref;
        }
        [TestMethod]
        public void DeleteParamterWithTaxEntryMemoryLeak()
        {
            LoadProject(parameterProject);

            var wref = DeleteParamterWithTaxEntryMemoryLeak_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(wref.IsAlive);
        }
    }
}
