using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

namespace UnityToolBag
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static MainThreadDispatcher _instance;
        private static bool _hasInit = false;
        private static int _mainThreadId = -1;
        private static object _lockObject = new object();

        private static readonly Queue<Action> _actions = new Queue<Action>();

        public static bool IsMainThread
        {
            get
            {
                return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
            }
        }

        public static void InitMainThreadDispatcher()
        {
            CreateMainThreadDipatcher();
        }

        // must be called in main thread
        private static void CreateMainThreadDipatcher()
        {
            GameObject newobj = new GameObject("MainThreadDispatcher(Clone)");
            newobj.AddComponent<MainThreadDispatcher>();
        }

        // Execute action on Unity Game Main Thread
        // no-block main thread
        public static void InvokeOnMainThread(Action action)
        {
            if (!_hasInit)
            {
                Debug.LogError("No Dispatcher exits in the scene. Actions will not be invoked!");
                return;
            }
            if (IsMainThread)
            {
                action();
            }
            else
            {
                lock (_lockObject)
                {
                    _actions.Enqueue(action);
                }
            }
        }

        void Awake()
        {
            Debug.Log("MainThreadDispacther Awake() called...");
            if (_instance)
            {
                DestroyImmediate(this);
            }
            else
            {
                _instance = this;
                _hasInit = true;
                _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            }
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _hasInit = false;
            }
        }

        void Update()
        {
            lock (_lockObject)
            {
                while (_actions.Count > 0)
                {
                    Action action = _actions.Dequeue();
                    action.Invoke();
                }
            }
        }
    }
}

