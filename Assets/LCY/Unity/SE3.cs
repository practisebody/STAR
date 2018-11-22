using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LCY
{
    public class SE3 : ILerpable<SE3>
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
            // TODO
            // return new Matrix4x4(matrix.GetColumn(0), matrix.GetColumn(1), -matrix.GetColumn(2), matrix.GetColumn(3));
        }

        public static SE3 ConvertHoloLensPoseToSE3(Vector3 position, Quaternion rotation)
        {
            Matrix4x4 m = new Matrix4x4();
            m.SetTRS(position, new Quaternion(rotation.x, rotation.y, -rotation.z, -rotation.w), Vector3.one);
            return m;
        }

        public SE3 Lerp(SE3 a, SE3 b, float t)
        {
            Quaternion Ra = a.Rotation, Rb = b.Rotation;
            Vector3 Ta = a.Translation, Tb = b.Translation;
            Quaternion R = Quaternion.Lerp(Ra, Rb, t);
            Vector3 T = Vector3.Lerp(Ta, Tb, t);
            return new SE3(R, T);
        }
    }
}