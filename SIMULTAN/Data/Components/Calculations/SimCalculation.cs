using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Threading;

namespace SIMULTAN.Data.Components
{
    #region ENUMS

    /// <summary>
    /// The current state of a calculation
    /// </summary>
    [Flags]
    public enum SimCalculationValidity
    {
        /// <summary>
        /// A valid and working calculation
        /// </summary>
        Valid = 0,
        /// <summary>
        /// A variable is not bound to a parameter
        /// </summary>
        ParamNotBound = 1,
        /// <summary>
        /// The expression is invalid
        /// </summary>
        InvalidExpression = 4,
    }

    /// <summary>
    /// Describes how results of multiple iterations are aggregated
    /// </summary>
    public enum SimResultAggregationMethod
    {
        /// <summary>
        /// The average of all calculations is calculated
        /// </summary>
        Average,
        /// <summary>
        /// No aggregation happens, all results are stored
        /// </summary>
        Separate,
    }

    #endregion

    #region HELPER CLASSES

    /// <summary>
    /// Helper class for storing calculation information during loading
    /// </summary>
    internal class CalculationInitializationData
    {
        /// <summary>
        /// The local Id of the calculation
        /// </summary>
        public long LocalID { get; }

        /// <summary>
        /// The name of the calculation
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The expression of the calculation (may be valid or invalid)
        /// </summary>
        public string Expression { get; }

        /// <summary>
        /// Input parameters
        /// A dictionary matching variable symbols with parameters local ids
        /// </summary>
        public Dictionary<string, long?> InputParamsPreview { get; }
        /// <summary>
        /// Return parameters
        /// A dictionary matching variable symbols with parameters local ids
        /// </summary>
        public Dictionary<string, long?> ReturnParamsPreview { get; }

        /// <summary>
        /// In-Order list of all vector operations (only includes binary operations)
        /// </summary>
        public List<MultiValueCalculationBinaryOperation> VectorOperationList { get; }

        /// <summary>
        /// Meta data for variables
        /// Dictionary which matches variable symbol to meta data
        /// </summary>
        public IDictionary<string, CalculationParameterMetaData> MetaData { get; }

        /// <summary>
        /// The number of executions for MultiValue calculations
        /// </summary>
        public int NrExecutions { get; }

        /// <summary>
        /// Method used to aggregate multiple iterations in MultiValue calculations
        /// </summary>
        public SimResultAggregationMethod AggregationMethod { get; }

        /// <summary>
        /// When set to True, the calculation will override existing MultiValues
        /// </summary>
        public bool OverrideVectorResult { get; }

        /// <summary>
        /// Initializes a new instance of the CalculationPreview class
        /// </summary>
        /// <param name="localId">The local id</param>
        /// <param name="_name">The name of the calculation</param>
        /// <param name="_expression">The expression of the calculation (may be valid or invalid)</param>
        /// <param name="_input_params_preview">Input parameters. A dictionary matching variable symbols with parameters local ids</param>
        /// <param name="_return_params_preview">Return parameters. A dictionary matching variable symbols with parameters local ids</param>
        /// <param name="_vector_operation_list">In-Order list of all vector operations (only includes binary operations)</param>
        /// <param name="_metaData">Meta data for variables. Dictionary which matches variable symbol to meta data</param>
        /// <param name="_nr_executions">The number of executions for MultiValue calculations</param>
        /// <param name="aggregationMethod">Method used to aggregate multiple iterations in MultiValue calculations</param>
        /// <param name="_override_vector_result">When set to True, the calculation will override existing MultiValues</param>
        public CalculationInitializationData(long localId, string _name, string _expression,
                                    IDictionary<string, long?> _input_params_preview,
                                    IDictionary<string, long?> _return_params_preview,
                                    List<MultiValueCalculationBinaryOperation> _vector_operation_list,
                                    IDictionary<string, CalculationParameterMetaData> _metaData,
                                    int _nr_executions, SimResultAggregationMethod aggregationMethod, bool _override_vector_result)
        {
            this.LocalID = localId;
            this.Name = _name;
            this.Expression = _expression;

            if (_input_params_preview == null)
                this.InputParamsPreview = new Dictionary<string, long?>();
            else
                this.InputParamsPreview = new Dictionary<string, long?>(_input_params_preview);

            if (_return_params_preview == null)
                this.ReturnParamsPreview = new Dictionary<string, long?>();
            else
                this.ReturnParamsPreview = new Dictionary<string, long?>(_return_params_preview);

            if (_vector_operation_list == null || _vector_operation_list.Count == 0)
                this.VectorOperationList = null;
            else
                this.VectorOperationList = new List<MultiValueCalculationBinaryOperation>(_vector_operation_list);

            this.MetaData = _metaData;

            this.NrExecutions = _nr_executions;
            this.AggregationMethod = aggregationMethod;
            this.OverrideVectorResult = _override_vector_result;
        }
    }

    #endregion

    /// <summary>
    /// Allows the user to define calculations on parameters. The results are again written into parameters.
    /// The mathematic expression is given in the Expression property. Setting the property causes the a reevaluation of the expression tree.
    /// 
    /// Supports two modes: Scalar calculations and MultiValue calculations. Depending on the type, different operations are supported.
    /// For both types, all scalar expressions are supported which use either basic operations or functions from the .Net Math namespace.
    /// Constants from the Math namespace are also supported.
    /// To switch between the two modes, set the IsMultiValueCalculation property.
    /// 
    /// For special operations on MultiValues, see MultiValueCalculationUnaryOperation and MultiValueCalculationBinaryOperation.
    /// In addition, MultiValue calculations support multiple iterations and randomization on Parameters
    /// Parameter randomization is done by applying a normal distribute random number to the actual value. The parameter can be controlled by the
    /// parameter meta data.
    /// </summary>
    public partial class SimCalculation : SimObjectNew<SimComponentCollection>
    {
        //~Calculation() { Console.WriteLine("~Calculation"); }

        #region PROPERTIES

        private Func<Dictionary<string, double>, double> expressionFunction = null;
        private bool isExpressionValid = false; //Supresses State changes & Reorder calls

        /// <summary>
        /// The mathematical expression
        /// </summary>
        public string Expression
        {
            get { return this.expression; }
            set
            {
                if (this.expression != value)
                {
                    NotifyWriteAccess();
                    this.expression = value;

                    OnExpressionChanged();
                    this.NotifyPropertyChanged(nameof(Expression));
                    NotifyChanged();
                }
            }
        }
        private string expression;

        /// <summary>
        /// The component this calculation belongs to
        /// </summary>
        public SimComponent Component
        {
            get { return component; }
            internal set
            {
                if (component != value)
                {
                    component = value;
                    NotifyPropertyChanged(nameof(Component));
                }
            }
        }
        private SimComponent component;

        /// <summary>
        /// List of input parameters. Entries are added and removed automatically when the expression has been modified
        /// </summary>
        public SimCalculationInputParameterCollection InputParams { get; }
        /// <summary>
        /// List of input parameters. Entries have to be added and removed by the user
        /// </summary>
        public SimCalculationOutputParameterCollection ReturnParams { get; }

        /// <summary>
        /// The current state the calculation is in.
        /// </summary>
		public SimCalculationValidity State
        {
            get { return state; }
            private set
            {
                if (state != value)
                {
                    state = value;
                    NotifyPropertyChanged(nameof(State));
                }
            }
        }
        private SimCalculationValidity state;

        #endregion

        #region PROPERTIES: for vector calculations

        /// <summary>
        /// The expression tree for a MultiValue calculation. Automatically adjusted when the Expression is modified
        /// </summary>
        public SimMultiValueExpression MultiValueCalculation { get; private set; }

        /// <summary>
        /// Number of times a MultiValue calculation will be executed. Only useful when randomization is enabled.
        /// See ResultAggregation to control how the results of the executions should be treated.
        /// Minimum value: 1
        /// </summary>
        public int IterationCount
        {
            get { return this.nr_executions; }
            set
            {
                if (this.nr_executions != value)
                {
                    NotifyWriteAccess();
                    this.nr_executions = Math.Max(1, value);
                    NotifyPropertyChanged(nameof(IterationCount));
                    NotifyChanged();
                }
            }
        }
        private int nr_executions = 1;

        /// <summary>
        /// Specifies how results of multiple iterations should be treated in MultiValue calculations
        /// </summary>
        public SimResultAggregationMethod ResultAggregation
        {
            get { return multiIterationMode; }
            set
            {
                NotifyWriteAccess();
                if (multiIterationMode != value)
                {
                    multiIterationMode = value;
                    NotifyPropertyChanged(nameof(ResultAggregation));
                    NotifyChanged();
                }
            }
        }
        private SimResultAggregationMethod multiIterationMode;

        /// <summary>
        /// When set to True, the vector calculation should override the output ValueField.
        /// When set to False, a new ValueField should be created
        /// </summary>
        public bool OverrideResult
        {
            get { return overrideResult; }
            set
            {
                if (overrideResult != value)
                {
                    NotifyWriteAccess();
                    overrideResult = value;
                    NotifyPropertyChanged(nameof(OverrideResult));
                    NotifyChanged();
                }
            }
        }
        private bool overrideResult;

        /// <summary>
        /// When set to True, the calculation is executed as a Vector calculation.
        /// Setting this property to True parses the expression as a vector expression.
        /// </summary>
        public bool IsMultiValueCalculation
        {
            get { return isMultiValueCalculation; }
            set
            {
                if (isMultiValueCalculation != value)
                {
                    NotifyWriteAccess();
                    this.isMultiValueCalculation = value;
                    NotifyPropertyChanged(nameof(IsMultiValueCalculation));
                    OnExpressionChanged();
                    NotifyChanged();
                }
            }
        }
        private bool isMultiValueCalculation;

        #endregion

        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the Calculation class.
        /// Input parameters are only used when the expression makes use of it. Output parameters are always copied.
        /// </summary>
        /// <param name="expression">A mathematical expression with named parameters (e.g. (x + y/2)*3.5)</param>
        /// <param name="name">The name of the calculation</param>
        /// <param name="inputParameters">List of input parameters. The key has to match the variables in the expression</param>
        /// <param name="returnParameters">List of output / return parameters (each receives the same value)</param>
        public SimCalculation(string expression, string name, IDictionary<string, SimParameter> inputParameters = null,
                                                       IDictionary<string, SimParameter> returnParameters = null)
        {
            this.InputParams = new SimCalculationInputParameterCollection(this);
            this.ReturnParams = new SimCalculationOutputParameterCollection(this);
            Init(expression, name, inputParameters, returnParameters, null, 1, true, SimResultAggregationMethod.Average);
        }

        /// <summary>
        /// Initializes a new instance of the Calculation class
        /// </summary>
        /// <param name="localId">Local Id</param>
        /// <param name="expression">A mathematical expression with named parameters (e.g. (x + y/2)*3.5)</param>
        /// <param name="name">The name of the calculation</param>
        /// <param name="inputParameters">List of input parameters. The key has to match the variables in the expression</param>
        /// <param name="returnParameters">List of output / return parameters (each receives the same value)</param>
        /// <param name="metaData">The additional meta data for parameters. They actually only affect input parameters.</param>
        /// <param name="inOrderOperations">A list of operations which are used to restore the MultiValue expression tree.
        /// When this parameter is supplied, the whole calculation is treated as a MultiValue calculation.
        /// </param>
        /// <param name="iterationCount">Number of iterations for MultiValue calculations</param>
        /// <param name="overrideResult">When set to True, MultiValue calculations will override a pre-existing valuefield.</param>
        /// <param name="resultAggregation">Aggregation mode for multiple iterations</param>
        public SimCalculation(long localId, string expression, string name,
                           IDictionary<string, SimParameter> inputParameters,
                           IDictionary<string, SimParameter> returnParameters,
                           IDictionary<string, CalculationParameterMetaData> metaData,
                           List<MultiValueCalculationBinaryOperation> inOrderOperations,
                           int iterationCount, bool overrideResult, SimResultAggregationMethod resultAggregation)
            : base(new SimId(localId))
        {
            this.InputParams = new SimCalculationInputParameterCollection(this);
            this.ReturnParams = new SimCalculationOutputParameterCollection(this);
            Init(expression, name, inputParameters, returnParameters, metaData, iterationCount, overrideResult, resultAggregation);

            //Restore operations
            if (inOrderOperations != null && inOrderOperations.Count > 0)
            {
                this.IsMultiValueCalculation = true;
                if (this.MultiValueCalculation != null)
                {
                    int index = 0;
                    RestoreMultiValueOperations(this.MultiValueCalculation, inOrderOperations, ref index);
                }
            }
        }

        private void Init(string expression, string name,
            IDictionary<string, SimParameter> inputParameters, IDictionary<string, SimParameter> returnParameters,
            IDictionary<string, CalculationParameterMetaData> metaData,
            int iterationCount, bool overrideResult, SimResultAggregationMethod resultAggregation)
        {
            this.Name = name;
            this.IterationCount = iterationCount;
            this.OverrideResult = overrideResult;
            this.ResultAggregation = resultAggregation;

            if (expression == null)
                this.Expression = "";
            else
                this.Expression = expression;

            if (inputParameters != null)
            {
                foreach (var entry in inputParameters)
                    if (this.InputParams.ContainsKey(entry.Key))
                    {
                        this.InputParams[entry.Key] = entry.Value;
                        if (metaData != null && metaData.TryGetValue(entry.Key, out var entryMeta))
                            this.InputParams.GetMetaData(entry.Key).AssignFrom(entryMeta);
                    }
            }

            if (returnParameters != null)
                this.ReturnParams.AddRange(returnParameters);
        }

        private void RestoreMultiValueOperations(SimMultiValueExpression operand, List<MultiValueCalculationBinaryOperation> operations, ref int counter)
        {
            if (operand is SimMultiValueExpressionBinary step)
            {
                RestoreMultiValueOperations(step.Left, operations, ref counter);

                if (operations.Count > counter)
                    step.Operation = operations[counter];
                counter++;

                RestoreMultiValueOperations(step.Right, operations, ref counter);
            }
        }

        #endregion

        #region COPY .CTOR

        /// <summary>
        /// Initializes a new instance of the Calculation class while copying all settings from the original calculation
        /// </summary>
        /// <param name="_original"></param>
        public SimCalculation(SimCalculation _original)
        {
            this.InputParams = new SimCalculationInputParameterCollection(this);
            this.ReturnParams = new SimCalculationOutputParameterCollection(this);

            this.Name = _original.Name;
            this.Expression = _original.Expression;

            this.isMultiValueCalculation = _original.isMultiValueCalculation;
            this.MultiValueCalculation = _original.MultiValueCalculation;

            foreach (var entry in _original.InputParams)
            {
                if (this.InputParams.ContainsKey(entry.Key))
                {
                    this.InputParams[entry.Key] = entry.Value;
                    this.InputParams.GetMetaData(entry.Key).AssignFrom(_original.InputParams.GetMetaData(entry.Key));
                }
            }
            foreach (var entry in _original.ReturnParams)
            {
                this.ReturnParams.Add(entry.Key, entry.Value);
                this.ReturnParams.GetMetaData(entry.Key).AssignFrom(_original.ReturnParams.GetMetaData(entry.Key));
            }
        }

        #endregion

        #region UPDATE

        //Call this when the expression has changed
        private void OnExpressionChanged()
        {
            this.supressEvents = true; //Disable State & Reordering

            //Find out which parameters are used and if the expression compiles
            CalculationParser parser = new CalculationParser(CalculationParserFlags.FullOptimization);
            bool isParserValid = false;

            Expression<Func<Dictionary<string, double>, double>> expressionTree = null;

            try
            {
                expressionTree = parser.ParseFunction(expression);
                isParserValid = true;
            }
            catch (Exception)
            {
                this.expressionFunction = null;
            }

            //Update parameters
            if (isParserValid)
            {
                foreach (var item in this.InputParams.Where(x => !parser.Parameters.Contains(x.Key)).ToList())
                    InputParams.RemoveInternal(item.Key);

                foreach (var key in parser.Parameters)
                    if (!InputParams.ContainsKey(key))
                        InputParams.AddInternal(key, null);

                if (this.IsMultiValueCalculation) //Parse to MV Calculation
                {
                    try
                    {
                        this.MultiValueCalculation = MultiValueCalculationParser.Parse(expressionTree);
                        isExpressionValid = true;
                    }
                    catch (Exception) { isExpressionValid = false; }
                }
                else //Parse to normal calculation
                {
                    this.expressionFunction = expressionTree.Compile();
                    this.isExpressionValid = true;
                }
            }
            else
                this.isExpressionValid = false;


            this.supressEvents = false;
            UpdateState();
            NotifyComponentReordering();
        }

        private void UpdateState()
        {
            if (!supressEvents)
            {
                var newState = SimCalculationValidity.Valid;

                if (!isExpressionValid)
                    newState |= SimCalculationValidity.InvalidExpression;

                if (this.InputParams != null && this.ReturnParams != null && (
                    this.InputParams.Any(x => x.Value == null) || this.ReturnParams.All(x => x.Value == null)
                    ))
                    newState |= SimCalculationValidity.ParamNotBound;

                this.State = newState;
            }
        }

        private bool supressEvents = false;
        private void NotifyComponentReordering()
        {
            if (!supressEvents)
            {
                if (this.component != null)
                    CalculationAlgorithms.OrderCalculations(this.component);
            }
        }

        #endregion

        #region METHODS: Calculation

        /// <summary>
        /// Allows to specify localized names for tables created during a MultiValue calculations.
        /// </summary>
        /// <param name="calculation">The executed calculation</param>
        /// <param name="iteration">The current execution iteration</param>
        /// <returns></returns>
        public delegate string TableNameProviderDelegate(SimCalculation calculation, int iteration);

        /// <summary>
        /// Executes the calculation. Uses the parameters directly unless the parameter is found in parameterReplacements in which case 
        /// the replacement is used instead.
        /// </summary>
        /// <param name="valuefieldCollection">The MultiValue manager used to store results of MultiValue calculations. 
        /// May be set to Null when in scalar calculation mode</param>
        /// <param name="tableNameProvider">A method which returns the name of newly created <see cref="SimMultiValueBigTable"/>s during
        /// a vector calculation. May be used to localize table names.</param>
        /// <param name="tableNameAverageProvider">A method which returns the name of newly created <see cref="SimMultiValueBigTable"/>s during
        /// the averaging of vector calculation results. May be used to localize table names.</param>
        /// <param name="parameterReplacements">A list of replacements. Key is the parameter in the calculation, Value is the replacement</param>
        /// <param name="dispatcher">Dispatcher used to write back results. May be needed when the calculation is executed in a separate thread</param>
        public void Calculate(SimMultiValueCollection valuefieldCollection,
            TableNameProviderDelegate tableNameProvider = null, TableNameProviderDelegate tableNameAverageProvider = null,
            Dictionary<SimParameter, SimParameter> parameterReplacements = null,
            Dispatcher dispatcher = null)
        {
            if (this.IsMultiValueCalculation)
            {
                ExecuteAsVectorEquation(valuefieldCollection,
                    tableNameProvider, tableNameAverageProvider,
                    dispatcher);
            }
            else //Normal calculation
            {
                ExecuteAsNormalCalculation(parameterReplacements);
            }
        }

        /// <summary>
        /// Executes the calculation. Uses the parameters directly unless the parameter is found in parameterReplacements in which case 
        /// the replacement is used instead.
        /// Does not support Vector calculations
        /// </summary>
        /// <param name="parameterReplacements">A list of replacements. Key is the parameter in the calculation, Value is the replacement value</param>
        public void Calculate(Dictionary<SimParameter, double> parameterReplacements)
        {
            if (parameterReplacements == null)
                throw new ArgumentNullException(nameof(parameterReplacements));

            double result = double.NaN;

            if (State == SimCalculationValidity.Valid)
            {
                //Collect values
                Dictionary<string, double> parameterValues = new Dictionary<string, double>();
                foreach (var inputParam in this.InputParams)
                {
                    if (parameterReplacements.TryGetValue(inputParam.Value, out var replacement))
                        parameterValues.Add(inputParam.Key, replacement);
                    else
                        parameterValues.Add(inputParam.Key, inputParam.Value.ValueCurrent);
                }

                try
                {
                    if (expressionFunction != null)
                    {
                        result = expressionFunction(parameterValues);
                    }
                }
                catch
                {
                    result = double.NaN;
                }
            }

            foreach (var entry in this.ReturnParams.Where(x => x.Value != null))
            {
                if (parameterReplacements.ContainsKey(entry.Value))
                    parameterReplacements[entry.Value] = result;
            }
        }

        private void ExecuteAsVectorEquation(SimMultiValueCollection valuefieldCollection,
            TableNameProviderDelegate tableNameProvider = null, TableNameProviderDelegate tableNameAverageProvider = null,
            Dispatcher dispatcher = null)
        {
            if (this.Component == null || valuefieldCollection == null || this.ReturnParams.Count == 0) return;

            List<double[,]> allResults = new List<double[,]>(this.IterationCount);
            List<string> resultNames = new List<string>(this.IterationCount);

            // EXECUTE (multiple times makes sense only for values that are randomized each time before the calculation)
            for (int i = 0; i < this.IterationCount; i++)
            {
                var result = this.MultiValueCalculation.Calculate(this);
                allResults.Add(result);

                string name = tableNameProvider != null ? tableNameProvider(this, i) :
                    string.Format("{0} - {1} Result Iteration {2} ({3})", this.Component.Name, this.Name, i, DateTime.Now);
                resultNames.Add(name);
            }

            // CREATE TABLES from the execution results
            if (allResults[0].GetLength(0) == 0 || allResults[0].GetLength(1) == 0) return;

            var firstReturnParam = this.ReturnParams.First().Value;

            //Average (if necessary)
            if (this.ResultAggregation == SimResultAggregationMethod.Average && this.IterationCount > 1)
            {
                var avg_values = MultiValueCalculationsNEW.Average(allResults);
                allResults.Clear();
                allResults.Add(avg_values);
                resultNames.Clear();

                string name = null;

                if (tableNameAverageProvider != null)
                    name = tableNameAverageProvider(this, 0);
                else if (tableNameProvider != null)
                    name = tableNameProvider(this, 0);
                else
                    name = string.Format("{0} - {1} Result Averaged ({2})", this.Component.Name, this.Name, DateTime.Now);
                resultNames.Add(name);
            }

            //Store
            for (int i = 0; i < allResults.Count; ++i)
            {
                var result = allResults[i];
                var rowHeaders = Enumerable.Range(1, result.GetLength(0)).Select(x => new SimMultiValueBigTableHeader(
                    x.ToString(), string.Empty
                    )).ToList();
                var columnHeaders = Enumerable.Range(1, result.GetLength(1)).Select(x => new SimMultiValueBigTableHeader(
                    x.ToString(), string.Empty
                    )).ToList();

                SimMultiValueBigTable table = new SimMultiValueBigTable(resultNames[i], string.Empty,
                    string.Empty, columnHeaders, rowHeaders, result);

                Action createTablesAction = new Action(() =>
                {
                    valuefieldCollection.Add(table);

                    if (i == allResults.Count - 1) // assign last result to the return parameter  
                    {
                        foreach (var entry in this.ReturnParams)
                        {
                            SimParameter p = entry.Value;
                            if (p != null)
                                SimCalculation.AssignResultFromVectorCalc(p, table, valuefieldCollection, this.OverrideResult);
                        }
                    }
                });

                if (dispatcher != null)
                    dispatcher.Invoke(createTablesAction);
                else
                    createTablesAction();
            }
        }

        private void ExecuteAsNormalCalculation(Dictionary<SimParameter, SimParameter> parameterReplacements)
        {
            double result = double.NaN;

            if (State == SimCalculationValidity.Valid)
            {
                //Collect input data
                Dictionary<string, double> params_in_value = new Dictionary<string, double>();
                foreach (var entry in this.InputParams.Where(x => x.Value != null))
                {
                    var inputParam = entry.Value;

                    if (parameterReplacements != null && parameterReplacements.TryGetValue(entry.Value, out var replaceParam))
                        inputParam = replaceParam;

                    params_in_value.Add(entry.Key, inputParam.ValueCurrent);
                }

                try
                {
                    if (expressionFunction != null)
                    {
                        result = expressionFunction(params_in_value);
                    }
                }
                catch
                {
                    result = double.NaN;
                }
            }

            // assign the return value to all return parameters
            foreach (var entry in this.ReturnParams.Where(x => x.Value != null))
            {
                var param = entry.Value;
                if (parameterReplacements != null && parameterReplacements.TryGetValue(entry.Value, out var replacedParam))
                    param = replacedParam;

                if (param.MultiValuePointer != null)
                    param.MultiValuePointer = null;
                param.ValueCurrent = result;
            }
        }

        private static void AssignResultFromVectorCalc(SimParameter _p, SimMultiValueBigTable _table, SimMultiValueCollection _table_factory, bool _override_value_fields)
        {
            if (_override_value_fields)
            {
                if (_p.MultiValuePointer == null)
                    _p.MultiValuePointer = _table.DefaultPointer;
                else
                {
                    if (_p.MultiValuePointer is SimMultiValueBigTable.SimMultiValueBigTablePointer)
                    {
                        // perform the switch
                        SimMultiValueBigTable table_old = _p.MultiValuePointer.ValueField as SimMultiValueBigTable;

                        table_old.ReplaceData(_table);
                        table_old.Name = _table.Name;
                        _table_factory.Remove(_table);
                    }
                    else
                    {
                        _p.MultiValuePointer = _table.DefaultPointer;
                    }
                }
            }
            else
            {
                if (_p.MultiValuePointer != null && _p.MultiValuePointer is SimMultiValueBigTable.SimMultiValueBigTablePointer btp)
                    _p.MultiValuePointer = new SimMultiValueBigTable.SimMultiValueBigTablePointer(_table, btp.Row, btp.Column);
                else
                    _p.MultiValuePointer = _table.DefaultPointer;
            }
        }

        #endregion

        #region METHODS: To String

        /// <inheritdoc />
        [Obsolete]
        public override string ToString()
        {
            string output = this.Id + ": " + this.Name;

            output += " [ ";
            foreach (var entry in this.ReturnParams)
            {
                output += (entry.Value == null) ? entry.Key + " " : entry.Value.Name + " ";
            }
            output += "]= ";
            output += " {" + this.Expression + "} ";

            return output;
        }

        /// <summary>
        /// Serializes the calculation
        /// </summary>
        /// <param name="_sb">The output string builder</param>
        public void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.CALCULATION);                             // CALCULATION

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // general
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.Id.LocalId.ToString());

            _sb.AppendLine(((int)CalculationSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)CalculationSaveCode.EXPRESSION).ToString());
            _sb.AppendLine(this.Expression);

            // parameter: input -> saves only REFERENCES
            _sb.AppendLine(((int)CalculationSaveCode.PARAMS_INPUT).ToString());
            _sb.AppendLine(this.InputParams.Count.ToString());

            foreach (var entry in this.InputParams)
            {
                SimParameter p = entry.Value;

                _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                _sb.AppendLine(entry.Key);

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_REF).ToString());
                if (p == null)
                    _sb.AppendLine("NULL");
                else
                    _sb.AppendLine(p.Id.LocalId.ToString());
            }

            // parameter: output -> saves only REFERENCES
            _sb.AppendLine(((int)CalculationSaveCode.PARAMS_OUTPUT).ToString());
            _sb.AppendLine(this.ReturnParams.Count.ToString());

            foreach (var entry in this.ReturnParams)
            {
                SimParameter p = entry.Value;

                _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                _sb.AppendLine(entry.Key);

                _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_REF).ToString());
                if (p == null)
                    _sb.AppendLine("NULL");
                else
                    _sb.AppendLine(p.Id.LocalId.ToString());
            }

            string tmp;

            // ------------------------- vector calculations -------------------------------
            if (this.MultiValueCalculation != null)
            {
                List<MultiValueCalculationBinaryOperation> operations = new List<MultiValueCalculationBinaryOperation>();
                InOrderOperationSequence(this.MultiValueCalculation, operations);

                if (operations.Count > 0)
                {
                    _sb.AppendLine(((int)CalculationSaveCode.VECTOR_CALC_OPERATIONS).ToString());
                    _sb.AppendLine(operations.Count.ToString());
                    foreach (MultiValueCalculationBinaryOperation op in operations)
                    {
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                        _sb.AppendLine(((int)op).ToString());
                    }
                }

                if (this.InputParams.Count > 0)
                {
                    //Range
                    _sb.AppendLine(((int)CalculationSaveCode.VECTOR_CALC_RANGES).ToString());
                    _sb.AppendLine(this.InputParams.Count.ToString());

                    foreach (var symbol in this.InputParams)
                    {
                        var meta = this.InputParams.GetMetaData(symbol.Key);

                        _sb.AppendLine(((int)ParamStructCommonSaveCode.V5_VALUE).ToString());
                        _sb.AppendLine(symbol.Key);

                        _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString()); //Row Start
                        _sb.AppendLine(meta.Range.RowStart.ToString());
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.Y_VALUE).ToString()); //Row Count
                        _sb.AppendLine(meta.Range.RowCount.ToString());
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.Z_VALUE).ToString()); //Column Start
                        _sb.AppendLine(meta.Range.ColumnStart.ToString());
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.W_VALUE).ToString()); //Column Count
                        _sb.AppendLine(meta.Range.ColumnCount.ToString());
                    }

                    //Randomize
                    _sb.AppendLine(((int)CalculationSaveCode.VECTOR_CALC_RANDOM).ToString());
                    _sb.AppendLine(this.InputParams.Count.ToString());

                    foreach (var symbol in this.InputParams)
                    {
                        var meta = this.InputParams.GetMetaData(symbol.Key);

                        _sb.AppendLine(((int)ParamStructCommonSaveCode.V5_VALUE).ToString());
                        _sb.AppendLine(symbol.Key);

                        _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                        _sb.AppendLine(DXFDecoder.DoubleToString(meta.RandomizeRelativeMean, "F8")); //Mean
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.Y_VALUE).ToString());
                        _sb.AppendLine(DXFDecoder.DoubleToString(meta.RandomizeDeviation, "F8")); //Deviation
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.Z_VALUE).ToString());
                        _sb.AppendLine(((int)meta.RandomizeDeviationMode).ToString()); //Deviation Mode
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.W_VALUE).ToString());
                        _sb.AppendLine(meta.IsRandomized ? "1" : "0"); //IsRandomized
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.V6_VALUE).ToString());
                        _sb.AppendLine(meta.RandomizeIsClamping ? "1" : "0"); //IsClamping
                        _sb.AppendLine(((int)ParamStructCommonSaveCode.V7_VALUE).ToString());
                        _sb.AppendLine(DXFDecoder.DoubleToString(meta.RandomizeClampDeviation, "F8")); //ClampDeviation
                    }
                }
            }

            // nr of executions
            _sb.AppendLine(((int)CalculationSaveCode.VECTOR_CALC_NR_EXEC).ToString());
            _sb.AppendLine(this.IterationCount.ToString());

            // what to do with the results of multiple executions
            _sb.AppendLine(((int)CalculationSaveCode.VECTOR_CALC_AVERAGE).ToString());
            tmp = (this.ResultAggregation == SimResultAggregationMethod.Average) ? "1" : "0";
            _sb.AppendLine(tmp);
            _sb.AppendLine(((int)CalculationSaveCode.VECTOR_CALC_OVERRIDE).ToString());
            tmp = (this.OverrideResult) ? "1" : "0";
            _sb.AppendLine(tmp);
        }

        private void InOrderOperationSequence(SimMultiValueExpression operand, List<MultiValueCalculationBinaryOperation> result)
        {
            if (operand is SimMultiValueExpressionBinary step)
            {
                InOrderOperationSequence(step.Left, result);

                result.Add(step.Operation);

                InOrderOperationSequence(step.Right, result);
            }
        }

        #endregion

        /// <inheritdoc />
        protected override void NotifyWriteAccess()
        {
            if (this.Component != null)
                this.Component.RecordWriteAccess();
            base.NotifyWriteAccess();
        }
    }

}
