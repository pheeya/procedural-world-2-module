using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace ProcWorld
{
    public class TerrainBackgroundProcessor
    {


        int m_timeStepMs;
        Thread m_main;
        // Start is called before the first frame update






        public TerrainBackgroundProcessor(int _ts)
        {
            m_timeStepMs = _ts;
            m_main = new Thread(new ThreadStart(() => Loop()));
            m_main.Name = "Terrain Thread";
            m_main.IsBackground = true;
            m_main.Start();

        }

        bool exit = false;


        float m_lastUpdate = -9999;

        float elapsedMs = 0;
        float dt;

        System.Diagnostics.Stopwatch sw = new();



        Queue<TerrainChunk> Queue = new();
        Queue<TerrainChunk> PhysicsQueue = new();
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

        public void EnqueueChunk(TerrainChunk _chunk)
        {
            Profiler.BeginSample("Enqueue Chunk");
            lock (Queue)
            {
                Profiler.BeginSample("Enqueue locked");
                Queue.Enqueue(_chunk);
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }
        public void EnqueuePhysics(TerrainChunk _chunk)
        {
            lock (PhysicsQueue)
            {
                PhysicsQueue.Enqueue(_chunk);
            }
        }

        public bool Processing { get; private set; }
        void Process()
        {
            Profiler.BeginThreadProfiling("Terrain Background Thread", m_main.Name);
            Processing = false;
            while (Queue.Count > 0)
            {
                Processing = true;
                TerrainChunk chunk = Queue.Dequeue();
                chunk.Regenerate();
            }

            while (PhysicsQueue.Count > 0)
            {
                Processing = true;
                TerrainChunk chunk = PhysicsQueue.Dequeue();
                chunk.BakeMeshForCollision();
            }

            Profiler.EndThreadProfiling();

            // lock (PhysicsQueue)
            // {
            //     while (PhysicsQueue.Count > 0)
            //     {
            //         TerrainChunk chunk = Queue.Dequeue();
            //         chunk.BakeMeshForCollision();
            //     }
            // }
        }
        public void Cleanup()
        {
            exit = true;
            if (m_main != null && m_main.IsAlive)
            {
                Queue.Clear();
                PhysicsQueue.Clear();
            Debug.Log("Cleaning up terrain background processors");
                m_main.Join(); // Wait for the thread to finish
            }
        }
      

        // Update is called once per frame
        void Update()
        {

        }
    }

}