using System;
using UnityEngine;

namespace NyanbreFOVEAS
{
    public class Utils
    {
        private const float TOLERANCE = 0.0001f;
        public static bool NotEqual(float a, float b)
        {
            return Math.Abs(a - b) > TOLERANCE;
        }
        public static bool Equal(float a, float b)
        {
            return Math.Abs(a - b) < TOLERANCE;
        }

        public static bool ThresholdFloor(float value, float deltaTime)
        {
            return Mathf.FloorToInt(value) != Mathf.FloorToInt(value + deltaTime);
        }
    }
}