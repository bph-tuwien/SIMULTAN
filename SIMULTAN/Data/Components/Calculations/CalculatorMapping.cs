using SIMULTAN.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Enumeration with different error cases for CalculatorMapping instances
    /// </summary>
    public enum CalculatorMappingErrors
    {
        /// <summary>
        /// No error
        /// </summary>
        NONE,
        /// <summary>
        /// The calculator component is the same as the data component
        /// </summary>
        SELF_REFERENCE,
        /// <summary>
        /// The calculator component doesn't contain a calculation
        /// </summary>
        NO_CALCULATION_FOUND,
        /// <summary>
        /// One of the parameters does not have the required propagation state. For example, an output parameter is set to input.
        /// </summary>
        INVALID_PARAMETER_PROPAGATION,
        /// <summary>
        /// There are only input mappings towards the calculator component defined, but the output is never read back.
        /// </summary>
        NO_OUTPUT_MAPPING,
    }

    /// <summary>
    /// Maps a data component's parameters to a calculator component, executes all calculations in the calculator component
    /// and copies the results back to the data component
    /// </summary>
    public class CalculatorMapping : INotifyPropertyChanged
    {
        #region Collections

        /// <summary>
        /// Stores a mapping of a Data parameter onto a Calculator parameter
        /// </summary>
        public struct MappingParameterTuple
        {
            /// <summary>
            /// The parameter in the data component (or in it's subtree)
            /// </summary>
            public SimDoubleParameter DataParameter { get; }
            /// <summary>
            /// The parameter in the calculator component (or in it's subtree)
            /// </summary>
            public SimDoubleParameter CalculatorParameter { get; }

            /// <summary>
            /// Initializes a new instance of the MappingParameterTuple class
            /// </summary>
            /// <param name="dataParameter">The parameter in the data component (or in it's subtree)</param>
            /// <param name="calculatorParameter">The parameter in the calculator component (or in it's subtree)</param>
            public MappingParameterTuple(SimDoubleParameter dataParameter, SimDoubleParameter calculatorParameter)
            {
                this.DataParameter = dataParameter;
                this.CalculatorParameter = calculatorParameter;
            }
        }

        /// <summary>
        /// Collection for input mappings. Throws an ArgumentException when mappings with null parameters are added
        /// </summary>
        public class InputParametersCollection : ObservableCollection<MappingParameterTuple>,
            IReadOnlyCollection<MappingParameterTuple>
        {
            /// <inheritdoc />
            protected override void InsertItem(int index, MappingParameterTuple item)
            {
                //Validate
                ValidateItem(item);
                base.InsertItem(index, item);
            }
            /// <inheritdoc />
            protected override void SetItem(int index, MappingParameterTuple item)
            {
                ValidateItem(item);
                base.SetItem(index, item);
            }

            /// <summary>
            /// Initializes a new instance of the InputParametersCollection class
            /// </summary>
            public InputParametersCollection()
            { }
            /// <summary>
            /// Initializes a new instance of the InputParametersCollection class
            /// </summary>
            /// <param name="collection">The initial items for the collection</param>
            public InputParametersCollection(IEnumerable<MappingParameterTuple> collection) : base(collection)
            {
                foreach (var item in collection)
                    ValidateItem(item);
            }

            private void ValidateItem(MappingParameterTuple item)
            {
                if (item.CalculatorParameter == null || item.DataParameter == null)
                    throw new ArgumentException("Mapping parameters may not contain null values");
            }
        };

        /// <summary>
        /// Collection for output mappings. Throws an ArgumentException when mappings with null parameters are added
        /// or when the parameter is not MIXED or OUTPUT.
        /// </summary>
        public class OutputParametersCollection : ObservableCollection<MappingParameterTuple>,
            IReadOnlyCollection<MappingParameterTuple>
        {
            /// <inheritdoc />
            protected override void InsertItem(int index, MappingParameterTuple item)
            {
                Validate(item);
                base.InsertItem(index, item);
            }
            /// <inheritdoc />
            protected override void SetItem(int index, MappingParameterTuple item)
            {
                Validate(item);
                base.SetItem(index, item);
            }

            /// <summary>
            /// Initializes a new instance of the OutputParametersCollection class
            /// </summary>
            public OutputParametersCollection() { }
            /// <summary>
            /// Initializes a new instance of the OutputParametersCollection class
            /// </summary>
            /// <param name="collection">The initial items for the collection</param>
            /// <param name="validate">When set to True, the inputed parameter mappings are validated</param>
            public OutputParametersCollection(IEnumerable<MappingParameterTuple> collection, bool validate) : base(collection)
            {
                if (validate)
                    foreach (var item in collection)
                        Validate(item);
            }

            private void Validate(MappingParameterTuple item)
            {
                if (item.CalculatorParameter == null || item.DataParameter == null)
                    throw new ArgumentException("Mapping parameters may not contain null values");
                if (!IsValidMappingPropagation(item))
                    throw new ArgumentException("Output mapping parameters have to have a Propagation of MIXED or OUTPUT.");
            }

            /// <summary>
            /// Returns True when the propagation of all parameters are valid.
            /// Returns True when both parameters are either in <see cref="SimInfoFlow.Mixed"/> or <see cref="SimInfoFlow.Output"/> mode.
            /// </summary>
            /// <param name="item">The mapping for which the propagation should be checked</param>
            /// <returns>True when the parameters have a valid propagation state, otherwise False</returns>
            public virtual bool IsValidMappingPropagation(MappingParameterTuple item)
            {
                return (item.CalculatorParameter.Propagation == SimInfoFlow.Mixed || item.CalculatorParameter.Propagation == SimInfoFlow.Output) &&
                    (item.DataParameter.Propagation == SimInfoFlow.Mixed || item.DataParameter.Propagation == SimInfoFlow.Output);
            }
        };

        private class OutputParametersCollectionPrivate : OutputParametersCollection
        {
            /// <summary>
            /// Initializes a new instance of the OutputParametersCollectionPrivate class
            /// </summary>
            public OutputParametersCollectionPrivate() { }

            /// <summary>
            /// Initializes a new instance of the OutputParametersCollectionPrivate class
            /// </summary>
            /// <param name="collection">The initial items for the collection</param>
            /// <param name="validate">When set to True, the inputed parameter mappings are validated</param>
            public OutputParametersCollectionPrivate(IEnumerable<MappingParameterTuple> collection, bool validate) : base(collection, validate) { }

            public bool ValidatePropagation { get; set; } = true;

            public override bool IsValidMappingPropagation(MappingParameterTuple item)
            {
                if (!ValidatePropagation)
                    return true;
                return base.IsValidMappingPropagation(item);
            }
        }

        #endregion

        #region PROPERTIES

        /// <summary>
        /// The name of the mapping
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set
            {
                if (this.name != value)
                {
                    var old_value = this.name;
                    this.name = value;
                    NotifyPropertyChanged(nameof(Name));
                }
            }
        }
        private string name;

        /// <summary>
        /// The calculator component
        /// </summary>
        public SimComponent Calculator
        {
            get { return this.calculator; }
            set
            {
                if (value != null && value != calculator)
                {
                    this.calculator = value;
                    this.parsingCalculatorID = SimId.Empty;
                    NotifyPropertyChanged(nameof(Calculator));
                }
            }
        }
        private SimComponent calculator;

        /// <summary>
        /// Stores all mappings from Data to Calculator
        /// </summary>
        public InputParametersCollection InputMapping { get; }
        /// <summary>
        /// Stores all mappings from Calculator to Mapping
        /// </summary>
        public OutputParametersCollection OutputMapping { get { return outputMapping; } }
        private OutputParametersCollectionPrivate outputMapping;

        private IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)> parsingInputMappings;
        private IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)> parsingOutputMapping;
        private SimId parsingCalculatorID;

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the CalculatorMapping class
        /// </summary>
        /// <param name="_name">The name of the mapping</param>
        /// <param name="_calculator">The calculator component</param>
        /// <param name="_input_mapping">A list of input mappings (parameters that are used instead of input parameters in the equation)</param>
        /// <param name="_output_mapping">A list of output mappings (parameters which are used instead of output parameters of the equation)</param>
        internal CalculatorMapping(string _name, SimComponent _calculator,
            IEnumerable<MappingParameterTuple> _input_mapping,
            IEnumerable<MappingParameterTuple> _output_mapping) : this(_name, _calculator, _input_mapping, _output_mapping, true)
        { }
        private CalculatorMapping(string _name, SimComponent _calculator,
            IEnumerable<MappingParameterTuple> _input_mapping,
            IEnumerable<MappingParameterTuple> _output_mapping, bool validateOutputMappings)
        {
            if (_name == null)
                throw new ArgumentNullException(nameof(_name));
            if (_calculator == null)
                throw new ArgumentNullException(nameof(_calculator));
            if (_input_mapping == null)
                throw new ArgumentNullException(nameof(_input_mapping));
            if (_output_mapping == null)
                throw new ArgumentNullException(nameof(_output_mapping));

            this.Name = _name;
            this.Calculator = _calculator;
            this.InputMapping = new InputParametersCollection(_input_mapping);
            this.outputMapping = new OutputParametersCollectionPrivate(_output_mapping, validateOutputMappings);
        }

        #endregion

        #region PARSING .CTOR

        /// <summary>
        /// Initializes a new instance of the CalculatorMapping class. May only be used by the DXF deserializer
        /// </summary>
        /// <param name="name">The name of the calculator mapping</param>
        /// <param name="calculatorId">The id of the calculator component. Will be resolved after loading</param>
        /// <param name="inputMapping">A list of id tuples of parameter mappings. Will be resolved after loading</param>
        /// <param name="outputMapping">A list of id tuples of parameter mappings. Will be resolved after loading</param>
        internal CalculatorMapping(string name, SimId calculatorId, IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)> inputMapping,
            IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)> outputMapping)
        {
            this.Name = name;

            this.parsingCalculatorID = calculatorId;
            this.parsingInputMappings = inputMapping;
            this.parsingOutputMapping = outputMapping;

            this.InputMapping = new InputParametersCollection();
            this.outputMapping = new OutputParametersCollectionPrivate();
        }

        #endregion


        /// <summary>
        /// Called when copying a component.
        /// </summary>
        /// <param name="_parameter_copy_record">key = id of Parameter in the original, value = Parameter in the copy</param>
        /// <returns>The copied calculator mapping</returns>
        internal CalculatorMapping ExchangeDataParameter(Dictionary<SimDoubleParameter, SimDoubleParameter> _parameter_copy_record)
        {
            if (_parameter_copy_record == null)
                throw new ArgumentNullException(nameof(_parameter_copy_record));

            List<MappingParameterTuple> inputMapping = new List<MappingParameterTuple>(this.InputMapping.Count);
            foreach (var entry in InputMapping)
            {
                if (_parameter_copy_record.TryGetValue(entry.DataParameter, out var newParam))
                    inputMapping.Add(new MappingParameterTuple(newParam, entry.CalculatorParameter));
                else
                    throw new ParameterNotFoundException(entry.DataParameter.Id.LocalId);
            }

            List<MappingParameterTuple> outputMapping = new List<MappingParameterTuple>(this.InputMapping.Count);
            foreach (var entry in OutputMapping)
            {
                if (_parameter_copy_record.TryGetValue(entry.DataParameter, out var newParam))
                    outputMapping.Add(new MappingParameterTuple(newParam, entry.CalculatorParameter));
                else
                    throw new ParameterNotFoundException(entry.DataParameter.Id.LocalId);
            }

            //Do not validate, otherwise copying an invalid mapped component would crash
            return new CalculatorMapping(this.Name, this.Calculator, inputMapping, outputMapping, false);
        }

        /// <summary>
        /// Performs the mapped calculation
        /// </summary>
        /// <param name="dataComponent">The data component for which the mapping should be performed</param>
        public void Evaluate(SimComponent dataComponent)
        {
            if (!GetErrors(dataComponent).Any())
            {
                Dictionary<SimDoubleParameter, SimDoubleParameter> parameterReplacements = new Dictionary<SimDoubleParameter, SimDoubleParameter>();
                foreach (var mapping in InputMapping)
                    parameterReplacements.Add(mapping.CalculatorParameter, mapping.DataParameter);
                foreach (var mapping in OutputMapping)
                    parameterReplacements.Add(mapping.CalculatorParameter, mapping.DataParameter);

                Calculator.ExecuteAllCalculationChains(null, null, parameterReplacements);
            }
        }

        /// <summary>
        /// Restores the parameters and the calculator component based on the id's stored in the loader variables
        /// </summary>
        /// <param name="dataComponent">The data component</param>
        internal void RestoreReferences(SimComponent dataComponent)
        {
            this.Calculator = dataComponent.Factory.ProjectData.IdGenerator.GetById<SimComponent>(this.parsingCalculatorID);

            if (this.Calculator != null)
            {
                this.Calculator.MappedToBy.Add(dataComponent);

                if (this.parsingInputMappings != null)
                {
                    foreach (var map in this.parsingInputMappings)
                    {
                        var dataParameterId = map.dataParameterId;
                        var calculatorParameterId = map.calculatorParameterId;

                        var dataParameter = dataComponent.Factory.ProjectData.IdGenerator.GetById<SimDoubleParameter>(dataParameterId);
                        var calcParameter = Calculator.Factory.ProjectData.IdGenerator.GetById<SimDoubleParameter>(calculatorParameterId);

                        if (dataParameter != null && calcParameter != null)
                        {
                            this.InputMapping.Add(new MappingParameterTuple(dataParameter, calcParameter));
                        }
                    }

                    this.parsingInputMappings = null;
                }

                this.outputMapping.ValidatePropagation = false;
                if (this.parsingOutputMapping != null)
                {
                    foreach (var map in this.parsingOutputMapping)
                    {
                        var dataParameterId = map.dataParameterId;
                        var calculatorParameterId = map.calculatorParameterId;

                        var dataParameter = dataComponent.Factory.ProjectData.IdGenerator.GetById<SimDoubleParameter>(dataParameterId);
                        var calcParameter = Calculator.Factory.ProjectData.IdGenerator.GetById<SimDoubleParameter>(calculatorParameterId);

                        if (dataParameter != null && calcParameter != null)
                        {
                            this.OutputMapping.Add(new MappingParameterTuple(dataParameter, calcParameter));
                        }
                    }

                    this.parsingOutputMapping = null;
                }
                this.outputMapping.ValidatePropagation = true;
            }
        }

        /// <summary>
        /// Checks the mapping and returns a list of potential problems (see <see cref="CalculatorMappingErrors"/>9
        /// </summary>
        /// <param name="dataComponent">The data component of the mapping</param>
        /// <returns>A list of problems with this mapping</returns>
        public IEnumerable<CalculatorMappingErrors> GetErrors(SimComponent dataComponent)
        {
            if (dataComponent == null)
                throw new ArgumentNullException(nameof(dataComponent));

            if (dataComponent == this.Calculator)
                yield return CalculatorMappingErrors.SELF_REFERENCE;
            if (!HasAnyCalculation(this.Calculator))
                yield return CalculatorMappingErrors.NO_CALCULATION_FOUND;
            if (this.OutputMapping.Any(x => !this.OutputMapping.IsValidMappingPropagation(x)))
                yield return CalculatorMappingErrors.INVALID_PARAMETER_PROPAGATION;
            if (!this.OutputMapping.Any())
                yield return CalculatorMappingErrors.NO_OUTPUT_MAPPING;
        }

        private bool HasAnyCalculation(SimComponent comp)
        {
            if (comp.Calculations.Count > 0)
                return true;

            return comp.Components.Where(x => x.Component != null).Any(x => HasAnyCalculation(x.Component));
        }
    }
}
