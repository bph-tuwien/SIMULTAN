using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Utils;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class MathTests
    {
        [TestMethod]
        public void QuatToEulerAngles()
        {
            Vector3D a1 = new Vector3D(0.0, 0.0, 0.0);
            Vector3D a2 = new Vector3D(90.0, 0.0, 0.0);
            Vector3D a3 = new Vector3D(180.0, 0.0, 0.0);
            Vector3D a4 = new Vector3D(0.0, 90.0, 90.0);
            Vector3D a5 = new Vector3D(45.0, 45.0, 45.0);
            Vector3D a6 = new Vector3D(3.0, 16.0, 170.0);
            Vector3D a7 = new Vector3D(-45.0, 13.0, 23.0);

            Quaternion q1 = QuaternionExtensions.CreateFromYawPitchRoll(a1);
            Quaternion q2 = QuaternionExtensions.CreateFromYawPitchRoll(a2);
            Quaternion q3 = QuaternionExtensions.CreateFromYawPitchRoll(a3);
            Quaternion q4 = QuaternionExtensions.CreateFromYawPitchRoll(a4);
            Quaternion q5 = QuaternionExtensions.CreateFromYawPitchRoll(a5);
            Quaternion q6 = QuaternionExtensions.CreateFromYawPitchRoll(a6);
            Quaternion q7 = QuaternionExtensions.CreateFromYawPitchRoll(a7);

            Vector3D aq1 = q1.ToEulerAngles();
            Vector3D aq2 = q2.ToEulerAngles();
            Vector3D aq3 = q3.ToEulerAngles();
            Vector3D aq4 = q4.ToEulerAngles();
            Vector3D aq5 = q5.ToEulerAngles();
            Vector3D aq6 = q6.ToEulerAngles();
            Vector3D aq7 = q7.ToEulerAngles();

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
            Assert.AreEqual(a4.X, aq4.X, tol);
            Assert.AreEqual(a4.Y, aq4.Y, tol);
            Assert.AreEqual(a4.Z, aq4.Z, tol);
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
