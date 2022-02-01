using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.Connectors;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.SimGeo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange
{
    /// <summary>
    /// A dummy implementation of the INetworkGeometryExchange interface.
    /// </summary>
	public class DummyNetworkGeometryExchange : INetworkGeometryExchange
    {
        /// <inheritdoc/>
		public void AddGeometricRepresentationToNetwork(GeometryModel model, SimFlowNetwork network, ProjectData modelStore) { }

        /// <inheritdoc/>
        public int GetNetworkRepresentationFileIndex(FileInfo representationFile)
        {
            return -1;
        }

        /// <inheritdoc/>
        public void Associate(SimFlowNetworkElement networkElement, BaseGeometry geometry) { }

        /// <inheritdoc/>
        public FileInfo GetNetworkRepresentationFile(SimFlowNetwork network)
        {
            return null;
        }

        /// <inheritdoc/>
        public void Reset()
        { }

        /// <inheritdoc/>
        public void RemoveGeometricRepresentationFromNetwork(GeometryModel model, SimFlowNetwork network, ProjectData modelStore)
        {
        }
    }
}
