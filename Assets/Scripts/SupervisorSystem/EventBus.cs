using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventBus : MonoBehaviour
{
    private readonly Dictionary<string, SupervisorEventUnityEvent> subscribers = new();

    public static EventBus Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void Subscribe(string eventType, UnityAction<SupervisorEvent> handler)
    {
        if (!subscribers.TryGetValue(eventType, out SupervisorEventUnityEvent unityEvent))
        {
            unityEvent = new SupervisorEventUnityEvent();
            subscribers[eventType] = unityEvent;
        }

        unityEvent.AddListener(handler);
    }

    public void Unsubscribe(string eventType, UnityAction<SupervisorEvent> handler)
    {
        if (subscribers.TryGetValue(eventType, out SupervisorEventUnityEvent unityEvent))
        {
            unityEvent.RemoveListener(handler);
        }
    }

    public void Publish(SupervisorEvent ev)
    {
        if (ev == null)
        {
            return;
        }

        if (subscribers.TryGetValue(ev.EventType, out SupervisorEventUnityEvent unityEvent))
        {
            unityEvent?.Invoke(ev);
        }
    }
}
