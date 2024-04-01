using UnityEngine;
using UnityEngine.Profiling;
using System.Threading;
namespace ProcWorld
{
    public class TerrainChunk
    {
        HeightMap m_heightMap;
        Color[] m_colorMap;
        GameObject chunkObj;
        Vector2 position;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        Bounds bounds;
        MeshCollider meshCollider;


        Vector3 m_worldPos;
        float m_heightScale;
        AnimationCurve m_heightCurve;
        int m_defaultLOD;
        public bool Initial { get; private set; }

        public Collider Col { get { return meshCollider; } }
        public GameObject gameObject { get { return chunkObj; } }
        int m_size;
        public Vector2 ChunkCoordinate { get; private set; }

        float[,] m_noise;
        float[,] m_roadNoise;
        float[,] m_roadNoiseBlurred;
        float[,] m_valleyNoiseBlurred;
        TerrainGenerator gen;


        int m_meshInstanceId;

        MeshData m_mainMeshData;
        public bool Processing { get; private set; } = false;


        RoadNoiseConfig m_roadConfig;
        RoadNoiseConfig m_valleyConfig;
        public TerrainChunk(bool _initial, int _noiseMapSize, int _size, float _heightScale, AnimationCurve _heightCurve, Vector2 _coord, Material _mat, Transform _parent, int _defaultLOD)
        {
            gen = TerrainGenerator.Instance;
            ChunkCoordinate = _coord;
            m_size = _size;

            Initial = _initial;
            position = _coord * _size;
            bounds = new Bounds(position, Vector2.one * _size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            m_roadConfig = gen.RoadConfig;
            m_valleyConfig = gen.ValleyConfig;

            m_roadConfig.brush = new(m_roadConfig.brush.keys);
            m_valleyConfig.brush = new(m_valleyConfig.brush.keys);


            chunkObj = new GameObject("Chunk");
            meshRenderer = chunkObj.AddComponent<MeshRenderer>();
            meshCollider = chunkObj.AddComponent<MeshCollider>();
            meshFilter = chunkObj.AddComponent<MeshFilter>();
            meshRenderer.material = _mat;
            // meshRenderer.material.mainTexture = _tex;
            m_worldPos = positionV3;
            chunkObj.transform.position = positionV3;
            // chunkObj.transform.localScale = Vector3.one * _size / 10f;
            chunkObj.transform.parent = _parent;
            chunkObj.transform.gameObject.layer = _parent.gameObject.layer;


            m_heightScale = _heightScale;
            m_heightCurve = new(_heightCurve.keys);


            SetVisibility(false);
            m_defaultLOD = _defaultLOD;

            // allocate memory only once
            m_noise = new float[_noiseMapSize, _noiseMapSize];
            m_roadNoise = new float[_noiseMapSize, _noiseMapSize];
            m_roadNoiseBlurred = new float[_noiseMapSize + gen.RoadConfig.blurPadding, _noiseMapSize + gen.RoadConfig.blurPadding];
            m_valleyNoiseBlurred = new float[_noiseMapSize + m_valleyConfig.blurPadding, _noiseMapSize + m_valleyConfig.blurPadding];
            m_heightMap = new(_noiseMapSize, _noiseMapSize, 1, m_noise);
            // Regenerate();

        }
        public void Regenerate()
        {
            // ThreadPool.QueueUserWorkItem(CreateMapData, gen);
            // Thread th = new Thread(new ThreadStart(CreateMapData));
            // th.IsBackground = true;
            // th.Start();

            Processing = true;
            CreateMapData();

        }

        public void UpdateCoord(Vector2 _coord)
        {
            ChunkCoordinate = _coord;
            position = ChunkCoordinate * m_size;
            bounds.center = position;
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);
            m_worldPos = positionV3;

        }


        public Mesh GetMesh() { return meshFilter.mesh; }
        void CreateMapData()
        {


            Helpers.Reset2DArray(m_noise);
            Helpers.Reset2DArray(m_roadNoise);
            Helpers.Reset2DArray(m_roadNoiseBlurred);
            Helpers.Reset2DArray(m_valleyNoiseBlurred);



            float ofstX = gen._offsetX + m_worldPos.x;
            float ofstY = gen._offsetY + m_worldPos.z;

            NoiseGenerator.GenerateNoiseMapNonAlloc(m_noise, gen.PerlinConfig, gen.VertsPerSide() + 2, gen.VertsPerSide() + 2, ofstX, ofstY);

            gen.AddRoadNoiseNonAlloc(m_noise, m_roadConfig, m_roadNoise, m_roadNoiseBlurred, ofstX, ofstY);

            // reuse m_roadNoise for valleyNoise, don't need its values again
            Helpers.Reset2DArray(m_roadNoise);


            // gen.CreateValleyAroundRoadNonAlloc(m_noise, m_valleyConfig, m_roadNoise, m_valleyNoiseBlurred, ofstX, ofstY);
            NoiseGenerator.NormalizeGloballyNonAlloc(m_noise, gen.VertsPerSide() + 2, gen.VertsPerSide() + 2, gen.PerlinConfig.standardMaxValue + gen.ValleyNoiseExtrusion);

            m_heightMap.UpdateNoise(m_noise);
            // no colormap so null instead of gen.ColorMapFromNoise
            MapData mapdata = new(m_heightMap, null, gen.VertsPerSide() + 2, gen.VertsPerSide() + 2);

            OnMapDataCreated(mapdata);

        }
        void OnMapDataCreated(MapData _data)
        {
            // create mesh data
            MeshData md = MeshGenerator.GenerateMeshFromHeightMap(_data.GetHeightMap(), m_heightScale, m_heightCurve, m_defaultLOD);


            MainThreadDispatcher.Instance.Enqueue(() =>
       {
           OnMeshdataCreated(md);
       });

        }


        void OnMeshdataCreated(MeshData _data)
        {
            // mesh from data
            // set collider           

            Profiler.BeginSample("Terrain chunk created, creating mesh");
            if (meshFilter.mesh != null)
            {
                meshFilter.mesh.SetVertices(_data.vertices);
                meshFilter.mesh.SetTriangles(_data.triangles, 0);
                meshFilter.mesh.SetNormals(_data.normals);
                meshFilter.mesh.SetUVs(0, _data.uvs);
                meshFilter.mesh.RecalculateBounds();
            }
            else
            {
                meshFilter.mesh = _data.CreateMesh();
            }
            chunkObj.transform.position = m_worldPos;
            m_meshInstanceId = meshFilter.mesh.GetInstanceID();

            Profiler.EndSample();




            TerrainGenerator.Instance.OnChunkCreated(this);


        }
        public void CreateCollider()
        {
            meshCollider.sharedMesh = meshFilter.mesh;
        }
        public void BakeMeshForCollision()
        {
            Physics.BakeMesh(m_meshInstanceId, false);
            int id = m_meshInstanceId;
            MainThreadDispatcher.Instance.Enqueue(() =>
              {
                  OnMeshBakedForCollision();
              });
        }

        void OnMeshBakedForCollision()
        {
            meshCollider.sharedMesh = meshFilter.mesh;
            TerrainGenerator.Instance.OnChunkPhysicsCreated(this);
            SetVisibility(true);

            Processing = false;
        }

        public HeightMap GetHeightMap()
        {
            return m_heightMap;
        }
        public void UpdateChunk()
        {
            float closestDist = Mathf.Sqrt(bounds.SqrDistance(TerrainGenerator.playerPos));
            bool isVisible = closestDist <= TerrainGenerator._drawDistance;

            SetVisibility(isVisible);
        }

        public void SetVisibility(bool _v)
        {
            chunkObj.SetActive(_v);
        }


        public void SetMesh(Mesh _mesh)
        {
            meshFilter.mesh = _mesh;
        }

        public bool isVisible()
        {
            return chunkObj.activeSelf;
        }
    }


}