using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Excel
{
    public struct InstanceStateFilter
    {
        public SimInstanceType Type { get; }
        public bool IsRealized { get; }

        public InstanceStateFilter(SimInstanceType type, bool isRealized)
        {
            this.Type = type;
            this.IsRealized = isRealized;
        }
    }
}
