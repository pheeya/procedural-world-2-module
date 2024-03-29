using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ProcWorld
{

    public class TextureGenerator
    {
        public static Texture2D TextureFromMap(Color[] map, int width, int height)
        {

            Texture2D texture = new Texture2D(width, height);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels(map);
            texture.Apply();

            return texture;
        }
        public static Texture2D TextureFromMap(float[,] map)
        {
            int mapWidth = map.GetLength(0);
            int mapHeight = map.GetLength(1);


            Color[] colormap = new Color[mapWidth * mapHeight];

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    int alreadyColored = y * mapWidth;
                    colormap[alreadyColored + x] = Color.Lerp(Color.black, Color.white, map[x, y]);
                }
            }
            return TextureFromMap(colormap, mapWidth, mapHeight);
        }


    }
}