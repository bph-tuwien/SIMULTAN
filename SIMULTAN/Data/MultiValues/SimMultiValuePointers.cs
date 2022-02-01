using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Base class for all pointers into ValueFields
    /// SimMultiValuePointer are the connection between Parameters and ValueFields
    /// </summary>
    public abstract class SimMultiValuePointer : IDisposable
    {
        #region Properties

        private Dictionary<string, SimParameter> valuePointerParameters;
        private Dictionary<string, string> valuePointerParameterUnits;

        /// <summary>
        /// Stores the parameter to which the pointer is attached. Automatically set by the parameter class
        /// </summary>
        public SimParameter TargetParameter
        {
            get { return targetParameter; }
            set
            {
                if (targetParameter != null)
                {
                    targetParameter.PropertyChanged -= TargetParameter_PropertyChanged;
                }

                targetParameter = value;

                if (targetParameter != null)
                {
                    targetParameter.PropertyChanged += TargetParameter_PropertyChanged;

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
                    LastAttachedComponent = null;
                    DetachAllPointerParameter();

                    foreach (var key in valuePointerParameters.Keys.ToList())
                        ReplaceValuePointerParameter(key, null);
                }
            }
        }
        private SimParameter targetParameter = null;

        /// <summary>
        /// Stores the ValueField into which this pointer points
        /// </summary>
        public abstract SimMultiValue ValueField { get; set; }

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

        #region IDisposable

        /// <summary>
        /// Stores whether this instance has been disposed
        /// </summary>
        protected bool IsDisposed { get; private set; } = false;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }
        /// <summary>
        /// Called when the instance is disposed
        /// </summary>
        /// <param name="isDisposing">True when called from the Dispose() method, False when called from the finalizer</param>
        protected virtual void Dispose(bool isDisposing)
        {
            if (!IsDisposed)
            {
                if (this.targetParameter != null)
                {
                    targetParameter.PropertyChanged -= TargetParameter_PropertyChanged;

                    if (this.targetParameter.Component != null)
                        targetParameter.Component.Parameters.CollectionChanged -= Component_Parameters_CollectionChanged;
                }

                foreach (var param in this.valuePointerParameters)
                    if (param.Value != null)
                        param.Value.PropertyChanged -= PointerParameter_PropertyChanged;
            }

            IsDisposed = true;
        }

        internal virtual void AttachEvents() { }

        internal virtual void DetachEvents() { }

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the value of this pointer changes. 
        /// This can happen either because the addressing is changed or because the ValueField has changed.
        /// </summary>
        public event EventHandler ValueChanged;
        /// <summary>
        /// Invokes the ValueChanged event
        /// </summary>
        protected void NotifyValueChanged()
        {
            this.ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion


        /// <summary>
        /// Initializes a new instance of the MultiValuePointer class
        /// </summary>
        /// <param name="valueField">The value field this pointer belongs to</param>
        protected SimMultiValuePointer(SimMultiValue valueField)
        {
            if (valueField == null)
                throw new ArgumentNullException(nameof(valueField));

            this.ValueField = valueField;
            this.valuePointerParameters = new Dictionary<string, SimParameter>();
            this.valuePointerParameterUnits = new Dictionary<string, string>();
        }

        /// <summary>
        /// Returns the value of this pointer.
        /// </summary>
        /// <returns></returns>
        public abstract double GetValue();


        /// <summary>
        /// Creates a copy of this pointer
        /// </summary>
        /// <returns></returns>
        public abstract SimMultiValuePointer Clone();
        /// <summary>
        /// Stores the information of this pointer into a DXF file.
        /// Has to call the other AddToExport overload with proper values
        /// </summary>
        /// <param name="sb">The target stream</param>
        public abstract void AddToExport(ref StringBuilder sb);
        /// <summary>
        /// Writes structured data of this valuepointer to a DXF file. Should be used by implementations of the abstract AddToExport method
        /// </summary>
        /// <param name="sb">The target string</param>
        /// <param name="axisValueX">ValuePointer X-Value</param>
        /// <param name="axisValueY">ValuePointer Y-Value</param>
        /// <param name="axisValueZ">ValuePointer Z-Value</param>
        /// <param name="graphName">ValuePointer graph name</param>
        protected void AddToExport(ref StringBuilder sb, double axisValueX, double axisValueY, double axisValueZ, string graphName)
        {
            sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_MVLOCATION).ToString());
            sb.AppendLine(this.ValueField.Id.GlobalId.ToString());

            sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_MVID).ToString());
            sb.AppendLine(this.ValueField.Id.LocalId.ToString());

            sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_AXIS_VAL_X).ToString());
            sb.AppendLine(axisValueX.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_AXIS_VAL_Y).ToString());
            sb.AppendLine(axisValueY.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_AXIS_VAL_Z).ToString());
            sb.AppendLine(axisValueZ.ToString(CultureInfo.InvariantCulture));

            sb.AppendLine(((int)MultiValueSaveCode.MVDisplayVector_GRAPH_NAME).ToString());
            sb.AppendLine(graphName);
        }


        /// <summary>
        /// Assignes values from a stored data to this pointer
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
        public abstract bool IsSamePointer(SimMultiValuePointer other);

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
                        var param = new SimParameter(
                            string.Format(paramEntry.Key, this.TargetParameter.Name),
                            valuePointerParameterUnits[paramEntry.Key],
                            0.0, SimParameterOperations.EditValue);
                        comp.Parameters.Add(param);
                    }
                }
            }
        }
        /// <summary>
        /// Returns a parameter for a previousely registered Pointer parameter (see RegisterParameter)
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <returns>Either a parameter when a parameter with this name exists, or Null when no such parameter exists</returns>
        protected SimParameter GetValuePointerParameter(string name)
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

        private void ReplaceValuePointerParameter(string name, SimParameter newValue)
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
                    var paramName = string.Format(key, TargetParameter.Name);
                    ReplaceValuePointerParameter(key, targetParameter.Component.Parameters.FirstOrDefault(x => x.Name == paramName));
                }
            }
        }
        private void DetachAllPointerParameter()
        {
            foreach (var key in valuePointerParameters.Keys.ToList())
                ReplaceValuePointerParameter(key, null);
        }

        #region EventHandler

        private void TargetParameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SimParameter.Component):
                    LastAttachedComponent = TargetParameter.Component;
                    break;
            }
        }

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
                        var itemParam = (SimParameter)item;
                        var key = valuePointerParameters.Keys.FirstOrDefault(x => string.Format(x, TargetParameter.Name) == itemParam.Name);
                        if (key != null)
                            ReplaceValuePointerParameter(key, itemParam);
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var itemParam = (SimParameter)item;
                        var key = valuePointerParameters.Keys.FirstOrDefault(x => string.Format(x, TargetParameter.Name) == itemParam.Name);
                        if (key != null)
                            ReplaceValuePointerParameter(key, null);
                    }
                }
            }
        }

        private void PointerParameter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimParameter.ValueCurrent))
                NotifyValueChanged();
        }

        #endregion
    }
}
