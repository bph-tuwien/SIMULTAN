using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores the combination of a vertex and a Geo-Reference location
    /// </summary>
    public class GeoReference : INotifyPropertyChanged
    {
        /// <summary>
        /// The vertex
        /// </summary>
        public Vertex Vertex { get; private set; }
        /// <summary>
        /// The geo location, x = long (degrees), y = lat (degrees), z = height (meter) 
        /// </summary>
        public Point3D ReferencePoint
        {
            get { return referencePoint; }
            set
            {
                referencePoint = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReferencePoint)));
            }
        }
        private Point3D referencePoint;

        /// <summary>
        /// Initializes a new instance of the GeoReference class
        /// </summary>
        /// <param name="vertex">The vertex</param>
        /// <param name="reference">The reference location</param>
        public GeoReference(Vertex vertex, Point3D reference)
        {
            this.Vertex = vertex;
            this.ReferencePoint = reference;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
