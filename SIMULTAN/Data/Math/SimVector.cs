// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace SIMULTAN.Data.SimMath
{
    /// <summary>
    /// Vector - A value type which defined a vector in terms of X and Y
    /// </summary>
    public partial struct SimVector
    {
        #region Constructors

        /// <summary>
        /// Constructor which sets the vector's initial values
        /// </summary>
        /// <param name="x"> double - The initial X </param>
        /// <param name="y"> double - THe initial Y </param>
        public SimVector(double x, double y)
        {
            _x = x;
            _y = y;
        }

        #endregion Constructors

        #region Public Methods

        /// <summary>
        /// Length Property - the length of this Vector
        /// </summary>
        public double Length
        {
            get
            {
                return Math.Sqrt(_x * _x + _y * _y);
            }
        }

        /// <summary>
        /// LengthSquared Property - the squared length of this Vector
        /// </summary>
        public double LengthSquared
        {
            get
            {
                return _x * _x + _y * _y;
            }
        }

        /// <summary>
        /// Normalize - Updates this Vector to maintain its direction, but to have a length
        /// of 1.  This is equivalent to dividing this Vector by Length
        /// </summary>
        public void Normalize()
        {
            // Avoid overflow
            this /= Math.Max(Math.Abs(_x), Math.Abs(_y));
            this /= Length;
        }

        /// <summary>
        /// CrossProduct - Returns the cross product: vector1.X*vector2.Y - vector1.Y*vector2.X
        /// </summary>
        /// <returns>
        /// Returns the cross product: vector1.X*vector2.Y - vector1.Y*vector2.X
        /// </returns>
        /// <param name="vector1"> The first Vector </param>
        /// <param name="vector2"> The second Vector </param>
        public static double CrossProduct(SimVector vector1, SimVector vector2)
        {
            return vector1._x * vector2._y - vector1._y * vector2._x;
        }

        /// <summary>
        /// AngleBetween - the angle between 2 vectors
        /// </summary>
        /// <returns>
        /// Returns the the angle in degrees between vector1 and vector2
        /// </returns>
        /// <param name="vector1"> The first Vector </param>
        /// <param name="vector2"> The second Vector </param>
        public static double AngleBetween(SimVector vector1, SimVector vector2)
        {
            double sin = vector1._x * vector2._y - vector2._x * vector1._y;
            double cos = vector1._x * vector2._x + vector1._y * vector2._y;

            return Math.Atan2(sin, cos) * (180 / Math.PI);
        }

        #endregion Public Methods

        #region Public Operators
        /// <summary>
        /// Operator -Vector (unary negation)
        /// </summary>
        public static SimVector operator -(SimVector vector)
        {
            return new SimVector(-vector._x, -vector._y);
        }

        /// <summary>
        /// Negates the values of X and Y on this Vector
        /// </summary>
        public void Negate()
        {
            _x = -_x;
            _y = -_y;
        }

        /// <summary>
        /// Operator Vector + Vector
        /// </summary>
        public static SimVector operator +(SimVector vector1, SimVector vector2)
        {
            return new SimVector(vector1._x + vector2._x,
                              vector1._y + vector2._y);
        }

        /// <summary>
        /// Add: Vector + Vector
        /// </summary>
        public static SimVector Add(SimVector vector1, SimVector vector2)
        {
            return new SimVector(vector1._x + vector2._x,
                              vector1._y + vector2._y);
        }

        /// <summary>
        /// Operator Vector - Vector
        /// </summary>
        public static SimVector operator -(SimVector vector1, SimVector vector2)
        {
            return new SimVector(vector1._x - vector2._x,
                              vector1._y - vector2._y);
        }

        /// <summary>
        /// Subtract: Vector - Vector
        /// </summary>
        public static SimVector Subtract(SimVector vector1, SimVector vector2)
        {
            return new SimVector(vector1._x - vector2._x,
                              vector1._y - vector2._y);
        }

        /// <summary>
        /// Operator Vector + Point
        /// </summary>
        public static SimPoint operator +(SimVector vector, SimPoint point)
        {
            return new SimPoint(point._x + vector._x, point._y + vector._y);
        }

        /// <summary>
        /// Add: Vector + Point
        /// </summary>
        public static SimPoint Add(SimVector vector, SimPoint point)
        {
            return new SimPoint(point._x + vector._x, point._y + vector._y);
        }

        /// <summary>
        /// Operator Vector * double
        /// </summary>
        public static SimVector operator *(SimVector vector, double scalar)
        {
            return new SimVector(vector._x * scalar,
                              vector._y * scalar);
        }

        /// <summary>
        /// Multiply: Vector * double
        /// </summary>
        public static SimVector Multiply(SimVector vector, double scalar)
        {
            return new SimVector(vector._x * scalar,
                              vector._y * scalar);
        }

        /// <summary>
        /// Operator double * Vector
        /// </summary>
        public static SimVector operator *(double scalar, SimVector vector)
        {
            return new SimVector(vector._x * scalar,
                              vector._y * scalar);
        }

        /// <summary>
        /// Multiply: double * Vector
        /// </summary>
        public static SimVector Multiply(double scalar, SimVector vector)
        {
            return new SimVector(vector._x * scalar,
                              vector._y * scalar);
        }

        /// <summary>
        /// Operator Vector / double
        /// </summary>
        public static SimVector operator /(SimVector vector, double scalar)
        {
            return vector * (1.0 / scalar);
        }

        /// <summary>
        /// Multiply: Vector / double
        /// </summary>
        public static SimVector Divide(SimVector vector, double scalar)
        {
            return vector * (1.0 / scalar);
        }

        /// <summary>
        /// Operator Vector * Matrix
        /// </summary>
        public static SimVector operator *(SimVector vector, SimMatrix matrix)
        {
            return matrix.Transform(vector);
        }

        /// <summary>
        /// Multiply: Vector * Matrix
        /// </summary>
        public static SimVector Multiply(SimVector vector, SimMatrix matrix)
        {
            return matrix.Transform(vector);
        }

        /// <summary>
        /// Operator Vector * Vector, interpreted as their dot product
        /// </summary>
        public static double operator *(SimVector vector1, SimVector vector2)
        {
            return vector1._x * vector2._x + vector1._y * vector2._y;
        }

        /// <summary>
        /// Multiply - Returns the dot product: vector1.X*vector2.X + vector1.Y*vector2.Y
        /// </summary>
        /// <returns>
        /// Returns the dot product: vector1.X*vector2.X + vector1.Y*vector2.Y
        /// </returns>
        /// <param name="vector1"> The first Vector </param>
        /// <param name="vector2"> The second Vector </param>
        public static double Multiply(SimVector vector1, SimVector vector2)
        {
            return vector1._x * vector2._x + vector1._y * vector2._y;
        }

        /// <summary>
        /// Determinant - Returns the determinant det(vector1, vector2)
        /// </summary>
        /// <returns>
        /// Returns the determinant: vector1.X*vector2.Y - vector1.Y*vector2.X
        /// </returns>
        /// <param name="vector1"> The first Vector </param>
        /// <param name="vector2"> The second Vector </param>
        public static double Determinant(SimVector vector1, SimVector vector2)
        {
            return vector1._x * vector2._y - vector1._y * vector2._x;
        }

        /// <summary>
        /// Explicit conversion to Size.  Note that since Size cannot contain negative values,
        /// the resulting size will contains the absolute values of X and Y
        /// </summary>
        /// <returns>
        /// Size - A Size equal to this Vector
        /// </returns>
        /// <param name="vector"> Vector - the Vector to convert to a Size </param>
        public static explicit operator SimSize(SimVector vector)
        {
            return new SimSize(Math.Abs(vector._x), Math.Abs(vector._y));
        }

        /// <summary>
        /// Explicit conversion to Point
        /// </summary>
        /// <returns>
        /// Point - A Point equal to this Vector
        /// </returns>
        /// <param name="vector"> Vector - the Vector to convert to a Point </param>
        public static explicit operator SimPoint(SimVector vector)
        {
            return new SimPoint(vector._x, vector._y);
        }
        #endregion Public Operators
    }
}
