using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.Projects;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace _01_ProjectBasics
{
    /// <summary>
    /// This example shows how to open an existing project, authenticate a user and
    /// print the name of all root components
    /// </summary>
    internal class Program
    {
        static void Main(string[] args)
        {
            //----------------------------------------------------------------
            // Setup
            //----------------------------------------------------------------

            //The project file that should be opened
            var projectFile = new FileInfo("Project_01.simultan");

            //A services provider contains service implementations which are used by SIMULTAN to perform
            //task that require UI interactions
            var servicesProvider = new ServicesProvider();
            //Add a service to authenticate a user. See ConsoleAuthenticationService for more details
            servicesProvider.AddService<IAuthenticationService>(new ConsoleAuthenticationService());



            //----------------------------------------------------------------
            // Open Project
            //----------------------------------------------------------------

            //ProjectData contains the data loaded from the project
            var projectData = new ExtendedProjectData();
            //Load the project. Loading reads all public information and also the user information
            var project = ZipProjectIO.Load(projectFile, projectData);

            //Authenticate a user. This method calls the IAuthenticationService
            var isAuthenticated = ZipProjectIO.AuthenticateUserAfterLoading(project, projectData, servicesProvider);
            if (!isAuthenticated)
            {
                Console.WriteLine("Wrong username or password");
                return;
            }

            //Opens the project. Opening loads all data that the user has access to
            ZipProjectIO.OpenAfterAuthentication(project, projectData);



            //----------------------------------------------------------------
            // Print root components
            //----------------------------------------------------------------

            foreach (var comp in projectData.Components)
            {
                Console.WriteLine("{0} [{1}]", comp.Name, comp.CurrentSlot);
            }

            Console.WriteLine();
            Console.WriteLine("Press any Key to close the application");
            Console.Read();

            //----------------------------------------------------------------
            // Close Project
            //----------------------------------------------------------------

            //Disable folder watcher so they don't interfere with project closing
            project.DisableProjectUnpackFolderWatcher();
            //Close the project, undoes the Open operation
            ZipProjectIO.Close(project, false, true);
            //Unload the project, undoes the Load operation
            ZipProjectIO.Unload(project);
            //Free all data from the project
            projectData.Reset();
        }
    }

    /// <summary>
    /// Authenticates a user
    /// </summary>
    internal class ConsoleAuthenticationService : IAuthenticationService
    {
        /// <summary>
        /// Authenticates a user. 
        /// This method has to set <see cref="SimUsersManager.CurrentUser"/> and 
        /// <see cref="SimUsersManager.EncryptionKey"/> in case a valid user has been found
        /// </summary>
        /// <param name="userManager">The userManager</param>
        /// <param name="projectFile">The project File</param>
        /// <returns>Either a valid user or Null when authentication fails</returns>
        public SimUser Authenticate(SimUsersManager userManager, FileInfo projectFile)
        {
            Console.Write("Username: ");
            string name = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();
            var securePassword = new SecureString();
            password.ForEach(x => securePassword.AppendChar(x));

            (var user, var userKey) = userManager.Authenticate(name, securePassword);
            if (user != null)
            {
                userManager.CurrentUser = user;
                userManager.EncryptionKey = userKey;
            }

            return user;
        }
    }
}
