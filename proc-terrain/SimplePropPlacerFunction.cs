using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProcWorld
{
    public class SimplePropPlacerFunction : MonoBehaviour
    {


        [SerializeField] int m_radius;
        [SerializeField] float m_noiseSpawnThreashold;
        [SerializeField] PropPlacer m_placer;
        [SerializeField] PerlinNoiseConfig m_perlinConfig;



        List<PropTransformInfo> data;
        void Awake()
        {
            m_placer.EInit += Init;
        }
        List<PropTransformInfo> Process()
        {

            Vector2 origin = TerrainGenerator.PlayerPosV2;

            Vector2[] offsets = NoiseGenerator.GetOctaveOffsets(m_perlinConfig, Mathf.Round(origin.x), Mathf.Round(origin.y));


            int i = 0;

            for (int y = -m_radius / 2; y < m_radius / 2; y++)
            {
                for (int x = -m_radius / 2; x < m_radius / 2; x++)
                {
                    if (i >= data.Count) return data;


                    float noise = NoiseGenerator.GetPerlinValue(m_perlinConfig, x, y, offsets, 0.2f, .5f);

                    // conver to 0 to 1, GetPerlinValue gives -1 to 1
                    noise += 1;
                    noise /= 2;
                    PropTransformInfo info = data[i];

                    if (noise < m_noiseSpawnThreashold)
                    {


                        info.enabled = true;
                        info.position = new(x, TerrainGenerator.Instance.GetScaledNoiseAt(x, y), y);
                        info.rotation = Quaternion.identity;

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
                data.Add(inf);
            }
            m_placer.SetFunction(Process);
        }
    }

}