using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using System.Threading;
using UnityEngine.AI;
namespace ProcWorld
{
    public class NoiseGenerator
    {
        public static float[,] GenerateNoiseMap(PerlinNoiseConfig _config, int _width, int _height, float _offsetX, float _offsetY)
        {
            float[,] map = new float[_width, _height];
            Vector2[] octaveOffsets = GetOctaveOffsets(_config, _offsetX, _offsetY);
            float halfWidth = _width / 2f;
            float halfHeight = _height / 2f;


            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {

                    map[x, y] = GetPerlinValue(_config, x, y, octaveOffsets, -halfWidth, -halfHeight);


                }
            }
            return map;
        }
        public static void GenerateNoiseMapNonAlloc(float[,] map, PerlinNoiseConfig _config, int _width, int _height, float _offsetX, float _offsetY)
        {


            Vector2[] octaveOffsets = GetOctaveOffsets(_config, _offsetX, _offsetY);
            float halfWidth = _width / 2f;
            float halfHeight = _height / 2f;


            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {

                    map[x, y] = GetPerlinValue(_config, x, y, octaveOffsets, -halfWidth, -halfHeight);
                    map[x, y] += 1;
                    map[x, y] /= 2;
                }
            }
        }


        public static float[,] NormalizeGlobally(float[,] map, int _height, int _width, float _maxValue)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    float normH = (map[x, y] + 1) / 2f;
                    normH /= _maxValue * .5f;

                    map[x, y] = normH;
                }
            }

            return map;
        }
        public static void NormalizeGloballyNonAlloc(float[,] map, int _height, int _width, float _maxValue)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    float normH = (map[x, y]);
                    normH /= _maxValue * .5f;

                    map[x, y] = normH;
                }
            }
        }
        public static void NormalizeGloballyNonAlloc(PerlinNoiseConfig _noise, float[,] map, int _height, int _width, float _mul)
        {

            float amplitude = 1;
            float value = 0;
            for (int o = 0; o < _noise.octaves; o++)
            {


                // figure out why we are changing this range from 01 to -1 1
                // then make sure returned value is 0-1
                // road noise works fine with perlin -1 to 1
                // not sure why but it doesn't work properly if we don't do this

                value += amplitude;


                amplitude *= _noise.persistance;
            }




            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    float normH = map[x, y] + .05f; // add a small number to keep all values above 0, Mathf.perlin can return values slightly smaller than 0 and larger than
                    normH /= value * _mul;

                    map[x, y] = normH;
                }
            }
        }
        public static float[,] GenerateSingleAxisNoiseMap(PerlinNoiseConfig _config, int _width, int _height, float _offsetX, float _offsetY)
        {
            float[,] map = new float[1, _height];
            Vector2[] octaveOffsets = GetOctaveOffsets(_config, _offsetX, _offsetY);
            float halfWidth = _width / 2f;
            float halfHeight = _height / 2f;


            for (int y = 0; y < _height; y++)
            {
                map[0, y] = GetPerlinValue(_config, 0, y, octaveOffsets, -halfWidth, -halfHeight);
            }

            return map;
        }
        public static float GetPerlinValue(PerlinNoiseConfig _config, float _x, float _y, Vector2[] _octaves, float _additionalOffsetX, float _additionalOffsetY)
        {

            float amplitude = 1;
            float frequency = 1;
            float value = 0;


            for (int o = 0; o < _config.octaves; o++)
            {
                float samplex = (_x + _additionalOffsetX + _octaves[o].x) / _config.scale * frequency;
                float sampley = (_y + _additionalOffsetY + _octaves[o].y) / _config.scale * frequency;


                // figure out why we are changing this range from 01 to -1 1
                // then make sure returned value is 0-1
                // road noise works fine with perlin -1 to 1
                // not sure why but it doesn't work properly if we don't do this
                float perlin = Mathf.PerlinNoise(samplex, sampley) * 2 - 1; // change perlinnoise range to -1 1

                value += perlin * amplitude;


                amplitude *= _config.persistance;
                frequency *= _config.lacunarity;
            }


            // for (int y = 0; y < _mapheight; y++)
            // {
            //     for (int x = 0; x < _config.width; x++)
            //     {
            //         map[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, map[x, y]);
            //     }
            // }
            return value;

        }

        public static Vector2[] GetOctaveOffsets(PerlinNoiseConfig _config, float _offsetX, float _offsetY, out float MaxPossibleValue)
        {
            System.Random prng = new System.Random(_config.seed);
            Vector2[] octaveOffsets = new Vector2[_config.octaves];
            float amp = 1;

            float maxPossibleNoise = 0;
            for (int i = 0; i < _config.octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + _offsetX;
                float offsetY = prng.Next(-100000, 100000) - _offsetY;

                octaveOffsets[i] = new Vector2(offsetX, offsetY);

                maxPossibleNoise += amp;
                amp *= _config.persistance;
            }

            MaxPossibleValue = maxPossibleNoise;

            return octaveOffsets;
        }




        public static Vector2[] GetOctaveOffsets(PerlinNoiseConfig _config, float _offsetX, float _offsetY)
        {
            System.Random prng = new System.Random(_config.seed);
            Vector2[] octaveOffsets = new Vector2[_config.octaves];

            for (int i = 0; i < _config.octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + _offsetX;
                float offsetY = prng.Next(-100000, 100000) - _offsetY;

                octaveOffsets[i] = new Vector2(offsetX, offsetY);

            }

            return octaveOffsets;
        }


        public static float[,] GenerateLongitudinalSinNoise(int _width, int _height, RoadNoiseConfig _roadConfig, float _offsetX, float _offsetY, PerlinNoiseConfig _horizontalNoise, PerlinNoiseConfig _verticalNoise)
        {

            if (_roadConfig.brushSpacing < 1)
            {
                _roadConfig.brushSpacing = 1;
            }



            int blurredMapWidth = _width + _roadConfig.blurPadding;

            float[,] map = new float[_width, _height];


            float[,] blurredMap = new float[blurredMapWidth, blurredMapWidth];


            Vector2 previousPos = Vector2.zero;


            int startingYPos = 0;

            int dif = blurredMapWidth - _width;
            dif /= 2;
            Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_horizontalNoise, 0, _offsetY);
            Vector2[] verticalOctaveOffsets = GetOctaveOffsets(_verticalNoise, 0, _offsetY);

            for (int y = 0; y < blurredMapWidth; y += _roadConfig.brushSpacing)
            {
                int xPosLocal = blurredMapWidth / 2;

                float hw = blurredMapWidth / 2f;
                float hh = blurredMapWidth / 2f;



                int worldYPos = startingYPos - y + (int)_offsetY + (int)(blurredMapWidth) / 2;

                // only happens once at the start, after that y is always a factor of _brushSpacing
                if (y == 0)
                {
                    int mod = worldYPos % _roadConfig.brushSpacing;
                    if (mod != 0)
                    {
                        startingYPos -= mod;
                        worldYPos = startingYPos - y + (int)_offsetY + (int)(blurredMapWidth) / 2;

                    }
                }





                // float perlin = Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPosLocal, y - startingYPos, horizontalOctaveOffsets, -hw, -hh) * _roadConfig.amplitude * ((_width - 2) / 2.0f)); ;
                // xPosLocal += Mathf.RoundToInt(perlin);
                xPosLocal = GetPointOnLongNoise(horizontalOctaveOffsets, y - startingYPos, _width, _horizontalNoise, _roadConfig, _offsetY);
                xPosLocal -= (int)_offsetX;

                Vector2 currentPos = new(xPosLocal, y - startingYPos);


                if (y > 0)
                {
                    float dist = (currentPos - previousPos).magnitude;
                    if (dist > _roadConfig.brushSpacing)
                    {
                        int strokes = Mathf.FloorToInt(dist / _roadConfig.brushSpacing);
                        for (float t = 0; t < 1; t += (1f / strokes))
                        {
                            Vector2 pos = Vector2.Lerp(previousPos, currentPos, t);
                            blurredMap = StampCircle(blurredMap, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), _roadConfig.brushRadius, _roadConfig.brush);

                        }

                    }
                }

                blurredMap = StampCircle(blurredMap, xPosLocal, y - startingYPos, _roadConfig.brushRadius, _roadConfig.brush);
                previousPos = currentPos;

            }

            blurredMap = ApplyBlur(blurredMap, _roadConfig.blurAmount);

            for (int y = dif; y < blurredMapWidth - dif; y++)
            {
                for (int x = dif; x < blurredMapWidth - dif; x++)
                {
                    map[x - dif, y - dif] = blurredMap[x, y];

                }
            }



            return map;
        }

        public static void GenerateCurveNonAlloc(float[,] generatedMap, int _width, int _height, CurveConfig _config, float _offsetX, float _offsetY)
        {

            System.Diagnostics.Stopwatch sw = new();

            int blurredMapWidth = _width + _config.padding;
            _config.distanceModifier = new(_config.distanceModifier.keys);



            // instead of generating a blur map (which we don't need anymore in this function)
            // generate a 1d Vector2 array of points.
            Vector2 previous = Vector3.zero;
            Vector2[] curve = new Vector2[blurredMapWidth * 4];

            int curveCount = 0;

            int xCoord;

            Vector2 point = Vector2.zero;
            Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_config.perlinConfig, 0, _offsetY);
            int yCoord = 0;
            while (yCoord < blurredMapWidth)
            {

                xCoord = GetPointOnLongNoise(horizontalOctaveOffsets, yCoord, _width, _config, _offsetY);
                xCoord -= (int)_offsetX;
                point.x = xCoord;
                point.y = yCoord;
                if (yCoord > 0)
                {
                    previous = curve[curveCount - 1];
                    Vector2 dif = (point - previous);
                    while (dif.magnitude > 1)
                    {

                        Vector2 fillPoint = previous + dif.normalized;
                        curve[curveCount] = (fillPoint);
                        dif = point - fillPoint;
                        previous = fillPoint;

                        curveCount++;
                    }

                }
                curve[curveCount] = point;

                curveCount++;

                yCoord++;
            }





            Vector2 target;
            float normalizedDist;
            Vector2 dist;
            float distSquared;
            float widthSquared = _config.width * _config.width;
            float halfPadding = _config.padding / 2;
            for (int y = 0; y < _width; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    float closestDistance = float.MaxValue;
                    point.x = x;
                    point.y = y;


                    int i = 0;


                    while (i < curveCount)
                    {

                        target = curve[i];


                        // offset by padding/2 to bring it to the middle of the actual noise map
                        dist.x = target.x - point.x - halfPadding;
                        dist.y = target.y - point.y - halfPadding;


                        distSquared = dist.x * dist.x + dist.y * dist.y;


                        if (distSquared < closestDistance)
                        {
                            closestDistance = distSquared;
                        }


                        i++;
                    }

                    normalizedDist = Mathf.Sqrt(closestDistance) / _config.width;
                    normalizedDist = Mathf.Clamp01(normalizedDist);
                    if (_config.invert)
                    {
                        generatedMap[x, y] = 1 - _config.distanceModifier.Evaluate(1 - normalizedDist);

                    }
                    else
                    {

                        generatedMap[x, y] = _config.distanceModifier.Evaluate(1 - normalizedDist);
                    }
                }
            }





        }

        // public static void FlattenRectangle(float[,] _map, float _targetNormalizedHeight, float _blend, Vector2 _rectWorldCenter, float _rotation, Vector2 _rectSize, AnimationCurve _fallOff, float _fallOffSize, float _offsetX, float _offsetY)
        // {

        //     float halfWidth = _map.GetLength(0) / 2f;
        //     float halfHeight = _map.GetLength(1) / 2f;

        //     // chunk not within rectangle bounds
        //     if (Mathf.Abs(_offsetX - _rectWorldCenter.x) > Mathf.Abs(_rectSize.x / 2f + halfWidth + _fallOffSize) || Mathf.Abs(_offsetY + _rectWorldCenter.y) > Mathf.Abs(_rectSize.y / 2f + halfWidth + _fallOffSize))
        //     {

        //         return;
        //     };



        //     Vector2 topLeft = new(_rectWorldCenter.x - _rectSize.x / 2f, _rectWorldCenter.y + _rectSize.y / 2f);
        //     Vector2 topRight = new(_rectWorldCenter.x + _rectSize.x / 2f, _rectWorldCenter.y + _rectSize.y / 2f);




        //     // Y is same for left or right pos
        //     Vector2 bottomLeft = new(_rectWorldCenter.x - _rectSize.x / 2f, _rectWorldCenter.y - _rectSize.y / 2f);


        //     float rectMag = _rectSize.magnitude;


        //     AnimationCurve _threadSafe = new(_fallOff.keys);


        //     for (int y = 0; y < _map.GetLength(1); y++)
        //     {
        //         for (int x = 0; x < _map.GetLength(0); x++)
        //         {



        //             // https://stackoverflow.com/questions/5254838/calculating-distance-between-a-point-and-a-rectangular-box-nearest-point
        //             // distance to rectangle

        //             float xWorld = x + _offsetX - halfWidth;
        //             float yWorld = y - _offsetY - halfHeight;

        //             float dist = 0;



        //             float bleedX = Mathf.Abs(xWorld) - Mathf.Abs(_rectWorldCenter.x / 2f);
        //             float bleedY = Mathf.Abs(yWorld) - Mathf.Abs(_rectWorldCenter.y / 2f);
        //             if (bleedX > 0 || bleedY > 0)
        //             {

        //                 float dx = Mathf.Max(topLeft.x - xWorld, 0, xWorld - topRight.x);
        //                 float dy = Mathf.Max(bottomLeft.y - yWorld, 0, yWorld - topLeft.y);
        //                 dist = Mathf.Sqrt(dx * dx + dy * dy) / _fallOffSize;
        //                 dist = 1 - dist;
        //                 dist = Mathf.Clamp01(dist);
        //                 dist = _threadSafe.Evaluate(dist);
        //             }


        //             _map[x, y] = Mathf.Lerp(_map[x, y], _targetNormalizedHeight, dist * _blend);

        //         }
        //     }

        // }
        public static void FlattenRectangle(float[,] _map, float _targetNormalizedHeight, float _blend, Vector2 _rectWorldCenter, float _rotation, Vector2 _rectSize, AnimationCurve _fallOff, float _fallOffSize, float _offsetX, float _offsetY)
        {

            float halfWidth = _map.GetLength(0) / 2f;
            float halfHeight = _map.GetLength(1) / 2f;

            Vector2 mapSize = new Vector2(halfWidth * 2f, halfHeight * 2f);

            // chunk not within rectangle bounds
            Vector2 chunkCenter = new(_offsetX, _offsetY);

            Vector2 rectTopRight = _rectWorldCenter;
            rectTopRight.x += _rectSize.x / 2f;
            rectTopRight.y += _rectSize.y / 2f;

            Vector2 rectBottomRight = _rectWorldCenter;
            rectBottomRight.x += _rectSize.x / 2f;
            rectBottomRight.y -= _rectSize.y / 2f;


            Vector2 rectTopLeft = _rectWorldCenter;
            rectTopLeft.x -= _rectSize.x / 2f;
            rectTopLeft.y += _rectSize.y / 2f;

            Vector2 rectBottomLeft = _rectWorldCenter;
            rectBottomLeft.x -= _rectSize.x / 2f;
            rectBottomLeft.y -= _rectSize.y / 2f;





            Vector2 rectTopRightRot = util.RotateAround(rectTopRight, _rectWorldCenter, -_rotation);
            Vector2 rectTopLeftRot = util.RotateAround(rectTopLeft, _rectWorldCenter, -_rotation);
            Vector2 rectBottomRightRot = util.RotateAround(rectBottomRight, _rectWorldCenter, -_rotation);
            Vector2 rectBottomLeftRot = util.RotateAround(rectBottomLeft, _rectWorldCenter, -_rotation);



            // if (Mathf.Abs(rotatedChunkPosition.x - _rectWorldCenter.x) > Mathf.Abs(_rectSize.x / 2f + halfWidth + _fallOffSize) || Mathf.Abs(rotatedChunkPosition.y - _rectWorldCenter.y) > Mathf.Abs(_rectSize.y / 2f + halfWidth + _fallOffSize))

            if (
                util.IsInBounds(rectTopRightRot, chunkCenter, mapSize) ||
                util.IsInBounds(rectTopLeftRot, chunkCenter, mapSize) ||
                util.IsInBounds(rectBottomRightRot, chunkCenter, mapSize) ||
                util.IsInBounds(rectBottomLeftRot, chunkCenter, mapSize)
            )
            {

            }
         




            float rectMag = _rectSize.magnitude;


            AnimationCurve _threadSafe = new(_fallOff.keys);


            for (int y = 0; y < _map.GetLength(1); y++)
            {
                for (int x = 0; x < _map.GetLength(0); x++)
                {



                    // https://stackoverflow.com/questions/5254838/calculating-distance-between-a-point-and-a-rectangular-box-nearest-point
                    // distance to rectangle

                    float xWorld = x + _offsetX - halfWidth;
                    float yWorld = y + _offsetY - halfHeight;


                    Vector2 rotated = util.RotateAround(xWorld, yWorld, _rectWorldCenter.x, _rectWorldCenter.y, -_rotation);

                    float dist = 0;

                    xWorld = rotated.x;
                    yWorld = rotated.y;


                    float dx = Mathf.Max(rectTopLeft.x - xWorld, 0, xWorld - rectTopRight.x);
                    float dy = Mathf.Max(rectBottomLeft.y - yWorld, 0, yWorld - rectTopLeft.y);



                    dist = Mathf.Sqrt(dx * dx + dy * dy) / _fallOffSize;
                    dist = 1 - dist;
                    dist = Mathf.Clamp01(dist);
                    dist = _threadSafe.Evaluate(dist);

                    _map[x, y] = Mathf.Lerp(_map[x, y], _targetNormalizedHeight, dist * _blend);

                }
            }

        }
        public static void GenerateLongitudinalSinNoiseNonAlloc(float[,] generatedMap, float[,] generatedBlurredMap, int _width, int _height, RoadNoiseConfig _roadConfig, float _offsetX, float _offsetY, PerlinNoiseConfig _horizontalNoise, PerlinNoiseConfig _verticalNoise)
        {

            if (_roadConfig.brushSpacing < 1)
            {
                _roadConfig.brushSpacing = 1;
            }
            // animation curve still not working correctly it seems even though only one thread should be accessing it at a time
            // _roadConfig.brush = new(_roadConfig.brush.keys);


            int blurredMapWidth = _width + _roadConfig.blurPadding;





            Vector2 previousPos = Vector2.zero;


            int startingYPos = 0;

            int dif = blurredMapWidth - _width;
            dif /= 2;
            Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_horizontalNoise, 0, _offsetY);
            Vector2[] verticalOctaveOffsets = GetOctaveOffsets(_verticalNoise, 0, _offsetY);


            // find a way to not have to allocate memory every time
            AnimationCurve brush = new(_roadConfig.brush.keys);

            for (int y = 0; y < blurredMapWidth; y += _roadConfig.brushSpacing)
            {
                int xPosLocal = blurredMapWidth / 2;

                float hw = blurredMapWidth / 2f;
                float hh = blurredMapWidth / 2f;



                int worldYPos = startingYPos - y + (int)_offsetY + (int)(blurredMapWidth) / 2;

                // only happens once at the start, after that y is always a factor of _brushSpacing
                if (y == 0)
                {
                    int mod = worldYPos % _roadConfig.brushSpacing;
                    if (mod != 0)
                    {
                        startingYPos -= mod;
                        worldYPos = startingYPos - y + (int)_offsetY + (int)(blurredMapWidth) / 2;

                    }
                }





                // float perlin = Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPosLocal, y - startingYPos, horizontalOctaveOffsets, -hw, -hh) * _roadConfig.amplitude * ((_width - 2) / 2.0f)); ;
                // xPosLocal += Mathf.RoundToInt(perlin);
                xPosLocal = GetPointOnLongNoise(horizontalOctaveOffsets, y - startingYPos, _width, _horizontalNoise, _roadConfig, _offsetY);
                xPosLocal -= (int)_offsetX;

                Vector2 currentPos = new(xPosLocal, y - startingYPos);


                if (y > 0)
                {
                    float dist = (currentPos - previousPos).magnitude;
                    if (dist > _roadConfig.brushSpacing)
                    {
                        int strokes = Mathf.FloorToInt(dist / _roadConfig.brushSpacing);
                        for (float t = 0; t < 1; t += (1f / strokes))
                        {
                            Vector2 pos = Vector2.Lerp(previousPos, currentPos, t);
                            StampCircleNonAlloc(generatedBlurredMap, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), _roadConfig.brushRadius, brush);

                        }

                    }
                }

                StampCircleNonAlloc(generatedBlurredMap, xPosLocal, y - startingYPos, _roadConfig.brushRadius, brush);
                previousPos = currentPos;

            }

            generatedBlurredMap = ApplyBlur(generatedBlurredMap, _roadConfig.blurAmount);

            for (int y = dif; y < blurredMapWidth - dif; y++)
            {
                for (int x = dif; x < blurredMapWidth - dif; x++)
                {
                    float val = generatedBlurredMap[x, y];
                    generatedMap[x - dif, y - dif] = val;
                }
            }



        }
        public static int GetPointOnLongNoise(int _y, int _width, PerlinNoiseConfig _horizontalNoise, RoadNoiseConfig _roadConfig, float _offsetY)
        {

            Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_horizontalNoise, 0, _offsetY);
            return GetPointOnLongNoise(horizontalOctaveOffsets, _y, _width, _horizontalNoise, _roadConfig, _offsetY);
        }

        public static int GetPointOnLongNoise(int _y, int _width, CurveConfig _config, float _offsetY)
        {

            Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_config.perlinConfig, 0, _offsetY);
            return GetPointOnLongNoise(horizontalOctaveOffsets, _y, _width, _config, _offsetY);
        }
        public static int GetPointOnLongNoise(Vector2[] _horizontalOctaveOffsets, int _y, int _width, CurveConfig _config, float _offsetY)
        {

            int blurredMapWidth = _width + _config.padding;
            float hw = blurredMapWidth / 2f;
            float hh = blurredMapWidth / 2f;

            int xPosLocal = blurredMapWidth / 2;
            float perlin = Mathf.RoundToInt(GetPerlinValue(_config.perlinConfig, xPosLocal, _y, _horizontalOctaveOffsets, -hw, -hh) * _config.amplitude * ((_width - 2) / 2.0f)); ;
            xPosLocal += Mathf.RoundToInt(perlin);
            return xPosLocal;
        }
        public static int GetPointOnLongNoise(Vector2[] _horizontalOctaveOffsets, int _y, int _width, PerlinNoiseConfig _horizontalNoise, RoadNoiseConfig _roadConfig, float _offsetY)
        {

            int blurredMapWidth = _width + _roadConfig.blurPadding;
            float hw = blurredMapWidth / 2f;
            float hh = blurredMapWidth / 2f;

            int xPosLocal = blurredMapWidth / 2;
            float perlin = Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPosLocal, _y, _horizontalOctaveOffsets, -hw, -hh) * _roadConfig.amplitude * ((_width - 2) / 2.0f)); ;
            xPosLocal += Mathf.RoundToInt(perlin);
            return xPosLocal;
        }
        public static Vector2 GetLongNoiseGradient(int _y, int _width, PerlinNoiseConfig _horizontalNoise, RoadNoiseConfig _roadConfig, float _offsetY)
        {

            Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_horizontalNoise, 0, _offsetY);
            int blurredMapWidth = _width + _roadConfig.blurPadding;
            float hw = blurredMapWidth / 2f;
            float hh = blurredMapWidth / 2f;

            int y1, y2, y3;
            y1 = _y;
            y2 = y1 + 1;
            y3 = y1 + 2;


            int xPosLocal = blurredMapWidth / 2;
            int perlin = Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPosLocal, y1, horizontalOctaveOffsets, -hw, -hh) * _roadConfig.amplitude * ((_width - 2) / 2.0f)); ;
            int perlin1 = Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPosLocal, y2, horizontalOctaveOffsets, -hw, -hh) * _roadConfig.amplitude * ((_width - 2) / 2.0f)); ;
            int perlin2 = Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPosLocal, y3, horizontalOctaveOffsets, -hw, -hh) * _roadConfig.amplitude * ((_width - 2) / 2.0f)); ;

            int x1, x2, x3;
            x1 = xPosLocal + perlin;
            x2 = xPosLocal + perlin1;
            x3 = xPosLocal + perlin2;


            Vector2 p1, p2, p3;
            p1 = new(x1, y1);
            p2 = new(x2, y2);
            p3 = new(x3, y3);

            Vector2 gradient = (p3 - p2);
            Vector2 gradient2 = (p2 - p1);

            return (gradient + gradient2).normalized;
        }
        static int maxLogs = 100;
        static int logs = 0;
        public static float[,] StampCircle(float[,] _noise, int _centerX, int _centerY, int _radius, AnimationCurve _brush)
        {


            Vector2 pos;
            float dist;
            float radiusSquared = _radius * _radius;



            for (int y = _centerY - _radius; y < _centerY + _radius; y++)
            {
                if (y < 0 || y > _noise.GetLength(0) - 1) continue;

                for (int x = _centerX - _radius; x < _centerX + _radius; x++)
                {
                    if (x < 0 || x > _noise.GetLength(0) - 1) continue;

                    int dx = x - _centerX;
                    int dy = y - _centerY;
                    int distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared <= radiusSquared)
                    {
                        pos = new Vector2(x, y);

                        // circle shape
                        dist = (pos - new Vector2(_centerX, _centerY)).magnitude / _radius;
                        dist = Mathf.Clamp01(dist);




                        // box shape 
                        // dist = (pos - new Vector2(_centerX, pos.y)).magnitude / _radius;
                        // dist = Mathf.Clamp01(dist);


                        dist = 1 - dist;
                        // _noise[x, y] += dist > 0 ? 1 : 0;
                        _noise[x, y] += _brush.Evaluate(dist);


                        _noise[x, y] = Mathf.Clamp01(_noise[x, y]);
                    }
                }
            }


            return _noise;
        }

        public static void StampCircleNonAlloc(float[,] _noise, int _centerX, int _centerY, int _radius, AnimationCurve _brush)
        {

            Vector2 pos;
            float dist;
            float radiusSquared = _radius * _radius;



            for (int y = _centerY - _radius; y < _centerY + _radius; y++)
            {
                if (y < 0 || y > _noise.GetLength(0) - 1) continue;

                for (int x = _centerX - _radius; x < _centerX + _radius; x++)
                {
                    if (x < 0 || x > _noise.GetLength(0) - 1) continue;

                    int dx = x - _centerX;
                    int dy = y - _centerY;
                    int distanceSquared = dx * dx + dy * dy;

                    if (distanceSquared <= radiusSquared)
                    {
                        pos = new Vector2(x, y);

                        // circle shape
                        dist = (pos - new Vector2(_centerX, _centerY)).magnitude / _radius;
                        dist = Mathf.Clamp01(dist);




                        // box shape 
                        // dist = (pos - new Vector2(_centerX, pos.y)).magnitude / _radius;
                        // dist = Mathf.Clamp01(dist);


                        dist = 1 - dist;
                        // _noise[x, y] += dist > 0 ? 1 : 0;
                        _noise[x, y] += _brush.Evaluate(dist);


                        _noise[x, y] = Mathf.Clamp01(_noise[x, y]);
                    }
                }
            }

        }

        delegate void BlurParamThreadStart(int _lineIndex, int _lineNum);
        struct BlurParams
        {
            public int lineIndex;
            public int lineCount;
        }

        // find a way to make this non allocating
        public static float[,] ApplyBlur(float[,] inputArray, int blurSize)
        {
            int width = inputArray.GetLength(0);
            int height = inputArray.GetLength(1);
            float[,] resultArray = new float[width, height];



            // void blur(object _params)
            // {
            //     int _lineIndex = ((BlurParams)_params).lineIndex;
            //     int _lineNum = ((BlurParams)_params).lineCount;
            //     for (int x = 0; x < width; x++)
            //     {
            //         for (int y = _lineIndex; y < _lineIndex + _lineNum; y++)
            //         {
            //             float sum = 0;
            //             int count = 0;

            //             // Apply blur kernel
            //             for (int i = -blurSize; i <= blurSize; i++)
            //             {
            //                 for (int j = -blurSize; j <= blurSize; j++)
            //                 {
            //                     int newX = Mathf.Clamp(x + i, 0, width - 1);
            //                     int newY = Mathf.Clamp(y + j, 0, height - 1);

            //                     sum += inputArray[newX, newY];
            //                     count++;
            //                 }
            //             }

            //             resultArray[x, y] = sum / count;

            //         }
            //     }
            // }



            // this is slow, should be much faster probably and should be able to make lines per thread smaller
            // but this is not the case, likely due to overhead from threads being created here instead of using a pool

            // int lines = height;
            // int linesPerThread = 15;


            // List<Thread> workers = new();
            // int count = 0;

            // for (int i = 0; i < height; i += linesPerThread)
            // {

            //     int lineNum = linesPerThread;

            //     if (i + lineNum > (height - 1))
            //     {
            //         lineNum = height - i;
            //     }


            //     Thread t = new Thread(new ParameterizedThreadStart(blur));


            //     BlurParams p;
            //     p.lineCount = lineNum;
            //     p.lineIndex = i;
            //     t.Start(p);

            //     workers.Add(t);
            //     count++;
            // }

            // for (int i = 0; i < workers.Count; i++)
            // {
            //     workers[i].Join();
            // }



            // main thread

            float sum;
            int count;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    sum = 0;
                    count = 0;
                    // Apply blur kernel
                    for (int i = -blurSize; i <= blurSize; i++)
                    {
                        for (int j = -blurSize; j <= blurSize; j++)
                        {
                            int newX = Mathf.Clamp(x + i, 0, width - 1);
                            int newY = Mathf.Clamp(y + j, 0, height - 1);

                            sum += inputArray[newX, newY];
                            count++;
                        }
                    }

                    resultArray[x, y] = sum / count;
                }
            }



            // for (int x = 0; x < width; x++)
            // {
            //     for (int y = 0; y < height; y++)
            //     {
            //         float sum = 0;
            //         int count = 0;

            //         // Apply blur kernel
            //         for (int i = -blurSize; i <= blurSize; i++)
            //         {
            //             for (int j = -blurSize; j <= blurSize; j++)
            //             {
            //                 int newX = Mathf.Clamp(x + i, 0, width - 1);
            //                 int newY = Mathf.Clamp(y + j, 0, height - 1);

            //                 sum += inputArray[newX, newY];
            //                 count++;
            //             }
            //         }

            //         // Calculate the average
            //         resultArray[x, y] = sum / count;
            //     }
            // }

            return resultArray;
        }
        public static List<float[,]> Normalize(List<float[,]> _noiseMaps, int _height, int _width)
        {
            float maxNoiseHeight = float.MinValue;
            float minNoiseHeight = float.MaxValue;

            for (int i = 0; i < _noiseMaps.Count; i++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        float val = _noiseMaps[i][x, y];

                        if (val > maxNoiseHeight)
                        {
                            maxNoiseHeight = val;
                        }
                        else if (val < minNoiseHeight)
                        {
                            minNoiseHeight = val;
                        }
                    }
                }
            }


            for (int i = 0; i < _noiseMaps.Count; i++)
            {
                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        _noiseMaps[i][x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, _noiseMaps[i][x, y]);

                    }
                }
            }

            return _noiseMaps;
        }



    }

    [System.Serializable]
    public struct PerlinNoiseConfig
    {
        public int seed;
        public float scale;
        [Range(1, 5)]
        public int octaves;
        [Range(0.01f, 1)]
        public float persistance;
        [Range(1, 20)]
        public float lacunarity;
        public float standardMaxValue;
    }
    [System.Serializable]
    public struct RoadNoiseConfig
    {
        public float amplitude;
        public int brushRadius;
        public int brushSpacing;
        public int blurPadding;
        public int blurAmount;
        public int test;
        public AnimationCurve brush;

    }
    [System.Serializable]
    public struct CurveConfig
    {
        public float amplitude;
        public int padding;
        public int width;
        public AnimationCurve distanceModifier;
        public bool invert;
        public PerlinNoiseConfig perlinConfig;

    }
}