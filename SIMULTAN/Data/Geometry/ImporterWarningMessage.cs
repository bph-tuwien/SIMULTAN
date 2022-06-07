using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Different reasons why an import of a proxy geometry has failed
    /// </summary>
    public enum ImportWarningReason
    {
        /// <summary>
        /// The file could not be found
        /// </summary>
        /// Expects one data element ({0}: file name)
        FileNotFound,
        /// <summary>
        /// The Import itself noticed a problem.
        /// </summary>
        /// Expects two data elements ({0}: file path, {1}: import error message
        ImportFailed,
    }

    /// <summary>
    /// Stores a reason and it's data for importer warnings
    /// </summary>
    public class ImportWarningMessage : IEquatable<ImportWarningMessage>
    {
        /// <summary>
        /// The reason of the warning
        /// </summary>
        public ImportWarningReason Reason { get; }

        /// <summary>
        /// The data of the warning (see <see cref="ImportWarningReason"/> for which data elements are expected for a specific reason)
        /// </summary>
        public object[] Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportWarningMessage"/> class
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="data"></param>
        public ImportWarningMessage(ImportWarningReason reason, object[] data)
        {
            this.Reason = reason;
            this.Data = data;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is ImportWarningMessage iwm)
                return Equals(iwm);
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = Reason.GetHashCode();
            foreach (var item in Data)
                hash = hash ^ item.GetHashCode();

            return hash;
        }

        /// <inheritdoc/>
        public bool Equals(ImportWarningMessage other)
        {
            return other.Reason == this.Reason &&
                Enumerable.SequenceEqual(other.Data, this.Data);
        }

        /// <summary>
        /// Compares two ImportWarningMessage instances for equality
        /// </summary>
        /// <param name="lhs">The first instance</param>
        /// <param name="rhs">The second instance</param>
        /// <returns>True when the two instances are equal</returns>
        public static bool operator ==(ImportWarningMessage lhs, ImportWarningMessage rhs)
        {
            if (lhs == null && rhs == null)
                return true;
            if (lhs != null && rhs != null)
                return lhs.Equals(rhs);
            return false;
        }

        /// <summary>
        /// Compares two ImportWarningMessage instances for inequality
        /// </summary>
        /// <param name="lhs">The first instance</param>
        /// <param name="rhs">The second instance</param>
        /// <returns>True when the two instances are not equal</returns>
        public static bool operator !=(ImportWarningMessage lhs, ImportWarningMessage rhs)
        {
            return !(rhs == lhs);
        }
    }
}
