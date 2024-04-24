using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{

    public abstract class NoiseFunction : MonoBehaviour
    {
        public abstract void GenerateNonAlloc(TerrainChunk _c);
        public abstract bool ShouldGeneratePhysics(TerrainChunk _c);
    }


}