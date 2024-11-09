using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class DefaultNoiseFunction : NoiseFunction
    {
        [SerializeField] TerrainGenerator m_terrainGenerator;
        [SerializeField] float m_heightScale;
        [SerializeField] AnimationCurve m_terrainHeightCurve;
        [SerializeField] PerlinNoiseConfig m_noiseConfig;
        [SerializeField] float m_maxApproximateNoiseValue;
        List<NoiseModifierSource> m_modifiers = new();
        public override void GenerateNonAlloc(TerrainChunk _c)
        {
            Helpers.Reset2DArray(_c.m_noise);

            float ofstX = m_terrainGenerator._offsetX + _c.m_relativePosToParent.x;
            float ofstY = m_terrainGenerator._offsetY + _c.m_relativePosToParent.z;

            NoiseGenerator.GenerateNoiseMapNonAlloc(_c.m_noise, m_noiseConfig, _c.m_noise.GetLength(0), _c.m_noise.GetLength(1), ofstX, ofstY);

            for (int i = 0; i < m_modifiers.Count; i++)
            {

                if (m_modifiers[i].Disabled) continue;
                NoiseModifierFunction func = m_modifiers[i].Function;
                func(_c.m_noise, ofstX, ofstY);
            }


            NoiseGenerator.NormalizeGloballyNonAlloc(m_noiseConfig, _c.m_noise, m_terrainGenerator.VertsPerSide() + 2, m_terrainGenerator.VertsPerSide() + 2, m_maxApproximateNoiseValue);


        }

        public override void GenerateTestNoiseNonAlloc(float[,] n)
        {

            NoiseGenerator.GenerateNoiseMapNonAlloc(n, m_noiseConfig, n.GetLength(0), n.GetLength(1), m_terrainGenerator._offsetX, m_terrainGenerator._offsetY);

            NoiseGenerator.NormalizeGloballyNonAlloc(m_noiseConfig, n, m_terrainGenerator.VertsPerSide() + 2, m_terrainGenerator.VertsPerSide() + 2, m_maxApproximateNoiseValue);
        }

        public override float GetHeightScale()
        {
            return m_heightScale;
        }

        public override List<NoiseMapPart> GetDebugNoiseMapParts()
        {
            float[,] noise = new float[m_terrainGenerator.VertsPerSide() + 2, m_terrainGenerator.VertsPerSide() + 2];
            NoiseGenerator.GenerateNoiseMapNonAlloc(noise, m_noiseConfig, noise.GetLength(0), noise.GetLength(1), m_terrainGenerator._offsetX, m_terrainGenerator._offsetY);
            NoiseMapPart part;
            part.title = "Full map";
            part.map = noise;
            List<NoiseMapPart> list = new();
            list.Add(part);



            return list;
        }

        public override AnimationCurve GetTerrainHeightCurve()
        {
            return m_terrainHeightCurve;
        }

        public override void OnNoiseModifierCreated(NoiseModifierSource mod)
        {
            if (m_modifiers.Contains(mod))
            {

                Debug.Log("Already have this noise mod");
                Debug.Log(mod);
                return;
            }
            m_modifiers.Add(mod);
        }
        public override void OnNoiseModifierRemoved(NoiseModifierSource mod)
        {
            if (!m_modifiers.Contains(mod))
            {
                Debug.Log("doesn't contain this noise mod");
                Debug.Log(mod);
                return;
            }
            m_modifiers.Remove(mod);

        }

        public override bool ShouldGeneratePhysics(TerrainChunk _c)
        {
            return true;
        }


    }

}