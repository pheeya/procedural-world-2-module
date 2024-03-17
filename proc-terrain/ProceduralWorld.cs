using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class ProceduralWorld : MonoBehaviour
    {


        [SerializeField] LayerMask m_terrainHeightSampleLayerMask;
        TerrainGenerator generator;

        bool m_finished = false;

        bool m_terrainFinished = false;

        public delegate void ProceduralWorldEvent();
        public ProceduralWorldEvent EOnCreated;

        [SerializeField] TerrainGenerator m_terrainGen;
        private void Awake()
        {
            FindObjectOfType<DebugTerrain>().transform.gameObject.SetActive(false);

            generator = FindObjectOfType<TerrainGenerator>();


            generator.EOnFinished += OnTerrainFinished;
        }
        void OnTerrainFinished()
        {
            m_terrainFinished = true;

            // for now, only terrain is being generated so consider the whole world generated
            m_finished = true;


            EOnCreated?.Invoke();
        }

        public Vector2 GetMapSize()
        {
            return m_terrainGen.GetFinalTerrainSize();
        }
        public bool GetHeightAtPoint(Vector3 _pos, out float _height)
        {
            _height = 0;
            Vector3 sample = _pos;
            sample.y = m_terrainGen._heightScale + 10f; ;
            bool hit = Physics.Raycast(sample, Vector3.down, out RaycastHit hitInfo, m_terrainGen._heightScale + 20f, m_terrainHeightSampleLayerMask);


            if (!hit)
            {
                return false;
            }

            _height = hitInfo.point.y;
            return true;
        }

        public int GetRoadCenterAtPos(int _yPos)
        {
            return m_terrainGen.GetRoadCenterAtPos(_yPos);
        }
        public Vector2 GetRoadForwardAtPos(int _yPos)
        {
            return m_terrainGen.GetRoadForwardAtPos(_yPos);
        }
    }
}