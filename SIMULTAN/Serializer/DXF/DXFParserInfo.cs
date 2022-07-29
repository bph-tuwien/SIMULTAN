using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Contains additional data for the DXF parser and helps with Id translations
    /// </summary>
    public class DXFParserInfo
    {
        #region Id Translation

        private static readonly Dictionary<Type, (long start, long maxCount, ulong version)> idTranslationInfo = 
            new Dictionary<Type, (long start, long maxCount, ulong version)>
        {
            { typeof(SimComponent),     (0, 1073741824, 8)  }, //2^30
            { typeof(SimMultiValue),    (1073741824 + 1000000, 1073741824 + 2000000, 5) }, //1M
            { typeof(SimCalculation),   (1073741824 + 2000000, 1073741824 + 3000000, 4) }, //1M
            { typeof(SimParameter),     (1073741824 + 3000000, 1073741824 + 4000000, 4) }, //1M
            { typeof(SimComponentInstance), (1073741824 + 4000000, 1073741824L + 1073741824L, 8) }, //2^30
        };
        private static ulong IdTranslationMaxId { get; } = 1073741824L + 1073741824L;

        private Dictionary<Type, long> idTranslationCount = new Dictionary<Type, long>()
        {
            { typeof(SimComponent), 1 }, //1 because 0 is the empty id
            { typeof(SimMultiValue), 0 },
            { typeof(SimCalculation), 0 },
            { typeof(SimParameter), 0 },
            { typeof(SimComponentInstance), 0 },
        };

        private Dictionary<(Type, long), long> idTranslation = new Dictionary<(Type, long), long>();
        private Dictionary<(Type, long), bool> isOwningTranslation = new Dictionary<(Type, long), bool>();

        /// <summary>
        /// Translates an id from the old system (version 0-8) to the new id system
        /// </summary>
        /// <param name="type">The type of object this Id belongs to</param>
        /// <param name="id">The old id that should be translated</param>
        /// <returns>The new Id</returns>
        internal long TranslateId(Type type, long id)
        {
            var tdata = idTranslationInfo[type];
            if (this.FileVersion <= tdata.version)
            {
                if (id == -1)
                    return 0;

                if (!idTranslation.TryGetValue((type, id), out var translatedId))
                {
                    var count = idTranslationCount[type];

                    //Not translated yet, translate and store
                    if (count > tdata.maxCount)
                        throw new Exception(String.Format("To many items of Type {0}", type));

                    translatedId = tdata.start + count;
                    count++;
                    idTranslationCount[type] = count;
                    idTranslation.Add((type, id), translatedId);
                    isOwningTranslation[(type, id)] = false;
                }

                return translatedId;
            }

            return id;
        }
        /// <summary>
        /// Translates an id from the old system (version 0-8) to the new id system
        /// </summary>
        /// <param name="type">The type of object this Id belongs to</param>
        /// <param name="id">The old id that should be translated</param>
        /// <returns>The new Id</returns>
        internal SimId TranslateId(Type type, SimId id)
        {
            if (id == SimId.Empty)
                return SimId.Empty;

            var tdata = idTranslationInfo[type];
            if (this.FileVersion <= tdata.version)
            {
                if (id.LocalId == -1)
                    return SimId.Empty;

                if (!idTranslation.TryGetValue((type, id.LocalId), out var translatedId))
                {
                    var count = idTranslationCount[type];

                    //Not translated yet, translate and store
                    if (count > tdata.maxCount)
                        throw new Exception(String.Format("To many items of Type {0}", type));

                    translatedId = tdata.start + count;
                    count++;
                    idTranslationCount[type] = count;
                    idTranslation.Add((type, id.LocalId), translatedId);
                    isOwningTranslation[(type, id.LocalId)] = false;
                }

                return new SimId(id.GlobalId, translatedId);
            }

            return id;
        }
        /// <summary>
        /// Generates a new Id no matter if a translation exists and registers it if it doesn't exist
        /// </summary>
        /// <param name="type">The type of object this Id belongs to</param>
        /// <param name="id">The old id that should be translated</param>
        /// <param name="exists">Returns whether the id has already existed or not.</param>
        /// <returns>The new Id</returns>
        internal long GenerateNewId(Type type, long id, out bool exists)
        {
            exists = false;

            var tdata = idTranslationInfo[type];
            if (this.FileVersion <= tdata.version)
            {
                if (id == -1)
                    return 0;

                //Ids should be unique, but some old projects have errors for Parameter ids in them
                exists = idTranslation.TryGetValue((type, id), out var translatedId);
                if (exists && isOwningTranslation.TryGetValue((type, id), out var isOwning))
                {
                    if (!isOwning) //Translation exists, but only as a reference
                    {
                        exists = false;
                        isOwningTranslation[(type, id)] = true;
                        return translatedId;
                    }
                }

                var count = idTranslationCount[type];

                //Not translated yet (or duplicate), translate and store
                if (count > tdata.maxCount)
                    throw new Exception(String.Format("To many items of Type {0}", type));

                translatedId = tdata.start + count;
                count++;
                idTranslationCount[type] = count;

                if (!exists)
                {
                    idTranslation.Add((type, id), translatedId);
                    isOwningTranslation[(type, id)] = true;
                }

                return translatedId;
            }

            return id;
        }
        /// <summary>
        /// Checks if a translation for an Id exists
        /// </summary>
        /// <param name="type">The type of object this Id belongs to</param>
        /// <param name="id">The old id that should be translated</param>
        /// <returns>The new Id</returns>
        internal bool TranslationExists(Type type, long id)
        {
            return idTranslation.ContainsKey((type, id));
        }

        #endregion

        #region Logging

        private StreamWriter logFileWriter;

        /// <summary>
        /// Writes a parser log message to a log file. The logfile is created when the first
        /// message is logged and is located in the current working directory
        /// </summary>
        /// <param name="message">The message to lgo</param>
        public void Log(string message)
        {
            if (logFileWriter == null)
            {

                //Try to find valid filename
                string fileNameBase = string.Format(@".\ImportLog_{0:dd_MM_yyyy-HH_mm_ss}", DateTime.Now);
                string fileName = fileNameBase + ".txt";

                if (ProjectData.ImportLogFile != null)
                {
                    fileName = ProjectData.ImportLogFile.FullName;
                    fileNameBase = Path.Combine(
                        ProjectData.ImportLogFile.DirectoryName,
                        Path.GetFileNameWithoutExtension(ProjectData.ImportLogFile.Name)
                        );
                }

                int i = 1;
                while (File.Exists(fileName))
                {
                    fileName = string.Format("{0} ({1}).txt", fileNameBase, i);
                    i++;
                }

                logFileWriter = new StreamWriter(fileName, true);
            }

            if (logFileWriter != null)
            {
                Console.WriteLine("Import log: {0}", message);
                logFileWriter.WriteLine(message);
            }
        }

        public void FinishLog()
        {
            if (logFileWriter != null)
            {
                logFileWriter.Close();
                logFileWriter.Dispose();
                logFileWriter = null;
            }
        }

        #endregion

        /// <summary>
        /// The DXF file version. Has to be set when parsing the version section.
        /// </summary>
        public ulong FileVersion { get; set; } = 0;

        /// <summary>
        /// The current projects global Id. This id is used when objects are loaded and to restore in-project references 
        /// (where the stored Guid is Empty)
        /// </summary>
        public Guid GlobalId { get; }
        /// <summary>
        /// The project data
        /// </summary>
        public ExtendedProjectData ProjectData { get; }

        /// <summary>
        /// The file that is currently being processed.
        /// Set this before reading the file.
        /// Is null if not loading from a file.
        /// </summary>
        public FileInfo CurrentFile { get; set; } = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFParserInfo"/> class
        /// </summary>
        /// <param name="globalId">The current projects global id</param>
        /// <param name="projectData">The project data</param>
        public DXFParserInfo(Guid globalId, ExtendedProjectData projectData)
        {
            this.GlobalId = globalId;
            this.ProjectData = projectData;
        }

        /// <summary>
        /// Parses the result of a version section. Expects an entry containing the version number with
        /// Code <see cref="ParamStructCommonSaveCode.COORDS_X"/>
        /// </summary>
        /// <param name="data">The DXF reader result set</param>
        /// <param name="info">Parser info. The version gets stored here</param>
        /// <returns>The parser info with the version number stored in it</returns>
        internal static DXFParserInfo Parse(DXFParserResultSet data, DXFParserInfo info)
        {
            info.FileVersion = data.Get<ulong>(ParamStructCommonSaveCode.COORDS_X, 0);
            return info;
        }
    }
}
