using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.CODXF
{
    /// <summary>
    /// Placement for transferring the <see cref="SimInstanceType"/> for Version &lt; 7 files
    /// </summary>
    internal class InstanceTypeTransferPlacement : SimInstancePlacement
    {
        public InstanceTypeTransferPlacement(SimInstanceType instanceType) : base(instanceType) { }

        public override void AddToTarget() { }

        public override void RemoveFromTarget() { }

        internal override bool RestoreReferences(Dictionary<SimObjectId, SimFlowNetworkElement> networkElements) { return false; }
    }
}
