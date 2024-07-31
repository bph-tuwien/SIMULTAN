using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Parameters
{
    [TestClass]
    public class EnumParameterTests : BaseProjectTest
    {
        private static readonly FileInfo parameterProject = new FileInfo(@"./ParameterTestsProject.simultan");
        private static readonly FileInfo calculationProject = new FileInfo(@"./CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@"./ComponentAccessTestsProject.simultan");
        private static readonly FileInfo parameterAccessProject = new FileInfo(@"./AccessTestsProject.simultan");

        internal void CheckParameter(SimEnumParameter parameter, string nameOrKey, SimTaxonomyEntry baseEntry, SimTaxonomyEntry valueEntry, SimParameterOperations op)
        {
            Assert.AreEqual(nameOrKey, parameter.NameTaxonomyEntry.TextOrKey);
            Assert.AreEqual(baseEntry, parameter.ParentTaxonomyEntryRef.Target);
            if (parameter.Value != null)
            {
                Assert.AreEqual(valueEntry, parameter.Value.Target);
            }
            else
            {
                Assert.IsNull(valueEntry);
            }
            Assert.AreEqual(nameOrKey, parameter.NameTaxonomyEntry.TextOrKey);
            Assert.AreEqual(op, parameter.AllowedOperations);
        }

        [TestMethod]
        public void Ctor()
        {

            var tax = new SimTaxonomy(new SimId(1200));
            tax.Languages.Add(CultureInfo.InvariantCulture);
            tax.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, "Taxonomy"));
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");
            tax.Entries.Add(taxEntry);

            var taxonomy = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);

            SimEnumParameter parameter = new SimEnumParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output, baseTaxonomyEntry,
               taxVal1, "text value with spaces", null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
            parameter.AllowedOperations = SimParameterOperations.EditName;
            CheckParameter(parameter, "key", baseTaxonomyEntry, taxVal1, SimParameterOperations.EditName);

        }



        [TestMethod]
        public void Clone()
        {
            LoadProject(parameterProject);

            var tax = new SimTaxonomy(new SimId(1200));
            tax.Languages.Add(CultureInfo.InvariantCulture);
            tax.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, "Taxonomy"));
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");
            tax.Entries.Add(taxEntry);

            var taxonomy = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);

            SimEnumParameter paramSource = new SimEnumParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output, baseTaxonomyEntry,
               taxVal1, "text value with spaces", null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);



            projectData.Components.StartLoading();
            projectData.Components.First().Parameters.Add(paramSource);
            projectData.Components.EndLoading();
            Assert.AreNotEqual(null, paramSource.Component);


            var param = paramSource.Clone() as SimEnumParameter;

            CheckParameter(param, "Parameter X", baseTaxonomyEntry, taxVal1,
                SimParameterOperations.EditValue | SimParameterOperations.EditName);
            Assert.IsFalse(object.ReferenceEquals(param.Value, paramSource.Value));
            Assert.AreEqual(0, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(false, param.IsAutomaticallyGenerated); //Isn't cloned
            Assert.AreEqual(SimParameterInstancePropagation.PropagateAlways, param.InstancePropagationMode);
            Assert.AreEqual("text value with spaces", param.Description);


            Assert.AreEqual(null, param.Component);
        }

        [TestMethod]
        public void CloneWithUnsetValue()
        {
            LoadProject(parameterProject);

            var tax = new SimTaxonomy(new SimId(1200));
            tax.Languages.Add(CultureInfo.InvariantCulture);
            tax.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, "Taxonomy"));
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");
            tax.Entries.Add(taxEntry);

            var taxonomy = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);

            SimEnumParameter paramSource = new SimEnumParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output, baseTaxonomyEntry,
               taxVal1, "text value with spaces", null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);

            paramSource.Value = null;
            Assert.IsNull(paramSource.Value);

            projectData.Components.StartLoading();
            projectData.Components.First().Parameters.Add(paramSource);
            projectData.Components.EndLoading();
            Assert.AreNotEqual(null, paramSource.Component);


            var param = paramSource.Clone() as SimEnumParameter;

            CheckParameter(param, "Parameter X", baseTaxonomyEntry, null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName);
            Assert.AreEqual(param.Value, paramSource.Value);
            Assert.AreEqual(0, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(false, param.IsAutomaticallyGenerated); //Isn't cloned
            Assert.AreEqual(SimParameterInstancePropagation.PropagateAlways, param.InstancePropagationMode);
            Assert.AreEqual("text value with spaces", param.Description);

        }


        /// <summary>
        /// Load the testProject
        /// </summary>
        /// <returns></returns>
        private (SimTaxonomyEntry baseTax, SimTaxonomyEntry selected, SimTaxonomy parentTaxonomy) CreateTaxonomyEnumForParam()
        {

            var tax = new SimTaxonomy(new SimId(1200));
            tax.Languages.Add(CultureInfo.InvariantCulture);
            tax.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, "Taxonomy"));
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");
            tax.Entries.Add(taxEntry);

            var taxonomy = new SimTaxonomy("BaseTax");


            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);


            return (baseTaxonomyEntry, taxVal1, tax);

        }


        [TestMethod]
        public void BaseTaxonomyEntryDeleted()
        {
            LoadProject(parameterProject);
            var defaultEnumTaxEntry = projectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.SIMENUMPARAM_DEFAULT);
            Assert.IsNotNull(defaultEnumTaxEntry);
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");

            var taxonomy = new SimTaxonomy("BaseTax");
            this.projectData.Taxonomies.Add(taxonomy);
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);


            var enumparam = new SimEnumParameter(taxEntry, baseTaxonomyEntry)
            {
                Value = new SimTaxonomyEntryReference(taxVal1),
            };



            this.projectData.Components.First().Parameters.Add(enumparam);

            taxonomy.Entries.Remove(baseTaxonomyEntry);
            Assert.IsNull(enumparam.Value);
            Assert.IsNotNull(enumparam.ParentTaxonomyEntryRef);
            Assert.AreEqual(defaultEnumTaxEntry, enumparam.ParentTaxonomyEntryRef.Target);
        }


        [TestMethod]
        public void ValueTaxonomyEntryDeletedCheck()
        {
            LoadProject(parameterProject);
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");

            var taxonomy = new SimTaxonomy("BaseTax");
            this.projectData.Taxonomies.Add(taxonomy);
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);

            var enumparam = new SimEnumParameter(taxEntry, baseTaxonomyEntry)
            {
                Value = new SimTaxonomyEntryReference(taxVal1),
            };

            this.projectData.Components.First().Parameters.Add(enumparam);

            baseTaxonomyEntry.Children.Remove(taxVal1);
            Assert.IsNull(enumparam.Value);
            Assert.IsNotNull(enumparam.ParentTaxonomyEntryRef);
        }


        [TestMethod]
        public void SetParentNullExceptionCheck()
        {
            LoadProject(parameterProject);
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");

            var taxonomy = new SimTaxonomy("BaseTax");
            this.projectData.Taxonomies.Add(taxonomy);
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);

            var enumparam = new SimEnumParameter(taxEntry, baseTaxonomyEntry)
            {
                Value = new SimTaxonomyEntryReference(taxVal1),
            };

            Assert.ThrowsException<NullReferenceException>(() => { enumparam.ParentTaxonomyEntryRef = null; });
        }


        [TestMethod]
        public void ValueTaxonomyEntryDeletedCheckInstance()
        {
            LoadProject(parameterProject);
            var parentComponent = new SimComponent() { InstanceType = SimInstanceType.SimNetworkBlock };
            this.projectData.Components.Add(parentComponent);
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");

            var taxonomy = new SimTaxonomy("BaseTax");
            this.projectData.Taxonomies.Add(taxonomy);
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);

            var enumparam = new SimEnumParameter(taxEntry, baseTaxonomyEntry)
            {
                Value = new SimTaxonomyEntryReference(taxVal1),
            };
            parentComponent.Parameters.Add(enumparam);

            var networkBlock = new SimNetworkBlock("Block1", new SimPoint(0, 0));
            var compInstance = new SimComponentInstance(networkBlock);
            parentComponent.Instances.Add(compInstance);

            Assert.IsNotNull(parentComponent.Instances[0]);
            Assert.IsNotNull(parentComponent.Instances[0].InstanceParameterValuesPersistent[enumparam]);
            Assert.IsNotNull(parentComponent.Instances[0].InstanceParameterValuesPersistent[enumparam]);

            baseTaxonomyEntry.Children.Remove(taxVal1);
            Assert.IsNull(enumparam.Value);
            Assert.IsNotNull(enumparam.ParentTaxonomyEntryRef);
            Assert.IsNull(parentComponent.Instances[0].InstanceParameterValuesPersistent[enumparam]);
        }


        private static (WeakReference taxVal1Ref, WeakReference baseRef, SimTaxonomy taxonomy) MemoryLeakTestBaseTaxonomyReferenceDelete_Execute(ProjectData projectData)
        {
            var parentComponent = new SimComponent() { InstanceType = SimInstanceType.SimNetworkBlock };
            projectData.Components.Add(parentComponent);
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");

            var taxonomy = new SimTaxonomy("BaseTax");
            projectData.Taxonomies.Add(taxonomy);
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            taxonomy.Entries.Add(baseTaxonomyEntry);
            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);

            var enumparam = new SimEnumParameter(taxEntry, baseTaxonomyEntry)
            {
                Value = new SimTaxonomyEntryReference(taxVal1),
            };
            parentComponent.Parameters.Add(enumparam);

            var taxVal1Ref = new WeakReference(taxVal1);
            var baseRef = new WeakReference(baseTaxonomyEntry);

            return (taxVal1Ref, baseRef, taxonomy);
        }

        private static void MemoryLeakTestBaseTaxonomyReferenceDelete_Execute2(WeakReference taxVal1Ref, WeakReference baseRef)
        {
            ((SimTaxonomyEntry)baseRef.Target).Children.Remove((SimTaxonomyEntry)taxVal1Ref.Target);
        }

        private static void MemoryLeakTestBaseTaxonomyReferenceDelete_Execute3(SimTaxonomy taxonomy, WeakReference baseRef)
        {
            taxonomy.Entries.Remove((SimTaxonomyEntry)baseRef.Target);
        }

        [TestMethod]
        public void MemoryLeakTestBaseTaxonomyReferenceDelete()
        {
            LoadProject(parameterProject);

            var weakRefs = MemoryLeakTestBaseTaxonomyReferenceDelete_Execute(projectData);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(weakRefs.taxVal1Ref.IsAlive);

            MemoryLeakTestBaseTaxonomyReferenceDelete_Execute2(weakRefs.taxVal1Ref, weakRefs.baseRef);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(weakRefs.taxVal1Ref.IsAlive);

            //Remove the base Taxonomy
            MemoryLeakTestBaseTaxonomyReferenceDelete_Execute3(weakRefs.taxonomy, weakRefs.baseRef);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(weakRefs.baseRef.IsAlive);

        }





        private SimTaxonomyEntry GetExistingBaseTaxEntry()
        {
            return this.projectData.Taxonomies.FirstOrDefault(p => p.Localization.Localize().Name == "BASETAX_ENUM").Entries.FirstOrDefault(t => t.Localization.Localize().Name == "BaseEntry");

        }


        private (SimEnumParameter param, SimTaxonomyEntry baseTax, SimTaxonomyEntryReference valueTax, SimTaxonomy parentTaxonomy) CreateEnumParameter(string name = "Parameter X")
        {
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");
            var taxs = CreateTaxonomyEnumForParam();

            var parameter = new SimEnumParameter(99, name,
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output, taxs.baseTax,
               taxs.selected, "text value with spaces", null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateNever, true);

            parameter.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
            return (parameter, taxs.baseTax, parameter.Value, taxs.parentTaxonomy);
        }

        [TestMethod]
        public void CheckBaseParamValue()
        {
            var created = CreateEnumParameter();
            var parameter = created.param as SimBaseParameter;
            Assert.IsNotNull(parameter.Value);
            Assert.IsTrue(parameter.Value is SimTaxonomyEntryReference);
            Assert.AreEqual(parameter.Value, created.valueTax);
        }



        [TestMethod]
        public void CtorParsing()
        {
            LoadProject(parameterProject);

            //Without pointer
            var created = CreateEnumParameter();
            created.param.AllowedOperations = SimParameterOperations.Move;
            created.param.Category = SimCategory.Cooling;
            created.param.Propagation = SimInfoFlow.Output;
            created.param.Description = "description";
            CheckParameter(created.param, "key", created.baseTax, created.valueTax.Target, SimParameterOperations.Move);

            Assert.AreEqual(99, created.param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, created.param.Category);
            Assert.AreEqual(SimInfoFlow.Output, created.param.Propagation);
            Assert.AreEqual("description", created.param.Description);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, created.param.InstancePropagationMode);
            Assert.AreEqual(true, created.param.IsAutomaticallyGenerated);
            Assert.AreEqual(null, created.param.ValueSource);

        }



        #region Properties

        [TestMethod]
        public void PropertyAllowedOperations()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.AllowedOperations), SimParameterOperations.All);
        }


        [TestMethod]
        public void PropertyCategory()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescription()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyName()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"), new System.Collections.Generic.List<string> { nameof(SimEnumParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyNameTaxonomyEntry()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry), new System.Collections.Generic.List<string> { nameof(SimEnumParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyPropagation()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationMode()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValue()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.Description), "someText");
        }



        [TestMethod]
        public void PropertyValueCurrent()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var reference = new SimTaxonomyEntryReference(taxs.selected);
            var otherRef = new SimTaxonomyEntryReference(taxs.baseTax.Children.FirstOrDefault(t => t.Localization.Localize().Name != taxs.selected.Localization.Localize().Name));
            var param = new SimEnumParameter("p1", taxs.baseTax, null, SimParameterOperations.None)
            {
                Value = reference
            };

            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.Value), otherRef);
        }


        [TestMethod]
        public void PropertyValueChangeWhenNull()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var reference = new SimTaxonomyEntryReference(taxs.selected);
            var otherRef = new SimTaxonomyEntryReference(taxs.baseTax.Children.FirstOrDefault(t => t.Localization.Localize().Name != taxs.selected.Localization.Localize().Name));
            var param = new SimEnumParameter("p1", taxs.baseTax, null, SimParameterOperations.None);
            var eventsFired = 0;
            param.PropertyChanged += delegate (object sender, PropertyChangedEventArgs e)
            {
                eventsFired++;
            };
            param.Value = null;
            Assert.AreEqual(0, eventsFired);
        }



        [TestMethod]
        public void PropertyIsAutomaticallyGenerated()
        {
            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("p1", taxs.selected, null, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimEnumParameter.IsAutomaticallyGenerated), true);
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
            CheckParameterPropertyAccess(nameof(SimEnumParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryAccess()
        {
            CheckParameterPropertyAccess(nameof(SimEnumParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionAccess()
        {
            CheckParameterPropertyAccess(nameof(SimEnumParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameAccess()
        {
            CheckParameterPropertyAccess(nameof(SimEnumParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyPropagationAccess()
        {
            CheckParameterPropertyAccess(nameof(SimEnumParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeAccess()
        {
            CheckParameterPropertyAccess(nameof(SimEnumParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueAccess()
        {
            CheckParameterPropertyAccess(nameof(SimEnumParameter.Description), "someText");
        }


        [TestMethod]
        public void PropertyValueCurrentAccess()
        {
            CheckParameterPropertyAccess(nameof(SimEnumParameter.Value), 11.1);
        }


        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedAccess()
        {
            CheckParameterPropertyAccess(nameof(SimEnumParameter.IsAutomaticallyGenerated), true);
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
            CheckParameterPropertyChanges(nameof(SimEnumParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryChanges()
        {
            CheckParameterPropertyChanges(nameof(SimEnumParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionChanges()
        {
            CheckParameterPropertyChanges(nameof(SimEnumParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            CheckParameterPropertyChanges(nameof(SimEnumParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyNameChangesTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            CheckParameterPropertyChanges(nameof(SimEnumParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry));
        }

        [TestMethod]
        public void PropertyPropagationChanges()
        {
            CheckParameterPropertyChanges(nameof(SimEnumParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeChanges()
        {
            CheckParameterPropertyChanges(nameof(SimEnumParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueChanges()
        {
            CheckParameterPropertyChanges(nameof(SimEnumParameter.Description), "someText");
        }


        [TestMethod]
        public void PropertyValueCurrentChanges()
        {
            CheckParameterPropertyChanges(nameof(SimEnumParameter.Value), 11.1);
        }


        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedChanges()
        {
            CheckParameterPropertyChanges(nameof(SimEnumParameter.IsAutomaticallyGenerated), true);
        }


        #endregion


        [TestMethod]
        public void HasSameCurrentValue()
        {

            var param = CreateEnumParameter();
            var simTaxonomyEntry = new SimTaxonomyEntry("demokey");

            Assert.IsTrue(param.param.IsSameValue(param.valueTax, param.valueTax));
            Assert.IsTrue(param.param.IsSameValue(new SimTaxonomyEntryReference(param.valueTax), param.valueTax));
            Assert.IsFalse(param.param.IsSameValue(new SimTaxonomyEntryReference(simTaxonomyEntry), param.valueTax));
            Assert.IsFalse(param.param.IsSameValue(null, param.valueTax));
            Assert.IsTrue(param.param.IsSameValue(null, null));
        }


        [TestMethod]
        public void DeleteParentTaxonomyEntry()
        {

            LoadProject(parameterProject);
            var parentComponent = new SimComponent();
            this.projectData.Components.Add(parentComponent);
            var tax = new SimTaxonomy("BaseTax");
            this.projectData.Taxonomies.Add(tax);
            var taxEntry = new SimTaxonomyEntry("KEYX", "Param X");



            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");
            tax.Entries.Add(baseTaxonomyEntry);

            var taxVal1 = new SimTaxonomyEntry("EnumVal1", "EnumVal1");
            var taxVal2 = new SimTaxonomyEntry("EnumVal2", "EnumVal2");

            baseTaxonomyEntry.Children.Add(taxVal1);
            baseTaxonomyEntry.Children.Add(taxVal2);


            var enumparam = new SimEnumParameter(taxEntry, baseTaxonomyEntry)
            {
                Value = new SimTaxonomyEntryReference(taxVal1),
            };
            parentComponent.Parameters.Add(enumparam);

            tax.Entries.Remove(baseTaxonomyEntry);
            var defaultTaxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.SIMENUMPARAM_DEFAULT);
            Assert.IsNotNull(enumparam.ParentTaxonomyEntryRef);
            Assert.AreEqual(enumparam.ParentTaxonomyEntryRef.Target.Key, defaultTaxEntry.Key);

        }

        [TestMethod]
        public void StateRefNotFound()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");

            var baseTax = this.GetExistingBaseTaxEntry();
            var taxVal = baseTax.Children.FirstOrDefault(t => t.Localization.Localize().Name == "EnumVal2");

            var param = new SimEnumParameter("B", baseTax)
            {
                Propagation = SimInfoFlow.FromReference,
                Value = new SimTaxonomyEntryReference(taxVal)
            };


            Assert.IsFalse(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.ReferenceNotFound));

            param.NameTaxonomyEntry = new Data.Taxonomy.SimTaxonomyEntryOrString("Enum_A");
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

            var baseTax = this.GetExistingBaseTaxEntry();
            var taxVal = baseTax.Children.FirstOrDefault(t => t.Localization.Localize().Name == "EnumVal2");

            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("Enum_A", baseTax)
            {
                Propagation = SimInfoFlow.Output
            };
            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");



            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            comp.Parameters.Add(param);
            Assert.IsTrue(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("B");
            Assert.IsFalse(param.State.HasFlag(SimParameterState.HidesReference));

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("Enum_A");
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

            var taxs = CreateTaxonomyEnumForParam();



            var baseTax = this.GetExistingBaseTaxEntry();
            var taxVal = baseTax.Children.FirstOrDefault(t => t.Localization.Localize().Name == "EnumVal2");

            var param = new SimEnumParameter("B", baseTax)
            {
                Propagation = SimInfoFlow.Input,
                Value = new SimTaxonomyEntryReference(taxVal)
            };

            Assert.ThrowsException<InvalidOperationException>(() => { param.GetReferencedParameter(); });

            var refTarget = projectData.Components.First(x => x.Name == "ReferenceSource").Parameters.First(x => x.NameTaxonomyEntry.Text == "Enum_A");

            var refComp = projectData.Components.First(x => x.Name == "RefParent")
                .Components.First(x => x.Component != null && x.Component.Name == "RefChild").Component;
            refComp.Parameters.Add(param);

            var target = param.GetReferencedParameter();
            Assert.AreEqual(param, target);

            param.Propagation = SimInfoFlow.FromReference;
            target = param.GetReferencedParameter();
            Assert.AreEqual(null, target);

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("Enum_A");
            target = param.GetReferencedParameter();
            Assert.AreEqual(refTarget, target);
        }

        [TestMethod]
        public void NewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");

            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("Enum_A", taxs.baseTax)
            {
                Propagation = SimInfoFlow.Input,
                Value = new SimTaxonomyEntryReference(taxs.selected)
            };



            Assert.AreEqual(taxs.selected, param.Value.Target);

            comp.Parameters.Add(param);

            Assert.AreEqual("EnumVal1", param.Value.Target.Localization.Localize().Name);
        }




        [TestMethod]
        public void WrongNewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");

            var taxs = CreateTaxonomyEnumForParam();
            var param = new SimEnumParameter("Bool_A", taxs.baseTax)
            {
                Propagation = SimInfoFlow.Input,
                Value = new SimTaxonomyEntryReference(taxs.selected)
            };



            Assert.AreEqual(taxs.selected, param.Value.Target);

            comp.Parameters.Add(param);

            Assert.AreEqual(taxs.selected, param.Value.Target);
        }



        [TestMethod]
        public void ParameterChangeToReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");

            var taxs = CreateTaxonomyEnumForParam();

            var baseTaxonomy = this.projectData.Taxonomies.FirstOrDefault(t => t.Localization.Localize().Name == "BASETAX_ENUM");
            var valueTaxEntry = baseTaxonomy.Entries.FirstOrDefault().Children.FirstOrDefault();

            var param = new SimEnumParameter("Enum_A", baseTaxonomy.Entries.FirstOrDefault())
            {
                Propagation = SimInfoFlow.Input,
                Value = new SimTaxonomyEntryReference(valueTaxEntry)
            };


            Assert.AreEqual(valueTaxEntry, param.Value.Target);

            comp.Parameters.Add(param);

            Assert.AreEqual(valueTaxEntry, param.Value.Target);

            param.Propagation = SimInfoFlow.FromReference;

            Assert.AreEqual("EnumVal2", param.Value.Target.Localization.Localize().Name);
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



            var taxs = CreateTaxonomyEnumForParam();
            var parameter = new SimEnumParameter(taxEntry, taxs.selected.Parent);


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

            var taxs = CreateTaxonomyEnumForParam();
            var parameter = new SimEnumParameter(taxEntry, taxs.selected);

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

            var taxs = CreateTaxonomyEnumForParam();
            var parameter = new SimEnumParameter(taxEntry, taxs.selected);

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
