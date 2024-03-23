using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.AI;

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
        public static int _drawDistance = 480;
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
        private List<TerrainChunk> terrainChunksVisibleLastFrame = new List<TerrainChunk>();

        public delegate void TerrainGeneratorEvent();
        public delegate void TerrainChunkEvent(TerrainChunk _chunk);

        public TerrainGeneratorEvent EOnFinished;
        public TerrainChunkEvent EChunkCreated;

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
            System.Diagnostics.Stopwatch roadSw = new();
            roadSw.Start();
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

            Debug.Log("Generated road , took: " + roadSw.Elapsed.TotalMilliseconds + " ms");
            return _noise;
        }
        float[,] CreateValleyAroundRoad(float _ofstX, float _ofstY, float[,] _noise)
        {
            logs = 0;
            System.Diagnostics.Stopwatch valleySw = new();
            valleySw.Start();
            float[,] valleyNoise = NoiseGenerator.GenerateLongitudinalSinNoise(VertsPerSide() + 2, VertsPerSide() + 2, ValleyConfig, _ofstX, _ofstY, ValleyPerlinConfig, RoadVerticalPerlinConfig);
            float[,] verticality = NoiseGenerator.GenerateSingleAxisNoiseMap(RoadVerticalPerlinConfig, VertsPerSide() + 2, VertsPerSide() + 2, _offsetX, _offsetY);
            for (int i = 0; i < valleyNoise.GetLength(1); i++)
            {
                for (int j = 0; j < valleyNoise.GetLength(0); j++)
                {
                    // float vert = verticality[0,i];
                    // vert += ValleyNoiseExtrusion;
                    // vert = Mathf.Clamp01(vert);




                    _noise[i, j] = Mathf.Lerp(_noise[i, j], ValleyNoiseExtrusion, Mathf.Clamp01((1 - valleyNoise[i, j])) * ValleyNoiseBlend);

                    if (logs < maxLogs && _noise[i, j] > 5)
                    {
                        logs++;
                        Debug.Log(_noise[i, j]);

                    }
                }
            }



            Debug.Log("Generated valley , took: " + valleySw.Elapsed.TotalMilliseconds + " ms");
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

            System.Diagnostics.Stopwatch masterSw = new();
            masterSw.Start();
            System.Diagnostics.Stopwatch sw = new();

            List<MapData> MapDatas = new();
            List<Color[]> colorMaps = new();
            List<float[,]> noises = new();





            sw.Start();
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
            sw.Stop();
            Debug.Log("Generated terrain base shape, took: " + sw.Elapsed.TotalMilliseconds + " ms");
            if (Normalize)
            {
                sw.Start();
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
                sw.Stop();
                Debug.Log("Normalized terrain, took: " + sw.Elapsed.TotalMilliseconds + " ms");
            }


            sw.Start();
            for (int y = 0; y < m_neighboursY; y++)
            {
                for (int x = 0; x < m_neighboursX; x++)
                {
                    Vector2 pos = new(x - (m_neighboursX - 1) / 2, y - (m_neighboursY - 1) / 2);
                    int index = x + y * m_neighboursX;
                    MapData mapdata = MapDatas[index];
                    Texture tex = TextureGenerator.TextureFromMap(mapdata.colormap, VertsPerSide() + 2, VertsPerSide() + 2);
                    TerrainChunk chunk = new TerrainChunk(m_chunkSize, mapdata.GetHeightMap(), mapdata.GetColorMap(), _heightScale, _heightCurve, pos, _terrainMat, tex, m_chunksParent, DefaultLOD);
                    // chunk.SetMesh(MeshGenerator.GenerateMeshFromHeightMap(mapdata.GetHeightMap(), _heightScale, _heightCurve, DefaultLOD).mesh);
                    terrainChunks.Add(pos, chunk);
                    chunk.SetVisibility(true);
                }
            }
            sw.Stop();
            Debug.Log("Generated terrain mesh, took: " + sw.Elapsed.TotalMilliseconds + " ms");

            // Texture tex = TextureGenerator.TextureFromMap(mapdata.colormap, VertsPerSide(), VertsPerSide());
            // TerrainChunk terrain = new TerrainChunk(m_chunkSize, new HeightMap(m_chunkSize, m_chunkSize, mapdata.GetHeightMap().Values), mapdata.GetColorMap(), _heightScale, _heightCurve, Vector2.zero, _terrainMat, tex, transform);


            // terrain.SetMesh(MeshGenerator.GenerateMeshFromHeightMap(mapdata.GetHeightMap(), _heightScale, _heightCurve).mesh);
            // terrain.SetVisibility(true);
            //disabled features for now
            //_featureGenerator.GenerateFeatures(NoiseGenerator.GenerateNoiseMap(_seed+1, VertsPerSide(), VertsPerSide(), _noiseScale, _octaves, _persistance, _lacunarity, _offsetX, _offsetY), _heightScale, _heightCurve, mapdata.GetHeightMap());



            masterSw.Stop();


            Debug.Log("Finished generating terrain, took: " + masterSw.Elapsed.TotalMilliseconds + " ms");
            Debug.Log("Finished generating terrain, took: " + masterSw.Elapsed.TotalSeconds + " s");


            EOnFinished?.Invoke();
        }






        private void GenerateEndlessTerrain()
        {

            for (int i = 0; i < terrainChunksVisibleLastFrame.Count; i++)
            {
                terrainChunksVisibleLastFrame[i].SetVisibility(false);
            }
            terrainChunksVisibleLastFrame.Clear();

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
                            terrainChunksVisibleLastFrame.Add(terrainChunks[viewedChunkCoord]);
                        }
                    }
                    else
                    {
                    
                        float ofstX = _offsetX + viewedChunkCoord.x * m_chunkSize;
                        float ofstY = _offsetY + viewedChunkCoord.y * m_chunkSize;
                        float[,] no = NoiseGenerator.GenerateNoiseMap(PerlinConfig, VertsPerSide() + 2, VertsPerSide() + 2, ofstX, ofstY);
                        no = AddRoadNoise(ofstX, ofstY, no);
                        no = CreateValleyAroundRoad(ofstX, ofstY, no);
                        no = NoiseGenerator.NormalizeGlobally(no, VertsPerSide() + 2, VertsPerSide() + 2, PerlinConfig.standardMaxValue + ValleyNoiseExtrusion);
                        HeightMap hm = HeightMap.FromNoise(no, 1);
                        MapData mapdata = new(hm, ColorMapFromHeight(hm), VertsPerSide() + 2, VertsPerSide() + 2);
                        Texture tex = TextureGenerator.TextureFromMap(mapdata.colormap, VertsPerSide() + 2, VertsPerSide() + 2);

                        TerrainChunk chunk = new TerrainChunk(m_chunkSize, mapdata.GetHeightMap(), mapdata.GetColorMap(), _heightScale, _heightCurve, viewedChunkCoord, _terrainMat, tex, m_chunksParent, DefaultLOD);
                        terrainChunks.Add(viewedChunkCoord, chunk);

                        EChunkCreated?.Invoke(chunk);
                    }
                }
            }



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
            if (Mode == TerrainMode.Static)
            {
                GenerateTerrain();

            }
        }
        private void Update()
        {
            playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);
            if (Mode == TerrainMode.Endless)
            {
                GenerateEndlessTerrain();
            }
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

            public TerrainChunk(int _size, HeightMap _heightMap, Color[] _colorMap, float _heightScale, AnimationCurve _heightCurve, Vector2 _coord, Material _mat, Texture _tex, Transform _parent, int _defaultLOD)
            {
                position = _coord * _size;
                bounds = new Bounds(position, Vector2.one * _size);
                Vector3 positionV3 = new Vector3(position.x, 0, position.y);

                chunkObj = new GameObject("Chunk");
                meshRenderer = chunkObj.AddComponent<MeshRenderer>();
                meshFilter = chunkObj.AddComponent<MeshFilter>();
                meshCollider = chunkObj.AddComponent<MeshCollider>();
                meshRenderer.material = _mat;
                // meshRenderer.material.mainTexture = _tex;
                chunkObj.transform.position = positionV3;
                // chunkObj.transform.localScale = Vector3.one * _size / 10f;
                chunkObj.transform.parent = _parent;
                chunkObj.transform.gameObject.layer = _parent.gameObject.layer;

                m_heightMap = _heightMap;
                m_colorMap = _colorMap;
                meshFilter.mesh = MeshGenerator.GenerateMeshFromHeightMap(m_heightMap, _heightScale, _heightCurve, _defaultLOD).mesh;
                meshCollider.sharedMesh = meshFilter.mesh;
                SetVisibility(false);


                // generate LODS
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