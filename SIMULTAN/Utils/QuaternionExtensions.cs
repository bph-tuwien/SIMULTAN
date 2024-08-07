﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Common extensions or static methods for SimQuaternions
    /// </summary>
    public static class SimQuaternionExtensions
    {
        /// <summary>
        /// Creates a SimQuaternion from Euler angles (in deg)
        /// https://en.wikipedia.org/wiki/Conversion_between_SimQuaternions_and_Euler_angles
        /// </summary>
        /// <param name="yaw">Rotation around z-axis</param>
        /// <param name="pitch">Rotation around y-axis</param>
        /// <param name="roll">Rotation around x-axis</param>
        /// <returns>SimQuaternion from Euler angles</returns>
        public static SimQuaternion CreateFromYawPitchRoll(double yaw, double pitch, double roll)
        {
            double fromDeg = Math.PI / 180.0;
            yaw = yaw * fromDeg;
            pitch = pitch * fromDeg;
            roll = roll * fromDeg;

            // Abbreviations for the various angular functions
            double cy = Math.Cos(yaw * 0.5);
            double sy = Math.Sin(yaw * 0.5);
            double cp = Math.Cos(pitch * 0.5);
            double sp = Math.Sin(pitch * 0.5);
            double cr = Math.Cos(roll * 0.5);
            double sr = Math.Sin(roll * 0.5);

            SimQuaternion q = new SimQuaternion();
            q.W = cr * cp * cy + sr * sp * sy;
            q.X = sr * cp * cy - cr * sp * sy;
            q.Y = cr * sp * cy + sr * cp * sy;
            q.Z = cr * cp * sy - sr * sp * cy;

            return q;
        }

        /// <summary>
        /// Creates a SimQuaternion from Euler angles (in deg), where x is roll, y is pitch and z is yaw
        /// </summary>
        /// <param name="eulerAngles">Euler angles (in deg)</param>
        /// <returns>SimQuaternion from Euler angles</returns>
        public static SimQuaternion CreateFromYawPitchRoll(SimVector3D eulerAngles)
        {
            return CreateFromYawPitchRoll(eulerAngles.Z, eulerAngles.Y, eulerAngles.X);
        }

        /// <summary>
        /// Converts this SimQuaternion to Euler angles (in deg)
        /// https://en.wikipedia.org/wiki/Conversion_between_SimQuaternions_and_Euler_angles
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static SimVector3D ToEulerAngles(this SimQuaternion q)
        {
            double toDeg = 180.0 / Math.PI;
            SimVector3D angles = new SimVector3D();

            // roll (x-axis rotation)
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch (y-axis rotation)
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
                angles.Y = Math.PI / 2.0 * Math.Sign(sinp); // use 90 degrees if out of range
            else
                angles.Y = Math.Asin(sinp);

            // yaw (z-axis rotation)
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = Math.Atan2(siny_cosp, cosy_cosp);

            return angles * toDeg;
        }
    }
}
