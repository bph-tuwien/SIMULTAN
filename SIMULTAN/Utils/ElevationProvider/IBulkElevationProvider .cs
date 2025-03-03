using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.ElevationProvider
{

    /// <summary>
    /// Interface defining a provider for elevation data.
    /// </summary>
    public interface IBulkElevationProvider : IElevationProvider
    {
        /// <summary>
        /// Fetches the elevations of the given coordinates in WSG84 format.
        /// Throws an ElevationNotFoundException when one the elevation could not be determined or an error occurred.
        /// </summary>
        /// <param name="coordinates">The coordinates to retrieve the elevation for. In (latitude, longitude) pairs.</param>
        /// <param name="zoomLevel">The zoom level to retrieve the data for. 0 = lowest resolution. Will only change result if implemented.</param>
        /// <returns>Height in reference to the implementation. Reference 0 point can vary between implementations. Ordering matches the input coordinate ordering.</returns>
        IList<double> GetElevationAtPoints(IList<(double lat, double lng)> coordinates, int zoomLevel = 0);

        /// <summary>
        /// Fetches the data of a whole tile. Data is in a row by row format.
        /// </summary>
        /// <param name="pointInTile">A point somewhere inside of the tile you want to fetch in Webmercator coordinates (easting, northing)/[long, lat].</param>
        /// <param name="zoomLevel">The zoom level at which the tile should be fetched.</param>
        /// <returns>The data of the tile in a row-by-row format, the width and the height.</returns>
        (double[] data, int width, int height) GetTileData((double lng, double lat) pointInTile, int zoomLevel = 0);
    }
}
