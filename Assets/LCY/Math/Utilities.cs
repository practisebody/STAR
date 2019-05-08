using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace LCY
{
    /// <summary>
    /// Math utility functions
    /// </summary>
    public static partial class Utilities
    {
        /// <summary>
        /// Converts a Rodrigues as Vector3 to Quaternion.
        /// See https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula.
        /// </summary>
        public static Quaternion Rodrigues2Quaternion(Vector3 rotation)
        {
            float theta = Mathf.Rad2Deg * rotation.magnitude;
            Vector3 axis = rotation.normalized;
            return Quaternion.AngleAxis(theta, axis);
        }

        /// <summary>
        /// Converts a List of 3 double to a Vector3
        /// </summary>
        public static Vector3 List2Vector3(List<double> vec)
        {
            return new Vector3((float)(vec[0]), (float)(vec[1]), (float)(vec[2]));
        }

        /// <summary>
        /// Converts a Rodrigues as a List of 3 doubles to a Quaternion
        /// </summary>
        public static Quaternion Rodrigues2Quaternion(List<double> rvec)
        {
            Vector3 axis = List2Vector3(rvec);
            float theta = Mathf.Rad2Deg * axis.magnitude;
            axis.Normalize();
            return Quaternion.AngleAxis(theta, axis);
        }

        //[DllImport("HoloOpenCVHelper")]
        //public static extern void addLine(float x, float y, float z, float dx, float dy, float dz);
        //[DllImport("HoloOpenCVHelper")]
        //public static extern void solveLineAndClear();
        //[DllImport("HoloOpenCVHelper")]
        //public static extern float getLinesIntersectionX();
        //[DllImport("HoloOpenCVHelper")]
        //public static extern float getLinesIntersectionY();
        //[DllImport("HoloOpenCVHelper")]
        //public static extern float getLinesIntersectionZ();

        //public static Vector3 LinesIntersection(List<Tuple<Vector3, Vector3>> lines)
        //{
        //    foreach (Tuple<Vector3, Vector3> line in lines)
        //    {
        //        addLine(line.Item1.x, line.Item1.y, line.Item1.z, line.Item2.x, line.Item2.y, line.Item2.z);
        //    }
        //    solveLineAndClear();
        //    float x = getLinesIntersectionX();
        //    float y = getLinesIntersectionY();
        //    float z = getLinesIntersectionZ();
        //    return new Vector3(x, y, z);
        //}
    }
}