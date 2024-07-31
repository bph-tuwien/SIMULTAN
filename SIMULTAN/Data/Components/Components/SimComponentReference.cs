using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Stores a reference from one component to another.
    /// The reference may also point to nothing (<see cref="Target"/> equals null)
    /// </summary>
    public class SimComponentReference : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// The referenced component. Null when the reference points nowhere
        /// </summary>
        public SimComponent Target
        {
            get { return target; }
            set
            {
                if (target != value)
                {
                    if (Owner != null)
                        Owner.RecordWriteAccess();

                    var oldTarget = target;

                    this.target = value;

                    if (this.target != null)
                        this.TargetId = this.target.Id;
                    else
                        this.TargetId = SimId.Empty;


                    if (oldTarget != null)
                    {
                        oldTarget.ReferencedBy_Internal.Remove(this);
                        oldTarget.IsBeingDeleted -= this.Target_IsBeingDeleted;
                    }

                    if (target != null && Owner != null && Owner.Factory != null)
                    {
                        target.ReferencedBy_Internal.Add(this);
                        Owner.PropagateRefParamValueToClosest(target);
                        target.IsBeingDeleted += this.Target_IsBeingDeleted;
                    }

                    if (Owner != null)
                    {
                        Owner.Parameters.ForEach(x => x.UpdateState());
                        Owner.Factory?.NotifyChanged();
                    }

                    NotifyPropertyChanged(nameof(Target));
                }
            }
        }
        private SimComponent target = null;

        /// <summary>
        /// Id of the referenced component. This property is used during loading and in cases where the target component couldn't be found
        /// </summary>
        public SimId TargetId
        {
            get
            {
                if (this.Target != null)
                    return this.Target.Id;
                else
                    return targetId;
            }
            private set
            {
                if (this.targetId != value)
                {
                    this.targetId = value;
                    NotifyPropertyChanged(nameof(TargetId));
                }
            }
        }
        private SimId targetId = SimId.Empty;

        /// <summary>
        /// The slot for the reference. Currently, the slot of the target component does not have to match the reference slot.
        /// </summary>
        public SimSlot Slot
        {
            get { return slot; }
            set
            {
                if (this.slot != value)
                {
                    if (slot.SlotBase != null)
                        slot.SlotBase.RemoveDeleteAction();

                    this.slot = value;

                    if (slot.SlotBase != null)
                        slot.SlotBase.SetDeleteAction(SlotBaseTaxonomyEntryDeleted);

                    NotifyPropertyChanged(nameof(Slot));
                }
            }
        }
        private SimSlot slot;

        /// <summary>
        /// Stores the component which owns the reference.
        /// Automatically set when adding the reference to the <see cref="SimComponent.ReferencedComponents"/> collection.
        /// </summary>
        public SimComponent Owner
        {
            get { return owner; }
            internal set
            {
                if (this.owner != value)
                {
                    if (value == null && Target != null) //Remove from old
                    {
                        this.Target.ReferencedBy_Internal.Remove(this);
                        Target.IsBeingDeleted -= this.Target_IsBeingDeleted;
                    }

                    this.owner = value;

                    if (this.owner != null && this.owner.Factory != null && Target != null) //Add to new
                    {
                        this.Target.ReferencedBy_Internal.Add(this);
                        owner.PropagateRefParamValueToClosest(Target);
                        Target.Parameters.ForEach(x => x.UpdateState());
                        Target.IsBeingDeleted += this.Target_IsBeingDeleted;
                    }

                    NotifyPropertyChanged(nameof(Owner));
                }
            }
        }
        private SimComponent owner = null;

        #endregion

        #region Events

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the SimComponentReference class.
        /// This constructor creates an empty reference
        /// </summary>
        /// <param name="slot">The slot of the reference</param>
        public SimComponentReference(SimSlot slot)
        {
            this.Slot = slot;
        }
        /// <summary>
        /// Initializes a new instance of the SimComponentReference class.
        /// The target component is searched when <see cref="RestoreReferences"/> is called.
        /// </summary>
        /// <param name="slot">The slot of the reference</param>
        /// <param name="targetId">Id of the target component</param>
        public SimComponentReference(SimSlot slot, SimId targetId)
        {
            this.Slot = slot;
            this.TargetId = targetId;
        }
        /// <summary>
        /// Initializes a new instance of the SimComponentReference class.
        /// </summary>
        /// <param name="slot">The slot of the reference</param>
        /// <param name="target">The target component</param>
        public SimComponentReference(SimSlot slot, SimComponent target)
        {
            this.Slot = slot;
            this.Target = target;
        }


        internal void RestoreReferences()
        {
            if (this.Owner == null || this.Owner.Factory == null)
                throw new InvalidOperationException("Reference has to be added to an active component first");

            if (this.Target == null)
            {
                this.Target = this.Owner.Factory.ProjectData.IdGenerator.GetById<SimComponent>(this.TargetId);
                Debug.WriteLine("Search: {0}; Found {1}", this.TargetId, this.Target != null);
            }
        }

        internal void NotifyFactoryChanged(SimComponentCollection newValue, SimComponentCollection oldValue)
        {
            if (oldValue != null && Target != null)
            {
                Target.ReferencedBy_Internal.Remove(this);
                if (Owner != null)
                    Owner.Parameters.ForEach(x => x.UpdateState());
                Target.IsBeingDeleted -= this.Target_IsBeingDeleted;
            }

            if (newValue != null && Owner != null && Target != null)
            {
                Target.ReferencedBy_Internal.Add(this);
                Owner.PropagateRefParamValueToClosest(target);
                Owner.Parameters.ForEach(x => x.UpdateState());
                Target.IsBeingDeleted += this.Target_IsBeingDeleted;
            }
        }

        private void Target_IsBeingDeleted(object sender)
        {
            this.Target = null;
            this.TargetId = SimId.Empty;
        }

        private void SlotBaseTaxonomyEntryDeleted(SimTaxonomyEntry caller)
        {
            if (Owner != null && Owner.Factory != null)
            {
                var undefinedTax = Owner.GetDefaultSlotTaxonomyEntry(SimDefaultSlotKeys.Undefined);
                Slot = new SimSlot(new SimTaxonomyEntryReference(undefinedTax), Slot.SlotExtension);
            }
        }
    }
}