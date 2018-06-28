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

        public static SE3 operator *(SE3 a, SE3 b) => new SE3(a.matrix * b.matrix);

        public static Vector3 operator *(SE3 a, Vector3 b) => a.matrix * b;

        public SE3 inverse { get { return matrix.inverse; } }

        public static SE3 ConvertLeftHandedFloatArrayToSE3(float[] matrixAsArray)
        {
            Matrix4x4 m = new Matrix4x4();
            m.m00 = matrixAsArray[0];
            m.m01 = matrixAsArray[1];
            m.m02 = -matrixAsArray[2];
            m.m03 = matrixAsArray[3];
            m.m10 = matrixAsArray[4];
            m.m11 = matrixAsArray[5];
            m.m12 = -matrixAsArray[6];
            m.m13 = matrixAsArray[7];
            m.m20 = matrixAsArray[8];
            m.m21 = matrixAsArray[9];
            m.m22 = -matrixAsArray[10];
            m.m23 = matrixAsArray[11];
            m.m30 = matrixAsArray[12];
            m.m31 = matrixAsArray[13];
            m.m32 = matrixAsArray[14];
            m.m33 = matrixAsArray[15];
            return m;
        }

        public static SE3 ConvertLeftHandedMatrix4x4ToSE3(Matrix4x4 matrix)
        {
            Matrix4x4 m = new Matrix4x4(matrix.GetColumn(0), matrix.GetColumn(1), matrix.GetColumn(2), matrix.GetColumn(3));
            m.m02 = -m.m02;
            m.m12 = -m.m12;
            m.m22 = -m.m22;
            return m;
            // TODO
            // return new Matrix4x4(matrix.GetColumn(0), matrix.GetColumn(1), -matrix.GetColumn(2), matrix.GetColumn(3));
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