using SIMULTAN.Data.Assets;
using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    public static class ComponentMapping
    {
        #region to EXCEL

        /// <summary>
        /// Adds a rule for mapping the component's data to an excel sheet.
        /// </summary>
        /// <param name="_comp">the component instance</param>
        /// <param name="_path">the traversal path of sub- or referenced components</param>
        /// <param name="_tool">the tool containing the mapping rule</param>
        /// <param name="_rule">the mapping rule</param>
        /// <param name="_replace_old">if true, replace the rule associated with the same path</param>
        /// <returns>true, if the mapping could be successfully added to the component</returns>
        public static bool AddComponent2ExcelMappingRule(this SimComponent _comp, List<long> _path, ExcelTool _tool, ExcelMappingNode _rule, bool _replace_old)
        {
            if (_path == null || _tool == null || _rule == null) return false;

            ExcelComponentMapping record = new ExcelComponentMapping(_path, _tool.Name, _tool.LastPathToFile, _rule.NodeName, _tool.GetIndexOfRule(_rule));
            string key = record.ConstructKey();
            if (_comp.MappingsPerExcelTool.ContainsKey(key)) return false;

            // same tool AND same rule && same path are NOT ADMISSIBLE
            string key_to_remove = null;
            foreach (var entry in _comp.MappingsPerExcelTool)
            {
                if (entry.Value.ToolName == _tool.Name && ExcelComponentMapping.IsSamePath(entry.Value.Path, _path))
                {
                    if (entry.Value.RuleName == _rule.NodeName)
                    {
                        // duplicate
                        key_to_remove = entry.Key;
                    }
                    else if (entry.Value.RuleName != _rule.NodeName && _replace_old)
                    {
                        // rule to be replaced
                        key_to_remove = entry.Key;
                    }
                }

            }
            if (!(string.IsNullOrEmpty(key_to_remove)))
                _comp.MappingsPerExcelTool.Remove(key_to_remove);

            _comp.MappingsPerExcelTool.Add(key, record);
            return true;
        }

        /// <summary>
        /// Remove the rule for mapping to an excel sheet when the traversal path and the excel tool are known.
        /// </summary>
        /// <param name="_comp">the component instance</param>
        /// <param name="_path">the traversal path of sub- or referenced components</param>
        /// <param name="_tool">the excel tool containing the rule to be removed</param>
        /// <returns></returns>
        public static bool RemoveComponent2ExcelMappingRule(this SimComponent _comp, List<long> _path, ExcelTool _tool)
        {
            if (_path == null || _tool == null) return false;

            string key = null;
            string query_path = ExcelComponentMapping.ConstructQueryFromPath(_path);
            foreach (var entry in _comp.MappingsPerExcelTool)
            {
                if (entry.Key.Contains(_tool.Name))
                {
                    string key_path = ExcelComponentMapping.ExtractPathFromKey(entry.Key);
                    if (key_path == query_path)
                    {
                        key = entry.Key;
                        break;
                    }
                }
            }

            if (key != null)
                return _comp.MappingsPerExcelTool.Remove(key);
            else
                return false;
        }

        /// <summary>
        /// Find a mapping to a rule contained in the tool and applied to the component when the traversal path is '_path'.
        /// </summary>
        /// <param name="_comp">the component instance</param>
        /// <param name="_tool">the excel tool</param>
        /// <param name="_path">the traversal path of sub- or referenced components</param>
        /// <returns></returns>
        public static ExcelMappingNode ExtractSelectedNodeInContext(this SimComponent _comp, ExcelTool _tool, List<long> _path)
        {
            if (_tool == null || _path == null) return null;
            if (_comp.MappingsPerExcelTool.Count == 0) return null;

            var match_1 = _comp.MappingsPerExcelTool.Keys.Where(x => x.StartsWith(_tool.Name + "."));
            if (match_1.Count() == 0) return null;

            List<string> match_2 = match_1.Where(x => ExcelComponentMapping.ExtractPathFromKey(x) == ExcelComponentMapping.ConstructQueryFromPath(_path)).ToList();
            if (match_2.Count > 0)
            {
                for (int i = 0; i < match_2.Count; i++)
                {
                    ExcelComponentMapping record = _comp.MappingsPerExcelTool[match_2[i]];
                    ExcelMappingNode corrsponding_rule = null;
                    if (_tool.InputRules.Count > 1 && 0 < record.RuleIndexInTool && record.RuleIndexInTool < _tool.InputRules.Count)
                    {
                        corrsponding_rule = _tool.InputRules[record.RuleIndexInTool];
                        if (corrsponding_rule.NodeName == record.RuleName)
                            return corrsponding_rule;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Traverses the component along sub- and reference component links and looks for mappings
        /// to rules contained in the given excel tool.
        /// </summary>
        /// <param name="_comp">the component instance</param>
        /// <param name="_tool_name">the excel tool's name</param>
        /// <param name="_path">the traversal path of sub- or referenced components so far</param>
        /// <param name="_abort">stop the search, if true</param>
        /// <param name="_aborted">the path starts that have been discarded so far</param>
        /// <returns></returns>
        public static bool HasComponent2ExcelMappingsTo(this SimComponent _comp, string _tool_name, ref List<long> _path,
            ref bool _abort, ref HashSet<long> _aborted)
        {
            // added 2020.06.09: reasonable cut-off (hardly anyone would go beyond a path of length 16) 
            // in case a loop encompass all Components in the project at once :-0
            if (_path != null && _path.Count >= 16)
            {
                _abort = true;
                if (_aborted == null)
                    _aborted = new HashSet<long>();
                _aborted.Add(_comp.Id.LocalId);
                return false;
            }

            if (_aborted != null && _aborted.Contains(_comp.Id.LocalId))
            {
                _abort = true;
                return false;
            }

            if (_tool_name == null) return false;

            if (_path == null)
                _path = new List<long>();

            // loop test
            int repeat_count = 0;
            for (int i = 0; i < _path.Count; ++i)
            {
                if (_path[i] == _comp.Id.LocalId)
                {
                    repeat_count++;
                    if (repeat_count > 2)
                    {
                        _abort = true;
                        if (_aborted == null)
                            _aborted = new HashSet<long>();
                        _aborted.Add(_comp.Id.LocalId);
                        return false;
                    }
                }
            }

            _path.Add(_comp.Id.LocalId);

            // look in self
            var match_1 = _comp.MappingsPerExcelTool.Keys.Where(x => x.StartsWith(_tool_name + ".")).ToList();
            if (match_1.Count > 0)
            {
                string path_check = ExcelComponentMapping.ConstructQueryFromPath(_path);
                var match_2 = match_1.Where(x => ExcelComponentMapping.ExtractPathFromKey(x).Equals(path_check));
                if (match_2.Any())
                    return true;
            }

            // look in sub-components
            foreach (var entry in _comp.Components)
            {
                if (entry.Component != null)
                {
                    bool found_in_entry = entry.Component.HasComponent2ExcelMappingsTo(_tool_name, ref _path, ref _abort, ref _aborted);
                    if (found_in_entry)
                        return true;
                }
            }

            // look in referenced components
            foreach (var entry in _comp.ReferencedComponents)
            {
                if (entry.Target != null)
                {
                    bool found_in_entry = entry.Target.HasComponent2ExcelMappingsTo(_tool_name, ref _path, ref _abort, ref _aborted);
                    if (found_in_entry)
                        return true;
                }
            }

            //Reset path
            _path.RemoveAt(_path.Count - 1);
            return false;
        }

        #endregion

        #region to OTHER COMPONENTS: mapping


        /// <summary>
        /// Called by the component carrying the data to input into the calculator.
        /// </summary>
        /// <param name="_comp">The component instance</param>
        /// <param name="_name">The name of the mapping</param>
        /// <param name="_calculator">Component carrying the calculation(s)</param>
        /// <returns>The created mapping</returns>
        public static CalculatorMapping CreateMappingTo(this SimComponent _comp, string _name, SimComponent _calculator)
        {
            return CreateMappingTo(_comp, _name, _calculator, new List<CalculatorMapping.MappingParameterTuple>(),
                new List<CalculatorMapping.MappingParameterTuple>());
        }

        /// <summary>
        /// Called by the component carrying the data to input into the calculator.
        /// </summary>
        /// <param name="_comp">The component instance</param>
        /// <param name="_name">The name of the mapping</param>
        /// <param name="_calculator">Component carrying the calculation(s)</param>
        /// <param name="_input_mapping">Contains combination of parameter which are mapped onto each other</param>
        /// <param name="_output_mapping">Contains combination of parameter which are mapped onto each other</param>
        /// <returns>The created mapping</returns>
        public static CalculatorMapping CreateMappingTo(this SimComponent _comp, string _name, SimComponent _calculator,
            IEnumerable<CalculatorMapping.MappingParameterTuple> _input_mapping,
            IEnumerable<CalculatorMapping.MappingParameterTuple> _output_mapping
            )
        {
            if (_name == null)
                throw new ArgumentNullException(nameof(_name));
            if (_calculator == null)
                throw new ArgumentNullException(nameof(_calculator));
            if (_input_mapping == null)
                throw new ArgumentNullException(nameof(_input_mapping));
            if (_output_mapping == null)
                throw new ArgumentNullException(nameof(_output_mapping));

            var mapping = new CalculatorMapping(_name, _calculator, _input_mapping, _output_mapping);

            _calculator.MappedToBy.Add(_comp);
            _comp.CalculatorMappings_Internal.Add(mapping);

            return mapping;
        }

        /// <summary>
        /// Removes the mapping to another component.
        /// </summary>
        /// <param name="_comp">the component instance</param>
        /// <param name="_mapping">the mapping</param>
        /// <returns>true, if the operation was performed successfully</returns>
        public static bool RemoveMapping(this SimComponent _comp, CalculatorMapping _mapping)
        {
            if (_mapping == null) return false;

            if (_mapping.Calculator != null)
                _mapping.Calculator.MappedToBy.Remove(_comp);

            return _comp.CalculatorMappings_Internal.Remove(_mapping);
        }

        /// <summary>
        /// Removed the mapping to the '_calc' component.
        /// </summary>
        /// <param name="_comp">the component instance</param>
        /// <param name="_calc">the component acting as a calculator for other components</param>
        /// <returns>true, if the operation was perfomed successfully</returns>
        internal static bool RemoveMappingTo(this SimComponent _comp, SimComponent _calc)
        {
            if (_calc == null) return false;

            CalculatorMapping to_remove = _comp.CalculatorMappings.FirstOrDefault(x => x.Calculator != null && x.Calculator.Id == _calc.Id);
            if (to_remove != null)
                return _comp.RemoveMapping(to_remove);
            else
                return false;
        }

        /// <summary>
        /// Recursively called by the component carrying the data to input into the calculator.
        /// It calls the calculation chain in the calculator with the values supplied by the caller.
        /// </summary>
        /// <param name="_comp">the calling component instance</param>
        public static void EvaluateAllMappings(this SimComponent _comp)
        {
            foreach (var entry in _comp.Components)
            {
                SimComponent sC = entry.Component;
                if (sC == null) continue;

                sC.EvaluateAllMappings();
            }
            foreach (CalculatorMapping mapping in _comp.CalculatorMappings)
            {
                mapping.Evaluate(_comp);
            }
        }

        #endregion

        #region to OTHERS: assets

        //Missleading: Can only create Document Assets while GetAsset also returns GeometryAssets
        public static Asset AddAsset(this SimComponent _comp, ResourceFileEntry _resource, string _content_id)
        {
            var asset = _comp.GetAsset(_resource, _content_id);

            if (asset == null)
            {
                asset = _comp.Factory.ProjectData.AssetManager.CreateDocumentAsset(_comp, _resource, _content_id);
                _comp.ReferencedAssets_Internal.Add(asset);
            }

            return asset;
        }

        public static Asset GetAsset(this SimComponent _comp, ResourceFileEntry _resource, string _content_id)
        {
            return _comp.ReferencedAssets.FirstOrDefault(x => x.ResourceKey == _resource.Key && x.ContainedObjectId == _content_id);
        }

        /// <summary>
        /// Removes the asset in the file found at the coded location and with the given id from the component.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_path_code">the integer code that corresponds to a file nam in the asset manager</param>
        /// <param name="_content_id">the id of the asset in the file found at the coded location</param>
        /// <returns></returns>
        [Obsolete]
        public static Asset RemoveAsset(this SimComponent _comp, int _path_code, string _content_id)
        {
            Asset found = _comp.ReferencedAssets.FirstOrDefault(x => x.ResourceKey == _path_code && x.ContainedObjectId == _content_id);
            if (found != null)
            {
                _comp.ReferencedAssets_Internal.Remove(found);
                found.RemoveReferencing(_comp.Id.LocalId);
            }

            return found;
        }

        public static void RemoveAsset(this SimComponent _comp, Asset _asset)
        {
            if (_asset == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(_asset)));

            _comp.ReferencedAssets_Internal.Remove(_asset);
            _asset.RemoveReferencing(_comp.Id.LocalId);
        }

        #endregion
    }
}
