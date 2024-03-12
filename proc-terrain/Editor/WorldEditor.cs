using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace ProcWorld
{

    [CustomEditor(typeof(TerrainGenerator))]
    public class WorldEditor : Editor
    {

        Texture2D heightmap = null;
        Texture2D colormap = null;
        DebugTerrain debugTerrain;
        Texture2D roadMap = null;
        Texture2D roadVerticallityMap = null;

        public override void OnInspectorGUI()
        {

            base.OnInspectorGUI();
            System.Diagnostics.Stopwatch sw = new();


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
                sw.Start();
                HeightMap hm = terrainGenerator.GenerateTestHeightMap();

                Color[] cm = terrainGenerator.ColorMapFromHeight(hm);

                heightmap = TextureGenerator.TextureFromMap(hm.Values);

                HeightMap roadNoise = HeightMap.FromNoise(NoiseGenerator.GenerateLongitudinalSinNoise(hm.Width, hm.Height, terrainGenerator.RoadConfig, terrainGenerator.testX, terrainGenerator.testY, terrainGenerator.RoadHorizontalPerlinConfig, terrainGenerator.RoadVerticalPerlinConfig), 0);

                HeightMap roadVerticalityNoise = HeightMap.FromNoise(NoiseGenerator.GenerateSingleAxisNoiseMap(terrainGenerator.RoadVerticalPerlinConfig, hm.Width, hm.Height, terrainGenerator._offsetX, terrainGenerator._offsetY), 0);

                roadMap = TextureGenerator.TextureFromMap(roadNoise.Values);

                colormap = TextureGenerator.TextureFromMap(cm, heightmap.width, heightmap.height);

                roadVerticallityMap = TextureGenerator.TextureFromMap(roadVerticalityNoise.Values);

                debugTerrain.GenerateMesh(terrainGenerator.GenerateTestMeshData().mesh, colormap);
                sw.Stop();
                Debug.Log("Generated debug terrain, took: " + sw.Elapsed.TotalMilliseconds + " ms");
            }

            EditorGUILayout.BeginVertical();
            GUILayout.Label(heightmap);
            GUILayout.Label(colormap);
            GUILayout.Label(roadMap);
            GUILayout.Label(roadVerticallityMap);
            EditorGUILayout.EndVertical();
        }

    }
}