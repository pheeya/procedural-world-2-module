using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProcWorld
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher instance;

        // Queue for actions to execute on the main thread
        private readonly Queue<Action> actionQueue = new Queue<Action>();

        private void Awake()
        {
            instance = this;
        }

        private void Update()
        {

            if(actionQueue.Count>0){
                Debug.Log("Got action");
            }
            // Execute queued actions on the main thread
            lock (actionQueue)
            {
                while (actionQueue.Count > 0)
                {
                    Action action = actionQueue.Dequeue();
                    action.Invoke();
                }
            }
        }

        // Enqueue an action to execute on the main thread
        public void Enqueue(Action action)
        {
            lock (actionQueue)
            {
                actionQueue.Enqueue(action);
            }
        }

        // Get the instance of MainThreadDispatcher
        public static MainThreadDispatcher Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<MainThreadDispatcher>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("MainThreadDispatcher");
                        instance = obj.AddComponent<MainThreadDispatcher>();
                    }
                }
                return instance;
            }
        }
    }
}