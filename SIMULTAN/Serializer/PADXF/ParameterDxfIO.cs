using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.PADXF
{
    /// <summary>
    /// Provides methods for serializing parameters into a parameter library file (*.PADXF)
    /// </summary>
    public class ParameterDxfIO
    {
        #region Syntax Section

        internal static DXFSectionParserElement<SimParameter> ParameterSectionEntityElement =
            new DXFSectionParserElement<SimParameter>(ParamStructTypes.ENTITY_SECTION,
                new DXFEntityParserElementBase<SimParameter>[]
                {
                    ComponentDxfIOComponents.ParameterEntityElement
                });

        #endregion

        /// <summary>
        /// Writes the project's parameter library into a parameter library file
        /// </summary>
        /// <param name="file">The target file</param>
        /// <param name="projectData">The project data from which the parameter library should be exported</param>
        public static void Write(FileInfo file, ProjectData projectData)
        {
            Write(file, projectData, projectData.ParameterLibraryManager.ParameterRecord);
        }

        /// <summary>
        /// Writes the a list of parameters to a parameter library file
        /// </summary>
        /// <param name="file">The target file</param>
        /// <param name="projectData">The project data to which the parameters belong</param>
        /// <param name="parameters">The parameters to export</param>
        public static void Write(FileInfo file, ProjectData projectData, IEnumerable<SimParameter> parameters)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(fs))
                {
                    Write(writer, projectData, parameters);
                }
            }
        }

        internal static void Write(DXFStreamWriter writer, ProjectData projectData, IEnumerable<SimParameter> parameters)
        {
            //File header
            writer.WriteVersionSection();

            //Data
            writer.StartSection(ParamStructTypes.ENTITY_SECTION);

            foreach (var parameter in parameters)
            {
                ComponentDxfIOComponents.WriteParameter(parameter, writer);
            }

            writer.EndSection();

            //EOF
            writer.WriteEOF();
        }

        /// <summary>
        /// Reads a parameter library file and stores the result in a project data
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <param name="parserInfo">Additional parser info. The results are stored in the project data</param>
        public static void Read(FileInfo file, DXFParserInfo parserInfo)
        {
            using (FileStream stream = file.OpenRead())
            {
                if (stream.Length == 0)
                    return;

                using (DXFStreamReader reader = new DXFStreamReader(stream))
                {
                    Read(reader, parserInfo);
                }
            }
        }

        internal static void Read(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            //Version section
            if(CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }
            else
            {
                parserInfo.FileVersion = ComponentDxfIO.LastParsedFileVersion;
            }

            //Data section
            var parameters = ParameterSectionEntityElement.Parse(reader, parserInfo);

            foreach (var parameter in parameters.Where(x => x != null))
            {
                parserInfo.ProjectData.ParameterLibraryManager.ParameterRecord.Add(parameter);
                parameter.RestoreReferences(parserInfo.ProjectData.IdGenerator);
            }

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.FinishLog();
        }
    }
}
