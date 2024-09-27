using System;
using System.Collections;
using System.Collections.Generic;
using ProcWorld;
using UnityEditor;
using UnityEngine;

namespace ProcWorld
{
    public struct PropTransformInfo
    {
        public Vector3 position;
        public Quaternion rotation;
        public bool enabled;
        public Vector2 addedOffset;
        public int variant;
    }
    [System.Serializable]
    public struct PropVariant
    {
        [SerializeField] public GameObject prefab;
        [SerializeField, Range(0, 1)] public float probability;
        public Vector3 offset;
    }

    [System.Serializable]
    public struct PropPool
    {
        public int placed;
        public List<GameObject> objects;

    }
    public class PropPlacer : MonoBehaviour
    {

        public struct DeadZone
        {
            public Vector2 size;
            public Vector2 pos;
        }

        static List<DeadZone> s_deadZones = new();

        [SerializeField] List<PropVariant> m_variants;
        [SerializeField] int m_varianceSeedOffset;
        [SerializeField] bool m_ignoreDeadZone;

        [field: SerializeField] public List<PropPool> Pools { get; private set; }


        [SerializeField] float m_updateDistance;
        [field: SerializeField] public int PoolAmount;
        [field: SerializeField] public int PoolGeneratedAmount;
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
            for (int i = 0; i < Pool.Count; i++)
            {
                DestroyImmediate(Pool[i]);
                Pool.RemoveAt(i);
                i--;
            }
            PoolGeneratedAmount = 0;
            Pools = new(m_variants.Count);
            for (int i = 0; i < m_variants.Count; i++)
            {
                int range = Mathf.FloorToInt((m_variants[i].probability * PoolAmount));
                PropPool pool;
                pool.placed = 0;
                pool.objects = new(range);
                Pools.Add(pool);
            }

            for (int i = 0; i < m_variants.Count; i++)
            {

                int range = Mathf.FloorToInt((m_variants[i].probability * PoolAmount));
                for (int j = 0; j < range; j++)
                {
#if UNITY_EDITOR
                    GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(m_variants[i].prefab);

#else
                    GameObject obj = (GameObject)GameObject.Instantiate(m_variants[i].prefab);
#endif
                    obj.transform.parent = transform;
                    Pools[i].objects.Add(obj);
                    Pool.Add(obj);
                    obj.SetActive(false);
                    PoolGeneratedAmount++;
                }
            }
        }
        public delegate List<PropTransformInfo> PropPlacementFunction();
        PropPlacementFunction Function;

        public void SetFunction(PropPlacementFunction _a)
        {
            Function = _a;
            UpdatePlacement();
        }

        public int ChooseRandomWithProbability(int prob)
        {

            // give each variant a "band" in the 1-100 range based on its probability
            // for example if two variants have 30, 30 probability 
            // then 0-30 is the first variant's band, and 30-60 is the second's 
            // then select based on which band the prob value falls in


            int cumilativeStartingRange = 0;
            int index = 0;
            bool found = false;
            for (int i = 0; i < m_variants.Count; i++)
            {

                int start = cumilativeStartingRange;
                int end = start + Mathf.RoundToInt(m_variants[i].probability * 100);

                if (prob >= start && prob < end && (Pools[i].placed < Pools[i].objects.Count))
                {
                    found = true;
                    index = i;
                    break;
                }
                cumilativeStartingRange += Mathf.RoundToInt(m_variants[i].probability * 100);
            }
            if (!found)
            {
                index = -1;
            }

            return index;
        }

        public int GetRandomVariant(PropTransformInfo _data)
        {
            Vector3 pos = _data.position;
            //+ pos.y * 12
            // not adding pos.y in the LCG Random equation anymore
            // because when we add offset in the prop placement function, we cause a difference in the y value as well which comes from
            // the offsets causing a change in elevation. Figure it out if we ever need to have Y offset in the functions
            int random = util.LCGRandom(Mathf.RoundToInt((pos.x - _data.addedOffset.x) * 23 + (pos.z - _data.addedOffset.y) + 25) + m_varianceSeedOffset, 0, 100);


            int chosen = ChooseRandomWithProbability(random);
            if (chosen == -1)
            {
                for (int j = 0; j < m_variants.Count; j++)
                {

                    // if no pool found with correct probability with stock
                    // choose any with stock left
                    if (Pools[j].placed < Pools[j].objects.Count)
                    {
                        chosen = j;
                        break;
                    }
                }
            }
            return chosen;
        }
        void callback(List<PropTransformInfo> _data)
        {

            for (int i = 0; i < Pools.Count; i++)
            {

                PropPool pool = Pools[i];
                pool.placed = 0;
                Pools[i] = pool;
            }
            for (int i = 0; i < _data.Count; i++)
            {
                if (!_data[i].enabled)
                {

                    continue;

                }



                int chosen = _data[i].variant;

                PropPool pool = Pools[chosen];

                GameObject obj = pool.objects[pool.placed];
                obj.transform.localPosition = _data[i].position + m_variants[chosen].offset;
                obj.transform.localRotation = _data[i].rotation;

                obj.SetActive(_data[i].enabled);

                pool.placed++;
                Pools[chosen] = pool;
            }

            for (int i = 0; i < Pools.Count; i++)
            {
                for (int j = Pools[i].placed; j < Pools[i].objects.Count; j++)
                {
                    Pools[i].objects[j].SetActive(false);
                }
            }
        }

        public static void AddDeadZone(Vector2 _span, Vector2 _pos)
        {
            DeadZone dz;
            dz.size = _span;
            dz.pos = _pos;
            // lock while removing or adding so that the main thread isn't blocked when adding/removing
            // it's okay if main thread is adding or removing a dead zone and causing background threads to wait
            lock (s_deadZones)
            {
                if (s_deadZones.Contains(dz))
                {
                    Debug.Log("Identical dead zone already exists, skipping");
                    return;
                }
                s_deadZones.Add(dz);
            }

            Debug.Log("added deadzone");
        }
        public bool IsInDeadZone(Vector3 _point)
        {
            for (int i = 0; i < s_deadZones.Count; i++)
            {
                float boundaryXRight = s_deadZones[i].pos.x + s_deadZones[i].size.x;
                float boundaryXLeft = s_deadZones[i].pos.x - s_deadZones[i].size.x;
                float boundaryYForward = s_deadZones[i].pos.y + s_deadZones[i].size.y;
                float boundaryYBackward = s_deadZones[i].pos.y - s_deadZones[i].size.y;



                if ((_point.x >= boundaryXLeft && _point.x <= boundaryXRight) && (_point.z >= boundaryYBackward && _point.z <= boundaryYForward))
                {
                    return true;
                }
            }
            return false;

        }
        public static void RemoveDeadZone(DeadZone dz)
        {

            // lock while removing or adding so that the main thread isn't blocked when adding/removing
            // it's okay if main thread is adding or removing a dead zone and causing background threads to wait
            lock (s_deadZones)
            {
                if (!s_deadZones.Contains(dz))
                {
                    Debug.Log("Dead zone doesn't exist in list");
                    return;
                }
                s_deadZones.Remove(dz);
            }
        }

        void UpdatePlacement()
        {
            m_lastUpdatePosition = TerrainGenerator.PlayerPosV2;
            GeneralBackgroundProcessor.instance.Enqueue(() =>
            {
                List<PropTransformInfo> data = Function();

                MainThreadDispatcher.Instance.Enqueue(() =>
                {
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