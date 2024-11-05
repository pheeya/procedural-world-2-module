using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public delegate void NoiseModifierFunction(float[,] original, float _offsetX, float _offsetY);




    public class NoiseModifierSource
    {
        public int Order { get; private set; }
        public NoiseModifierFunction Function { get; private set; }
        public BoundsVec2 Bounds { get; private set; }
        public bool Disabled { get; private set; }
        public NoiseModifierSource(NoiseModifierFunction func, int order, BoundsVec2 bounds, bool disabled)
        {
            Order = order;
            Function = func;
            Bounds = bounds;
            Disabled = disabled;
        }
    }
    public class NoiseModifier : MonoBehaviour
    {

        [field: SerializeField] public int Order { get; private set; }
        [field: SerializeField] public bool Disabled { get; private set; }
        public BoundsVec2 Bounds { get; private set; }
        public NoiseModifierSource Source { get; private set; }
        public void SetFunction(NoiseModifierFunction func, BoundsVec2 bounds)
        {
            if (Source != null)
            {
                TerrainGenerator.Instance.noiseFunction.OnNoiseModifierRemoved(Source);
            }
            Source = new(func, Order, bounds, Disabled);
            TerrainGenerator.Instance.noiseFunction.OnNoiseModifierCreated(Source);


            if (TerrainGenerator.Instance.terrainChunks != null)
            {
                foreach (TerrainChunk chunk in TerrainGenerator.Instance.terrainChunks.Values)
                {
                    if (chunk.IsInBounds(bounds))
                    {
                        chunk.MarkDirty();

                    }
                }
            }
        }

        void OnDestroy()
        {
            if(TerrainGenerator.Instance!=null)
            TerrainGenerator.Instance.noiseFunction.OnNoiseModifierRemoved(Source);
        }
    }
}