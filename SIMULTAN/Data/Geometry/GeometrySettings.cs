using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores application wide settings
    /// </summary>
    public class GeometrySettings
    {
        /// <summary>
        /// Returns the singleton instance
        /// </summary>
        public static GeometrySettings Instance
        {
            get
            {
                return instance;
            }
        }
        private static GeometrySettings instance;

        /// <summary>
        /// Initializes the singleton and other static resources
        /// </summary>
        static GeometrySettings()
        {
            instance = new GeometrySettings();
        }

        /// <summary>
        /// Gets or sets the general calculation tolerance
        /// </summary>
        public double Tolerance { get { return tolerance; } set { tolerance = value; toleranceSquared = value * value; } }
        private double tolerance = 0.01;
        private double toleranceSquared = 0.01 * 0.01;

        /// <summary>
        /// Returns the squared calculation tolerance. This value is derived from <see cref="Tolerance"/>
        /// </summary>
        public double ToleranceSquared { get { return toleranceSquared; } }

        /// <summary>
        /// Specifies whether offset surfaces should be calculated
        /// </summary>
        public bool CalculateOffsetSurfaces
        {
            get
            {
                return calculateOffsetSurfaces;
            }
            set
            {
                calculateOffsetSurfaces = value;
            }
        }
        private bool calculateOffsetSurfaces;

        /// <summary>
        /// Stores the delay for offset surface full mode recalculations during fast mode.
        /// In milliseconds
        /// </summary>
        public int OffsetSurfaceRecalcDelay { get; set; }

        /// <summary>
        /// Initializes a new instance of the AppSettings class
        /// </summary>
        public GeometrySettings()
        {
            OffsetSurfaceRecalcDelay = 1000;
            calculateOffsetSurfaces = true;
            Tolerance = 0.001;
        }
    }
}
