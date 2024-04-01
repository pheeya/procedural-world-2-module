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
        Texture2D valleyMap = null;


        Texture2D roadCurveTexture = null;
        Texture2D valleyCurveTexture = null;

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

                HeightMap roadNoise = HeightMap.FromNoise(NoiseGenerator.GenerateLongitudinalSinNoise(hm.Width, hm.Height, terrainGenerator.RoadConfig, 0, 0, terrainGenerator.RoadHorizontalPerlinConfig, terrainGenerator.RoadVerticalPerlinConfig), 0);
                HeightMap valleyNoise = HeightMap.FromNoise(NoiseGenerator.GenerateLongitudinalSinNoise(hm.Width, hm.Height, terrainGenerator.ValleyConfig, 0, 0, terrainGenerator.ValleyPerlinConfig, terrainGenerator.RoadVerticalPerlinConfig), 0);

                HeightMap roadVerticalityNoise = HeightMap.FromNoise(NoiseGenerator.GenerateSingleAxisNoiseMap(terrainGenerator.RoadVerticalPerlinConfig, hm.Width, hm.Height, terrainGenerator._offsetX, terrainGenerator._offsetY), 0);

                roadMap = TextureGenerator.TextureFromMap(roadNoise.Values);

                colormap = TextureGenerator.TextureFromMap(cm, heightmap.width, heightmap.height);

                roadVerticallityMap = TextureGenerator.TextureFromMap(roadVerticalityNoise.Values);

                valleyMap = TextureGenerator.TextureFromMap(valleyNoise.Values);


                float[,] generatedRoadCurve= new float[hm.Width, hm.Height];
                NoiseGenerator.GenerateCurveNonAlloc(generatedRoadCurve, hm.Width, hm.Height, terrainGenerator.RoadCurveConfig, terrainGenerator.testX, terrainGenerator.testY);
                HeightMap RoadCurve = HeightMap.FromNoise(generatedRoadCurve, 0);
                roadCurveTexture = TextureGenerator.TextureFromMap(RoadCurve.Values);


                float[,] generatedValleyNoise= new float[hm.Width, hm.Height];
                NoiseGenerator.GenerateCurveNonAlloc(generatedValleyNoise, hm.Width, hm.Height, terrainGenerator.ValleyCurveConfig, terrainGenerator.testX, terrainGenerator.testY);
                HeightMap VallleyCurve = HeightMap.FromNoise(generatedValleyNoise, 0);
                valleyCurveTexture = TextureGenerator.TextureFromMap(VallleyCurve.Values);




                debugTerrain.GenerateMesh(terrainGenerator.GenerateTestMeshData().CreateMesh(), colormap);


            }

            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(valleyMap);
            GUILayout.Label(heightmap);
            GUILayout.Label(colormap);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(roadMap);
            GUILayout.Label(roadVerticallityMap);
            GUILayout.Label(roadCurveTexture);
            GUILayout.Label(valleyCurveTexture);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

    }
}