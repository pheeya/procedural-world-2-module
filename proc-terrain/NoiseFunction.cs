using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public struct NoiseMapPart
    {
        public string title;
        public float[,] map;
    }
    public abstract class NoiseFunction : MonoBehaviour
    {

        public abstract void GenerateNonAlloc(TerrainChunk _c);
        public abstract bool ShouldGeneratePhysics(TerrainChunk _c);
        public abstract List<NoiseMapPart> GetDebugNoiseMapParts();
        public abstract void GenerateTestNoiseNonAlloc(float[,] n);
        public abstract void OnNoiseModifierCreated(NoiseModifierSource mod);
        public abstract void OnNoiseModifierRemoved(NoiseModifierSource mod);

        public abstract ThreadSafeAnimationCurve GetTerrainHeightCurve();

        public abstract float GetHeightScale();


        void OnValidate()
        {
            DebugTerrain debugTerrain;
            debugTerrain = FindObjectOfType<DebugTerrain>();
            debugTerrain.GenerateMesh(TerrainGenerator.Instance.GenerateTestMeshData().CreateMesh(), null);
        }


    }


}