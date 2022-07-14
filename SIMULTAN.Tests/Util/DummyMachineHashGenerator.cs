using SIMULTAN.Data.Assets.Links;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Util
{
    internal class DummyMachineHashGenerator : IMachineHashGenerator
    {
        public int GetMachineHash()
        {
            return 11223344;
        }
    }
}
