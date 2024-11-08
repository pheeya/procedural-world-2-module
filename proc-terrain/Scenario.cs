using System;
using System.Collections;
using System.Collections.Generic;
using ProcWorld;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProcWorld
{
    public class Scenario : MonoBehaviour
    {
        // [SerializeField] GameObject m_subScenePrototype;


        [SerializeField] bool m_useTerrainBaselineHeightForOrigin;
        [SerializeField] Vector3 m_originMeters;
        [SerializeField] float m_spawnDistanceMeters;
        [SerializeField] Vector3 m_subSceneOffsetPosition;
        [SerializeField] bool m_spawnSubScene;
        [SerializeField] string m_sceneName;


        [SerializeField] bool m_Debug;

        Transform m_sceneRoot;

        public Vector3 GetOrigin() { return m_originMeters; }
        public Vector3 GetSpawnedSceneOrigin() { return m_originMeters + m_subSceneOffsetPosition; }




        public Action EScenarioLoaded;


        public bool Loaded { get; private set; }

        void Awake()
        {
            SceneManager.sceneLoaded += OnLoad;
        }
        void OnLoad(Scene sc, LoadSceneMode mode)
        {
            if (sc.name != m_sceneName) return;


            m_loadedSubScene = sc;
            m_sceneRoot = m_loadedSubScene.GetRootGameObjects()[0].transform;
            EBeforePositioned?.Invoke();

            m_sceneRoot.transform.parent = transform;
            m_sceneRoot.transform.localPosition = Vector3.zero;

            Vector3 origin = m_originMeters;
            if (m_useTerrainBaselineHeightForOrigin)
            {
                origin.y = ProceduralWorld.Instance.BaseLineHeight;

            }
            transform.localPosition = origin + m_subSceneOffsetPosition;

            Physics.SyncTransforms();

            ScenarioScene scene = m_sceneRoot.GetComponent<ScenarioScene>();

            scene.OnPlaced();

            Loaded = true;

            EAfterPositioned?.Invoke();
            EScenarioLoaded?.Invoke();

        }

        public bool LoadStarted { get; private set; }
        public void LoadScenario()
        {
            LoadStarted = true;
            StartCoroutine(LoadSceneCoroutine());
        }


        public void FixedUpdate()
        {



            if (!m_spawnSubScene) return;
            if (LoadStarted || Loaded) return;
            if (!Game.Instance.Loaded) return;




            float distFromOrigin = util.DistanceXZ(TerrainGenerator.PlayerPosV3, m_originMeters);
            if (Mathf.Abs(distFromOrigin) <= m_spawnDistanceMeters)
            {


                LoadScenario();
            }

        }
        Scene m_loadedSubScene;


        public Action EBeforeLoadStarted;
        public Action EBeforePositioned;
        public Action EAfterPositioned;

        IEnumerator LoadSceneCoroutine()
        {
            EBeforeLoadStarted?.Invoke();

            System.Diagnostics.Stopwatch watch = new();

            watch.Start();
            AsyncOperation op = SceneManager.LoadSceneAsync(m_sceneName, LoadSceneMode.Additive);
            Debug.Log("Started scene load");
            // op.allowSceneActivation = false;
            while (!op.isDone)
            {
                // if (op.progress >= 0.9f)
                // {

                //     op.allowSceneActivation = true;
                // }
                // yield return new WaitForFixedUpdate();

                yield return null;
            }
            watch.Stop();
            Debug.Log("Finished scene load, took: " + watch.Elapsed.TotalSeconds + " seconds");


        }

        public Transform GetSceneRoot() { return m_sceneRoot; }


    }

}