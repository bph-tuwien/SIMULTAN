﻿using SIMULTAN.Data.Components;
using SIMULTAN.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SIMULTAN.Data.Components.SimCalculation;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Stores the parameter binding for a variable. This class is never exposed to the user.
    /// Only used internally by the BaseCalculationParameterCollections
    /// </summary>
    public sealed class CalculationParameterReference : IDisposable
    {
        private WeakReference<BaseCalculationParameterCollections> owner;

        /// <summary>
        /// The parameter
        /// </summary>
        public SimParameter Parameter { get; }

        /// <summary>
        /// The meta data for this parameter
        /// </summary>
        public CalculationParameterMetaData MetaData { get; }

        /// <summary>
        /// Initializes a new instance of the CalculationParameterReference class
        /// </summary>
        /// <param name="calculation">The calculation this entry belongs to</param>
        /// <param name="parameter">The parameter referenced by the symbol</param>
        /// <param name="metaData">The meta data for this parameter</param>
        public CalculationParameterReference(BaseCalculationParameterCollections calculation, SimParameter parameter, CalculationParameterMetaData metaData = null)
        {
            if (calculation == null)
                throw new ArgumentNullException(nameof(calculation));

            this.owner = new WeakReference<BaseCalculationParameterCollections>(calculation);
            this.Parameter = parameter;

            if (metaData == null)
                metaData = new CalculationParameterMetaData();
            this.MetaData = metaData;

            if (this.Parameter != null)
            {
                this.Parameter.PropertyChanged += Parameter_PropertyChanged;
                this.Parameter.IsBeingDeleted += this.Parameter_IsBeingDeleted;
            }
        }

        private void Parameter_IsBeingDeleted(object sender)
        {
            //The last criteria prevents calculations from being modified when the parameter + the collection are removed at the same time
            if (this.owner.TryGetTarget(out var own) && own.Owner != null && own.Owner.Factory != null)
            {
                var key = own.GetKey(this);
                if (key != null)
                    own[key] = null; //Unbind. Do not remove entry
            }
        }

        private void Parameter_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Parameter.Propagation))
            {
                if (owner.TryGetTarget(out var target))
                {
                    try { target.CheckParameter((SimParameter)sender); }
                    catch (InvalidStateException ise)
                    {
                        throw new InvalidStateException("Invalid Calculation Parameter", ise);
                    }
                }
            }
        }

        private bool isDisposed = false;

        /// <inheritdoc />
        public void Dispose()
        {
            if (!isDisposed)
            {
                if (this.Parameter != null)
                {
                    this.Parameter.PropertyChanged -= Parameter_PropertyChanged;
                    this.Parameter.IsBeingDeleted -= Parameter_IsBeingDeleted;
                }
                isDisposed = true;
            }
        }

        internal CalculationParameterReference Clone(BaseCalculationParameterCollections targetCollection)
        {
            return new CalculationParameterReference(targetCollection, this.Parameter, this.MetaData);
        }
    }
}