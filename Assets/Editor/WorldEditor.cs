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
    Texture2D roadMap = null;

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



        GUILayout.Button("Refresh");
        if (GUI.changed || heightmap == null)
        {
            HeightMap hm = terrainGenerator.GenerateTestHeightMap();

            Color[] cm = terrainGenerator.ColorMapFromHeight(hm);

            heightmap = TextureGenerator.TextureFromMap(hm.Values);

            HeightMap roadNoise = HeightMap.FromNoise(NoiseGenerator.GenerateLongitudinalSinNoise(hm.Width, hm.Height,terrainGenerator.RoadNoiseSoftness,terrainGenerator.RoadNoiseThickness, terrainGenerator.RoadNoiseSharpness, terrainGenerator.RoadNoiseAmp, terrainGenerator.RoadNoiseFreq, terrainGenerator.RoadNoiseInvert,terrainGenerator.testX, terrainGenerator.testY), 0);

            roadMap = TextureGenerator.TextureFromMap(roadNoise.Values);

            colormap = TextureGenerator.TextureFromMap(cm, heightmap.width, heightmap.height);
            debugTerrain.GenerateMesh(terrainGenerator.GenerateTestMeshData().mesh, colormap);
        }
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(heightmap);
        GUILayout.Label(colormap);
        GUILayout.Label(roadMap);
        EditorGUILayout.EndHorizontal();
    }

}