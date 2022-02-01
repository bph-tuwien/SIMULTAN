using System.Diagnostics;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores a oriented association of a Face with a Volume
    /// </summary>
    [DebuggerDisplay("PFace ID={Face.Id}")]
    public class PFace
    {
        /// <summary>
        /// Returns the Face
        /// </summary>
        public Face Face { get; private set; }
        /// <summary>
        /// Returns the Volume
        /// </summary>
        public Volume Volume { get; private set; }

        /// <summary>
        /// Returns the orientation relative to the Face
        /// </summary>
        public GeometricOrientation Orientation { get; set; }

        /// <summary>
        /// Initializes a new instance of the PFace class
        /// </summary>
        /// <param name="face">The Face</param>
        /// <param name="volume">The Volume</param>
        /// <param name="orientation">The orientation relative to the Face (Forward means same Normal)</param>
        public PFace(Face face, Volume volume, GeometricOrientation orientation)
        {
            this.Face = face;
            this.Volume = volume;
            Orientation = orientation;
        }

        /// <inheritdoc />
        public void MakeConsistent()
        {
            if (!this.Face.PFaces.Contains(this))
                this.Face.PFaces.Add(this);
        }
    }
}
