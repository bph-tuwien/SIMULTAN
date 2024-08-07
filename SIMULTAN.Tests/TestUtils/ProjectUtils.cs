﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.Projects;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Utils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.TestUtils
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

            ZipProjectIO.AuthenticateUserAfterLoading(project, projectDataManager, servicesProvider);
            ZipProjectIO.OpenAfterAuthentication(project, projectDataManager);

            Assert.IsFalse(projectDataManager.ValueManager.HasChanges);

            return (project, projectDataManager, servicesProvider);
        }

        public static (GeometryModel geometryModel, ResourceEntry resource)
            LoadGeometry(string resourceName, ExtendedProjectData dataManager, IServicesProvider serviceProvider)
        {
            var resource = (ResourceFileEntry)FindResource(dataManager.AssetManager.Resources, resourceName);

            var errors = new List<SimGeoIOError>();
            var model = SimGeoIO.Load(resource, dataManager, errors);
            dataManager.GeometryModels.AddGeometryModel(model);

            return (model, resource);
        }

        private static ResourceEntry FindResource(IEnumerable<ResourceEntry> entries, string resourceName)
        {
            foreach (var res in entries)
            {
                if (res.Name == resourceName)
                    return res;

                if (res is ResourceDirectoryEntry dir)
                {
                    var childResult = FindResource(dir.Children, resourceName);
                    if (childResult != null)
                        return childResult;
                }
            }

            return null;
        }

        public static void CleanupTestData(ref HierarchicalProject project, ref ExtendedProjectData dataManager)
        {
            if (project != null)
            {
                if (project.IsOpened)
                    ZipProjectIO.Close(project, true);
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

        public static Dictionary<string, SimBaseParameter> GetParameters(this ExtendedProjectData manager, string component)
        {
            var comp = manager.Components.First(x => x.Name == component);
            Dictionary<string, SimBaseParameter> result = new Dictionary<string, SimBaseParameter>();

            foreach (var param in comp.Parameters)
                result.Add(param.NameTaxonomyEntry.Text, param);

            return result;
        }



        public static Dictionary<string, T> GetParameters<T>(this ExtendedProjectData manager, string component) where T : SimBaseParameter
        {
            var comp = manager.Components.First(x => x.Name == component);
            Dictionary<string, T> result = new Dictionary<string, T>();

            foreach (var param in comp.Parameters)
                if (param is T casted)
                {
                    result.Add(param.NameTaxonomyEntry.Text, casted);
                }


            return result;
        }
    }
}
