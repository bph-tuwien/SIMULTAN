using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Usefull extensions for the SimPoint3D class
    /// </summary>
    public static class PointVectorExtensions
    {
        /// <summary>
        /// Multiplies the point with a scalar value
        /// </summary>
        /// <param name="p">The point</param>
        /// <param name="d">The scalar</param>
        /// <returns>The multiplied point</returns>
        public static SimPoint3D Multiply(this SimPoint3D p, double d)
        {
            return new SimPoint3D(p.X * d, p.Y * d, p.Z * d);
        }
        /// <summary>
        /// Divides the point by a scalar value
        /// </summary>
        /// <param name="p">The point</param>
        /// <param name="d">The scalar</param>
        /// <returns>The divided point</returns>
        public static SimPoint3D Divide(this SimPoint3D p, double d)
        {
            return new SimPoint3D(p.X / d, p.Y / d, p.Z / d);
        }

        /// <summary>
        /// Returns a point containing only the X and Y coordinate
        /// </summary>
        /// <param name="p">Original point</param>
        /// <returns>A point containing only X and Y coordinate</returns>
        public static SimPoint XY(this SimPoint3D p)
        {
            return new SimPoint(p.X, p.Y);
        }

        /// <summary>
        /// Returns a vector containing only the X and Y coordinate
        /// </summary>
        /// <param name="v">Original vector</param>
        /// <returns>A vector containing only X and Y coordinate</returns>
        public static SimVector XY(this SimVector3D v)
        {
            return new SimVector(v.X, v.Y);
        }

        /// <summary>
        /// Returns a specific dimension of a SimVector3D
        /// </summary>
        /// <param name="v">The SimVector3D</param>
        /// <param name="idx">Index of the dimension: X: 0, Y: 1, Z: 2</param>
        /// <returns></returns>
		public static double Get(this SimVector3D v, int idx)
        {
            switch (idx)
            {
                case 0:
                    return v.X;
                case 1:
                    return v.Y;
                case 2:
                    return v.Z;
                default:
                    throw new IndexOutOfRangeException("idx has to be between 0 and 2");
            }
        }
        /// <summary>
        /// Returns a specific dimension of a SimPoint3D
        /// </summary>
        /// <param name="v">The SimPoint3D</param>
        /// <param name="idx">Index of the dimension: X: 0, Y: 1, Z: 2</param>
        /// <returns></returns>
		public static double Get(this SimPoint3D v, int idx)
        {
            switch (idx)
            {
                case 0:
                    return v.X;
                case 1:
                    return v.Y;
                case 2:
                    return v.Z;
                default:
                    throw new IndexOutOfRangeException("idx has to be between 0 and 2");
            }
        }

        /// <summary>
        /// Allows to get XYZ with an index
        /// </summary>
        /// <param name="p">The point</param>
        /// <param name="idx">The index (0=X, 1=Y, 2=Z, 3.. = Exception)</param>
        /// <returns>The desired axis</returns>
        public static double At(this SimPoint3D p, int idx)
        {
            switch (idx)
            {
                case 0:
                    return p.X;
                case 1:
                    return p.Y;
                case 2:
                    return p.Z;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
