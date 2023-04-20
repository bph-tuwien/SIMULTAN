using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Exception that is thrown when a delete action for a <see cref="SimTaxonomyEntryReference"/> was already added to a <see cref="SimTaxonomyEntry"/>
    /// </summary>
    public class DeleteActionAlreadyRegisteredException : Exception
    {
        /// <summary>
        /// Creates a new <see cref="DeleteActionAlreadyRegisteredException"/>
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="innerException">The inner exception</param>
        public DeleteActionAlreadyRegisteredException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception can be thrown if an <see cref="SimTaxonomyEntry"/> could not be found during lookup. (while restoring references for example)
    /// </summary>
    public class TaxonomyEntryNotFoundException : Exception
    {
        /// <inheritdoc/>
        public TaxonomyEntryNotFoundException()
        {
        }

        /// <inheritdoc/>
        public TaxonomyEntryNotFoundException(string message) : base(message)
        {
        }

        /// <inheritdoc/>
        public TaxonomyEntryNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
