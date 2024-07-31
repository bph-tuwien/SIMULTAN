using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    public static class ComponentParameters
    {
        #region PARAMETER GROUPING

        [Flags]
        internal enum AggregationPreprocessingState
        {
            OK = 0,
            DIMENSION_DISPARITY_SK_VEC = 1,
            DIMENSION_DISPARITY_VEC_MAT = 2,
            DIMENSION_CROWDING = 4
        }

        internal struct AggregationInfo
        {
            public AggregationPreprocessingState State { get; set; }
            public int NrParameters { get; set; }
            public int MaxNrRowsPerParameter { get; set; }
            public int MaxNrColsPerParameter { get; set; }
        }

        /// <summary>
        /// Aggregate parameter values in tables.
        /// If the alignment is set to horizontal: parameters are gathered column-wise and aligned horizontally. The aggregation function is performed horizontally.
        /// If the alignment is set to vertical:   parameters are gathered row-wise    and aligned vertically.   The aggregation function is performed vertically.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_value_factory">the current value manager</param>
        public static void PerformAggregation(this SimComponent _comp, SimMultiValueCollection _value_factory)
        {
            if (!_comp.InstanceType.HasFlag(SimInstanceType.Group) ||
                _comp.Factory == null || _value_factory == null)
                return;

            SimDoubleParameter p_orientation = _comp.Parameters.FirstOrDefault(x => x is SimDoubleParameter && x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_ORIENTATION_HRZ)) as SimDoubleParameter;
            if (p_orientation == null)
            {
                var taxEntry = _comp.Factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_ORIENTATION_HRZ);
                p_orientation = new SimDoubleParameter(ReservedParameterKeys.RP_ORIENTATION_HRZ, "-", 1.0)
                {
                    NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry))
                };
                p_orientation.IsAutomaticallyGenerated = true;
                p_orientation.ValueMin = 0.0;
                p_orientation.ValueMax = 1.0;
                p_orientation.Description = "Alignment is horizontal";
                p_orientation.Propagation = SimInfoFlow.Automatic;
                p_orientation.AllowedOperations = SimParameterOperations.EditValue;
                _comp.Parameters.Add(p_orientation);
            }

            SimDoubleParameter p_table = _comp.Parameters.FirstOrDefault(x => x is SimDoubleParameter && x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_TABLE_POINTER)) as SimDoubleParameter;
            if (p_table == null)
            {
                var taxEntry = _comp.Factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_TABLE_POINTER);
                p_table = new SimDoubleParameter(ReservedParameterKeys.RP_TABLE_POINTER, "-", 0.0)
                {
                    NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry))
                };
                p_table.IsAutomaticallyGenerated = true;
                p_table.ValueMin = double.MinValue;
                p_table.ValueMax = double.MaxValue;
                p_table.Description = "Tabellenverweis";
                p_table.Propagation = SimInfoFlow.Automatic;
                p_table.AllowedOperations = SimParameterOperations.None;
                _comp.Parameters.Add(p_table);
            }

            SimDoubleParameter p_aggreg_fct = _comp.Parameters.FirstOrDefault(x => x is SimDoubleParameter && x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_AGGREGATION_OPERATION)) as SimDoubleParameter;
            if (p_aggreg_fct == null)
            {
                var taxEntry = _comp.Factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_AGGREGATION_OPERATION);
                p_aggreg_fct = new SimDoubleParameter(ReservedParameterKeys.RP_AGGREGATION_OPERATION, "-", 0.0)
                {
                    NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry))
                };
                p_aggreg_fct.IsAutomaticallyGenerated = true;
                p_aggreg_fct.ValueMin = double.MinValue;
                p_aggreg_fct.ValueMax = double.MaxValue;
                p_aggreg_fct.Description = SimAggregationFunction.Sum.ToStringRepresentation();
                p_aggreg_fct.Propagation = SimInfoFlow.Automatic;
                p_aggreg_fct.AllowedOperations = SimParameterOperations.EditValue;
                _comp.Parameters.Add(p_aggreg_fct);
            }

            SimDoubleParameter p_label = _comp.Parameters.FirstOrDefault(x => x is SimDoubleParameter && x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_LABEL_SOURCE)) as SimDoubleParameter;
            if (p_label == null)
            {
                var taxEntry = _comp.Factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_LABEL_SOURCE);
                p_label = new SimDoubleParameter(ReservedParameterKeys.RP_LABEL_SOURCE, "-", 0.0)
                {
                    NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry))
                };
                p_label.IsAutomaticallyGenerated = true;
                p_label.ValueMin = double.MinValue;
                p_label.ValueMax = double.MaxValue;
                p_label.Description = "Label";
                p_label.Propagation = SimInfoFlow.Automatic;
                p_label.AllowedOperations = SimParameterOperations.EditValue;
                _comp.Parameters.Add(p_label);
            }

            List<KeyValuePair<string, List<SimDoubleParameter>>> to_aggregate = new List<KeyValuePair<string, List<SimDoubleParameter>>>();
            List<SimDoubleParameter> filter = _comp.Parameters.OfType<SimDoubleParameter>().Where(x => x is SimDoubleParameter &&
                 x != null &&
                !x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_ORIENTATION_HRZ) &&
                !x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_TABLE_POINTER) &&
                !x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_AGGREGATION_OPERATION) &&
                !x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_LABEL_SOURCE)).ToList();

            if (filter.Any())
            {

                if (_comp.Parent == null)
                    to_aggregate = _comp.Factory.GetAllCorrespondingParameters<SimDoubleParameter, double>(filter, p_label.Description, _comp.Id.LocalId);
                else
                    to_aggregate = _comp.Parent.Factory.GetAllCorrespondingParameters<SimDoubleParameter, double>(filter, p_label.Description, _comp.Id.LocalId);

            }
            if (!to_aggregate.Any())
                return;

            // aggregate
            bool orientation_hrz = p_orientation.Value == 1;
            string table_name = _comp.Name + " " + _comp.Description + " Aggregation";
            List<SimMultiValueBigTable> tables = Aggregate(table_name, to_aggregate, _value_factory, orientation_hrz);

            // apply aggregation function
            foreach (SimMultiValueBigTable bt in tables)
            {
                ApplyAggregationFunction(bt,
                    SimAggregationFunctionExtensions.FromStringRepresentation(p_aggreg_fct.Description), !orientation_hrz);
            }

            if (tables != null && tables.Count > 0)
            {
                // tables.ForEach(x => x.SetStandardPointer());
                if (tables.Count == 1)
                {
                    OverrideValueField(p_table, tables[0], _value_factory);
                    filter.ForEach(x => x.ValueSource = null);
                }
                else
                {
                    foreach (var fp in filter)
                    {
                        SimMultiValueBigTable corresponding = tables.FirstOrDefault(x => x.Name.Contains("[" + fp.NameTaxonomyEntry.TextOrKey + "]"));
                        if (corresponding != null)
                            OverrideValueField(fp, corresponding, _value_factory);
                        else
                            fp.ValueSource = null;
                    }
                }

            }
        }

        /// <summary>
        /// Collects parameters for value aggregation according to name and unit.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_filter">parameter whose name and unit act as a filter</param>
        /// <param name="_label_paramter_name">the name of the parameter supplying a label to the resulting collection</param>
        /// <param name="_excluded_id">id of a component to be excluded</param>
        /// <returns>key = component or parameter name, value = parameters of the same name that passed the filter</returns>
        private static List<KeyValuePair<string, List<T>>> GetAllCorrespondingParameters<T, NType>(
            this SimComponentCollection _factory, List<T> _filter, string _label_paramter_name = null, long _excluded_id = -1L)
            where T : SimBaseNumericParameter<NType>
        {
            var found = new List<KeyValuePair<string, List<T>>>();
            foreach (SimComponent c in _factory)
            {
                if (c.Id.LocalId == _excluded_id) continue;
                found.AddRange(c.GetAllCorrespondingParameters<T, NType>(_filter, _label_paramter_name, _excluded_id));
            }
            return found;
        }


        /// <summary>
        /// Replaces the value field (of type MultiValueBigTable) of the parameter's value pointer with a new one.
        /// </summary>
        /// <param name="parameter">The parameter for which the MultiValuePointer should be modified</param>
        /// <param name="table">the new value field of type MultiValueBigTable</param>
        /// <param name="multiValueFactory">the value manager</param>
        private static void OverrideValueField(SimBaseParameter parameter, SimMultiValueBigTable table, SimMultiValueCollection multiValueFactory)
        {
            if (parameter is SimDoubleParameter dParam)
            {
                if (dParam.ValueSource != null && dParam.ValueSource is SimMultiValueBigTableParameterSource ptr)
                {
                    var table_old = ptr.Table;
                    table_old.ReplaceData(table);
                    table_old.Name = table.Name;
                    multiValueFactory.Remove(table);
                }
                else
                {
                    dParam.ValueSource = table.CreateNewPointer();
                }
            }
            if (parameter is SimIntegerParameter intParam)
            {
                throw new NotImplementedException();
            }
            if (parameter is SimStringParameter strParam)
            {
                throw new NotImplementedException();
            }
            if (parameter is SimBoolParameter boolParam)
            {
                throw new NotImplementedException();
            }
            if (parameter is SimEnumParameter eParam)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Collects parameters for aggregation according to name.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_filter">the parameter whose name and unit act as a filter</param>
        /// <param name="_label_paramter_name">the name of the parameter supplying a label to the resulting collection</param>
        /// <param name="_excluded_id">the id of a component to be excluded from the collection</param>
        /// <returns>key = component or parameter name, value = parameters of the same name</returns>
        private static List<KeyValuePair<string, List<T>>> GetAllCorrespondingParameters<T, NType>(this SimComponent _comp,
            List<T> _filter, string _label_paramter_name = null, long _excluded_id = -1L)
             where T : SimBaseNumericParameter<NType>
        {
            List<KeyValuePair<string, List<T>>> found = new List<KeyValuePair<string, List<T>>>();
            if (_comp.Id.LocalId != _excluded_id && !_comp.InstanceType.HasFlag(SimInstanceType.Group))
            {
                var locally_found = _comp.Parameters.OfType<T>().Where(x =>
                    _filter.Any(y => y.NameTaxonomyEntry.Equals(x.NameTaxonomyEntry) && y.Unit == x.Unit)
                );

                List<T> locally_found_sorted = locally_found.OrderBy(x => x.NameTaxonomyEntry.TextOrKey ?? "").ToList();
                if (locally_found_sorted.Count > 0)
                {
                    string key = _comp.Name + " " + _comp.Description;
                    if (!string.IsNullOrEmpty(_label_paramter_name))
                    {
                        SimBaseParameter label_p = _comp.Parameters
                            .FirstOrDefault(x => x != null && x.NameTaxonomyEntry.TextOrKey == _label_paramter_name);
                        if (label_p != null)
                            key = label_p.Description;
                    }

                    found.Add(new KeyValuePair<string, List<T>>(key, locally_found_sorted));
                }
            }
            foreach (var entry in _comp.Components)
            {
                if (entry.Component == null) continue;
                found.AddRange(entry.Component.GetAllCorrespondingParameters<T, NType>(_filter, _label_paramter_name, _excluded_id));
            }

            return found;
        }

        private static List<SimMultiValueBigTable> Aggregate(string _table_name, List<KeyValuePair<string, List<SimDoubleParameter>>> _to_aggregate,
                                                    SimMultiValueCollection _factory, bool _orientation_hrz)
        {
            // check dimensions and consistency
            var ppstate = AggregationPreprocessingState.OK;

            List<KeyValuePair<string, AggregationInfo>> dimensions = new List<KeyValuePair<string, AggregationInfo>>();
            foreach (var entry in _to_aggregate)
            {
                var ps = entry.Value;
                var ps_non_skalar = ps.Where(x => x.ValueSource != null && x.ValueSource is SimMultiValueBigTableParameterSource ptr);

                AggregationPreprocessingState p_ppstate = AggregationPreprocessingState.OK;
                if (ps.Count() != ps_non_skalar.Count() && ps_non_skalar.Count() > 0)
                    p_ppstate |= AggregationPreprocessingState.DIMENSION_DISPARITY_SK_VEC;

                List<int> ps_dim_rows = new List<int>(ps.Count);
                List<int> ps_dim_cols = new List<int>(ps.Count);

                foreach (var p in ps)
                {
                    if (p.ValueSource != null && p.ValueSource is SimMultiValueBigTableParameterSource vpTablePtr)
                    {
                        var vpTable = vpTablePtr.Table;
                        ps_dim_rows.Add(vpTable.Count(0));
                        ps_dim_cols.Add(vpTable.Count(1));
                    }
                    else
                    {
                        ps_dim_rows.Add(1);
                        ps_dim_cols.Add(1);
                    }
                }

                int ps_dim_rows_unique = ps_dim_rows.GroupBy(x => x).Count();
                int ps_dim_cols_unique = ps_dim_cols.GroupBy(x => x).Count();

                if (ps_dim_rows_unique > 1 && !_orientation_hrz)
                    p_ppstate |= AggregationPreprocessingState.DIMENSION_CROWDING;
                if (ps_dim_cols_unique > 1 && _orientation_hrz)
                    p_ppstate |= AggregationPreprocessingState.DIMENSION_CROWDING;

                if (ps.Count == 1 && ps_non_skalar.Count() == 1)
                    p_ppstate |= AggregationPreprocessingState.DIMENSION_CROWDING;

                AggregationInfo info = new AggregationInfo
                {
                    State = p_ppstate,
                    NrParameters = ps.Count(),
                    MaxNrRowsPerParameter = ps_dim_rows.Max(),
                    MaxNrColsPerParameter = ps_dim_cols.Max()
                };
                dimensions.Add(new KeyValuePair<string, AggregationInfo>(entry.Key, info));
            }

            // aggregate
            List<string> names = new List<string>();
            List<string> units = new List<string>();
            List<List<object>> values = new List<List<object>>();
            List<string> row_names = new List<string>();

            ppstate = dimensions.Select(x => x.Value.State).Aggregate((x, y) => x | y);
            var parameter_lists = _to_aggregate.Select(x => x.Value).ToList();
            var aligned_parameter_lists = AlignLists<SimDoubleParameter>(parameter_lists);

            if (ppstate == AggregationPreprocessingState.OK)
            {
                // aggregate in one table               
                if (_orientation_hrz)
                {
                    // components in the rows, parameters in the columns
                    names.AddRange(Enumerable.Repeat<string>(string.Empty, aligned_parameter_lists[0].Count));
                    units.AddRange(Enumerable.Repeat<string>(string.Empty, aligned_parameter_lists[0].Count));
                    for (int i = 0; i < _to_aggregate.Count; i++)
                    {
                        row_names.Add(_to_aggregate[i].Key);
                        List<object> row_values = aligned_parameter_lists[i].Select(x => (x == null) ? double.NaN : x.Value).OfType<object>().ToList();
                        values.Add(row_values);

                        List<string> col_names = aligned_parameter_lists[i].Select(x => (x == null) ? string.Empty : x.NameTaxonomyEntry.TextOrKey).ToList();
                        List<string> col_units = aligned_parameter_lists[i].Select(x => (x == null) ? string.Empty : x.Unit).ToList();
                        for (int col = 0; col < col_names.Count; col++)
                        {
                            if (string.IsNullOrEmpty(names[col]) && !string.IsNullOrEmpty(col_names[col]))
                                names[col] = col_names[col];
                            if (string.IsNullOrEmpty(units[col]) && !string.IsNullOrEmpty(col_units[col]))
                                units[col] = col_units[col];
                        }
                    }

                    // assemble the resulting table
                    var column_headers = names.Zip(units, (x, y) => new SimMultiValueBigTableHeader(x, y)).ToList();
                    var row_headers = row_names.Select(x => new SimMultiValueBigTableHeader(x, "-")).ToList();
                    SimMultiValueBigTable table = new SimMultiValueBigTable(_table_name, units[0], "-", column_headers, row_headers, values);
                    _factory.Add(table);
                    return new List<SimMultiValueBigTable> { table };
                }
                else
                {
                    // parameters in the rows, components in the columns
                    names.AddRange(_to_aggregate.Select(x => x.Key));
                    units.AddRange(Enumerable.Repeat<string>("-", _to_aggregate.Count));
                    row_names.AddRange(Enumerable.Repeat(string.Empty, aligned_parameter_lists[0].Count));
                    for (int i = 0; i < _to_aggregate.Count; i++)
                    {
                        List<object> row_values = aligned_parameter_lists[i].Select(x => (x == null) ? double.NaN : x.Value).OfType<object>().ToList();
                        values.Add(row_values);

                        List<string> row_names_i = aligned_parameter_lists[i].Select(x => (x == null) ? string.Empty : x.NameTaxonomyEntry.TextOrKey).ToList();
                        for (int col = 0; col < row_names_i.Count; col++)
                        {
                            if (string.IsNullOrEmpty(row_names[col]) && !string.IsNullOrEmpty(row_names_i[col]))
                                row_names[col] = row_names_i[col];
                        }
                    }
                    values = values.Transpose();

                    // assemble the resulting table
                    var column_headers = names.Zip(units, (x, y) => new SimMultiValueBigTableHeader(x, y)).ToList();
                    var row_headers = row_names.Select(x => new SimMultiValueBigTableHeader(x, "-")).ToList();
                    SimMultiValueBigTable table = new SimMultiValueBigTable(_table_name, units[0], "-", column_headers, row_headers, values);
                    _factory.Add(table);
                    return new List<SimMultiValueBigTable> { table };
                }
            }
            else
            {
                // aggregate in separate tables (per parameter)
                // NOTE: aggregation per component is the standard layout of the component list
                List<SimMultiValueBigTable> tables = new List<SimMultiValueBigTable>();
                for (int p = 0; p < aligned_parameter_lists[0].Count; p++)
                {
                    List<SimDoubleParameter> params_of_same_name = aligned_parameter_lists.Select(x => x[p]).ToList();
                    var params_name_entry = params_of_same_name.SkipWhile(x => x == null).Take(1).First().NameTaxonomyEntry;
                    string params_name = params_name_entry.TextOrKey;
                    string params_unit = params_of_same_name.SkipWhile(x => x == null).Take(1).First().Unit;
                    List<string> labels = _to_aggregate.Select(x => x.Key + ": " + params_name).ToList();

                    if (_orientation_hrz)
                    {
                        // component + parameter in the columns (oriented vertically), parameter values in the rows
                        values = ExtractValueVectors(params_of_same_name, false);
                        names = labels;
                        units = Enumerable.Repeat(params_unit, names.Count).ToList();

                        string final_table_name = _table_name + " [" + params_name + "] " + DateTime.Now.ToString();
                        var column_headers = names.Zip(units, (x, y) => new SimMultiValueBigTableHeader(x, y)).ToList();
                        var row_headers = values.Select(x => new SimMultiValueBigTableHeader("", "")).ToList();
                        SimMultiValueBigTable table = new SimMultiValueBigTable(final_table_name, units[0], "-", column_headers, row_headers, values);
                        _factory.Add(table);
                        if (table != null)
                            tables.Add(table);
                    }
                    else
                    {
                        // component + parameters in the rows (oriented horizontally), parameter values in the columns
                        values = ExtractValueVectors(params_of_same_name, true);
                        row_names = labels;
                        names = new List<string>(); // new List<string> { "Komponenten" };
                        names.AddRange(Enumerable.Range(1, values[0].Count).Select(x => "Column " + x.ToString()));
                        units = new List<string>(); // new List<string> { "-" };
                        units.AddRange(Enumerable.Repeat(params_unit, values[0].Count));

                        string final_table_name = _table_name + " [" + params_name + "] " + DateTime.Now.ToString();
                        var column_headers = names.Zip(units, (x, y) => new SimMultiValueBigTableHeader(x, y)).ToList();
                        var row_headers = row_names.Select(x => new SimMultiValueBigTableHeader(x, "-")).ToList();
                        SimMultiValueBigTable table = new SimMultiValueBigTable(final_table_name, units[0], "-", column_headers, row_headers, values);
                        _factory.Add(table);
                        if (table != null)
                            tables.Add(table);
                    }
                }
                return tables;
            }
        }

        /// <summary>
        /// Align parameters only with a specific type <see cref="SimBaseParameter"/>
        /// </summary>
        /// <typeparam name="T">The type of the parameter <see cref="SimBaseParameter"/></typeparam>
        /// <param name="_lists"></param>
        /// <returns>List of the parameters aligned</returns>
        private static IList<List<T>> AlignLists<T>(IList<List<T>> _lists) where T : SimBaseParameter
        {
            var aligned = new List<List<T>>();

            var all_names = new List<SimTaxonomyEntryOrString>();
            foreach (var entry in _lists)
            {
                aligned.Add(new List<T>());
                all_names.AddRange(entry.Select(x => x.NameTaxonomyEntry));
            }
            var unique_names = all_names.Distinct();

            foreach (var n in unique_names)
            {
                var n_lists = _lists.Select(x => x.SkipWhile(y => !y.NameTaxonomyEntry.Equals(n)).TakeWhile(y => y.NameTaxonomyEntry.Equals(n))).ToList();
                int max_list_length_for_n = n_lists.Select(x => x.Count()).Max();
                for (int k = 0; k < _lists.Count; k++)
                {
                    aligned[k].AddRange(n_lists[k]);
                    int padding_length = max_list_length_for_n - n_lists[k].Count();
                    var padding = Enumerable.Repeat<T>(null, padding_length);
                    aligned[k].AddRange(padding);
                }
            }

            return aligned;
        }


        private static List<List<object>> ExtractValueVectors(List<SimDoubleParameter> _params_aligned, bool _take_rows)
        {
            List<List<object>> params_values = new List<List<object>>();

            // get the values
            foreach (SimDoubleParameter p in _params_aligned)
            {
                if (p == null)
                {
                    params_values.Add(new List<object> { double.NaN });
                }
                else
                {
                    if (p.ValueSource == null)
                        params_values.Add(new List<object> { p.Value });
                    else
                    {

                        if (p.ValueSource is SimMultiValueBigTableParameterSource ptr)
                        {
                            SimMultiValueBigTable param_table = ptr.Table;
                            int col_index = ptr.Column;
                            int row_index = ptr.Row;

                            List<List<object>> p_values = new List<List<object>>();
                            List<object> p_values_vector = new List<object>();
                            if (_take_rows)
                            {
                                p_values = param_table.GetRange(new SimPoint4D(row_index + 1, row_index + 1, 1, param_table.Count(1)));
                                p_values_vector = p_values[0];
                                if (p_values_vector.Count < param_table.Count(1))
                                {
                                    // table orientation not suitable -> get transposed range
                                    p_values = param_table.GetRange(new SimPoint4D(1, param_table.Count(0), col_index + 1, col_index + 1));
                                    p_values_vector = p_values.Select(x => x[0]).ToList();
                                }
                            }
                            else
                            {
                                p_values = param_table.GetRange(new SimPoint4D(1, param_table.Count(0), col_index + 1, col_index + 1));
                                p_values_vector = p_values.Select(x => x[0]).ToList();
                                if (p_values_vector.Count < param_table.Count(0))
                                {
                                    // table orientation not suitable -> get transposed range
                                    p_values = param_table.GetRange(new SimPoint4D(row_index + 1, row_index + 1, 1, param_table.Count(1)));
                                    p_values_vector = p_values[0];
                                }
                            }
                            params_values.Add(p_values_vector);
                        }
                        else
                            params_values.Add(new List<object> { p.Value });
                    }
                }
            }

            // align the values
            int max_length = params_values.Select(x => x.Count).Max();
            for (int i = 0; i < params_values.Count; i++)
            {
                int padding_langth = max_length - params_values[i].Count;
                var padding = Enumerable.Repeat<object>(double.NaN, padding_langth);
                params_values[i].AddRange(padding);
            }

            if (!_take_rows)
                params_values = params_values.Transpose();

            return params_values;
        }

        #endregion

        #region PARAMETER VALUE MANIPULATION BY REFERENCE

        /// <summary>
        /// Propagates the numeric and textual value of the given parameter to all corresponding
        /// parameters in components referencing the calling component.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_p">the parameter in the calling component whose value changed</param>
        internal static void PropagateRefParamValueFromClosestRef(this SimComponent _comp, SimBaseParameter _p)
        {
            using (AccessCheckingDisabler.Disable(_comp.Factory)) //Referencing parameters may be changed even if there is no write access
            {
                // follow references up the parent chain
                List<SimComponent> parent_chain = ComponentWalker.GetParents(_comp).ToList();

                List<SimComponent> comps_referencing_this_or_parent = new List<SimComponent>();
                foreach (SimComponent pC in parent_chain)
                {
                    foreach (var rpC in pC.ReferencedBy)
                    {
                        if (!(comps_referencing_this_or_parent.Contains(rpC.Owner)))
                            comps_referencing_this_or_parent.Add(rpC.Owner);
                    }
                }
                // look for referencing parameters
                foreach (SimComponent c in comps_referencing_this_or_parent)
                {
                    GetLocalParamsListWDirectRefsAndLimits(c, out var closest_source, out var closest_sourceMAX, out var closest_sourceMIN, true);

                    // direct reference
                    List<SimBaseParameter> to_synch = new List<SimBaseParameter>();
                    foreach (var x in closest_source)
                    {
                        if (x.Key.Propagation == SimInfoFlow.FromReference &&
                            x.Key.NameTaxonomyEntry.Equals(_p.NameTaxonomyEntry) &&
                            x.Value.Id.LocalId == _p.Id.LocalId)
                        {
                            if (x.Key is SimDoubleParameter dParam && dParam.ValueSource == null)
                            {
                                to_synch.Add(x.Key);
                            }
                            if (x.Key is SimIntegerParameter iParam && iParam.ValueSource == null)
                            {
                                to_synch.Add(x.Key);
                            }
                            if (x.Key is SimStringParameter sParam && sParam.ValueSource == null)
                            {
                                to_synch.Add(x.Key);
                            }
                            if (x.Key is SimBoolParameter bParam && bParam.ValueSource == null)
                            {
                                to_synch.Add(x.Key);
                            }
                        }

                    }
                    //List<SimBaseParameter> to_synch = closest_source.Where(x => x.Key.Propagation == SimInfoFlow.FromReference &&
                    //    ((SimBaseParameter<dynamic>)x.Key).ValueSource == null && x.Key.NameTaxonomyEntry.Name == _p.NameTaxonomyEntry.Name &&
                    //    x.Value.Id.LocalId == _p.Id.LocalId).Select(x => x.Key).ToList();


                    foreach (SimBaseParameter cP in to_synch)
                    {
                        PropagateParameterValueChange(cP, _p);
                    }


                    if (_p is SimBaseNumericParameter<ValueType>)
                    {
                        // reference as a minimum value
                        if (!_p.NameTaxonomyEntry.HasTaxonomyEntry && _p.NameTaxonomyEntry.Text.EndsWith("MIN"))
                        {
                            string p_name_only = _p.NameTaxonomyEntry.Text.Substring(0, _p.NameTaxonomyEntry.Text.Length - 3);
                            List<SimBaseParameter> to_synch_min = closest_sourceMIN.Where(x => x.Key.NameTaxonomyEntry.Text == p_name_only && x.Value.Id.LocalId == _p.Id.LocalId)
                                .Select(y => y.Key).ToList();
                            if (to_synch_min != null && to_synch_min.Count > 0)
                            {
                                foreach (SimBaseParameter cP in to_synch_min)
                                {
                                    if (_p is SimDoubleParameter doubleParam && cP is SimDoubleParameter doubleCp)
                                    {
                                        if (doubleCp.ValueMin != doubleParam.Value)
                                            doubleCp.ValueMin = doubleParam.Value;
                                    }
                                }
                            }
                        }
                        // reference as a maximum value
                        if (!_p.NameTaxonomyEntry.HasTaxonomyEntry && _p.NameTaxonomyEntry.Text.EndsWith("MAX"))
                        {
                            string p_name_only = _p.NameTaxonomyEntry.Text.Substring(0, _p.NameTaxonomyEntry.Text.Length - 3);
                            List<SimBaseParameter> to_synch_max = closest_sourceMAX.Where(x => x.Key.NameTaxonomyEntry.Text == p_name_only && x.Value.Id.LocalId == _p.Id.LocalId)
                                .Select(y => y.Key).ToList();
                            if (to_synch_max != null && to_synch_max.Count > 0)
                            {
                                foreach (SimBaseParameter cP in to_synch_max)
                                {
                                    if (_p is SimDoubleParameter doubleParam && cP is SimDoubleParameter doubleCp)
                                    {
                                        if (doubleCp.ValueMin != doubleParam.Value)
                                            doubleCp.ValueMin = doubleParam.Value;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        internal static void PropagateParameterValueChange(SimBaseParameter referencing, SimBaseParameter referenced)
        {
            if (referencing is SimDoubleParameter dParamReferencing && referenced is SimDoubleParameter dParamReferenced)
            {
                dParamReferencing.Value = dParamReferenced.Value;
                dParamReferencing.Description = dParamReferenced.Description;
            }
            else if (referencing is SimIntegerParameter iParamReferencing && referenced is SimIntegerParameter iParamReferenced)
            {
                iParamReferencing.Value = iParamReferenced.Value;
                iParamReferencing.Description = iParamReferenced.Description;
            }
            else if (referencing is SimStringParameter sParamReferencing && referenced is SimStringParameter sParamReferenced)
            {
                sParamReferencing.Value = sParamReferenced.Value;
                sParamReferencing.Description = sParamReferenced.Description;
            }
            else if (referencing is SimBoolParameter bParamReferencing && referenced is SimBoolParameter bParamReferenced)
            {
                bParamReferencing.Value = bParamReferenced.Value;
                bParamReferencing.Description = bParamReferenced.Description;
            }
            else if (referencing is SimEnumParameter eParamReferencing && referenced is SimEnumParameter eParamReferenced)
            {
                eParamReferencing.Value = eParamReferenced.Value;
                eParamReferencing.Description = eParamReferenced.Description;
            }
            else
            {
                throw new InvalidOperationException(referencing.GetType().ToString() + " type does not match type " + referenced.GetType().ToString());
            }

        }

        /// <summary>
        /// Propagates the numeric and textual value of all parameters in the added referenced component
        /// to all corresponding parameters in components referencing the calling component.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_rComp">the just added referenced component</param>
        internal static void PropagateRefParamValueToClosest(this SimComponent _comp, SimComponent _rComp)
        {
            if (_rComp == null) return;

            Dictionary<SimBaseParameter, SimBaseParameter> closest_source = new Dictionary<SimBaseParameter, SimBaseParameter>();
            Dictionary<SimBaseParameter, SimBaseParameter> closest_sourceMIN = new Dictionary<SimBaseParameter, SimBaseParameter>();
            Dictionary<SimBaseParameter, SimBaseParameter> closest_sourceMAX = new Dictionary<SimBaseParameter, SimBaseParameter>();
            GetLocalParamsListWDirectRefsAndLimits(_comp, out closest_source, out closest_sourceMAX, out closest_sourceMIN, true);


            // new
            foreach (var entry in closest_source)
            {
                if (entry.Key.GetType() == entry.Value.GetType()) //Same type
                {
                    entry.Key.ConvertValueFrom(entry.Value.Value);
                    entry.Key.Description = entry.Value.Description;
                }
            }
            foreach (var entry in closest_sourceMIN)
            {
                if (entry.Key.GetType() == entry.Value.GetType())
                {
                    ((dynamic)entry.Key).ValueMin = entry.Value.Value;
                }
            }
            foreach (var entry in closest_sourceMAX)
            {
                if (entry.Key.GetType() == entry.Value.GetType())
                {
                    ((dynamic)entry.Key).ValueMax = entry.Value.Value;
                }
            }
        }

        /// <summary>
        /// Gathers the parameters of the calling component and all its sub-components in three flat collections
        /// The key is the parameter, the value is the Parameter in a referenced component, that 
        /// serves as a value, upper or lower limit source respectively. The source component can be referenced by this or any of its parent components.
        /// If there is more than one candidate, chooses the component closest to the calling one in the referenced component's hierarchy.
        /// </summary>
        /// <param name="component">The component in which the search should be started</param>
        /// <param name="references">key = parameter of the calling component, value = corresponding value source parameter</param>
        /// <param name="upper_limits">key = parameter of the calling component, value = corresponding upper limit source parameter</param>
        /// <param name="lower_limits">key = parameter of the calling component, value = corresponding lower limit source parameter</param>
        /// <param name="_recursive">if true, gets a flat list for all subcomponents, if false - only for the calling component</param>
        private static void GetLocalParamsListWDirectRefsAndLimits(SimComponent component,
                                                             out Dictionary<SimBaseParameter, SimBaseParameter> references,
                                                             out Dictionary<SimBaseParameter, SimBaseParameter> upper_limits,
                                                             out Dictionary<SimBaseParameter, SimBaseParameter> lower_limits,
                                                             bool _recursive)
        {
            references = new Dictionary<SimBaseParameter, SimBaseParameter>();
            upper_limits = new Dictionary<SimBaseParameter, SimBaseParameter>();
            lower_limits = new Dictionary<SimBaseParameter, SimBaseParameter>();

            GetParamListWDirectRefsAndLimits_Optimized(component, references, upper_limits, lower_limits, _recursive);
        }


        private static void GetParamListWDirectRefsAndLimits_Optimized(SimComponent component,
                                                             Dictionary<SimBaseParameter, SimBaseParameter> references,
                                                             Dictionary<SimBaseParameter, SimBaseParameter> upper_limits,
                                                             Dictionary<SimBaseParameter, SimBaseParameter> lower_limits,
                                                             bool _recursive)
        {
            // find all components referenced by this (1st in the list) or its parents
            List<SimComponent> comps_referenced_by_this_or_parent = new List<SimComponent>();

            var currentComp = component;
            while (currentComp != null)
            {
                foreach (var entry in currentComp.ReferencedComponents)
                {
                    SimComponent rpC = entry.Target;
                    if (rpC == null) continue;

                    if (!(comps_referenced_by_this_or_parent.Contains(rpC)))
                        comps_referenced_by_this_or_parent.Add(rpC);
                }

                currentComp = currentComp.Parent;
            }

            // filter the parameters
            foreach (SimBaseParameter p in component.Parameters)
            {
                if (p == null) continue;
                if (p is SimDoubleParameter doubleParam)
                {
                    if (doubleParam.ValueSource != null) continue;
                }
                if (p is SimIntegerParameter intParam)
                {
                    if (intParam.ValueSource != null) continue;
                }
                if (p is SimBoolParameter boolParam)
                {
                    if (boolParam.ValueSource != null) continue;
                }
                if (p is SimStringParameter stringParameter)
                {
                    if (stringParameter.ValueSource != null) continue;
                }
                if (p is SimEnumParameter enumParameter)
                {
                    if (enumParameter.ValueSource != null) continue;
                }

                if (p.Propagation != SimInfoFlow.FromReference) continue;

                //if (p.Propagation == InfoFlow.REF_IN)
                {
                    SimComponent c_source_R = comps_referenced_by_this_or_parent.FirstOrDefault(x => x.Parameters.Where(y => y.NameTaxonomyEntry.Equals(p.NameTaxonomyEntry)).Count() > 0);
                    if (c_source_R != null)
                    {
                        SimBaseParameter p_source = c_source_R.Parameters.FirstOrDefault(x => x.NameTaxonomyEntry.Equals(p.NameTaxonomyEntry));
                        if (p_source != null)
                            references.Add(p, p_source);
                    }
                }

                //if (p.Propagation != InfoFlow.TYPE)
                if (!p.NameTaxonomyEntry.HasTaxonomyEntry) // can only compare names if they are only text
                {
                    string limit_key = "MAX";
                    SimComponent c_source_UL = comps_referenced_by_this_or_parent.FirstOrDefault(x => x.Parameters.Where(y => !y.NameTaxonomyEntry.HasTaxonomyEntry && y.NameTaxonomyEntry.Text.EndsWith(limit_key) && y.NameTaxonomyEntry.Text.Substring(0, y.NameTaxonomyEntry.Text.Length - limit_key.Length) == p.NameTaxonomyEntry.Text).Count() > 0);
                    if (c_source_UL != null)
                    {
                        SimBaseParameter p_source = c_source_UL.Parameters.FirstOrDefault(x => !x.NameTaxonomyEntry.HasTaxonomyEntry && x.NameTaxonomyEntry.Text.EndsWith(limit_key) && x.NameTaxonomyEntry.Text.Substring(0, x.NameTaxonomyEntry.Text.Length - limit_key.Length) == p.NameTaxonomyEntry.Text);
                        if (p_source != null)
                            upper_limits.Add(p, p_source);
                    }

                    limit_key = "MIN";
                    SimComponent c_source_LL = comps_referenced_by_this_or_parent.FirstOrDefault(x => x.Parameters.Where(y => !y.NameTaxonomyEntry.HasTaxonomyEntry && y.NameTaxonomyEntry.Text.EndsWith(limit_key) && y.NameTaxonomyEntry.Text.Substring(0, y.NameTaxonomyEntry.Text.Length - limit_key.Length) == p.NameTaxonomyEntry.Text).Count() > 0);
                    if (c_source_LL != null)
                    {
                        SimBaseParameter p_source = c_source_LL.Parameters.FirstOrDefault(x => !x.NameTaxonomyEntry.HasTaxonomyEntry && x.NameTaxonomyEntry.Text.EndsWith(limit_key) && x.NameTaxonomyEntry.Text.Substring(0, x.NameTaxonomyEntry.Text.Length - limit_key.Length) == p.NameTaxonomyEntry.Text);
                        if (p_source != null)
                            lower_limits.Add(p, p_source);
                    }
                }
            }

            // recursion
            if (_recursive)
            {
                foreach (var entry in component.Components.Where(x => x.Component != null))
                {
                    GetParamListWDirectRefsAndLimits_Optimized(entry.Component, references, upper_limits, lower_limits, _recursive);
                }
            }
        }

        #endregion

        #region Aggregation

        /// <summary>
        /// Applies an aggregation function to the table and replaces the data in this table by the result.
        /// </summary>
        /// <param name="table">The table on which the aggregation is performed</param>
        /// <param name="function">The function to apply</param>
        /// <param name="transpose">When set to False, the aggregation is performed to each columns. When set to True, the aggregation is applied to each rows</param>
        public static void ApplyAggregationFunction(SimMultiValueBigTable table, SimAggregationFunction function, bool transpose)
        {
            //Does not handle units correctly

            if (transpose) //Aggregate along rows
            {
                // extract named columns
                List<List<double>> columns = GetDoubles(table, true);

                List<KeyValuePair<string, List<double>>> named_columns = new List<KeyValuePair<string, List<double>>>();
                for (int i = 0; i < table.ColumnHeaders.Count; i++)
                {
                    named_columns.Add(new KeyValuePair<string, List<double>>(table.ColumnHeaders[i].Name, columns[i]));
                }
                // group named columns
                var groups = named_columns.GroupBy(x => x.Key).ToDictionary(gr => gr.Key, gr => gr.Select(x => x.Value).ToList());
                // aggregate rows
                var aggregation = ApplyAggregationTo(groups, function);


                var resultData = new List<List<object>>(aggregation.Count);

                foreach (var group in aggregation)
                {
                    resultData.Add(group.values.Cast<object>().ToList());
                }

                resultData = resultData.Transpose();

                var columnHeaders = new List<SimMultiValueBigTableHeader>(
                    aggregation.Select((x, xi) => new SimMultiValueBigTableHeader(
                        string.Format("{0} {1}", function.ToStringRepresentation(), x.key),
                        table.ColumnHeaders.First(ch => ch.Name == x.key).Unit)
                    ));

                table.ReplaceData(columnHeaders, table.RowHeaders, resultData);
            }
            else
            {
                // use rows
                List<KeyValuePair<string, List<double>>> named_rows = new List<KeyValuePair<string, List<double>>>();
                for (int i = 0; i < table.RowHeaders.Count; i++)
                {
                    named_rows.Add(new KeyValuePair<string, List<double>>(table.RowHeaders[i].Name, table.GetRow(i).Select(x => GetDoubles(x)).ToList()));
                }
                // group named rows
                Dictionary<string, List<List<double>>> groups = named_rows.GroupBy(x => x.Key).ToDictionary(gr => gr.Key, gr => gr.Select(x => x.Value).ToList());
                // aggregate rows
                var aggregation = ApplyAggregationTo(groups, function);
                var rowHeaders = new ObservableCollection<SimMultiValueBigTableHeader>(
                    aggregation.Select((x, xi) => new SimMultiValueBigTableHeader(
                        string.Format("{0} {1}", function.ToStringRepresentation(), x.key),
                        table.RowHeaders.First(rh => rh.Name == x.key).Unit))
                    );
                var resultData = aggregation.Select(x => x.values).ToList();

                var test = resultData.Select(x => x.Cast<object>().ToList()).ToList();
                table.ReplaceData(table.ColumnHeaders, rowHeaders, test, false);
            }
        }

        private static List<(string key, List<double> values)> ApplyAggregationTo(Dictionary<string, List<List<double>>> _input,
            SimAggregationFunction function, bool _order = true)
        {
            List<(string, List<double>)> result = new List<(string, List<double>)>(_input.Count);

            IEnumerable<KeyValuePair<string, List<List<double>>>> orderedInput = _input;
            if (_order)
                orderedInput = orderedInput.OrderBy(x => x.Key);

            foreach (var group in orderedInput)
            {
                List<double> groupResult = new List<double>();
                double[] row = new double[group.Value.Count];

                for (int r = 0; r < group.Value[0].Count; r++)
                {
                    for (int c = 0; c < group.Value.Count; c++)
                        row[c] = group.Value[c][r];

                    double rowResult = double.NaN;
                    switch (function)
                    {
                        case SimAggregationFunction.Sum:
                            rowResult = row.Sum();
                            break;
                        case SimAggregationFunction.Average:
                            rowResult = row.Sum() / row.Length;
                            break;
                        case SimAggregationFunction.Max:
                            rowResult = row.Max();
                            break;
                        case SimAggregationFunction.Min:
                            rowResult = row.Min();
                            break;
                        case SimAggregationFunction.Count:
                            rowResult = row.Length;
                            break;
                    }

                    groupResult.Add(rowResult);
                }

                result.Add((group.Key, groupResult));
            }

            return result;
        }

        private static List<List<double>> GetDoubles(SimMultiValueBigTable table, bool transpose)
        {
            List<List<double>> result;

            if (transpose)
            {
                result = new List<List<double>>(table.Count(1));

                for (int c = 0; c < table.Count(1); c++)
                {
                    List<double> row = new List<double>(table.Count(1));

                    for (int r = 0; r < table.Count(0); r++)
                        row.Add(GetDoubles(table[r, c]));

                    result.Add(row);
                }
            }
            else
            {
                result = new List<List<double>>(table.Count(0));

                for (int r = 0; r < table.Count(0); r++)
                {
                    List<double> row = new List<double>(table.Count(1));

                    for (int c = 0; c < table.Count(1); c++)
                        row.Add(GetDoubles(table[r, c]));

                    result.Add(row);
                }
            }

            return result;
        }
        private static double GetDoubles(object value)
        {
            if (value is double d)
                return d;
            if (value is int i)
                return i;
            else
                return double.NaN;
        }

        #endregion
    }
}
