using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange
{

    internal static class ExchangeHelpers
    {
        private static readonly Dictionary<string, (string unit, SimParameterOperations operations, SimInfoFlow propagation)> reservedParameterInfo =
            new Dictionary<string, (string unit, SimParameterOperations operations, SimInfoFlow propagation)>
            {
                { ReservedParameters.RP_AREA,                       ("m²", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_AREA_MIN,                   ("m²", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_AREA_MAX,                   ("m²", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_COUNT,                      ("-", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN,    ("m", SimParameterOperations.EditValue, SimInfoFlow.Mixed) },
                { ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT,   ("m", SimParameterOperations.EditValue, SimInfoFlow.Mixed) },
                { ReservedParameters.RP_WIDTH,                      ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_WIDTH_MIN,                  ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_WIDTH_MAX,                  ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_HEIGHT,                     ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_HEIGHT_MIN,                 ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_HEIGHT_MAX,                 ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_K_FOK,                      ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_K_FOK_ROH,                  ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_K_F_AXES,                   ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_K_DUK,                      ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_K_DUK_ROH,                  ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_K_D_AXES,                   ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_H_NET,                      ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_H_GROSS,                    ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_H_AXES,                     ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_L_PERIMETER,                ("m", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_AREA_BGF,                   ("m²", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_AREA_NGF,                   ("m²", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_AREA_NF,                    ("m²", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_AREA_AXES,                  ("m²", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_VOLUME_BRI,                 ("m³", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_VOLUME_NRI,                 ("m³", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_VOLUME_NRI_NF,              ("m³", SimParameterOperations.None, SimInfoFlow.Input) },
                { ReservedParameters.RP_VOLUME_AXES,                ("m³", SimParameterOperations.None, SimInfoFlow.Input) },
            };

        /// <summary>
        /// Checks if a parameter exists and creates it if it doesn't exist.
        /// For existing parameters, the propagation mode is updated and the <see cref="SimParameter.IsAutomaticallyGenerated"/> property
        /// is set to True
        /// </summary>
        /// <param name="component">The component in which the parameter should be created</param>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="propagation">The propagation mode for the parameter</param>
        /// <param name="value">The inital numerical value of the parameter (ignored when the parameter exists)</param>
        /// <returns>Returns the parameter</returns>
        internal static SimParameter CreateParameterIfNotExists(SimComponent component, string parameterName,
            SimParameterInstancePropagation propagation, double value)
        {
            var parameter = component.Parameters.FirstOrDefault(x => x.Name == parameterName);

            if (parameter == null)
            {
                if (!reservedParameterInfo.TryGetValue(parameterName, out var pInfo))
                    pInfo = ("-", SimParameterOperations.All, SimInfoFlow.Mixed);

                parameter = new SimParameter(parameterName, pInfo.unit, value, double.MinValue, double.MaxValue, pInfo.operations)
                {
                    Propagation = pInfo.propagation,
                    InstancePropagationMode = propagation,
                    IsAutomaticallyGenerated = true,
                    TextValue = "generated",
                    Category = SimCategory.Geometry,
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
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="value">The new value. Only used when a parameter with the given name exists</param>
        internal static void SetParameterIfExists(SimInstancePlacementGeometry placement, string parameterName, double value)
        {
            if (placement.Instance != null && placement.Instance.Component != null)
            {
                var param = placement.Instance.Component.Parameters.FirstOrDefault(x => x.Name == parameterName);
                if (param != null)
                {
                    param.ValueCurrent = value;
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
