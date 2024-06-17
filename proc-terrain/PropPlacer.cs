using System.Collections;
using System.Collections.Generic;
using ProcWorld;
using UnityEngine;

public class PropPlacer : MonoBehaviour
{
    [SerializeField] GameObject m_prefab;
    [SerializeField] float m_updateDistance;
    [SerializeField] int m_poolAmount;
    public List<Vector3> Positions { get; private set; }
    public List<Vector3> Rotations { get; private set; }

    public delegate void PropPlacerEvent();

    public event PropPlacerEvent ESpawnRequested;


    Vector2 m_lastUpdatePosition;

struct PropTransformInfo {
    
}
    void Awake()
    {
        TerrainGenerator.Instance.EInitialChunksCreated += Init;
    }
    bool m_init = false;
    void Init()
    {
        m_lastUpdatePosition = TerrainGenerator.PlayerPosV2;
        m_init = true;
    }
    public delegate void Function();

    void FixedUpdate()
    {
        if (!m_init) return;

        Vector3 dist = (TerrainGenerator.PlayerPosV2 - m_lastUpdatePosition);
        if (dist.magnitude >= m_updateDistance)
        {
            m_lastUpdatePosition = TerrainGenerator.PlayerPosV2;

        }


    }
    public void SetPositions(List<Vector3> _pos)
    {

    }
    public void SetRotations(List<Quaternion> _pos)
    {


    }
}
