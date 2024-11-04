using System;
using UnityEngine;
namespace ProcWorld
{
    public class util
    {
        public static int LCGRandom(int seed, int minValue, int maxValue)
        {
            int a = 1664525;
            int c = 1013904223;



            // simplified version by chatgpt probably using some poor man's stack overflow 
            // it just multiplied the seed with some random numbers
            // if you want proper LCG, refer to the wiki page. 
            // LCG: X_n+1 = (a * X_n + c) % m
            seed = a * seed + c;

            // Map the seed to a value in the desired range
            int range = maxValue - minValue;
            return (Math.Abs(seed) % range) + minValue;
        }


        // Rotate point around by -deg where deg is rectangle's rotation
        //https://gamedev.stackexchange.com/questions/86420/how-do-i-calculate-the-distance-between-a-point-and-a-rotated-rectangle


        public static Vector2 RotateAround(Vector2 _point, Vector2 _around, float _radians)
        {

            return RotateAround(_point.x, _point.y,_around.x, _around.y, _radians);
        }

        public static Vector2 RotateAround(float _pointx, float _pointy, float _aroundx, float _aroundy, float _radians)
        {

            float relX = _pointx - _aroundx;
            float relY = _pointy - _aroundy;

            float rotatedRelX = relX * Mathf.Cos(-_radians) - relY * Mathf.Sin(-_radians);
            float rotatedRelY = relX * Mathf.Sin(-_radians) + relY * Mathf.Cos(-_radians);



            _pointx = rotatedRelX + _aroundx;
            _pointy = rotatedRelY + _aroundy;
            return new Vector2(_pointx, _pointy);
        }

        public static bool IsInBounds(Vector2 _point, Vector2 _boundCenter, Vector2 _boundsSize)
        {
            _point -= _boundCenter;


            bool overlapX = Mathf.Abs(_point.x) <= _boundsSize.x / 2f;
            bool overlapY = Mathf.Abs(_point.y) <= _boundsSize.y / 2f;


            return overlapX && overlapY;
        }
    }
}