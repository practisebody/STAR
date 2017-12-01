using LCY;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace STAR
{
    public class Checkerboard : SE3Object
    {
        public HololensCamera HololensCamera;
        protected Transform _Checkerboard { get; set; }

        public int X { get; protected set; }
        public int Y { get; protected set; }
        public float Size { get; protected set; }
        protected Plane plane;
        public Plane Plane { get { return plane; } }

        void Start()
        {
            _Checkerboard = transform.Find("Checkerboard");
            LCY.Utilities.SetVisibility(transform, false);
            Configurations.Instance.SetAndAddCallback("ShowChecker", true, v => gameObject.SetActive(v), Configurations.CallNow.YES, Configurations.RunOnMainThead.YES);
            OnChange += (sender) => plane = new Plane(-transform.forward, transform.position);
        }

        public void SetCheckerSize(int x, int y, float size)
        {
            LCY.Utilities.SetVisibility(transform, true);
            X = x;
            Y = y;
            Size = size;
            Mesh mesh = _Checkerboard.GetComponent<MeshFilter>().mesh;
            mesh.vertices = new Vector3[]
            {
                new Vector2(-size, -size),
                new Vector2(x * size, y * size),
                new Vector2(x * size, -size),
                new Vector2(-size, y * size),
            };
            float u = (x + 1) * 0.5f;
            float v = (y + 1) * 0.5f;
            mesh.uv = new Vector2[]
            {
                new Vector2(0.0f, 0.0f),
                new Vector2(u, v),
                new Vector2(u, 0.0f),
                new Vector2(0.0f, v),
            };
        }

        public void FromImage(byte[] LatestImageBytes)
        {
            // TODO(chengyuanlin)
        }
    }
}