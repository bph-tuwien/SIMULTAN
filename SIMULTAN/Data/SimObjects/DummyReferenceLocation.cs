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
        /// <inheritdoc/>
        public string AbsolutePath { get; }

        /// <summary>
        /// Instantiates a minimal implementation of the IReferenceLocation interface w/o any behaviour.
        /// </summary>
        /// <param name="globalId">the golbal id</param>
        /// <param name="absolutePath">the absolute path to the location</param>
        public DummyReferenceLocation(Guid globalId, string absolutePath)
        {
            this.GlobalID = globalId;
            this.AbsolutePath = absolutePath;
        }
    }
}
