using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Users;
using SIMULTAN.Exchange.Connectors;
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
    /// Dummy implementation of the IComponentGeometryExchange interface. Used for testing and for starting the GeometryViewer without components
    /// </summary>
    public class DummyComponentGeometryExchange : IComponentGeometryExchange
    {
        private FileInfo file;

        /// <summary>
        /// Initializes a new instance of the DummyComponentGeometryExchange class
        /// </summary>
        /// <param name="file">The file that should be opened</param>
        public DummyComponentGeometryExchange(FileInfo file)
        {
            this.file = file;
        }



#pragma warning disable 0067
        /// <inheritdoc/>
        public event AssociationChangedEventHandler AssociationChanged;
        /// <inheritdoc/>
        public event GeometryInvalidatedEventHandler GeometryInvalidated;
        /// <inheritdoc/>
        public event EventHandler AddGeometryModelFinished;
#pragma warning restore 0067

        /// <inheritdoc/>
        public INetworkGeometryExchange NetworkCommunicator { get { return new DummyNetworkGeometryExchange(); } }

        /// <inheritdoc/>
        public bool EnableAsyncUpdates
        {
            get => false;
            set { }
        }

        /// <inheritdoc/>
        public IEnumerable<SimComponent> GetComponents(BaseGeometry geometry)
        {
            return new SimComponent[] { };
        }
        /// <inheritdoc/>
        public void Associate(SimComponent _comp, BaseGeometry _geometry) { }
        /// <inheritdoc/>
        public void Associate(SimComponent _comp, IEnumerable<BaseGeometry> _geometry) { }
        /// <inheritdoc/>
        public (double outer, double inner) GetFaceOffset(Face _face)
        {
            return (0.5, 0.25);
        }
        /// <inheritdoc/>
        public void DisAssociate(SimComponent _comp, BaseGeometry _geometry)
        { }
        /// <inheritdoc/>
        public void TransferObservations(GeometryModelData source, GeometryModelData target)
        { }
        /// <inheritdoc/>
        public void ReplaceGeometryModel(GeometryModelData modelOld, GeometryModelData modelNew)
        { }
        /// <inheritdoc/>
        public bool ResourceFileExists(FileInfo file)
        {
            return true;
        }
        /// <inheritdoc/>
        public bool IsValidResourcePath(FileInfo fi, bool isContained)
        {
            return true;
        }
        /// <inheritdoc/>
        public void AddResourceFile(FileInfo file)
        { }

        private static int modelId;

        /// <inheritdoc/>
        public int GetResourceFileIndex(FileInfo fi)
        {
            var id = modelId;
            modelId++;
            return id;
        }
        /// <inheritdoc/>
        public FileInfo GetFileFromResourceIndex(int resourceIndex)
        {
            return file;
        }

        /// <inheritdoc/>
        public bool RemoveGeometryModel(FileInfo _file, GeometryModelRemovalMode _reason)
        {
            return true;
        }

        /// <inheritdoc/>
        public BaseGeometry GetGeometryFromId(int _index_of_geometry_model, ulong _geom_id)
        {
            return null;
        }
    }
}
