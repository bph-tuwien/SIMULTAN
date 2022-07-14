using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets.Links
{
    internal class DefaultMachineHashGenerator : IMachineHashGenerator
    {
        public int GetMachineHash()
        {
            string local = Environment.MachineName + "_" + Environment.UserDomainName + "_" + Environment.UserName;
            return local.GetHashCode();
        }
    }
}
