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
    public class IntParameterTests : BaseProjectTest
    {
        private static readonly FileInfo parameterProject = new FileInfo(@".\ParameterTestsProject.simultan");
        private static readonly FileInfo calculationProject = new FileInfo(@".\CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@".\ComponentAccessTestsProject.simultan");
        private static readonly FileInfo parameterAccessProject = new FileInfo(@".\AccessTestsProject.simultan");

        internal void CheckParameter(SimIntegerParameter parameter, string name, string unit, int value, int min, int max, SimParameterOperations op)
        {
            Assert.AreEqual(name, parameter.NameTaxonomyEntry.Name);
            Assert.AreEqual(unit, parameter.Unit);
            Assert.AreEqual(value, parameter.Value);
            Assert.AreEqual(min, parameter.ValueMin);
            Assert.AreEqual(max, parameter.ValueMax);
            Assert.AreEqual(op, parameter.AllowedOperations);
        }

        [TestMethod]
        public void Ctor()
        {
            var parameter = new SimIntegerParameter("param", "unit", 5, SimParameterOperations.EditName);
            CheckParameter(parameter, "param", "unit", 5, int.MinValue, int.MaxValue, SimParameterOperations.EditName);

            parameter = new SimIntegerParameter("param2", "unit2", 5, 0, 5, SimParameterOperations.EditName | SimParameterOperations.EditValue);
            CheckParameter(parameter, "param2", "unit2", 5, 0, 5, SimParameterOperations.EditName | SimParameterOperations.EditValue);
        }

        [TestMethod]
        public void CtorParsing()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_int");
            var ptr = new SimMultiValueBigTableParameterSource(table, 1, 2);

            //Without pointer
            var param = new SimIntegerParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99, 0, 99, "textval",
                null, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", "unit", 99, 0, 99, SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual("textval", param.Description);
            Assert.AreEqual(null, param.ValueSource);


            //With pointer
            param = new SimIntegerParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99, 0, 99, "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", "unit", 66, 0, 99, SimParameterOperations.Move);
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
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_int");
            var ptr = new SimMultiValueBigTableParameterSource(table, 1, 2);

            var paramSource = new SimIntegerParameter(99887766, "name", "unit", SimCategory.Cooling, SimInfoFlow.Output, 99, -2, 99, "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            projectData.Components.StartLoading();
            projectData.Components.First().Parameters.Add(paramSource);
            projectData.Components.EndLoading();

            Assert.AreNotEqual(null, paramSource.Component);


            var param = paramSource.Clone() as SimIntegerParameter;

            CheckParameter(param, "name", "unit", 66, -2, 99, SimParameterOperations.Move);
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
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.AllowedOperations), SimParameterOperations.All);
        }

        [TestMethod]
        public void PropertyCategory()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescription()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyName()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"), new System.Collections.Generic.List<string> { nameof(SimIntegerParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyNameTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry), new System.Collections.Generic.List<string> { nameof(SimIntegerParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyPropagation()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationMode()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValue()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyUnit()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrent()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.Value), 11);
        }

        [TestMethod]
        public void PropertyValueMin()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.ValueMin), -20);
        }

        [TestMethod]
        public void PropertyValueMax()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.ValueMax), 20);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGenerated()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointer()
        {
            LoadProject(parameterAccessProject);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_Int");

            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimIntegerParameter.ValueSource), table.CreateNewPointer(),
                new System.Collections.Generic.List<string> { nameof(SimIntegerParameter.ValueSource), nameof(SimIntegerParameter.Value) });
        }

        #endregion

        #region Property Access

        private void CheckParameterPropertyAccess<T>(string prop, T value)
        {
            LoadProject(accessProject, "bph", "bph");
            var bphParameter = projectData.Components.First(x => x.Name == "BPHRoot").Parameters.First(x => x.NameTaxonomyEntry.Name == "BPHParameter") as SimDoubleParameter;
            var archParameter = projectData.Components.First(x => x.Name == "ArchRoot").Parameters.First(x => x.NameTaxonomyEntry.Name == "ArchParameter") as SimDoubleParameter;

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, prop, value);
        }

        [TestMethod]
        public void PropertyAllowedOperationsAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyPropagationAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyUnitAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrentAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.Value), 11.1);
        }

        [TestMethod]
        public void PropertyValueMinAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.ValueMin), -20.4);
        }

        [TestMethod]
        public void PropertyValueMaxAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.ValueMax), 20.6);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedAccess()
        {
            CheckParameterPropertyAccess(nameof(SimIntegerParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerAccess()
        {
            LoadProject(parameterAccessProject, "bph", "bph");
            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var archParameter = archComp.Parameters.First(x => x.NameTaxonomyEntry.Name == "Parameter1") as SimDoubleParameter;
            var bphParameter = bphComp.Parameters.First(x => x.NameTaxonomyEntry.Name == "Parameter2") as SimDoubleParameter;

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, nameof(SimIntegerParameter.ValueSource), table.CreateNewPointer());
        }

        #endregion

        #region Property Changes

        private void CheckParameterPropertyChanges<T>(string prop, T value)
        {
            //Setup
            LoadProject(accessProject, "bph", "bph");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHRoot");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Name == "BPHParameter_Int");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, prop, value, SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }


        [TestMethod]
        public void PropertyAllowedOperationsChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyNameChangesTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry));
        }

        [TestMethod]
        public void PropertyPropagationChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyUnitChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.Unit), "someUnit");
        }

        [TestMethod]
        public void PropertyValueCurrentChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.Value), 11);
        }

        [TestMethod]
        public void PropertyValueMinChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.ValueMin), -20);
        }

        [TestMethod]
        public void PropertyValueMaxChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.ValueMax), 20);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedChanges()
        {
            CheckParameterPropertyChanges(nameof(SimIntegerParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerChanges()
        {
            //Setup
            LoadProject(parameterAccessProject, "bph", "bph");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHComp");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Name == "Parameter2");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, nameof(SimIntegerParameter.ValueSource), table.CreateNewPointer(),
                SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }

        #endregion


        [TestMethod]
        public void HasSameCurrentValue()
        {
            var param = new SimIntegerParameter("p1", "unit", 1, SimParameterOperations.None);

            Assert.IsTrue(param.IsSameValue(1, 1));
            Assert.IsFalse(param.IsSameValue(1, 2));
            Assert.IsFalse(param.IsSameValue(1, default(int)));

            Assert.IsFalse(param.IsSameValue(0, 1));
            Assert.IsTrue(param.IsSameValue(0, default(int)));
        }


        [TestMethod]
        public void StateOutOfRange()
        {
            SimIntegerParameter p = new SimIntegerParameter("name", "unit", 50, 0, 2);
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueOutOfRange));

            p.Value = 1;
            Assert.IsFalse(p.State.HasFlag(SimParameterState.ValueOutOfRange));

            p.Value = -2;
            Assert.IsTrue(p.State.HasFlag(SimParameterState.ValueOutOfRange));
        }

        [TestMethod]
        public void CheckBaseParamValue()
        {
            var parameter = new SimIntegerParameter("name", "unit", 50, 0, 2) as SimBaseParameter;
            Assert.IsNotNull(parameter.Value);
            Assert.IsTrue(parameter.Value is int);
            Assert.AreEqual(parameter.Value, 50);
        }



        [TestMethod]
        public void StateRefNotFound()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimIntegerParameter("B", "unit", 99)
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.NameTaxonomyEntry = new Data.Taxonomy.SimTaxonomyEntryOrString("Int_A");
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
            var param = new SimIntegerParameter("Int_A", "unit", 99)
            {
                Propagation = SimInfoFlow.Output
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("B");
            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("Int_A");
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
        public void HasAccess()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "NotEmpty");
            var param = comp.Parameters.First(x => x.NameTaxonomyEntry.Name == "a");

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

            var param = new SimIntegerParameter("B", "u", 1);
            Assert.ThrowsException<InvalidOperationException>(() => { param.GetReferencedParameter(); });

            var refTarget = projectData.Components.First(x => x.Name == "ReferenceSource").Parameters.First(x => x.NameTaxonomyEntry.Name == "Int_A");

            var refComp = projectData.Components.First(x => x.Name == "RefParent")
                .Components.First(x => x.Component != null && x.Component.Name == "RefChild").Component;
            refComp.Parameters.Add(param);

            var target = param.GetReferencedParameter();
            Assert.AreEqual(param, target);

            param.Propagation = SimInfoFlow.FromReference;
            target = param.GetReferencedParameter();
            Assert.AreEqual(null, target);

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("Int_A");
            target = param.GetReferencedParameter();
            Assert.AreEqual(refTarget, target);
        }

        [TestMethod]
        public void NewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimIntegerParameter("Int_A", "unit", 99)
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.AreEqual(99, param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual(0, param.Value);
        }


        [TestMethod]
        public void WrongNewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimIntegerParameter("Bool_A", "unit", 99)
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.AreEqual(99, param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual(99, param.Value);
        }

        [TestMethod]
        public void ParameterChangeToReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");

            var param = new SimIntegerParameter("Int_A", "unit", 99)
            {
                Propagation = SimInfoFlow.Input
            };

            Assert.AreEqual(99, param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual(99, param.Value);

            param.Propagation = SimInfoFlow.FromReference;

            Assert.AreEqual(0, param.Value);
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
            var taxEntry = new SimTaxonomyEntry("key", entryName);
            taxonomy.Entries.Add(taxEntry);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "Empty");
            Assert.IsNotNull(comp);

            var parameter = new SimIntegerParameter(taxEntry, "", 1);
            Assert.AreEqual(entryName, parameter.NameTaxonomyEntry.Name);
            Assert.AreEqual(taxEntry, parameter.NameTaxonomyEntry.TaxonomyEntryReference.Target);
            comp.Parameters.Add(parameter);

            // now deleting the taxonomy entry should keep the name of the parameter but remove the entry reference
            taxonomy.Entries.Remove(taxEntry);

            Assert.AreEqual(entryName, parameter.NameTaxonomyEntry.Name);
            Assert.IsFalse(parameter.NameTaxonomyEntry.HasTaxonomyEntry());
            Assert.IsFalse(parameter.NameTaxonomyEntry.HasTaxonomyEntryReference());
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

            var parameter = new SimIntegerParameter(taxEntry, "", 1);
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

            var parameter = new SimIntegerParameter(taxEntry, "", 1);
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
