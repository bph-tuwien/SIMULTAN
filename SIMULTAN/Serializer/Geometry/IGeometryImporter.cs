using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Serializer.Geometry
{
    /// <summary>
    /// Gets thrown if an error happens while importing a geometry file.
    /// </summary>
    public class GeometryImporterException : Exception
    {

        /// <summary>
        /// Creates an instance of this Exception with a default message.
        /// </summary>
        public GeometryImporterException() : base()
        {
        }

        /// <summary>
        /// Creates an instance of this Exception with the provided message.
        /// <paramref name="message">The message of the exception</paramref>
        /// </summary>
        public GeometryImporterException(string message) : base(message)
        {

        }

        /// <summary>
        /// Creates an instance of this Exception with the provided message and inner exception.
        /// <paramref name="message">The message of the exception</paramref>
        /// <paramref name="innerException">The inner exception</paramref>
        /// </summary>
        public GeometryImporterException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }

    /// <summary>
    /// Interface for a geometry importer.
    /// </summary>
    public interface IGeometryImporter
    {
        /// <summary>
        /// Loads and parses the model file at the given path and returns its contents.
        /// Throws a FileNotFoundException if the file could not be found.
        /// Throws a GeometryImporterException if an error occurred while loading the file.
        /// </summary>
        /// <param name="path">Path to the model file.</param>
        /// <returns>A GeometryImporterResult containing the parsed model informations.</returns>
        SimMeshGeometryData Import(string path);
    }
}
