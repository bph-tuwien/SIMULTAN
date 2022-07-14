using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.Projects;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.UI.Services;
using SIMULTAN.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Tests.Utils
{
    public static class ProjectUtils
    {
        public static (HierarchicalProject project, ExtendedProjectData dataManager, ServicesProvider serviceProvider) LoadTestData(FileInfo projectFile)
        {
            return LoadTestData(projectFile, "admin", "admin");
        }

        public static (HierarchicalProject project, ExtendedProjectData dataManager, ServicesProvider serviceProvider) LoadTestData(FileInfo projectFile,
            string user, string password)
        {
            var projectDataManager = new ExtendedProjectData();

            ServicesProvider servicesProvider = new ServicesProvider();
            servicesProvider.AddService<IAuthenticationService>(new UnitTestAuthentificationService(user, password));

            CompactProject project = ZipProjectIO.Load(projectFile, projectDataManager) as CompactProject;

            ZipProjectIO.AuthenticateUserAfterLoading(project, projectDataManager, Encoding.ASCII.GetBytes("ThWmZq4t6w9z$C&F"), servicesProvider);
            ZipProjectIO.OpenAfterAuthentication(project, projectDataManager);

            Assert.IsFalse(projectDataManager.ValueManager.HasChanges);

            return (project, projectDataManager, servicesProvider);
        }

        public static (GeometryModel geometryModel, ResourceEntry resource)
            LoadGeometry(string resourceName, ExtendedProjectData dataManager, IServicesProvider serviceProvider)
        {
            var resource = (ResourceFileEntry)dataManager.AssetManager.Resources.FirstOrDefault(x => x.Name == resourceName);

            var errors = new List<SimGeoIOError>();
            var model = SimGeoIO.Load(resource, dataManager, errors);
            dataManager.GeometryModels.AddGeometryModel(model);

            return (model, resource);
        }

        public static void CleanupTestData(ref HierarchicalProject project, ref ExtendedProjectData dataManager)
        {
            if (project != null)
            {
                if (project.IsOpened)
                    ZipProjectIO.Close(project, false, true);
                if (project.IsLoaded)
                    ZipProjectIO.Unload(project);

                project = null;
            }

            if (dataManager != null)
            {
                dataManager.Reset();
                dataManager = null;
            }
        }

        public static Dictionary<string, SimParameter> GetParameters(this ExtendedProjectData manager, string component)
        {
            var comp = manager.Components.First(x => x.Name == component);
            Dictionary<string, SimParameter> result = new Dictionary<string, SimParameter>();

            foreach (var param in comp.Parameters)
                result.Add(param.Name, param);

            return result;
        }
    }
}
