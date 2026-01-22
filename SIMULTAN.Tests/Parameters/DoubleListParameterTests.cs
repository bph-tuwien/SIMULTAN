using MathNet.Numerics.Integration;
using MathNet.Numerics.Statistics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;

namespace SIMULTAN.Tests.Parameters
{
    [TestClass]
    public class DoubleListParameterTests : BaseProjectTest
    {
        private static readonly FileInfo parameterProject = new FileInfo(@"./ParameterTestsProject.simultan");
        private static readonly FileInfo calculationProject = new FileInfo(@"./CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@"./ComponentAccessTestsProject.simultan");
        private static readonly FileInfo parameterAccessProject = new FileInfo(@"./AccessTestsProject.simultan");


        private void AssertValueEquals(SimParameterValueCollection<double> expected, SimParameterValueCollection<double> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
            }
            else
            {
                Assert.AreEqual(expected.Count, actual.Count);
                for (int i = 0; i < expected.Count; i++)
                {
                    AssertUtil.AssertDoubleEqual(expected[i], actual[i]);
                }
            }
        }

        internal void CheckParameter(SimDoubleListParameter parameter, string name, SimParameterValueCollection<double> value, SimParameterOperations op)
        {
            Assert.AreEqual(name, parameter.NameTaxonomyEntry.Text);
            if (value == null)
            {
                Assert.IsNull(parameter.Value);
            }
            else
            {
                Assert.AreEqual(value.Count, parameter.Value.Count);
                Assert.AreEqual(parameter, parameter.Value.Parameter);
                for (int i = 0; i < value.Count; i++)
                {
                    AssertUtil.AssertDoubleEqual(value[i], parameter.Value[i]);
                }
            }
            Assert.AreEqual(op, parameter.AllowedOperations);
        }

        [TestMethod]
        public void Ctor()
        {
            var parameter = new SimDoubleListParameter("param", null, SimParameterOperations.EditName);
            CheckParameter(parameter, "param", null, SimParameterOperations.EditName);

            var values = new[] { 1.0, 2.0, 3.0 };
            parameter = new SimDoubleListParameter("param2", new SimParameterValueCollection<double>(values), SimParameterOperations.EditName | SimParameterOperations.EditValue);
            CheckParameter(parameter, "param2", new SimParameterValueCollection<double>(values), SimParameterOperations.EditName | SimParameterOperations.EditValue);
        }

        [TestMethod]
        public void CtorParsing()
        {
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_A");
            var ptr = new SimMultiValueBigTableParameterSource(table, 1, 2);

            //Without pointer
            var values = new[] { 1.0, 2.0, 3.0 };
            var param = new SimDoubleListParameter(99887766, "name", SimCategory.Cooling, SimInfoFlow.Output, new SimParameterValueCollection<double>(values), "textval",
                null, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", new SimParameterValueCollection<double>(values), SimParameterOperations.Move);
            Assert.AreEqual(99887766, param.Id.LocalId);
            Assert.AreEqual(SimCategory.Cooling, param.Category);
            Assert.AreEqual(SimInfoFlow.Output, param.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateNever, param.InstancePropagationMode);
            Assert.AreEqual(true, param.IsAutomaticallyGenerated);
            Assert.AreEqual("textval", param.Description);
            Assert.AreEqual(null, param.ValueSource);


            //With pointer
            param = new SimDoubleListParameter(99887766, "name", SimCategory.Cooling, SimInfoFlow.Output, new SimParameterValueCollection<double>(values), "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);

            CheckParameter(param, "name", new SimParameterValueCollection<double>(values), SimParameterOperations.Move);
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
            var values = new[] { 1.0, 2.0, 3.0 };
            LoadProject(parameterProject);
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table_A");
            var ptr = new SimMultiValueBigTableParameterSource(table, 1, 2);

            var paramSource = new SimDoubleListParameter(99887766, "name", SimCategory.Cooling, SimInfoFlow.Output, new SimParameterValueCollection<double>(values), "textval",
                ptr, SimParameterOperations.Move, SimParameterInstancePropagation.PropagateNever, true);
            projectData.Components.StartLoading();
            projectData.Components.First().Parameters.Add(paramSource);
            projectData.Components.EndLoading();
            Assert.AreNotEqual(null, paramSource.Component);

            var param = paramSource.Clone() as SimDoubleListParameter;

            CheckParameter(param, "name", new SimParameterValueCollection<double>(values), SimParameterOperations.Move);
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
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.AllowedOperations), SimParameterOperations.All);
        }

        [TestMethod]
        public void PropertyCategory()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescription()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyName()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"), new System.Collections.Generic.List<string> { nameof(SimDoubleListParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyNameTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry), new System.Collections.Generic.List<string> { nameof(SimDoubleListParameter.NameTaxonomyEntry) });
        }

        [TestMethod]
        public void PropertyPropagation()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationMode()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValue()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyValueCurrent()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.Value), new SimParameterValueCollection<double> { 11.0, 12.0, 13.0 });
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGenerated()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0 }, SimParameterOperations.None);
            PropertyTestUtils.CheckProperty(param, nameof(SimDoubleListParameter.IsAutomaticallyGenerated), true);
        }

        #endregion

        #region Property Access

        private void CheckParameterPropertyAccess<T>(string prop, T value)
        {
            LoadProject(accessProject, "bph", "bph");
            var bphParameter = projectData.Components.First(x => x.Name == "BPHRoot").Parameters.OfType<SimDoubleListParameter>().First();
            var archParameter = projectData.Components.First(x => x.Name == "ArchRoot").Parameters.OfType<SimDoubleListParameter>().First();

            PropertyTestUtils.CheckPropertyAccess(bphParameter, archParameter, prop, value);
        }

        [TestMethod]
        public void PropertyAllowedOperationsAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyPropagationAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyValueCurrentAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.Value), new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 });
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.Value), (SimParameterValueCollection<double>)null);
        }

        [TestMethod]
        public void PropertyValueCurrentChangeAccess()
        {
            LoadProject(accessProject, "bph", "bph");
            var bphParameter = projectData.Components.First(x => x.Name == "BPHRoot").Parameters.OfType<SimDoubleListParameter>().First();
            var archParameter = projectData.Components.First(x => x.Name == "ArchRoot").Parameters.OfType<SimDoubleListParameter>().First();

            Assert.AreEqual(3, bphParameter.Count);
            bphParameter.Value.Add(1234.0);
            Assert.AreEqual(4, bphParameter.Count);
            AssertUtil.AssertDoubleEqual(1234.0, bphParameter.Value[3]);
            bphParameter.Value.RemoveAt(0);
            bphParameter.Value.Insert(0, 0.0);

            Assert.ThrowsException<AccessDeniedException>(() => archParameter.Value.Add(1234.0), "Should not be allowed to change parameter");
            Assert.ThrowsException<AccessDeniedException>(() => archParameter.Value.RemoveAt(2), "Should not be allowed to change parameter");
            Assert.ThrowsException<AccessDeniedException>(() => archParameter.Value.Insert(0, 1.0), "Should not be allowed to change parameter");
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedAccess()
        {
            CheckParameterPropertyAccess(nameof(SimDoubleListParameter.IsAutomaticallyGenerated), true);
        }

        #endregion

        #region Property Changes

        private void CheckParameterPropertyChanges<T>(string prop, T value)
        {
            //Setup
            LoadProject(accessProject, "bph", "bph");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHRoot");
            var bphParameter = bphComponent.Parameters.OfType<SimDoubleListParameter>().First();

            PropertyTestUtils.CheckPropertyChanges(bphParameter, prop, value, SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }


        [TestMethod]
        public void PropertyAllowedOperationsChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.AllowedOperations), SimParameterOperations.None);
        }

        [TestMethod]
        public void PropertyCategoryChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.Category), SimCategory.Air);
        }

        [TestMethod]
        public void PropertyDescriptionChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.Description), "randomdescription");
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString("randomName"));
        }

        [TestMethod]
        public void PropertyNameChangesTaxonomyEntry()
        {
            var taxEntry = TaxonomyUtils.GetDefaultTaxonomies().GetReservedParameter(ReservedParameterKeys.RP_COUNT);
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.NameTaxonomyEntry), new SimTaxonomyEntryOrString(taxEntry));
        }

        [TestMethod]
        public void PropertyPropagationChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.Propagation), SimInfoFlow.Input);
        }

        [TestMethod]
        public void PropertyInstancePropagationModeChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.InstancePropagationMode), SimParameterInstancePropagation.PropagateAlways);
        }

        [TestMethod]
        public void PropertyTextValueChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.Description), "someText");
        }

        [TestMethod]
        public void PropertyValueCurrentChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.Value), new SimParameterValueCollection<double> { 1.0 });
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.Value), (SimParameterValueCollection<double>)null);
        }
        [TestMethod]
        public void PropertyValueCurrentChangesCollection()
        {
            LoadProject(accessProject, "bph", "bph");

            var owningComponent = projectData.Components.First(x => x.Name == "BPHRoot");
            var bphParameter = owningComponent.Parameters.OfType<SimDoubleListParameter>().First();

            var collection = projectData.Components;
            var writeRole = SimUserRole.BUILDING_PHYSICS;
            var startAccess = owningComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            var startCollectionAccess = collection.LastChange;

            Assert.AreNotEqual(writeRole, startAccess.role);
            Assert.IsFalse(collection.HasChanges);

            Thread.Sleep(5);

            //Action
            bphParameter.Value.Add(1.0);

            //Checks
            Assert.IsTrue(collection.HasChanges);
            Assert.IsTrue(collection.LastChange > startCollectionAccess);
            Assert.IsTrue(collection.LastChange <= DateTime.Now);

            var endAccess = owningComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(endAccess.lastAccess > startAccess.lastAccess);
            Assert.IsTrue(endAccess.lastAccess <= DateTime.Now);
            Assert.AreEqual(writeRole, endAccess.role);
        }

        [TestMethod]
        public void PropertyIsAutomaticallyGeneratedChanges()
        {
            CheckParameterPropertyChanges(nameof(SimDoubleListParameter.IsAutomaticallyGenerated), true);
        }

        [TestMethod]
        public void PropertyMultiValuePointerChanges()
        {
            //Setup
            LoadProject(parameterAccessProject, "bph", "bph");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHComp");
            var bphParameter = bphComponent.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter2");

            PropertyTestUtils.CheckPropertyChanges(bphParameter, nameof(SimDoubleListParameter.ValueSource), table.CreateNewPointer(),
                SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }

        #endregion


        [TestMethod]
        public void HasSameCurrentValue()
        {
            var param = new SimDoubleListParameter("p1", new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, SimParameterOperations.None);

            Assert.IsTrue(param.IsSameValue(null, null));
            Assert.IsTrue(param.IsSameValue(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }));

            Assert.IsFalse(param.IsSameValue(null, new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }));
            Assert.IsFalse(param.IsSameValue(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, null));
            Assert.IsFalse(param.IsSameValue(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0, 4.0 }, new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }));
            Assert.IsFalse(param.IsSameValue(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, new SimParameterValueCollection<double> { 1.0, 2.0, 3.0, 4.0 }));
            Assert.IsFalse(param.IsSameValue(new SimParameterValueCollection<double> { 123.0, 2.0, 3.0 }, new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }));
        }


        [TestMethod]
        public void CheckBaseParamValue()
        {
            var parameter = new SimDoubleListParameter("name", new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }) as SimBaseParameter;
            Assert.IsNotNull(parameter.Value);
            Assert.IsTrue(parameter.Value is SimParameterValueCollection<double>);
            Assert.AreEqual(parameter, ((SimParameterValueCollection<double>)parameter.Value).Parameter);
        }

        [TestMethod]
        public void GetReferencedParameter()
        {
            LoadProject(parameterProject);

            var param = new SimDoubleListParameter("B", new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 });
            Assert.ThrowsException<InvalidOperationException>(() => { param.GetReferencedParameter(); });

            var refTarget = projectData.Components.First(x => x.Name == "ReferenceSource").Parameters.First(x => x.NameTaxonomyEntry.Text == "DoubleList_A");

            var refComp = projectData.Components.First(x => x.Name == "RefParent")
                .Components.First(x => x.Component != null && x.Component.Name == "RefChild").Component;
            refComp.Parameters.Add(param);

            var target = param.GetReferencedParameter();
            Assert.AreEqual(param, target);

            param.Propagation = SimInfoFlow.FromReference;
            target = param.GetReferencedParameter();
            Assert.AreEqual(null, target);

            param.NameTaxonomyEntry = new SimTaxonomyEntryOrString("DoubleList_A");
            target = param.GetReferencedParameter();
            Assert.AreEqual(refTarget, target);
        }

        [TestMethod]
        public void NewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimDoubleListParameter("DoubleList_A", new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 })
            {
                Propagation = SimInfoFlow.FromReference
            };

            AssertValueEquals(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, param.Value);

            comp.Parameters.Add(param);

            AssertValueEquals(new SimParameterValueCollection<double> { 4.0, 5.0, 6.0 }, param.Value);
        }

        [TestMethod]
        public void WrongNewParameterReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimDoubleListParameter("NONEXIST_A", new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 })
            {
                Propagation = SimInfoFlow.FromReference
            };

            AssertValueEquals(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, param.Value);

            comp.Parameters.Add(param);

            AssertValueEquals(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, param.Value);
        }



        [TestMethod]
        public void ParameterChangeToReferencing()
        {
            LoadProject(parameterProject);

            var comp = projectData.Components.FirstOrDefault(x => x.Name == "WithReference");
            var param = new SimDoubleListParameter("DoubleList_A", new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 })
            {
                Propagation = SimInfoFlow.Input
            };

            AssertValueEquals(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, param.Value);

            comp.Parameters.Add(param);

            AssertValueEquals(new SimParameterValueCollection<double> { 1.0, 2.0, 3.0 }, param.Value);

            param.Propagation = SimInfoFlow.FromReference;

            AssertValueEquals(new SimParameterValueCollection<double> { 4.0, 5.0, 6.0 }, param.Value);
        }
    }
}
