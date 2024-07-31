// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SIMULTAN.Data.SimMath
{
    /// <summary>
    /// Color
    /// The Color structure, composed of a private, synchronized ScRgb (IEC 61966-2-2) value
    /// a color context, composed of an ICC profile and the native color values.
    /// </summary>
    [TypeConverter(typeof(SimColorConverter))]
    public struct SimColor : IFormattable, IEquatable<SimColor>
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        ///<summary>
        /// Color - sRgb legacy interface, assumes Rgb values are sRgb
        ///</summary>
        internal static SimColor FromUInt32(uint argb)// internal legacy sRGB interface
        {
            SimColor c1 = new SimColor();

            c1.sRgbColor.a = (byte)((argb & 0xff000000) >> 24);
            c1.sRgbColor.r = (byte)((argb & 0x00ff0000) >> 16);
            c1.sRgbColor.g = (byte)((argb & 0x0000ff00) >> 8);
            c1.sRgbColor.b = (byte)(argb & 0x000000ff);
            c1.scRgbColor.a = (float)c1.sRgbColor.a / 255.0f;
            c1.scRgbColor.r = sRgbToScRgb(c1.sRgbColor.r);  // note that context is undefined and thus unloaded
            c1.scRgbColor.g = sRgbToScRgb(c1.sRgbColor.g);
            c1.scRgbColor.b = sRgbToScRgb(c1.sRgbColor.b);

            c1.isFromScRgb = false;

            return c1;
        }

        ///<summary>
        /// FromScRgb
        ///</summary>
        public static SimColor FromScRgb(float a, float r, float g, float b)
        {
            SimColor c1 = new SimColor();

            c1.scRgbColor.r = r;
            c1.scRgbColor.g = g;
            c1.scRgbColor.b = b;
            c1.scRgbColor.a = a;
            if (a < 0.0f)
            {
                a = 0.0f;
            }
            else if (a > 1.0f)
            {
                a = 1.0f;
            }

            c1.sRgbColor.a = (byte)((a * 255.0f) + 0.5f);
            c1.sRgbColor.r = ScRgbTosRgb(c1.scRgbColor.r);
            c1.sRgbColor.g = ScRgbTosRgb(c1.scRgbColor.g);
            c1.sRgbColor.b = ScRgbTosRgb(c1.scRgbColor.b);

            c1.isFromScRgb = true;

            return c1;
        }

        ///<summary>
        /// Color - sRgb legacy interface, assumes Rgb values are sRgb, alpha channel is linear 1.0 gamma
        ///</summary>
        public static SimColor FromArgb(byte a, byte r, byte g, byte b)// legacy sRGB interface, bytes are required to properly round trip
        {
            SimColor c1 = new SimColor();

            c1.scRgbColor.a = (float)a / 255.0f;
            c1.scRgbColor.r = sRgbToScRgb(r);  // note that context is undefined and thus unloaded
            c1.scRgbColor.g = sRgbToScRgb(g);
            c1.scRgbColor.b = sRgbToScRgb(b);
            c1.sRgbColor.a = a;
            c1.sRgbColor.r = ScRgbTosRgb(c1.scRgbColor.r);
            c1.sRgbColor.g = ScRgbTosRgb(c1.scRgbColor.g);
            c1.sRgbColor.b = ScRgbTosRgb(c1.scRgbColor.b);

            c1.isFromScRgb = false;

            return c1;
        }

        ///<summary>
        /// Color - sRgb legacy interface, assumes Rgb values are sRgb
        ///</summary>
        public static SimColor FromRgb(byte r, byte g, byte b)// legacy sRGB interface, bytes are required to properly round trip
        {
            SimColor c1 = SimColor.FromArgb(0xff, r, g, b);
            return c1;
        }
        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        ///<summary>
        /// GetHashCode
        ///</summary>
        public override int GetHashCode()
        {
            return this.scRgbColor.GetHashCode(); //^this.context.GetHashCode();
        }

        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            // Delegate to the internal method which implements all ToString calls.

            string format = isFromScRgb ? c_scRgbFormat : null;

            return ConvertToString(format, null);
        }

        /// <summary>
        /// Creates a string representation of this object based on the IFormatProvider
        /// passed in.  If the provider is null, the CurrentCulture is used.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public string ToString(IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.

            string format = isFromScRgb ? c_scRgbFormat : null;

            return ConvertToString(format, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string
        /// and IFormatProvider passed in.
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        string IFormattable.ToString(string format, IFormatProvider provider)
        {
            // Delegate to the internal method which implements all ToString calls.
            return ConvertToString(format, provider);
        }

        /// <summary>
        /// Creates a string representation of this object based on the format string 
        /// and IFormatProvider passed in.  
        /// If the provider is null, the CurrentCulture is used.
        /// See the documentation for IFormattable for more information.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        internal string ConvertToString(string format, IFormatProvider provider)
        {
            if (format == null)
            {
                return string.Create(provider, stackalloc char[128], $"#{this.sRgbColor.a:X2}{this.sRgbColor.r:X2}{this.sRgbColor.g:X2}{this.sRgbColor.b:X2}");
            }
            else
            {
                // Helper to get the numeric list separator for a given culture.
                char separator = SimTokenizerHelper.GetNumericListSeparator(provider);
                return string.Format(provider,
                    $"sc#{{1:{format}}}{{0}} {{{format}}}{{0}} {{3:{format}}}{{0}} {{4:{format}}}",
                    separator, scRgbColor.a, scRgbColor.r, scRgbColor.g, scRgbColor.b);
            }
        }

        ///<summary>
        /// Clamp - the color channels to the gamut [0..1].  If a channel is out
        /// of gamut, it will be set to 1, which represents full saturation.
        /// We need to sync up context values if they exist
        ///</summary>
        public void Clamp()
        {
            scRgbColor.r = (scRgbColor.r < 0) ? 0 : (scRgbColor.r > 1.0f) ? 1.0f : scRgbColor.r;
            scRgbColor.g = (scRgbColor.g < 0) ? 0 : (scRgbColor.g > 1.0f) ? 1.0f : scRgbColor.g;
            scRgbColor.b = (scRgbColor.b < 0) ? 0 : (scRgbColor.b > 1.0f) ? 1.0f : scRgbColor.b;
            scRgbColor.a = (scRgbColor.a < 0) ? 0 : (scRgbColor.a > 1.0f) ? 1.0f : scRgbColor.a;
            sRgbColor.a = (byte)(scRgbColor.a * 255f);
            sRgbColor.r = ScRgbTosRgb(scRgbColor.r);
            sRgbColor.g = ScRgbTosRgb(scRgbColor.g);
            sRgbColor.b = ScRgbTosRgb(scRgbColor.b);

            //add code to check if context is null and if not null then clamp native values
        }

        #endregion Public Methods

        //------------------------------------------------------
        //
        //  Public Operators
        //
        //------------------------------------------------------

        #region Public Operators
        ///<summary>
        /// Addition operator - Adds each channel of the second color to each channel of the
        /// first and returns the result
        ///</summary>
        public static SimColor operator +(SimColor color1, SimColor color2)
        {
            SimColor c1 = FromScRgb(
                  color1.scRgbColor.a + color2.scRgbColor.a,
                  color1.scRgbColor.r + color2.scRgbColor.r,
                  color1.scRgbColor.g + color2.scRgbColor.g,
                  color1.scRgbColor.b + color2.scRgbColor.b);
            return c1;
        }

        ///<summary>
        /// Addition method - Adds each channel of the second color to each channel of the
        /// first and returns the result
        ///</summary>
        public static SimColor Add(SimColor color1, SimColor color2)
        {
            return (color1 + color2);
        }

        /// <summary>
        /// Subtract operator - substracts each channel of the second color from each channel of the
        /// first and returns the result
        /// </summary>
        /// <param name='color1'>The minuend</param>
        /// <param name='color2'>The subtrahend</param>
        /// <returns>Returns the unclamped differnce</returns>
        public static SimColor operator -(SimColor color1, SimColor color2)
        {
            SimColor c1 = FromScRgb(
                color1.scRgbColor.a - color2.scRgbColor.a,
                color1.scRgbColor.r - color2.scRgbColor.r,
                color1.scRgbColor.g - color2.scRgbColor.g,
                color1.scRgbColor.b - color2.scRgbColor.b
                );
            return c1;
        }

        ///<summary>
        /// Subtract method - subtracts each channel of the second color from each channel of the
        /// first and returns the result
        ///</summary>
        public static SimColor Subtract(SimColor color1, SimColor color2)
        {
            return (color1 - color2);
        }

        /// <summary>
        /// Multiplication operator - Multiplies each channel of the color by a coefficient and returns the result
        /// </summary>
        /// <param name='color'>The color</param>
        /// <param name='coefficient'>The coefficient</param>
        /// <returns>Returns the unclamped product</returns>
        public static SimColor operator *(SimColor color, float coefficient)
        {
            SimColor c1 = FromScRgb(color.scRgbColor.a * coefficient, color.scRgbColor.r * coefficient, color.scRgbColor.g * coefficient, color.scRgbColor.b * coefficient);

            return c1;
        }

        ///<summary>
        /// Multiplication method - Multiplies each channel of the color by a coefficient and returns the result
        ///</summary>
        public static SimColor Multiply(SimColor color, float coefficient)
        {
            return (color * coefficient);
        }

        ///<summary>
        /// Equality method for two colors - return true of colors are equal, otherwise returns false
        ///</summary>
        public static bool Equals(SimColor color1, SimColor color2)
        {
            return (color1 == color2);
        }

        /// <summary>
        /// Compares two colors for exact equality.  Note that float values can acquire error
        /// when operated upon, such that an exact comparison between two values which are logically
        /// equal may fail. see cref="AreClose" for a "fuzzy" version of this comparison.
        /// </summary>
        /// <param name='color'>The color to compare to "this"</param>
        /// <returns>Whether or not the two colors are equal</returns>
        public bool Equals(SimColor color)
        {
            return this == color;
        }

        /// <summary>
        /// Compares two colors for exact equality.  Note that float values can acquire error
        /// when operated upon, such that an exact comparison between two vEquals(color);alues which are logically
        /// equal may fail. see cref="AreClose" for a "fuzzy" version of this comparison.
        /// </summary>
        /// <param name='o'>The object to compare to "this"</param>
        /// <returns>Whether or not the two colors are equal</returns>
        public override bool Equals(object o)
        {
            if (o is SimColor)
            {
                SimColor color = (SimColor)o;

                return (this == color);
            }
            else
            {
                return false;
            }
        }

        ///<summary>
        /// IsEqual operator - Compares two colors for exact equality.  Note that float values can acquire error
        /// when operated upon, such that an exact comparison between two values which are logically
        /// equal may fail. see cref="AreClose".
        ///</summary>
        public static bool operator ==(SimColor color1, SimColor color2)
        {
            if (color1.scRgbColor.r != color2.scRgbColor.r)
            {
                return false;
            }

            if (color1.scRgbColor.g != color2.scRgbColor.g)
            {
                return false;
            }

            if (color1.scRgbColor.b != color2.scRgbColor.b)
            {
                return false;
            }

            if (color1.scRgbColor.a != color2.scRgbColor.a)
            {
                return false;
            }

            return true;
        }

        ///<summary>
        /// !=
        ///</summary>
        public static bool operator !=(SimColor color1, SimColor color2)
        {
            return (!(color1 == color2));
        }
        #endregion Public Operators

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        ///<summary>
        /// A
        ///</summary>
        public byte A
        {
            get
            {
                return sRgbColor.a;
            }
            set
            {
                scRgbColor.a = (float)value / 255.0f;
                sRgbColor.a = value;
            }
        }

        /// <value>The Red channel as a byte whose range is [0..255].
        /// the value is not allowed to be out of range</value>
        /// <summary>
        /// R
        /// </summary>
        public byte R
        {
            get
            {
                return sRgbColor.r;
            }
            set
            {
                scRgbColor.r = sRgbToScRgb(value);
                sRgbColor.r = value;
            }
        }

        ///<value>The Green channel as a byte whose range is [0..255].
        /// the value is not allowed to be out of range</value><summary>
        /// G
        ///</summary>
        public byte G
        {
            get
            {
                return sRgbColor.g;
            }
            set
            {
                scRgbColor.g = sRgbToScRgb(value);
                sRgbColor.g = value;
            }
        }

        ///<value>The Blue channel as a byte whose range is [0..255].
        /// the value is not allowed to be out of range</value><summary>
        /// B
        ///</summary>
        public byte B
        {
            get
            {
                return sRgbColor.b;
            }
            set
            {
                scRgbColor.b = sRgbToScRgb(value);
                sRgbColor.b = value;
            }
        }

        ///<value>The Alpha channel as a float whose range is [0..1].
        /// the value is allowed to be out of range</value><summary>
        /// ScA
        ///</summary>
        public float ScA
        {
            get
            {
                return scRgbColor.a;
            }
            set
            {
                scRgbColor.a = value;
                if (value < 0.0f)
                {
                    sRgbColor.a = 0;
                }
                else if (value > 1.0f)
                {
                    sRgbColor.a = (byte)255;
                }
                else
                {
                    sRgbColor.a = (byte)(value * 255f);
                }
            }
        }

        ///<value>The Red channel as a float whose range is [0..1].
        /// the value is allowed to be out of range</value>
        ///<summary>
        /// ScR
        ///</summary>
        public float ScR
        {
            get
            {
                return scRgbColor.r;
                // throw new ArgumentException(SR.Get(SRID.Color_ColorContextNotsRgb_or_ScRgb, null));
            }
            set
            {
                scRgbColor.r = value;
                sRgbColor.r = ScRgbTosRgb(value);
            }
        }

        ///<value>The Green channel as a float whose range is [0..1].
        /// the value is allowed to be out of range</value><summary>
        /// ScG
        ///</summary>
        public float ScG
        {
            get
            {
                return scRgbColor.g;
                // throw new ArgumentException(SR.Get(SRID.Color_ColorContextNotsRgb_or_ScRgb, null));
            }
            set
            {
                scRgbColor.g = value;
                sRgbColor.g = ScRgbTosRgb(value);
            }
        }

        ///<value>The Blue channel as a float whose range is [0..1].
        /// the value is allowed to be out of range</value><summary>
        /// ScB
        ///</summary>
        public float ScB
        {
            get
            {
                return scRgbColor.b;
                // throw new ArgumentException(SR.Get(SRID.Color_ColorContextNotsRgb_or_ScRgb, null));
            }
            set
            {
                scRgbColor.b = value;
                sRgbColor.b = ScRgbTosRgb(value);
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
        #region Private Methods

        ///<summary>
        /// private helper function to set context values from a color value with a set context and ScRgb values
        ///</summary>
        private static float sRgbToScRgb(byte bval)
        {
            float val = ((float)bval / 255.0f);

            if (!(val > 0.0))       // Handles NaN case too. (Though, NaN isn't actually
                                    // possible in this case.)
            {
                return (0.0f);
            }
            else if (val <= 0.04045)
            {
                return (val / 12.92f);
            }
            else if (val < 1.0f)
            {
                return (float)Math.Pow(((double)val + 0.055) / 1.055, 2.4);
            }
            else
            {
                return (1.0f);
            }
        }

        ///<summary>
        /// private helper function to set context values from a color value with a set context and ScRgb values
        ///</summary>
        ///
        private static byte ScRgbTosRgb(float val)
        {
            if (!(val > 0.0))       // Handles NaN case too
            {
                return (0);
            }
            else if (val <= 0.0031308)
            {
                return ((byte)((255.0f * val * 12.92f) + 0.5f));
            }
            else if (val < 1.0)
            {
                return ((byte)((255.0f * ((1.055f * (float)Math.Pow((double)val, (1.0 / 2.4))) - 0.055f)) + 0.5f));
            }
            else
            {
                return (255);
            }
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Events
        //
        //------------------------------------------------------
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private struct MILColorF // this structure is the "milrendertypes.h" structure and should be identical for performance
        {
            public float a, r, g, b;

            public override int GetHashCode()
            {
                return a.GetHashCode() ^ r.GetHashCode() ^ g.GetHashCode() ^ b.GetHashCode();
            }
            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }
        };

        private MILColorF scRgbColor;

        private struct MILColor
        {
            public byte a, r, g, b;
        }

        private MILColor sRgbColor;

        private bool isFromScRgb;

        private const string c_scRgbFormat = "R";

        #endregion Private Fields
    }
}
