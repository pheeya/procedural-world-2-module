using System;
using System.Collections;
using System.Collections.Generic;
using ProcWorld;
using UnityEditor;
using UnityEngine;

namespace ProcWorld
{


    [System.Serializable]
    public struct PropTransformInfo
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector2 addedOffset;
        public Prop associatedObject;
        public bool skip;
    }
    [System.Serializable]
    public struct PropVariant
    {
        [SerializeField] public Prop prefab;
        [SerializeField, Range(0, 1)] public float probability;
        public Vector3 offset;
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
        [SerializeField] string m_debugString;


        [SerializeField] float m_updateDistance;
        [field: SerializeField] public int PoolAmount;
        [field: SerializeField] public int PoolGeneratedAmount;
        public List<Vector3> Positions { get; private set; }
        public List<Vector3> Rotations { get; private set; }


        [field: SerializeField] public List<Prop> AllObjects { get; private set; }
        [field: SerializeField] public List<Prop> Pool { get; private set; }
        [field: SerializeField] public List<Prop> Placed { get; private set; } = new();
        [SerializeField] List<PropPlacer> m_avoidCollisionsWith;
        public delegate void PropPlacerEvent();

        public event PropPlacerEvent ESpawnRequested;



        Vector2 m_lastUpdatePosition;



        [SerializeField] List<PropTransformInfo> m_lastData;


        public event Action EInit;
        bool m_init = false;


        void Awake()
        {
            s_deadZones = new();
        }
        public void Init()
        {

            GeneratePool();
            m_lastUpdatePosition = TerrainGenerator.PlayerPosV2;
            m_init = true;

            EInit?.Invoke();
        }

        public void GeneratePool()
        {

            Pool = new(PoolAmount);
            AllObjects = new(PoolAmount);
            m_lastData = new(PoolAmount);

            PoolGeneratedAmount = 0;
            for (int i = 0; i < m_variants.Count; i++)
            {

                int range = Mathf.FloorToInt((m_variants[i].probability * PoolAmount));
                for (int j = 0; j < range; j++)
                {
                    Prop obj = Instantiate(m_variants[i].prefab);
                    obj.Init(i);
                    obj.transform.parent = transform;
                    Pool.Add(obj);
                    AllObjects.Add(obj);
                    obj.gameObject.SetActive(false);
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


        public int GetRandomObjectIndex(PropTransformInfo _data)
        {
            Vector3 pos = _data.position;

            if (Pool.Count == 1) return 0;
            //+ pos.y * 12
            // not adding pos.y in the LCG Random equation anymore
            // because when we add offset in the prop placement function, we cause a difference in the y value as well which comes from
            // the offsets causing a change in elevation. Figure it out if we ever need to have Y offset in the functions
            int random = util.LCGRandom(Mathf.RoundToInt((pos.x - _data.addedOffset.x) * 23 + (pos.z - _data.addedOffset.y) + 25) + m_varianceSeedOffset, 0, Pool.Count - 1);

            int chosen = random;
            return chosen;
        }


        void callback(List<PropTransformInfo> _data)
        {

            for (int i = 0; i < _data.Count; i++)
            {
                if (_data[i].skip)
                {

                    continue;

                }


                int chosen = _data[i].associatedObject.GetPropVarient();


                Prop obj = _data[i].associatedObject;
                obj.transform.localPosition = _data[i].position + m_variants[chosen].offset;
                obj.transform.localRotation = _data[i].rotation;

                obj.gameObject.SetActive(true);





            }

            for (int i = 0; i < Pool.Count; i++)
            {
                Pool[i].gameObject.SetActive(false);
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


        bool DataContainsPosition(List<PropTransformInfo> info, Vector3 pos, out int _dataIndex)
        {
            _dataIndex = 0;
            for (int i = 0; i < info.Count; i++)
            {
                if (info[i].position == pos)
                {
                    _dataIndex = i;
                    return true;
                };
            }
            return false;
        }

        public bool CurrentlyHasPropAtPosition(Vector3 _pos)
        {
            return DataContainsPosition(m_lastData, _pos, out int x);
        }

        public int GetLastDataCount() { return m_lastData.Count; }

        void UpdatePlacement()
        {
            m_lastUpdatePosition = TerrainGenerator.PlayerPosV2;
            GeneralBackgroundProcessor.instance.Enqueue(() =>
            {



                List<PropTransformInfo> data = Function();


                for (int i = 0; i < data.Count; i++)
                {
                    for (int j = 0; j < m_avoidCollisionsWith.Count; j++)
                    {


                        // lock so that the propplacer at J doesn't add to the list while we reading it
                        lock (m_avoidCollisionsWith[j])
                        {
                            if (m_avoidCollisionsWith[j].CurrentlyHasPropAtPosition(data[i].position))
                            {
                                data.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }

                for (int i = 0; i < m_lastData.Count; i++)
                {
                    PropTransformInfo info = m_lastData[i];
                    info.skip = false;
                    m_lastData[i] = info;
                }

                Pool.Clear();
                Placed.Clear();

                for (int i = 0; i < AllObjects.Count; i++)
                {
                    Pool.Add(AllObjects[i]);
                }

                for (int j = 0; j < m_lastData.Count; j++)
                {
                    if (DataContainsPosition(data, m_lastData[j].position, out int index))
                    {
                        PropTransformInfo info = m_lastData[j];
                        info.skip = true;
                        data[index] = info;
                        Placed.Add(data[index].associatedObject);
                        Pool.Remove(data[index].associatedObject);
                    }
                    // else
                    // {
                    //     Prop go = m_lastData[j].associatedObject;
                    //     Pool.Add(go);
                    // }
                }



                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].skip)
                    {
                        continue;
                    }


                    PropTransformInfo info = data[i];

                    int index = GetRandomObjectIndex(data[i]);


                    info.associatedObject = Pool[index];

                    Placed.Add(info.associatedObject);
                    Pool.Remove(info.associatedObject);
                    data[i] = info;
                }



                // dont do m_lastdata = data;
                // these are lists, ref types
                m_lastData.Clear();
                for (int i = 0; i < data.Count; i++)
                {
                    m_lastData.Add(data[i]);

                }





                MainThreadDispatcher.Instance.Enqueue(() =>
                {
                    callback(data);
                });
            });
        }


        public void ManualUpdate()
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