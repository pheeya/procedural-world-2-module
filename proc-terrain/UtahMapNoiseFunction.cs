using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class UtahMapNoiseFunction : NoiseFunction

    {

        [Header("Terrain")]

        [SerializeField] TerrainGenerator terrainGen;
        [SerializeField] PerlinNoiseConfig m_perlin;

        [Header("Road")]
        [SerializeField] PerlinNoiseConfig m_roadPerlin;
        [SerializeField] RoadNoiseConfig m_road;
        [SerializeField] float RoadNoiseMaxHeight;
        [SerializeField] float RoadNoiseBlend;
        [Header("Valley")]
        [SerializeField] RoadNoiseConfig m_valley;
        [SerializeField] float ValleyNoiseExtrusion;
        [SerializeField] PerlinNoiseConfig m_valleyPerlin;
        [SerializeField] float ValleyNoiseBlend;


        void Awake()
        {
            terrainGen.EChunkGameObjectCreated += InitializeChunk;
        }
        void InitializeChunk(TerrainChunk c)
        {
            c.PreAllocatedNoise = new()
            {
                // road noise
                new float[terrainGen.VertsPerSide() + 2, terrainGen.VertsPerSide() + 2],


                // road noise blurred
                new float[terrainGen.VertsPerSide() + 2 + m_road.blurPadding, terrainGen.VertsPerSide() + 2 + m_road.blurPadding],

              // valley noise blurred
                new float[terrainGen.VertsPerSide() + 2 + m_valley.blurPadding, terrainGen.VertsPerSide() + 2 + m_valley.blurPadding]
            };

        }

        public override void GenerateNonAlloc(TerrainChunk _c)
        {

            Helpers.Reset2DArray(_c.m_noise);
            Helpers.Reset2DArray(_c.PreAllocatedNoise[0]);
            Helpers.Reset2DArray(_c.PreAllocatedNoise[1]);
            Helpers.Reset2DArray(_c.PreAllocatedNoise[2]);

            float ofstX = terrainGen._offsetX + _c.m_relativePosToParent.x;
            float ofstY = terrainGen._offsetY + _c.m_relativePosToParent.z;

            NoiseGenerator.GenerateNoiseMapNonAlloc(_c.m_noise, m_perlin, terrainGen.VertsPerSide() + 2, terrainGen.VertsPerSide() + 2, ofstX, ofstY);
            AddRoadNoiseNonAlloc(_c.m_noise, m_road, _c.PreAllocatedNoise[0], _c.PreAllocatedNoise[1], ofstX, ofstY);


            Helpers.Reset2DArray(_c.PreAllocatedNoise[0]);


            CreateValleyAroundRoadNonAlloc(_c.m_noise, m_valley, _c.PreAllocatedNoise[0], _c.PreAllocatedNoise[2], ofstX, ofstY);
            NoiseGenerator.NormalizeGloballyNonAlloc(_c.m_noise, terrainGen.VertsPerSide() + 2, terrainGen.VertsPerSide() + 2, m_perlin.standardMaxValue + ValleyNoiseExtrusion);



        }


        public void AddRoadNoiseNonAlloc(float[,] to, RoadNoiseConfig _config, float[,] roadNoise, float[,] blurredRoadNoise, float _ofstX, float _ofstY)
        {

            NoiseGenerator.GenerateLongitudinalSinNoiseNonAlloc(roadNoise, blurredRoadNoise, terrainGen.VertsPerSide() + 2, terrainGen.VertsPerSide() + 2, _config, _ofstX, _ofstY, m_roadPerlin, m_roadPerlin);

            for (int i = 0; i < roadNoise.GetLength(1); i++)
            {
                for (int j = 0; j < roadNoise.GetLength(0); j++)
                {
                    to[i, j] = Mathf.Lerp(to[i, j], RoadNoiseMaxHeight, roadNoise[i, j] * RoadNoiseBlend);
                }
            }
        }

        public void CreateValleyAroundRoadNonAlloc(float[,] _noise, RoadNoiseConfig _valleyConfig, float[,] generatedRoadNoise, float[,] generatedBlurredNoise, float _ofstX, float _ofstY)
        {
            NoiseGenerator.GenerateLongitudinalSinNoiseNonAlloc(generatedRoadNoise, generatedBlurredNoise, terrainGen.VertsPerSide() + 2, terrainGen.VertsPerSide() + 2, _valleyConfig, _ofstX, _ofstY, m_valleyPerlin, m_valleyPerlin);
            // NoiseGenerator.GenerateCurveNonAlloc(generatedRoadNoise, VertsPerSide() + 2, VertsPerSide() + 2, ValleyCurveConfig, _ofstX, _ofstY);
            for (int i = 0; i < generatedRoadNoise.GetLength(1); i++)
            {
                for (int j = 0; j < generatedRoadNoise.GetLength(0); j++)
                {
                    // float vert = verticality[0,i];
                    // vert += ValleyNoiseExtrusion;
                    // vert = Mathf.Clamp01(vert);



                    // for stamp noise
                    _noise[i, j] = Mathf.Lerp(_noise[i, j], m_perlin.standardMaxValue + ValleyNoiseExtrusion, Mathf.Clamp01((1 - generatedRoadNoise[i, j])) * ValleyNoiseBlend);


                    // for curve noise
                    // _noise[i, j] = Mathf.Lerp(_noise[i, j], PerlinConfig.standardMaxValue + ValleyNoiseExtrusion, Mathf.Clamp01((generatedRoadNoise[i, j])) * ValleyNoiseBlend);


                }
            }
        }

        public override bool ShouldGeneratePhysics(TerrainChunk _c)
        {
            return true;
        }

        public override List<NoiseMapPart> GetDebugNoiseMapParts()
        {
            throw new System.NotImplementedException();
        }

        public override void GenerateTestNoiseNonAlloc(float[,] n)
        {
            throw new System.NotImplementedException();
        }

        public override void OnNoiseModifierCreated(NoiseModifier mod)
        {
            throw new System.NotImplementedException();
        }
    }

}