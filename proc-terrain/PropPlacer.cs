using System;
using System.Collections;
using System.Collections.Generic;
using ProcWorld;
using UnityEngine;

namespace ProcWorld
{
    public struct PropTransformInfo
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool enabled;
    }
    public class PropPlacer : MonoBehaviour
    {


        [SerializeField] GameObject m_prefab;
        [SerializeField] float m_updateDistance;
        [field: SerializeField] public int PoolAmount;
        public List<Vector3> Positions { get; private set; }
        public List<Vector3> Rotations { get; private set; }


        [field: SerializeField] public List<GameObject> Pool { get; private set; } = new();
        public delegate void PropPlacerEvent();

        public event PropPlacerEvent ESpawnRequested;


        Vector2 m_lastUpdatePosition;


        void Awake()
        {
            TerrainGenerator.Instance.EInitialChunksCreated += Init;
        }

        public event Action EInit;
        bool m_init = false;
        void Init()
        {
            m_lastUpdatePosition = TerrainGenerator.PlayerPosV2;
            m_init = true;
            EInit?.Invoke();
        }
        public void GeneratePool()
        {
            int count = PoolAmount - Pool.Count;

            for (int i = 0; i < count; i++)
            {
                GameObject obj = GameObject.Instantiate(m_prefab, transform);
                Pool.Add(obj);
                obj.SetActive(false);
            }
        }
        public delegate List<PropTransformInfo> PropPlacementFunction();
        PropPlacementFunction Function;

        public void SetFunction(PropPlacementFunction _a)
        {
            Function = _a;
            UpdatePlacement();
        }
        void callback(List<PropTransformInfo> _data)
        {
            for (int i = 0; i < _data.Count; i++)
            {
                Pool[i].transform.localPosition = _data[i].position;
                Pool[i].transform.localRotation = _data[i].rotation;
                Pool[i].gameObject.SetActive(_data[i].enabled);
            }
        }

        void UpdatePlacement()
        {
            m_lastUpdatePosition = TerrainGenerator.PlayerPosV2;
            List<PropTransformInfo> data = Function();
         
            GeneralBackgroundProcessor.instance.Enqueue(() =>
            {
                List<PropTransformInfo> data = Function();

                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.Log("calling bac");
                    callback(data);
                });
            });
        }
        void FixedUpdate()
        {
            if (!m_init) return;

            Vector3 dist = (TerrainGenerator.PlayerPosV2 - m_lastUpdatePosition);

            if (dist.magnitude >= m_updateDistance)
            {
                UpdatePlacement();
            }


        }

    }

}