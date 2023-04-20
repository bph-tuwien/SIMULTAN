using SIMULTAN.Data.Assets;
using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Provides helper methods for working with Components.
    /// Note, that this methods will be removed in the future
    /// </summary>
    public static class ComponentMapping
    {
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
