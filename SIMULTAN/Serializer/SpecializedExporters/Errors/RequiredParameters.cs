using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.SpecializedExporters
{
    /// <summary>
    /// A class which holds the required parameters for the simulation export
    /// </summary>
    public class RequiredExportParameters
    {

        #region properties
        /// <summary>
        /// Parameters for the simulationParameter component 
        /// </summary>
        public List<string> SimulationParameters { get; set; }
        /// <summary>
        /// Parmeters which are needed for an edge 
        /// </summary>
        public List<string> EdgeParameters { get; set; }
        /// <summary>
        /// Parameters for nodes 
        /// </summary>
        public List<string> NodeParameters { get; set; }
        /// <summary>
        /// Name of the parameter which will be the wieght in the summ (should be asigned to an edge) 
        /// </summary>
        public string WeightParameter { get; set; }
        /// <summary>
        /// Name of the parameter which will be summarized during the export (should be assigned to a node) 
        /// </summary>
        public string SummarizedParameter { get; set; }
        #endregion


        #region .CTOR


        /// <summary>
        /// Constructor for the RequiredExportParameters
        /// </summary>
        public RequiredExportParameters(string summarizedParameter, string weightParameter,
            List<string> simulationParameters,
            List<string> edgeParameters,
            List<string> nodeParameters)
        {
            this.SimulationParameters = simulationParameters;
            this.NodeParameters = edgeParameters;
            this.NodeParameters = nodeParameters;
            this.SummarizedParameter = summarizedParameter;
            this.WeightParameter = weightParameter;

        }

        #endregion
    }
}
