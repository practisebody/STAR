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

        protected bool VisualCheckerboard { get; set; } = true;

        public int x { get; protected set; } = 9;
        public int y { get; protected set; } = 6;
        public float size { get; protected set; } = 0.05f;
        protected Plane plane;
        public Plane Plane { get { return plane; } }

        public new bool valid { get { return x > 0 && y > 0 && size > 0.0f; } }

        void Start()
        {
            _Checkerboard = transform.Find("Checkerboard");
            Configurations.Instance.SetAndAddCallback("Checker_X", x, v => { x = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Checker_Y", y, v => { y = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Checker_Size", size, v => { size = v; Refresh(); }, Configurations.RunOnMainThead.YES);
            Configurations.Instance.SetAndAddCallback("Visual_Checkerboard", VisualCheckerboard, v => gameObject.SetActive(VisualCheckerboard = v), Configurations.RunOnMainThead.YES);
            Refresh();
            gameObject.SetActive(false);
            OnChange += (sender) =>
            {
                Debug.Log("new plane!");
                plane = new Plane(-transform.forward, transform.position);
                Debug.Log("plane" + plane == null);
                WorldManager.Instance.Up = -transform.forward;
                WorldManager.Instance.position = transform.position;
                gameObject.SetActive(VisualCheckerboard);
            };
        }

        protected void Refresh()
        {
            if (valid == false)
                return;
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

        public void SetCheckerSize(int x, int y, float size)
        {
            this.x = x;
            this.y = y;
            this.size = size;
            Refresh();
            // TODO(chengyuanlin)
            //ControllerManager.Instance.SendControl();
        }

        public void FromImage(byte[] LatestImageBytes)
        {
            // TODO(chengyuanlin)
        }
    }
}