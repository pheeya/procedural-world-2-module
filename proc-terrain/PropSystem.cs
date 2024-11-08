using System;
using System.Collections;
using System.Collections.Generic;
using ProcWorld;
using UnityEngine;

namespace ProcWorld
{

    public struct DeadZoneSource
    {
        public Vector2 size;
        public Vector2 pos;
        public float rotation;
    }


    public class PropSystem : MonoBehaviour
    {
        [Header("The order of prop placers is important if you they are doing collision avoidance with each other. \nIf prop placer X is avoiding collisions with prop placer Y then X needs to be below Y in the list.\nThis is because prop Y needs to update its positions first before prop X can check for collisions.")]
        [Header("For props that update at a different ditance interval but a collision has to be avoided, it is required that both have each other in their collision avoidance list. The one that exists already will take priority.")]
        [Space(10)]
        [SerializeField] List<PropPlacer> m_propPlacers;

        public bool DidInit = false;

        static PropSystem _instance;
        public Action EOnInit;
        public static PropSystem Instance
        {
            get
            {
                if (_instance == null) _instance = FindObjectOfType<PropSystem>();
                return _instance;
            }
        }
        void Awake()
        {
            TerrainGenerator.Instance.EInitialChunksCreated += Init;

        }
        void Init()
        {

            DidInit = true;

            EOnInit?.Invoke();



            for (int i = 0; i < m_propPlacers.Count; i++)
            {
                m_propPlacers[i].Init();
            }

        }


        public bool AddedNewDeadzonesLastFrame;
        public List<DeadZoneSource> DeadZones { get; private set; } = new();

        public void AddDeadZone(Vector2 _span, Vector2 _pos, float rot)
        {
            DeadZoneSource dz;
            dz.size = _span;
            dz.pos = _pos;
            dz.rotation = rot;
            // lock while removing or adding so that the main thread isn't blocked when adding/removing
            // it's okay if main thread is adding or removing a dead zone and causing background threads to wait
            lock (DeadZones)
            {
                if (DeadZones.Contains(dz))
                {
                    Debug.Log("Identical dead zone already exists, skipping");
                    return;
                }
                DeadZones.Add(dz);
            }

            AddedNewDeadzonesLastFrame = true;
        }

        public bool IsDirty { get; private set; }
        public void MarkDirty()
        {
            IsDirty = true;
        }
        public void RemoveDeadZone(DeadZoneSource dz)
        {

            // lock while removing or adding so that the main thread isn't blocked when adding/removing
            // it's okay if main thread is adding or removing a dead zone and causing background threads to wait
            lock (DeadZones)
            {
                if (!DeadZones.Contains(dz))
                {
                    Debug.Log("Dead zone doesn't exist in list");
                    return;
                }
                DeadZones.Remove(dz);
            }
        }
        void FixedUpdate()
        {
            if (!DidInit) return;

            for (int i = 0; i < m_propPlacers.Count; i++)
            {
                m_propPlacers[i].ManualUpdate();
            }
            AddedNewDeadzonesLastFrame = false;
            IsDirty = false;
        }
    }

}