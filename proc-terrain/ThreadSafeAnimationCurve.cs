using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProcWorld
{
    public class ThreadSafeAnimationCurve : MonoBehaviour
    {

        [SerializeField, Tooltip("Just for your reference in case you have multiple curves on the same gameobject")] string m_friendlyName;
        [SerializeField] AnimationCurve m_unityCurve;
        [SerializeField, Tooltip("2 minimum")] int m_dataPoints = 50;
        [SerializeField] float m_testEvaluateValueAt;
        [SerializeField] float m_testEvaluteValueAnswer;
        [SerializeField] float m_testEvaluteValueAnswerUnity;


        [SerializeField] List<float> m_data;

        [SerializeField, HideInInspector] int m_dataPointActualSize;
        void OnValidate()
        {
            m_dataPointActualSize = m_dataPoints;
            if (m_dataPoints < 2)
            {
                m_dataPointActualSize = 2;
            }
            m_data = new(m_dataPointActualSize);

            for (int i = 0; i < m_dataPointActualSize; i++)
            {
                float val = m_unityCurve.Evaluate((float)i / (m_dataPointActualSize - 1));
                m_data.Add(val);
            }

            m_testEvaluteValueAnswer = Evaluate(m_testEvaluateValueAt);
            m_testEvaluteValueAnswerUnity = m_unityCurve.Evaluate(m_testEvaluateValueAt);
        }


        public float Evaluate(float _time)
        {

            float time = _time * (m_dataPointActualSize - 1); // 0 - 99 if dataPoints = 100

            if (time < 0)
            {
                return m_data[0];
            }
            else if (time > m_dataPointActualSize - 1)
            {
                if (m_dataPointActualSize != m_data.Count)
                {
                    Debug.Log(m_dataPointActualSize);
                    Debug.Log(m_data.Count);
                }
                return m_data[m_dataPointActualSize - 1];
            }

            float lowerVal = Mathf.Floor(time);
            float upperVal = Mathf.Ceil(time);

            // if we got whole number
            if (lowerVal == upperVal)
            {
                return m_data[Mathf.FloorToInt(time)];
            }

            float interp = time - Mathf.FloorToInt(time);


            int upperIndex = (int)upperVal;
            int lowerIndex = (int)lowerVal;


            if (lowerIndex < 0 || upperIndex > m_dataPointActualSize)
            {
                Debug.Log(lowerIndex);
                Debug.Log(upperIndex);
                Debug.Log(time);
            }

            float evaluateLow = m_data[lowerIndex];
            float evaluateHigh = m_data[upperIndex];

            float val = Mathf.Lerp(evaluateLow, evaluateHigh, interp);
            return val;
        }
    }

}