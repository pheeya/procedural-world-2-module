using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public delegate void NoiseModifierFunction(float[,] original, float _offsetX, float _offsetY);
    public class NoiseModifier : MonoBehaviour
    {

        [field: SerializeField] public int Order { get; private set; }
        [field: SerializeField] public bool Disabled { get; private set; }
        public BoundsVec2 Bounds { get; private set; }
        NoiseModifierFunction m_func;

        public NoiseModifierFunction GetNoiseModifierFunction() { return m_func; }
        public void SetFunction(NoiseModifierFunction func, BoundsVec2 bounds)
        {
            m_func = func;
            Bounds = bounds;
            TerrainGenerator.Instance.noiseFunction.OnNoiseModifierCreated(this);


            Debug.Log(bounds.center);
            if (TerrainGenerator.Instance.terrainChunks != null)
            {
                foreach (TerrainChunk chunk in TerrainGenerator.Instance.terrainChunks.Values)
                {
                    if (chunk.IsInBounds(bounds))
                    {
                        Debug.Log("Marking dirty", chunk.gameObject);
                        Debug.Log(chunk.ChunkCoordinate);
                        chunk.MarkDirty();


                    }
                }
            }
        }
    }
}