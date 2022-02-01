using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Provides IO methods for <see cref="GeoMap"/>
    /// </summary>
    public class SitePlannerIO
    {
        /// <summary>
        /// Saves a given GeoMap as DXF (.gmdxf) to the specified file path.
        /// </summary>
        /// <param name="file">Path to save the GeoMap to</param>
        /// <param name="map">GeoMap to save</param>
        public static void SaveGeoMap(FileInfo file, GeoMap map)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    //Version
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SECTION_START);
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
                    sw.WriteLine(ParamStructTypes.VERSION_SECTION);

                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                    sw.WriteLine(ParamStructTypes.FILE_VERSION);                            // FILE_VERSION

                    sw.WriteLine(((int)ParamStructCommonSaveCode.COORDS_X).ToString());       // 10
                    sw.WriteLine(DXFDecoder.CurrentFileFormatVersion.ToString());

                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SECTION_END);

                    //Content
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SECTION_START);
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
                    sw.WriteLine(ParamStructTypes.GEOMAP_SECTION);

                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.GEOMAP);

                    sw.WriteLine(((int)GeoMapSaveCode.MAP_PATH).ToString());
                    sw.WriteLine(map.MapImageRes != null ? ResourceReference.ToDXF(map.MapImageRes) : string.Empty);

                    sw.WriteLine(((int)GeoMapSaveCode.GEOREFS).ToString());
                    sw.WriteLine(map.GeoReferences.Count.ToString());

                    foreach (var gr in map.GeoReferences)
                    {
                        sw.WriteLine(((int)GeoMapSaveCode.IMAGEPOS_X).ToString());
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:F10}", gr.ImagePosition.X));

                        sw.WriteLine(((int)GeoMapSaveCode.IMAGEPOS_Y).ToString());
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:F10}", gr.ImagePosition.Y));

                        sw.WriteLine(((int)GeoMapSaveCode.LONGITUDE).ToString());
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:F10}", gr.ReferencePoint.X));

                        sw.WriteLine(((int)GeoMapSaveCode.LATITUDE).ToString());
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:F10}", gr.ReferencePoint.Y));

                        sw.WriteLine(((int)GeoMapSaveCode.HEIGHT).ToString());
                        sw.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0:F10}", gr.ReferencePoint.Z));
                    }

                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SECTION_END);

                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.EOF);
                }
            }
        }

        /// <summary>
        /// Creates an empty GeoMap as DXF (.gmdxf) at the specified file path.
        /// </summary>
        /// <param name="file">Path to save the empty GeoMap to</param>
        public static void CreateEmptyGeoMap(FileInfo file)
        {
            SaveGeoMap(file, new GeoMap(null));
        }

        /// <summary>
        /// Saves a given SitePlannerProject as DXF (.spdxf) to the specified file path.
        /// </summary>
        /// <param name="file">Path to save the SitePlannerProject to</param>
        /// <param name="project">SitePlannerProject to save</param>
        public static void SaveSitePlannerProject(FileInfo file, SitePlannerProject project)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    //Version
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SECTION_START);
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
                    sw.WriteLine(ParamStructTypes.VERSION_SECTION);

                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                    sw.WriteLine(ParamStructTypes.FILE_VERSION);                            // FILE_VERSION

                    sw.WriteLine(((int)ParamStructCommonSaveCode.COORDS_X).ToString());       // 10
                    sw.WriteLine(DXFDecoder.CurrentFileFormatVersion.ToString());

                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SECTION_END);

                    //Content

                    // Start
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SECTION_START);
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
                    sw.WriteLine(ParamStructTypes.SITEPLANNER_SECTION);

                    // General
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SITEPLANNER);

                    // GeoMaps
                    sw.WriteLine(((int)SitePlannerSaveCode.GEOMAPS).ToString());
                    sw.WriteLine(project.Maps.Count.ToString());

                    for (int i = 0; i < project.Maps.Count; i++)
                    {
                        sw.WriteLine(((int)SitePlannerSaveCode.GEOMAP_PATH).ToString());
                        sw.WriteLine(ResourceReference.ToDXF(project.Maps[i].GeoMapRes));

                        sw.WriteLine(((int)SitePlannerSaveCode.ELEVATION_PROVIDER_TYPE).ToString());
                        var elevationProvider = project.Maps[i].ElevationProvider;
                        sw.WriteLine(elevationProvider == null ? "" : elevationProvider.GetType().Name);

                        sw.WriteLine(((int)SitePlannerSaveCode.GRID_CELL_SIZE).ToString());
                        sw.WriteLine(project.Maps[i].GridCellSize.ToString());
                    }

                    // Buildings
                    sw.WriteLine(((int)SitePlannerSaveCode.BUILDINGS).ToString());
                    sw.WriteLine(project.Buildings.Count.ToString());

                    for (int i = 0; i < project.Buildings.Count; i++)
                    {
                        sw.WriteLine(((int)SitePlannerSaveCode.BUILDING_INDEX).ToString());
                        sw.WriteLine(i.ToString());

                        sw.WriteLine(((int)SitePlannerSaveCode.BUILDING_ID).ToString());
                        sw.WriteLine(project.Buildings[i].ID.ToString());

                        sw.WriteLine(((int)SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_PATH).ToString());
                        sw.WriteLine(ResourceReference.ToDXF(project.Buildings[i].GeometryModelRes));

                        sw.WriteLine(((int)SitePlannerSaveCode.BUILDING_CUSTOM_COLOR).ToString());
                        var color = project.Buildings[i].CustomColor;
                        sw.WriteLine(string.Format("{0} {1} {2}", color.R, color.G, color.B));
                    }

                    // ValueMapping
                    sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS).ToString());
                    sw.WriteLine(project.ValueMap.ParametersAssociations.Count.ToString());

                    for (int i = 0; i < project.ValueMap.ParametersAssociations.Count; i++)
                    {
                        var vma = project.ValueMap.ParametersAssociations[i];

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_INDEX).ToString());
                        sw.WriteLine(i.ToString());

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_NAME).ToString());
                        sw.WriteLine(vma.Name);

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_LOCATION).ToString());
                        sw.WriteLine(vma.Parameters.ValueTable.Id.GlobalId.ToString());

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_KEY).ToString());
                        sw.WriteLine(vma.Parameters.ValueTable.Id.LocalId.ToString());

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_INDEX_USAGE).ToString());
                        sw.WriteLine(vma.Parameters.ComponentIndexUsage.ToString());

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_TYPE).ToString());
                        sw.WriteLine(vma.Parameters.ValueToColorMap.GetType().ToString());

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_PARAMS).ToString());
                        sw.WriteLine(vma.Parameters.ValueToColorMap.Parameters.Serialize());

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_TYPE).ToString());
                        sw.WriteLine(vma.Parameters.ValuePreFilter.GetType().ToString());

                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_PARAMS).ToString());
                        sw.WriteLine(vma.Parameters.ValuePreFilter.Parameters.Serialize());
                    }

                    if (project.ValueMap.ActiveParametersAssociation != null)
                    {
                        sw.WriteLine(((int)SitePlannerSaveCode.VALUE_MAPPING_ACTIVE_ASSOCIATION_INDEX).ToString());
                        sw.WriteLine(project.ValueMap.ActiveParametersAssociationIndex);
                    }

                    // End
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.SECTION_END);
                    sw.WriteLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                    sw.WriteLine(ParamStructTypes.EOF);
                }
            }
        }

        /// <summary>
        /// Creates an empty SitePlannerProject as DXF (.spdxf) at the specified file path.
        /// </summary>
        /// <param name="file">Path to save the empty SitePlannerProject to</param>
        public static void CreateEmptySitePlannerProject(FileInfo file)
        {
            SaveSitePlannerProject(file, new SitePlannerProject(null));
        }
    }
}
