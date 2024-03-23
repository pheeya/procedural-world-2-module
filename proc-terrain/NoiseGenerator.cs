using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using System.Threading;
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
                Debug.Log("Check brush spacing, it should be > 0");
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
        public static int GetPointOnLongNoise(int _y, int _width, PerlinNoiseConfig _horizontalNoise, RoadNoiseConfig _roadConfig, float _offsetY)
        {

            Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_horizontalNoise, 0, _offsetY);
            return GetPointOnLongNoise(horizontalOctaveOffsets, _y, _width, _horizontalNoise, _roadConfig, _offsetY);
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
            for (int y = _centerY - _radius; y < _centerY + _radius; y++)
            {
                if (y < 0 || y > _noise.GetLength(0) - 1) continue;

                for (int x = _centerX - _radius; x < _centerX + _radius; x++)
                {
                    if (x < 0 || x > _noise.GetLength(0) - 1) continue;



                    Vector2 pos = new Vector2(x, y);

                    // circle shape
                    float dist = (pos - new Vector2(_centerX, _centerY)).magnitude / _radius;
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

            return _noise;
        }
        delegate void BlurParamThreadStart(int _lineIndex, int _lineNum);
        struct BlurParams
        {
            public int lineIndex;
            public int lineCount;
        }
        public static float[,] ApplyBlur(float[,] inputArray, int blurSize)
        {
            int width = inputArray.GetLength(0);
            int height = inputArray.GetLength(1);
            float[,] resultArray = new float[width, height];
            void blur(object _params)
            {
                int _lineIndex = ((BlurParams)_params).lineIndex;
                int _lineNum = ((BlurParams)_params).lineCount;
                for (int x = 0; x < width; x++)
                {
                    for (int y = _lineIndex; y < _lineIndex + _lineNum; y++)
                    {
                        float sum = 0;
                        int count = 0;

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
                        // Calculate the average

                        if (y > height - 1 || y < 0)
                        {

                            Debug.Log(_lineNum);
                            Debug.Log(y);
                            Debug.Log(y < y + _lineNum);
                        }

                        if (x > width - 1 || x < 0)
                        {
                            Debug.Log(_lineIndex);
                            Debug.Log(_lineNum);
                            Debug.Log(y);
                            Debug.Log(x);
                        }
                        resultArray[x, y] = sum / count;

                    }
                }
            }



            // this is slow, should be much faster probably and should be able to make lines per thread smaller
            // but this is not the case, likely due to overhead from threads being created here instead of using a pool

            int lines = height;
            int linesPerThread = 15;

            List<Thread> workers = new();
            int count = 0;

            for (int i = 0; i < height; i += linesPerThread)
            {

                int lineNum = linesPerThread;

                if (i + lineNum > (height - 1))
                {
                    lineNum = height - i;
                }


                Thread t = new Thread(new ParameterizedThreadStart(blur));


                BlurParams p;
                p.lineCount = lineNum;
                p.lineIndex = i;
                t.Start(p);

                workers.Add(t);
                count++;


            }

            for (int i = 0; i < workers.Count; i++)
            {
                workers[i].Join();
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

}