using SIMULTAN.Data.Assets;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Event handler delegate for the SitePlannerProjectOpened event.
    /// </summary>
    /// <param name="sender">the sending object</param>
    /// <param name="project">The project</param>
    public delegate void SitePlannerProjectOpenedEventHandler(object sender, SitePlannerProject project);

    /// <summary>
    /// Event handler delegate for the SitePlannerProjectClosed event.
    /// </summary>
    /// <param name="sender">the sending object</param>
    /// <param name="project">The project</param>
    public delegate void SitePlannerProjectClosedEventHandler(object sender, SitePlannerProject project);

    /// <summary>
    /// Manager for SitePlanner and GeoReference data.
    /// </summary>
    public class SitePlannerManager : ILocated
    {
        /// <summary>
        /// The project data this instance belongs to
        /// </summary>
        public ProjectData ProjectData { get; }


        /// <inheritdoc/>
        public IReferenceLocation CalledFromLocation
        {
            get; private set;
        }

        /// <summary>
        /// List of all GeoMaps in the project
        /// </summary>
        public ObservableCollection<GeoMap> GeoMaps { get; private set; }

        /// <summary>
        /// List of all SitePlannerProjects in the project
        /// </summary>
        public SitePlannerProjectsCollection SitePlannerProjects { get; }

        /// <summary>
        /// Initializes a new instance of the SitePlannerManager class
        /// </summary>
        public SitePlannerManager(ProjectData projectData)
        {
            this.ProjectData = projectData;

            GeoMaps = new ObservableCollection<GeoMap>();
            SitePlannerProjects = new SitePlannerProjectsCollection(this);
        }

        /// <summary>
        /// Generates a new id for buildings which is unique across this manager (i.e. the currently opened project)
        /// </summary>
        /// <returns>ID</returns>
        public ulong GenerateUniqueBuildingID()
        {
            if (SitePlannerProjects.Count == 0)
                return 0;

            var allBuildings = SitePlannerProjects.SelectMany(x => x.Buildings).ToList();
            if (allBuildings.Count == 0)
                return 0;

            return allBuildings.Max(x => x.ID) + 1;
        }

        /// <summary>
        /// Searches for a GeoMap with the given file path
        /// </summary>
        /// <param name="file">file path of gmdxf</param>
        /// <returns>GeoMap if found or null otherwise</returns>
        public GeoMap GetGeoMapByFile(FileInfo file)
        {
            if (file == null) return null;
            return GeoMaps.Where(x => x.GeoMapFile.Name.Equals(file.Name)).FirstOrDefault();
        }

        /// <summary>
        /// Searches for a SitePlannerProject with the given file path
        /// </summary>
        /// <param name="file">file path of spdxf</param>
        /// <returns>SitePlannerProject if found or null otherwise</returns>
        public SitePlannerProject GetSitePlannerProjectByFile(FileInfo file)
        {
            if (file == null) return null;
            return SitePlannerProjects.Where(x => x.SitePlannerFile.Name.Equals(file.Name)).FirstOrDefault();
        }

        /// <summary>
        /// Clears all information from the manager
        /// </summary>
        public void ClearRecord()
        {
            foreach (var project in SitePlannerProjects)
                ProjectData.ComponentGeometryExchange.RemoveSiteplannerProject(project);

            this.GeoMaps.Clear();
            this.SitePlannerProjects.Clear();
        }

        /// <inheritdoc/>
        public void SetCallingLocation(IReferenceLocation caller)
        {
            CalledFromLocation = caller;
        }
    }
}
