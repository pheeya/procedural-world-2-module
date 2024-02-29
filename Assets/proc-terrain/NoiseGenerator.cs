using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoiseGenerator
{
    public static float[,] GenerateNoiseMap(int _seed, int _mapwidth, int _mapheight, float _scale, int _octaves, float _persistance, float _lacunarity, float _offsetX, float _offsetY)
    {
        float[,] map = new float[_mapwidth, _mapheight];
        System.Random prng = new System.Random(_seed);
        Vector2[] octaveOffsets = new Vector2[_octaves];

        for (int i = 0; i < _octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + _offsetX;
            float offsetY = prng.Next(-100000, 100000) - _offsetY;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }


        float halfWidth = _mapwidth / 2f;
        float halfHeight = _mapheight / 2f;

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        for (int y = 0; y < _mapheight; y++)
        {
            for (int x = 0; x < _mapwidth; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float value = 0;
                for (int o = 0; o < _octaves; o++)
                {
                    float samplex = (x - halfWidth + octaveOffsets[o].x) / _scale * frequency;
                    float sampley = (y - halfHeight + octaveOffsets[o].y) / _scale * frequency;
                    float perlin = Mathf.PerlinNoise(samplex, sampley) * 2 - 1; // change perlinnoise range to -1 1

                    value += perlin * amplitude;

                    amplitude *= _persistance;
                    frequency *= _lacunarity;
                }

                if(value>=1){
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
        //     for (int x = 0; x < _mapwidth; x++)
        //     {
        //         map[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, map[x, y]);
        //     }
        // }
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