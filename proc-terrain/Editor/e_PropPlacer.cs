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


            bool generate = GUILayout.Button("Generate Pool");
            if(!generate) return;
            PropPlacer tar = (PropPlacer)target;

            tar.GeneratePool();
            EditorUtility.SetDirty(tar);

        }
    }
}
