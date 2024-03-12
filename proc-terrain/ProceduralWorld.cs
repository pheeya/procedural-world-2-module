using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class ProceduralWorld : MonoBehaviour
    {

        TerrainGenerator generator;

        bool m_finished = false;

        bool m_terrainFinished = false;

        public delegate void ProceduralWorldEvent();
        public ProceduralWorldEvent EOnCreated;
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
    }
}