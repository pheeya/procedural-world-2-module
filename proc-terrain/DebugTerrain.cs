using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class DebugTerrain : MonoBehaviour
    {
        public MeshFilter _meshFilter;
        public MeshRenderer _meshRenderer;
        public void GenerateMesh(Mesh _mesh, Texture _tex)
        {
            _meshFilter.sharedMesh = _mesh;
            // because of this procedural terrain material texture keeps getting set to null
            // _meshRenderer.sharedMaterial.mainTexture = _tex;
        }

    }
}