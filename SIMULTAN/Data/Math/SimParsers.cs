// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Synopsis: Implements class Parsers for internal use of type converters
//

using System;
using System.ComponentModel;

namespace SIMULTAN.Data.SimMath
{
    internal static partial class SimParsers
    {
        private const int s_zeroChar = (int)'0';
        private const int s_aLower = (int)'a';
        private const int s_aUpper = (int)'A';

        static private int ParseHexChar(char c)
        {
            int intChar = (int)c;

            if ((intChar >= s_zeroChar) && (intChar <= (s_zeroChar + 9)))
            {
                return (intChar - s_zeroChar);
            }

            if ((intChar >= s_aLower) && (intChar <= (s_aLower + 5)))
            {
                return (intChar - s_aLower + 10);
            }

            if ((intChar >= s_aUpper) && (intChar <= (s_aUpper + 5)))
            {
                return (intChar - s_aUpper + 10);
            }
            throw new FormatException("Found illegal token while parsing color.");
        }

        static private SimColor ParseHexColor(string trimmedColor)
        {
            int a, r, g, b;
            a = 255;

            if (trimmedColor.Length > 7)
            {
                a = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
                r = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
                g = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
                b = ParseHexChar(trimmedColor[7]) * 16 + ParseHexChar(trimmedColor[8]);
            }
            else if (trimmedColor.Length > 5)
            {
                r = ParseHexChar(trimmedColor[1]) * 16 + ParseHexChar(trimmedColor[2]);
                g = ParseHexChar(trimmedColor[3]) * 16 + ParseHexChar(trimmedColor[4]);
                b = ParseHexChar(trimmedColor[5]) * 16 + ParseHexChar(trimmedColor[6]);
            }
            else if (trimmedColor.Length > 4)
            {
                a = ParseHexChar(trimmedColor[1]);
                a = a + a * 16;
                r = ParseHexChar(trimmedColor[2]);
                r = r + r * 16;
                g = ParseHexChar(trimmedColor[3]);
                g = g + g * 16;
                b = ParseHexChar(trimmedColor[4]);
                b = b + b * 16;
            }
            else
            {
                r = ParseHexChar(trimmedColor[1]);
                r = r + r * 16;
                g = ParseHexChar(trimmedColor[2]);
                g = g + g * 16;
                b = ParseHexChar(trimmedColor[3]);
                b = b + b * 16;
            }

            return (SimColor.FromArgb((byte)a, (byte)r, (byte)g, (byte)b));
        }

        internal const string s_ContextColor = "ContextColor ";
        internal const string s_ContextColorNoSpace = "ContextColor";

        static private SimColor ParseScRgbColor(string trimmedColor, IFormatProvider formatProvider)
        {
            if (!trimmedColor.StartsWith("sc#", StringComparison.Ordinal))
            {
                throw new FormatException("Found illegal token while parsing color.");
            }

            string tokens = trimmedColor.Substring(3, trimmedColor.Length - 3);

            // The tokenizer helper will tokenize a list based on the IFormatProvider.
            SimTokenizerHelper th = new SimTokenizerHelper(tokens, formatProvider);
            float[] values = new float[4];

            for (int i = 0; i < 3; i++)
            {
                values[i] = Convert.ToSingle(th.NextTokenRequired(), formatProvider);
            }

            if (th.NextToken())
            {
                values[3] = Convert.ToSingle(th.GetCurrentToken(), formatProvider);

                // We should be out of tokens at this point
                if (th.NextToken())
                {
                    throw new FormatException("Found illegal token while parsing color.");
                }

                return SimColor.FromScRgb(values[0], values[1], values[2], values[3]);
            }
            else
            {
                return SimColor.FromScRgb(1.0f, values[0], values[1], values[2]);
            }
        }

        /// <summary>
        /// ParseColor
        /// <param name="color"> string with color description </param>
        /// <param name="formatProvider">IFormatProvider for processing string</param>
        /// </summary>
        internal static SimColor ParseColor(string color, IFormatProvider formatProvider)
        {
            return ParseColor(color, formatProvider, null);
        }

        /// <summary>
        /// ParseColor
        /// <param name="color"> string with color description </param>
        /// <param name="formatProvider">IFormatProvider for processing string</param>
        /// <param name="context">ITypeDescriptorContext</param>
        /// </summary>
        internal static SimColor ParseColor(string color, IFormatProvider formatProvider, ITypeDescriptorContext context)
        {
            bool isPossibleKnowColor;
            bool isNumericColor;
            bool isScRgbColor;
            bool isContextColor;
            string trimmedColor = SimKnowncolors.MatchColor(color, out isPossibleKnowColor, out isNumericColor, out isContextColor, out isScRgbColor);

            if ((isPossibleKnowColor == false) &&
                (isNumericColor == false) &&
                (isScRgbColor == false) &&
                (isContextColor == false))
            {
                throw new FormatException("Found illegal token while parsing color.");
            }

            //Is it a number?
            if (isNumericColor)
            {
                return ParseHexColor(trimmedColor);
            }
            else if (isContextColor)
            {
                throw new NotSupportedException("Colors with context cannot be parsed");
            }
            else if (isScRgbColor)
            {
                return ParseScRgbColor(trimmedColor, formatProvider);
            }
            else
            {
                KnownColor kc = SimKnowncolors.ColorStringToKnownColor(trimmedColor);

                if (kc == KnownColor.UnknownColor)
                {
                    throw new FormatException("Found illegal token while parsing color.");
                }

                return SimColor.FromUInt32((uint)kc);
            }
        }

    }
}
