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
    public class BoolParameterTests : BaseProjectTest
    {
        private static readonly FileInfo parameterProject = new FileInfo(@"./ParameterTestsProject.simultan");
        private static readonly FileInfo calculationProject = new FileInfo(@"./CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@"./ComponentAccessTestsProject.simultan");
        private static readonly FileInfo parameterAccessProject = new FileInfo(@"./AccessTestsProject.simultan");

        internal void CheckParameter(SimBoolParameter parameter, string name, bool value, SimParameterOperations op)
        {
            Assert.AreEqual(name, parameter.NameTaxonomyEntry.Text);
            Assert.AreEqual(value, parameter.Value);
            Assert.AreEqual(op, parameter.AllowedOperations);
        }

        [TestMethod]
        public void Ctor()
        {
            var parameter = new SimBoolParameter("param", true, SimParameterOperations.EditName);
            CheckParameter(parameter, "param", true, SimParameterOperations.EditName);

            parameter = new SimBoolParameter("param2", true, SimParameterOperations.EditName | SimParameterOperations.EditValue);
            CheckParameter(parameter, "param2", true, SimParameterOperations.EditName | SimParameterOperations.EditValue);
        }

        [TestMethod]
        public void CtorParsing()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_bool");
            var ptr = new SimMultiValueBigTableParameterSource(table, 0, 0);

            //Without pointer
            var param = new SimBoolParameter(99887766, "name", SimCategory.Cooling, SimInfoFlow.Output, false, "textval",
                null, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", false, SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual("textval", param.Description);
            Assert.AreEqual(null, param.ValueSource);

            //With pointer
            param = new SimBoolParameter(99887766, "name", SimCategory.Cooling, SimInfoFlow.Output, true, "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", true, SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual("textval", param.Description);

            var paramPtr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            Assert.AreEqual(table, paramPtr.ValueField);
            Assert.AreEqual(0, paramPtr.Row);
            Assert.AreEqual(0, paramPtr.Column);
        }

        [TestMethod]
        public void Clone()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_bool");
            var ptr = new SimMultiValueBigTableParameterSource(table, 0, 0);

            var paramSource = new SimBoolParameter(99887766, "name", SimCategory.Cooling, SimInfoFlow.Output, true, "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            projectData.Components.StartLoading();
            projectData.Components.First().Parameters.Add(paramSource);
            projectData.Components.EndLoading();

            Assert.AreNotEqual(null, paramSource.Component);

            var param = paramSource.Clone() as SimBoolParameter;

            CheckParameter(param, "name", true, SimParameterOperations.Move);
            Assert.AreEqual(0, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(false, param.IsAutomaticallyGenerated); //Isn't cloned
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual("textval", param.Description);

            var paramPtr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            Assert.AreEqual(table, paramPtr.ValueField);
            Assert.AreEqual(0, paramPtr.Row);
            Assert.AreEqual(0, paramPtr.Column);


            Assert.AreEqual(null, param.Component);
        }

        #region Properties

        [TestMethod]
        public void PropertyAllowedOperations()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.AllowedOperations), SimParameterOperations.All);
        }

        [TestMethod]
        public void PropertyCategory()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescription()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyName()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"), new System.Collections.Generic.List<string> { nameof(SimBoolParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyNameTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry), new System.Collections.Generic.List<string> { nameof(SimBoolParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyPropagation()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationMode()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValue()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.Description), "someText");
        }


        [TestMethod]
        public void PropertyValueCurrent()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.Value), false);
        }


        [TestMethod]
        public void PropertyIsAutomaticallyGenerated()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointer()
        {
            LoadProject(parameterAccessProject);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_Bool");

            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimBoolParameter.ValueSource), table.CreateNewPointer(),
                new System.Collections.Generic.List<string> { nameof(SimBoolParameter.ValueSource), nameof(SimBoolParameter.Value) });
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
            CheckParameterPropertyAccess(nameof(SimBoolParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryAccess()
        {
            CheckParameterPropertyAccess(nameof(SimBoolParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionAccess()
        {
            CheckParameterPropertyAccess(nameof(SimBoolParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameAccess()
        {
            CheckParameterPropertyAccess(nameof(SimBoolParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyPropagationAccess()
        {
            CheckParameterPropertyAccess(nameof(SimBoolParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeAccess()
        {
            CheckParameterPropertyAccess(nameof(SimBoolParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueAccess()
        {
            CheckParameterPropertyAccess(nameof(SimBoolParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyValueCurrentAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.Value), 11.1);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleParameter.Value), 11.1);
        }

        [TestMethod]
        public void PropertyMultiValuePointerAccess()
        {
            LoadProject(parameterAccessProject, "bph", "bph");
            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_Bool");

            var archParameter = archComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter1") as SimDoubleParameter;
            var bphParameter = bphComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter2") as SimDoubleParameter;

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, nameof(SimBoolParameter.ValueSource), table.CreateNewPointer());
        }

        #endregion

        #region Property Changes

        private void CheckParameterPropertyChanges<T>(string prop, T value)
        {
            //Setup
            LoadProject(accessProject, "bph", "bph");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHRoot");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Text == "BPHParameter_Bool");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, prop, value, SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }


        [TestMethod]
        public void PropertyAllowedOperationsChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyNameChangesTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            CheckParameterPropertyChanges(nameof(SimBoolParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry));
        }

        [TestMethod]
        public void PropertyPropagationChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.Description), "someText");
        }



        [TestMethod]
        public void PropertyValueCurrentChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.Value), false);
        }



        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedChanges()
        {
            CheckParameterPropertyChanges(nameof(SimBoolParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerChanges()
        {
            //Setup
            LoadProject(parameterAccessProject, "bph", "bph");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_Bool");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHComp");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter2");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, nameof(SimBoolParameter.ValueSource), table.CreateNewPointer(),
                SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }

        #endregion


        [TestMethod]
        public void HasSameCurrentValue()
        {
            var param = new SimBoolParameter("p1", true, SimParameterOperations.None);

            Assert.IsTrue(param.IsSameValue(true, true));
            Assert.IsFalse(param.IsSameValue(true, false));

            Assert.IsFalse(param.IsSameValue(false, true));
            Assert.IsTrue(param.IsSameValue(false, false));
        }



        [TestMethod]
        public void CheckBaseParamValue()
        {
            var parameter = new SimBoolParameter("p1", true, SimParameterOperations.None) as SimBaseParameter;
            Assert.IsNotNull(parameter.Value);
            Assert.IsTrue(parameter.Value is bool);
            Assert.AreEqual(parameter.Value, true);
        }


        [TestMethod]
        public void StateRefNotFound()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimBoolParameter("B", true)
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.NameTaxonomyEntry = new Data.Taxonomy.SimTaxonomyEntryOrString("Bool_A");
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
            var param = new SimBoolParameter("Bool_A", true)
            {
                Propagation = SimInfoFlow.Output
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("B");
            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("Bool_A");
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

            var param = new SimBoolParameter("B", true);
            Assert.ThrowsException<InvalidOperationException>(() => { param.GetReferencedParameter(); });

            var refTarget = projectData.Components.First(x => x.Name == "ReferenceSource").Parameters.First(x => x.NameTaxonomyEntry.Text == "Bool_A");

            var refComp = projectData.Components.First(x => x.Name == "RefParent")
                .Components.First(x => x.Component != null && x.Component.Name == "RefChild").Component;
            refComp.Parameters.Add(param);

            var target = param.GetReferencedParameter();
            Assert.AreEqual(param, target);

            param.Propagation = SimInfoFlow.FromReference;
            target = param.GetReferencedParameter();
            Assert.AreEqual(null, target);

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("Bool_A");
            target = param.GetReferencedParameter();
            Assert.AreEqual(refTarget, target);
        }

        [TestMethod]
        public void NewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimBoolParameter("Bool_A", true)
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.AreEqual(true, param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual(true, param.Value);
        }




        [TestMethod]
        public void WrongNewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimBoolParameter("Int_A", true)
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.AreEqual(true, param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual(true, param.Value);
        }


        [TestMethod]
        public void ParameterChangeToReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimBoolParameter("Bool_A", true)
            {
                Propagation = SimInfoFlow.Input
            };

            Assert.AreEqual(true, param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual(true, param.Value);

            param.Propagation = SimInfoFlow.FromReference;

            Assert.AreEqual(true, param.Value);
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

            var parameter = new SimBoolParameter(taxEntry, true);
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

            var parameter = new SimBoolParameter(taxEntry, true);
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

            var parameter = new SimBoolParameter(taxEntry, true);
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
