using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SIMULTAN.Data.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.ResourceEntríes
{
    [TestClass]
    public class DefaultMachineHashGeneratorTests
    {
        [TestMethod]
        public void TestMachineHash()
        {
            var compareHash = "68FC666CB0AAE19849632C1B0B09258DDAD0461A7F2D4628207FA751C5DD0F88";
            var mock = new Mock<DefaultMachineHashGenerator>();
            mock.Setup(x => x.GetMachineName()).Returns("MachineName");
            mock.Setup(x => x.GetUserDomainName()).Returns("DomainName");
            mock.Setup(x => x.GetUserName()).Returns("UserName");

            var generator = mock.Object;
            var hash = generator.GetMachineHash();

            Assert.AreEqual(compareHash, hash);
        }
    }
}
