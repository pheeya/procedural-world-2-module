using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{


    [Header("Components")]
    public FeatureGenerator _featureGenerator;
    public Material _terrainMat;
    public AnimationCurve _heightCurve;
    public Transform _player;
    private static Vector2 playerPos;

    [Header("Terrain Config")]
    public float _heightScale;
    [field: SerializeField, Range(0,6)] public int DefaultLOD { get; private set; }
    [field: SerializeField] public bool Normalize { get; private set; }
    public static int _drawDistance = 480;
    public TerrainType[] terrainTypes;

    [SerializeField, Range(1, 250)] int m_chunkSize;
    [SerializeField] int m_neighboursX;
    [SerializeField] int m_neighboursY;
    [Header("Noise Config")]
    [Range(0.001f, 100)]
    public float _noiseScale;
    private static int maxChunksVisible;

    [Range(1, 5)]
    public int _octaves = 1;
    [Range(0.01f, 1)]
    public float _persistance;


    [Range(1, 20)]
    public float _lacunarity;

    public int _seed;

    public float _offsetX, _offsetY;



    Dictionary<Vector2, TerrainChunk> terrainChunks = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> terrainChunksVisibleLastFrame = new List<TerrainChunk>();


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
        float[,] noise = NoiseGenerator.GenerateNoiseMap(_seed, VertsPerSide(), VertsPerSide(), _noiseScale, _octaves, _persistance, _lacunarity, _ofX, _ofY);
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

                float[,] no = NoiseGenerator.GenerateNoiseMap(_seed, VertsPerSide() + 2, VertsPerSide() + 2, _noiseScale, _octaves, _persistance, _lacunarity, _offsetX + (m_chunkSize * x), _offsetY + (m_chunkSize * y));
                if (!Normalize)
                {
                    HeightMap hm = HeightMap.FromNoise(no, 1);
                    MapData md = new(hm, ColorMapFromHeight(hm), VertsPerSide() + 2, VertsPerSide() + 2);
                    MapDatas.Add(md);
                }
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
                    int index = x + y * m_neighboursY;
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
                Vector2 pos = new(x, y);
                int index = x + y * m_neighboursY;
                MapData mapdata = MapDatas[index];
                Texture tex = TextureGenerator.TextureFromMap(mapdata.colormap, VertsPerSide() + 2, VertsPerSide() + 2);
                TerrainChunk chunk = new TerrainChunk(m_chunkSize, mapdata.GetHeightMap(), mapdata.GetColorMap(), _heightScale, _heightCurve, pos, _terrainMat, tex, transform, DefaultLOD);
                chunk.SetMesh(MeshGenerator.GenerateMeshFromHeightMap(mapdata.GetHeightMap(), _heightScale, _heightCurve, DefaultLOD).mesh);
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
    }

    // public TerrainChunk CreateTerrain(ColorMap _heightMap, ColorMap _colorMap)
    // {

    // }

    public MeshData GenerateTerrainMeshData()
    {
        MapData mapdata = GenerateMapData(_offsetX, _offsetY);
        List<float[,]> heights = new();
        heights.Add(mapdata.GetHeightMap().Values);
        if (Normalize)
        {
            mapdata.OverrideHeightMap(HeightMap.FromNoise(NoiseGenerator.Normalize(heights, mapdata.height, mapdata.width)[0], 0));
        }

        return MeshGenerator.GenerateMeshFromHeightMap(mapdata.GetHeightMap(), _heightScale, _heightCurve, DefaultLOD);
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

                    MapData mapdata = GenerateMapData(_offsetX + viewedChunkCoord.x * m_chunkSize, _offsetY + viewedChunkCoord.y * m_chunkSize);
                    Texture tex = TextureGenerator.TextureFromMap(mapdata.colormap, VertsPerSide(), VertsPerSide());
                    TerrainChunk chunk = new TerrainChunk(m_chunkSize, mapdata.GetHeightMap(), mapdata.GetColorMap(), _heightScale, _heightCurve, viewedChunkCoord, _terrainMat, tex, transform, DefaultLOD);
                    chunk.SetMesh(MeshGenerator.GenerateMeshFromHeightMap(mapdata.GetHeightMap(), _heightScale, _heightCurve, DefaultLOD).mesh);
                    terrainChunks.Add(viewedChunkCoord, chunk);
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

    private void Awake()
    {
        maxChunksVisible = Mathf.RoundToInt(_drawDistance / m_chunkSize);
        GenerateTerrain();

    }
    private void Update()
    {
        playerPos = new Vector2(_player.transform.position.x, _player.transform.position.z);
        // GenerateEndlessTerrain();
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

        public TerrainChunk(int _size, HeightMap _heightMap, Color[] _colorMap, float _heightScale, AnimationCurve _heightCurve, Vector2 _coord, Material _mat, Texture _tex, Transform _parent, int _defaultLOD)
        {
            position = _coord * _size;
            bounds = new Bounds(position, Vector2.one * _size);
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            chunkObj = new GameObject("Chunk");
            meshRenderer = chunkObj.AddComponent<MeshRenderer>();
            meshFilter = chunkObj.AddComponent<MeshFilter>();
            meshRenderer.material = _mat;
            meshRenderer.material.mainTexture = _tex;
            chunkObj.transform.position = positionV3;
            // chunkObj.transform.localScale = Vector3.one * _size / 10f;
            chunkObj.transform.parent = _parent;

            m_heightMap = _heightMap;
            m_colorMap = _colorMap;
            meshFilter.mesh = MeshGenerator.GenerateMeshFromHeightMap(m_heightMap, _heightScale, _heightCurve, _defaultLOD).mesh;
            SetVisibility(false);
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