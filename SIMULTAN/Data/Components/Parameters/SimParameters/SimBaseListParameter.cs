using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Base class for list parameters
    /// </summary>
    /// <typeparam name="T">Inner type of the list</typeparam>
    public abstract class SimBaseListParameter<T> : SimBaseParameter<SimParameterValueCollection<T>>
    {
        /// <summary>
        /// Count of the value or -1 if null
        /// </summary>
        public int Count => HasValue ? Value.Count : -1;

        /// <summary>
        /// If the value is not null
        /// </summary>
        public bool HasValue => Value != null;

        /// <summary>
        /// The value of the parameter. Type depends on the implementation. 
        /// </summary>
        public override SimParameterValueCollection<T> Value
        {
            get { return value; }
            set
            {
                if (!IsSameValue(this.value, value))
                {
                    this.NotifyWriteAccess();

                    this.value = value;
                    this.SendInstanceValueChanges = false;
                    this.NotifyPropertyChanged(nameof(Value));
                    this.NotifyValueChanged();
                    UpdateState();
                    this.NotifyChanged();
                    if (value != null)
                    {
                        value.Parameter = this;
                    }
                    this.SendInstanceValueChanges = true;

                    //Notify geometry exchange
                    if (this.Component != null && this.Component.Factory != null)
                        this.Component.Factory.ProjectData.ComponentGeometryExchange.OnParameterValueChanged(this);
                }
            }
        }

        /// <summary>
        /// Creates a copy of a <see cref="SimBaseListParameter{T}"/>
        /// </summary>
        /// <param name="original">The original</param>
        /// <param name="copyValue">If the value should be copied</param>
        protected SimBaseListParameter(SimBaseParameter<SimParameterValueCollection<T>> original, bool copyValue = true)
            : base(original, copyValue)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimBaseListParameter{T}"/>
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name</param>
        /// <param name="value">The value</param>
        /// <param name="allowedOperations">The allowed operations</param>
        protected SimBaseListParameter(SimTaxonomyEntry nameTaxonomyEntry, SimParameterValueCollection<T> value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(nameTaxonomyEntry, value, allowedOperations)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimBaseListParameter{T}"/>
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="value">The value</param>
        /// <param name="allowedOperations">The allowed operations</param>
        protected SimBaseListParameter(string name, SimParameterValueCollection<T> value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, value, allowedOperations)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimBaseListParameter{T}"/>
        /// </summary>
        /// <param name="localId">The local ID</param>
        /// <param name="name">The name</param>
        /// <param name="category">The category</param>
        /// <param name="propagation">The propagation mode</param>
        /// <param name="value">The value</param>
        /// <param name="description">The description</param>
        /// <param name="valueFieldPointer">The value field pointer</param>
        /// <param name="allowedOperations">Allowed operations</param>
        /// <param name="instancePropagationMode">The instance propagation mode</param>
        /// <param name="isAutomaticallyGenerated">If the parameter was automatically generated</param>
        protected SimBaseListParameter(long localId, string name, SimCategory category, SimInfoFlow propagation, SimParameterValueCollection<T> value, string description, SimParameterValueSource valueFieldPointer, SimParameterOperations allowedOperations = SimParameterOperations.All, SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance, bool isAutomaticallyGenerated = false)
            : base(localId, name, category, propagation, value, description, valueFieldPointer, allowedOperations, instancePropagationMode, isAutomaticallyGenerated)
        {
        }

        internal void NotifyValuePropertyChanged()
        {
            NotifyPropertyChanged(nameof(Value));
        }

        /// <summary>
        /// Call when the value collection changes
        /// </summary>
        internal void NotifyCollectionChanged()
        {
            NotifyWriteAccess();
            NotifyValueChanged();
            UpdateState();
            NotifyChanged();
        }
    }
}
