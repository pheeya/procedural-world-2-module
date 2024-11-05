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

            return RotateAround(_point.x, _point.y, _around.x, _around.y, _radians);
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

        public static float SqrDistanceFromRectangle(Vector2 _point, Vector2 _rectCenter, Vector2 _rectSize)
        {


            Vector2 rectTopRight = _rectCenter;
            rectTopRight.x += _rectSize.x / 2f;
            rectTopRight.y += _rectSize.y / 2f;

            Vector2 rectBottomRight = _rectCenter;
            rectBottomRight.x += _rectSize.x / 2f;
            rectBottomRight.y -= _rectSize.y / 2f;


            Vector2 rectTopLeft = _rectCenter;
            rectTopLeft.x -= _rectSize.x / 2f;
            rectTopLeft.y += _rectSize.y / 2f;

            Vector2 rectBottomLeft = _rectCenter;
            rectBottomLeft.x -= _rectSize.x / 2f;
            rectBottomLeft.y -= _rectSize.y / 2f;


            float dx = Mathf.Max(rectTopLeft.x - _point.x, 0, _point.x - rectTopRight.x);
            float dy = Mathf.Max(rectBottomLeft.y - _point.y, 0, _point.y - rectTopLeft.y);

            return dx * dx + dy * dy;
        }

        public static bool IntersectSegments(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {

            Vector2 dirA = (a2 - a1);
            Vector2 dirB = (b2 - b1);


            // a1.x + t*dirA.x = b1.x + u*dirB.x;    ---- 1
            // a1.y + t*dirA.y = b1.y + u*dirB.y;    ---- 2

            // t = (b1.y + u*dirB.y - a1.y)/dirA.y ---- 3

            // u = (a1.x + t*dirA.x - b1.x)/dirB.x ---- 4


            // t = (b1.y +  ((a1.x + t*dirA.x -b1.x)/dirB.x) *dirB.y - a1.y )/dirA.y
            // t * dirA.y = (b1.y +  ((a1.x + t*dirA.x -b1.x)/dirB.x) *dirB.y - a1.y )


            // t*dirA.y * dirB.x = b1.y *dirB.x + (a1.x + t*dirA.x - b1.x)*dirB.y - a1.y *dirB.x

            // t*dirA.y*  dirB.x - t*dirA.x*dirB.y = b1.y*dirB.x + (a1.x-b1.x)*dirB.1 - a1.y*dirB.x
            // t = (b1.y*dirB.x + (a1.x-b1.x)*dirB.y - a1.y*dirB.x) / (dirA.y * dirB.x - dirA.x*dirB.y)

            // t = (dirB.x(b1.y - a1.y) + (a1.x-b1.x)*dirB.y) / (dirA.y * dirB.x - dirA.x*dirB.y)
            float den = (dirA.y * dirB.x - dirA.x * dirB.y);
            if (Mathf.Approximately(den, 0))
            {
                return false;
            }
            // float t = (dirB.x * (b1.y - a1.y) + (a1.x - b1.x) * dirB.y) / den;


            // float u = (a1.x + t * dirA.x - b1.x) / dirB.x;

            // alternative for u, to avoid dividied by 0 cases for when dirB.x = 0
            // float u = (dirA.x * (b1.y - a1.y) + (a1.x - b1.x) * dirA.y) / den
            // this was derrived by equation 3 into equation 1

            // using the previous u equation results in errors, u goes to infinity and intersection is dropped


            float t = (dirB.x * (b1.y - a1.y) + (a1.x - b1.x) * dirB.y) / den;
            float u = (dirA.x * (b1.y - a1.y) + (a1.x - b1.x) * dirA.y) / den;


            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }
    }
}