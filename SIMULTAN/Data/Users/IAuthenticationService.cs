﻿using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Users
{
    /// <summary>
    /// Provides methods to authenticate a user
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user.
        /// This method has to set <see cref="SimUsersManager.CurrentUser"/> and 
        /// <see cref="SimUsersManager.EncryptionKey"/> in case a valid user has been found
        /// </summary>
        /// <param name="userManager">The userManager</param>
        /// <param name="projectFile">The project File</param>
        /// <returns>Either a valid user or Null when authentication fails</returns>
        SimUser Authenticate(SimUsersManager userManager, FileInfo projectFile);
    }
}
