using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace ProcWorld
{
    [CustomEditor(typeof(PropPlacer))]
    public class e_PropPlacer : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

// no longer generating pool in editor, doing it at run time upon chunks creation

        }
    }
}
