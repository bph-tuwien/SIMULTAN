using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.FlowNetworks
{
    /// <summary>
    /// A two-part id for product definitions. It contains a local and a global id.
    /// </summary>
    public class SimObjectId : INotifyPropertyChanged, IComparable
    {
        #region STATIC

        /// <summary>
        /// The empty product id with default values for its properties.
        /// </summary>
        public static readonly SimObjectId Empty = new SimObjectId(Guid.Empty, -1L);

        #endregion

        #region PROPERTIES: INotifyPropertyChanged

        /// <summary>
        /// The event raised on the change of a property.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Called when a property value has changed.
        /// </summary>
        /// <param name="_propName">the name of the changed property</param>
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion  

        #region IComparable

        /// <summary>
        /// Implementation of the IComparable interface.
        /// </summary>
        /// <param name="obj">the other ProductId to compare to</param>
        /// <returns>the result</returns>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            SimObjectId otherPID = obj as SimObjectId;
            if (otherPID != null)
            {
                if (this.GlobalId.CompareTo(otherPID.GlobalId) == 0)
                    return this.LocalId.CompareTo(otherPID.LocalId);
                else
                    return this.GlobalId.CompareTo(otherPID.GlobalId);
            }
            else
                throw new ArgumentException("Object is not a ProductId!");
        }

        #endregion

        #region PROPERTIES: Local

        /// <summary>
        /// The id of the displayable product definition. The id is unique 
        /// within an open file or project. When projects are merged, duplicate ids are changed!
        /// </summary>
        public long LocalId
        {
            get { return this.local_id; }
            internal set
            {
                if (this.local_id != value)
                {
                    this.local_id = value;
                    this.RegisterPropertyChanged(nameof(LocalId));
                }
            }
        }
        private long local_id;

        #endregion

        #region PROPERTIES: Global

        /// <summary>
        /// The id of the product's global location.
        /// </summary>
        public Guid GlobalId
        {
            get { return this.global_id; }
            private set
            {
                if (this.global_id != value)
                {
                    this.global_id = value;
                    this.RegisterPropertyChanged(nameof(GlobalId));
                }
            }
        }
        private Guid global_id = Guid.Empty;

        /// <summary>
        /// The actual global location. Can be null. It can be set only if its id corresponds to the
        /// id in Property GlobalId.
        /// </summary>
        public IReferenceLocation GlobalLocation
        {
            get { return this.global_location; }
            internal set
            {
                if (this.GlobalId != Guid.Empty && value != default && this.GlobalId == value.GlobalID)
                {
                    this.global_location = value;
                    this.RegisterPropertyChanged(nameof(GlobalLocation));
                }
            }
        }
        private IReferenceLocation global_location;

        #endregion

        #region OVERRIDES

        /// <inheritdoc/>
        public override string ToString()
        {
            if (this.GlobalId == Guid.Empty)
                return this.LocalId.ToString();
            else
                return this.GlobalId.ToString() + ": " + this.LocalId.ToString();
        }

        /// <summary>
        /// Parses a string into a tuple of Guid and long. The expected delimiter is ": ".
        /// </summary>
        /// <param name="simObjectString">the input string</param>
        /// <returns>the tuple of global Guid and local long id</returns>
        public static (Guid global, long local, int err) FromString(string simObjectString)
        {
            if (string.IsNullOrEmpty(simObjectString))
                return (Guid.Empty, -1L, -1);

            string[] comps = simObjectString.Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries);
            if (comps.Length != 2)
                return (Guid.Empty, -1L, -1);

            bool success_global = Guid.TryParse(comps[0], out var global);
            bool success_local = long.TryParse(comps[1], out var local);
            if (success_global && success_local)
                return (global, local, 0);
            else
                return (Guid.Empty, -1L, -1);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(this, obj)) return true;

            //Check whether the object is null. 
            if (Object.ReferenceEquals(obj, null)) return false;

            SimObjectId pid = obj as SimObjectId;
            if (pid == null)
                return false;
            else
                return (this.GetHashCode() == pid.GetHashCode());
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.global_id.GetHashCode() * 31 ^ this.local_id.GetHashCode();
        }

        /// <summary>
        /// Checks two instances of ProductId for equality.
        /// </summary>
        /// <param name="pid1">operand 1</param>
        /// <param name="pid2">operand 2</param>
        /// <returns>true if both the local and global ids are the same</returns>
        public static bool operator ==(SimObjectId pid1, SimObjectId pid2)
        {
            if (Object.ReferenceEquals(pid1, null) || Object.ReferenceEquals(pid2, null))
                return Object.Equals(pid1, pid2);

            return pid1.Equals(pid2);
        }
        /// <summary>
        /// Checks two instances of ProductId for inequality.
        /// </summary>
        /// <param name="pid1">operand 1</param>
        /// <param name="pid2">operand 2</param>
        /// <returns>true if either the local or global ids differ</returns>
        public static bool operator !=(SimObjectId pid1, SimObjectId pid2)
        {
            if (Object.ReferenceEquals(pid1, null) || Object.ReferenceEquals(pid2, null))
                return !Object.Equals(pid1, pid2);

            return !(pid1.Equals(pid2));
        }

        #endregion

        #region .CTOR

        /// <summary>
        /// Create a locally identifiable instance of type ProductId.
        /// </summary>
        /// <param name="localId">the local id</param>
        public SimObjectId(long localId)
        {
            this.local_id = localId;
        }

        /// <summary>
        /// Create a globally identifiable instance of type ProductId.
        /// </summary>
        /// <param name="globalId">the global id</param>
        /// <param name="localId">the local id</param>
        public SimObjectId(Guid globalId, long localId)
        {
            this.global_id = globalId;
            this.local_id = localId;
        }

        /// <summary>
        /// Create a globally located instance of ProductId.
        /// </summary>
        /// <param name="globalLocation">the global location, can be null</param>
        /// <param name="localId">the local id</param>
        public SimObjectId(IReferenceLocation globalLocation, long localId)
        {
            this.global_id = (globalLocation == null) ? Guid.Empty : globalLocation.GlobalID;
            this.local_id = localId;
            this.global_location = globalLocation;
        }

        #endregion
    }
}
