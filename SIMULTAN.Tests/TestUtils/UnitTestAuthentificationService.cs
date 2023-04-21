using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.TestUtils
{
    public class UnitTestAuthentificationService : IAuthenticationService
    {
        private string user, password;

        public UnitTestAuthentificationService(string user, string password)
        {
            this.user = user;
            this.password = password;
        }

        public SimUser Authenticate(SimUsersManager userManager, FileInfo projectFile)
        {
            SecureString passwd = new SecureString();
            foreach (var c in password)
                passwd.AppendChar(c);

            var authResult = userManager.Authenticate(user, passwd);

            userManager.CurrentUser = authResult.user;
            userManager.EncryptionKey = authResult.encryptionKey;
            return authResult.user;
        }
    }
}
