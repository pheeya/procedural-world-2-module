using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProcWorld
{
    public class SimplePropPlacerFunction : MonoBehaviour
    {


        [SerializeField] bool m_randomizeYRotation;
        [SerializeField] Vector2 m_offset;
        [SerializeField] float m_forbiddenBandWidth;
        [SerializeField] int m_spanX;
        [SerializeField] int m_spanY;

        [SerializeField] int m_spacingX;
        [SerializeField] int m_spacingY;
        [SerializeField, Range(0, 1)] float m_perlinNoiseSpawnThreshold;
        [SerializeField] PropPlacer m_placer;
        [SerializeField] bool m_usePerlin;
        [SerializeField] int m_nonPerlinSeed;
        [SerializeField, Range(0, 100)] int m_nonPerlinSpawnPossibility;
        [SerializeField] PerlinNoiseConfig m_perlinConfig;



        List<PropTransformInfo> data;
        void Awake()
        {
            m_placer.EInit += Init;
        }
        List<PropTransformInfo> Process()
        {

            Vector2 origin = TerrainGenerator.PlayerPosV2;
            int originIntx = Mathf.RoundToInt(origin.x) / m_spacingX;
            int originInty = Mathf.RoundToInt(origin.y) / m_spacingY;

            originIntx *= m_spacingX;
            originInty *= m_spacingY;

            Vector2[] offsets = NoiseGenerator.GetOctaveOffsets(m_perlinConfig, originIntx, -originInty);



            int i = 0;


            for (int y = -m_spanY; y <= m_spanY; y += m_spacingY)
            {

                for (int x = -m_spanX; x <= m_spanX; x += m_spacingX)
                {
                    if (i >= data.Count) return data;


                    int xVal, yVal;
                    xVal = x + (int)m_offset.x;
                    yVal = y + (int)m_offset.y;

                    float noise;
                    if (m_usePerlin)
                    {
                        noise = NoiseGenerator.GetPerlinValue(m_perlinConfig, x,y, offsets, 0, 0);
                        // conver to 0 to 1, GetPerlinValue gives -1 to 1
                        noise += 1;
                        noise /= 2;
                    }
                    else
                    {
                        noise = util.LCGRandom(Mathf.RoundToInt((originIntx + x) * 23 + (originInty + y) * 15) + m_nonPerlinSeed, 0, 100);


                        if (noise <= m_nonPerlinSpawnPossibility)
                        {
                            noise = 1;
                        }
                        else
                        {
                            noise = 0;
                        }
                    }



                    PropTransformInfo info = data[i];



                    if (noise > m_perlinNoiseSpawnThreshold && (Mathf.Abs(originIntx + xVal) > m_forbiddenBandWidth))
                    {


                        info.enabled = true;
                        info.position = new(originIntx + xVal, TerrainGenerator.Instance.GetScaledNoiseAt(originIntx + xVal, originInty + yVal), originInty + yVal);
                        if (m_randomizeYRotation)
                        {
                            info.rotation = Quaternion.Euler(0, noise * 1360, 0); // can't use unity's Random.range in separate thread, use any random thing as rotation
                        }


                        data[i] = info;

                        i++;
                    }
                    else
                    {
                        info.enabled = false;
                        data[i] = info;
                    }



                }

            }

            return data;
        }
        void Init()
        {
            data = new(m_placer.PoolAmount);
            for (int i = 0; i < m_placer.PoolAmount; i++)
            {
                PropTransformInfo inf;
                inf.position = Vector3.zero;
                inf.rotation = Quaternion.identity;
                inf.enabled = false;
                inf.addedOffset = m_offset;
                data.Add(inf);
            }
            m_placer.SetFunction(Process);
        }
    }

}