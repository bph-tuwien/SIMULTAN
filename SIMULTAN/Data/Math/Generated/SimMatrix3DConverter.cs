// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// This file was generated, please do not edit it directly.
//
// Please see MilCodeGen.html for more information.
//

using System;
using System.ComponentModel;
using System.Globalization;

namespace SIMULTAN.Data.SimMath
{
    /// <summary>
    /// Matrix3DConverter - Converter class for converting instances of other types to and from Matrix3D instances
    /// </summary>
    public sealed class SimMatrix3DConverter : TypeConverter
    {
        /// <summary>
        /// Returns true if this type converter can convert from a given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert from the provided type, false if not.
        /// </returns>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="sourceType"> The Type being queried for support. </param>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Returns true if this type converter can convert to the given type.
        /// </summary>
        /// <returns>
        /// bool - True if this converter can convert to the provided type, false if not.
        /// </returns>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="destinationType"> The Type being queried for support. </param>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Attempts to convert to a Matrix3D from the given object.
        /// </summary>
        /// <returns>
        /// The Matrix3D which was constructed.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if the example object is null or is not a valid type
        /// which can be converted to a Matrix3D.
        /// </exception>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="culture"> The requested CultureInfo.  Note that conversion uses "en-US" rather than this parameter. </param>
        /// <param name="value"> The object to convert to an instance of Matrix3D. </param>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value == null)
            {
                throw GetConvertFromException(value);
            }

            String source = value as string;

            if (source != null)
            {
                return SimMatrix3D.Parse(source);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// ConvertTo - Attempt to convert an instance of Matrix3D to the given type
        /// </summary>
        /// <returns>
        /// The object which was constructoed.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// A NotSupportedException is thrown if "value" is null or not an instance of Matrix3D,
        /// or if the destinationType isn't one of the valid destination types.
        /// </exception>
        /// <param name="context"> The ITypeDescriptorContext for this call. </param>
        /// <param name="culture"> The CultureInfo which is respected when converting. </param>
        /// <param name="value"> The object to convert to an instance of "destinationType". </param>
        /// <param name="destinationType"> The type to which this will convert the Matrix3D instance. </param>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != null && value is SimMatrix3D)
            {
                SimMatrix3D instance = (SimMatrix3D)value;

                if (destinationType == typeof(string))
                {
                    // Delegate to the formatting/culture-aware ConvertToString method.

#pragma warning disable 6506 // instance is obviously not null
                    return instance.ConvertToString(null, culture);
#pragma warning restore 6506
                }
            }

            // Pass unhandled cases to base class (which will throw exceptions for null value or destinationType.)
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
