using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScenarioScene : MonoBehaviour
{
    [SerializeField] Transform m_enableOnPlaced;
    public void OnPlaced()
    {
        if(m_enableOnPlaced == null) return;
        m_enableOnPlaced.gameObject.SetActive(true);

    }


}
