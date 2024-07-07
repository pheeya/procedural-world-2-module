using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ProcWorld
{
    public class GeneralBackgroundThread
    {


        int m_timeStepMs;
        Thread m_main;
        // Start is called before the first frame update



        private readonly Queue<Action> actionQueue = new Queue<Action>();

        public GeneralBackgroundThread(int _ts)
        {
            m_timeStepMs = _ts;
            m_main = new Thread(new ThreadStart(() => Loop()));
            m_main.Name = "General Background Thread";
            m_main.IsBackground = true;
            m_main.Start();

        }

        bool exit = false;


        float m_lastUpdate = -9999;

        float elapsedMs = 0;
        float dt;

        System.Diagnostics.Stopwatch sw = new();



        void Loop()
        {
            // careful when passing values to main thread: 
            // https://www.albahari.com/threading/#_Passing_Data_to_a_Thread
            // see "Passing Data to a Thread" section


            sw.Start();

            while (!exit)
            {

                // is a timestep even required?

                elapsedMs = sw.ElapsedMilliseconds;
                dt = elapsedMs / 1000f;
                if (elapsedMs < m_timeStepMs) continue;
                Process();
                sw.Restart();
            }
            sw.Stop();
        }

        public void Enqueue(Action _action)
        {
            lock (actionQueue)
            {
                actionQueue.Enqueue(_action);
            }
        }


        public bool Processing { get; private set; }
        void Process()
        {
            Processing = false;
            while (actionQueue.Count > 0)
            {
                Processing = true;
                Action ac = actionQueue.Dequeue();
                ac();
            }



        }
        public void Cleanup()
        {
            exit = true;
            if (m_main != null && m_main.IsAlive)
            {

                actionQueue.Clear();
                Debug.Log("Cleaning up general background processors");
                m_main.Join(); // Wait for the thread to finish
            }
        }


    }

}