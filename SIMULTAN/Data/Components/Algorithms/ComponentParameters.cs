using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
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
            if (_comp.InstanceType != SimInstanceType.Group ||
                _comp.Factory == null || _value_factory == null)
                return;

            SimParameter p_orientation = _comp.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_ORIENTATION_HRZ));
            if (p_orientation == null)
            {
                var taxEntry = ReservedParameterKeys.GetReservedTaxonomyEntry(_comp.Factory.ProjectData.Taxonomies, ReservedParameterKeys.RP_ORIENTATION_HRZ);
                p_orientation = new SimParameter(ReservedParameterKeys.RP_ORIENTATION_HRZ, "-", 1.0)
                {
                    TaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry))
                };
                p_orientation.IsAutomaticallyGenerated = true;
                p_orientation.ValueMin = 0.0;
                p_orientation.ValueMax = 1.0;
                p_orientation.TextValue = "Alignment is horizontal";
                p_orientation.Propagation = SimInfoFlow.Automatic;
                p_orientation.AllowedOperations = SimParameterOperations.EditValue;
                _comp.Parameters.Add(p_orientation);
            }

            SimParameter p_table = _comp.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_TABLE_POINTER));
            if (p_table == null)
            {
                var taxEntry = ReservedParameterKeys.GetReservedTaxonomyEntry(_comp.Factory.ProjectData.Taxonomies, ReservedParameterKeys.RP_TABLE_POINTER);
                p_table = new SimParameter(ReservedParameterKeys.RP_TABLE_POINTER, "-", 0.0)
                {
                    TaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry))
                };
                p_table.IsAutomaticallyGenerated = true;
                p_table.ValueMin = double.MinValue;
                p_table.ValueMax = double.MaxValue;
                p_table.TextValue = "Tabellenverweis";
                p_table.Propagation = SimInfoFlow.Automatic;
                p_table.AllowedOperations = SimParameterOperations.None;
                _comp.Parameters.Add(p_table);
            }

            SimParameter p_aggreg_fct = _comp.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_AGGREGATION_OPERATION));
            if (p_aggreg_fct == null)
            {
                var taxEntry = ReservedParameterKeys.GetReservedTaxonomyEntry(_comp.Factory.ProjectData.Taxonomies, ReservedParameterKeys.RP_AGGREGATION_OPERATION);
                p_aggreg_fct = new SimParameter(ReservedParameterKeys.RP_AGGREGATION_OPERATION, "-", 0.0)
                {
                    TaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry))
                };
                p_aggreg_fct.IsAutomaticallyGenerated = true;
                p_aggreg_fct.ValueMin = double.MinValue;
                p_aggreg_fct.ValueMax = double.MaxValue;
                p_aggreg_fct.TextValue = SimAggregationFunction.Sum.ToStringRepresentation();
                p_aggreg_fct.Propagation = SimInfoFlow.Automatic;
                p_aggreg_fct.AllowedOperations = SimParameterOperations.EditValue;
                _comp.Parameters.Add(p_aggreg_fct);
            }

            SimParameter p_label = _comp.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_LABEL_SOURCE));
            if (p_label == null)
            {
                var taxEntry = ReservedParameterKeys.GetReservedTaxonomyEntry(_comp.Factory.ProjectData.Taxonomies, ReservedParameterKeys.RP_LABEL_SOURCE);
                p_label = new SimParameter(ReservedParameterKeys.RP_LABEL_SOURCE, "-", 0.0)
                {
                    TaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry))
                };
                p_label.IsAutomaticallyGenerated = true;
                p_label.ValueMin = double.MinValue;
                p_label.ValueMax = double.MaxValue;
                p_label.TextValue = "Label";
                p_label.Propagation = SimInfoFlow.Automatic;
                p_label.AllowedOperations = SimParameterOperations.EditValue;
                _comp.Parameters.Add(p_label);
            }

            List<KeyValuePair<string, List<SimParameter>>> to_aggregate = new List<KeyValuePair<string, List<SimParameter>>>();
            List<SimParameter> filter = _comp.Parameters.Where(x => x != null && 
                x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_ORIENTATION_HRZ) &&
                x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_TABLE_POINTER) &&
                x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_AGGREGATION_OPERATION) &&
                x.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_LABEL_SOURCE)).ToList();

            if (filter.Count > 0)
            {
                if (_comp.Parent == null)
                    to_aggregate = _comp.Factory.GetAllCorrespondingParameters(filter, p_label.TextValue, _comp.Id.LocalId);
                else
                    to_aggregate = _comp.Parent.GetAllCorrespondingParameters(filter, p_label.TextValue, _comp.Id.LocalId);
            }
            if (to_aggregate.Count == 0)
                return;

            // aggregate
            bool orientation_hrz = p_orientation.ValueCurrent == 1;
            string table_name = _comp.Name + " " + _comp.Description + " Aggregation";
            List<SimMultiValueBigTable> tables = Aggregate(table_name, to_aggregate, _value_factory, orientation_hrz);

            // apply aggregation function
            foreach (SimMultiValueBigTable bt in tables)
            {
                bt.ApplyAggregationFunction(
                    SimAggregationFunctionExtensions.FromStringRepresentation(p_aggreg_fct.TextValue), !orientation_hrz);
            }

            if (tables != null && tables.Count > 0)
            {
                // tables.ForEach(x => x.SetStandardPointer());
                if (tables.Count == 1)
                {
                    OverrideValueField(p_table, tables[0], _value_factory);
                    filter.ForEach(x => x.MultiValuePointer = null);
                }
                else
                {
                    foreach (var fp in filter)
                    {
                        SimMultiValueBigTable corresponding = tables.FirstOrDefault(x => x.Name.Contains("[" + fp.TaxonomyEntry.Name + "]"));
                        if (corresponding != null)
                            OverrideValueField(fp, corresponding, _value_factory);
                        else
                            fp.MultiValuePointer = null;
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
        private static List<KeyValuePair<string, List<SimParameter>>> GetAllCorrespondingParameters(
            this SimComponentCollection _factory, List<SimParameter> _filter, string _label_paramter_name = null, long _excluded_id = -1L)
        {
            List<KeyValuePair<string, List<SimParameter>>> found = new List<KeyValuePair<string, List<SimParameter>>>();
            foreach (SimComponent c in _factory)
            {
                if (c.Id.LocalId == _excluded_id) continue;
                found.AddRange(c.GetAllCorrespondingParameters(_filter, _label_paramter_name, _excluded_id));
            }
            return found;
        }

        /// <summary>
        /// Replaces the value field (of type MultiValueBigTable) of the parameter's value pointer with a new one.
        /// </summary>
        /// <param name="parameter">The parameter for which the MultiValuePointer should be modified</param>
        /// <param name="table">the new value field of type MultiValueBigTable</param>
        /// <param name="multiValueFactory">the value manager</param>
        private static void OverrideValueField(SimParameter parameter, SimMultiValueBigTable table, SimMultiValueCollection multiValueFactory)
        {
            if (parameter.MultiValuePointer != null && parameter.MultiValuePointer.ValueField is SimMultiValueBigTable table_old)
            {
                table_old.ReplaceData(table);
                table_old.Name = table.Name;
                multiValueFactory.Remove(table);
            }
            else
            {
                parameter.MultiValuePointer = table.CreateNewPointer();
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
        private static List<KeyValuePair<string, List<SimParameter>>> GetAllCorrespondingParameters(this SimComponent _comp, List<SimParameter> _filter, string _label_paramter_name = null, long _excluded_id = -1L)
        {
            List<KeyValuePair<string, List<SimParameter>>> found = new List<KeyValuePair<string, List<SimParameter>>>();
            if (_comp.Id.LocalId != _excluded_id && _comp.InstanceType != SimInstanceType.Group)
            {
                var locally_found = _comp.Parameters.Where(x => _filter.FirstOrDefault(y => y.TaxonomyEntry.Equals(x.TaxonomyEntry) && y.Unit == x.Unit) != null);
                List<SimParameter> locally_found_sorted = locally_found.OrderBy(x => x.TaxonomyEntry.Name).ToList();
                if (locally_found_sorted.Count > 0)
                {
                    string key = _comp.Name + " " + _comp.Description;
                    if (!(string.IsNullOrEmpty(_label_paramter_name)))
                    {
                        SimParameter label_p = _comp.Parameters.FirstOrDefault(x => x != null && x.TaxonomyEntry.Name == _label_paramter_name);
                        if (label_p != null)
                            key = label_p.TextValue;
                    }

                    found.Add(new KeyValuePair<string, List<SimParameter>>(key, locally_found_sorted));
                }
            }
            foreach (var entry in _comp.Components)
            {
                if (entry.Component == null) continue;
                found.AddRange(entry.Component.GetAllCorrespondingParameters(_filter, _label_paramter_name, _excluded_id));
            }

            return found;
        }

        private static List<SimMultiValueBigTable> Aggregate(string _table_name, List<KeyValuePair<string, List<SimParameter>>> _to_aggregate,
                                                    SimMultiValueCollection _factory, bool _orientation_hrz)
        {
            // check dimensions and consistency
            var ppstate = AggregationPreprocessingState.OK;

            List<KeyValuePair<string, AggregationInfo>> dimensions = new List<KeyValuePair<string, AggregationInfo>>();
            foreach (var entry in _to_aggregate)
            {
                var ps = entry.Value;
                var ps_non_skalar = ps.Where(x => x.MultiValuePointer != null && x.MultiValuePointer.ValueField is SimMultiValueBigTable);

                AggregationPreprocessingState p_ppstate = AggregationPreprocessingState.OK;
                if (ps.Count() != ps_non_skalar.Count() && ps_non_skalar.Count() > 0)
                    p_ppstate |= AggregationPreprocessingState.DIMENSION_DISPARITY_SK_VEC;

                List<int> ps_dim_rows = new List<int>(ps.Count);
                List<int> ps_dim_cols = new List<int>(ps.Count);

                foreach (var p in ps)
                {
                    if (p.MultiValuePointer != null && p.MultiValuePointer.ValueField is SimMultiValueBigTable vpTable)
                    {
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
            List<List<double>> values = new List<List<double>>();
            List<string> row_names = new List<string>();

            ppstate = dimensions.Select(x => x.Value.State).Aggregate((x, y) => x | y);
            var parameter_lists = _to_aggregate.Select(x => x.Value).ToList();
            var aligned_parameter_lists = AlignLists(parameter_lists);

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
                        List<double> row_values = aligned_parameter_lists[i].Select(x => (x == null) ? double.NaN : x.ValueCurrent).ToList();
                        values.Add(row_values);

                        List<string> col_names = aligned_parameter_lists[i].Select(x => (x == null) ? string.Empty : x.TaxonomyEntry.Name).ToList();
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
                        List<double> row_values = aligned_parameter_lists[i].Select(x => (x == null) ? double.NaN : x.ValueCurrent).ToList();
                        values.Add(row_values);

                        List<string> row_names_i = aligned_parameter_lists[i].Select(x => (x == null) ? string.Empty : x.TaxonomyEntry.Name).ToList();
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
                    List<SimParameter> params_of_same_name = aligned_parameter_lists.Select(x => x[p]).ToList();
                    string params_name = params_of_same_name.SkipWhile(x => x == null).Take(1).First().TaxonomyEntry.Name;
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

        private static IList<List<SimParameter>> AlignLists(IList<List<SimParameter>> _lists)
        {
            IList<List<SimParameter>> aligned = new List<List<SimParameter>>();

            List<string> all_names = new List<string>();
            foreach (var entry in _lists)
            {
                aligned.Add(new List<SimParameter>());
                all_names.AddRange(entry.Select(x => x.TaxonomyEntry.Name));
            }
            List<string> unique_names = all_names.GroupBy(x => x).Select(gr => gr.First()).ToList();

            foreach (string n in unique_names)
            {
                var n_lists = _lists.Select(x => x.SkipWhile(y => y.TaxonomyEntry.Name != n).TakeWhile(y => y.TaxonomyEntry.Name == n)).ToList();
                int max_list_length_for_n = n_lists.Select(x => x.Count()).Max();
                for (int k = 0; k < _lists.Count; k++)
                {
                    aligned[k].AddRange(n_lists[k]);
                    int padding_length = max_list_length_for_n - n_lists[k].Count();
                    var padding = Enumerable.Repeat<SimParameter>(null, padding_length);
                    aligned[k].AddRange(padding);
                }
            }

            return aligned;
        }

        private static List<List<double>> ExtractValueVectors(List<SimParameter> _params_aligned, bool _take_rows)
        {
            List<List<double>> params_values = new List<List<double>>();

            // get the values
            foreach (SimParameter p in _params_aligned)
            {
                if (p == null)
                    params_values.Add(new List<double> { double.NaN });
                else
                {
                    if (p.MultiValuePointer == null)
                        params_values.Add(new List<double> { p.ValueCurrent });
                    else
                    {
                        if (p.MultiValuePointer is SimMultiValueBigTable.SimMultiValueBigTablePointer)
                        {
                            var mvp = (SimMultiValueBigTable.SimMultiValueBigTablePointer)p.MultiValuePointer;

                            SimMultiValueBigTable param_table = p.MultiValuePointer.ValueField as SimMultiValueBigTable;
                            int col_index = mvp.Column;
                            int row_index = mvp.Row;

                            List<List<double>> p_values = new List<List<double>>();
                            List<double> p_values_vector = new List<double>();
                            if (_take_rows)
                            {
                                p_values = param_table.GetRange(new System.Windows.Media.Media3D.Point4D(row_index + 1, row_index + 1, 1, param_table.Count(1)));
                                p_values_vector = p_values[0];
                                if (p_values_vector.Count < param_table.Count(1))
                                {
                                    // table orientation not suitable -> get transposed range
                                    p_values = param_table.GetRange(new System.Windows.Media.Media3D.Point4D(1, param_table.Count(0), col_index + 1, col_index + 1));
                                    p_values_vector = p_values.Select(x => x[0]).ToList();
                                }
                            }
                            else
                            {
                                p_values = param_table.GetRange(new System.Windows.Media.Media3D.Point4D(1, param_table.Count(0), col_index + 1, col_index + 1));
                                p_values_vector = p_values.Select(x => x[0]).ToList();
                                if (p_values_vector.Count < param_table.Count(0))
                                {
                                    // table orientation not suitable -> get transposed range
                                    p_values = param_table.GetRange(new System.Windows.Media.Media3D.Point4D(row_index + 1, row_index + 1, 1, param_table.Count(1)));
                                    p_values_vector = p_values[0];
                                }
                            }
                            params_values.Add(p_values_vector);
                        }
                        else
                            params_values.Add(new List<double> { p.ValueCurrent });
                    }
                }
            }

            // align the values
            int max_length = params_values.Select(x => x.Count).Max();
            for (int i = 0; i < params_values.Count; i++)
            {
                int padding_langth = max_length - params_values[i].Count;
                var padding = Enumerable.Repeat(double.NaN, padding_langth);
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
        internal static void PropagateRefParamValueFromClosestRef(this SimComponent _comp, SimParameter _p)
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
                    List<SimParameter> to_synch = closest_source.Where(x => x.Key.Propagation == SimInfoFlow.FromReference &&
                        x.Key.MultiValuePointer == null && x.Key.TaxonomyEntry.Name == _p.TaxonomyEntry.Name &&
                        x.Value.Id.LocalId == _p.Id.LocalId).Select(x => x.Key).ToList();

                    foreach (SimParameter cP in to_synch)
                    {
                        PropagateParameterValueChange(cP, _p);
                    }

                    // reference as a minimum value
                    if (_p.TaxonomyEntry.Name.EndsWith("MIN"))
                    {
                        string p_name_only = _p.TaxonomyEntry.Name.Substring(0, _p.TaxonomyEntry.Name.Length - 3);
                        List<SimParameter> to_synch_min = closest_sourceMIN.Where(x => x.Key.TaxonomyEntry.Name == p_name_only && x.Value.Id.LocalId == _p.Id.LocalId)
                            .Select(y => y.Key).ToList();
                        if (to_synch_min != null && to_synch_min.Count > 0)
                        {
                            foreach (SimParameter cP in to_synch_min)
                            {
                                if (cP.ValueMin != _p.ValueCurrent)
                                    cP.ValueMin = _p.ValueCurrent;
                            }
                        }
                    }

                    // reference as a maximum value
                    if (_p.TaxonomyEntry.Name.EndsWith("MAX"))
                    {
                        string p_name_only = _p.TaxonomyEntry.Name.Substring(0, _p.TaxonomyEntry.Name.Length - 3);
                        List<SimParameter> to_synch_max = closest_sourceMAX.Where(x => x.Key.TaxonomyEntry.Name == p_name_only && x.Value.Id.LocalId == _p.Id.LocalId)
                            .Select(y => y.Key).ToList();
                        if (to_synch_max != null && to_synch_max.Count > 0)
                        {
                            foreach (SimParameter cP in to_synch_max)
                            {
                                if (cP.ValueMax != _p.ValueCurrent)
                                    cP.ValueMax = _p.ValueCurrent;
                            }
                        }
                    }
                }
            }
        }

        internal static void PropagateParameterValueChange(SimParameter referencing, SimParameter referenced)
        {
            referencing.ValueCurrent = referenced.ValueCurrent;
            referencing.TextValue = referenced.TextValue;
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

            Dictionary<SimParameter, SimParameter> closest_source = new Dictionary<SimParameter, SimParameter>();
            Dictionary<SimParameter, SimParameter> closest_sourceMIN = new Dictionary<SimParameter, SimParameter>();
            Dictionary<SimParameter, SimParameter> closest_sourceMAX = new Dictionary<SimParameter, SimParameter>();
            GetLocalParamsListWDirectRefsAndLimits(_comp, out closest_source, out closest_sourceMAX, out closest_sourceMIN, true);

            // new
            foreach (var entry in closest_source)
            {
                entry.Key.ValueCurrent = entry.Value.ValueCurrent;
                entry.Key.TextValue = entry.Value.TextValue;
            }
            foreach (var entry in closest_sourceMIN)
            {
                entry.Key.ValueMin = entry.Value.ValueCurrent;
            }
            foreach (var entry in closest_sourceMAX)
            {
                entry.Key.ValueMax = entry.Value.ValueCurrent;
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
                                                             out Dictionary<SimParameter, SimParameter> references,
                                                             out Dictionary<SimParameter, SimParameter> upper_limits,
                                                             out Dictionary<SimParameter, SimParameter> lower_limits,
                                                             bool _recursive)
        {
            references = new Dictionary<SimParameter, SimParameter>();
            upper_limits = new Dictionary<SimParameter, SimParameter>();
            lower_limits = new Dictionary<SimParameter, SimParameter>();

            GetParamListWDirectRefsAndLimits_Optimized(component, references, upper_limits, lower_limits, _recursive);
        }


        private static void GetParamListWDirectRefsAndLimits_Optimized(SimComponent component,
                                                             Dictionary<SimParameter, SimParameter> references,
                                                             Dictionary<SimParameter, SimParameter> upper_limits,
                                                             Dictionary<SimParameter, SimParameter> lower_limits,
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
            foreach (SimParameter p in component.Parameters)
            {
                if (p == null) continue;
                if (p.MultiValuePointer != null) continue;
                if (p.Propagation != SimInfoFlow.FromReference) continue;

                //if (p.Propagation == InfoFlow.REF_IN)
                {
                    SimComponent c_source_R = comps_referenced_by_this_or_parent.FirstOrDefault(x => x.Parameters.Where(y => y.TaxonomyEntry.Name == p.TaxonomyEntry.Name).Count() > 0);
                    if (c_source_R != null)
                    {
                        SimParameter p_source = c_source_R.Parameters.FirstOrDefault(x => x.TaxonomyEntry.Name == p.TaxonomyEntry.Name);
                        if (p_source != null)
                            references.Add(p, p_source);
                    }
                }

                //if (p.Propagation != InfoFlow.TYPE)
                {
                    string limit_key = "MAX";
                    SimComponent c_source_UL = comps_referenced_by_this_or_parent.FirstOrDefault(x => x.Parameters.Where(y => y.TaxonomyEntry.Name.EndsWith(limit_key) && y.TaxonomyEntry.Name.Substring(0, y.TaxonomyEntry.Name.Length - limit_key.Length) == p.TaxonomyEntry.Name).Count() > 0);
                    if (c_source_UL != null)
                    {
                        SimParameter p_source = c_source_UL.Parameters.FirstOrDefault(x => x.TaxonomyEntry.Name.EndsWith(limit_key) && x.TaxonomyEntry.Name.Substring(0, x.TaxonomyEntry.Name.Length - limit_key.Length) == p.TaxonomyEntry.Name);
                        if (p_source != null)
                            upper_limits.Add(p, p_source);
                    }

                    limit_key = "MIN";
                    SimComponent c_source_LL = comps_referenced_by_this_or_parent.FirstOrDefault(x => x.Parameters.Where(y => y.TaxonomyEntry.Name.EndsWith(limit_key) && y.TaxonomyEntry.Name.Substring(0, y.TaxonomyEntry.Name.Length - limit_key.Length) == p.TaxonomyEntry.Name).Count() > 0);
                    if (c_source_LL != null)
                    {
                        SimParameter p_source = c_source_LL.Parameters.FirstOrDefault(x => x.TaxonomyEntry.Name.EndsWith(limit_key) && x.TaxonomyEntry.Name.Substring(0, x.TaxonomyEntry.Name.Length - limit_key.Length) == p.TaxonomyEntry.Name);
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
    }
}
