using System;
using System.Collections.Generic;
using UnityEngine;

namespace LCY
{
    public abstract class Stablizer<T>
    {
        protected T value;
        public T Value
        {
            get { return value; }
            set { AddMeasure(value); }
        }
        public bool Stable { get; protected set; }

        //protected List<Tuple<T, float>> Poses { get; set; }
        protected int Count { get; set; }
        public readonly int WindowSize;

        public Stablizer(int windowSize = 20)
        {
            WindowSize = windowSize;
            Reset();
        }

        public void AddMeasure(T v)
        {
            value = Lerp(value, v, 1.0f / Count);
            if (Count < WindowSize)
                ++Count;
            else
                Stable = true;
        }

        public abstract T Lerp(T a, T b, float t);

        public void Reset()
        {
            Stable = false;
            Count = 0;
            //Poses = new List<Tuple<T, float>>();
        }
    }

    public class SE3Stablizer : Stablizer<SE3>
    {
        public override SE3 Lerp(SE3 a, SE3 b, float t)
        {
            return b.Lerp(a, b, t);
        }
    }

    public class Vector3Stablizer : Stablizer<Vector3>
    {
        public override Vector3 Lerp(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Lerp(a, b, t);
        }
    }
}
