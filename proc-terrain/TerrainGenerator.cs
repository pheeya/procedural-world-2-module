using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProcWorld
{
    public class TerrainGenerator : MonoBehaviour
    {

        enum TerrainMode
        {
            Static,
            Endless
        }


        [Header("Components")]
        public FeatureGenerator _featureGenerator;
        public Material _terrainMat;
        public AnimationCurve _heightCurve;
        public Transform _player;
        private static Vector2 playerPos;

        [Header("Terrain Config")]
        [field: SerializeField] TerrainMode Mode;
        [SerializeField] Transform m_chunksParent;
        public float _heightScale;
        [field: SerializeField, Range(0, 6)] public int DefaultLOD { get; private set; }
        [field: SerializeField] public bool Normalize { get; private set; }
        public static int _drawDistance = 500;
        public TerrainType[] terrainTypes;
        [field: SerializeField] public RoadNoiseConfig RoadConfig { get; private set; }
        [field: SerializeField] public float RoadNoiseMaxHeight { get; private set; }
        [field: SerializeField] public float RoadNoiseBlend { get; private set; }
        [field: SerializeField] public RoadNoiseConfig ValleyConfig { get; private set; }
        [field: SerializeField] public float ValleyNoiseExtrusion { get; private set; }
        [field: SerializeField] public float ValleyNoiseBlend { get; private set; }

        [SerializeField, Range(1, 250)] int m_chunkSize;
        [SerializeField] int m_neighboursX;
        [SerializeField] int m_neighboursY;
        [field: SerializeField] public PerlinNoiseConfig PerlinConfig { get; private set; }
        [field: SerializeField] public PerlinNoiseConfig RoadHorizontalPerlinConfig { get; private set; }
        [field: SerializeField] public PerlinNoiseConfig RoadVerticalPerlinConfig { get; private set; }
        [field: SerializeField] public PerlinNoiseConfig ValleyPerlinConfig { get; private set; }
        private static int maxChunksVisible;

        public float _offsetX, _offsetY;

        public float testX;
        public float testY;

        Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
        private List<TerrainChunk> m_visibleChunks = new List<TerrainChunk>();

        public delegate void TerrainGeneratorEvent();
        public delegate void TerrainChunkEvent(TerrainChunk _chunk);

        public TerrainGeneratorEvent EOnFinished;
        public TerrainChunkEvent EChunkCreated;
        public TerrainGeneratorEvent EInitialChunksCreated;


        public List<Collider> PhysicsColliders { get; private set; } = new();

        public Vector2 GetFinalTerrainSize()
        {
            return new Vector2(m_chunkSize * m_neighboursX, m_chunkSize * m_neighboursY);
        }
        public Color[] ColorMapFromHeight(HeightMap _hm)
        {

            Color[] colormap = new Color[_hm.Width * _hm.Height];
            for (int y = 0; y < _hm.Height; y++)
            {
                for (int x = 0; x < _hm.Width; x++)
                {
                    float value = _hm.Values[x, y];
                    // loop through all terrain height regions and assign color
                    for (int i = 0; i < terrainTypes.Length; i++)
                    {
                        if (value <= terrainTypes[i].height)
                        {
                            int alreadyColored = y * _hm.Height;
                            colormap[alreadyColored + x] = terrainTypes[i].color;
                            break;
                        }
                    }

                }
            }
            return colormap;
        }



        public MapData GenerateMapData(float _ofX, float _ofY)
        {
            float[,] noise = NoiseGenerator.GenerateNoiseMap(PerlinConfig, VertsPerSide(), VertsPerSide(), _ofX, _ofY);
            HeightMap heightmap = HeightMap.FromNoise(noise, 0);
            Color[] colormap = new Color[VertsPerSide() * VertsPerSide()];

            for (int y = 0; y < VertsPerSide(); y++)
            {
                for (int x = 0; x < VertsPerSide(); x++)
                {
                    float value = heightmap.Values[x, y];
                    // loop through all terrain height regions and assign color
                    for (int i = 0; i < terrainTypes.Length; i++)
                    {
                        if (value <= terrainTypes[i].height)
                        {
                            int alreadyColored = y * VertsPerSide();
                            colormap[alreadyColored + x] = terrainTypes[i].color;
                            break;
                        }
                    }

                }
            }
            MapData mapdata = new MapData(heightmap, colormap, VertsPerSide(), VertsPerSide());
            return mapdata;
        }
        public int VertsPerSide() { return m_chunkSize + 1; }

        public HeightMap GenerateTestHeightMap()
        {
            float[,] noise = NoiseGenerator.GenerateNoiseMap(PerlinConfig, VertsPerSide() + 2, VertsPerSide() + 2, _offsetX, _offsetY);
            List<float[,]> maps = new(1);
            maps.Add(noise);

            // add valley before normalization, so that it's the highest point
            noise = CreateValleyAroundRoad(0, 0, noise);
            if (Normalize)
            {
                noise = NoiseGenerator.Normalize(maps, VertsPerSide() + 2, VertsPerSide() + 2)[0];
            }
            else
            {
                noise = NoiseGenerator.NormalizeGlobally(noise, VertsPerSide() + 2, VertsPerSide() + 2, PerlinConfig.standardMaxValue + ValleyNoiseExtrusion);
            }

            noise = AddRoadNoise(0, 0, noise);

            return HeightMap.FromNoise(noise, 1);
        }
        public MeshData GenerateTestMeshData()
        {
            HeightMap hm = GenerateTestHeightMap();

            return MeshGenerator.GenerateMeshFromHeightMap(hm, _heightScale, _heightCurve, DefaultLOD);
        }



        int maxLogs = 50;
        int logs = 0;
        float[,] AddRoadNoise(float _ofstX, float _ofstY, float[,] _noise)
        {
            float[,] roadNoise = NoiseGenerator.GenerateLongitudinalSinNoise(VertsPerSide() + 2, VertsPerSide() + 2, RoadConfig, _ofstX, _ofstY, RoadHorizontalPerlinConfig, RoadVerticalPerlinConfig);
            float[,] verticality = NoiseGenerator.GenerateSingleAxisNoiseMap(RoadVerticalPerlinConfig, VertsPerSide() + 2, VertsPerSide() + 2, _offsetX, _offsetY);
            for (int i = 0; i < roadNoise.GetLength(1); i++)
            {
                for (int j = 0; j < roadNoise.GetLength(0); j++)
                {
                    // float vert = verticality[0,i];
                    // vert += RoadNoiseMaxHeight;
                    // vert = Mathf.Clamp01(vert);



                    // figure out why this blending doesn't work properly
                    _noise[i, j] = Mathf.Lerp(_noise[i, j], RoadNoiseMaxHeight, roadNoise[i, j] * RoadNoiseBlend);
                }
            }

            // for (int y = dif; y < roadNoise.GetLength(1) - dif; y++)
            // {
            //     for (int x = dif; x < roadNoise.GetLength(0) - dif; x++)
            //     {
            //         // float vert = verticality[0,i];
            //         // vert += RoadNoiseMaxHeight;
            //         // vert = Mathf.Clamp01(vert);



            //         // figure out why this blending doesn't work properly
            //         // it was because roadNoise[x,y] was giving big values like 30
            //         _noise[x-dif, y-dif] = Mathf.Lerp(_noise[x-dif, y-dif], RoadNoiseMaxHeight, roadNoise[x,y] * RoadNoiseBlend);
            //     }
            // }

            return _noise;
        }
        float[,] CreateValleyAroundRoad(float _ofstX, float _ofstY, float[,] _noise)
        {
            float[,] valleyNoise = NoiseGenerator.GenerateLongitudinalSinNoise(VertsPerSide() + 2, VertsPerSide() + 2, ValleyConfig, _ofstX, _ofstY, ValleyPerlinConfig, RoadVerticalPerlinConfig);
            float[,] verticality = NoiseGenerator.GenerateSingleAxisNoiseMap(RoadVerticalPerlinConfig, VertsPerSide() + 2, VertsPerSide() + 2, _offsetX, _offsetY);
            for (int i = 0; i < valleyNoise.GetLength(1); i++)
            {
                for (int j = 0; j < valleyNoise.GetLength(0); j++)
                {
                    // float vert = verticality[0,i];
                    // vert += ValleyNoiseExtrusion;
                    // vert = Mathf.Clamp01(vert);




                    _noise[i, j] = Mathf.Lerp(_noise[i, j], PerlinConfig.standardMaxValue + ValleyNoiseExtrusion, Mathf.Clamp01((1 - valleyNoise[i, j])) * ValleyNoiseBlend);


                }
            }



            return _noise;
        }
        float[,] GetRoadNoise(float _ofstX, float _ofstY, float[,] _noise)
        {
            float[,] roadNoise = NoiseGenerator.GenerateLongitudinalSinNoise(VertsPerSide() + 2, VertsPerSide() + 2, RoadConfig, _ofstX, _ofstY, RoadHorizontalPerlinConfig, RoadVerticalPerlinConfig);
            return roadNoise;
        }

        public int GetRoadCenterAtPos(int _yPos)
        {

            int blurredMapWidth = (VertsPerSide() + 1) + RoadConfig.blurPadding;
            float hw = blurredMapWidth / 2f;
            float hh = blurredMapWidth / 2f;

            int chunk = (_yPos / m_chunkSize);

            int localY = (int)hh - _yPos + chunk * m_chunkSize;



            float offsetY = chunk * m_chunkSize;

            return NoiseGenerator.GetPointOnLongNoise(localY, VertsPerSide() + 2, RoadHorizontalPerlinConfig, RoadConfig, offsetY) - (int)hw;
        }
        public Vector2 GetRoadForwardAtPos(int _yPos)
        {
            int blurredMapWidth = (VertsPerSide() + 1) + RoadConfig.blurPadding;
            float hw = blurredMapWidth / 2f;
            float hh = blurredMapWidth / 2f;

            int chunk = (_yPos / m_chunkSize);

            int localY = (int)hh - _yPos + chunk * m_chunkSize;
            float offsetY = chunk * m_chunkSize;


            return NoiseGenerator.GetLongNoiseGradient(localY, VertsPerSide() + 2, RoadHorizontalPerlinConfig, RoadConfig, offsetY);
        }

        public float GetDistanceBetweenRoadPoints(int _fromY, int _toY)
        {
            int xFrom = GetRoadCenterAtPos(_fromY);
            int xTo = GetRoadCenterAtPos(_toY);

            Vector2 from = new(xFrom, _fromY);
            Vector2 to = new(xTo, _toY);

            return (to - from).magnitude;
        }
        public Vector2 GetPointOnRoadWithDistance(int _fromY, float _distance)
        {
            float dist = 0;

            int maxTries = Mathf.Abs(Mathf.RoundToInt((_distance + 10)));
            int tries = 0;


            float finalX = GetRoadCenterAtPos((Mathf.RoundToInt(_fromY))), finalY;
            finalY = _fromY;


            while (dist < _distance && tries < maxTries)
            {
                int xFrom = GetRoadCenterAtPos((Mathf.RoundToInt(finalY)));
                int xTo = GetRoadCenterAtPos((Mathf.RoundToInt(finalY)) + 1);


                Vector2 from = new(xFrom, finalY);
                Vector2 to = new(xTo, finalY + 1);

                dist += (to - from).magnitude;



                finalX = xTo;
                finalY++;


                tries++;
            }

            return new(finalX, finalY);

        }


        public float GetPlayableAreaWidth()
        {
            return ValleyConfig.brushRadius * 2;
        }

        public void GenerateTerrain()
        {
            //float[,] heightmap = NoiseGenerator.GenerateNoiseMap(_seed, VertsPerSide(), VertsPerSide(), _noiseScale, _octaves, _persistance, _lacunarity, _offsetX, _offsetY);


            List<MapData> MapDatas = new();
            List<Color[]> colorMaps = new();
            List<float[,]> noises = new();





            for (int y = 0; y < m_neighboursY; y++)
            {
                for (int x = 0; x < m_neighboursX; x++)
                {

                    float offsetX = _offsetX + (m_chunkSize * x) - (m_neighboursX - 1) / 2 * m_chunkSize;
                    float offsetY = _offsetY + (m_chunkSize * y) - (m_neighboursY - 1) / 2 * m_chunkSize;


                    int index = x + y * m_neighboursX;




                    float[,] no = NoiseGenerator.GenerateNoiseMap(PerlinConfig, VertsPerSide() + 2, VertsPerSide() + 2, offsetX, offsetY);
                    no = CreateValleyAroundRoad(offsetX, offsetY, no);
                    if (!Normalize)
                    {

                        // create road
                        noises[index] = AddRoadNoise(offsetX, offsetY, noises[index]);
                        HeightMap hm = HeightMap.FromNoise(no, 1);
                        MapData md = new(hm, ColorMapFromHeight(hm), VertsPerSide() + 2, VertsPerSide() + 2);
                        MapDatas.Add(md);


                    }


                    // create valley before normalization, set values to greater than 1 so that it's the highest point after normalization
                    noises.Add(no);
                }
            }
            if (Normalize)
            {
                noises = NoiseGenerator.Normalize(noises, VertsPerSide() + 2, VertsPerSide() + 2);
                for (int y = 0; y < m_neighboursY; y++)
                {
                    for (int x = 0; x < m_neighboursX; x++)
                    {
                        int index = x + y * m_neighboursX;
                        float offsetX = _offsetX + (m_chunkSize * x) - (m_neighboursX - 1) / 2 * m_chunkSize;

                        // offsetX = _offsetX - (x - (m_neighboursX - 1) / 2) * m_chunkSize;

                        float offsetY = _offsetY + (m_chunkSize * y) - (m_neighboursY - 1) / 2 * m_chunkSize;

                        // offsetY = _offsetY + (y - (m_neighboursY - 1) / 2) * m_chunkSize;

                        // create road


                        noises[index] = AddRoadNoise(offsetX, offsetY, noises[index]);


                        HeightMap hm = HeightMap.FromNoise(noises[index], 1);
                        MapData md = new(hm, ColorMapFromHeight(hm), VertsPerSide() + 2, VertsPerSide() + 2);
                        MapDatas.Add(md);
                    }
                }
            }


            for (int y = 0; y < m_neighboursY; y++)
            {
                for (int x = 0; x < m_neighboursX; x++)
                {
                    Vector2 pos = new(x - (m_neighboursX - 1) / 2, y - (m_neighboursY - 1) / 2);
                    int index = x + y * m_neighboursX;
                    MapData mapdata = MapDatas[index];
                    Texture tex = TextureGenerator.TextureFromMap(mapdata.colormap, VertsPerSide() + 2, VertsPerSide() + 2);
                    Debug.Log("Creating new chunk");
                    TerrainChunk chunk = new TerrainChunk(true, m_chunkSize, _heightScale, _heightCurve, pos, _terrainMat, m_chunksParent, DefaultLOD);
                    // chunk.SetMesh(MeshGenerator.GenerateMeshFromHeightMap(mapdata.GetHeightMap(), _heightScale, _heightCurve, DefaultLOD).mesh);
                    terrainChunks.Add(pos, chunk);
                    chunk.SetVisibility(true);
                }
            }

            // Texture tex = TextureGenerator.TextureFromMap(mapdata.colormap, VertsPerSide(), VertsPerSide());
            // TerrainChunk terrain = new TerrainChunk(m_chunkSize, new HeightMap(m_chunkSize, m_chunkSize, mapdata.GetHeightMap().Values), mapdata.GetColorMap(), _heightScale, _heightCurve, Vector2.zero, _terrainMat, tex, transform);


            // terrain.SetMesh(MeshGenerator.GenerateMeshFromHeightMap(mapdata.GetHeightMap(), _heightScale, _heightCurve).mesh);
            // terrain.SetVisibility(true);
            //disabled features for now
            //_featureGenerator.GenerateFeatures(NoiseGenerator.GenerateNoiseMap(_seed+1, VertsPerSide(), VertsPerSide(), _noiseScale, _octaves, _persistance, _lacunarity, _offsetX, _offsetY), _heightScale, _heightCurve, mapdata.GetHeightMap());




            EOnFinished?.Invoke();
        }




        bool m_endlessInit = false;
        int m_initialEndlessChunks = 0;

        private void GenerateEndlessTerrain()
        {
            for (int i = 0; i < m_visibleChunks.Count; i++)
            {
                m_visibleChunks[i].SetVisibility(false);
            }
            m_visibleChunks.Clear();

            int currentChunkCoordX = Mathf.RoundToInt(playerPos.x / m_chunkSize);
            int currentChunkCoordY = Mathf.RoundToInt(playerPos.y / m_chunkSize);


            for (int yOffset = -maxChunksVisible; yOffset <= maxChunksVisible; yOffset++)
            {
                for (int xOffset = -maxChunksVisible; xOffset <= maxChunksVisible; xOffset++)
                {
                    Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                    if (terrainChunks.ContainsKey(viewedChunkCoord))
                    {
                        terrainChunks[viewedChunkCoord].UpdateChunk();
                        if (terrainChunks[viewedChunkCoord].isVisible())
                        {
                            m_visibleChunks.Add(terrainChunks[viewedChunkCoord]);
                        }
                    }
                    else
                    {

                        float ofstX = _offsetX + viewedChunkCoord.x * m_chunkSize;
                        float ofstY = _offsetY + viewedChunkCoord.y * m_chunkSize;
                        // float[,] no = NoiseGenerator.GenerateNoiseMap(PerlinConfig, VertsPerSide() + 2, VertsPerSide() + 2, ofstX, ofstY);
                        // no = AddRoadNoise(ofstX, ofstY, no);
                        // no = CreateValleyAroundRoad(ofstX, ofstY, no);
                        // no = NoiseGenerator.NormalizeGlobally(no, VertsPerSide() + 2, VertsPerSide() + 2, PerlinConfig.standardMaxValue + ValleyNoiseExtrusion);
                        // HeightMap hm = HeightMap.FromNoise(no, 1);
                        // MapData mapdata = new(hm, ColorMapFromHeight(hm), VertsPerSide() + 2, VertsPerSide() + 2);

                        // Texture tex = TextureGenerator.TextureFromMap(mapdata.colormap, VertsPerSide() + 2, VertsPerSide() + 2);
                        bool initial = false;
                        if (!m_endlessInit)
                        {
                            m_initialEndlessChunks++;
                            initial = true;
                        }
                        TerrainChunk chunk = new TerrainChunk(initial, m_chunkSize, _heightScale, _heightCurve, viewedChunkCoord, _terrainMat, m_chunksParent, DefaultLOD);
                        terrainChunks.Add(viewedChunkCoord, chunk);


                        m_visibleChunks.Add(chunk);
                        chunk.SetVisibility(true);
                    }
                }
            }
        
            m_endlessInit = true;
        }

        static TerrainGenerator _instance;
        public static TerrainGenerator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<TerrainGenerator>();
                }
                return _instance;
            }
        }

        private void Start()
        {

            maxChunksVisible = Mathf.RoundToInt(_drawDistance / m_chunkSize);
            playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);
            if (Mode == TerrainMode.Static)
            {
                GenerateTerrain();

            }
        }
        private void FixedUpdate()
        {
            playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);
            if (Mode == TerrainMode.Endless)
            {
                GenerateEndlessTerrain();
            }
        }
        int m_chunksFinished = 0;
        public void OnChunkCreated(TerrainChunk _c)
        {

            m_chunksFinished++;

            PhysicsColliders.Add(_c.Col);

            if (m_chunksFinished == m_initialEndlessChunks)
            {
                Debug.Log("Chunks created: " + m_chunksFinished);
                EInitialChunksCreated?.Invoke();
            }
            EChunkCreated?.Invoke(_c);


        }

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
            public TerrainChunk(bool _initial, int _size, float _heightScale, AnimationCurve _heightCurve, Vector2 _coord, Material _mat, Transform _parent, int _defaultLOD)
            {
                Initial = _initial;
                position = _coord * _size;
                bounds = new Bounds(position, Vector2.one * _size);
                Vector3 positionV3 = new Vector3(position.x, 0, position.y);

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
                m_heightCurve = _heightCurve;


                SetVisibility(false);
                m_defaultLOD = _defaultLOD;

                TerrainGenerator gen = TerrainGenerator.Instance;
                // ThreadPool.QueueUserWorkItem(CreateMapData, gen);

                Thread th = new Thread(new ParameterizedThreadStart(CreateMapData));

                th.Start(gen);
            }
            public Mesh GetMesh() { return meshFilter.mesh; }
            void CreateMapData(object _obj)
            {
                TerrainGenerator gen = (TerrainGenerator)_obj;

                float ofstX = gen._offsetX + m_worldPos.x;
                float ofstY = gen._offsetY + m_worldPos.z;

                float[,] no = NoiseGenerator.GenerateNoiseMap(gen.PerlinConfig, gen.VertsPerSide() + 2, gen.VertsPerSide() + 2, ofstX, ofstY);

                no = gen.AddRoadNoise(ofstX, ofstY, no);
                no = gen.CreateValleyAroundRoad(ofstX, ofstY, no);
                no = NoiseGenerator.NormalizeGlobally(no, gen.VertsPerSide() + 2, gen.VertsPerSide() + 2, gen.PerlinConfig.standardMaxValue + gen.ValleyNoiseExtrusion);

                HeightMap hm = HeightMap.FromNoise(no, 1);
                MapData mapdata = new(hm, gen.ColorMapFromHeight(hm), gen.VertsPerSide() + 2, gen.VertsPerSide() + 2);


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
                meshFilter.mesh = _data.CreateMesh();
             
                // create phys on separate thread
                // ThreadPool.QueueUserWorkItem(BakeMeshForCollision, meshFilter.mesh.GetInstanceID());

                Thread th = new Thread(new ParameterizedThreadStart(BakeMeshForCollision));
                th.Start(meshFilter.mesh.GetInstanceID());
                // create phys on main thread
                // meshCollider.sharedMesh = meshFilter.mesh;
                // TerrainGenerator.Instance.OnChunkCreated(this);



            }

            void BakeMeshForCollision(object _obj)
            {
                // Physics.BakeMesh((int)_obj, false);
                MainThreadDispatcher.Instance.Enqueue(() =>
                  {
                      OnMeshBakedForCollision();
                  });
            }

            void OnMeshBakedForCollision()
            {
                meshCollider.sharedMesh = meshFilter.mesh;
                TerrainGenerator.Instance.OnChunkCreated(this);

            }

            public HeightMap GetHeightMap()
            {
                return m_heightMap;
            }
            public void UpdateChunk()
            {
                float closestDist = Mathf.Sqrt(bounds.SqrDistance(playerPos));
                bool isVisible = closestDist <= _drawDistance;

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




        [System.Serializable]
        public struct TerrainType  // can also have a field called features which would be an array of another struct called TerrainFeature
        {
            // requirements
            public string name;
            public float height;
            public Color color;
        }

    }

    public struct MapData
    {

        public readonly int height, width;
        private HeightMap heightmap;
        public readonly Color[] colormap;
        public MapData(HeightMap heightmap, Color[] colormap, int _height, int _width)
        {
            this.heightmap = heightmap;
            this.colormap = colormap;
            height = _height;
            width = _width;
        }
        public void OverrideHeightMap(HeightMap _hm)
        {
            this.heightmap = _hm;
        }
        public HeightMap GetHeightMap() { return heightmap; }
        public Color[] GetColorMap() { return colormap; }
    }
}