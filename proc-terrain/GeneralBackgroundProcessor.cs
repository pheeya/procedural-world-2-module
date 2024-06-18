using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
namespace ProcWorld
{
    public class GeneralBackgroundProcessor
    {
        int m_timeStepMS = 20;
        public static GeneralBackgroundProcessor instance { get; private set; } = new();


        static int m_numThreads = 1;
        List<GeneralBackgroundThread> m_threads = new();


        int m_nextThread = 0;
        public GeneralBackgroundThread GetNextProcessor()
        {

            // round robin
            // give tasks to processors in a sequence one by one
            // probably not ideal but good enough
            // could choose processors that are free instead or have fewer tasks remaining.
            GeneralBackgroundThread pr = m_threads[m_nextThread];
            m_nextThread++;
            m_nextThread %= m_threads.Count;

            return pr;

        }
        public void Init()
        {
            m_threads = new(m_numThreads);

            for (int i = 0; i < m_numThreads; i++)
            {
                m_threads.Add(new(m_timeStepMS));
            }
        }


        // Enqueue an action to execute on the main thread
        public void Enqueue(Action action)
        {
            Debug.Log("enque");
            GetNextProcessor().Enqueue(action);
        }

        // Get the instance of GeneralBackgroundProcessor

    }
}