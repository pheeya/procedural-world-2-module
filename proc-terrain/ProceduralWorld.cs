using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class ProceduralWorld : MonoBehaviour
    {
        private void Awake()
        {
            FindObjectOfType<DebugTerrain>().transform.gameObject.SetActive(false);
        }
    }
}