using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.SIMUSER;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class SimUserDXFTests
    {
        // in base64
        private static byte[] projectEncKey = Convert.FromBase64String("rqnY3pk+IJBIaWEG0JdaLL8rD2hbbYxKx+p27SHBnGE=");
        private static Guid user1Id = new Guid("58714270-ba3e-4f3a-95a4-b0bf96e918ab");
        private static byte[] user1PwHash = Convert.FromBase64String("jh7ah3HkZ8eA9WTLiNMxF6+70Up1tKDfaIfMTTvip4eJhYXq");
        private static byte[] user1Enc = Convert.FromBase64String("1u21SQIPQEmS0TCUcqGXfv34Y8uao15VfNoSmoHbV/sePcG+kEE/UCOF3dmMIhr0GgG651m/FYqhazlmpSH9s1td+4eOuFoEdFdRakKKAAQ=");
        private static Guid user2Id = new Guid("72e6b180-3f3f-4f36-96c5-ae77d2c852c9");
        private static byte[] user2PwHash = Convert.FromBase64String("iLXx0lWZMVU3PIXoNwHFk7e1RB4kh1sDOPi+cX2YQrSZ7oiw");
        private static byte[] user2Enc = Convert.FromBase64String("QsgWmfO3xYtpdTKOdleuPMUCLMmAFHZfL6IaEIDDxPaNAzEBzKuFZ62SDyRhypn6QkUkmvvKZ6h2KbL27Q8CUO/NpPLIgKNYvA0Xmo5HjyE=");

        [TestMethod]
        public void ReadEmpty()
        {
            var projectData = new ExtendedProjectData();
            var id = Guid.NewGuid();

            List<SimUser> users = null;
            using (var reader = new DXFStreamReader(StringStream.Create(Properties.Resources.DXFSerializer_ReadSIMUSER_EmptyV12)))
            {
                users = SimUserDxfIO.Read(reader, new DXFParserInfo(id, projectData));
            }

            Assert.AreEqual(0, users.Count);
        }

        [TestMethod]
        public void WriteEmpty()
        {
            string export = null;
            using (var stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimUserDxfIO.Write(new SimUser[] { }, writer);
                }
                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                export = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SIMUSER_WriteEmpty, export);
        }

        [TestMethod]
        public void ReadUsersV11()
        {
            var projectData = new ExtendedProjectData();
            var id = Guid.NewGuid();

            List<SimUser> users = null;
            using (var reader = new DXFStreamReader(StringStream.Create(Properties.Resources.DXFSerializer_ReadSIMUSER_UsersV11)))
            {
                users = SimUserDxfIO.Read(reader, new DXFParserInfo(id, projectData), false);
            }

            Assert.AreEqual(2, users.Count);

            var user = users[0];
            Assert.AreEqual(user1Id, user.Id);
            Assert.AreEqual("TestUser", user.Name);
            AssertUtil.ContainEqualValues(user1PwHash, user.PasswordHash);
            AssertUtil.ContainEqualValues(user1Enc, user.EncryptedEncryptionKey);
            Assert.AreEqual(SimUserRole.ADMINISTRATOR, user.Role);

            user = users[1];
            Assert.AreEqual(user2Id, user.Id);
            Assert.AreEqual("TestUser2", user.Name);
            AssertUtil.ContainEqualValues(user2PwHash, user.PasswordHash);
            AssertUtil.ContainEqualValues(user2Enc, user.EncryptedEncryptionKey);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, user.Role);
        }

        [TestMethod]
        public void ReadUsersV12()
        {
            var projectData = new ExtendedProjectData();
            var id = Guid.NewGuid();

            List<SimUser> users = null;
            using (var reader = new DXFStreamReader(StringStream.Create(Properties.Resources.DXFSerializer_ReadSIMUSER_UsersV12)))
            {
                users = SimUserDxfIO.Read(reader, new DXFParserInfo(id, projectData));
            }

            Assert.AreEqual(2, users.Count);

            var user = users[0];
            Assert.AreEqual(user1Id, user.Id);
            Assert.AreEqual("TestUser", user.Name);
            AssertUtil.ContainEqualValues(user1PwHash, user.PasswordHash);
            AssertUtil.ContainEqualValues(user1Enc, user.EncryptedEncryptionKey);
            Assert.AreEqual(SimUserRole.ADMINISTRATOR, user.Role);

            user = users[1];
            Assert.AreEqual(user2Id, user.Id);
            Assert.AreEqual("TestUser2", user.Name);
            AssertUtil.ContainEqualValues(user2PwHash, user.PasswordHash);
            AssertUtil.ContainEqualValues(user2Enc, user.EncryptedEncryptionKey);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, user.Role);
        }

        [TestMethod]
        public void WriteUsers()
        {
            var name = "TestUser";
            var user1 = new SimUser(user1Id, name, user1PwHash, user1Enc, SimUserRole.ADMINISTRATOR);

            name = "TestUser2";
            var user2 = new SimUser(user2Id, name, user2PwHash, user2Enc, SimUserRole.BUILDING_PHYSICS);

            string export = null;
            using (var stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SimUserDxfIO.Write(new SimUser[] { user1, user2 }, writer);
                }
                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                export = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SIMUSER_WriteUsers, export);
        }

        [TestMethod]
        public void FileEncrypt()
        {
            var projectData = new ExtendedProjectData();
            var id = Guid.NewGuid();
            var file = new FileInfo("TestUsersFile.simuser");

            var name = "TestUser";
            var user1 = new SimUser(user1Id, name, user1PwHash, user1Enc, SimUserRole.ADMINISTRATOR);

            name = "TestUser2";
            var user2 = new SimUser(user2Id, name, user2PwHash, user2Enc, SimUserRole.BUILDING_PHYSICS);

            SimUserDxfIO.Write(new SimUser[] { user1, user2 }, new FileInfo("TestUsersFile.simuser"), projectEncKey);

            var users = SimUserDxfIO.Read(file, projectEncKey, new DXFParserInfo(id, projectData));

            Assert.AreEqual(2, users.Count);

            var user = users[0];
            Assert.AreEqual(user1Id, user.Id);
            Assert.AreEqual("TestUser", user.Name);
            AssertUtil.ContainEqualValues(user1PwHash, user.PasswordHash);
            AssertUtil.ContainEqualValues(user1Enc, user.EncryptedEncryptionKey);
            Assert.AreEqual(SimUserRole.ADMINISTRATOR, user.Role);

            user = users[1];
            Assert.AreEqual(user2Id, user.Id);
            Assert.AreEqual("TestUser2", user.Name);
            AssertUtil.ContainEqualValues(user2PwHash, user.PasswordHash);
            AssertUtil.ContainEqualValues(user2Enc, user.EncryptedEncryptionKey);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, user.Role);
        }
    }
}
