using System.Collections;
using System.Collections.Generic;
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

    public static float[,] GenerateLongitudinalSinNoise(int _width, int _height, float _softness, float _waveThickness, float _sharpness, float _amplitude, float _frequency, bool _invert, float _offsetX, float _offsetY, PerlinNoiseConfig _horizontalNoise, PerlinNoiseConfig _verticalNoise)
    {


        Vector2[] horizontalOctaveOffsets = GetOctaveOffsets(_horizontalNoise, 0, _offsetY);
        Vector2[] verticalOctaveOffsets = GetOctaveOffsets(_verticalNoise, 0, _offsetY);
        float halfWidth = _width / 2f;
        float halfHeight = _height / 2f;

        float[,] map = new float[_width, _height];

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {

                // float physicalWidth = _width - 3;
                // float physicalHeight = _height - 3;
                // float x01 = (x + _offsetX) / (float)(physicalWidth);
                // float y01 = (y + _offsetY) / (float)(physicalHeight);

                // x01 = x - 2+ _offsetX;
                // x01 /= _width - 3;

                // y01 = y - 2+ _offsetY;
                // y01 /= _height - 3;


                // float xMinusOneToOne = x01 * 2 - 1.0f;

                // float centerMinusOneToOne = 0.0f;
                // float offset = (Mathf.Sin(y01 * Mathf.PI * 2 * _frequency)) * _amplitude;

                // float distanceFromCenter = Mathf.Abs(xMinusOneToOne + offset - centerMinusOneToOne) * Mathf.Pow(2, _sharpness) - _waveThickness;


                // float debugDist = distanceFromCenter;
                // distanceFromCenter = Mathf.Max(0, distanceFromCenter);
                // distanceFromCenter = Mathf.Pow(distanceFromCenter, _softness);

                // if (float.IsNaN(distanceFromCenter))
                // {
                //     Debug.Log(debugDist);
                // }
                // if (_invert)
                // {
                //     distanceFromCenter = 1 - distanceFromCenter;
                // }

                // map[x, y] = distanceFromCenter;


                float distanceFromCenter = x - GetPerlinValue(_horizontalNoise, 0, y, horizontalOctaveOffsets, -halfWidth, -halfHeight) * _amplitude * _width - halfWidth + _offsetX;
                distanceFromCenter /= _width;
                distanceFromCenter *= Mathf.Pow(2, _sharpness);
                distanceFromCenter = Mathf.Abs(distanceFromCenter);
                if (_invert)
                {
                    distanceFromCenter = 1 - distanceFromCenter;
                }
                map[x, y] = distanceFromCenter;

            }
        }

        return map;
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