using UnityEngine;
using UnityEngine.Profiling;
using System.Threading;
using System.Collections.Generic;
namespace ProcWorld
{



   
    class TerrainChunkLOD
    {
        public MeshRenderer meshRenderer;
        public MeshFilter meshFilter;

        public GameObject go;

        public bool Empty { get; private set; }
        public TerrainChunkLOD(Material mat, Transform _parent, bool _empty)
        {

            Empty = _empty;

            go = new GameObject("Chunk");
            go.transform.parent = _parent;
            go.transform.localPosition = Vector3.zero;
            go.gameObject.layer = _parent.gameObject.layer;
            if (Empty) return;

            meshRenderer = go.AddComponent<MeshRenderer>();
            meshFilter = go.AddComponent<MeshFilter>();
            meshRenderer.material = mat;


        }

        public void OnMeshDataCreated(MeshData _data)
        {

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
        }


    }
    public class TerrainChunk
    {
        HeightMap m_heightMap;
        Color[] m_colorMap;
        GameObject chunkObj;
        Vector2 position;
        // MeshRenderer meshRenderer;
        // MeshFilter meshFilter;
        MeshCollider meshCollider;
        Bounds bounds;

        GameObject colliderObject;


        // contrary to popular belief and common sense
        // this is not actually worldpos, this is position from terrain generator origin (normalized_coord*size)
        public Vector3 m_worldPos;
        float m_heightScale;
        AnimationCurve m_heightCurve;
        int m_defaultLOD;
        public bool Initial { get; private set; }

        public Collider Col { get { return meshCollider; } }
        public GameObject gameObject { get { return chunkObj; } }
        int m_size;
        public Vector2 ChunkCoordinate { get; private set; }

        public float[,] m_noise;
        float[,] m_roadNoise;
        float[,] m_roadNoiseBlurred;
        float[,] m_valleyNoiseBlurred;
        TerrainGenerator gen;


        int m_meshInstanceId;

        MeshData m_mainMeshData;
        public bool Processing { get; private set; } = false;


        RoadNoiseConfig m_roadConfig;
        RoadNoiseConfig m_valleyConfig;


        public List<float[,]> PreAllocatedNoise;
        const int NUM_LOD = 4;
        const int LOD_OFFSET = 0;
        const int LOD_STEP_SIZE = 2;
        const int COLLIDER_LOD_INDEX = 0;
        const int LOD_ENABLE_DISTANCE = 150;
        List<MeshData> m_lodMeshData = new(NUM_LOD);
        List<TerrainChunkLOD> m_lods = new(NUM_LOD);
        Material m_mat;

        int m_currentLOD = 0;
        public Vector3[] Normals { get; private set; }


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
            colliderObject = new GameObject("Collider");
            colliderObject.transform.parent = chunkObj.transform;
            colliderObject.transform.localPosition = Vector3.zero;

            meshCollider = colliderObject.AddComponent<MeshCollider>();
            // meshRenderer = chunkObj.AddComponent<MeshRenderer>();
            // meshFilter = chunkObj.AddComponent<MeshFilter>();
            // meshRenderer.material = _mat;

            m_mat = _mat;
            // meshRenderer.material.mainTexture = _tex;
            m_worldPos = positionV3;
            chunkObj.transform.localPosition = positionV3;
            // chunkObj.transform.localScale = Vector3.one * _size / 10f;
            chunkObj.transform.parent = _parent;
            chunkObj.transform.gameObject.layer = _parent.gameObject.layer;
            colliderObject.transform.gameObject.layer = _parent.gameObject.layer;


            m_heightScale = _heightScale;
            m_heightCurve = new(_heightCurve.keys);


            SetVisibility(false);
            m_defaultLOD = _defaultLOD;

            Normals = new Vector3[_size * _size];
            // allocate memory only once
            m_noise = new float[_noiseMapSize, _noiseMapSize];

            // m_roadNoise = new float[_noiseMapSize, _noiseMapSize];
            // m_roadNoiseBlurred = new float[_noiseMapSize + gen.RoadConfig.blurPadding, _noiseMapSize + gen.RoadConfig.blurPadding];
            // m_valleyNoiseBlurred = new float[_noiseMapSize + m_valleyConfig.blurPadding, _noiseMapSize + m_valleyConfig.blurPadding];

            m_heightMap = new(_noiseMapSize, _noiseMapSize, 1, m_noise);


            CreateLODObjects();
            // Regenerate();
        }

        public bool Dirty { get; private set; }
        public void MarkDirty()
        {
            Dirty = true;
        }

        void CreateLODObjects()
        {
            for (int i = 0; i < NUM_LOD; i++)
            {
                TerrainChunkLOD lod = new(m_mat, chunkObj.transform, NUM_LOD - 1 == i);
                lod.go.gameObject.SetActive(false);
                m_lods.Add(lod);
            }
            m_lods[m_currentLOD].go.SetActive(true);
        }
        public void OnUpdate()
        {
            float closestDist = Mathf.Sqrt(bounds.SqrDistance(TerrainGenerator.PlayerPosV2));


            int index = NUM_LOD - 1;
            for (int i = NUM_LOD - 1; i >= 0; i--)
            {
                float lod_distance = (i + 1) * LOD_ENABLE_DISTANCE;

                if (closestDist < lod_distance)
                {
                    index = i;
                }
            }
            if (m_currentLOD != index)
            {
                m_lods[m_currentLOD].go.SetActive(false);
                m_currentLOD = index;
                m_lods[m_currentLOD].go.SetActive(true);
            }
        }

        public Vector3 GetNormalAt(int x, int y)
        {
            return Normals[y * gen.VertsPerSide() + x];
        }


        void GenerateLODMeshes(MapData _data)
        {
            m_lodMeshData.Clear();

            for (int i = 0; i < NUM_LOD; i++)
            {
                if (m_lods[i].Empty) return; // last lod is empty
                MeshData md = MeshGenerator.GenerateMeshFromHeightMap(_data.GetHeightMap(), m_heightScale, m_heightCurve, i * LOD_STEP_SIZE + LOD_OFFSET);

                m_lodMeshData.Add(md);
            }
        }

        public Material GetMaterial() { return m_mat; }
        System.Diagnostics.Stopwatch sw = new();
        public void Regenerate()
        {
            Dirty = false;
            // ThreadPool.QueueUserWorkItem(CreateMapData, gen);
            // Thread th = new Thread(new ThreadStart(CreateMapData));
            // th.IsBackground = true;
            // th.Start();

            Processing = true;

            if (Initial)
            {
                sw.Start();
            }
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

        public Mesh GetMesh() { return m_lods[COLLIDER_LOD_INDEX].meshFilter.mesh; }
        void CreateMapData()
        {


            // Helpers.Reset2DArray(m_noise);
            // Helpers.Reset2DArray(m_roadNoise);
            // Helpers.Reset2DArray(m_roadNoiseBlurred);
            // Helpers.Reset2DArray(m_valleyNoiseBlurred);



            // float ofstX = gen._offsetX + m_worldPos.x;
            // float ofstY = gen._offsetY + m_worldPos.z;

            // NoiseGenerator.GenerateNoiseMapNonAlloc(m_noise, gen.PerlinConfig, gen.VertsPerSide() + 2, gen.VertsPerSide() + 2, ofstX, ofstY);

            // gen.AddRoadNoiseNonAlloc(m_noise, m_roadConfig, m_roadNoise, m_roadNoiseBlurred, ofstX, ofstY);

            // // reuse m_roadNoise for valleyNoise, don't need its values again
            // Helpers.Reset2DArray(m_roadNoise);


            // gen.CreateValleyAroundRoadNonAlloc(m_noise, m_valleyConfig, m_roadNoise, m_valleyNoiseBlurred, ofstX, ofstY);
            // NoiseGenerator.NormalizeGloballyNonAlloc(m_noise, gen.VertsPerSide() + 2, gen.VertsPerSide() + 2, gen.PerlinConfig.standardMaxValue + gen.ValleyNoiseExtrusion);

            gen.noiseFunction.GenerateNonAlloc(this);

            m_heightMap.UpdateNoise(m_noise);
            // no colormap so null instead of gen.ColorMapFromNoise
            MapData mapdata = new(m_heightMap, null, gen.VertsPerSide() + 2, gen.VertsPerSide() + 2);

            OnMapDataCreated(mapdata);

        }
        void OnMapDataCreated(MapData _data)
        {
            // create mesh data
            MeshData md = MeshGenerator.GenerateMeshFromHeightMap(_data.GetHeightMap(), m_heightScale, m_heightCurve, m_defaultLOD);
            GenerateLODMeshes(_data);

            Normals = md.normals;

            sw.Stop();

            double elapsed = sw.Elapsed.TotalMilliseconds / 1000f;
            MainThreadDispatcher.Instance.Enqueue(() =>
       {
           OnMeshdataCreated(m_lodMeshData);
           //    OnMeshdataCreated(md);
       });

        }


        void OnMeshdataCreated(List<MeshData> _lodMeshData)
        {
            // mesh from data
            // set collider           

            Profiler.BeginSample("Terrain chunk created, creating mesh");
            // if (meshFilter.mesh != null)
            // {
            //     meshFilter.mesh.SetVertices(_data.vertices);
            //     meshFilter.mesh.SetTriangles(_data.triangles, 0);
            //     meshFilter.mesh.SetNormals(_data.normals);
            //     meshFilter.mesh.SetUVs(0, _data.uvs);
            //     meshFilter.mesh.RecalculateBounds();
            // }
            // else
            // {
            //     meshFilter.mesh = _data.CreateMesh();
            // }

            for (int i = 0; i < NUM_LOD; i++)
            {
                if (m_lods[i].Empty) continue;
                m_lods[i].OnMeshDataCreated(_lodMeshData[i]);
            }



            chunkObj.transform.localPosition = m_worldPos;
            m_meshInstanceId = m_lods[COLLIDER_LOD_INDEX].meshFilter.mesh.GetInstanceID();





            Profiler.EndSample();







            TerrainGenerator.Instance.OnChunkCreated(this);


        }
        public void CreateCollider()
        {
            meshCollider.sharedMesh = GetMesh();
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
            meshCollider.sharedMesh = GetMesh();
            TerrainGenerator.Instance.OnChunkPhysicsCreated(this);
            SetVisibility(true);

            Processing = false;
        }

        public HeightMap GetHeightMap()
        {
            return m_heightMap;
        }

        public bool IsInBounds(BoundsVec2 bounds)
        {
            if (Mathf.Abs(ChunkCoordinate.x - bounds.center.x) > Mathf.Abs(bounds.size.x/2f) || Mathf.Abs(ChunkCoordinate.y - bounds.center.y) > Mathf.Abs(bounds.size.y/2f))
            {
                return false;
            }
            return true;
        }

        public bool IsInWorldSpaceBounds(BoundsVec2 bounds)
        {
            if (Mathf.Abs(m_worldPos.x - bounds.center.x) > Mathf.Abs(bounds.size.x/2f) || Mathf.Abs(m_worldPos.z - bounds.center.y) > Mathf.Abs(bounds.size.y/2f))
            {
                return false;
            }
            return true;
        }


        public void SetVisibility(bool _v)
        {
            chunkObj.SetActive(_v);
        }


        public void SetMesh(Mesh _mesh)
        {
            m_lods[COLLIDER_LOD_INDEX].meshFilter.mesh = _mesh;
        }

        public bool isVisible()
        {
            return chunkObj.activeSelf;
        }
    }


}