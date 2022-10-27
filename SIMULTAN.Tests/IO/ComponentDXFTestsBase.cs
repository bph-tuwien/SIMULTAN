using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Users;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Excel;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.IO
{
    public abstract class ComponentDXFTestsBase
    {
        protected DirectoryInfo workingDirectory = null;
        protected DirectoryInfo linkDirectory = null;

        [TestInitialize]
        public void Setup()
        {
            workingDirectory = new DirectoryInfo("./TestWorkingDirectory");
            workingDirectory.Create();

            var rootFolder = workingDirectory.CreateSubdirectory("RootFolder");

            rootFolder.CreateSubdirectory("ChildFolder1");
            rootFolder.CreateSubdirectory("ChildFolder2");

            File.Create("./TestWorkingDirectory/RootFolder/ChildFolder1/MyPublicContainedFile.txt").Dispose();
            File.Create("./TestWorkingDirectory/RootFolder/ChildFolder1/MyContainedFile.txt").Dispose();

            linkDirectory = new DirectoryInfo("./LinkedFiles");
            linkDirectory.Create();
            File.Create("./LinkedFiles/MyLinkedFile.txt").Dispose();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (workingDirectory != null && workingDirectory.Exists)
                workingDirectory.Delete(true);
            workingDirectory = null;

            if (linkDirectory != null && linkDirectory.Exists)
                linkDirectory.Delete(true);
            linkDirectory = null;
        }


        #region TestData

        public void CreateNetworkTestData(ProjectData data)
        {
            //Reset Ids. Needed because it's a static variable
            SimFlowNetworkElement.NR_FL_NET_ELEMENTS = 0;

            var network = data.NetworkManager.CreateEmptyNetwork("Network 1", SimUserRole.BUILDING_PHYSICS);
            network.Name = "Network 1";
            network.Description = "Network Description 1";
            network.IsDirected = false;
            network.RepresentationReference = new Data.GeometricReference(3, 90);
            network.IndexOfGeometricRepFile = 3;

            //Node 1

            var node1Id = network.AddNode(new Point(33.4, -7.6));
            var node1 = network.ContainedNodes[node1Id];
            node1.Name = "Network Node";
            node1.Description = "Description Text";
            node1.CalculationRules.Add(new SimFlowNetworkCalcRule()
            {
                Operator = SimFlowNetworkOperator.Assignment,
                Direction = SimFlowNetworkCalcDirection.Forward,
                Suffix_Operands = "suf op",
                Suffix_Result = "suf result"
            });
            node1.CalculationRules.Add(new SimFlowNetworkCalcRule()
            {
                Operator = SimFlowNetworkOperator.Subtraction,
                Direction = SimFlowNetworkCalcDirection.Backward,
                Suffix_Operands = "suf op 2",
                Suffix_Result = "suf result 2"
            });
            node1.RepresentationReference = new Data.GeometricReference(3, 66);

            //Node 2

            var node2Id = network.AddNode(new Point(36.4, -7.6));
            var node2 = network.ContainedNodes[node2Id];
            node2.Name = "Network Node 2";

            //Edge 1
            var edge1Id = network.AddEdge(node1, node2);
            var edge1 = network.ContainedEdges[edge1Id];
            edge1.Name = "Network Edge 1";
            edge1.Description = "Network Edge Description";
            edge1.RepresentationReference = new Data.GeometricReference(3, 67);

            //SubNetwork
            var subnetId = network.AddFlowNetwork(new Point(40.4, -7.6), "Subnet 1", "A Sub Network");
            var subnet = network.ContainedFlowNetworks[subnetId];
            subnet.Name = "Subnetwork 2";
            subnet.Description = "Network Description 2";
            subnet.IsDirected = true;

            //Edge 2
            var edge2Id = network.AddEdge(subnet, node2);
            var edge2 = network.ContainedEdges[edge2Id];
            edge2.Name = "Network Edge 2";
        }

        public void CreateResourceTestData(ProjectData projectData)
        {
            var rootComponent = projectData.Components.First();

            ResourceDirectoryEntry rootDirectory = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "RootFolder", false, 3, false);
            projectData.AssetManager.AddResourceEntry(rootDirectory);

            ResourceDirectoryEntry childDirectory1 = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.ARCHITECTURE, "RootFolder\\ChildFolder1", false, 4, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(childDirectory1);
            ResourceDirectoryEntry childDirectory2 = new ResourceDirectoryEntry(projectData.AssetManager,
                SimUserRole.ARCHITECTURE, "RootFolder\\ChildFolder2", false, 5, false)
            {
                Visibility = SimComponentVisibility.VisibleInProject
            };
            projectData.AssetManager.AddResourceEntry(childDirectory2);

            ContainedResourceFileEntry geometryResource = new ContainedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "RootFolder\\ChildFolder1\\MyContainedFile.txt", false, 11, false);
            projectData.AssetManager.AddResourceEntry(geometryResource, childDirectory1);

            LinkedResourceFileEntry documentResource = new LinkedResourceFileEntry(projectData.AssetManager,
                SimUserRole.BUILDING_PHYSICS, "MyLinkedFile.txt", false, 12);
            projectData.AssetManager.AddResourceEntry(documentResource, childDirectory1);

            projectData.AssetManager.CreateDocumentAsset(rootComponent, documentResource, "2");
            projectData.AssetManager.CreateGeometricAsset(rootComponent.Id.LocalId, geometryResource.Key, "3");
        }

        public void CreateUserListsTestData(ProjectData data)
        {
            var list = new SimUserComponentList("demolist", new SimComponent[]
            {
                data.Components[0],
                data.Components[0].Components[0].Component
            });
            data.UserComponentLists.Add(list);

            var list2 = new SimUserComponentList("demo list 2", new SimComponent[]
            {

            });
            data.UserComponentLists.Add(list2);

            var list3 = new SimUserComponentList("demolist3", new SimComponent[]
            {
                data.Components[0].Components[0].Component
            });
            data.UserComponentLists.Add(list3);
        }

        public void CreateComponentTestData(ProjectData data, Guid guid, Guid otherguid)
        {
            var node = data.NetworkManager.NetworkRecord.First().ContainedNodes.Values.First();

            SimComponent childComponent1 = new SimComponent()
            {
                Id = new Data.SimId(Guid.Empty, 131),
                Name = "Child Component 1",
                Description = "Some\r\ndescriptive\r\ntext2",
                IsAutomaticallyGenerated = false,
                CurrentSlot = new SimSlotBase(SimDefaultSlots.Cost),
                ComponentColor = Color.FromArgb(230, 100, 10, 20),
                InstanceType = SimInstanceType.AttributesFace,
                Visibility = SimComponentVisibility.VisibleInProject,
                SortingType = SimComponentContentSorting.ByName,
            };
            childComponent1.AccessLocal[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.All;
            childComponent1.AccessLocal[SimUserRole.ARCHITECTURE].LastAccessSupervize = new DateTime(2022, 05, 05, 0, 0, 0, DateTimeKind.Utc);
            childComponent1.AccessLocal[SimUserRole.ARCHITECTURE].LastAccessWrite = new DateTime(2022, 05, 06, 0, 0, 0, DateTimeKind.Utc);
            SimComponent childComponent2 = new SimComponent()
            {
                Id = new Data.SimId(Guid.Empty, 141),
                Name = "Child Component 2",
                Description = "Some\r\ndescriptive\r\ntext3",
                IsAutomaticallyGenerated = true,
                CurrentSlot = new SimSlotBase(SimDefaultSlots.Undefined),
                ComponentColor = Color.FromArgb(230, 240, 10, 32),
                InstanceType = SimInstanceType.AttributesPoint,
                Visibility = SimComponentVisibility.VisibleInProject,
                SortingType = SimComponentContentSorting.ByName,
            };
            childComponent2.AccessLocal[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.All;
            childComponent2.AccessLocal[SimUserRole.ARCHITECTURE].LastAccessSupervize = new DateTime(2022, 05, 05, 0, 0, 0, DateTimeKind.Utc);
            childComponent2.AccessLocal[SimUserRole.ARCHITECTURE].LastAccessRelease = new DateTime(2022, 05, 06, 0, 0, 0, DateTimeKind.Utc);
            childComponent2.AccessLocal[SimUserRole.ARCHITECTURE].LastAccessWrite = new DateTime(2022, 05, 07, 0, 0, 0, DateTimeKind.Utc);

            //Parameter
            childComponent1.Parameters.Add(new SimParameter("A1", "aunit", 34.6, SimParameterOperations.All)
            {
                Id = new Data.SimId(20011),
                Propagation = SimInfoFlow.Input,
                Category = SimCategory.Air,
            });
            childComponent1.Parameters.Add(new SimParameter("B1", "bunit", 35.7, SimParameterOperations.All)
            {
                Id = new Data.SimId(20012),
                Propagation = SimInfoFlow.Output,
                Category = SimCategory.FireSafety
            });

            childComponent2.Parameters.Add(new SimParameter("A", "aunit", 34.6, SimParameterOperations.All)
            {
                Id = new Data.SimId(20001),
                Propagation = SimInfoFlow.Input,
                Category = SimCategory.Air
            });
            childComponent2.Parameters.Add(new SimParameter("B", "bunit", 35.7, SimParameterOperations.All)
            {
                Id = new Data.SimId(20002),
                Propagation = SimInfoFlow.Output,
                Category = SimCategory.FireSafety
            });

            //Instances
            childComponent2.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesPoint)
            {
                Id = new SimId(guid, 3669),
                Name = "Custom Instance",
                State = new SimInstanceState(true, SimInstanceConnectionState.Ok),
                InstanceRotation = new Quaternion(1, 2, 3, 4),
                InstanceSize = new SimInstanceSize(new Vector3D(-1, -2, -3), new Vector3D(0, 4, 6)),
                SizeTransfer = new SimInstanceSizeTransferDefinition(new SimInstanceSizeTransferDefinitionItem[]
                {
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.Parameter, childComponent2.Parameters[1], 12.5),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.Path, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                }),
                InstancePath = new List<Point3D> { new Point3D(0.175, 0, 0.18) },
                PropagateParameterChanges = false,
            });
            childComponent2.Instances[0].Placements.Add(new SimInstancePlacementNetwork(node));
            childComponent2.Instances[0].InstanceParameterValuesPersistent[childComponent2.Parameters[0]] = 6677.88;
            childComponent2.Instances[0].InstanceParameterValuesPersistent[childComponent2.Parameters[1]] = 45.7;

            childComponent2.Instances.Add(new SimComponentInstance(SimInstanceType.AttributesPoint)
            {
                Id = new SimId(guid, 3670),
                Name = "Custom Instance 2",
                State = new SimInstanceState(true, SimInstanceConnectionState.Ok),
                InstanceRotation = new Quaternion(1, 2, 3, 4),
                InstanceSize = new SimInstanceSize(new Vector3D(-2, -4, -6), new Vector3D(0, 8, 12)),
                SizeTransfer = new SimInstanceSizeTransferDefinition(new SimInstanceSizeTransferDefinitionItem[]
                {
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.Parameter, childComponent2.Parameters[0], 12.5),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.Path, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                    new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 0.0),
                }),
                InstancePath = new List<Point3D> { new Point3D(0.175, 0, 0.18) },
                PropagateParameterChanges = false,
            });
            childComponent2.Instances[1].Placements.Add(new SimInstancePlacementGeometry(3, 332));
            childComponent2.Instances[1].InstanceParameterValuesPersistent[childComponent2.Parameters[0]] = -1;
            childComponent2.Instances[1].InstanceParameterValuesPersistent[childComponent2.Parameters[1]] = -2;

            //Calculation
            childComponent2.Calculations.Add(new SimCalculation("x*x", "AB-Calc",
                new Dictionary<string, SimParameter> { { "x", childComponent2.Parameters[0] } },
                new Dictionary<string, SimParameter> { { "ret", childComponent2.Parameters[1] } })
            {
                Id = new SimId(30001)
            });

            SimComponent root = new SimComponent()
            {
                Id = new Data.SimId(Guid.Empty, 123),
                Name = "Root Component",
                Description = "Some\r\ndescriptive\r\ntext",
                IsAutomaticallyGenerated = true,
                CurrentSlot = new SimSlotBase(SimDefaultSlots.Joint),
                ComponentColor = Color.FromArgb(230, 240, 10, 20),
                Visibility = SimComponentVisibility.AlwaysVisible,
                SortingType = SimComponentContentSorting.BySlot,
            };
            root.Components.Add(new SimChildComponentEntry(new SimSlot(SimDefaultSlots.Cost, "0"), childComponent1));
            root.Components.Add(new SimChildComponentEntry(new SimSlot(SimDefaultSlots.Undefined, "1"), childComponent2));
            root.Components.Add(new SimChildComponentEntry(new SimSlot(SimDefaultSlots.Cost, "1")));

            root.AccessLocal[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.All;
            root.AccessLocal[SimUserRole.ARCHITECTURE].LastAccessWrite = new DateTime(2022, 05, 05, 0, 0, 0, DateTimeKind.Utc);
            root.AccessLocal[SimUserRole.ARCHITECTURE].LastAccessSupervize = new DateTime(2022, 05, 06, 0, 0, 0, DateTimeKind.Utc);
            root.AccessLocal[SimUserRole.ARCHITECTURE].LastAccessRelease = new DateTime(2022, 05, 07, 0, 0, 0, DateTimeKind.Utc);

            //References
            childComponent1.ReferencedComponents.Add(new SimComponentReference(new SimSlot(new SimSlotBase(SimDefaultSlots.Joint), "0")));
            childComponent1.ReferencedComponents.Add(new SimComponentReference(new SimSlot(new SimSlotBase(SimDefaultSlots.Joint), "1"),
                childComponent2));
            childComponent1.ReferencedComponents.Add(new SimComponentReference(new SimSlot(new SimSlotBase(SimDefaultSlots.Joint), "2"),
                new SimId(otherguid, 8877)));
            childComponent1.ReferencedComponents.Add(new SimComponentReference(new SimSlot(new SimSlotBase(SimDefaultSlots.Joint), "3"),
                new SimId(guid, 4456)));

            //Mappings
            childComponent1.CreateMappingTo("My Mapping", childComponent2,
                new CalculatorMapping.MappingParameterTuple[] {
                    new CalculatorMapping.MappingParameterTuple(childComponent1.Parameters[0], childComponent2.Parameters[0])
                },
                new CalculatorMapping.MappingParameterTuple[] {
                    new CalculatorMapping.MappingParameterTuple(childComponent1.Parameters[1], childComponent2.Parameters[1])
                });

            //Excel Mapping
            var excelMap1 = new ExcelComponentMapping(new long[] { 5566, 5567, 5568 }, "ExcelTool", "ExcelRuleA", 1);
            root.MappingsPerExcelTool.Add(excelMap1.ConstructKey(), excelMap1);
            var excelMap2 = new ExcelComponentMapping(new long[] { 5569 }, "ExcelTool", "ExcelRuleB", 2);
            root.MappingsPerExcelTool.Add(excelMap2.ConstructKey(), excelMap2);

            //Chat
            var childItem1 = new SimChatItem(SimChatItemType.QUESTION, SimUserRole.BUILDING_PHYSICS,
                "0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", "bdbe91dd450d2bd134a5c429eaa731aa66980dac0629effb8ec025a4403ac63f",
                "", new DateTime(1987, 10, 18, 0, 0, 0, DateTimeKind.Utc),
                "This is the better question", SimChatItemState.OPEN,
                new List<SimUserRole> { SimUserRole.MODERATOR },
                Enumerable.Empty<SimChatItem>());

            var childItem2 = new SimChatItem(SimChatItemType.ANSWER_REJECT, SimUserRole.BUILDING_PHYSICS,
                "0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", "bdbe91dd450d2bd134a5c429eaa731aa66980dac0629effb8ec025a4403ac63f",
                "", new DateTime(1987, 10, 19, 0, 0, 0, DateTimeKind.Utc),
                "Rejected!", SimChatItemState.CLOSED,
                new List<SimUserRole> { },
                Enumerable.Empty<SimChatItem>());

            var chatItem = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.BUILDING_PHYSICS,
                "0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", "bdbe91dd450d2bd134a5c429eaa731aa66980dac0629effb8ec025a4403ac63f",
                "", new DateTime(1987, 10, 17, 0, 0, 0, DateTimeKind.Utc),
                "This is the message", SimChatItemState.OPEN,
                new List<SimUserRole> { SimUserRole.MODERATOR, SimUserRole.ENERGY_NETWORK_OPERATOR },
                new SimChatItem[] { childItem1, childItem2 });

            root.Conversation.AddItem(chatItem);


            data.Components.StartLoading();
            data.Components.Add(root);
            data.Components.EndLoading();
        }

        public void CreateValueMappingTestData(ProjectData data, Guid guid)
        {
            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(guid, 2151483658);
            data.ValueManager.StartLoading();
            data.ValueManager.Add(table);
            data.ValueManager.EndLoading();

            data.ValueMappings.StartLoading();
            var valueMap1 = new SimValueMapping("my mapping 1", table, new SimMinimumPrefilter(), new SimLinearGradientColorMap(
                new SimColorMarker[]
                {
                    new SimColorMarker(-1.0, Color.FromArgb(255, 255, 0, 0)),
                    new SimColorMarker(5.0, Color.FromArgb(255, 0, 255, 0)),
                    new SimColorMarker(99.0, Color.FromArgb(255, 0, 0, 255)),
                }))
            {
                Id = new SimId(data.SitePlannerManager.CalledFromLocation.GlobalID, 665577),
                ComponentIndexUsage = SimComponentIndexUsage.Column,
            };
            data.ValueMappings.Add(valueMap1);
            var valueMap2 = new SimValueMapping("my mapping 2", table, new SimAveragePrefilter(), new SimThresholdColorMap(
                new SimColorMarker[]
                {
                    new SimColorMarker(-1.0, Color.FromArgb(255, 255, 0, 0)),
                    new SimColorMarker(5.0, Color.FromArgb(255, 0, 255, 0)),
                }))
            {
                Id = new SimId(data.SitePlannerManager.CalledFromLocation.GlobalID, 665578),
                ComponentIndexUsage = SimComponentIndexUsage.Row,
            };
            data.ValueMappings.Add(valueMap2);
        }

        public void CreateSimNetworkTestData(ExtendedProjectData projectData, Guid guid)
        {
            projectData.SimNetworks.StartLoading();

            SimNetwork network = new SimNetwork("My Network")
            {
                Id = new SimId(guid, 65001),
                Position = new System.Windows.Point(45, 46)
            };
            projectData.SimNetworks.Add(network);

            SimNetworkBlock b1 = new SimNetworkBlock("Block A", new System.Windows.Point(1, 2))
            {
                Id = new SimId(guid, 65002)
            };
            SimNetworkBlock b2 = new SimNetworkBlock("Block B", new System.Windows.Point(11, 22))
            {
                Id = new SimId(guid, 65003)
            };

            network.ContainedElements.Add(b1);
            network.ContainedElements.Add(b2);

            var p1 = new SimNetworkPort(PortType.Output)
            {
                Name = "My Port A",
                Id = new SimId(guid, 65004)
            };
            b1.Ports.Add(p1);

            var p3 = new SimNetworkPort(PortType.Input)
            {
                Name = "My Port C",
                Id = new SimId(guid, 65005)
            };
            b1.Ports.Add(p3);

            var p2 = new SimNetworkPort(PortType.Input)
            {
                Name = "My Port B",
                Id = new SimId(guid, 65006)
            };
            b2.Ports.Add(p2);

            var con = new SimNetworkConnector(p1, p2)
            {
                Name = "My Connector",
                Id = new SimId(guid, 65007)
            };
            network.ContainedConnectors.Add(con);

            var np1 = new SimNetworkPort(PortType.Output)
            {
                Name = "Network Port 1",
                Id = new SimId(guid, 65008)
            };
            network.Ports.Add(np1);

            SimNetwork childNetwork = new SimNetwork("Child Network")
            {
                Id = new SimId(guid, 65009),
            };
            network.ContainedElements.Add(childNetwork);

            projectData.SimNetworks.EndLoading();
        }


        public ExtendedProjectData CreateTestData()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;

            CreateNetworkTestData(projectData);
            CreateComponentTestData(projectData, guid, otherGuid);
            CreateResourceTestData(projectData);
            CreateUserListsTestData(projectData);
            CreateSimNetworkTestData(projectData, guid);
            CreateValueMappingTestData(projectData, guid);

            return projectData;
        }

        public ExtendedProjectData CreateTestData(out Guid guid)
        {
            guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));
            projectData.AssetManager.WorkingDirectory = workingDirectory.FullName;

            CreateNetworkTestData(projectData);
            CreateComponentTestData(projectData, guid, otherGuid);
            CreateResourceTestData(projectData);
            CreateUserListsTestData(projectData);
            CreateSimNetworkTestData(projectData, guid);
            CreateValueMappingTestData(projectData, guid);

            return projectData;
        }

        #endregion
    }
}
