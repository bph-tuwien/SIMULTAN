﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 3D point implementation. 
//
//              See spec at http://avalon/medialayer/Specifications/Avalon3D%20API%20Spec.mht 
//
//

using MathNet.Numerics.Integration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SimMath
{
    /// <summary>
    /// Point3D - 3D point representation. 
    /// Defaults to (0,0,0).
    /// </summary>
    public partial struct SimPoint3D
    {

        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor that sets point's initial values.
        /// </summary>
        /// <param name="x">Value of the X coordinate of the new point.</param>
        /// <param name="y">Value of the Y coordinate of the new point.</param>
        /// <param name="z">Value of the Z coordinate of the new point.</param>
        public SimPoint3D(double x, double y, double z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Offset - update point position by adding offsetX to X, offsetY to Y, and offsetZ to Z.
        /// </summary>
        /// <param name="offsetX">Offset in the X direction.</param>
        /// <param name="offsetY">Offset in the Y direction.</param>
        /// <param name="offsetZ">Offset in the Z direction.</param>
        public void Offset(double offsetX, double offsetY, double offsetZ)
        {
            _x += offsetX;
            _y += offsetY;
            _z += offsetZ;
        }

        /// <summary>
        /// Point3D + Vector3D addition.
        /// </summary>
        /// <param name="point">Point being added.</param>
        /// <param name="vector">Vector being added.</param>
        /// <returns>Result of addition.</returns>
        public static SimPoint3D operator +(SimPoint3D point, SimVector3D vector)
        {
            return new SimPoint3D(point._x + vector._x,
                               point._y + vector._y,
                               point._z + vector._z);
        }

        /// <summary>
        /// Point3D + Vector3D addition.
        /// </summary>
        /// <param name="point">Point being added.</param>
        /// <param name="vector">Vector being added.</param>
        /// <returns>Result of addition.</returns>
        public static SimPoint3D Add(SimPoint3D point, SimVector3D vector)
        {
            return new SimPoint3D(point._x + vector._x,
                               point._y + vector._y,
                               point._z + vector._z);
        }

        /// <summary>
        /// Point3D - Vector3D subtraction.
        /// </summary>
        /// <param name="point">Point from which vector is being subtracted.</param>
        /// <param name="vector">Vector being subtracted from the point.</param>
        /// <returns>Result of subtraction.</returns>
        public static SimPoint3D operator -(SimPoint3D point, SimVector3D vector)
        {
            return new SimPoint3D(point._x - vector._x,
                               point._y - vector._y,
                               point._z - vector._z);
        }

        /// <summary>
        /// Point3D - Vector3D subtraction.
        /// </summary>
        /// <param name="point">Point from which vector is being subtracted.</param>
        /// <param name="vector">Vector being subtracted from the point.</param>
        /// <returns>Result of subtraction.</returns>
        public static SimPoint3D Subtract(SimPoint3D point, SimVector3D vector)
        {
            return new SimPoint3D(point._x - vector._x,
                               point._y - vector._y,
                               point._z - vector._z);
        }

        /// <summary>
        /// Subtraction.
        /// </summary>
        /// <param name="point1">Point from which we are subtracting the second point.</param>
        /// <param name="point2">Point being subtracted.</param>
        /// <returns>Vector between the two points.</returns>
        public static SimVector3D operator -(SimPoint3D point1, SimPoint3D point2)
        {
            return new SimVector3D(point1._x - point2._x,
                                point1._y - point2._y,
                                point1._z - point2._z);
        }

        /// <summary>
        /// Subtraction.
        /// </summary>
        /// <param name="point1">Point from which we are subtracting the second point.</param>
        /// <param name="point2">Point being subtracted.</param>
        /// <returns>Vector between the two points.</returns>
        public static SimVector3D Subtract(SimPoint3D point1, SimPoint3D point2)
        {
            SimVector3D v = new SimVector3D();
            Subtract(ref point1, ref point2, out v);
            return v;
        }

        /// <summary>
        /// Faster internal version of Subtract that avoids copies
        ///
        /// p1 and p2 to a passed by ref for perf and ARE NOT MODIFIED
        /// </summary>
        internal static void Subtract(ref SimPoint3D p1, ref SimPoint3D p2, out SimVector3D result)
        {
            result._x = p1._x - p2._x;
            result._y = p1._y - p2._y;
            result._z = p1._z - p2._z;
        }

        /// <summary>
        /// Point3D * Matrix3D multiplication.
        /// </summary>
        /// <param name="point">Point being transformed.</param>
        /// <param name="matrix">Transformation matrix applied to the point.</param>
        /// <returns>Result of the transformation matrix applied to the point.</returns>
        public static SimPoint3D operator *(SimPoint3D point, SimMatrix3D matrix)
        {
            return matrix.Transform(point);
        }

        /// <summary>
        /// Point3D * Matrix3D multiplication.
        /// </summary>
        /// <param name="point">Point being transformed.</param>
        /// <param name="matrix">Transformation matrix applied to the point.</param>
        /// <returns>Result of the transformation matrix applied to the point.</returns>
        public static SimPoint3D Multiply(SimPoint3D point, SimMatrix3D matrix)
        {
            return matrix.Transform(point);
        }

        /// <summary>
        /// Explicit conversion to Vector3D.
        /// </summary>
        /// <param name="point">Given point.</param>
        /// <returns>Vector representing the point.</returns>
        public static explicit operator SimVector3D(SimPoint3D point)
        {
            return new SimVector3D(point._x, point._y, point._z);
        }

        /// <summary>
        /// Explicit conversion to Point4D.
        /// </summary>
        /// <param name="point">Given point.</param>
        /// <returns>4D point representing the 3D point.</returns>
        public static explicit operator SimPoint4D(SimPoint3D point)
        {
            return new SimPoint4D(point._x, point._y, point._z, 1.0);
        }

        #endregion Public Methods
    }
}
