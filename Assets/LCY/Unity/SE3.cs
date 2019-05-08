using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCY
{
    /// <summary>
    /// A SE3 class, helpful function to convert between rotation, translation.
    /// See https://www.seas.upenn.edu/~meam620/slides/kinematicsI.pdf.
    /// </summary>
    public class SE3
    {
        protected Quaternion rotation;
        public Quaternion Rotation
        {
            get { return rotation; }
        }
        protected Vector3 translation;
        public Vector3 Translation
        {
            get { return translation; }
        }
        protected Matrix4x4 matrix;
        public Matrix4x4 Matrix
        {
            get { return matrix; }
            set
            {
                matrix = value;
                rotation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
                translation = matrix.GetColumn(3);
            }
        }

        public SE3(Quaternion rotation, Vector3 translation)
        {
            Matrix = Matrix4x4.TRS(translation, rotation, Vector3.one);
        }

        public SE3(Matrix4x4 m)
        {
            Matrix = m;
        }

        public static implicit operator SE3(Matrix4x4 m)
        {
            return new SE3(m);
        }

        public static SE3 operator*(SE3 a, SE3 b) => new SE3(a.matrix * b.matrix);

        public static Vector3 operator *(SE3 a, Vector3 b) => a.matrix * b;

        public SE3 inverse { get { return matrix.inverse; } }

        public static SE3 ConvertHoloLensMatrix4x4ToSE3(Matrix4x4 matrix)
        {
            Matrix4x4 m = new Matrix4x4(matrix.GetColumn(0), matrix.GetColumn(1), matrix.GetColumn(2), matrix.GetColumn(3));
            m.m02 = -m.m02;
            m.m12 = -m.m12;
            m.m22 = -m.m22;
            return m;
        }

        public static SE3 ConvertHoloLensPoseToSE3(Vector3 position, Quaternion rotation)
        {
            Matrix4x4 m = new Matrix4x4();
            m.SetTRS(position, new Quaternion(rotation.x, rotation.y, -rotation.z, -rotation.w), Vector3.one);
            return m;
        }
    }
}