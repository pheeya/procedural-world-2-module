using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMap
{
    public float[,] Values { get; private set; }
    public int Height { get; private set; }

    public int Width { get; private set; }
    public HeightMap(int _h, int _w, float[,] _values)
    {
        Height = _h;
        Width = _w;
        Values = _values;
    }

}
