using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class PropDeadZone : MonoBehaviour
    {

      
        void Start()
        {
            if (PropSystem.Instance.DidInit)
            {
                Add();
            }
            else
            {
                PropSystem.Instance.EOnInit += Add;
            }
        }

        void Add()
        {
            Vector3 size = transform.lossyScale;
            Vector2 sizeVec2;
            sizeVec2.x = size.x;
            sizeVec2.y = size.z;

            Vector3 pos = transform.position - TerrainGenerator.Instance.GetTerrainChunksParent().position;
            float rot = transform.rotation.eulerAngles.y;
            Vector2 posVec2;
            posVec2.x = pos.x;
            posVec2.y = pos.z;
            PropSystem.Instance.AddDeadZone(sizeVec2, posVec2, rot * Mathf.Deg2Rad);
        }
        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(.4f, .1f, .1f, .5f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }



    }

}