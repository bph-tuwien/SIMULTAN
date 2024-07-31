using SIMULTAN.Data.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.TestUtils
{
    internal class DummyMachineHashGenerator : IMachineHashGenerator
    {
        public string GetMachineHash()
        {
            return "11223344";
        }
    }
}
