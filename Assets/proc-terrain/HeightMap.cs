using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMap
{
    public float[,] Values { get; private set; }
    public int Height { get; private set; }

    public int Width { get; private set; }
    public int BorderSize {get; private set;}
    public HeightMap(int _w, int _h,int _borderSize ,float[,] _values)
    {
        Height = _h;
        Width = _w;
        BorderSize = _borderSize;
        Values = _values;
    }

    public static HeightMap FromNoise(float[,] _noise, int _borderSize)
    {
        return new(_noise.GetLength(0), _noise.GetLength(1),_borderSize, _noise);
    }

}
