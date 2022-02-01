using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// Stores the ids of a geometric element.
    /// Each entry consists of the id of the resource in which the geometry exists (FileId) 
    /// and an Id inside this file (GeometryId).
    /// </summary>
    public struct GeometricReference : IEquatable<GeometricReference>
    {
        /// <summary>
        /// The Id of the geometry model file. This is the resource key of the resource.
        /// </summary>
        public int FileId { get; private set; }
        /// <summary>
        /// Id of the geometry in the geometry model file
        /// </summary>
        public ulong GeometryId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the GeometricReference class
        /// </summary>
        /// <param name="fileId">Id of the file</param>
        /// <param name="geometryId">Id of the geometry inside the file</param>
        public GeometricReference(int fileId, ulong geometryId)
        {
            this.FileId = fileId;
            this.GeometryId = geometryId;
        }



        /// <summary>
        /// Returns a GeometricReference which doesn't point to a valid geometry
        /// </summary>
        public static GeometricReference Empty
        {
            get
            {
                return new GeometricReference(-1, ulong.MaxValue);
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is GeometricReference reference &&
                   FileId == reference.FileId &&
                   GeometryId == reference.GeometryId;
        }
        /// <inheritdoc />
        public bool Equals(GeometricReference other)
        {
            return FileId == other.FileId && GeometryId == other.GeometryId;
        }
        /// <inheritdoc />
        public static bool operator ==(GeometricReference lhs, GeometricReference rhs)
        {
            return lhs.Equals(rhs);
        }
        /// <inheritdoc />
        public static bool operator !=(GeometricReference lhs, GeometricReference rhs)
        {
            return !(lhs == rhs);
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hashCode = 2112486946;
            hashCode = hashCode * -1521134295 + FileId.GetHashCode();
            hashCode = hashCode * -1521134295 + GeometryId.GetHashCode();
            return hashCode;
        }
    }
}