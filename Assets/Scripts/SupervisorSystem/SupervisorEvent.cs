using System.Collections.Generic;
using UnityEngine;

public class SupervisorEvent
{
    public string EventType { get; }
    public GameObject Source { get; }
    public Dictionary<string, object> Data { get; }

    public SupervisorEvent(string eventType, GameObject source = null, Dictionary<string, object> data = null)
    {
        EventType = eventType;
        Source = source;
        Data = data ?? new Dictionary<string, object>();
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        if (Data != null && Data.TryGetValue(key, out object val) && val is T tVal)
        {
            return tVal;
        }
            
        return defaultValue;
    }
}
