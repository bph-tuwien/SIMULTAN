using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Default machine hash generator. 
    /// Has methods to generate a machine specific unique hash using the username, domain name and machine name.
    /// </summary>
    public class DefaultMachineHashGenerator : IMachineHashGenerator
    {
        /// <summary>
        /// Returns the machine name.
        /// </summary>
        /// <returns>The machine name.</returns>
        public virtual string GetMachineName()
        {
            return Environment.MachineName;
        }

        /// <summary>
        /// Returns the user domain name.
        /// </summary>
        /// <returns>The user domain name.</returns>
        public virtual string GetUserDomainName()
        {
            return Environment.UserDomainName;
        }

        /// <summary>
        /// Returns the user name.
        /// </summary>
        /// <returns>The user name.</returns>
        public virtual string GetUserName()
        {
            return Environment.UserName;
        }

        /// <inheritdoc/>
        public string GetMachineHash()
        {
            string local = GetMachineName() + "_" + GetUserDomainName() + "_" + GetUserName();
            var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(local));
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}
