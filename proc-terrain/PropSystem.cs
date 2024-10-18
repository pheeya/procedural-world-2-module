using System.Collections;
using System.Collections.Generic;
using ProcWorld;
using UnityEngine;

public class PropSystem : MonoBehaviour
{
    [Header("The order of prop placers is important if you they are doing collision avoidance with each other. \nIf prop placer X is avoiding collisions with prop placer Y then X needs to be below Y in the list.\nThis is because prop Y needs to update its positions first before prop X can check for collisions.")]
    [Header("For props that update at a different ditance interval but a collision has to be avoided, it is required that both have each other in their collision avoidance list. The one that exists already will take priority.")]
    [Space(10)]
    [SerializeField] List<PropPlacer> m_propPlacers;

    bool m_init = false;
    void Awake()
    {
        TerrainGenerator.Instance.EInitialChunksCreated += Init;

    }
    void Init()
    {
        m_init = true;

        for (int i = 0; i < m_propPlacers.Count; i++)
        {
            m_propPlacers[i].Init();
        }
    }
    void FixedUpdate()
    {
        if (!m_init) return;
        for (int i = 0; i < m_propPlacers.Count; i++)
        {
            m_propPlacers[i].ManualUpdate();
        }

    }
}
