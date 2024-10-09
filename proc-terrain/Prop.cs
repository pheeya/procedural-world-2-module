using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class Prop : MonoBehaviour
    {
        int m_variant;
        public int GetPropVarient()
        {
            return m_variant;
        }
        public void Init(int _variant)
        {
            m_variant = _variant;
        }
    }

}