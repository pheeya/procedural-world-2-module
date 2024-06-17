using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace ProcWorld
{

    public class TerrainPropManager : MonoBehaviour
    {
        [SerializeField] Transform m_propsParent;
        [SerializeField] List<PropPlacer> m_placers;
        [SerializeField] int m_numBackgroundThreads;
        [SerializeField] int m_timeStepMS;
        List<TerrainBackgroundProcessor> m_processors = new();

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

        public void EnqueueProcess(Action _process){
            
        }
        public void Init()
        {
            m_processors = new(m_numBackgroundThreads);

            for (int i = 0; i < m_numBackgroundThreads; i++)
            {
                m_processors.Add(new(m_timeStepMS));
            }

            foreach (PropPlacer p in m_placers)
            {
            }
        }

        //   [SerializeField] 
        //   [SerializeField] public List<GameObject> m_pool; 

    }

}