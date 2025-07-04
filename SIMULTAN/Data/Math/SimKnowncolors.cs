// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace SIMULTAN.Data.SimMath
{
    /// Enum containing handles to all known colors
    /// Since the first element is 0, second is 1, etc, we can use this to index
    /// directly into an array
    internal enum KnownColor : uint
    {
        // We've reserved the value "1" as unknown.  If for some odd reason "1" is added to the
        // list, redefined UnknownColor

        AliceBlue = 0xFFF0F8FF,
        AntiqueWhite = 0xFFFAEBD7,
        Aqua = 0xFF00FFFF,
        Aquamarine = 0xFF7FFFD4,
        Azure = 0xFFF0FFFF,
        Beige = 0xFFF5F5DC,
        Bisque = 0xFFFFE4C4,
        Black = 0xFF000000,
        BlanchedAlmond = 0xFFFFEBCD,
        Blue = 0xFF0000FF,
        BlueViolet = 0xFF8A2BE2,
        Brown = 0xFFA52A2A,
        BurlyWood = 0xFFDEB887,
        CadetBlue = 0xFF5F9EA0,
        Chartreuse = 0xFF7FFF00,
        Chocolate = 0xFFD2691E,
        Coral = 0xFFFF7F50,
        CornflowerBlue = 0xFF6495ED,
        Cornsilk = 0xFFFFF8DC,
        Crimson = 0xFFDC143C,
        Cyan = 0xFF00FFFF,
        DarkBlue = 0xFF00008B,
        DarkCyan = 0xFF008B8B,
        DarkGoldenrod = 0xFFB8860B,
        DarkGray = 0xFFA9A9A9,
        DarkGreen = 0xFF006400,
        DarkKhaki = 0xFFBDB76B,
        DarkMagenta = 0xFF8B008B,
        DarkOliveGreen = 0xFF556B2F,
        DarkOrange = 0xFFFF8C00,
        DarkOrchid = 0xFF9932CC,
        DarkRed = 0xFF8B0000,
        DarkSalmon = 0xFFE9967A,
        DarkSeaGreen = 0xFF8FBC8F,
        DarkSlateBlue = 0xFF483D8B,
        DarkSlateGray = 0xFF2F4F4F,
        DarkTurquoise = 0xFF00CED1,
        DarkViolet = 0xFF9400D3,
        DeepPink = 0xFFFF1493,
        DeepSkyBlue = 0xFF00BFFF,
        DimGray = 0xFF696969,
        DodgerBlue = 0xFF1E90FF,
        Firebrick = 0xFFB22222,
        FloralWhite = 0xFFFFFAF0,
        ForestGreen = 0xFF228B22,
        Fuchsia = 0xFFFF00FF,
        Gainsboro = 0xFFDCDCDC,
        GhostWhite = 0xFFF8F8FF,
        Gold = 0xFFFFD700,
        Goldenrod = 0xFFDAA520,
        Gray = 0xFF808080,
        Green = 0xFF008000,
        GreenYellow = 0xFFADFF2F,
        Honeydew = 0xFFF0FFF0,
        HotPink = 0xFFFF69B4,
        IndianRed = 0xFFCD5C5C,
        Indigo = 0xFF4B0082,
        Ivory = 0xFFFFFFF0,
        Khaki = 0xFFF0E68C,
        Lavender = 0xFFE6E6FA,
        LavenderBlush = 0xFFFFF0F5,
        LawnGreen = 0xFF7CFC00,
        LemonChiffon = 0xFFFFFACD,
        LightBlue = 0xFFADD8E6,
        LightCoral = 0xFFF08080,
        LightCyan = 0xFFE0FFFF,
        LightGoldenrodYellow = 0xFFFAFAD2,
        LightGreen = 0xFF90EE90,
        LightGray = 0xFFD3D3D3,
        LightPink = 0xFFFFB6C1,
        LightSalmon = 0xFFFFA07A,
        LightSeaGreen = 0xFF20B2AA,
        LightSkyBlue = 0xFF87CEFA,
        LightSlateGray = 0xFF778899,
        LightSteelBlue = 0xFFB0C4DE,
        LightYellow = 0xFFFFFFE0,
        Lime = 0xFF00FF00,
        LimeGreen = 0xFF32CD32,
        Linen = 0xFFFAF0E6,
        Magenta = 0xFFFF00FF,
        Maroon = 0xFF800000,
        MediumAquamarine = 0xFF66CDAA,
        MediumBlue = 0xFF0000CD,
        MediumOrchid = 0xFFBA55D3,
        MediumPurple = 0xFF9370DB,
        MediumSeaGreen = 0xFF3CB371,
        MediumSlateBlue = 0xFF7B68EE,
        MediumSpringGreen = 0xFF00FA9A,
        MediumTurquoise = 0xFF48D1CC,
        MediumVioletRed = 0xFFC71585,
        MidnightBlue = 0xFF191970,
        MintCream = 0xFFF5FFFA,
        MistyRose = 0xFFFFE4E1,
        Moccasin = 0xFFFFE4B5,
        NavajoWhite = 0xFFFFDEAD,
        Navy = 0xFF000080,
        OldLace = 0xFFFDF5E6,
        Olive = 0xFF808000,
        OliveDrab = 0xFF6B8E23,
        Orange = 0xFFFFA500,
        OrangeRed = 0xFFFF4500,
        Orchid = 0xFFDA70D6,
        PaleGoldenrod = 0xFFEEE8AA,
        PaleGreen = 0xFF98FB98,
        PaleTurquoise = 0xFFAFEEEE,
        PaleVioletRed = 0xFFDB7093,
        PapayaWhip = 0xFFFFEFD5,
        PeachPuff = 0xFFFFDAB9,
        Peru = 0xFFCD853F,
        Pink = 0xFFFFC0CB,
        Plum = 0xFFDDA0DD,
        PowderBlue = 0xFFB0E0E6,
        Purple = 0xFF800080,
        Red = 0xFFFF0000,
        RosyBrown = 0xFFBC8F8F,
        RoyalBlue = 0xFF4169E1,
        SaddleBrown = 0xFF8B4513,
        Salmon = 0xFFFA8072,
        SandyBrown = 0xFFF4A460,
        SeaGreen = 0xFF2E8B57,
        SeaShell = 0xFFFFF5EE,
        Sienna = 0xFFA0522D,
        Silver = 0xFFC0C0C0,
        SkyBlue = 0xFF87CEEB,
        SlateBlue = 0xFF6A5ACD,
        SlateGray = 0xFF708090,
        Snow = 0xFFFFFAFA,
        SpringGreen = 0xFF00FF7F,
        SteelBlue = 0xFF4682B4,
        Tan = 0xFFD2B48C,
        Teal = 0xFF008080,
        Thistle = 0xFFD8BFD8,
        Tomato = 0xFFFF6347,
        Transparent = 0x00FFFFFF,
        Turquoise = 0xFF40E0D0,
        Violet = 0xFFEE82EE,
        Wheat = 0xFFF5DEB3,
        White = 0xFFFFFFFF,
        WhiteSmoke = 0xFFF5F5F5,
        Yellow = 0xFFFFFF00,
        YellowGreen = 0xFF9ACD32,
        UnknownColor = 0x00000001
    }

    internal static class SimKnowncolors
    {
#if !PBTCOMPILER

        static SimKnowncolors()
        {
            Array knownColorValues = Enum.GetValues(typeof(KnownColor));
            foreach (KnownColor colorValue in knownColorValues)
            {
                string aRGBString = String.Format("#{0,8:X8}", (uint)colorValue);
                s_knownArgbColors[aRGBString] = colorValue;
            }
        }

        static internal string MatchColor(string colorString, out bool isKnownColor, out bool isNumericColor, out bool isContextColor, out bool isScRgbColor)
        {
            string trimmedString = colorString.Trim();

            if (((trimmedString.Length == 4) ||
                (trimmedString.Length == 5) ||
                (trimmedString.Length == 7) ||
                (trimmedString.Length == 9)) &&
                (trimmedString[0] == '#'))
            {
                isNumericColor = true;
                isScRgbColor = false;
                isKnownColor = false;
                isContextColor = false;
                return trimmedString;
            }
            else
                isNumericColor = false;

            if ((trimmedString.StartsWith("sc#", StringComparison.Ordinal) == true))
            {
                isNumericColor = false;
                isScRgbColor = true;
                isKnownColor = false;
                isContextColor = false;
            }
            else
            {
                isScRgbColor = false;
            }

            if ((trimmedString.StartsWith(SimParsers.s_ContextColor, StringComparison.OrdinalIgnoreCase) == true))
            {
                isContextColor = true;
                isScRgbColor = false;
                isKnownColor = false;
                return trimmedString;
            }
            else
            {
                isContextColor = false;
                isKnownColor = true;
            }

            return trimmedString;
        }
#endif

        /// Return the KnownColor from a color string.  If there's no match, KnownColor.UnknownColor
        internal static KnownColor ColorStringToKnownColor(string colorString)
        {
            if (null != colorString)
            {
                // We use invariant culture because we don't globalize our color names
                string colorUpper = colorString.ToUpper(System.Globalization.CultureInfo.InvariantCulture);

                // Use String.Equals because it does explicit equality
                // StartsWith/EndsWith are culture sensitive and are 4-7 times slower than Equals

                switch (colorUpper.Length)
                {
                    case 3:
                        if (colorUpper.Equals("RED")) return KnownColor.Red;
                        if (colorUpper.Equals("TAN")) return KnownColor.Tan;
                        break;
                    case 4:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("AQUA")) return KnownColor.Aqua;
                                break;
                            case 'B':
                                if (colorUpper.Equals("BLUE")) return KnownColor.Blue;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CYAN")) return KnownColor.Cyan;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GOLD")) return KnownColor.Gold;
                                if (colorUpper.Equals("GRAY")) return KnownColor.Gray;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIME")) return KnownColor.Lime;
                                break;
                            case 'N':
                                if (colorUpper.Equals("NAVY")) return KnownColor.Navy;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PERU")) return KnownColor.Peru;
                                if (colorUpper.Equals("PINK")) return KnownColor.Pink;
                                if (colorUpper.Equals("PLUM")) return KnownColor.Plum;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SNOW")) return KnownColor.Snow;
                                break;
                            case 'T':
                                if (colorUpper.Equals("TEAL")) return KnownColor.Teal;
                                break;
                        }
                        break;
                    case 5:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("AZURE")) return KnownColor.Azure;
                                break;
                            case 'B':
                                if (colorUpper.Equals("BEIGE")) return KnownColor.Beige;
                                if (colorUpper.Equals("BLACK")) return KnownColor.Black;
                                if (colorUpper.Equals("BROWN")) return KnownColor.Brown;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CORAL")) return KnownColor.Coral;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GREEN")) return KnownColor.Green;
                                break;
                            case 'I':
                                if (colorUpper.Equals("IVORY")) return KnownColor.Ivory;
                                break;
                            case 'K':
                                if (colorUpper.Equals("KHAKI")) return KnownColor.Khaki;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LINEN")) return KnownColor.Linen;
                                break;
                            case 'O':
                                if (colorUpper.Equals("OLIVE")) return KnownColor.Olive;
                                break;
                            case 'W':
                                if (colorUpper.Equals("WHEAT")) return KnownColor.Wheat;
                                if (colorUpper.Equals("WHITE")) return KnownColor.White;
                                break;
                        }
                        break;
                    case 6:
                        switch (colorUpper[0])
                        {
                            case 'B':
                                if (colorUpper.Equals("BISQUE")) return KnownColor.Bisque;
                                break;
                            case 'I':
                                if (colorUpper.Equals("INDIGO")) return KnownColor.Indigo;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MAROON")) return KnownColor.Maroon;
                                break;
                            case 'O':
                                if (colorUpper.Equals("ORANGE")) return KnownColor.Orange;
                                if (colorUpper.Equals("ORCHID")) return KnownColor.Orchid;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PURPLE")) return KnownColor.Purple;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SALMON")) return KnownColor.Salmon;
                                if (colorUpper.Equals("SIENNA")) return KnownColor.Sienna;
                                if (colorUpper.Equals("SILVER")) return KnownColor.Silver;
                                break;
                            case 'T':
                                if (colorUpper.Equals("TOMATO")) return KnownColor.Tomato;
                                break;
                            case 'V':
                                if (colorUpper.Equals("VIOLET")) return KnownColor.Violet;
                                break;
                            case 'Y':
                                if (colorUpper.Equals("YELLOW")) return KnownColor.Yellow;
                                break;
                        }
                        break;
                    case 7:
                        switch (colorUpper[0])
                        {
                            case 'C':
                                if (colorUpper.Equals("CRIMSON")) return KnownColor.Crimson;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKRED")) return KnownColor.DarkRed;
                                if (colorUpper.Equals("DIMGRAY")) return KnownColor.DimGray;
                                break;
                            case 'F':
                                if (colorUpper.Equals("FUCHSIA")) return KnownColor.Fuchsia;
                                break;
                            case 'H':
                                if (colorUpper.Equals("HOTPINK")) return KnownColor.HotPink;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MAGENTA")) return KnownColor.Magenta;
                                break;
                            case 'O':
                                if (colorUpper.Equals("OLDLACE")) return KnownColor.OldLace;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SKYBLUE")) return KnownColor.SkyBlue;
                                break;
                            case 'T':
                                if (colorUpper.Equals("THISTLE")) return KnownColor.Thistle;
                                break;
                        }
                        break;
                    case 8:
                        switch (colorUpper[0])
                        {
                            case 'C':
                                if (colorUpper.Equals("CORNSILK")) return KnownColor.Cornsilk;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKBLUE")) return KnownColor.DarkBlue;
                                if (colorUpper.Equals("DARKCYAN")) return KnownColor.DarkCyan;
                                if (colorUpper.Equals("DARKGRAY")) return KnownColor.DarkGray;
                                if (colorUpper.Equals("DEEPPINK")) return KnownColor.DeepPink;
                                break;
                            case 'H':
                                if (colorUpper.Equals("HONEYDEW")) return KnownColor.Honeydew;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LAVENDER")) return KnownColor.Lavender;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MOCCASIN")) return KnownColor.Moccasin;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SEAGREEN")) return KnownColor.SeaGreen;
                                if (colorUpper.Equals("SEASHELL")) return KnownColor.SeaShell;
                                break;
                        }
                        break;
                    case 9:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("ALICEBLUE")) return KnownColor.AliceBlue;
                                break;
                            case 'B':
                                if (colorUpper.Equals("BURLYWOOD")) return KnownColor.BurlyWood;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CADETBLUE")) return KnownColor.CadetBlue;
                                if (colorUpper.Equals("CHOCOLATE")) return KnownColor.Chocolate;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKGREEN")) return KnownColor.DarkGreen;
                                if (colorUpper.Equals("DARKKHAKI")) return KnownColor.DarkKhaki;
                                break;
                            case 'F':
                                if (colorUpper.Equals("FIREBRICK")) return KnownColor.Firebrick;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GAINSBORO")) return KnownColor.Gainsboro;
                                if (colorUpper.Equals("GOLDENROD")) return KnownColor.Goldenrod;
                                break;
                            case 'I':
                                if (colorUpper.Equals("INDIANRED")) return KnownColor.IndianRed;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LAWNGREEN")) return KnownColor.LawnGreen;
                                if (colorUpper.Equals("LIGHTBLUE")) return KnownColor.LightBlue;
                                if (colorUpper.Equals("LIGHTCYAN")) return KnownColor.LightCyan;
                                if (colorUpper.Equals("LIGHTGRAY")) return KnownColor.LightGray;
                                if (colorUpper.Equals("LIGHTPINK")) return KnownColor.LightPink;
                                if (colorUpper.Equals("LIMEGREEN")) return KnownColor.LimeGreen;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MINTCREAM")) return KnownColor.MintCream;
                                if (colorUpper.Equals("MISTYROSE")) return KnownColor.MistyRose;
                                break;
                            case 'O':
                                if (colorUpper.Equals("OLIVEDRAB")) return KnownColor.OliveDrab;
                                if (colorUpper.Equals("ORANGERED")) return KnownColor.OrangeRed;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PALEGREEN")) return KnownColor.PaleGreen;
                                if (colorUpper.Equals("PEACHPUFF")) return KnownColor.PeachPuff;
                                break;
                            case 'R':
                                if (colorUpper.Equals("ROSYBROWN")) return KnownColor.RosyBrown;
                                if (colorUpper.Equals("ROYALBLUE")) return KnownColor.RoyalBlue;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SLATEBLUE")) return KnownColor.SlateBlue;
                                if (colorUpper.Equals("SLATEGRAY")) return KnownColor.SlateGray;
                                if (colorUpper.Equals("STEELBLUE")) return KnownColor.SteelBlue;
                                break;
                            case 'T':
                                if (colorUpper.Equals("TURQUOISE")) return KnownColor.Turquoise;
                                break;
                        }
                        break;
                    case 10:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("AQUAMARINE")) return KnownColor.Aquamarine;
                                break;
                            case 'B':
                                if (colorUpper.Equals("BLUEVIOLET")) return KnownColor.BlueViolet;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CHARTREUSE")) return KnownColor.Chartreuse;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKORANGE")) return KnownColor.DarkOrange;
                                if (colorUpper.Equals("DARKORCHID")) return KnownColor.DarkOrchid;
                                if (colorUpper.Equals("DARKSALMON")) return KnownColor.DarkSalmon;
                                if (colorUpper.Equals("DARKVIOLET")) return KnownColor.DarkViolet;
                                if (colorUpper.Equals("DODGERBLUE")) return KnownColor.DodgerBlue;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GHOSTWHITE")) return KnownColor.GhostWhite;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTCORAL")) return KnownColor.LightCoral;
                                if (colorUpper.Equals("LIGHTGREEN")) return KnownColor.LightGreen;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MEDIUMBLUE")) return KnownColor.MediumBlue;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PAPAYAWHIP")) return KnownColor.PapayaWhip;
                                if (colorUpper.Equals("POWDERBLUE")) return KnownColor.PowderBlue;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SANDYBROWN")) return KnownColor.SandyBrown;
                                break;
                            case 'W':
                                if (colorUpper.Equals("WHITESMOKE")) return KnownColor.WhiteSmoke;
                                break;
                        }
                        break;
                    case 11:
                        switch (colorUpper[0])
                        {
                            case 'D':
                                if (colorUpper.Equals("DARKMAGENTA")) return KnownColor.DarkMagenta;
                                if (colorUpper.Equals("DEEPSKYBLUE")) return KnownColor.DeepSkyBlue;
                                break;
                            case 'F':
                                if (colorUpper.Equals("FLORALWHITE")) return KnownColor.FloralWhite;
                                if (colorUpper.Equals("FORESTGREEN")) return KnownColor.ForestGreen;
                                break;
                            case 'G':
                                if (colorUpper.Equals("GREENYELLOW")) return KnownColor.GreenYellow;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTSALMON")) return KnownColor.LightSalmon;
                                if (colorUpper.Equals("LIGHTYELLOW")) return KnownColor.LightYellow;
                                break;
                            case 'N':
                                if (colorUpper.Equals("NAVAJOWHITE")) return KnownColor.NavajoWhite;
                                break;
                            case 'S':
                                if (colorUpper.Equals("SADDLEBROWN")) return KnownColor.SaddleBrown;
                                if (colorUpper.Equals("SPRINGGREEN")) return KnownColor.SpringGreen;
                                break;
                            case 'T':
                                if (colorUpper.Equals("TRANSPARENT")) return KnownColor.Transparent;
                                break;
                            case 'Y':
                                if (colorUpper.Equals("YELLOWGREEN")) return KnownColor.YellowGreen;
                                break;
                        }
                        break;
                    case 12:
                        switch (colorUpper[0])
                        {
                            case 'A':
                                if (colorUpper.Equals("ANTIQUEWHITE")) return KnownColor.AntiqueWhite;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKSEAGREEN")) return KnownColor.DarkSeaGreen;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTSKYBLUE")) return KnownColor.LightSkyBlue;
                                if (colorUpper.Equals("LEMONCHIFFON")) return KnownColor.LemonChiffon;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MEDIUMORCHID")) return KnownColor.MediumOrchid;
                                if (colorUpper.Equals("MEDIUMPURPLE")) return KnownColor.MediumPurple;
                                if (colorUpper.Equals("MIDNIGHTBLUE")) return KnownColor.MidnightBlue;
                                break;
                        }
                        break;
                    case 13:
                        switch (colorUpper[0])
                        {
                            case 'D':
                                if (colorUpper.Equals("DARKSLATEBLUE")) return KnownColor.DarkSlateBlue;
                                if (colorUpper.Equals("DARKSLATEGRAY")) return KnownColor.DarkSlateGray;
                                if (colorUpper.Equals("DARKGOLDENROD")) return KnownColor.DarkGoldenrod;
                                if (colorUpper.Equals("DARKTURQUOISE")) return KnownColor.DarkTurquoise;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTSEAGREEN")) return KnownColor.LightSeaGreen;
                                if (colorUpper.Equals("LAVENDERBLUSH")) return KnownColor.LavenderBlush;
                                break;
                            case 'P':
                                if (colorUpper.Equals("PALEGOLDENROD")) return KnownColor.PaleGoldenrod;
                                if (colorUpper.Equals("PALETURQUOISE")) return KnownColor.PaleTurquoise;
                                if (colorUpper.Equals("PALEVIOLETRED")) return KnownColor.PaleVioletRed;
                                break;
                        }
                        break;
                    case 14:
                        switch (colorUpper[0])
                        {
                            case 'B':
                                if (colorUpper.Equals("BLANCHEDALMOND")) return KnownColor.BlanchedAlmond;
                                break;
                            case 'C':
                                if (colorUpper.Equals("CORNFLOWERBLUE")) return KnownColor.CornflowerBlue;
                                break;
                            case 'D':
                                if (colorUpper.Equals("DARKOLIVEGREEN")) return KnownColor.DarkOliveGreen;
                                break;
                            case 'L':
                                if (colorUpper.Equals("LIGHTSLATEGRAY")) return KnownColor.LightSlateGray;
                                if (colorUpper.Equals("LIGHTSTEELBLUE")) return KnownColor.LightSteelBlue;
                                break;
                            case 'M':
                                if (colorUpper.Equals("MEDIUMSEAGREEN")) return KnownColor.MediumSeaGreen;
                                break;
                        }
                        break;
                    case 15:
                        if (colorUpper.Equals("MEDIUMSLATEBLUE")) return KnownColor.MediumSlateBlue;
                        if (colorUpper.Equals("MEDIUMTURQUOISE")) return KnownColor.MediumTurquoise;
                        if (colorUpper.Equals("MEDIUMVIOLETRED")) return KnownColor.MediumVioletRed;
                        break;
                    case 16:
                        if (colorUpper.Equals("MEDIUMAQUAMARINE")) return KnownColor.MediumAquamarine;
                        break;
                    case 17:
                        if (colorUpper.Equals("MEDIUMSPRINGGREEN")) return KnownColor.MediumSpringGreen;
                        break;
                    case 20:
                        if (colorUpper.Equals("LIGHTGOLDENRODYELLOW")) return KnownColor.LightGoldenrodYellow;
                        break;
                }
            }
            // colorString was null or not found
            return KnownColor.UnknownColor;
        }

#if !PBTCOMPILER
        internal static KnownColor ArgbStringToKnownColor(string argbString)
        {
            string argbUpper = argbString.Trim().ToUpper(System.Globalization.CultureInfo.InvariantCulture);

            KnownColor color;
            if (s_knownArgbColors.TryGetValue(argbUpper, out color))
                return color;

            return KnownColor.UnknownColor;
        }

        private static Dictionary<string, KnownColor> s_knownArgbColors = new Dictionary<string, KnownColor>();
#endif
    }

#if !PBTCOMPILER
    /// <summary>
    /// Colors - A collection of well-known Colors
    /// </summary>
    public sealed class SimColors
    {
        #region Constructors

        // Colors only has static members, so it shouldn't be constructable.
        private SimColors()
        {
        }

        #endregion Constructors

        #region static Known Colors

        /// <summary>
        /// Well-known color: AliceBlue
        /// </summary>
        public static SimColor AliceBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.AliceBlue);
            }
        }

        /// <summary>
        /// Well-known color: AntiqueWhite
        /// </summary>
        public static SimColor AntiqueWhite
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.AntiqueWhite);
            }
        }

        /// <summary>
        /// Well-known color: Aqua
        /// </summary>
        public static SimColor Aqua
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Aqua);
            }
        }

        /// <summary>
        /// Well-known color: Aquamarine
        /// </summary>
        public static SimColor Aquamarine
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Aquamarine);
            }
        }

        /// <summary>
        /// Well-known color: Azure
        /// </summary>
        public static SimColor Azure
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Azure);
            }
        }

        /// <summary>
        /// Well-known color: Beige
        /// </summary>
        public static SimColor Beige
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Beige);
            }
        }

        /// <summary>
        /// Well-known color: Bisque
        /// </summary>
        public static SimColor Bisque
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Bisque);
            }
        }

        /// <summary>
        /// Well-known color: Black
        /// </summary>
        public static SimColor Black
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Black);
            }
        }

        /// <summary>
        /// Well-known color: BlanchedAlmond
        /// </summary>
        public static SimColor BlanchedAlmond
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.BlanchedAlmond);
            }
        }

        /// <summary>
        /// Well-known color: Blue
        /// </summary>
        public static SimColor Blue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Blue);
            }
        }

        /// <summary>
        /// Well-known color: BlueViolet
        /// </summary>
        public static SimColor BlueViolet
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.BlueViolet);
            }
        }

        /// <summary>
        /// Well-known color: Brown
        /// </summary>
        public static SimColor Brown
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Brown);
            }
        }

        /// <summary>
        /// Well-known color: BurlyWood
        /// </summary>
        public static SimColor BurlyWood
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.BurlyWood);
            }
        }

        /// <summary>
        /// Well-known color: CadetBlue
        /// </summary>
        public static SimColor CadetBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.CadetBlue);
            }
        }

        /// <summary>
        /// Well-known color: Chartreuse
        /// </summary>
        public static SimColor Chartreuse
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Chartreuse);
            }
        }

        /// <summary>
        /// Well-known color: Chocolate
        /// </summary>
        public static SimColor Chocolate
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Chocolate);
            }
        }

        /// <summary>
        /// Well-known color: Coral
        /// </summary>
        public static SimColor Coral
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Coral);
            }
        }

        /// <summary>
        /// Well-known color: CornflowerBlue
        /// </summary>
        public static SimColor CornflowerBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.CornflowerBlue);
            }
        }

        /// <summary>
        /// Well-known color: Cornsilk
        /// </summary>
        public static SimColor Cornsilk
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Cornsilk);
            }
        }

        /// <summary>
        /// Well-known color: Crimson
        /// </summary>
        public static SimColor Crimson
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Crimson);
            }
        }

        /// <summary>
        /// Well-known color: Cyan
        /// </summary>
        public static SimColor Cyan
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Cyan);
            }
        }

        /// <summary>
        /// Well-known color: DarkBlue
        /// </summary>
        public static SimColor DarkBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkBlue);
            }
        }

        /// <summary>
        /// Well-known color: DarkCyan
        /// </summary>
        public static SimColor DarkCyan
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkCyan);
            }
        }

        /// <summary>
        /// Well-known color: DarkGoldenrod
        /// </summary>
        public static SimColor DarkGoldenrod
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkGoldenrod);
            }
        }

        /// <summary>
        /// Well-known color: DarkGray
        /// </summary>
        public static SimColor DarkGray
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkGray);
            }
        }

        /// <summary>
        /// Well-known color: DarkGreen
        /// </summary>
        public static SimColor DarkGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkGreen);
            }
        }

        /// <summary>
        /// Well-known color: DarkKhaki
        /// </summary>
        public static SimColor DarkKhaki
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkKhaki);
            }
        }

        /// <summary>
        /// Well-known color: DarkMagenta
        /// </summary>
        public static SimColor DarkMagenta
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkMagenta);
            }
        }

        /// <summary>
        /// Well-known color: DarkOliveGreen
        /// </summary>
        public static SimColor DarkOliveGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkOliveGreen);
            }
        }

        /// <summary>
        /// Well-known color: DarkOrange
        /// </summary>
        public static SimColor DarkOrange
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkOrange);
            }
        }

        /// <summary>
        /// Well-known color: DarkOrchid
        /// </summary>
        public static SimColor DarkOrchid
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkOrchid);
            }
        }

        /// <summary>
        /// Well-known color: DarkRed
        /// </summary>
        public static SimColor DarkRed
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkRed);
            }
        }

        /// <summary>
        /// Well-known color: DarkSalmon
        /// </summary>
        public static SimColor DarkSalmon
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkSalmon);
            }
        }

        /// <summary>
        /// Well-known color: DarkSeaGreen
        /// </summary>
        public static SimColor DarkSeaGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkSeaGreen);
            }
        }

        /// <summary>
        /// Well-known color: DarkSlateBlue
        /// </summary>
        public static SimColor DarkSlateBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkSlateBlue);
            }
        }

        /// <summary>
        /// Well-known color: DarkSlateGray
        /// </summary>
        public static SimColor DarkSlateGray
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkSlateGray);
            }
        }

        /// <summary>
        /// Well-known color: DarkTurquoise
        /// </summary>
        public static SimColor DarkTurquoise
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkTurquoise);
            }
        }

        /// <summary>
        /// Well-known color: DarkViolet
        /// </summary>
        public static SimColor DarkViolet
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DarkViolet);
            }
        }

        /// <summary>
        /// Well-known color: DeepPink
        /// </summary>
        public static SimColor DeepPink
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DeepPink);
            }
        }

        /// <summary>
        /// Well-known color: DeepSkyBlue
        /// </summary>
        public static SimColor DeepSkyBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DeepSkyBlue);
            }
        }

        /// <summary>
        /// Well-known color: DimGray
        /// </summary>
        public static SimColor DimGray
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DimGray);
            }
        }

        /// <summary>
        /// Well-known color: DodgerBlue
        /// </summary>
        public static SimColor DodgerBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.DodgerBlue);
            }
        }

        /// <summary>
        /// Well-known color: Firebrick
        /// </summary>
        public static SimColor Firebrick
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Firebrick);
            }
        }

        /// <summary>
        /// Well-known color: FloralWhite
        /// </summary>
        public static SimColor FloralWhite
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.FloralWhite);
            }
        }

        /// <summary>
        /// Well-known color: ForestGreen
        /// </summary>
        public static SimColor ForestGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.ForestGreen);
            }
        }

        /// <summary>
        /// Well-known color: Fuchsia
        /// </summary>
        public static SimColor Fuchsia
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Fuchsia);
            }
        }

        /// <summary>
        /// Well-known color: Gainsboro
        /// </summary>
        public static SimColor Gainsboro
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Gainsboro);
            }
        }

        /// <summary>
        /// Well-known color: GhostWhite
        /// </summary>
        public static SimColor GhostWhite
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.GhostWhite);
            }
        }

        /// <summary>
        /// Well-known color: Gold
        /// </summary>
        public static SimColor Gold
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Gold);
            }
        }

        /// <summary>
        /// Well-known color: Goldenrod
        /// </summary>
        public static SimColor Goldenrod
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Goldenrod);
            }
        }

        /// <summary>
        /// Well-known color: Gray
        /// </summary>
        public static SimColor Gray
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Gray);
            }
        }

        /// <summary>
        /// Well-known color: Green
        /// </summary>
        public static SimColor Green
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Green);
            }
        }

        /// <summary>
        /// Well-known color: GreenYellow
        /// </summary>
        public static SimColor GreenYellow
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.GreenYellow);
            }
        }

        /// <summary>
        /// Well-known color: Honeydew
        /// </summary>
        public static SimColor Honeydew
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Honeydew);
            }
        }

        /// <summary>
        /// Well-known color: HotPink
        /// </summary>
        public static SimColor HotPink
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.HotPink);
            }
        }

        /// <summary>
        /// Well-known color: IndianRed
        /// </summary>
        public static SimColor IndianRed
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.IndianRed);
            }
        }

        /// <summary>
        /// Well-known color: Indigo
        /// </summary>
        public static SimColor Indigo
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Indigo);
            }
        }

        /// <summary>
        /// Well-known color: Ivory
        /// </summary>
        public static SimColor Ivory
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Ivory);
            }
        }

        /// <summary>
        /// Well-known color: Khaki
        /// </summary>
        public static SimColor Khaki
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Khaki);
            }
        }

        /// <summary>
        /// Well-known color: Lavender
        /// </summary>
        public static SimColor Lavender
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Lavender);
            }
        }

        /// <summary>
        /// Well-known color: LavenderBlush
        /// </summary>
        public static SimColor LavenderBlush
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LavenderBlush);
            }
        }

        /// <summary>
        /// Well-known color: LawnGreen
        /// </summary>
        public static SimColor LawnGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LawnGreen);
            }
        }

        /// <summary>
        /// Well-known color: LemonChiffon
        /// </summary>
        public static SimColor LemonChiffon
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LemonChiffon);
            }
        }

        /// <summary>
        /// Well-known color: LightBlue
        /// </summary>
        public static SimColor LightBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightBlue);
            }
        }

        /// <summary>
        /// Well-known color: LightCoral
        /// </summary>
        public static SimColor LightCoral
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightCoral);
            }
        }

        /// <summary>
        /// Well-known color: LightCyan
        /// </summary>
        public static SimColor LightCyan
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightCyan);
            }
        }

        /// <summary>
        /// Well-known color: LightGoldenrodYellow
        /// </summary>
        public static SimColor LightGoldenrodYellow
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightGoldenrodYellow);
            }
        }

        /// <summary>
        /// Well-known color: LightGray
        /// </summary>
        public static SimColor LightGray
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightGray);
            }
        }

        /// <summary>
        /// Well-known color: LightGreen
        /// </summary>
        public static SimColor LightGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightGreen);
            }
        }

        /// <summary>
        /// Well-known color: LightPink
        /// </summary>
        public static SimColor LightPink
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightPink);
            }
        }

        /// <summary>
        /// Well-known color: LightSalmon
        /// </summary>
        public static SimColor LightSalmon
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightSalmon);
            }
        }

        /// <summary>
        /// Well-known color: LightSeaGreen
        /// </summary>
        public static SimColor LightSeaGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightSeaGreen);
            }
        }

        /// <summary>
        /// Well-known color: LightSkyBlue
        /// </summary>
        public static SimColor LightSkyBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightSkyBlue);
            }
        }

        /// <summary>
        /// Well-known color: LightSlateGray
        /// </summary>
        public static SimColor LightSlateGray
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightSlateGray);
            }
        }

        /// <summary>
        /// Well-known color: LightSteelBlue
        /// </summary>
        public static SimColor LightSteelBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightSteelBlue);
            }
        }

        /// <summary>
        /// Well-known color: LightYellow
        /// </summary>
        public static SimColor LightYellow
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LightYellow);
            }
        }

        /// <summary>
        /// Well-known color: Lime
        /// </summary>
        public static SimColor Lime
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Lime);
            }
        }

        /// <summary>
        /// Well-known color: LimeGreen
        /// </summary>
        public static SimColor LimeGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.LimeGreen);
            }
        }

        /// <summary>
        /// Well-known color: Linen
        /// </summary>
        public static SimColor Linen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Linen);
            }
        }

        /// <summary>
        /// Well-known color: Magenta
        /// </summary>
        public static SimColor Magenta
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Magenta);
            }
        }

        /// <summary>
        /// Well-known color: Maroon
        /// </summary>
        public static SimColor Maroon
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Maroon);
            }
        }

        /// <summary>
        /// Well-known color: MediumAquamarine
        /// </summary>
        public static SimColor MediumAquamarine
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumAquamarine);
            }
        }

        /// <summary>
        /// Well-known color: MediumBlue
        /// </summary>
        public static SimColor MediumBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumBlue);
            }
        }

        /// <summary>
        /// Well-known color: MediumOrchid
        /// </summary>
        public static SimColor MediumOrchid
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumOrchid);
            }
        }

        /// <summary>
        /// Well-known color: MediumPurple
        /// </summary>
        public static SimColor MediumPurple
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumPurple);
            }
        }

        /// <summary>
        /// Well-known color: MediumSeaGreen
        /// </summary>
        public static SimColor MediumSeaGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumSeaGreen);
            }
        }

        /// <summary>
        /// Well-known color: MediumSlateBlue
        /// </summary>
        public static SimColor MediumSlateBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumSlateBlue);
            }
        }

        /// <summary>
        /// Well-known color: MediumSpringGreen
        /// </summary>
        public static SimColor MediumSpringGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumSpringGreen);
            }
        }

        /// <summary>
        /// Well-known color: MediumTurquoise
        /// </summary>
        public static SimColor MediumTurquoise
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumTurquoise);
            }
        }

        /// <summary>
        /// Well-known color: MediumVioletRed
        /// </summary>
        public static SimColor MediumVioletRed
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MediumVioletRed);
            }
        }

        /// <summary>
        /// Well-known color: MidnightBlue
        /// </summary>
        public static SimColor MidnightBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MidnightBlue);
            }
        }

        /// <summary>
        /// Well-known color: MintCream
        /// </summary>
        public static SimColor MintCream
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MintCream);
            }
        }

        /// <summary>
        /// Well-known color: MistyRose
        /// </summary>
        public static SimColor MistyRose
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.MistyRose);
            }
        }

        /// <summary>
        /// Well-known color: Moccasin
        /// </summary>
        public static SimColor Moccasin
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Moccasin);
            }
        }

        /// <summary>
        /// Well-known color: NavajoWhite
        /// </summary>
        public static SimColor NavajoWhite
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.NavajoWhite);
            }
        }

        /// <summary>
        /// Well-known color: Navy
        /// </summary>
        public static SimColor Navy
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Navy);
            }
        }

        /// <summary>
        /// Well-known color: OldLace
        /// </summary>
        public static SimColor OldLace
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.OldLace);
            }
        }

        /// <summary>
        /// Well-known color: Olive
        /// </summary>
        public static SimColor Olive
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Olive);
            }
        }

        /// <summary>
        /// Well-known color: OliveDrab
        /// </summary>
        public static SimColor OliveDrab
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.OliveDrab);
            }
        }

        /// <summary>
        /// Well-known color: Orange
        /// </summary>
        public static SimColor Orange
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Orange);
            }
        }

        /// <summary>
        /// Well-known color: OrangeRed
        /// </summary>
        public static SimColor OrangeRed
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.OrangeRed);
            }
        }

        /// <summary>
        /// Well-known color: Orchid
        /// </summary>
        public static SimColor Orchid
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Orchid);
            }
        }

        /// <summary>
        /// Well-known color: PaleGoldenrod
        /// </summary>
        public static SimColor PaleGoldenrod
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.PaleGoldenrod);
            }
        }

        /// <summary>
        /// Well-known color: PaleGreen
        /// </summary>
        public static SimColor PaleGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.PaleGreen);
            }
        }

        /// <summary>
        /// Well-known color: PaleTurquoise
        /// </summary>
        public static SimColor PaleTurquoise
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.PaleTurquoise);
            }
        }

        /// <summary>
        /// Well-known color: PaleVioletRed
        /// </summary>
        public static SimColor PaleVioletRed
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.PaleVioletRed);
            }
        }

        /// <summary>
        /// Well-known color: PapayaWhip
        /// </summary>
        public static SimColor PapayaWhip
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.PapayaWhip);
            }
        }

        /// <summary>
        /// Well-known color: PeachPuff
        /// </summary>
        public static SimColor PeachPuff
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.PeachPuff);
            }
        }

        /// <summary>
        /// Well-known color: Peru
        /// </summary>
        public static SimColor Peru
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Peru);
            }
        }

        /// <summary>
        /// Well-known color: Pink
        /// </summary>
        public static SimColor Pink
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Pink);
            }
        }

        /// <summary>
        /// Well-known color: Plum
        /// </summary>
        public static SimColor Plum
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Plum);
            }
        }

        /// <summary>
        /// Well-known color: PowderBlue
        /// </summary>
        public static SimColor PowderBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.PowderBlue);
            }
        }

        /// <summary>
        /// Well-known color: Purple
        /// </summary>
        public static SimColor Purple
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Purple);
            }
        }

        /// <summary>
        /// Well-known color: Red
        /// </summary>
        public static SimColor Red
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Red);
            }
        }

        /// <summary>
        /// Well-known color: RosyBrown
        /// </summary>
        public static SimColor RosyBrown
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.RosyBrown);
            }
        }

        /// <summary>
        /// Well-known color: RoyalBlue
        /// </summary>
        public static SimColor RoyalBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.RoyalBlue);
            }
        }

        /// <summary>
        /// Well-known color: SaddleBrown
        /// </summary>
        public static SimColor SaddleBrown
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SaddleBrown);
            }
        }

        /// <summary>
        /// Well-known color: Salmon
        /// </summary>
        public static SimColor Salmon
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Salmon);
            }
        }

        /// <summary>
        /// Well-known color: SandyBrown
        /// </summary>
        public static SimColor SandyBrown
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SandyBrown);
            }
        }

        /// <summary>
        /// Well-known color: SeaGreen
        /// </summary>
        public static SimColor SeaGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SeaGreen);
            }
        }

        /// <summary>
        /// Well-known color: SeaShell
        /// </summary>
        public static SimColor SeaShell
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SeaShell);
            }
        }

        /// <summary>
        /// Well-known color: Sienna
        /// </summary>
        public static SimColor Sienna
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Sienna);
            }
        }

        /// <summary>
        /// Well-known color: Silver
        /// </summary>
        public static SimColor Silver
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Silver);
            }
        }

        /// <summary>
        /// Well-known color: SkyBlue
        /// </summary>
        public static SimColor SkyBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SkyBlue);
            }
        }

        /// <summary>
        /// Well-known color: SlateBlue
        /// </summary>
        public static SimColor SlateBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SlateBlue);
            }
        }

        /// <summary>
        /// Well-known color: SlateGray
        /// </summary>
        public static SimColor SlateGray
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SlateGray);
            }
        }

        /// <summary>
        /// Well-known color: Snow
        /// </summary>
        public static SimColor Snow
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Snow);
            }
        }

        /// <summary>
        /// Well-known color: SpringGreen
        /// </summary>
        public static SimColor SpringGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SpringGreen);
            }
        }

        /// <summary>
        /// Well-known color: SteelBlue
        /// </summary>
        public static SimColor SteelBlue
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.SteelBlue);
            }
        }

        /// <summary>
        /// Well-known color: Tan
        /// </summary>
        public static SimColor Tan
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Tan);
            }
        }

        /// <summary>
        /// Well-known color: Teal
        /// </summary>
        public static SimColor Teal
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Teal);
            }
        }

        /// <summary>
        /// Well-known color: Thistle
        /// </summary>
        public static SimColor Thistle
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Thistle);
            }
        }

        /// <summary>
        /// Well-known color: Tomato
        /// </summary>
        public static SimColor Tomato
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Tomato);
            }
        }

        /// <summary>
        /// Well-known color: Transparent
        /// </summary>
        public static SimColor Transparent
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Transparent);
            }
        }

        /// <summary>
        /// Well-known color: Turquoise
        /// </summary>
        public static SimColor Turquoise
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Turquoise);
            }
        }

        /// <summary>
        /// Well-known color: Violet
        /// </summary>
        public static SimColor Violet
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Violet);
            }
        }

        /// <summary>
        /// Well-known color: Wheat
        /// </summary>
        public static SimColor Wheat
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Wheat);
            }
        }

        /// <summary>
        /// Well-known color: White
        /// </summary>
        public static SimColor White
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.White);
            }
        }

        /// <summary>
        /// Well-known color: WhiteSmoke
        /// </summary>
        public static SimColor WhiteSmoke
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.WhiteSmoke);
            }
        }

        /// <summary>
        /// Well-known color: Yellow
        /// </summary>
        public static SimColor Yellow
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.Yellow);
            }
        }

        /// <summary>
        /// Well-known color: YellowGreen
        /// </summary>
        public static SimColor YellowGreen
        {
            get
            {
                return SimColor.FromUInt32((uint)KnownColor.YellowGreen);
            }
        }

        #endregion static Known Colors
    }
#endif
}
