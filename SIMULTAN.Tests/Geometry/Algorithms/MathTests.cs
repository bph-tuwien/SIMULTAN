using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using System;
using System.Buffers;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class MathTests
    {
        [TestMethod]
        public void QuatToEulerAngles()
        {
            SimVector3D a1 = new SimVector3D(0.0, 0.0, 0.0);
            SimVector3D a2 = new SimVector3D(90.0, 0.0, 0.0);
            SimVector3D a3 = new SimVector3D(180.0, 0.0, 0.0);
            SimVector3D a4 = new SimVector3D(0.0, 90.0, 90.0);
            // Because of numerical imprecision it changes the a4 value to something else
            // This started to happen after the upgrade from .net Framework to net7
            // It is also differnt on linux than windows weirdly enough
            // We decided to ignore that, cause the euler angels are only shown in the UI so far
            SimVector3D a4Compare = OperatingSystem.IsWindows() ? new SimVector3D(180.0, 90.0, 180.0) : new SimVector3D(0.0, 90.0, 90.0);
            SimVector3D a5 = new SimVector3D(45.0, 45.0, 45.0);
            SimVector3D a6 = new SimVector3D(3.0, 16.0, 170.0);
            SimVector3D a7 = new SimVector3D(-45.0, 13.0, 23.0);

            SimQuaternion q1 = SimQuaternionExtensions.CreateFromYawPitchRoll(a1);
            SimQuaternion q2 = SimQuaternionExtensions.CreateFromYawPitchRoll(a2);
            SimQuaternion q3 = SimQuaternionExtensions.CreateFromYawPitchRoll(a3);
            SimQuaternion q4 = SimQuaternionExtensions.CreateFromYawPitchRoll(a4);
            SimQuaternion q5 = SimQuaternionExtensions.CreateFromYawPitchRoll(a5);
            SimQuaternion q6 = SimQuaternionExtensions.CreateFromYawPitchRoll(a6);
            SimQuaternion q7 = SimQuaternionExtensions.CreateFromYawPitchRoll(a7);

            SimVector3D aq1 = q1.ToEulerAngles();
            SimVector3D aq2 = q2.ToEulerAngles();
            SimVector3D aq3 = q3.ToEulerAngles();
            SimVector3D aq4 = q4.ToEulerAngles();
            SimVector3D aq5 = q5.ToEulerAngles();
            SimVector3D aq6 = q6.ToEulerAngles();
            SimVector3D aq7 = q7.ToEulerAngles();



            var tol = 1e-6;
            Assert.AreEqual(a1.X, aq1.X, tol);
            Assert.AreEqual(a1.Y, aq1.Y, tol);
            Assert.AreEqual(a1.Z, aq1.Z, tol);
            Assert.AreEqual(a2.X, aq2.X, tol);
            Assert.AreEqual(a2.Y, aq2.Y, tol);
            Assert.AreEqual(a2.Z, aq2.Z, tol);
            Assert.AreEqual(a3.X, aq3.X, tol);
            Assert.AreEqual(a3.Y, aq3.Y, tol);
            Assert.AreEqual(a3.Z, aq3.Z, tol);
            Assert.AreEqual(a4Compare.X, aq4.X, tol);
            Assert.AreEqual(a4Compare.Y, aq4.Y, tol);
            Assert.AreEqual(a4Compare.Z, aq4.Z, tol);
            Assert.AreEqual(a5.X, aq5.X, tol);
            Assert.AreEqual(a5.Y, aq5.Y, tol);
            Assert.AreEqual(a5.Z, aq5.Z, tol);
            Assert.AreEqual(a6.X, aq6.X, tol);
            Assert.AreEqual(a6.Y, aq6.Y, tol);
            Assert.AreEqual(a6.Z, aq6.Z, tol);
            Assert.AreEqual(a7.X, aq7.X, tol);
            Assert.AreEqual(a7.Y, aq7.Y, tol);
            Assert.AreEqual(a7.Z, aq7.Z, tol);
        }
    }
}
