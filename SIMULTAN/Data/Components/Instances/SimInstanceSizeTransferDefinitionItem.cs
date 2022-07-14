using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Defines how an instance size is calculated. Supports either size from user input, from parameter value or from instance path length
    /// </summary>
    public class SimInstanceSizeTransferDefinitionItem
    {
        private SimId loadingParameterId;
        private string loadingParameterName;

        /// <summary>
        /// Specifies which type of size transfer should happen
        /// </summary>
        public SimInstanceSizeTransferSource Source { get; }
        /// <summary>
        /// The parameter bound to this transfer item (only used when <see cref="Source"/> equals <see cref="SimInstanceSizeTransferSource.Parameter"/>)
        /// </summary>
        public SimParameter Parameter { get; private set; }
        /// <summary>
        /// A value which is added to the parameter value (only used when <see cref="Source"/> equals <see cref="SimInstanceSizeTransferSource.Parameter"/>) 
        /// </summary>
        public double Addend { get; }


        /// <summary>
        /// Initializes a new instance of the SimInstanceSizeTransferDefinitionItem class
        /// </summary>
        /// <param name="source">Defines which source should be used</param>
        /// <param name="parameter">The parameter to use</param>
        /// <param name="addend">Additive value which is added to the parameter value</param>
        public SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource source, SimParameter parameter, double addend)
        {
            this.Source = source;
            this.Parameter = parameter;
            this.Addend = addend;
        }

        /// <summary>
        /// Initializes a new instance of the InstanceSizeTransferDefinitionItem class.
        /// This constructor requires the <see cref="RestoreReferences(SimIdGenerator, SimComponentInstance)"/> method to be called afterwards
        /// 
        /// When the parameterId is empty, the parameterName is used. Otherwise the name is ignored 
        /// </summary>
        /// <param name="source">Defines which source should be used</param>
        /// <param name="parameterId">Id of the parameter. Gets restored afterwards</param>
        /// <param name="parameterName">Name of the parameter. Only used when parameterId is empty. This is a legacy parameter for very old project files</param>
        /// <param name="addend">Additive value which is added to the parameter value</param>
        internal SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource source, SimId parameterId, string parameterName, double addend)
        {
            this.Source = source;
            this.loadingParameterId = parameterId;
            this.loadingParameterName = parameterName;
            this.Addend = addend;
            this.Parameter = null;
        }

        /// <summary>
        /// Restores the reference to the parameter after loading. Either restores by Id (if available) or by name
        /// </summary>
        /// <param name="ids">The id generator for parameters</param>
        /// <param name="instance">The instance this item belongs to</param>
        public void RestoreReferences(SimIdGenerator ids, SimComponentInstance instance)
        {
            if (this.loadingParameterId != SimId.Empty)
            {
                this.Parameter = ids.GetById<SimParameter>(this.loadingParameterId);
            }
            else
            {
                if (loadingParameterName != null && instance.Component != null)
                {
                    var param = instance.Component.Parameters.FirstOrDefault(x => x.Name == loadingParameterName);
                    if (param != null)
                        Parameter = param;
                }
            }

            this.loadingParameterId = SimId.Empty;
            this.loadingParameterName = null;
        }

        /// <summary>
        /// Creates a copy of the current object
        /// </summary>
        /// <returns>A copy of the current object</returns>
        public SimInstanceSizeTransferDefinitionItem Clone()
        {
            return new SimInstanceSizeTransferDefinitionItem(Source, Parameter, Addend)
            {
                loadingParameterId = this.loadingParameterId,
                loadingParameterName = this.loadingParameterName,
            };
        }
    }
}
