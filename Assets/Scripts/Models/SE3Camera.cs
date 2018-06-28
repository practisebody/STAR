using LCY;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace STAR
{
    public class SE3Camera : SE3Object
    {
        // intrinsic
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public float Fx { get; protected set; }
        public float Fy { get; protected set; }
        public float Cx { get; protected set; }
        public float Cy { get; protected set; }
        public float K1 { get; protected set; }
        public float K2 { get; protected set; }
        public float P1 { get; protected set; }
        public float P2 { get; protected set; }
        public float K3 { get; protected set; }

        public enum DoUndistort
        {
            YES,
            NO,
        };

        public Vector2 Undistort(float x0, float y0)
        {
            float x = x0, y = y0;
            for (int i = 0; i < 5; i++)
            {
                float r2 = x * x + y * y;
                float icdist = 1 / (1 + ((K3 * r2 + K2) * r2 + K1) * r2);
                float deltaX = 2 * P1 * x * y + P2 * (r2 + 2 * x * x);
                float deltaY = P1 * (r2 + 2 * y * y) + 2 * P2 * x * y;
                x = (x0 - deltaX) * icdist;
                y = (y0 - deltaY) * icdist;
            }
            return new Vector2(x, y);
        }

        public Vector3 UnprojRaw(Vector2 p, DoUndistort undistort, bool invertX = false, bool invertY = false)
        {
            float x = (p.x - Cx) / Fx;
            float y = (p.y - Cy) / Fy;
            x = invertX ? -x : x;
            y = invertY ? -y : y;
            Vector2 undis;
            if (undistort == DoUndistort.YES)
            {
                undis = Undistort(x, y);
                return new Vector3(undis.x, undis.y, 1.0f).normalized;
            }
            else
                return new Vector3(x, y, 1.0f).normalized;
        }

        public Vector3 Unproj(Vector2 p, DoUndistort undistort, bool invertX = false, bool invertY = false)
        {
            return UnprojRaw(new Vector2(p.x * Width, p.y * Height), undistort, invertX, invertY);
        }

        public bool IntrinsicValid { get; protected set; } = false;
        public bool ExtrinsicValid { get; protected set; } = false;
        public new bool valid { get { return IntrinsicValid && ExtrinsicValid; } }
    }
}