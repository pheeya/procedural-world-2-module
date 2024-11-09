using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class FeatureGenerator : MonoBehaviour
    {
        public Feature[] features;

        public void GenerateFeatures(float[,] map, float _heightScale, ThreadSafeAnimationCurve _heightCurve, float[,] _terrainMap)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            float topLeftX = (width - 1) / -2f;
            float topLeftZ = (height - 1) / 2f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int i = 0; i < features.Length; i++)
                    {
                        if (_terrainMap[x, y] <= features[i].maxHeight && _terrainMap[x, y] >= features[i].minHeight && map[x, y] > features[i].minNoise)
                        {
                            features[i].CreateFeature(topLeftX + x, _heightCurve.Evaluate(_terrainMap[x, y]) * _heightScale, topLeftZ - y);
                        }
                    }
                }
            }
        }
    }



    [System.Serializable]
    public struct Feature
    {
        [Space(5)]
        public string name;
        public int priority;
        public FeatureType type;
        public float[,] map;
        public GameObject prefab;
        private GameObject obj;
        public int population;
        [Space(5)]
        [Header("Occurance Requirements")]
        public float minHeight;
        public float maxHeight;
        public int maxPopulation;
        [Range(0, 1)]
        public float minNoise;

        public void CreateFeature(float x, float y, float z)
        {
            if (population < maxPopulation)
            {
                obj = GameObject.Instantiate(prefab);
                obj.transform.position = new Vector3(x, y, z);
                population++;
            }
        }
    }

    public enum FeatureType
    {
        Vegetation,
        Prop
    }
}