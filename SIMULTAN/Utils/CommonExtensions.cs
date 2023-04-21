using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Contains extensions for common types
    /// </summary>
    public static class CommonExtensions
    {
        /// <summary>
        /// Return true when the value is inside the range boundaries
        /// </summary>
        /// <typeparam name="T">Type of all values</typeparam>
        /// <param name="value">The value</param>
        /// <param name="minRange">Minimum value</param>
        /// <param name="maxRange">Maximum value</param>
        /// <returns>True when value in [minRange, maxRange]</returns>
        public static bool InRange<T>(this T value, T minRange, T maxRange) where T : IComparable
        {
            return (value.CompareTo(minRange) >= 0 && value.CompareTo(maxRange) <= 0);
        }

        /// <summary>
        /// Compares two SecureStrings for equality.
        /// </summary>
        /// <param name="s1">The current string</param>
        /// <param name="s2">The other string</param>
        /// <returns>True when both strings contain the same text. Otherwise False</returns>
        public static bool SecureEquals(this SecureString s1, SecureString s2)
        {
            if (s1 == null)
            {
                throw new ArgumentNullException("s1");
            }
            if (s2 == null)
            {
                throw new ArgumentNullException("s2");
            }

            if (s1.Length != s2.Length)
            {
                return false;
            }

            IntPtr bstr1 = IntPtr.Zero;
            IntPtr bstr2 = IntPtr.Zero;

            RuntimeHelpers.PrepareConstrainedRegions();

            try
            {
                bstr1 = Marshal.SecureStringToBSTR(s1);
                bstr2 = Marshal.SecureStringToBSTR(s2);

                String str1 = Marshal.PtrToStringBSTR(bstr1);
                String str2 = Marshal.PtrToStringBSTR(bstr2);

                return str1.Equals(str2);
            }
            finally
            {
                if (bstr1 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr1);
                }

                if (bstr2 != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(bstr2);
                }
            }
        }

        /// <summary>
        /// Clamps a value to a range
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="val">The value</param>
        /// <param name="min">Minimum</param>
        /// <param name="max">Maximum</param>
        /// <returns>val when val in [min, max], otherwise min or max</returns>
        /// Code from https://stackoverflow.com/questions/2683442/where-can-i-find-the-clamp-function-in-net/20443081
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        /// <summary>
        /// Checks whether two given vectors are collinear using a custom threshold
        /// </summary>
        /// <param name="v1">The current vector</param>
        /// <param name="v2">The other vector</param>
        /// <param name="threshold">Threshold to test for collinearity</param>
        /// <returns>true if vectors are collinear, otherwise false</returns>
        public static bool IsCollinear(this Vector3D v1, Vector3D v2, double threshold = 1e-3)
        {
            return Math.Abs(Math.Abs(Vector3D.DotProduct(v1, v2)) - 1.0) < threshold;
        }

        /// <summary>
        /// Transposes a matrix and returns it as a new object
        /// </summary>
        /// <param name="m">This</param>
        /// <returns>A new matrix which is the transpose of this</returns>
        public static Matrix3D Transpose(this Matrix3D m)
        {
            return new Matrix3D(
                    m.M11, m.M21, m.M31, m.OffsetX,
                    m.M12, m.M22, m.M32, m.OffsetY,
                    m.M13, m.M23, m.M33, m.OffsetZ,
                    m.M14, m.M24, m.M34, m.M44
                );
        }


        /// <summary>
        /// Checks if two doubles are equal or if both are NaN
        /// </summary>
        /// <param name="d">First double</param>
        /// <param name="other">Second double</param>
        /// <returns>True when both doubles have the same value or both doubles are NaN</returns>
        public static bool EqualsWithNan(this double d, double other)
        {
            return (d == other) || (double.IsNaN(d) && double.IsNaN(other));
        }

        /// <summary>
        /// Creates a Matrix3D from 3 orthogonal axes x,y and z and an offset p
        /// </summary>
        /// <param name="x">x-axis, 1st row</param>
        /// <param name="y">y-axis, 2nd row</param>
        /// <param name="z">z-axis, 3rd row</param>
        /// <param name="p">offset, 4th row</param>
        /// <returns>Matrix</returns>
        public static Matrix3D MatrixFromAxes(Vector3D x, Vector3D y, Vector3D z, Point3D p)
        {
            return new Matrix3D(x.X, x.Y, x.Z, 0.0,
                                y.X, y.Y, y.Z, 0.0,
                                z.X, z.Y, z.Z, 0.0,
                                p.X, p.Y, p.Z, 1.0);
        }


        /// <summary>
        /// Returns a new version instance which contains only the first fieldCount components
        /// </summary>
        /// <param name="version">The version</param>
        /// <param name="fieldCount">The number of components to copy to the output</param>
        /// <returns>A new version containing the selected components. All other values have their default value</returns>
		public static Version GetVersion(this Version version, int fieldCount)
        {
            switch (fieldCount)
            {
                case 1:
                    return new Version(version.Major, -1);
                case 2:
                    return new Version(version.Major, version.Minor);
                case 3:
                    return new Version(version.Major, version.Minor, version.Build);
                case 4:
                    return new Version(version.Major, version.Minor, version.Build, version.Revision);
                default:
                    throw new ArgumentOutOfRangeException(string.Format("{0} has to be between 1 and 4", nameof(fieldCount)));
            }
        }

        /// <summary>
        /// Converts an object to a double. Numeric values (double, int) are directly converted. All other types return <see cref="double.NaN"/>.
        /// </summary>
        /// <param name="value">The value to convert</param>
        /// <returns>For double, int: The value cast to a double. All other types return <see cref="double.NaN"/></returns>
        public static double ConvertToDoubleIfNumeric(this object value)
        {
            if (value is double d)
                return d;
            else if (value is int i)
                return (double)i;
            else
                return double.NaN;
        }
    }
}
