using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

namespace ProcWorld
{
    public class TerrainGenerator : MonoBehaviour
    {

        enum TerrainMode
        {
            Static,
            Endless
        }

        [Header("Background Processing")]
        [SerializeField] int m_timeStepMS;
        [SerializeField] int m_numBackgroundThreads;


        [Header("Components")]
        public FeatureGenerator _featureGenerator;
        [field: SerializeField] public NoiseFunction noiseFunction { get; private set; }
        public Material _terrainMat;
        public AnimationCurve _heightCurve;
        public Transform _player;
        public static Vector2 playerPos { get; private set; }

        [Header("Terrain Config")]
        [field: SerializeField] TerrainMode Mode;
        [SerializeField] Transform m_chunksParent;
        public float _heightScale;
        [field: SerializeField, Range(0, 6)] public int DefaultLOD { get; private set; }
        [field: SerializeField] public bool Normalize { get; private set; }
        public static int _drawDistance = 600;
        public TerrainType[] terrainTypes;
        [field: SerializeField] public RoadNoiseConfig RoadConfig { get; private set; }
        [field: SerializeField] public CurveConfig RoadCurveConfig { get; private set; }
        [field: SerializeField] public CurveConfig ValleyCurveConfig { get; private set; }
        [field: SerializeField] public float RoadNoiseMaxHeight { get; private set; }
        [field: SerializeField] public float RoadNoiseBlend { get; private set; }
        [field: SerializeField] public RoadNoiseConfig ValleyConfig { get; private set; }
        [field: SerializeField] public float ValleyNoiseExtrusion { get; private set; }
        [field: SerializeField] public float ValleyNoiseBlend { get; private set; }

        [SerializeField, Range(1, 250)] public int m_chunkSize;
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

        public Dictionary<Vector2, TerrainChunk> terrainChunks { get; private set; } = new Dictionary<Vector2, TerrainChunk>();
        private List<TerrainChunk> m_visibleChunks = new List<TerrainChunk>();

        public delegate void TerrainGeneratorEvent();
        public delegate void TerrainChunkEvent(TerrainChunk _chunk);

        public TerrainGeneratorEvent EOnFinished;
        public TerrainChunkEvent EChunkCreated;
        public TerrainChunkEvent EChunkPhysicsCreated;
        public TerrainGeneratorEvent EInitialChunksCreated;
        public TerrainChunkEvent EChunkGameObjectCreated;


        public List<Collider> PhysicsColliders { get; private set; } = new();

        List<TerrainChunk> m_chunkpool = new();


        List<TerrainBackgroundProcessor> m_processors = new();


        Thread m_terrainGenerationThread;

        int m_nextProcessor = 0;
        public TerrainBackgroundProcessor GetNextProcessor()
        {


            // round robin
            // give tasks to processors in a sequence one by one
            // probably not ideal but good enough
            // could choose processors that are free instead or have fewer tasks remaining.
            TerrainBackgroundProcessor pr = m_processors[m_nextProcessor];
            m_nextProcessor++;
            m_nextProcessor %= m_processors.Count;

            return pr;



        }
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

            float[,] road = new float[VertsPerSide() + 2, VertsPerSide() + 2];
            float[,] roadBlurred = new float[VertsPerSide() + 2 + RoadConfig.blurPadding, VertsPerSide() + 2 + RoadConfig.blurPadding];
            AddRoadNoiseNonAlloc(noise, RoadConfig, road, roadBlurred, testX, testY);

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

            return _noise;
        }

        // pass _config separately here instead of using the global TerrainGenerator.RoadConfig because 
        // animation curves don't work well with multi threading, RoadConfig stores brush as animation curve

        public float[,] CreateValleyAroundRoad(float _ofstX, float _ofstY, float[,] _noise)
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

        public void AddRoadNoiseNonAlloc(float[,] to, RoadNoiseConfig _config, float[,] roadNoise, float[,] blurredRoadNoise, float _ofstX, float _ofstY)
        {

            NoiseGenerator.GenerateLongitudinalSinNoiseNonAlloc(roadNoise, blurredRoadNoise, VertsPerSide() + 2, VertsPerSide() + 2, _config, _ofstX, _ofstY, RoadHorizontalPerlinConfig, RoadVerticalPerlinConfig);
            // NoiseGenerator.GenerateCurveNonAlloc(roadNoise, VertsPerSide() + 2, VertsPerSide() + 2, RoadCurveConfig, _ofstX, _ofstY);

            for (int i = 0; i < roadNoise.GetLength(1); i++)
            {
                for (int j = 0; j < roadNoise.GetLength(0); j++)
                {
                    // float vert = verticality[0,i];
                    // vert += RoadNoiseMaxHeight;
                    // vert = Mathf.Clamp01(vert);



                    // figure out why this blending doesn't work properly
                    to[i, j] = Mathf.Lerp(to[i, j], RoadNoiseMaxHeight, roadNoise[i, j] * RoadNoiseBlend);
                }
            }
        }
        public void CreateValleyAroundRoadNonAlloc(float[,] _noise, RoadNoiseConfig _valleyConfig, float[,] generatedRoadNoise, float[,] generatedBlurredNoise, float _ofstX, float _ofstY)
        {
            NoiseGenerator.GenerateLongitudinalSinNoiseNonAlloc(generatedRoadNoise, generatedBlurredNoise, VertsPerSide() + 2, VertsPerSide() + 2, _valleyConfig, _ofstX, _ofstY, ValleyPerlinConfig, RoadVerticalPerlinConfig);
            // NoiseGenerator.GenerateCurveNonAlloc(generatedRoadNoise, VertsPerSide() + 2, VertsPerSide() + 2, ValleyCurveConfig, _ofstX, _ofstY);
            for (int i = 0; i < generatedRoadNoise.GetLength(1); i++)
            {
                for (int j = 0; j < generatedRoadNoise.GetLength(0); j++)
                {
                    // float vert = verticality[0,i];
                    // vert += ValleyNoiseExtrusion;
                    // vert = Mathf.Clamp01(vert);



                    // for stamp noise
                    _noise[i, j] = Mathf.Lerp(_noise[i, j], PerlinConfig.standardMaxValue + ValleyNoiseExtrusion, Mathf.Clamp01((1 - generatedRoadNoise[i, j])) * ValleyNoiseBlend);


                    // for curve noise
                    // _noise[i, j] = Mathf.Lerp(_noise[i, j], PerlinConfig.standardMaxValue + ValleyNoiseExtrusion, Mathf.Clamp01((generatedRoadNoise[i, j])) * ValleyNoiseBlend);


                }
            }
        }
        public float[,] GetRoadNoise(float _ofstX, float _ofstY, float[,] _noise)
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
                    TerrainChunk chunk = new TerrainChunk(true, VertsPerSide() + 2, m_chunkSize, _heightScale, _heightCurve, pos, _terrainMat, m_chunksParent, DefaultLOD);
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



        List<Vector2> m_toRemove = new();
        int m_chunksFinished = 0;

        bool m_initialChunksCreated = false;

        bool GetFreeChunkFromPool(out TerrainChunk ch, out int _index)
        {
            for (int i = 0; i < m_chunkpool.Count; i++)
            {
                if (!m_chunkpool[i].Processing)
                {
                    ch = m_chunkpool[i];
                    _index = i;
                    return true;
                };
            }
            ch = null;
            _index = 0;
            return false;
        }
        private void GenerateEndlessTerrain()
        {



            // for (int i = 0; i < m_visibleChunks.Count; i++)
            // {
            //     m_visibleChunks[i].SetVisibility(false);
            // }
            // m_visibleChunks.Clear();


            Profiler.BeginSample("Generate endless terrain");

            m_toRemove.Clear();
            int currentChunkCoordX = Mathf.RoundToInt(playerPos.x / m_chunkSize);
            int currentChunkCoordY = Mathf.RoundToInt(playerPos.y / m_chunkSize);
            foreach (var _ch in terrainChunks)
            {

                Profiler.BeginSample("Set Visibility");
                if (Mathf.Abs(_ch.Value.ChunkCoordinate.x - currentChunkCoordX) > Mathf.Abs(maxChunksVisible) || Mathf.Abs(_ch.Value.ChunkCoordinate.y - currentChunkCoordY) > Mathf.Abs(maxChunksVisible))
                {
                    _ch.Value.SetVisibility(false);
                    m_toRemove.Add(_ch.Key);
                }
                Profiler.EndSample();

            }

            Profiler.BeginSample("add to pool");
            for (int i = 0; i < m_toRemove.Count; i++)
            {
                TerrainChunk c = terrainChunks[m_toRemove[i]];
                m_chunkpool.Add(c);
                terrainChunks.Remove(m_toRemove[i]);
            }
            Profiler.EndSample();
            for (int yOffset = -maxChunksVisible; yOffset <= maxChunksVisible; yOffset++)
            {
                for (int xOffset = -maxChunksVisible; xOffset <= maxChunksVisible; xOffset++)
                {

                    Profiler.BeginSample("check if already visible");

                    Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                    bool containskey = terrainChunks.ContainsKey(viewedChunkCoord);
                    Profiler.EndSample();

                    if (containskey) continue;

                    if (m_chunkpool.Count == 0) continue;

                    bool found = GetFreeChunkFromPool(out TerrainChunk chunk, out int _index);
                    if (!found) continue;

                    chunk.UpdateCoord(viewedChunkCoord);
                    m_chunkpool.RemoveAt(_index);
                    terrainChunks.Add(viewedChunkCoord, chunk);


                    Profiler.BeginSample("enqueue");
                    GetNextProcessor().EnqueueChunk(chunk);
                    Profiler.EndSample();



                    // old
                    // if (terrainChunks.ContainsKey(viewedChunkCoord))
                    // {
                    //     terrainChunks[viewedChunkCoord].UpdateChunk();
                    //     if (terrainChunks[viewedChunkCoord].isVisible())
                    //     {
                    //         m_visibleChunks.Add(terrainChunks[viewedChunkCoord]);
                    //     }
                    // }
                    // else
                    // {

                    //     TerrainChunk chunk = new TerrainChunk(false, m_chunkSize, _heightScale, _heightCurve, viewedChunkCoord, _terrainMat, m_chunksParent, DefaultLOD);
                    //     terrainChunks.Add(viewedChunkCoord, chunk);

                    //     m_visibleChunks.Add(chunk);
                    //     chunk.SetVisibility(true);
                    // }
                }
            }

            Profiler.EndSample();
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


        bool m_started = false;
        System.Diagnostics.Stopwatch m_chunkCreationStopwatch = new();


        public void Stop()
        {
            m_started = false;
        }
        public void Init()
        {
            m_chunkCreationStopwatch.Start();

            if (m_timeStepMS < 10)
            {
                m_timeStepMS = 10;
                Debug.Log("Timestep too small, changing to 10");
            }
            if (m_numBackgroundThreads == 0)
            {
                Debug.Log("Processors set to 0, not allowed. Changing to 1.");
                m_numBackgroundThreads = 1;
            }


            m_processors = new(m_numBackgroundThreads);

            for (int i = 0; i < m_numBackgroundThreads; i++)
            {
                m_processors.Add(new(m_timeStepMS));
            }

            maxChunksVisible = Mathf.RoundToInt(_drawDistance / m_chunkSize);

            playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);

            if (Mode == TerrainMode.Static)
            {
                GenerateTerrain();

            }
            else
            {
                CreateChunkPool();
            }

            m_started = true;

        }

        void CreateChunkPool()
        {

            int extraChunks = 0;
            m_initialEndlessChunks = (maxChunksVisible + extraChunks) * (maxChunksVisible + extraChunks) * 4;
            m_initialEndlessChunks += (maxChunksVisible + extraChunks) * 4 + 1;



            int currentChunkCoordX = Mathf.RoundToInt(playerPos.x / m_chunkSize);
            int currentChunkCoordY = Mathf.RoundToInt(playerPos.y / m_chunkSize);

            int created = 0;

            for (int yOffset = -(maxChunksVisible + extraChunks); yOffset <= (maxChunksVisible + extraChunks); yOffset++)
            {
                for (int xOffset = -(maxChunksVisible + extraChunks); xOffset <= (maxChunksVisible + extraChunks); xOffset++)
                {
                    Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                    TerrainChunk chunk = new TerrainChunk(true, VertsPerSide() + 2, m_chunkSize, _heightScale, _heightCurve, viewedChunkCoord, _terrainMat, m_chunksParent, DefaultLOD);
                    chunk.UpdateCoord(viewedChunkCoord);

                    terrainChunks.Add(viewedChunkCoord, chunk);
                    chunk.SetVisibility(true);
                    EChunkGameObjectCreated?.Invoke(chunk);

                    GetNextProcessor().EnqueueChunk(chunk);
                    // chunk.UpdateChunk();
                    // if (chunk.isVisible())
                    // {
                    //     m_visibleChunks.Add(terrainChunks[viewedChunkCoord]);
                    // }
                    created++;
                    // m_chunkpool.Add(chunk);
                }
            }
            if (m_initialEndlessChunks != created)
            {
                Debug.Log("Created chunks: " + created + " not equal to expected amount: " + m_initialEndlessChunks);
            }

        }
        private void FixedUpdate()
        {

            if (!m_initialChunksCreated) return;
            if (!m_started) return;
            playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);
            if (Mode == TerrainMode.Endless)
            {
                GenerateEndlessTerrain();
            }
        }



        public void OnChunkCreated(TerrainChunk _c)
        {


            GetNextProcessor().EnqueuePhysics(_c);
            EChunkCreated?.Invoke(_c);



        }


        public void OnChunkPhysicsCreated(TerrainChunk _c)
        {


            PhysicsColliders.Add(_c.Col);

            m_chunksFinished++;
            EChunkPhysicsCreated?.Invoke(_c);
            if (m_chunksFinished == m_initialEndlessChunks)
            {
                Debug.Log("Chunks created: " + m_chunksFinished);

                m_chunkCreationStopwatch.Stop();
                Debug.Log("Initial chunks created, took: " + m_chunkCreationStopwatch.ElapsedMilliseconds / 1000f + " seconds");

                EInitialChunksCreated?.Invoke();

                m_initialChunksCreated = true;

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