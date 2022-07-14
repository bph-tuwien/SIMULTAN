using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// A dummy implementation of the interface IReferenceLocation as a placeholder.
    /// </summary>
    public class DummyReferenceLocation : IReferenceLocation
    {
        /// <inheritdoc/>
        public Guid GlobalID { get; }

        /// <summary>
        /// Instantiates a minimal implementation of the IReferenceLocation interface w/o any behavior.
        /// </summary>
        /// <param name="globalId">the global id</param>
        public DummyReferenceLocation(Guid globalId)
        {
            this.GlobalID = globalId;
        }
    }
}
