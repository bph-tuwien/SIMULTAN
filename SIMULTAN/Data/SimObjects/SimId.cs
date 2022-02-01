using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// Stores a unique id of a <see cref="SimObjectNew"/> instance.
    /// The id is composed of a global Id (<see cref="Guid"/>) and a local Id (long)
    /// 
    /// An id of (<see cref="Guid.Empty"/>, 0) means that the id describes the <see cref="Empty"/> id.
    /// </summary>
    public struct SimId : IEquatable<SimId>
    {
        /// <summary>
        /// Returns the empty id with <see cref="GlobalId"/> equals <see cref="Guid.Empty"/> and <see cref="LocalId"/> being 0.
        /// </summary>
        public static SimId Empty { get { return new SimId(Guid.Empty, 0); } }

        /// <summary>
        /// The local Id of the object 
        /// The local id should be unique inside the group described by <see cref="GlobalId"/>)
        /// </summary>
        public long LocalId { get; }
        /// <summary>
        /// The global Id. This id can either be set directly or can be derived from the <see cref="Location"/>
        /// </summary>
        public Guid GlobalId { get; }
        /// <summary>
        /// The location of the global Id. This property may be null.
        /// </summary>
        public IReferenceLocation Location { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimId"/> class
        /// </summary>
        /// <param name="location">The location in which this Id should exist</param>
        /// <param name="localId">The local Id inside the location</param>
        public SimId(IReferenceLocation location, long localId)
        {
            if (localId < 0)
                throw new ArgumentException(string.Format("{0} must be positive and unique", nameof(localId)));

            this.LocalId = localId;
            this.Location = location;

            if (location != null)
                this.GlobalId = location.GlobalID;
            else
                this.GlobalId = Guid.Empty;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimId"/> class
        /// </summary>
        /// <param name="globalId">The global id</param>
        /// <param name="localId">The local Id inside the location</param>
        public SimId(Guid globalId, long localId)
        {
            if (localId < 0)
                throw new ArgumentException(string.Format("{0} must be positive and unique", nameof(localId)));

            this.LocalId = localId;
            this.Location = null;
            this.GlobalId = globalId;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimId"/> class. Sets the <see cref="GlobalId"/> to <see cref="Guid.Empty"/>
        /// </summary>
        /// <param name="localId">The local Id inside the location</param>
        public SimId(long localId) : this(Guid.Empty, localId) { }

        /// <inheritdoc />
        public bool Equals(SimId other)
        {
            return other.LocalId == this.LocalId && other.GlobalId == this.GlobalId;
        }
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is SimId id)
                return this.Equals(id);
            return false;
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            return GlobalId.GetHashCode() * 31 ^ LocalId.GetHashCode();
        }

        /// <inheritdoc />
        public static bool operator ==(SimId lhs, SimId rhs)
        {
            return lhs.Equals(rhs);
        }
        /// <inheritdoc />
        public static bool operator !=(SimId lhs, SimId rhs)
        {
            return !lhs.Equals(rhs);
        }


        /// <inheritdoc/>
        public override string ToString()
        {
            if (this.GlobalId == Guid.Empty)
                return this.LocalId.ToString();
            else
                return this.GlobalId.ToString() + ": " + this.LocalId.ToString();
        }
    }
}
