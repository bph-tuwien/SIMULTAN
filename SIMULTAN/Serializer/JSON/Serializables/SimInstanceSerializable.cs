using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.DXF;
using System.Collections.Generic;
using SIMULTAN.Data.SimMath;
using System;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="SimComponentInstance"/>
    /// </summary>
    public class SimInstanceSerializable
    {
        /// <summary>
        /// The name of the instance
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The ID of the instance
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// The InstanceSize
        /// </summary>
        public SimInstanceSizeSerializable InstanceSize { get; set; }
        /// <summary>
        /// Rotation of the instance
        /// </summary>
        public SimQuaternionSerializable InstanceRotation { get; set; }
        /// <summary>
        /// Instance Parameters
        /// </summary>
        public List<KeyValuePair<long, string>> InstanceParameters { get; set; } = new List<KeyValuePair<long, string>>();
        /// <summary>
        /// Contained placements
        /// </summary>
        public List<SimInstancePlacementSerializable> Placements { get; set; } = new List<SimInstancePlacementSerializable>();
        /// <summary>
        /// Creates a new instance of SimInstanceSerializable
        /// </summary>
        /// <param name="instance">The SimComponentInstance</param>
        public SimInstanceSerializable(SimComponentInstance instance)
        {
            this.Id = instance.LocalID;
            this.Name = instance.Name;
            this.InstanceSize = new SimInstanceSizeSerializable(instance.InstanceSize);
            this.InstanceRotation = new SimQuaternionSerializable(instance.InstanceRotation);

            foreach (var item in instance.InstanceParameterValuesPersistent)
            {
                if (item.Key is SimDoubleParameter)
                {
                    InstanceParameters.Add(new KeyValuePair<long, string>(item.Key.LocalID, DXFDataConverter<double>.P.ToDXFString(item.Value)));
                }
                if (item.Key is SimIntegerParameter)
                {
                    InstanceParameters.Add(new KeyValuePair<long, string>(item.Key.LocalID, DXFDataConverter<int>.P.ToDXFString(item.Value)));
                }
                if (item.Key is SimStringParameter)
                {
                    InstanceParameters.Add(new KeyValuePair<long, string>(item.Key.LocalID, DXFDataConverter<string>.P.ToDXFString(item.Value)));
                }
                if (item.Key is SimBoolParameter)
                {
                    InstanceParameters.Add(new KeyValuePair<long, string>(item.Key.LocalID, DXFDataConverter<bool>.P.ToDXFString(item.Value)));
                }
                if (item.Key is SimEnumParameter)
                {
                    if (item.Value != null)
                    {
                        InstanceParameters.Add(new KeyValuePair<long, string>(item.Key.LocalID, DXFDataConverter<string>.P.ToDXFString(item.Value.Target.Key)));
                    }
                    else
                    {
                        InstanceParameters.Add(new KeyValuePair<long, string>(item.Key.LocalID, null));
                    }

                }
            }

            for (int i = 0; i < instance.Placements.Count; i++)
            {
                if (instance.Placements[i] is SimInstancePlacementSimNetwork sn)
                {
                    Placements.Add(new SimInstancePlacementSimNetworkSerializable(sn));
                }
                else if (instance.Placements[i] is SimInstancePlacementGeometry geo)
                {
                    Placements.Add(new SimInstancePlacementGeometrySerializable(geo));
                }
                else if (instance.Placements[i] is SimInstancePlacementNetwork nw)
                {
                    Placements.Add(new SimInstancePlacementNetworkSerializable(nw));
                }
            }
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimInstanceSerializable() { throw new NotImplementedException(); }
    }
}
