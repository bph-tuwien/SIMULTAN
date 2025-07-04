﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDxfEnumParameterTests
    {
        #region Parameter

        [TestMethod]
        public void WriteParameter()
        {
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

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteParameter(parameter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameter_Enum, exportedString);
        }




        [TestMethod]
        public void WriteParameterWithTaxonomyEntry()
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


            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteParameter(parameter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithTaxonomyEntry_Enum, exportedString);
        }


        [TestMethod]
        public void ParseParameterV19()
        {
            Guid guid = Guid.NewGuid();
            Guid baseGuid = new Guid("00000000-0000-0000-0000-000000000666");

            SimEnumParameter parameter = null;
            ExtendedProjectData projectData = new ExtendedProjectData();

            var tax = new SimTaxonomy("BaseTax");
            var baseTaxonomyEntry = new SimTaxonomyEntry("BaseEnumTaxEntry", "BaseTaxEntry");


            var location = new DummyReferenceLocation(baseGuid);
            projectData.SetCallingLocation(location);

            tax.Entries.Add(baseTaxonomyEntry);
            projectData.Taxonomies.Add(tax);



            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_EnumParameterV19)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 19;

                reader.Read();
                parameter = ComponentDxfIOComponents.BaseParameterEntityElement.Parse(reader, info) as SimEnumParameter;
            }

            Assert.IsNotNull(parameter);
            Assert.AreEqual("Parameter X", parameter.NameTaxonomyEntry.Text);
        }


        #endregion
    }
}
