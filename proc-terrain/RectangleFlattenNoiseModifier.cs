using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class RectangleFlattenNoiseModifier : MonoBehaviour
    {

        [SerializeField] bool m_useTransform;
        [SerializeField] NoiseModifier m_modifier;
        [SerializeField, Range(0, 1)] float m_flattenedNormalizedHeight;
        [SerializeField, Range(0, 1)] float m_flattenBlend;
        [SerializeField] Vector2 m_flattenArea;
        [SerializeField] Vector2 m_localPositionToTerrain;
        [SerializeField] AnimationCurve m_flattenAreaFalloff;
        [SerializeField] float m_flattenAreaFalloffSize;


        Vector3 m_staticPos;
        Vector3 m_chunkParentPos;

        Vector3 m_staticScale;

        bool m_applicationPlaying = false;
        void Awake()
        {
            m_applicationPlaying = true;
            m_staticPos = transform.position;
            m_staticScale = transform.lossyScale;
            m_chunkParentPos = TerrainGenerator.Instance.GetTerrainChunksParent().position;

            m_modifier.SetFunction(Flatten, CreateBoundsRelative());


   
        }

        void FixedUpdate()
        {
            if (m_useTransform)
            {
                m_staticPos = transform.position;
                m_staticScale = transform.lossyScale;
                m_chunkParentPos = TerrainGenerator.Instance.GetTerrainChunksParent().position;
            }
        }
        void Flatten(float[,] _original, float _offsetX, float _offsetY)
        {
            NoiseGenerator.FlattenRectangle(
                 _original,
                 m_flattenedNormalizedHeight,
                 m_flattenBlend,
                GetRelativePosV2YInverted(),
                GetDimensionsWithFalloffVec2(),
                 m_flattenAreaFalloff,
                 m_flattenAreaFalloffSize,
                 _offsetX, _offsetY);
        }


        Vector3 GetRelativePos()
        {
            Vector3 pos = m_staticPos - m_chunkParentPos;
            if (!m_applicationPlaying)
            {
                if (TerrainGenerator.Instance == null)
                {
                    pos = transform.position;
                }
                else
                {
                    pos = transform.position - TerrainGenerator.Instance.GetTerrainChunksParent().position;
                }
            }
            pos.y = 0;
            if (!m_useTransform)
            {
                pos.x = m_localPositionToTerrain.x;
                pos.z = m_localPositionToTerrain.y * -1;
            }
            return pos;
        }



        Vector2 GetRelativePosV2YInverted()
        {
            Vector3 vec3 = GetRelativePos();
            Vector2 vec2;
            vec2.x = vec3.x;
            vec2.y = vec3.z * -1;
            return vec2;
        }
        Vector2 GetRelativePosV2()
        {
            Vector3 vec3 = GetRelativePos();
            Vector2 vec2;
            vec2.x = vec3.x;
            vec2.y = vec3.z;
            return vec2;
        }

        public Vector3 GetDimensions()
        {
            Vector3 size = m_staticScale;
            if (!m_applicationPlaying)
            {
                size = transform.lossyScale;
            }
            size.y = 50;


            if (m_useTransform)
            {
                return size;
            }
            size.x = m_flattenArea.x;
            size.z = m_flattenArea.y;

            return size;

        }
        public Vector3 GetDimensionsWithFalloff()
        {
            Vector3 dimensions = GetDimensions();

            dimensions.x += m_flattenAreaFalloffSize;
            dimensions.z += m_flattenAreaFalloffSize;

            return dimensions;
        }
        public Vector2 GetDimensionsWithFalloffVec2()
        {
            Vector3 dimensionsVec3 = GetDimensions();
            Vector2 dimensions;
            dimensions.x = dimensionsVec3.x;
            dimensions.y = dimensionsVec3.z;

            dimensions.x += m_flattenAreaFalloffSize;
            dimensions.y += m_flattenAreaFalloffSize;

            return dimensions;
        }

        public Vector3 GetWorldPos()
        {
            if (!m_applicationPlaying)
            {
                if(TerrainGenerator.Instance == null){
                    return GetRelativePos();
                }
                return GetRelativePos() + TerrainGenerator.Instance.GetTerrainChunksParent().position;
            }
            return GetRelativePos() + m_chunkParentPos;
        }

        BoundsVec2 CreateBoundsRelative()
        {

            BoundsVec2 bounds;
            bounds.center = GetRelativePosV2();
            bounds.size = GetDimensionsWithFalloffVec2();
            return bounds;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;

            Gizmos.DrawWireCube(GetWorldPos(), GetDimensions());


            Gizmos.color = Color.gray;
            Gizmos.DrawWireCube(GetWorldPos(), GetDimensionsWithFalloff());

        }
    }

}