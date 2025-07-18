// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;


namespace SIMULTAN.Data.SimMath
{
    /// <summary>
    /// ColorConverter Parses a color.
    /// </summary>
    public sealed class SimColorConverter : TypeConverter
    {
        /// <summary>
        /// CanConvertFrom
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext td, Type t)
        {
            if (t == typeof(string))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// TypeConverter method override.
        /// </summary>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>true if conversion is possible</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        ///<summary>
        /// ConvertFromString
        ///</summary>
        public static new object ConvertFromString(string value)
        {
            if (null == value)
            {
                return null;
            }

            return SimParsers.ParseColor(value, null);
        }

        /// <summary>
        /// ConvertFrom - attempt to convert to a Color from the given object
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if the example object is null or is not a valid type
        /// which can be converted to a Color.
        /// </exception>
        public override object ConvertFrom(ITypeDescriptorContext td, System.Globalization.CultureInfo ci, object value)
        {
            if (null == value)
            {
                throw GetConvertFromException(value);
            }

            String s = value as string;

            if (null == s)
            {
                throw new ArgumentException("Value is not a string", "value");
            }

            return SimParsers.ParseColor(value as string, ci, td);
        }

        /// <summary>
        /// TypeConverter method implementation.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// An NotSupportedException is thrown if the example object is null or is not a Color,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="context">ITypeDescriptorContext</param>
        /// <param name="culture">current culture (see CLR specs)</param>
        /// <param name="value">value to convert from</param>
        /// <param name="destinationType">Type to convert to</param>
        /// <returns>converted value</returns>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != null && value is SimColor)
            {
                if (destinationType == typeof(InstanceDescriptor))
                {
                    MethodInfo mi = typeof(SimColor).GetMethod("FromArgb", new Type[] { typeof(byte), typeof(byte), typeof(byte), typeof(byte) });
                    SimColor c = (SimColor)value;
                    return new InstanceDescriptor(mi, new object[] { c.A, c.R, c.G, c.B });
                }
                else if (destinationType == typeof(string))
                {
                    SimColor c = (SimColor)value;
                    return c.ToString(culture);
                }
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
