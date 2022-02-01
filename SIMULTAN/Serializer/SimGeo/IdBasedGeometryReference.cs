using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.SimGeo
{
    internal class IdBasedGeometryReference : GeometryReference
    {
        internal ulong LegacyId { get; }

        internal IdBasedGeometryReference(ulong modelId, ulong geometryId, string cachedName, BaseGeometry cachedGeometry, SimGeometryModelCollection modelStore)
            : base(Guid.Empty, geometryId, cachedName, cachedGeometry, modelStore)
        {
            this.LegacyId = modelId;
        }

        internal (GeometryReference reference, bool success) ToGeometryReference(Dictionary<ulong, Guid> idToGuid)
        {
            bool success = true;

            if (!idToGuid.TryGetValue(this.LegacyId, out var guid))
            {
                guid = Guid.Empty;
                success = false;
            }
            return (new GeometryReference(guid, this.GeometryID, this.Name, this.Target, this.ModelStore), success);
        }
    }
}
