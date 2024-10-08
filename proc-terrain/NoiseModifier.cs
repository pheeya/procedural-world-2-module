using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public delegate void NoiseModifierFunction(float[,] original, float _offsetX, float _offsetY);
    public class NoiseModifier : MonoBehaviour
    {


        [field: SerializeField] public bool Disabled { get; private set; }
        NoiseModifierFunction m_func;

        public NoiseModifierFunction GetNoiseModifierFunction() { return m_func; }
        public void SetFunction(NoiseModifierFunction func)
        {
            m_func = func;
        }
    }
}