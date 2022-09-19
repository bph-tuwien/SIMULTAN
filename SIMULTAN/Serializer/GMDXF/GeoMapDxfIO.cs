using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;

namespace SIMULTAN.Serializer.GMDXF
{
    internal static class GeoMapDxfIO
    {
        #region Syntax

        private static DXFEntityParserElement<GeoMap> geoMapEntityElement =
            new DXFEntityParserElement<GeoMap>(ParamStructTypes.GEOMAP, ParseGeoMap,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<ResourceReference>(GeoMapSaveCode.MAP_PATH){MaxVersion = 11},
                    new DXFSingleEntryParserElement<Guid>(GeoMapSaveCode.MAP_PROJECT_ID){MinVersion = 12},
                    new DXFSingleEntryParserElement<long>(GeoMapSaveCode.MAP_RESOURCE_ID){MinVersion = 12},
                    new DXFStructArrayEntryParserElement<ImageGeoReference>(GeoMapSaveCode.GEOREFS, ParseGeoReference,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<double>(GeoMapSaveCode.IMAGEPOS_X),
                            new DXFSingleEntryParserElement<double>(GeoMapSaveCode.IMAGEPOS_Y),
                            new DXFSingleEntryParserElement<double>(GeoMapSaveCode.LONGITUDE),
                            new DXFSingleEntryParserElement<double>(GeoMapSaveCode.LATITUDE),
                            new DXFSingleEntryParserElement<double>(GeoMapSaveCode.HEIGHT)
                        })
                });

        private static DXFSectionParserElement<GeoMap> geoMapSection =
            new DXFSectionParserElement<GeoMap>(ParamStructTypes.GEOMAP_SECTION, new DXFEntityParserElement<GeoMap>[]
            {
                geoMapEntityElement
            });

        #endregion

        internal static void Read(FileInfo file, DXFParserInfo info)
        {
            info.CurrentFile = file;
            using(var fs = file.OpenRead())
            {
                if (fs.Length == 0)
                    return;
                using(var reader = new DXFStreamReader(fs))
                {
                    Read(reader, info);
                }
            }
        }

        internal static void Read(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            if(parserInfo.CurrentFile == null)
            {
                throw new ArgumentException("DXFParserInfo has no CurrentFile set but is needed for loading GeoMaps.");
            }

            //Version section
            if(CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }

            //Data section
            var geoMaps = geoMapSection.Parse(reader, parserInfo);

            foreach (var map in geoMaps)
            {
                if (map != null)
                    parserInfo.ProjectData.SitePlannerManager.GeoMaps.Add(map);
            }

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.FinishLog();
        }

        internal static void Write(FileInfo file, GeoMap geoMap, ProjectData projectData)
        {
            using (var fs = file.Open(FileMode.Create, FileAccess.Write))
            {
                using(var writer = new DXFStreamWriter(fs))
                {
                    Write(writer, geoMap, projectData);
                }
            }
        }

        internal static void Write(DXFStreamWriter writer, GeoMap geoMap, ProjectData projectData)
        {
            // File header
            writer.WriteVersionSection();

            // Data
            writer.StartSection(ParamStructTypes.GEOMAP_SECTION);

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.GEOMAP);

            if (geoMap.MapImageRes != null)
            {
                writer.WriteGlobalId(GeoMapSaveCode.MAP_PROJECT_ID, geoMap.MapImageRes.ProjectId, projectData.SitePlannerManager.CalledFromLocation.GlobalID);
                writer.Write<long>(GeoMapSaveCode.MAP_RESOURCE_ID, geoMap.MapImageRes.ResourceIndex);
            }
            else
            {
                writer.Write(GeoMapSaveCode.MAP_PROJECT_ID, Guid.Empty);
                writer.Write<long>(GeoMapSaveCode.MAP_RESOURCE_ID, -1);
            }

            writer.WriteArray(GeoMapSaveCode.GEOREFS, geoMap.GeoReferences,
                (gref, w) =>
                {
                    w.Write(GeoMapSaveCode.IMAGEPOS_X, gref.ImagePosition.X); 
                    w.Write(GeoMapSaveCode.IMAGEPOS_Y, gref.ImagePosition.Y); 
                    w.Write(GeoMapSaveCode.LONGITUDE, gref.ReferencePoint.X); 
                    w.Write(GeoMapSaveCode.LATITUDE, gref.ReferencePoint.Y); 
                    w.Write(GeoMapSaveCode.HEIGHT, gref.ReferencePoint.Z); 
                });


            writer.EndSection();

            // EOF
            writer.WriteEOF();
        }

        /// <summary>
        /// Creates an empty GeoMap as DXF (.gmdxf) at the specified file path.
        /// </summary>
        /// <param name="file">Path to save the empty GeoMap to</param>
        /// <param name="projectData">The project data to create the GeoMap for</param>
        public static void CreateEmptyGeoMap(FileInfo file, ProjectData projectData)
        {
            Write(file, new GeoMap(null), projectData);
        }

        #region Parsers

        private static GeoMap ParseGeoMap(DXFParserResultSet data, DXFParserInfo info)
        {
            ResourceReference mapRes = null;
            if (info.FileVersion < 12)
            {
                mapRes = data.Get<ResourceReference>(GeoMapSaveCode.MAP_PATH, null);
            }
            else
            {
                Guid projectId = data.Get<Guid>(GeoMapSaveCode.MAP_PROJECT_ID, new Guid());
                int resourceId = (int)data.Get<long>(GeoMapSaveCode.MAP_RESOURCE_ID, -1L);
                // resourceId is negative if there is none
                if (resourceId >= 0)
                {
                    if (projectId == Guid.Empty)
                    {
                        projectId = info.ProjectData.Project.GlobalID;
                    }
                    mapRes = new ResourceReference(projectId, resourceId, info.ProjectData.AssetManager);
                }
            }

            var geoRefs = data.Get<ImageGeoReference[]>(GeoMapSaveCode.GEOREFS, null);

            // Assume that assets are already loaded
            var resource = info.ProjectData.AssetManager.GetResource(info.CurrentFile);
            if(resource == null)
            {
                throw new Exception("Could not find GeoMap resource file in Assets. Are the Assets already loaded?");
            }

            var geoMap = new GeoMap(resource);
            geoMap.MapImageRes = mapRes;
            foreach (var gref in geoRefs)
                geoMap.GeoReferences.Add(gref);

            return geoMap;
        }

        private static ImageGeoReference ParseGeoReference(DXFParserResultSet arg, DXFParserInfo info)
        {
            double posX = arg.Get<double>(GeoMapSaveCode.IMAGEPOS_X, 0);
            double posY = arg.Get<double>(GeoMapSaveCode.IMAGEPOS_Y, 0);
            double longitude = arg.Get<double>(GeoMapSaveCode.LONGITUDE, 0);
            double latitude = arg.Get<double>(GeoMapSaveCode.LATITUDE, 0);
            double height = arg.Get<double>(GeoMapSaveCode.HEIGHT, 0);

            return new ImageGeoReference(new System.Windows.Point(posX, posY), 
                new System.Windows.Media.Media3D.Point3D(longitude, latitude, height));
        }

        #endregion

    }
}
