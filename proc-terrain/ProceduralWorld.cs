using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{

    public struct BoundsVec2
    {
        public Vector2 center;
        public Vector2 size;
    }
    public class ProceduralWorld : MonoBehaviour
    {


        [SerializeField] LayerMask m_terrainHeightSampleLayerMask;
        TerrainGenerator generator;

        bool m_finished = false;

        bool m_terrainFinished = false;

        public delegate void ProceduralWorldEvent();
        public ProceduralWorldEvent EInitialWorldCreated;
        public ProceduralWorldEvent EInitialChunksCreated;

        [SerializeField] TerrainGenerator m_terrainGen;

        [field: SerializeField, Tooltip("Optional, arbitray height that can be used by anything")] public float BaseLineHeight { get; private set; }

        static ProceduralWorld _instance;
        public static ProceduralWorld Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ProceduralWorld>();
                }

                return _instance;
            }
        }
        private void Awake()
        {
            FindObjectOfType<DebugTerrain>().transform.gameObject.SetActive(false);


        }
        void OnInitialChunksCreated()
        {


            EInitialChunksCreated?.Invoke();
            OnWorldCreated();
        }
        void OnWorldCreated()
        {
            m_terrainFinished = true;

            // for now, only terrain is being generated so consider the whole world generated
            m_finished = true;
            Loaded = true;


            EInitialWorldCreated?.Invoke();
        }
        bool m_started = false;

        bool exit = false;
        public void Begin()
        {
            MainThreadDispatcher.Init();

            generator = FindObjectOfType<TerrainGenerator>();
            generator.EInitialChunksCreated += OnInitialChunksCreated;
            m_started = true;

            generator.Init();
        }

        public bool Loaded { get; private set; }

        public void Stop()
        {
            m_started = false;
            generator.Stop();
        }

        public Vector2 GetMapSize()
        {
            return m_terrainGen.GetFinalTerrainSize();
        }
        public bool GetHeightAtPoint(Vector3 _pos, out RaycastHit hitInfo)
        {
            Vector3 sample = _pos;
            sample.y = m_terrainGen.noiseFunction.GetHeightScale() + 10f; ;
            bool hit = Physics.Raycast(sample, Vector3.down, out hitInfo, m_terrainGen.noiseFunction.GetHeightScale() + 20f, m_terrainHeightSampleLayerMask);

            if (!hit)
            {
                Debug.Log("couldn't find height");
                return false;
            }

            return true;
        }

        public float GetNoiseAt(int x, int y)
        {
            return m_terrainGen.GetNoiseAt(x, y);

        }

        public int GetRoadCenterAtPos(int _yPos)
        {
            return m_terrainGen.GetRoadCenterAtPos(_yPos);
        }
        public Vector2 GetRoadForwardAtPos(int _yPos)
        {
            return m_terrainGen.GetRoadForwardAtPos(_yPos);
        }
        public Vector2 GetPointOnRoadWithDistance(int _yFrom, float _dist)
        {
            return m_terrainGen.GetPointOnRoadWithDistance(_yFrom, _dist);
        }
        public Vector3 GetPointOnRoadWithDistance(Vector3 _currentCellPos, float _distance, out Vector3 _forward)
        {
            Vector2 point = GetPointOnRoadWithDistance((int)_currentCellPos.z, _distance);
            Vector3 pos;
            pos.x = point.x;
            pos.y = 0;
            pos.z = point.y;

            Vector2 f = GetRoadForwardAtPos((int)point.y);
            _forward = new(-f.x, 0, f.y);


            return pos;
        }
        public float GetPlayableAreaWidth()
        {
            return m_terrainGen.GetPlayableAreaWidth();
        }
        public List<Collider> PhysicsColliders { get { return m_terrainGen.PhysicsColliders; } }

        void Cleanup()
        {
            generator.Cleanup();
        }

        void OnApplicationQuit()
        {

            if (!exit)
            {
                exit = true;
                Cleanup();
            }

        }
        void OnDisable()
        {
            if (!exit)
            {
                exit = true;
                Cleanup();
            }
        }
        void OnDestroy()
        {

            if (!exit)
            {
                exit = true;
                Cleanup();
            }
        }
    }
}