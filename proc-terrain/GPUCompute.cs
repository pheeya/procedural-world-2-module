using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class GPUCompute : MonoBehaviour
    {
        // Start is called before the first frame update
        [field: SerializeField] public ComputeShader ComputeHeightMap { get; private set; }
        // void Start()
        // {
        //     int kernel = ComputeHeightMap.FindKernel("CSMain");
        //     RenderTexture texture = new(5, 5, 5);
        //     texture.enableRandomWrite = true;
        //     ComputeHeightMap.SetTexture(kernel, Shader.PropertyToID("Result"), texture);
        //     ComputeHeightMap.Dispatch(kernel, 1, 1, 1);
        // }
        public static void CreateHeightmap(ComputeShader _shader, string _kernel, int _width, int _height, ComputeBuffer _result, PerlinNoiseConfig _config, RoadNoiseConfig _roadConfig, RoadNoiseConfig _valleyConfig)
        {
            int kernel = _shader.FindKernel(_kernel);
            _result = new(_width * _height, 1 * sizeof(float));


        }
    }

}