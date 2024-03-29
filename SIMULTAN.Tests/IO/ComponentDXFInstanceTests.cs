﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFInstanceTests
    {
        [TestMethod]
        public void WriteInstance()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimFlowNetworkNode node = new SimFlowNetworkNode(guid, 123, "", "", true, new Point(3.5, 3.6), null);
            SimFlowNetworkNode otherNode = new SimFlowNetworkNode(otherGuid, 124, "", "", true, new Point(3.5, 3.6), null);

            SimNetworkBlock block = new SimNetworkBlock("Block", new Point(12, 13), new SimId(guid, 125), new SimNetworkPort[0], new DerivedColor(Colors.DarkGray));

            SimComponent component = new SimComponent()
            {
                Id = new SimId(guid, 999999),
                InstanceType = SimInstanceType.NetworkNode,
                CurrentSlot = new SimTaxonomyEntryReference(TaxonomyUtils.GetDefaultSlot(SimDefaultSlotKeys.Undefined)),
            };

            SimDoubleParameter param1 = new SimDoubleParameter("test", "", -2 - 12.5) { Id = new SimId(guid, 336) };
            SimDoubleParameter param2 = new SimDoubleParameter("test", "", -3 - 13.5) { Id = new SimId(otherGuid, 337) };

            SimIntegerParameter param3 = new SimIntegerParameter("test", "", 2) { Id = new SimId(guid, 338) };
            SimIntegerParameter param4 = new SimIntegerParameter("test", "", 4) { Id = new SimId(otherGuid, 339) };

            SimStringParameter param5 = new SimStringParameter("test", "ASD") { Id = new SimId(guid, 340) };
            SimStringParameter param6 = new SimStringParameter("test", "ASD2") { Id = new SimId(otherGuid, 341) };

            SimBoolParameter param7 = new SimBoolParameter("test", true) { Id = new SimId(guid, 342) };
            SimBoolParameter param8 = new SimBoolParameter("test", false) { Id = new SimId(otherGuid, 343) };



            var tax = new SimTaxonomy(new SimId(1200)) { Name = "Taxonomy" };
            var taxEntry = new SimTaxonomyEntry(new SimId(1201)) { Name = "Parameter X", Key = "key" };
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

            SimComponentInstance instance = new SimComponentInstance(SimInstanceType.NetworkNode)
            {
                Name = "Custom Instance",
                Id = new SimId(guid, 3669),
                InstanceRotation = new Quaternion(1, 2, 3, 4),
                InstanceSize = new SimInstanceSize(new Vector3D(-1, -2, -3), new Vector3D(2, 4, 6)),
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
            instance.Placements.Add(new SimInstancePlacementGeometry(3, 58, SimInstancePlacementState.Valid, null));
            instance.Placements.Add(new SimInstancePlacementNetwork(node));
            instance.Placements.Add(new SimInstancePlacementNetwork(otherNode));
            instance.Placements.Add(new SimInstancePlacementSimNetwork(block));
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
            Assert.AreEqual(SimInstanceType.NetworkNode, instance.InstanceType);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new Quaternion(1, 2, 3, 4), instance.InstanceRotation);
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

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId);
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);

            npl = instance.Placements[2] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(otherGuid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId);
            Assert.AreEqual(124, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);

            loadingMember = typeof(SimInstancePlacementSimNetwork).GetField("loadingElementId",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var snpl = instance.Placements[3] as SimInstancePlacementSimNetwork;
            Assert.IsNotNull(snpl);
            Assert.AreEqual(guid, ((SimId)loadingMember.GetValue(snpl)).GlobalId);
            Assert.AreEqual(125, ((SimId)loadingMember.GetValue(snpl)).LocalId);

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
            Assert.AreEqual(SimInstanceType.NetworkNode, instance.InstanceType);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new Quaternion(1, 2, 3, 4), instance.InstanceRotation);
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

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId); //<12, use project id instead of saved id
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);

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
            Assert.AreEqual(SimInstanceType.NetworkNode, instance.InstanceType);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new Quaternion(1, 2, 3, 4), instance.InstanceRotation);
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

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId); //<12, use project id instead of saved id
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);

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
            Assert.AreEqual(SimInstanceType.NetworkNode, instance.InstanceType);
            Assert.AreEqual(true, instance.State.IsRealized);
            Assert.AreEqual(SimInstanceConnectionState.Ok, instance.State.ConnectionState);

            Assert.AreEqual(new Quaternion(1, 2, 3, 4), instance.InstanceRotation);
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

            var loadingMember = typeof(SimInstancePlacementNetwork).GetField("loadingNetworkElement",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var npl = instance.Placements[1] as SimInstancePlacementNetwork;
            Assert.IsNotNull(npl);
            Assert.AreEqual(guid, ((SimObjectId)loadingMember.GetValue(npl)).GlobalId); //<12, use project id instead of saved id
            Assert.AreEqual(123, ((SimObjectId)loadingMember.GetValue(npl)).LocalId);

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
    }
}
