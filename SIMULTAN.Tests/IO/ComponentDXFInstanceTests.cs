using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;




namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFInstanceTests : BaseProjectTest
    {
        private static readonly FileInfo emptyProject = new FileInfo(@"./EmptyProject.simultan");

        [TestMethod]
        public void WriteInstance()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimFlowNetworkNode node = new SimFlowNetworkNode(guid, 123, "", "", true, new SimPoint(3.5, 3.6), null);
            SimFlowNetworkNode otherNode = new SimFlowNetworkNode(otherGuid, 124, "", "", true, new SimPoint(3.5, 3.6), null);

            SimNetworkBlock block = new SimNetworkBlock("Block", new SimPoint(12, 13), new SimId(guid, 125), new SimNetworkPort[0], SimColors.DarkGray);

            SimComponent component = new SimComponent()
            {
                Id = new SimId(guid, 999999),
                InstanceType = SimInstanceType.NetworkNode | SimInstanceType.SimNetworkBlock | SimInstanceType.AttributesPoint,
            };
            component.Slots.Add(new SimTaxonomyEntryReference(TaxonomyUtils.GetDefaultSlot(SimDefaultSlotKeys.Undefined)));

            SimDoubleParameter param1 = new SimDoubleParameter("test", "", -2 - 12.5) { Id = new SimId(guid, 336) };
            SimDoubleParameter param2 = new SimDoubleParameter("test", "", -3 - 13.5) { Id = new SimId(otherGuid, 337) };

            SimIntegerParameter param3 = new SimIntegerParameter("test", "", 2) { Id = new SimId(guid, 338) };
            SimIntegerParameter param4 = new SimIntegerParameter("test", "", 4) { Id = new SimId(otherGuid, 339) };

            SimStringParameter param5 = new SimStringParameter("test", "ASD") { Id = new SimId(guid, 340) };
            SimStringParameter param6 = new SimStringParameter("test", "ASD2") { Id = new SimId(otherGuid, 341) };

            SimBoolParameter param7 = new SimBoolParameter("test", true) { Id = new SimId(guid, 342) };
            SimBoolParameter param8 = new SimBoolParameter("test", false) { Id = new SimId(otherGuid, 343) };



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

            SimEnumParameter param9 = new SimEnumParameter("test", baseTaxonomyEntry) { Id = new SimId(guid, 344), Value = new SimTaxonomyEntryReference(taxVal1), };
            SimEnumParameter param10 = new SimEnumParameter("test", baseTaxonomyEntry) { Id = new SimId(otherGuid, 345), Value = new SimTaxonomyEntryReference(taxVal2), };

            component.Parameters.Add(param1);
            component.Parameters.Add(param3);
            component.Parameters.Add(param5);
            component.Parameters.Add(param7);
            component.Parameters.Add(param9);

            SimComponentInstance instance = new SimComponentInstance()
            {
                Name = "Custom Instance",
                Id = new SimId(guid, 3669),
                InstanceRotation = new SimQuaternion(1, 2, 3, 4),
                InstanceSize = new SimInstanceSize(new SimVector3D(-1, -2, -3), new SimVector3D(2, 4, 6)),
                SizeTransfer = new SimInstanceSizeTransferDefinition(new SimInstanceSizeTransferDefinitionItem[6]
                {
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.Parameter, param1, 12.5),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.Parameter, param2, 13.5),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                }),
            };
            instance.Placements.Add(new SimInstancePlacementGeometry(3, 58, SimInstanceType.AttributesPoint, SimInstancePlacementState.Valid));
            instance.Placements.Add(new SimInstancePlacementNetwork(node, SimInstanceType.NetworkNode));
            instance.Placements.Add(new SimInstancePlacementNetwork(otherNode, SimInstanceType.NetworkNode));
            instance.Placements.Add(new SimInstancePlacementSimNetwork(block, SimInstanceType.SimNetworkBlock));
            component.Instances.Add(instance);

            //Parameter values
            instance.PropagateParameterChanges = false;
            instance.InstanceParameterValuesPersistent[param1] = 6677.88;
            instance.InstanceParameterValuesPersistent[param3] = 6;
            instance.InstanceParameterValuesPersistent[param5] = "Different";
            instance.InstanceParameterValuesPersistent[param7] = false;
            instance.InstanceParameterValuesPersistent[param9] = new SimTaxonomyEntryReference(taxVal2);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteInstance(instance, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteInstance, exportedString);
        }


        [TestMethod]
        public void ParseInstanceV27()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimComponentInstance instance = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_InstanceV27)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 27;

                reader.Read();

                instance = ComponentDxfIOComponents.InstanceEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(instance);
            Assert.AreEqual(3669, instance.Id.LocalId);
            Assert.AreEqual("Custom Instance", instance.Name);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new SimQuaternion(1, 2, 3, 4), instance.InstanceRotation);
            Assert.AreEqual(-1.0, instance.InstanceSize.Min.X);
            Assert.AreEqual(12.5, instance.InstanceSize.Min.Y);
            Assert.AreEqual(13.5, instance.InstanceSize.Min.Z);
            Assert.AreEqual(0.0, instance.InstanceSize.Max.X);
            Assert.AreEqual(4.0, instance.InstanceSize.Max.Y);
            Assert.AreEqual(6.0, instance.InstanceSize.Max.Z);


            //Placements
            Assert.AreEqual(4, instance.Placements.Count);
            var gpl = instance.Placements[0] as SimInstancePlacementGeometry;
            Assert.IsNotNull(gpl);
            Assert.AreEqual(SimInstanceType.AttributesFace, gpl.InstanceType);
            Assert.AreEqual(3, gpl.FileId);
            Assert.AreEqual((ulong)58, gpl.GeometryId);

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId);
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.NetworkNode, npl.InstanceType);

            npl = instance.Placements[2] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(otherGuid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId);
            Assert.AreEqual(124, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.NetworkNode, npl.InstanceType);

            loadingMember = typeof(SimInstancePlacementSimNetwork).GetField("loadingElementId",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var snpl = instance.Placements[3] as SimInstancePlacementSimNetwork;
            Assert.IsNotNull(snpl);
            Assert.AreEqual(guid, ((SimId)loadingMember.GetValue(snpl)).GlobalId);
            Assert.AreEqual(125, ((SimId)loadingMember.GetValue(snpl)).LocalId);
            Assert.AreEqual(SimInstanceType.SimNetworkBlock, snpl.InstanceType);

            //Size Transfer
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);

            var stLoadingId = typeof(SimInstanceSizeTransferDefinitionItem).GetField(
                "loadingParameterId", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Source);
            Assert.AreEqual(12.5, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Addend);
            Assert.AreEqual(new SimId(guid, 336), stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Source);
            Assert.AreEqual(13.5, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Addend);
            Assert.AreEqual(new SimId(otherGuid, 337), stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinZ]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Path, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxZ]));

            //Parameters
            Assert.AreEqual(1, instance.LoadingParameterValuesPersistent.Count);
            Assert.AreEqual(new SimId(guid, 336), instance.LoadingParameterValuesPersistent[0].id);
            Assert.AreEqual(String.Empty, instance.LoadingParameterValuesPersistent[0].parameterName);
            Assert.AreEqual(6677.88, instance.LoadingParameterValuesPersistent[0].value);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
        }

        [TestMethod]
        public void ParseInstanceV21()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimComponentInstance instance = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_InstanceV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                instance = ComponentDxfIOComponents.InstanceEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(instance);
            Assert.AreEqual(3669, instance.Id.LocalId);
            Assert.AreEqual("Custom Instance", instance.Name);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new SimQuaternion(1, 2, 3, 4), instance.InstanceRotation);
            Assert.AreEqual(-1.0, instance.InstanceSize.Min.X);
            Assert.AreEqual(12.5, instance.InstanceSize.Min.Y);
            Assert.AreEqual(13.5, instance.InstanceSize.Min.Z);
            Assert.AreEqual(0.0, instance.InstanceSize.Max.X);
            Assert.AreEqual(4.0, instance.InstanceSize.Max.Y);
            Assert.AreEqual(6.0, instance.InstanceSize.Max.Z);


            //Placements
            Assert.AreEqual(4, instance.Placements.Count);
            var gpl = instance.Placements[0] as SimInstancePlacementGeometry;
            Assert.IsNotNull(gpl);
            Assert.AreEqual(3, gpl.FileId);
            Assert.AreEqual((ulong)58, gpl.GeometryId);
            Assert.AreEqual(SimInstanceType.None, gpl.InstanceType); //Gets restored in restore reference

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId);
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.None, npl.InstanceType); //Gets restored in restore reference

            npl = instance.Placements[2] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(otherGuid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId);
            Assert.AreEqual(124, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.None, npl.InstanceType); //Gets restored in restore reference

            loadingMember = typeof(SimInstancePlacementSimNetwork).GetField("loadingElementId",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var snpl = instance.Placements[3] as SimInstancePlacementSimNetwork;
            Assert.IsNotNull(snpl);
            Assert.AreEqual(guid, ((SimId)loadingMember.GetValue(snpl)).GlobalId);
            Assert.AreEqual(125, ((SimId)loadingMember.GetValue(snpl)).LocalId);
            Assert.AreEqual(SimInstanceType.None, snpl.InstanceType); //Gets restored in restore reference

            //Size Transfer
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);

            var stLoadingId = typeof(SimInstanceSizeTransferDefinitionItem).GetField(
                "loadingParameterId", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Source);
            Assert.AreEqual(12.5, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Addend);
            Assert.AreEqual(new SimId(guid, 336), stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Source);
            Assert.AreEqual(13.5, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Addend);
            Assert.AreEqual(new SimId(otherGuid, 337), stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinZ]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Path, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxZ]));

            //Parameters
            Assert.AreEqual(1, instance.LoadingParameterValuesPersistent.Count);
            Assert.AreEqual(new SimId(guid, 336), instance.LoadingParameterValuesPersistent[0].id);
            Assert.AreEqual(String.Empty, instance.LoadingParameterValuesPersistent[0].parameterName);
            Assert.AreEqual(6677.88, instance.LoadingParameterValuesPersistent[0].value);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
        }

        [TestMethod]
        public void ParseInstanceV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimComponentInstance instance = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_InstanceV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();
                instance = ComponentDxfIOComponents.InstanceEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(instance);
            Assert.AreEqual(3669, instance.Id.LocalId);
            Assert.AreEqual("Custom Instance", instance.Name);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new SimQuaternion(1, 2, 3, 4), instance.InstanceRotation);
            Assert.AreEqual(-1.0, instance.InstanceSize.Min.X);
            Assert.AreEqual(12.5, instance.InstanceSize.Min.Y);
            Assert.AreEqual(13.5, instance.InstanceSize.Min.Z);
            Assert.AreEqual(0.0, instance.InstanceSize.Max.X);
            Assert.AreEqual(4.0, instance.InstanceSize.Max.Y);
            Assert.AreEqual(6.0, instance.InstanceSize.Max.Z);


            //Placements
            Assert.AreEqual(4, instance.Placements.Count);
            var gpl = instance.Placements[0] as SimInstancePlacementGeometry;
            Assert.IsNotNull(gpl);
            Assert.AreEqual(3, gpl.FileId);
            Assert.AreEqual((ulong)58, gpl.GeometryId);
            Assert.AreEqual(SimInstanceType.None, gpl.InstanceType); //Gets restored in restore reference

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId);
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.None, npl.InstanceType); //Gets restored in restore reference

            npl = instance.Placements[2] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(otherGuid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId);
            Assert.AreEqual(124, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.None, npl.InstanceType); //Gets restored in restore reference

            loadingMember = typeof(SimInstancePlacementSimNetwork).GetField("loadingElementId",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var snpl = instance.Placements[3] as SimInstancePlacementSimNetwork;
            Assert.IsNotNull(snpl);
            Assert.AreEqual(guid, ((SimId)loadingMember.GetValue(snpl)).GlobalId);
            Assert.AreEqual(125, ((SimId)loadingMember.GetValue(snpl)).LocalId);
            Assert.AreEqual(SimInstanceType.None, snpl.InstanceType); //Gets restored in restore reference

            //Size Transfer
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);

            var stLoadingId = typeof(SimInstanceSizeTransferDefinitionItem).GetField(
                "loadingParameterId", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Source);
            Assert.AreEqual(12.5, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Addend);
            Assert.AreEqual(new SimId(guid, 336), stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Source);
            Assert.AreEqual(13.5, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Addend);
            Assert.AreEqual(new SimId(otherGuid, 337), stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinZ]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Path, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxZ]));

            //Parameters
            Assert.AreEqual(1, instance.LoadingParameterValuesPersistent.Count);
            Assert.AreEqual(new SimId(guid, 336), instance.LoadingParameterValuesPersistent[0].id);
            Assert.AreEqual(String.Empty, instance.LoadingParameterValuesPersistent[0].parameterName);
            Assert.AreEqual(6677.88, instance.LoadingParameterValuesPersistent[0].value);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
        }

        [TestMethod]
        public void ParseInstanceV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimComponentInstance instance = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_InstanceV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();
                instance = ComponentDxfIOComponents.InstanceEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(instance);
            Assert.AreEqual(3669, instance.Id.LocalId);
            Assert.AreEqual("Custom Instance", instance.Name);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new SimQuaternion(1, 2, 3, 4), instance.InstanceRotation);
            Assert.AreEqual(-1.0, instance.InstanceSize.Min.X);
            Assert.AreEqual(12.5, instance.InstanceSize.Min.Y);
            Assert.AreEqual(13.5, instance.InstanceSize.Min.Z);
            Assert.AreEqual(0.0, instance.InstanceSize.Max.X);
            Assert.AreEqual(4.0, instance.InstanceSize.Max.Y);
            Assert.AreEqual(6.0, instance.InstanceSize.Max.Z);


            //Placements
            Assert.AreEqual(2, instance.Placements.Count);
            var gpl = instance.Placements[0] as SimInstancePlacementGeometry;
            Assert.IsNotNull(gpl);
            Assert.AreEqual(3, gpl.FileId);
            Assert.AreEqual((ulong)58, gpl.GeometryId);
            Assert.AreEqual(SimInstanceType.AttributesPoint, gpl.InstanceType);

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId); //<12, use project id instead of saved id
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.NetworkNode, npl.InstanceType);

            //Size Transfer
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);

            var stLoadingId = typeof(SimInstanceSizeTransferDefinitionItem).GetField(
                "loadingParameterId", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Source);
            Assert.AreEqual(12.5, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Addend);
            Assert.AreEqual(new SimId(guid, 336), stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Source);
            Assert.AreEqual(13.5, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Addend);
            Assert.AreEqual(new SimId(guid, 337), stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinZ]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Path, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxZ]));

            //Parameters
            Assert.AreEqual(1, instance.LoadingParameterValuesPersistent.Count);
            Assert.AreEqual(new SimId(guid, 336), instance.LoadingParameterValuesPersistent[0].id);
            Assert.AreEqual(String.Empty, instance.LoadingParameterValuesPersistent[0].parameterName);
            Assert.AreEqual(6677.88, instance.LoadingParameterValuesPersistent[0].value);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
        }

        [TestMethod]
        public void ParseInstanceV7()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimComponentInstance instance = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_InstanceV7)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 7;

                reader.Read();
                instance = ComponentDxfIOComponents.InstanceEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(instance);
            Assert.AreEqual(1077741824, instance.Id.LocalId);
            Assert.AreEqual("Custom Instance", instance.Name);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new SimQuaternion(1, 2, 3, 4), instance.InstanceRotation);
            Assert.AreEqual(-1.0, instance.InstanceSize.Min.X);
            Assert.AreEqual(12.5, instance.InstanceSize.Min.Y);
            Assert.AreEqual(13.5, instance.InstanceSize.Min.Z);
            Assert.AreEqual(0.0, instance.InstanceSize.Max.X);
            Assert.AreEqual(4.0, instance.InstanceSize.Max.Y);
            Assert.AreEqual(6.0, instance.InstanceSize.Max.Z);


            //Placements
            Assert.AreEqual(2, instance.Placements.Count);
            var gpl = instance.Placements[0] as SimInstancePlacementGeometry;
            Assert.IsNotNull(gpl);
            Assert.AreEqual(3, gpl.FileId);
            Assert.AreEqual((ulong)58, gpl.GeometryId);
            Assert.AreEqual(SimInstanceType.AttributesPoint, gpl.InstanceType);

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId); //<12, use project id instead of saved id
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.NetworkNode, npl.InstanceType);

            //Size Transfer
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);

            var stLoadingId = typeof(SimInstanceSizeTransferDefinitionItem).GetField(
                "loadingParameterId", BindingFlags.NonPublic | BindingFlags.Instance);
            var stLoadingParamName = typeof(SimInstanceSizeTransferDefinitionItem).GetField(
                "loadingParameterName", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinX]));
            Assert.AreEqual(String.Empty, stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Source);
            Assert.AreEqual(12.5, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinY]));
            Assert.AreEqual("Parameter A", stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Source);
            Assert.AreEqual(13.5, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinZ]));
            Assert.AreEqual("Parameter B", stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinZ]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Path, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxX]));
            Assert.AreEqual(String.Empty, stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxY]));
            Assert.AreEqual(String.Empty, stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxZ]));
            Assert.AreEqual(String.Empty, stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxZ]));

            //Parameters
            Assert.AreEqual(1, instance.LoadingParameterValuesPersistent.Count);
            Assert.AreEqual(SimId.Empty, instance.LoadingParameterValuesPersistent[0].id);
            Assert.AreEqual("Parameter X", instance.LoadingParameterValuesPersistent[0].parameterName);
            Assert.AreEqual(6677.88, instance.LoadingParameterValuesPersistent[0].value);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
        }

        [TestMethod]
        public void ParseInstanceV6()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimComponentInstance instance = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_InstanceV6)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 6;

                reader.Read();
                instance = ComponentDxfIOComponents.InstanceEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(instance);
            Assert.AreEqual(1077741824, instance.Id.LocalId);
            Assert.AreEqual("Custom Instance", instance.Name);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new SimQuaternion(1, 2, 3, 4), instance.InstanceRotation);
            Assert.AreEqual(-1.0, instance.InstanceSize.Min.X);
            Assert.AreEqual(12.5, instance.InstanceSize.Min.Y);
            Assert.AreEqual(13.5, instance.InstanceSize.Min.Z);
            Assert.AreEqual(0.0, instance.InstanceSize.Max.X);
            Assert.AreEqual(4.0, instance.InstanceSize.Max.Y);
            Assert.AreEqual(6.0, instance.InstanceSize.Max.Z);


            //Placements
            Assert.AreEqual(2, instance.Placements.Count);
            var gpl = instance.Placements[0] as SimInstancePlacementGeometry;
            Assert.IsNotNull(gpl);
            Assert.AreEqual(3, gpl.FileId);
            Assert.AreEqual((ulong)58, gpl.GeometryId);
            Assert.AreEqual(SimInstanceType.AttributesPoint, gpl.InstanceType);

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId); //<12, use project id instead of saved id
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);
            Assert.AreEqual(SimInstanceType.NetworkNode, npl.InstanceType);

            //Size Transfer
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);

            var stLoadingId = typeof(SimInstanceSizeTransferDefinitionItem).GetField(
                "loadingParameterId", BindingFlags.NonPublic | BindingFlags.Instance);
            var stLoadingParamName = typeof(SimInstanceSizeTransferDefinitionItem).GetField(
                "loadingParameterName", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MinX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinX]));
            Assert.AreEqual(String.Empty, stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Source);
            Assert.AreEqual(12.5, instance.SizeTransfer[SimInstanceSizeIndex.MinY].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinY]));
            Assert.AreEqual("Parameter A", stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Parameter, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Source);
            Assert.AreEqual(13.5, instance.SizeTransfer[SimInstanceSizeIndex.MinZ].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinZ]));
            Assert.AreEqual("Parameter B", stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MinZ]));

            Assert.AreEqual(SimInstanceSizeTransferSource.Path, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxX].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxX]));
            Assert.AreEqual(String.Empty, stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxX]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxY].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxY]));
            Assert.AreEqual(String.Empty, stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxY]));

            Assert.AreEqual(SimInstanceSizeTransferSource.User, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Source);
            Assert.AreEqual(0.0, instance.SizeTransfer[SimInstanceSizeIndex.MaxZ].Addend);
            Assert.AreEqual(SimId.Empty, stLoadingId.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxZ]));
            Assert.AreEqual(String.Empty, stLoadingParamName.GetValue(instance.SizeTransfer[SimInstanceSizeIndex.MaxZ]));

            //Parameters
            Assert.AreEqual(1, instance.LoadingParameterValuesPersistent.Count);
            Assert.AreEqual(SimId.Empty, instance.LoadingParameterValuesPersistent[0].id);
            Assert.AreEqual("Parameter X", instance.LoadingParameterValuesPersistent[0].parameterName);
            Assert.AreEqual(6677.88, instance.LoadingParameterValuesPersistent[0].value);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
        }

        #region Restore Instance Type (V13-V26)

        [TestMethod]
        public void RestoreSimNetworkPlacementInstanceTypeBlockV13_V26()
        {
            LoadProject(emptyProject);

            SimNetwork network = new SimNetwork("");
            SimNetworkBlock block = new SimNetworkBlock("", new SimPoint(0, 0));
            network.ContainedElements.Add(block);
            SimNetworkPort portIn = new SimNetworkPort(PortType.Input);
            block.Ports.Add(portIn);
            SimNetworkPort portOut = new SimNetworkPort(PortType.Output);
            block.Ports.Add(portOut);
            projectData.SimNetworks.Add(network);

            SimComponent component = new SimComponent();
            projectData.Components.Add(component);
            SimComponentInstance instance = new SimComponentInstance();
            component.Instances.Add(instance);
            SimInstancePlacementSimNetwork pl = new SimInstancePlacementSimNetwork(block.Id, SimInstanceType.None);
            instance.Placements.Add(pl);

            pl.RestoreReferences(new Dictionary<SimObjectId, SimFlowNetworkElement>());

            Assert.AreEqual(SimInstanceType.SimNetworkBlock, pl.InstanceType);
        }

        [TestMethod]
        public void RestoreSimNetworkPlacementInstanceTypeOutPortV13_V26()
        {
            LoadProject(emptyProject);

            SimNetwork network = new SimNetwork("");
            SimNetworkBlock block = new SimNetworkBlock("", new SimPoint(0, 0));
            network.ContainedElements.Add(block);
            SimNetworkPort portIn = new SimNetworkPort(PortType.Input);
            block.Ports.Add(portIn);
            SimNetworkPort portOut = new SimNetworkPort(PortType.Output);
            block.Ports.Add(portOut);
            projectData.SimNetworks.Add(network);

            SimComponent component = new SimComponent();
            projectData.Components.Add(component);
            SimComponentInstance instance = new SimComponentInstance();
            component.Instances.Add(instance);
            SimInstancePlacementSimNetwork pl = new SimInstancePlacementSimNetwork(portOut.Id, SimInstanceType.None);
            instance.Placements.Add(pl);

            pl.RestoreReferences(new Dictionary<SimObjectId, SimFlowNetworkElement>());

            Assert.AreEqual(SimInstanceType.OutPort, pl.InstanceType);
        }

        [TestMethod]
        public void RestoreSimNetworkPlacementInstanceTypeInPortV13_V26()
        {
            LoadProject(emptyProject);

            SimNetwork network = new SimNetwork("");
            SimNetworkBlock block = new SimNetworkBlock("", new SimPoint(0, 0));
            network.ContainedElements.Add(block);
            SimNetworkPort portIn = new SimNetworkPort(PortType.Input);
            block.Ports.Add(portIn);
            SimNetworkPort portOut = new SimNetworkPort(PortType.Output);
            block.Ports.Add(portOut);
            projectData.SimNetworks.Add(network);

            SimComponent component = new SimComponent();
            projectData.Components.Add(component);
            SimComponentInstance instance = new SimComponentInstance();
            component.Instances.Add(instance);
            SimInstancePlacementSimNetwork pl = new SimInstancePlacementSimNetwork(portIn.Id, SimInstanceType.None);
            instance.Placements.Add(pl);

            pl.RestoreReferences(new Dictionary<SimObjectId, SimFlowNetworkElement>());

            Assert.AreEqual(SimInstanceType.InPort, pl.InstanceType);
        }

        [TestMethod]
        public void RestoreNetworkPlacementInstanceTypeNodeV13_V26()
        {
            LoadProject(emptyProject);

            var network = projectData.NetworkManager.CreateEmptyNetwork("", Data.Users.SimUserRole.ADMINISTRATOR);
            var blockId1 = network.AddNode(new SimPoint(0, 0));
            var block1 = network.ContainedNodes[blockId1];
            var blockId2 = network.AddNode(new SimPoint(1, 0));
            var block2 = network.ContainedNodes[blockId2];
            var edgeId = network.AddEdge(block1, block2);
            var edge = network.ContainedEdges[edgeId];

            SimComponent component = new SimComponent();
            projectData.Components.Add(component);
            SimComponentInstance instance = new SimComponentInstance();
            component.Instances.Add(instance);
            var pl = new SimInstancePlacementNetwork(block1.ID, SimInstanceType.None);
            instance.Placements.Add(pl);

            pl.RestoreReferences(new Dictionary<SimObjectId, SimFlowNetworkElement>()
            {
                { block1.ID, block1 }
            });

            Assert.AreEqual(SimInstanceType.NetworkNode, pl.InstanceType);
        }

        [TestMethod]
        public void RestoreNetworkPlacementInstanceTypeEdgeV13_V26()
        {
            LoadProject(emptyProject);

            var network = projectData.NetworkManager.CreateEmptyNetwork("", Data.Users.SimUserRole.ADMINISTRATOR);
            var blockId1 = network.AddNode(new SimPoint(0, 0));
            var block1 = network.ContainedNodes[blockId1];
            var blockId2 = network.AddNode(new SimPoint(1, 0));
            var block2 = network.ContainedNodes[blockId2];
            var edgeId = network.AddEdge(block1, block2);
            var edge = network.ContainedEdges[edgeId];

            SimComponent component = new SimComponent();
            projectData.Components.Add(component);
            SimComponentInstance instance = new SimComponentInstance();
            component.Instances.Add(instance);
            var pl = new SimInstancePlacementNetwork(edge.ID, SimInstanceType.None);
            instance.Placements.Add(pl);

            pl.RestoreReferences(new Dictionary<SimObjectId, SimFlowNetworkElement>()
            {
                { edge.ID, edge }
            });

            Assert.AreEqual(SimInstanceType.NetworkEdge, pl.InstanceType);
        }

        [TestMethod]
        public void RestoreGeometryPlacementInstanceTypeV13_V26()
        {
            SimComponent component = new SimComponent();
            component.InstanceType = SimInstanceType.AttributesPoint;
            SimComponentInstance instance = new SimComponentInstance();
            component.Instances.Add(instance);
            var pl = new SimInstancePlacementGeometry(99, 123, SimInstanceType.None);
            instance.Placements.Add(pl);

            pl.RestoreReferences(new Dictionary<SimObjectId, SimFlowNetworkElement>());

            Assert.AreEqual(SimInstanceType.AttributesPoint, pl.InstanceType);
        }

        #endregion
    }
}
