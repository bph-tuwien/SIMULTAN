using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFChatItemTests
    {
        [TestMethod]
        public void WriteChatItem()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            var chatItem = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.BUILDING_PHYSICS,
                "0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", "bdbe91dd450d2bd134a5c429eaa731aa66980dac0629effb8ec025a4403ac63f",
                "", new DateTime(1987, 10, 17, 0, 0, 0, DateTimeKind.Utc),
                "This is the message", SimChatItemState.OPEN,
                new List<SimUserRole> { SimUserRole.MODERATOR, SimUserRole.ENERGY_NETWORK_OPERATOR },
                Enumerable.Empty<SimChatItem>());

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteChatItem(chatItem, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteChatItem, exportedString);
        }

        [TestMethod]
        public void WriteChatItemMultiple()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

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
                new List<SimUserRole> {  },
                Enumerable.Empty<SimChatItem>());

            var chatItem = new SimChatItem(SimChatItemType.ANSWER, SimUserRole.BUILDING_PHYSICS,
                "0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", "bdbe91dd450d2bd134a5c429eaa731aa66980dac0629effb8ec025a4403ac63f",
                "", new DateTime(1987, 10, 17, 0, 0, 0, DateTimeKind.Utc),
                "This is the message", SimChatItemState.OPEN,
                new List<SimUserRole> { SimUserRole.MODERATOR, SimUserRole.ENERGY_NETWORK_OPERATOR },
                new SimChatItem[] { childItem1, childItem2 });

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteChatItem(chatItem, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteChatItem_Multiple, exportedString);
        }

        [TestMethod]
        public void ReadChatItemV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimChatItem chatItem = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ChatItemV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                chatItem = ComponentDxfIOComponents.SimChatItemElement.Parse(reader, info);
            }

            Assert.IsNotNull(chatItem);
            Assert.AreEqual(SimChatItemType.ANSWER, chatItem.Type);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, chatItem.Author);
            Assert.AreEqual("0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", chatItem.VotingRegistration_Address);
            Assert.AreEqual("", chatItem.GitCommitKey);
            Assert.AreEqual(new DateTime(1987, 10, 17, 0, 0, 0, DateTimeKind.Utc), chatItem.TimeStamp);
            Assert.AreEqual("This is the message", chatItem.Message);
            Assert.AreEqual(SimChatItemState.OPEN, chatItem.State);
            Assert.AreEqual(2, chatItem.ExpectsReacionsFrom.Count);
            Assert.IsTrue(chatItem.ExpectsReacionsFrom.Contains(SimUserRole.MODERATOR));
            Assert.IsTrue(chatItem.ExpectsReacionsFrom.Contains(SimUserRole.ENERGY_NETWORK_OPERATOR));

            Assert.AreEqual(2, chatItem.Children.Count);
            
            var child1 = chatItem.Children[0];
            Assert.IsNotNull(child1);
            Assert.AreEqual(SimChatItemType.QUESTION, child1.Type);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child1.Author);
            Assert.AreEqual("0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", child1.VotingRegistration_Address);
            Assert.AreEqual("", child1.GitCommitKey);
            Assert.AreEqual(new DateTime(1987, 10, 18, 0, 0, 0, DateTimeKind.Utc), child1.TimeStamp);
            Assert.AreEqual("This is the better question", child1.Message);
            Assert.AreEqual(SimChatItemState.OPEN, child1.State);
            Assert.AreEqual(1, child1.ExpectsReacionsFrom.Count);
            Assert.IsTrue(child1.ExpectsReacionsFrom.Contains(SimUserRole.MODERATOR));
            Assert.AreEqual(0, child1.Children.Count);

            var child2 = chatItem.Children[1];
            Assert.IsNotNull(child2);
            Assert.AreEqual(SimChatItemType.ANSWER_REJECT, child2.Type);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child2.Author);
            Assert.AreEqual("0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", child2.VotingRegistration_Address);
            Assert.AreEqual("", child2.GitCommitKey);
            Assert.AreEqual(new DateTime(1987, 10, 19, 0, 0, 0, DateTimeKind.Utc), child2.TimeStamp);
            Assert.AreEqual("Rejected!", child2.Message);
            Assert.AreEqual(SimChatItemState.CLOSED, child2.State);
            Assert.AreEqual(0, child2.ExpectsReacionsFrom.Count);
            Assert.AreEqual(0, child2.Children.Count);
        }

        [TestMethod]
        public void ReadChatItemV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimChatItem chatItem = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ChatItemV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                chatItem = ComponentDxfIOComponents.SimChatItemElement.Parse(reader, info);
            }

            Assert.IsNotNull(chatItem);
            Assert.AreEqual(SimChatItemType.ANSWER, chatItem.Type);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, chatItem.Author);
            Assert.AreEqual("0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", chatItem.VotingRegistration_Address);
            Assert.AreEqual("", chatItem.GitCommitKey);
            Assert.AreEqual(new DateTime(1987, 10, 17, 0, 0, 0, DateTimeKind.Utc), chatItem.TimeStamp);
            Assert.AreEqual("This is the message", chatItem.Message);
            Assert.AreEqual(SimChatItemState.OPEN, chatItem.State);
            Assert.AreEqual(2, chatItem.ExpectsReacionsFrom.Count);
            Assert.IsTrue(chatItem.ExpectsReacionsFrom.Contains(SimUserRole.MODERATOR));
            Assert.IsTrue(chatItem.ExpectsReacionsFrom.Contains(SimUserRole.ENERGY_NETWORK_OPERATOR));

            Assert.AreEqual(2, chatItem.Children.Count);

            var child1 = chatItem.Children[0];
            Assert.IsNotNull(child1);
            Assert.AreEqual(SimChatItemType.QUESTION, child1.Type);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child1.Author);
            Assert.AreEqual("0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", child1.VotingRegistration_Address);
            Assert.AreEqual("", child1.GitCommitKey);
            Assert.AreEqual(new DateTime(1987, 10, 18, 0, 0, 0, DateTimeKind.Utc), child1.TimeStamp);
            Assert.AreEqual("This is the better question", child1.Message);
            Assert.AreEqual(SimChatItemState.OPEN, child1.State);
            Assert.AreEqual(1, child1.ExpectsReacionsFrom.Count);
            Assert.IsTrue(child1.ExpectsReacionsFrom.Contains(SimUserRole.MODERATOR));
            Assert.AreEqual(0, child1.Children.Count);

            var child2 = chatItem.Children[1];
            Assert.IsNotNull(child2);
            Assert.AreEqual(SimChatItemType.ANSWER_REJECT, child2.Type);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child2.Author);
            Assert.AreEqual("0xEA8e38626b5D4fad44B974ee3538241b04c6B4D6", child2.VotingRegistration_Address);
            Assert.AreEqual("", child2.GitCommitKey);
            Assert.AreEqual(new DateTime(1987, 10, 19, 0, 0, 0, DateTimeKind.Utc), child2.TimeStamp);
            Assert.AreEqual("Rejected!", child2.Message);
            Assert.AreEqual(SimChatItemState.CLOSED, child2.State);
            Assert.AreEqual(0, child2.ExpectsReacionsFrom.Count);
            Assert.AreEqual(0, child2.Children.Count);
        }
    }
}
