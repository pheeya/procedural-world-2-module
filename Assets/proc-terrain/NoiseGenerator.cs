using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

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
            float samplex = (_x + _offsetX + _octaves[o].x) / _config.scale * frequency;
            float sampley = (_y + _offsetY + _octaves[o].y) / _config.scale * frequency;


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

    public static float[,] GenerateLongitudinalSinNoise(int _width, int _height, float _amplitude, float _frequency, bool _invert, float _offsetX, float _offsetY, PerlinNoiseConfig _horizontalNoise, PerlinNoiseConfig _verticalNoise, AnimationCurve _brush, int _brushRadius, int _brushSpacing)
    {
        if (_brushSpacing < 1)
        {
            _brushSpacing = 1;
            Debug.Log("Check brush spacing, it should be > 0");
        }

        Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_horizontalNoise, 0, _offsetY);
        Vector2[] verticalOctaveOffsets = GetOctaveOffsets(_verticalNoise, 0, _offsetY);
        float halfWidth = _width / 2f;
        float halfHeight = _height / 2f;

        float[,] map = new float[_width, _height];


        // -radius to radius offset in y axis to make brush stamping go beyond the bounds, to make the edges somewhat seamless



        // Circle stamp based noise generation
        // NOTE: brush spacing more than 1 causes small seams between chunks
        // should be okay to use 1 brush spacing since we save computation but only going through the y axis and x axis pixels around the noise

        Vector2 previousPos = Vector2.zero;
        float interpolatedStampStepSize = 0.1f;
        for (int y = -_brushRadius * 2; y < _height + _brushRadius * 2; y += _brushSpacing)
        {
            int yPos = y;
            int xPos = _width / 2;
            xPos += Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, xPos, yPos, horizontalOctaveOffsets, -halfWidth, -halfHeight) * _amplitude * _width - _offsetX);
            Vector2 currentPos = new(xPos, y);
            if (y > -_brushRadius * 2)
            {
                if ((currentPos - previousPos).magnitude > .5f)
                {



                    for (float t = 0; t < 1; t += interpolatedStampStepSize)
                    {
                        Vector2 pos = Vector2.Lerp(previousPos, currentPos, t);
                        map = StampCircle(map, Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), _brush, _brushRadius);

                    }

                }
            }

            map = StampCircle(map, xPos, yPos, _brush, _brushRadius);
            previousPos = currentPos;
        }
        // END


        // float interpolatedStampStepSize = 0.05f;
        // Vector2 previousPos = Vector2.zero;
        // for (int starting = (_width / 2) - 5; starting < (_width / 2) + 5; starting++)
        // {
        //     for (int y = 0; y < _height; y++)
        //     {
        //         int yPos = y;
        //         int xPos = starting + Mathf.RoundToInt(GetPerlinValue(_horizontalNoise, starting, yPos, horizontalOctaveOffsets, -halfWidth, -halfHeight) * _amplitude * _width - _offsetX);
        //         Vector2 currentPos = new(xPos, yPos);
        //         if (y > 0)
        //         {
        //             if ((currentPos - previousPos).magnitude > 1.0f)
        //             {
        //                 for (float t = 0; t < 1; t += interpolatedStampStepSize)
        //                 {
        //                     Vector2 pos = Vector2.Lerp(previousPos, currentPos, t);

        //                     if (pos.x > _width - 1 || pos.x < 0)
        //                     {

        //                     }
        //                     else
        //                     {
        //                         map[Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y)] = 1;
        //                     }

        //                 }

        //             }
        //         }

        //         if (xPos > _width - 1 || xPos < 0)
        //         {

        //         }
        //         else
        //         {
        //             map[xPos, yPos] = 1;
        //         }
        //         previousPos = currentPos;
        //     }


        // }

        return map;
    }


    public static float[,] StampCircle(float[,] _noise, int _centerX, int _centerY, AnimationCurve _brush, int _radius)
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

                // dist = dist * Mathf.Pow(2, _brushStrength);
                // box shape 
                // dist = (pos - new Vector2(_centerX, pos.y)).magnitude / _radius;
                // dist = Mathf.Clamp01(dist);




                _noise[x, y] += _brush.Evaluate((1 - dist));


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