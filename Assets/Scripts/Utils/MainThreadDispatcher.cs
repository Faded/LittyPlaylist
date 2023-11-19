using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly Queue<Action> actionQueue = new();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("MainThreadDispatcher").AddComponent<MainThreadDispatcher>();
            }
            return _instance;
        }
    }

    public void Enqueue(Action action)
    {
        actionQueue.Enqueue(action);
    }

    private void Update()
    {
        while (actionQueue.Count > 0)
        {
            var action = actionQueue.Dequeue();
            action.Invoke();
        }
    }
}