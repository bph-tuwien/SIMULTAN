using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Used to mark DXF savecodes which were previously in use but are not longer relevant.
    /// Do not delete this codes since they may not be used for something else.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class SaveCodeNotInUseAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the SaveCodeNotInUseAttribute class.
        /// The message isn't stored anywhere and is just there for reference
        /// </summary>
        /// <param name="message">A message why the savecode isn't used anymore</param>
        public SaveCodeNotInUseAttribute(string message) { }

        /// <summary>
        /// Initializes a new instance of the SaveCodeNotInUseAttribute class.
        /// </summary>
        public SaveCodeNotInUseAttribute() { }
    }
}
