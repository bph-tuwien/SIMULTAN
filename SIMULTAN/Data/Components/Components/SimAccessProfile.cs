using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SIMULTAN.Data.Components
{

    /// <summary>
    /// Stores the access management for a component.
    /// The profile consists of an <see cref="SimAccessProfileEntry"/> for each role which can be accessed via the indexing operator.
    ///
    /// Also provides a <see cref="ProfileState"/> which states whether access operations have happened in the correct order.
    /// The valid workflow is that a user writes to a component, then the component is supervised. When the component is finished, 
    /// it get's released.
    /// 
    /// The administrator always has read/write access.
    /// In addition to the administrator, only one user may have write access.
    /// 
    /// Check the <see cref="SimAccessProfileEntry.Access"/> to see which access rights the current user has.
    /// </summary>
    public class SimAccessProfile : INotifyPropertyChanged, IEnumerable<SimAccessProfileEntry>
    {

        #region PROPERTIES

        private Dictionary<SimUserRole, SimAccessProfileEntry> profile;

        /// <summary>
        /// Returns the <see cref="SimAccessProfileEntry"/> for a given role
        /// </summary>
        /// <param name="role">The role</param>
        /// <returns>The access tracker for the role</returns>
        public SimAccessProfileEntry this[SimUserRole role]
        {
            get { return this.profile[role]; }
        }
        /// <summary>
        /// Returns the <see cref="SimAccessProfileEntry"/> for a given user (determine by <see cref="SimUser.Role"/>)
        /// </summary>
        /// <param name="user">The user</param>
        /// <returns>The access tracker for the user's role</returns>
        public SimAccessProfileEntry this[SimUser user]
        {
            get
            {
                if (user == null)
                    throw new ArgumentNullException(string.Format("{0} may not be null", nameof(user)));
                return this[user.Role];
            }
        }

        /// <summary>
        /// Returns the state of the profile. The state is determined by the order of accesses to this profile.
        /// See <see cref="SimComponentValidity"/> for the different states that can happen.
        /// </summary>
        public SimComponentValidity ProfileState
        {
            get { return profileState; }
            private set
            {
                if (value != profileState)
                {
                    profileState = value;
                    this.NotifyPropertyChanged(nameof(ProfileState));
                }
            }
        }
        private SimComponentValidity profileState;

        #endregion

        #region Events

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        /// <summary>
        /// Called when the access rights of any user has changed
        /// </summary>
        public event EventHandler AccessChanged;
        private void NotifyAccessChanged()
        {
            this.AccessChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the last access date in one of the trackers has changed
        /// </summary>
        public event EventHandler LastAccessChanged;
        private void NotifyLastAccessChanged()
        {
            LastAccessChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the SimAccessProfile class
        /// </summary>
        /// <param name="owner">The user which receives write access</param>
        public SimAccessProfile(SimUserRole owner)
        {
            this.profile = new Dictionary<SimUserRole, SimAccessProfileEntry>();

            foreach (SimUserRole role in Enum.GetValues(typeof(SimUserRole)))
            {
                this.profile.Add(role, new SimAccessProfileEntry(role, this));
            }

            //Set access for admin and owner
            this.profile[SimUserRole.ADMINISTRATOR].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            this.profile[owner].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;

            UpdateProfileState();
        }

        /// <summary>
        /// Initializes a new instance of the SimAccessProfile class
        /// </summary>
        /// <param name="trackers">A dictionary of roles with associated access tracker which are used to initialize the profile</param>
        internal SimAccessProfile(IDictionary<SimUserRole, SimAccessProfileEntry> trackers)
        {
            if (trackers == null)
                throw new ArgumentNullException(nameof(trackers));

            this.profile = new Dictionary<SimUserRole, SimAccessProfileEntry>(trackers);

            foreach (SimUserRole role in Enum.GetValues(typeof(SimUserRole)))
            {
                if (!this.profile.ContainsKey(role))
                    this.profile.Add(role, new SimAccessProfileEntry(role, this));
            }

            foreach (var p in this.profile.Values)
                p.AccessProfile = this;

            UpdateProfileState();
        }

        /// <summary>
        /// Initializes a new instance of the SimAccessProfile class by copying all settings from another instance
        /// </summary>
        /// <param name="original">The profile to copy from</param>
        public SimAccessProfile(SimAccessProfile original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));

            this.profile = new Dictionary<SimUserRole, SimAccessProfileEntry>();
            foreach (var entry in original.profile)
            {
                this.profile.Add(entry.Key, new SimAccessProfileEntry(entry.Value, this));
            }

            UpdateProfileState();
        }


        #region METHODS

        /// <summary>
        /// Returns the last access time and role for a given privilege
        /// </summary>
        /// <param name="access">The access privilege. Supported types: 
        /// <see cref=" SimComponentAccessPrivilege.Write"/>, <see cref="SimComponentAccessPrivilege.Supervize"/>,
        /// <see cref="SimComponentAccessPrivilege.Release"/>
        /// </param>
        /// <returns></returns>
        public (DateTime lastAccess, SimUserRole role) LastAccess(SimComponentAccessPrivilege access)
        {
            Func<SimAccessProfileEntry, DateTime> timeSelector = null;
            switch (access)
            {
                case SimComponentAccessPrivilege.Write:
                    timeSelector = (x) => x.LastAccessWrite;
                    break;
                case SimComponentAccessPrivilege.Release:
                    timeSelector = (x) => x.LastAccessRelease;
                    break;
                case SimComponentAccessPrivilege.Supervize:
                    timeSelector = (x) => x.LastAccessSupervize;
                    break;
            }

            if (timeSelector != null)
            {
                var result = this.ArgMax(x => timeSelector(x));
                return (result.key, result.value.Role);
            }
            else
                throw new NotSupportedException("Enumeration value not supported");
        }


        internal void UpdateProfileState()
        {
            DateTime lastWrite = this.profile.Values.Max(x => x.LastAccessWrite);
            DateTime lastSupervize = this.profile.Values.Max(x => x.LastAccessSupervize);
            DateTime lastRelease = this.profile.Values.Max(x => x.LastAccessRelease);

            bool writeBeforeSupervize = lastSupervize == DateTime.MinValue || lastWrite <= lastSupervize;
            bool superVizeBeforeRelease = lastRelease == DateTime.MinValue || lastSupervize <= lastRelease;
            bool writeBeforeRelease = lastRelease == DateTime.MinValue || lastWrite <= lastRelease;

            if (writeBeforeSupervize && superVizeBeforeRelease && writeBeforeRelease)
                this.ProfileState = SimComponentValidity.Valid;
            else if (!writeBeforeSupervize && writeBeforeRelease) //Between super and release
                this.ProfileState = SimComponentValidity.WriteAfterSupervize;
            else if (superVizeBeforeRelease && !writeBeforeRelease) //Write is last
                this.ProfileState = SimComponentValidity.WriteAfterRelease;
            else if (!superVizeBeforeRelease)
                this.ProfileState = SimComponentValidity.SupervizeAfterRelease;
        }

        private void AfterTrackerChanged(SimUserRole sender)
        {
            this.AdjustWritingAccessAfterUserInput(sender);
            UpdateProfileState();
            NotifyAccessChanged();
        }

        internal void ResetAccessFlags(SimUserRole owner)
        {
            this.isCurrentlyAdjusting = true;

            foreach (var tracker in this.profile.Values)
            {
                if (tracker.Role == SimUserRole.ADMINISTRATOR || tracker.Role == owner)
                    tracker.Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
                else
                    tracker.Access = SimComponentAccessPrivilege.None;
            }

            this.isCurrentlyAdjusting = false;
        }

        internal void OnTrackerAccessChanged(SimAccessProfileEntry tracker)
        {
            if (!isCurrentlyAdjusting)
            {
                if (tracker != null)
                {
                    AfterTrackerChanged(tracker.Role);
                }
            }
        }

        internal void OnTrackerLastAccessChanged(SimAccessProfileEntry tracker)
        {
            this.NotifyLastAccessChanged();
        }

        /// Prevents the AdjustWritingAccessAfterUserInput method from calling itself
        private bool isCurrentlyAdjusting = false;

        /// <summary>
        /// Makes sure that the administrator always has read/write access and
        /// that only one user (in addition to the administrator) has write access
        /// </summary>
        /// <param name="changedRole"></param>
        private void AdjustWritingAccessAfterUserInput(SimUserRole changedRole)
        {
            this.isCurrentlyAdjusting = true;

            bool changeHasWriteAccess = changedRole != SimUserRole.ADMINISTRATOR && this.profile[changedRole].Access.HasFlag(SimComponentAccessPrivilege.Write);

            //Set access for admin
            this.profile[SimUserRole.ADMINISTRATOR].Access |= SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;

            foreach (var entry in this.profile.Values)
            {
                if (entry.Role != SimUserRole.ADMINISTRATOR && (entry.Role != changedRole && changeHasWriteAccess))
                    entry.Access &= ~SimComponentAccessPrivilege.Write;

            }

            this.isCurrentlyAdjusting = false;
        }

        #endregion

        #region TO STRING

        public void AddToExport(ref StringBuilder _sb, bool _is_last)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.ACCESS_PROFILE);                          // ACCESS_PROFILE

            _sb.AppendLine(((int)ComponentAccessProfileSaveCode.STATE).ToString());
            _sb.AppendLine(ComponentUtils.ComponentValidityToString(this.ProfileState));

            _sb.AppendLine(((int)ComponentAccessProfileSaveCode.PROFILE).ToString());
            _sb.AppendLine(this.profile.Count.ToString());

            foreach (var entry in this.profile)
            {
                entry.Value.AddToExport(ref _sb, ComponentUtils.ComponentManagerTypeToLetter(entry.Key));
            }

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND

            if (!_is_last)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
                _sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);                         // ENTCTN
            }
        }

        #endregion

        #region IEnumerable

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return profile.Values.GetEnumerator();
        }
        /// <inheritdoc/>
        IEnumerator<SimAccessProfileEntry> IEnumerable<SimAccessProfileEntry>.GetEnumerator()
        {
            return profile.Values.GetEnumerator();
        }

        #endregion
    }
}