using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator
{
    public static float[,] GenerateNoiseMap(PerlinNoiseConfig _config, int _width, int _height, float _offsetX, float _offsetY)
    {
        float[,] map = new float[_width, _height];
        System.Random prng = new System.Random(_config.seed);
        Vector2[] octaveOffsets = new Vector2[_config.octaves];

        for (int i = 0; i < _config.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + _offsetX;
            float offsetY = prng.Next(-100000, 100000) - _offsetY;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }


        float halfWidth = _width / 2f;
        float halfHeight = _height / 2f;

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float value = 0;
                for (int o = 0; o < _config.octaves; o++)
                {
                    float samplex = (x - halfWidth + octaveOffsets[o].x) / _config.scale * frequency;
                    float sampley = (y - halfHeight + octaveOffsets[o].y) / _config.scale * frequency;
                    float perlin = Mathf.PerlinNoise(samplex, sampley) * 2 - 1; // change perlinnoise range to -1 1

                    value += perlin * amplitude;

                    amplitude *= _config.persistance;
                    frequency *= _config.lacunarity;
                }

                if (value >= 1)
                {
                }

                if (value > maxNoiseHeight)
                {
                    maxNoiseHeight = value;
                }
                else if (value < minNoiseHeight)
                {
                    minNoiseHeight = value;
                }

                map[x, y] = value;

            }
        }

        // for (int y = 0; y < _mapheight; y++)
        // {
        //     for (int x = 0; x < _config.width; x++)
        //     {
        //         map[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, map[x, y]);
        //     }
        // }
        return map;
    }


    public static float[,] GenerateLongitudinalSinNoise(int _width, int _height,float _softness, float _waveThickness, float _sharpness, float _amplitude, float _frequency, bool _invert)
    {
        float[,] map = new float[_width, _height];
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                // float offset = Mathf.Sin(y / (float)(_height - 1) * 2 * Mathf.PI * _frequency) * _amplitude * _width;
                // float distanceFromCenter = 1 - Mathf.Abs(x + 1 + offset - _width / 2.0f) / (float)_width;

                float x01 = (x) / (float)(_width - 1);
                float y01 = (y) / (float)(_height - 1);

                float xMinusOneToOne = x01 * 2 - 1.0f;

                float centerMinusOneToOne = 0.0f;
                float offset = Mathf.Sin(y01 * Mathf.PI * 2 * _frequency) * _amplitude;

                float distanceFromCenter = Mathf.Abs(xMinusOneToOne + offset - centerMinusOneToOne) * Mathf.Pow(2, _sharpness) - _waveThickness;
                float debugDist = distanceFromCenter;
                distanceFromCenter = Mathf.Max(0, distanceFromCenter);
                distanceFromCenter = Mathf.Pow(distanceFromCenter, _softness);

                if(float.IsNaN(distanceFromCenter)){
                    Debug.Log(debugDist);
                }
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