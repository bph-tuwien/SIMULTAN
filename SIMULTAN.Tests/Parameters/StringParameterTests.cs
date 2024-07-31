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
    public class StringParameterTests : BaseProjectTest
    {
        private static readonly FileInfo parameterProject = new FileInfo(@"./ParameterTestsProject.simultan");
        private static readonly FileInfo calculationProject = new FileInfo(@"./CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@"./ComponentAccessTestsProject.simultan");
        private static readonly FileInfo parameterAccessProject = new FileInfo(@"./AccessTestsProject.simultan");

        internal void CheckParameter(SimStringParameter parameter, string name, string value, SimParameterOperations op)
        {
            Assert.AreEqual(name, parameter.NameTaxonomyEntry.Text);
            Assert.AreEqual(value, parameter.Value);
            Assert.AreEqual(op, parameter.AllowedOperations);
        }

        [TestMethod]
        public void Ctor()
        {
            var parameter = new SimStringParameter("param", "ASDASD", SimParameterOperations.EditName);
            CheckParameter(parameter, "param", "ASDASD", SimParameterOperations.EditName);

            parameter = new SimStringParameter("param2", "ASD", SimParameterOperations.EditName | SimParameterOperations.EditValue);
            CheckParameter(parameter, "param2", "ASD", SimParameterOperations.EditName | SimParameterOperations.EditValue);
        }

        [TestMethod]
        public void CheckBaseParamValue()
        {
            var parameter = new SimStringParameter("param", "ASDASD", SimParameterOperations.EditName) as SimBaseParameter;
            Assert.IsNotNull(parameter.Value);
            Assert.IsTrue(parameter.Value is string);
            Assert.AreEqual(parameter.Value, "ASDASD");

        }

        [TestMethod]
        public void CtorParsing()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_string");
            var ptr = new SimMultiValueBigTableParameterSource(table, 0, 1);



            //Without pointer
            var param = new SimStringParameter(99887766, "name", SimCategory.Cooling, SimInfoFlow.Output, "ASD", "description",
                null, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", "ASD", SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual("description", param.Description);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual(null, param.ValueSource);


            //With pointer
            param = new SimStringParameter(99887766, "name", SimCategory.Cooling, SimInfoFlow.Output, "ASD", "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", "ASD", SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual("textval", param.Description);

            var paramPtr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            Assert.AreEqual(table, paramPtr.ValueField);
            Assert.AreEqual(0, paramPtr.Row);
            Assert.AreEqual(1, paramPtr.Column);

        }

        [TestMethod]
        public void Clone()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_string");
            var ptr = new SimMultiValueBigTableParameterSource(table, 0, 1);
            var paramSource = new SimStringParameter(55667788, "name", SimCategory.Cooling, SimInfoFlow.Output, "ASD",
                "textval", ptr, SimParameterOperations.None, SimParameterInstancePropagation.PropagateNever, true);

            projectData.Components.StartLoading();
            projectData.Components.First().Parameters.Add(paramSource);
            projectData.Components.EndLoading();

            Assert.AreNotEqual(null, paramSource.Component);


            var param = paramSource.Clone() as SimStringParameter;

            CheckParameter(param, "name", "ASD", SimParameterOperations.None);
            Assert.AreEqual(0, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(false, param.IsAutomaticallyGenerated); //Isn't cloned
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual("textval", param.Description);

            var paramPtr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            Assert.AreEqual(table, paramPtr.ValueField);
            Assert.AreEqual(0, paramPtr.Row);
            Assert.AreEqual(1, paramPtr.Column);

            Assert.AreEqual(null, param.Component);
        }

        #region Properties

        [TestMethod]
        public void PropertyAllowedOperations()
        {
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.AllowedOperations), SimParameterOperations.All);
        }

        [TestMethod]
        public void PropertyCategory()
        {
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescription()
        {
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyName()
        {
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"), new System.Collections.Generic.List<string> { nameof(SimStringParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyNameTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry), new System.Collections.Generic.List<string> { nameof(SimStringParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyPropagation()
        {
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationMode()
        {
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValue()
        {
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.Description), "someText");
        }



        [TestMethod]
        public void PropertyValueCurrent()
        {
            var param = new SimStringParameter("p1", "1", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.Value), "11.1");
        }


        [TestMethod]
        public void PropertyIsAutomaticallyGenerated()
        {
            var param = new SimStringParameter("NAME", "ASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointer()
        {
            LoadProject(parameterAccessProject);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_String");

            var param = new SimStringParameter("p1", "ASDASD", SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimStringParameter.ValueSource), table.CreateNewPointer(),
                new System.Collections.Generic.List<string> { nameof(SimStringParameter.ValueSource), nameof(SimStringParameter.Value) });
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
            CheckParameterPropertyAccess(nameof(SimStringParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryAccess()
        {
            CheckParameterPropertyAccess(nameof(SimStringParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionAccess()
        {
            CheckParameterPropertyAccess(nameof(SimStringParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameAccess()
        {
            CheckParameterPropertyAccess(nameof(SimStringParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyPropagationAccess()
        {
            CheckParameterPropertyAccess(nameof(SimStringParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeAccess()
        {
            CheckParameterPropertyAccess(nameof(SimStringParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueAccess()
        {
            CheckParameterPropertyAccess(nameof(SimStringParameter.Description), "someText");
        }


        [TestMethod]
        public void PropertyValueCurrentAccess()
        {
            CheckParameterPropertyAccess(nameof(SimStringParameter.Value), 11.1);
        }


        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedAccess()
        {
            CheckParameterPropertyAccess(nameof(SimStringParameter.IsAutomaticallyGenerated), true);
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

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, nameof(SimStringParameter.ValueSource), table.CreateNewPointer());
        }

        #endregion

        #region Property Changes

        private void CheckParameterPropertyChanges<T>(string prop, T value)
        {
            //Setup
            LoadProject(accessProject, "bph", "bph");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHRoot");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Text == "BPHParameter_String");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, prop, value, SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }


        [TestMethod]
        public void PropertyAllowedOperationsChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyNameChangesTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            CheckParameterPropertyChanges(nameof(SimStringParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry));
        }

        [TestMethod]
        public void PropertyPropagationChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.Description), "someText");
        }


        [TestMethod]
        public void PropertyValueCurrentChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.Value), "STRINGVALUE");
        }


        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedChanges()
        {
            CheckParameterPropertyChanges(nameof(SimStringParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerChanges()
        {
            //Setup
            LoadProject(parameterAccessProject, "bph", "bph");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHComp");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter2");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, nameof(SimStringParameter.ValueSource), table.CreateNewPointer(),
                SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }

        #endregion


        [TestMethod]
        public void HasSameCurrentValue()
        {
            var param = new SimStringParameter("p1", "ASD", SimParameterOperations.None);

            Assert.IsTrue(param.IsSameValue("ASD", "ASD"));
            Assert.IsFalse(param.IsSameValue("ASD", "OnePointOne"));
            Assert.IsFalse(param.IsSameValue("ASD", default(string)));

            Assert.IsFalse(param.IsSameValue(default(string), "ASD"));
            Assert.IsTrue(param.IsSameValue(default(string), default(string)));
        }



        [TestMethod]
        public void StateRefNotFound()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimStringParameter("B", "99.9")
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.NameTaxonomyEntry = new Data.Taxonomy.SimTaxonomyEntryOrString("String_A");
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
            var param = new SimStringParameter("String_A", "ASD")
            {
                Propagation = SimInfoFlow.Output
            };

            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("B");
            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("String_A");
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

            var param = new SimStringParameter("B", "ASD");
            Assert.ThrowsException<InvalidOperationException>(() => { param.GetReferencedParameter(); });

            var refTarget = projectData.Components.First(x => x.Name == "ReferenceSource").Parameters.First(x => x.NameTaxonomyEntry.Text == "String_A");

            var refComp = projectData.Components.First(x => x.Name == "RefParent")
                .Components.First(x => x.Component != null && x.Component.Name == "RefChild").Component;
            refComp.Parameters.Add(param);

            var target = param.GetReferencedParameter();
            Assert.AreEqual(param, target);

            param.Propagation = SimInfoFlow.FromReference;
            target = param.GetReferencedParameter();
            Assert.AreEqual(null, target);

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("String_A");
            target = param.GetReferencedParameter();
            Assert.AreEqual(refTarget, target);
        }

        [TestMethod]
        public void NewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimStringParameter("String_A", "99.9")
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.AreEqual("99.9", param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual("ASD", param.Value);
        }

        [TestMethod]
        public void WronNewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimStringParameter("Bool_A", "99.9")
            {
                Propagation = SimInfoFlow.FromReference
            };

            Assert.AreEqual("99.9", param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual("99.9", param.Value);
        }



        [TestMethod]
        public void ParameterChangeToReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimStringParameter("String_A", "99.9")
            {
                Propagation = SimInfoFlow.Input
            };

            Assert.AreEqual("99.9", param.Value);

            comp.Parameters.Add(param);

            Assert.AreEqual("99.9", param.Value);

            param.Propagation = SimInfoFlow.FromReference;

            Assert.AreEqual("ASD", param.Value);
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

            var parameter = new SimStringParameter(taxEntry, "ASD");
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

            var parameter = new SimStringParameter(taxEntry, "ASD");
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

            var parameter = new SimStringParameter(taxEntry, "ASD");
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
