using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TerrainGenerator))]
public class WorldEditor : Editor
{

    Texture2D heightmap = null;
    Texture2D colormap = null;
    DebugTerrain debugTerrain;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (Application.isPlaying) return;

        TerrainGenerator terrainGenerator = (TerrainGenerator)target;


        GUILayout.Label("Maps Preview");
        GUILayout.Space(20);

        if (debugTerrain == null)
        {
            debugTerrain = FindObjectOfType<DebugTerrain>();
        }


        if (GUI.changed)
        {
            MapData mapdata = terrainGenerator.GenerateMapData(terrainGenerator._offsetX, terrainGenerator._offsetY);

            List<float[,]> heights = new();
            heights.Add(mapdata.GetHeightMap().Values);

            if (terrainGenerator.Normalize)
            {
                mapdata.OverrideHeightMap(HeightMap.FromNoise(NoiseGenerator.Normalize(heights, mapdata.height, mapdata.width)[0],0));
            }
            heightmap = TextureGenerator.TextureFromMap(mapdata.GetHeightMap().Values);

            colormap = TextureGenerator.TextureFromMap(mapdata.colormap, heightmap.width, heightmap.height);
            debugTerrain.GenerateMesh(terrainGenerator.GenerateTerrainMeshData().mesh, colormap);
        }
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(heightmap);
        GUILayout.Label(colormap);
        EditorGUILayout.EndHorizontal();
    }
}