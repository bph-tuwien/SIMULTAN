using System;
using System.Runtime.Serialization;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Exception thrown by properties when a value is not allowed. Notifies the PropertyUndoItem that the operation has failed
    /// </summary>
    [Serializable]
    public class PropertyUnsupportedValueException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the PropertyUnsupportedValueException class
        /// </summary>
        public PropertyUnsupportedValueException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the PropertyUnsupportedValueException class
        /// </summary>
        /// <param name="message">The message text</param>
        public PropertyUnsupportedValueException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PropertyUnsupportedValueException class
        /// </summary>
        /// <param name="message">The message text</param>
        /// <param name="innerException">The inner exception</param>
        public PropertyUnsupportedValueException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the PropertyUnsupportedValueException class
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected PropertyUnsupportedValueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}