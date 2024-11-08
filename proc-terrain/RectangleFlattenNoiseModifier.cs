using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    [RequireComponent(typeof(NoiseModifier))]
    public class RectangleFlattenNoiseModifier : MonoBehaviour
    {

        bool m_useTransform = true;
        [SerializeField] NoiseModifier m_modifier;
        [SerializeField, Range(0, 1)] float m_flattenedNormalizedHeight;
        [SerializeField, Range(0, 1)] float m_flattenBlend;

        // not using anymore, just use transforms man why you confusing yourself
        [SerializeField, HideInInspector] Vector2 m_flattenArea;
        [SerializeField, HideInInspector] Vector2 m_localPositionToTerrain;
        [SerializeField] AnimationCurve m_flattenAreaFalloff;
        [SerializeField] float m_flattenAreaFalloffSize;


        Vector3 m_staticPos;
        Vector3 m_chunkParentPos;

        Vector3 m_staticScale;

        bool m_applicationPlaying = false;


        // do this in start so that this always happens after a scenario has been placed, in case this is part of scenario
        //  this is because the sceneLoaded event is called after Awake but before start so the position of the scene is set before start and after awake

        void Awake()
        {
            TerrainGenerator.Instance.EBeforeChunkGenerationStarted += Init;
        }

        bool m_added = false;
        void Add()
        {
            if (m_added) return;

            m_added = true;
            m_applicationPlaying = true;
            m_staticPos = transform.position;
            m_staticScale = transform.lossyScale;
            m_rotationY = transform.rotation.eulerAngles.y;
            m_chunkParentPos = TerrainGenerator.Instance.GetTerrainChunksParent().position;
            m_modifier.SetFunction(Flatten, CreateBoundsRelative());


        }


        // this is to ensure that any noise modifiers that exist already from scene start as part of the scene
        // are loaded before the chunk generation
        void Init()
        {
            Add();
        }

        // this is for modifiers that are added dynamically at run time
        void Start()
        {
            Add();
        }

        void OnValidate()
        {
            m_modifier = GetComponent<NoiseModifier>();
        }
        float m_rotationY;
        // void FixedUpdate()
        // {
        //     if (m_useTransform)
        //     {
        //         m_rotationY = transform.rotation.eulerAngles.y;
        //         m_staticPos = transform.position;
        //         m_staticScale = transform.lossyScale;
        //         m_chunkParentPos = TerrainGenerator.Instance.GetTerrainChunksParent().position;
        //     }
        // }
        void Flatten(float[,] _original, float _offsetX, float _offsetY)
        {
            NoiseGenerator.FlattenRectangle(
                 _original,
                 m_flattenedNormalizedHeight,
                 m_flattenBlend,
                GetRelativePosV2(),
                Mathf.Deg2Rad * m_rotationY,
                GetDimensionsVec2(),
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
            Vector2 vec2 = GetRelativePosV2();
            vec2.y *= -1;
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


            if (m_useTransform)
            {
                return size;
            }
            size.x = m_flattenArea.x;
            size.z = m_flattenArea.y;

            return size;
        }

        public Vector2 GetDimensionsVec2()
        {
            Vector3 size = GetDimensions();
            Vector2 sz;
            sz.x = size.x;
            sz.y = size.z;

            return sz;
        }
        public Vector3 GetDimensionsWithFalloff()
        {
            Vector3 dimensions = GetDimensions();

            // multiplied by 2 because add fall off size to both sides
            dimensions.x += m_flattenAreaFalloffSize * 2f;
            dimensions.z += m_flattenAreaFalloffSize * 2f;

            return dimensions;
        }
        public Vector2 GetDimensionsWithFalloffVec2()
        {
            Vector3 dimensionsVec3 = GetDimensions();
            Vector2 dimensions;
            dimensions.x = dimensionsVec3.x;
            dimensions.y = dimensionsVec3.z;

            // multiplied by 2 because add fall off size to both sides
            dimensions.x += m_flattenAreaFalloffSize * 2f;
            dimensions.y += m_flattenAreaFalloffSize * 2f;

            return dimensions;
        }

        public Vector3 GetWorldPos()
        {
            if (!m_applicationPlaying)
            {
                if (TerrainGenerator.Instance == null)
                {
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

        void OnDrawGizmos()
        {

            Gizmos.matrix = transform.localToWorldMatrix;


            Gizmos.color = Color.gray;

            Vector3 dimensions = GetDimensionsWithFalloff();
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(dimensions.x / transform.lossyScale.x, dimensions.y / transform.lossyScale.y, dimensions.z / transform.lossyScale.z));

            Gizmos.color = Color.black;

            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        }
    }

}