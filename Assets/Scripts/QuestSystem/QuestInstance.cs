using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class QuestInstance
{
    public QuestSO quest;
    public GameObject player;

    private readonly Dictionary<string, int> progress = new Dictionary<string, int>();
    private readonly Dictionary<string, UnityAction<SupervisorEvent>> handlers = new Dictionary<string, UnityAction<SupervisorEvent>>();

    private readonly Action<QuestInstance> onCompleted;
    private readonly Action<QuestInstance, string, int, int> onProgressChanged;

    public QuestInstance(QuestSO quest, GameObject player, Action<QuestInstance> onCompleted, Action<QuestInstance, string, int, int> onProgressChanged)
    {
        this.quest = quest;
        this.player = player;
        this.onCompleted = onCompleted;
        this.onProgressChanged = onProgressChanged;
        foreach (QuestRequirement req in quest.requirements)
        {
            if (string.IsNullOrEmpty(req.eventId))
            {
                continue;
            }

            progress[req.eventId] = 0;
            UnityAction<SupervisorEvent> handler = (ev) =>
            {
                GameObject evTarget = ev.Get<GameObject>("target", null);
                if (evTarget != player)
                {
                    return;
                }

                int amount = ev.Get<int>("amount", 1);
                int curr = progress[req.eventId];
                int newVal = Mathf.Min(req.requiredAmount, curr + amount);
                if (newVal != curr)
                {
                    progress[req.eventId] = newVal;
                    onProgressChanged?.Invoke(this, req.eventId, newVal, req.requiredAmount);
                    CheckCompletion();
                }
            };
            EventBus.Instance.Subscribe(req.eventId, handler);
            handlers[req.eventId] = handler;
        }
    }
    private void CheckCompletion()
    {
        foreach (QuestRequirement req in quest.requirements)
        {
            int cur = progress.ContainsKey(req.eventId) ? progress[req.eventId] : 0;
            if (cur < req.requiredAmount)
            {
                return;
            }
        }
        Dispose();
        onCompleted?.Invoke(this);
    }
    public void Dispose()
    {
        foreach (var kv in handlers)
        {
            EventBus.Instance.Unsubscribe(kv.Key, kv.Value);
        }
        handlers.Clear();
    }
    public int GetProgress(string eventId)
    {
        return progress.TryGetValue(eventId, out var p) ? p : 0;
    }
}
