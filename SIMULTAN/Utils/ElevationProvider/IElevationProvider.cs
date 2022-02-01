using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.ElevationProvider
{
    /// <summary>
    /// Exception thrown when the elevation of a point could not be found.
    /// </summary>
    [Serializable()]
    public class ElevationNotFoundException : Exception
    {

        /// <summary>
        /// Creates an instance of this exception.
        /// </summary>
        public ElevationNotFoundException() : base() { }
        /// <summary>
        /// Creates an instance of this exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ElevationNotFoundException(string message) : base(message) { }
        /// <summary>
        /// Creates an instance of this exception.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="inner">The inner exception.</param>
        public ElevationNotFoundException(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        /// <inheritdoc />
        protected ElevationNotFoundException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Interface defining a provider for elevation data.
    /// </summary>
    public interface IElevationProvider
    {
        /// <summary>
        /// The maximum supported zoom level of this elevation provider.
        /// </summary>
        int MaxZoomLevel { get; }

        /// <summary>
        /// Fetches the elevation of the given longitude and latitude in WSG84 format.
        /// Throws an ElevationNotFoundException when the elevation could not be determined.
        /// </summary>
        /// <param name="longitude">Longitude of the point in WSG84 format.</param>
        /// <param name="latitude">Latitude of the point in WSG84 format.</param>
        /// <param name="zoomLevel">The zoom level to retrieve the data for. 0 = lowest resolution. Will only change result if implemented.</param>
        /// <returns>Height in reference to the implementation. Reference 0 point can vary between implementations.</returns>
        double GetElevationAtPoint(double longitude, double latitude, int zoomLevel = 0);

        /// <summary>
        /// If supported, returns the zoom level to use for a certain grid cell size. Unit of cell size depends on implementation.
        /// </summary>
        /// <param name="gridCellSize">The grid cell size.</param>
        /// <returns>The zoom level to use for a certain grid cell size.</returns>
        int GetZoomLevelForGridCellSize(double gridCellSize);

        /// <summary>
        /// Return the localized display name of this Provider implementation.
        /// </summary>
        /// <returns>The localized display name of this Provider implementation.</returns>
        string GetDisplayName();
    }
}
