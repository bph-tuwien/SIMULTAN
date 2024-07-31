using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SIMULTAN.Data.MultiValues
{


    /// <summary>
    /// Base class for all pointers into ValueFields
    /// SimMultiValuePointer are the connection between Parameters and ValueFields
    /// </summary>
    public abstract class SimMultiValueParameterSource : SimParameterValueSource
    {
        #region Properties

        private Dictionary<string, SimBaseParameter> valuePointerParameters;
        private Dictionary<string, string> valuePointerParameterUnits;

        /// <summary>
        /// Stores the parameter to which the pointer is attached. Automatically set by the parameter class
        /// </summary>
        public override SimBaseParameter TargetParameter
        {
            get { return targetParameter; }
            internal set
            {
                targetParameter = value;

                if (targetParameter != null)
                {
                    this.AttachEvents();


                    this.SetParamValue();

                    if (targetParameter.Component != null)
                    {
                        AttachAllPointerParameter();
                        LastAttachedComponent = targetParameter.Component;
                    }
                    else
                    {
                        LastAttachedComponent = null;
                        DetachAllPointerParameter();
                    }
                }
                else
                {
                    DetachEvents();

                    LastAttachedComponent = null;
                    DetachAllPointerParameter();

                    foreach (var key in valuePointerParameters.Keys.ToList())
                        ReplaceValuePointerParameter(key, null);
                }
            }
        }

        /// <summary>
        /// Stores the ValueField into which this pointer points
        /// </summary>
        public abstract SimMultiValue ValueField { get; }

        private SimComponent LastAttachedComponent
        {
            get { return lastAttachedComponent; }
            set
            {
                if (value != lastAttachedComponent)
                {
                    if (lastAttachedComponent != null)
                    {
                        lastAttachedComponent.Parameters.CollectionChanged -= Component_Parameters_CollectionChanged;
                        DetachAllPointerParameter();
                    }

                    lastAttachedComponent = value;

                    if (lastAttachedComponent != null)
                    {
                        lastAttachedComponent.Parameters.CollectionChanged += Component_Parameters_CollectionChanged;
                        AttachAllPointerParameter();
                    }
                }
            }
        }



        private SimComponent lastAttachedComponent;

        #endregion

        #region SimParameterValueSource

        /// <summary>
        /// Called when the instance is disposed
        /// </summary>
        /// <param name="isDisposing">True when called from the Dispose() method, False when called from the finalizer</param>
        protected override void Dispose(bool isDisposing)
        {
            if (!IsDisposed)
            {
                if (this.targetParameter != null)
                {
                    if (this.targetParameter.Component != null)
                        targetParameter.Component.Parameters.CollectionChanged -= Component_Parameters_CollectionChanged;
                }

                foreach (var param in this.valuePointerParameters)
                    if (param.Value != null)
                        param.Value.PropertyChanged -= PointerParameter_PropertyChanged;
            }

            base.Dispose(isDisposing);
        }

        internal virtual void AttachEvents() { }

        internal virtual void DetachEvents() { }

        /// <inheritdoc/>
        protected override void NotifyValueChanged()
        {
            if (this.TargetParameter != null)
            {
                this.SetParamValue();
            }

            base.NotifyValueChanged();
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the MultiValuePointer class
        /// </summary>
        protected SimMultiValueParameterSource()
        {
            this.valuePointerParameters = new Dictionary<string, SimBaseParameter>();
            this.valuePointerParameterUnits = new Dictionary<string, string>();
        }


        /// <summary>
        /// Returns the value of this pointer.
        /// </summary>
        /// <returns></returns>
        public abstract object GetValue();

        /// <summary>
        /// Assigns values from a stored data to this pointer
        /// </summary>
        /// <param name="axisValueX">ValuePointer X-Value</param>
        /// <param name="axisValueY">ValuePointer Y-Value</param>
        /// <param name="axisValueZ">ValuePointer Z-Value</param>
        /// <param name="graphName">ValuePointer graph name</param>
        public abstract void SetFromParameters(double axisValueX, double axisValueY, double axisValueZ, string graphName);


        /// <summary>
        /// Returns true when the current valuepointer points to the same address as the other pointer
        /// </summary>
        /// <param name="other">The other pointer</param>
        /// <returns>True when both pointers point to the same target, otherwise False</returns>
        public abstract bool IsSamePointer(SimMultiValueParameterSource other);

        /// <summary>
        /// Creates the parameters used by the ValuePointer in the parent component.
        /// Uses the information provided by RegisterParameter
        /// </summary>
        /// <param name="user">The user who creates the parameters</param>
        public void CreateValuePointerParameters(SimUser user)
        {
            if (this.TargetParameter != null && this.TargetParameter.Component != null)
            {
                var comp = this.TargetParameter.Component;

                foreach (var paramEntry in this.valuePointerParameters.ToList())
                {
                    if (paramEntry.Value == null)
                    {
                        var param = new SimDoubleParameter(
                            string.Format(paramEntry.Key, this.TargetParameter.NameTaxonomyEntry.TextOrKey),
                            valuePointerParameterUnits[paramEntry.Key],
                            0.0, SimParameterOperations.EditValue);
                        comp.Parameters.Add(param);
                    }
                }
            }
        }
        /// <summary>
        /// Returns a parameter for a previously registered Pointer parameter (see RegisterParameter)
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <returns>Either a parameter when a parameter with this name exists, or Null when no such parameter exists</returns>
        protected SimBaseParameter GetValuePointerParameter(string name)
        {
            if (this.valuePointerParameters.TryGetValue(name, out var param))
                return param;
            return null;
        }


        /// <summary>
        /// Registers a Pointer parameter. Used by CreateValuePointerParameters and GetValuePointerParameter
        /// </summary>
        /// <param name="name">Name of the parameter. May contain {0} (replaced with the name of the parameter the pointer is attached to)</param>
        /// <param name="unit">Unit of the pointer parameter</param>
        protected void RegisterParameter(string name, string unit)
        {
            this.valuePointerParameters[name] = null;
            this.valuePointerParameterUnits[name] = unit;
        }

        private void ReplaceValuePointerParameter(string name, SimBaseParameter newValue)
        {
            if (valuePointerParameters.TryGetValue(name, out var oldValue))
            {
                if (oldValue != null)
                {
                    oldValue.PropertyChanged -= PointerParameter_PropertyChanged;
                    oldValue.AllowedOperations = SimParameterOperations.All;
                }

                if (newValue != null)
                {
                    newValue.PropertyChanged += PointerParameter_PropertyChanged;
                    newValue.AllowedOperations = SimParameterOperations.EditValue;
                }

                valuePointerParameters[name] = newValue;
                NotifyValueChanged();
            }
        }

        private void AttachAllPointerParameter()
        {
            if (TargetParameter != null && TargetParameter.Component != null)
            {
                foreach (var key in valuePointerParameters.Keys.ToList())
                {
                    var paramName = string.Format(key, TargetParameter.NameTaxonomyEntry.TextOrKey);
                    ReplaceValuePointerParameter(key, targetParameter.Component.Parameters
                        .OfType<SimBaseParameter>().FirstOrDefault(x => !x.NameTaxonomyEntry.HasTaxonomyEntry && x.NameTaxonomyEntry.Text == paramName));
                }
            }
        }
        private void DetachAllPointerParameter()
        {
            foreach (var key in valuePointerParameters.Keys.ToList())
                ReplaceValuePointerParameter(key, null);
        }

        #region EventHandler

        private void Component_Parameters_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var key in valuePointerParameters.Keys.ToList())
                    ReplaceValuePointerParameter(key, null);
            }
            else
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        var itemParam = (SimBaseParameter)item;
                        var key = valuePointerParameters.Keys.FirstOrDefault(x => string.Format(x, TargetParameter.NameTaxonomyEntry.TextOrKey) == itemParam.NameTaxonomyEntry.Text);
                        if (key != null)
                            ReplaceValuePointerParameter(key, itemParam);
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var itemParam = (SimBaseParameter)item;
                        var key = valuePointerParameters.Keys.FirstOrDefault(x => string.Format(x, TargetParameter.NameTaxonomyEntry.TextOrKey) == itemParam.NameTaxonomyEntry.Text);
                        if (key != null)
                            ReplaceValuePointerParameter(key, null);
                    }
                }
            }
        }

        private void PointerParameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimBaseParameter<dynamic>.Value))
                NotifyValueChanged();

        }

        #endregion

        internal override void OnParameterComponentChanged(SimComponent newComponent)
        {
            LastAttachedComponent = newComponent;

            if (newComponent != null)
            {
                this.SetParamValue();
            }
            else
                this.DetachEvents();
        }


        private void SetParamValue()
        {

            object gotValue = this.GetValue();
            switch (TargetParameter)
            {
                case SimDoubleParameter dParam:
                    if (gotValue is double dV)
                    {
                        dParam.Value = dV;
                    }
                    else
                    {
                        dParam.SetToNeutral();
                    }
                    break;
                case SimIntegerParameter iParam:
                    if (gotValue is int iV)
                    {
                        iParam.Value = iV;
                    }
                    else
                    {
                        iParam.SetToNeutral();
                    }
                    break;
                case SimStringParameter sParam:
                    if (gotValue is string sV)
                    {
                        sParam.Value = sV;
                    }
                    else
                    {
                        sParam.SetToNeutral();
                    }
                    break;
                case SimBoolParameter bParam:
                    if (gotValue is bool bV)
                    {
                        bParam.Value = bV;
                    }
                    else
                    {
                        bParam.SetToNeutral();
                    }
                    break;
            }
        }
    }
}
