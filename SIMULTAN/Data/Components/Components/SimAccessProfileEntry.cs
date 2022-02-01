using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Stores the access settings for a user role. Used inside an <see cref="SimAccessProfile"/>.
    /// Additionally stores the last access timestamps for this role.
    /// </summary>
    public class SimAccessProfileEntry : INotifyPropertyChanged
    {
        #region PROPERTIES & EVENTS

        /// <summary>
        /// The <see cref="SimAccessProfile"/> this instance belongs to. Set automatically by assigning the tracker to a access profile.
        /// </summary>
        public SimAccessProfile AccessProfile { get; internal set; }

        /// <summary>
        /// A combination of access flags which describe which access right the rule has
        /// </summary>
        public SimComponentAccessPrivilege Access
        {
            get { return access; }
            set
            {
                if (access != value)
                {
                    access = value;
                    this.NotifyPropertyChanged(nameof(Access));

                    if (AccessProfile != null)
                        AccessProfile.OnTrackerAccessChanged(this);
                }
            }
        }
        private SimComponentAccessPrivilege access;

        /// <summary>
        /// The role affected by this tracker
        /// </summary>
        public SimUserRole Role { get; }

        /// <summary>
        /// Stores the last time at which this role has made a write access.
        /// Returns <see cref="DateTime.MinValue"/> when no write access has happened
        /// 
        /// Only users with the <see cref="SimComponentAccessPrivilege.Write"/> privilege may set this property.
        /// A new value must be greater than the current value.
        /// </summary>
        public DateTime LastAccessWrite
        {
            get { return this.lastAccessWrite; }
            internal set
            {
                if (!this.access.HasFlag(SimComponentAccessPrivilege.Write))
                    throw new AccessDeniedException("User without write access tried to record a write access");
                if (value < this.lastAccessWrite) //Value earlier than current write access
                    throw new ArgumentException("New date must be greater or equal to the current date");

                if (this.lastAccessWrite != value)
                {
                    this.lastAccessWrite = value;

                    if (AccessProfile != null)
                    {
                        AccessProfile.UpdateProfileState();
                        AccessProfile.OnTrackerLastAccessChanged(this);
                    }

                    this.NotifyPropertyChanged(nameof(LastAccessWrite));
                }
            }
        }
        private DateTime lastAccessWrite = DateTime.MinValue;

        /// <summary>
        /// Stores the last time when the component has been supervised.
        /// Returns <see cref="DateTime.MinValue"/> when no supervision access has happened
        ///
        /// Only users with the <see cref="SimComponentAccessPrivilege.Supervize"/> privilege may set this property.
        /// A new value must be greater than the current value.
        /// </summary>
        public DateTime LastAccessSupervize
        {
            get { return this.lastAccessSupervize; }
            set
            {
                if (!this.access.HasFlag(SimComponentAccessPrivilege.Supervize))
                    throw new AccessDeniedException("User without supervize access tried to record a supervize access");
                if (value < this.lastAccessSupervize) //Value earlier than current write access
                    throw new ArgumentException("New date must be greater or equal to the current date");

                if (this.lastAccessSupervize != value)
                {
                    this.lastAccessSupervize = value;

                    if (AccessProfile != null)
                    {
                        AccessProfile.UpdateProfileState();
                        AccessProfile.OnTrackerLastAccessChanged(this);
                    }

                    this.NotifyPropertyChanged(nameof(LastAccessSupervize));
                }
            }
        }
        private DateTime lastAccessSupervize = DateTime.MinValue;

        /// <summary>
        /// Stores the last time when the component has been released. 
        /// Returns <see cref="DateTime.MinValue"/> when no release access has happened.
        /// 
        /// Only users with the <see cref="SimComponentAccessPrivilege.Release"/> privilege may set this property.
        /// A new value must be greater than the current value.
        /// </summary>
        public DateTime LastAccessRelease
        {
            get { return this.lastAccessRelease; }
            set
            {
                if (!this.access.HasFlag(SimComponentAccessPrivilege.Release))
                    throw new AccessDeniedException("User without release access tried to record a release access");
                if (value < this.lastAccessRelease) //Value earlier than current write access
                    throw new ArgumentException("New date must be greater or equal to the current date");

                if (this.lastAccessRelease != value)
                {
                    this.lastAccessRelease = value;

                    if (AccessProfile != null)
                    {
                        AccessProfile.UpdateProfileState();
                        AccessProfile.OnTrackerLastAccessChanged(this);
                    }

                    this.NotifyPropertyChanged(nameof(LastAccessRelease));
                }
            }
        }
        private DateTime lastAccessRelease = DateTime.MinValue;

        #endregion

        #region Events

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the SimAccessProfileEntry class
        /// </summary>
        /// <param name="role">The role which is affected by this instance</param>
        /// <param name="profile">The profile to which this tracker belongs</param>
        internal SimAccessProfileEntry(SimUserRole role, SimAccessProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException("{0} may not be null", nameof(profile));

            this.AccessProfile = profile;
            this.Role = role;
        }

        /// <summary>
        /// Initializes a new instance of the SimAccessProfileEntry class by copying all settings from another profile
        /// </summary>
        /// <param name="original">The profile from which the settings should be copied</param>
        /// <param name="newProfile">The profile to which this tracker belongs</param>
        internal SimAccessProfileEntry(SimAccessProfileEntry original, SimAccessProfile newProfile)
        {
            if (original == null)
                throw new ArgumentNullException("{0} may not be null", nameof(original));
            if (newProfile == null)
                throw new ArgumentNullException("{0} may not be null", nameof(newProfile));

            this.AccessProfile = newProfile;

            this.Role = original.Role;
            this.access = original.access;

            this.lastAccessWrite = original.lastAccessWrite;
            this.lastAccessSupervize = original.lastAccessSupervize;
            this.lastAccessRelease = original.lastAccessRelease;
        }

        /// <summary>
        /// Initializes a new instance of the SimAccessProfileEntry class
        /// </summary>
        /// <param name="role">The role which is affected by this instance</param>
        /// <param name="access">The access flags for the role</param>
        /// <param name="lastWrite">Last write access by this role</param>
        /// <param name="lastSupervize">Last supervision access by this role</param>
        /// <param name="lastRelease">Last release access by this role</param>
        internal SimAccessProfileEntry(SimUserRole role, SimComponentAccessPrivilege access,
                                        DateTime lastWrite, DateTime lastSupervize, DateTime lastRelease)
        {
            this.Role = role;
            this.access = access;

            this.lastAccessWrite = lastWrite;
            this.lastAccessSupervize = lastSupervize;
            this.lastAccessRelease = lastRelease;
        }

        #endregion


        #region METHODS

        /// <summary>
        /// Determines if a given access right is allowed
        /// </summary>
        /// <param name="access">The access right to check</param>
        /// <returns>True when the access right is included in <see cref="Access"/>, otherwise False</returns>
        public bool HasAccess(SimComponentAccessPrivilege access)
        {
            return Access.HasFlag(access);
        }

        internal void ForceSetWriteAccess(SimUser user, DateTime value)
        {
            if (value < this.lastAccessWrite) //Value earlier than current write access
                throw new ArgumentException("New date must be greater or equal to the current date");

            if (this.lastAccessWrite != value)
            {
                this.lastAccessWrite = value;

                if (AccessProfile != null)
                    AccessProfile.UpdateProfileState();

                this.NotifyPropertyChanged(nameof(LastAccessWrite));
            }
        }

        #endregion

        #region TO STRING

        public void AddToExport(ref StringBuilder _sb, string _key = null)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.ACCESS_TRACKER);                          // ACCESS_TRACKER

            if (!(string.IsNullOrEmpty(_key)))
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_KEY).ToString());
                _sb.AppendLine(_key);
            }

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.FLAGS).ToString());
            _sb.AppendLine(ComponentUtils.ComponentAccessTypeToString(this.access));


            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.WRITE_LAST).ToString());
            _sb.AppendLine(this.lastAccessWrite.ToUniversalTime().Ticks.ToString());

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.SUPERVIZE_LAST).ToString());
            _sb.AppendLine(this.lastAccessSupervize.ToUniversalTime().Ticks.ToString());

            _sb.AppendLine(((int)ComponentAccessTrackerSaveCode.RELEASE_LAST).ToString());
            _sb.AppendLine(this.lastAccessRelease.ToUniversalTime().Ticks.ToString());
        }

        #endregion

        #region COMPARISON

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (!(obj is SimAccessProfileEntry t))
                return false;
            else
            {
                return (this.access == t.access) && (this.lastAccessWrite == t.lastAccessWrite) &&
                       (this.lastAccessSupervize == t.lastAccessSupervize) && (this.lastAccessRelease == t.lastAccessRelease) &&
                       this.Role == t.Role;
            }
        }
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.access.GetHashCode() ^ this.lastAccessWrite.GetHashCode() ^ this.lastAccessSupervize.GetHashCode() ^
                this.lastAccessRelease.GetHashCode() ^ this.Role.GetHashCode();
        }
        /// <inheritdoc/>
        public static bool operator ==(SimAccessProfileEntry t1, SimAccessProfileEntry t2)
        {
            if (object.Equals(t1, null) && object.Equals(t2, null))
                return true;

            // If one is null, but not both, return false.
            if ((object.Equals(t1, null) && !object.Equals(t2, null)) || (!object.Equals(t1, null) && object.Equals(t2, null)))
                return false;

            // Return true if all the relevant fields match:
            return (t1.Equals(t2));
        }
        /// <inheritdoc/>
        public static bool operator !=(SimAccessProfileEntry _t1, SimAccessProfileEntry _t2)
        {
            return !(_t1 == _t2);
        }

        #endregion
    }
}