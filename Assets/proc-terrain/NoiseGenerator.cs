using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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
        public static float GetPerlinValue(PerlinNoiseConfig _config, float _x, float _y, Vector2[] _octaves, float _offsetX, float _offsetY)
        {

            float amplitude = 1;
            float frequency = 1;
            float value = 0;
            for (int o = 0; o < _config.octaves; o++)
            {
                float samplex = (_x + _octaves[o].x) / _config.scale * frequency;
                float sampley = (_y + _octaves[o].y) / _config.scale * frequency;


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



        static int sinVal = 1;
        public static float[,] GenerateLongitudinalSinNoise(int _width, int _height, RoadNoiseConfig _roadConfig, float _offsetX, float _offsetY, PerlinNoiseConfig _horizontalNoise, PerlinNoiseConfig _verticalNoise)
        {
            System.Diagnostics.Stopwatch sw = new();

            sw.Start();
            if (_roadConfig.brushSpacing < 1)
            {
                _roadConfig.brushSpacing = 1;
                Debug.Log("Check brush spacing, it should be > 0");
            }

            Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_horizontalNoise, 0, _offsetY);
            Vector2[] verticalOctaveOffsets = GetOctaveOffsets(_verticalNoise, 0, _offsetY);
            float halfWidth = _width / 2f;
            float halfHeight = _height / 2f;

            int blurredMapWidth = _width + _roadConfig.blurPadding;

            float[,] map = new float[_width, _height];


            float[,] blurredMap = new float[blurredMapWidth, blurredMapWidth];


            int physicalWidth = blurredMapWidth - 2;


            int CenterX = (blurredMapWidth / 2);
            int CenterY = (blurredMapWidth / 2);

            int BottomY = CenterY + physicalWidth / 2;
            int TopY = CenterY - physicalWidth / 2;

            int LeftX = CenterX - physicalWidth / 2;
            int RightX = CenterX + physicalWidth / 2;


            // // stamp center
            // blurredMap = StampCircle(blurredMap, CenterX + Mathf.RoundToInt(_offsetX), CenterY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);


            // //stamp bottom center
            // blurredMap = StampCircle(blurredMap, CenterX + Mathf.RoundToInt(_offsetX), BottomY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);

            // //stamp top center
            // blurredMap = StampCircle(blurredMap, CenterX + Mathf.RoundToInt(_offsetX), TopY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);


            // //stamp Left center
            // blurredMap = StampCircle(blurredMap, LeftX + Mathf.RoundToInt(_offsetX), CenterY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);

            // //stamp Right center
            // blurredMap = StampCircle(blurredMap, RightX + Mathf.RoundToInt(_offsetX), CenterY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);


            // //stamp bottom left
            // blurredMap = StampCircle(blurredMap, LeftX + Mathf.RoundToInt(_offsetX), BottomY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);

            // //stamp bottom right
            // blurredMap = StampCircle(blurredMap, RightX + Mathf.RoundToInt(_offsetX), BottomY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);

            // //stamp Top left
            // blurredMap = StampCircle(blurredMap, LeftX + Mathf.RoundToInt(_offsetX), TopY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);

            // //stamp Top right
            // blurredMap = StampCircle(blurredMap, RightX + Mathf.RoundToInt(_offsetX), TopY + Mathf.RoundToInt(_offsetY), _roadConfig.brushRadius);



            // return blurredMap;

            // -radius to radius offset in y axis to make brush stamping go beyond the bounds, to make the edges somewhat seamless



            // Circle stamp based noise generation
            // NOTE: brush spacing more than 1 causes small seams between chunks
            // should be okay to use 1 brush spacing since we save computation but only going through the y axis and x axis pixels around the noise

            // Vector2 previousPos = Vector2.zero;
            // float interpolatedStampStepSize = 0.1f;
            // for (int y = -_roadConfig.brushRadius * 2; y < _height + _roadConfig.brushRadius * 2; y += _roadConfig.brushSpacing)
            // {
            //     int yPos = y;
            //     int xPos = _width / 2;
            //     xPos += Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPos, yPos, horizontalOctaveOffsets, -halfWidth, -halfHeight) * _roadConfig.amplitude * _width - _offsetX);
            //     Vector2 currentPos = new(xPos, y);
            //     if (y > -_roadConfig.brushRadius * 2)
            //     {
            //         if ((currentPos - previousPos).magnitude > .5f)
            //         {



            //             for (float t = 0; t < 1; t += interpolatedStampStepSize)
            //             {
            //                 Vector2 pos = Vector2.Lerp(previousPos, currentPos, t);
            //                 map = StampCircle(map, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), _roadConfig.brush, _roadConfig.brushRadius);

            //             }

            //         }
            //     }

            //     map = StampCircle(map, xPos, yPos, _roadConfig.brush, _roadConfig.brushRadius);
            //     previousPos = currentPos;
            // }
            // END

            Vector2 previousPos = Vector2.zero;

            float interpolatedStampStepSize = 0.1f;

            int startingYPos = 0;

            Debug.Log("Chunk: \n\n");
            int dif = blurredMapWidth - _width;
            dif /= 2;
            Debug.Log(_offsetX);
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





                float perlin = Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPosLocal, y - startingYPos, horizontalOctaveOffsets, -hw, -hh) * _roadConfig.amplitude * ((_width - 2) / 2.0f)); ;

                // float perlin = (Mathf.PerlinNoise(0f, worldYPos/500f) * 2 - 1) * _roadConfig.amplitude * 3;
                xPosLocal += Mathf.RoundToInt(perlin);
                // xPosLocal += Mathf.RoundToInt(Mathf.Sin(worldYPos) * _roadConfig.amplitude * 3);

                xPosLocal -= (int)_offsetX;

                Vector2 currentPos = new(xPosLocal, y - startingYPos);


                if (y > 0)
                {
                    Debug.Log((currentPos - previousPos).magnitude);
                    float dist = (currentPos - previousPos).magnitude;
                    if (dist > _roadConfig.brushSpacing)
                    {
                        int strokes = Mathf.FloorToInt(dist/_roadConfig.brushSpacing);
                        for (float t = 0; t < 1; t += (1f/strokes))
                        {
                            Vector2 pos = Vector2.Lerp(previousPos, currentPos, t);
                            blurredMap = StampCircle(blurredMap, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), _roadConfig.brushRadius);

                        }

                    }
                }

                // xPosLocal+=(y - Mathf.RoundToInt(_offsetY));
                // xPosLocal -= Mathf.RoundToInt(_offsetX);


                blurredMap = StampCircle(blurredMap, xPosLocal, y - startingYPos, _roadConfig.brushRadius);
                // blurredMap = StampCircle(blurredMap, xPosLocal, yPos, _roadConfig.brushRadius);
                previousPos = currentPos;


            }



            // return blurredMap;

            blurredMap = ApplyBlur(blurredMap, _roadConfig.blurAmount);

            for (int y = dif; y < blurredMapWidth - dif; y++)
            {
                for (int x = dif; x < blurredMapWidth - dif; x++)
                {
                    map[x - dif, y - dif] = blurredMap[x, y];

                }
            }


            sw.Stop();


            Debug.Log("Road noise generation completed, took: " + sw.Elapsed.TotalSeconds);
            return map;
        }



        static int maxLogs = 100;
        static int logs = 0;
        public static float[,] StampCircle(float[,] _noise, int _centerX, int _centerY, int _radius)
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
                    _noise[x, y] += dist > 0 ? 1 : 0;


                    _noise[x, y] = Mathf.Clamp01(_noise[x, y]);
                }
            }

            return _noise;
        }
        public static float[,] ApplyBlur(float[,] inputArray, int blurSize)
        {
            int width = inputArray.GetLength(0);
            int height = inputArray.GetLength(1);

            float[,] resultArray = new float[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
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
                    resultArray[x, y] = sum / count;
                }
            }

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


    }

}