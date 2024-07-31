using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Taxonomy;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Exchange
{

    internal static class ExchangeHelpers
    {
        private static readonly Dictionary<string, (string unit, SimParameterOperations operations, SimInfoFlow propagation)> reservedParameterInfo =
            new Dictionary<string, (string unit, SimParameterOperations operations, SimInfoFlow propagation)>
            {
                { ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_IN,    ("m", SimParameterOperations.EditValue, SimInfoFlow.Mixed) },
                { ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_OUT,   ("m", SimParameterOperations.EditValue, SimInfoFlow.Mixed) },
            };

        /// <summary>
        /// Checks if a parameter exists and creates it if it doesn't exist.
        /// For existing parameters, the propagation mode is updated and the <see cref="SimBaseParameter.IsAutomaticallyGenerated"/> property
        /// is set to True
        /// </summary>
        /// <param name="component">The component in which the parameter should be created</param>
        /// <param name="parameterKey">The key of the reserved parameter taxonomy entry</param>
        /// <param name="name">The name of the parameter. Used as a fallback check when the parameterKey didn't already match the taxonomy entry</param>
        /// <param name="propagation">The propagation mode for the parameter</param>
        /// <param name="value">The initial numerical value of the parameter (ignored when the parameter exists)</param>
        /// <returns>Returns the parameter</returns>
        internal static SimBaseParameter CreateParameterIfNotExists(SimComponent component, string parameterKey, string name,
            SimParameterInstancePropagation propagation, double value)
        {
            var parameter = component.Parameters.FirstOrDefault(x =>
            {
                var ret = x.HasReservedTaxonomyEntry(parameterKey);
                if (!ret && name != null)
                {
                    if (x.NameTaxonomyEntry.HasTaxonomyEntry)
                    {
                        // check if any translation contains the name
                        ret = x.NameTaxonomyEntry.TaxonomyEntryReference.Target.Localization.Entries.Values.Any(loc => loc.Name == name);
                    }
                    else
                    {
                        ret = x.NameTaxonomyEntry.Text == name;
                    }
                }
                return ret;
            });

            if (parameter == null)
            {
                if (!reservedParameterInfo.TryGetValue(parameterKey, out var pInfo))
                    pInfo = ("-", SimParameterOperations.All, SimInfoFlow.Mixed);

                var taxonomyEntry = component.Factory.ProjectData.Taxonomies.GetReservedParameter(parameterKey);


                parameter = new SimDoubleParameter(taxonomyEntry.Key, pInfo.unit, value, double.MinValue, double.MaxValue, pInfo.operations)
                {
                    Propagation = pInfo.propagation,
                    InstancePropagationMode = propagation,
                    IsAutomaticallyGenerated = true,
                    Description = "generated",
                    Category = SimCategory.Geometry,
                    NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxonomyEntry)),
                };
                component.Parameters.Add(parameter);
            }
            else
            {
                parameter.InstancePropagationMode = propagation;
                parameter.IsAutomaticallyGenerated = true;
            }

            return parameter;
        }


        /// <summary>
        /// Checks if a int parameter exists and creates it if it doesn't exist.
        /// For existing parameters, the propagation mode is updated and the <see cref="SimBaseParameter.IsAutomaticallyGenerated"/> property
        /// is set to True
        /// </summary>
        /// <param name="component">The component in which the parameter should be created</param>
        /// <param name="parameterKey">The key of the reserved parameter taxonomy entry</param>
        /// <param name="name">The name of the parameter. Used as a fallback check when the parameterKey didn't already match the taxonomy entry</param>
        /// <param name="propagation">The propagation mode for the parameter</param>
        /// <param name="value">The initial numerical value of the parameter (ignored when the parameter exists)</param>
        /// <returns>Returns the parameter</returns>
        internal static SimBaseParameter CreateIntegerParameterIfNotExsists(SimComponent component, string parameterKey, string name,
            SimParameterInstancePropagation propagation, int value)
        {
            var parameter = component.Parameters.FirstOrDefault(x =>
            {

                var ret = x.HasReservedTaxonomyEntry(parameterKey);
                if (!ret && name != null)
                {
                    if (x.NameTaxonomyEntry.HasTaxonomyEntry)
                    {
                        // check if any translation contains the name
                        ret = x.NameTaxonomyEntry.TaxonomyEntryReference.Target.Localization.Entries.Values.Any(loc => loc.Name == name);
                    }
                    else
                    {
                        ret = x.NameTaxonomyEntry.Text == name;
                    }
                }
                return ret;
            });


            if (parameter == null)
            {
                if (!reservedParameterInfo.TryGetValue(parameterKey, out var pInfo))
                    pInfo = ("-", SimParameterOperations.All, SimInfoFlow.Mixed);

                var taxonomyEntry = component.Factory.ProjectData.Taxonomies.GetReservedParameter(parameterKey);


                parameter = new SimIntegerParameter(taxonomyEntry.Key, pInfo.unit, value, int.MinValue, int.MaxValue, pInfo.operations)
                {
                    Propagation = pInfo.propagation,
                    InstancePropagationMode = propagation,
                    IsAutomaticallyGenerated = true,
                    Description = "generated",
                    Category = SimCategory.Geometry,
                    NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxonomyEntry)),
                };
                component.Parameters.Add(parameter);
            }
            else
            {
                parameter.InstancePropagationMode = propagation;
                parameter.IsAutomaticallyGenerated = true;
            }

            return parameter;
        }

        /// <summary>
        /// Sets the parameter value if a parameter with that name exists
        /// </summary>
        /// <param name="placement">The placement in which the parameter should exist</param>
        /// <param name="parameterKey">The key of the reserved parameter taxonomy entry</param>
        /// <param name="value">The new value. Only used when a parameter with the given name exists</param>
        internal static void SetParameterIfExists(SimInstancePlacementGeometry placement, string parameterKey, double value)
        {
            if (placement.Instance != null && placement.Instance.Component != null)
            {
                var param = placement.Instance.Component.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(parameterKey));
                if (param != null && param is SimDoubleParameter doubleParam)
                {
                    doubleParam.Value = value;
                }

            }
        }

        /// <summary>
        /// Creates an asset to a specific Geometry if it doesn't exist
        /// </summary>
        /// <param name="component">The component in which the asset should be created</param>
        /// <param name="geometry">The base geometry</param>
        /// <returns>The geometric asset (either newly created or the existing one)</returns>
        internal static GeometricAsset CreateAssetIfNotExists(SimComponent component, BaseGeometry geometry)
        {
            //Check if asset exists and add if not
            var asset = (GeometricAsset)component.ReferencedAssets.FirstOrDefault(x => x is GeometricAsset gm &&
                gm.Resource == geometry.ModelGeometry.Model.File &&
                gm.ContainedObjectId == geometry.Id.ToString());

            if (asset == null)
            {
                //Doesn't exit -> create
                asset = component.Factory.ProjectData.AssetManager.CreateGeometricAsset(
                    component.Id.LocalId, geometry.ModelGeometry.Model.File.Key, geometry.Id.ToString());
                component.ReferencedAssets_Internal.Add(asset);
            }

            return asset;
        }
    }
}
