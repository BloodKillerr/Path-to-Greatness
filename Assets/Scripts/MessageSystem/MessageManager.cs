using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static StatUpgradeEffectSO;

public class MessageManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject worldMessagePrefab;

    [Header("World message default")]
    public float defaultWorldDuration = 1f;

    private readonly Queue<SupervisorEvent> screenQueue = new Queue<SupervisorEvent>();
    private bool screenShowing = false;

    public static MessageManager Instance { get; private set; }

    private UnityAction<SupervisorEvent> spawnWorldHandler;
    private UnityAction<SupervisorEvent> screenNotifyHandler;

    private UnityAction<SupervisorEvent> _rewardGivenHandler;
    private UnityAction<SupervisorEvent> _orbsCollectedHandler;
    private UnityAction<SupervisorEvent> _abilityGrantedHandler;
    private UnityAction<SupervisorEvent> _questAddedHandler;

    private struct WorldMessageRequest
    {
        public GameObject target;
        public string text;
        public float duration;
    }

    private readonly Queue<WorldMessageRequest> worldQueue = new Queue<WorldMessageRequest>();

    public int maxConcurrentWorldMessages = 1;

    public float worldMessageSpacing = 0.6f;

    private int currentWorldVisible = 0;

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

    private void OnEnable()
    {
        spawnWorldHandler = HandleSpawnWorld;
        screenNotifyHandler = HandleScreenNotify;
        EventBus.Instance.Subscribe("Message.SpawnWorld", spawnWorldHandler);
        EventBus.Instance.Subscribe("Message.ScreenNotify", screenNotifyHandler);

        _rewardGivenHandler = (ev) =>
        {
            GameObject player = ev.Get<GameObject>("target", null);
            StatType stat = ev.Get<StatType>("stat", default);
            int applied = ev.Get<int>("appliedAmount", ev.Get<int>("appliedUnits", 0));

            if (player != null && applied > 0)
            {
                PublishWorld(player, $"+{applied} {stat}", 1f);
            }
        };
        EventBus.Instance.Subscribe("Supervisor.RewardGiven", _rewardGivenHandler);

        _questAddedHandler = OnQuestAdded_ShowScreen;
        EventBus.Instance.Subscribe("Quest.QuestAdded", _questAddedHandler);

        _orbsCollectedHandler = (ev) =>
        {
            GameObject player = ev.Get<GameObject>("target", null);
            if (player != null)
            {
                PublishWorld(player, "+1 Orb", 1f);
            }
        };
        EventBus.Instance.Subscribe("Orbs.Collected", _orbsCollectedHandler);

        _abilityGrantedHandler = (ev) =>
        {
            GameObject player = ev.Get<GameObject>("target", null);
            string abilityName = ev.Get<string>("abilityName", null);
            string abilityDesc = ev.Get<string>("abilityDesc", null);
            string title = "New Ability Acquired";
            string body = $"{abilityName}\n\n{abilityDesc}";
            Dictionary<string, object> extra = new Dictionary<string, object> { { "rewardType", "Ability" }, { "abilityName", abilityName } };
            PublishScreen(title, body, extra);
        };
        EventBus.Instance.Subscribe("Supervisor.AbilityGranted", _abilityGrantedHandler);
    }

    private void OnDisable()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Unsubscribe("Message.SpawnWorld", spawnWorldHandler);
            EventBus.Instance.Unsubscribe("Message.ScreenNotify", screenNotifyHandler);
            if (_rewardGivenHandler != null)
            {
                EventBus.Instance.Unsubscribe("Supervisor.RewardGiven", _rewardGivenHandler);
            }

            if (_questAddedHandler != null)
            {
                EventBus.Instance.Unsubscribe("Quest.QuestAdded", _questAddedHandler);
            }

            if (_orbsCollectedHandler != null)
            {
                EventBus.Instance.Unsubscribe("Orbs.Collected", _orbsCollectedHandler);
            }

            if (_abilityGrantedHandler != null)
            {
                EventBus.Instance.Unsubscribe("Supervisor.AbilityGranted", _abilityGrantedHandler);
            }
        }
    }

    private void OnQuestAdded_ShowScreen(SupervisorEvent ev)
    {
        GameObject player = ev.Get<GameObject>("target", null);
        if (player == null)
        {
            return;
        }

        string title = ev.Get<string>("questTitle", "Quest Added");
        string body = ev.Get<string>("questDesc", "");

        Dictionary<string, object> extra = new Dictionary<string, object>();
        extra["rewardType"] = ev.Get<string>("rewardType", null);

        if (extra["rewardType"].ToString() == "StatUpgrade")
        {
            extra["stat"] = ev.Get<StatType>("stat", StatType.Magic);
            extra["amount"] = ev.Get<int>("amount", 0);
        }
        else if (extra["rewardType"].ToString() == "AbilityGrant")
        {
            extra["abilityName"] = ev.Get<string>("abilityName", null);
        }

        MessageManager.Instance.PublishScreen(title, body, extra);
    }

    #region World messages
    private void HandleSpawnWorld(SupervisorEvent ev)
    {
        GameObject target = ev.Get<GameObject>(MessageKeys.Target, null);
        Vector3? pos = ev.Get<Vector3?>(MessageKeys.Position, null);
        string title = ev.Get<string>(MessageKeys.Title, null);
        float duration = ev.Get<float>(MessageKeys.Duration, defaultWorldDuration);

        Vector3 spawnPos;
        if (target != null)
        {
            spawnPos = target.transform.position + Vector3.up * 1.8f;
        }
        else if (pos.HasValue)
        {
            spawnPos = pos.Value;
        }
        else
        {
            spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 2f;
        }

        SpawnWorldMessage(Player.Instance.gameObject, title, duration);
    }

    public void HandleSpawnWorld(string message)
    {
        GameObject target = Player.Instance.gameObject;
        Vector3 spawnPos;
        if (target != null)
        {
            spawnPos = target.transform.position + Vector3.up * 1.8f;
        }
        else
        {
            spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 2f;
        }

        PublishWorld(Player.Instance.gameObject, message, defaultWorldDuration);
    }

    public void SpawnWorldMessage(GameObject player, string text, float duration = -1f, Vector3 localOffset = default)
    {
        if (player == null)
        {
            return;
        }

        if (localOffset == default)
        {
            localOffset = new Vector3(0f, 1.8f, 0.5f);
        }

        GameObject go = Instantiate(worldMessagePrefab, player.transform.TransformPoint(localOffset), Quaternion.identity);
        WorldMessage wm = go.GetComponent<WorldMessage>();
        if (wm != null)
        {
            wm.InitFollow(player.transform, localOffset, text, duration > 0 ? duration : defaultWorldDuration);
            SoundManager.PlaySound(SoundType.MESSAGE, Player.Instance.GetComponent<AudioSource>(), 1);
        }
    }
    #endregion

    #region Screen notifications (queueing + pause)
    private void HandleScreenNotify(SupervisorEvent ev)
    {
        screenQueue.Enqueue(ev);
        if (!screenShowing)
        {
            StartCoroutine(ProcessScreenQueue());
        }
    }

    private IEnumerator ProcessScreenQueue()
    {
        screenShowing = true;

        while (screenQueue.Count > 0)
        {
            SupervisorEvent ev = screenQueue.Dequeue();

            ShowScreenPanel(ev);

            if (UIManager.Instance != null)
            {
                while (UIManager.Instance.MessageOpen)
                {
                    yield return new WaitForSecondsRealtime(0.05f);
                }
            }
            else
            {
                yield return null;
            }

            yield return null;
        }

        screenShowing = false;
    }

    private void ShowScreenPanel(SupervisorEvent ev)
    {
        string title = ev.Get<string>(MessageKeys.Title, null);
        string body = ev.Get<string>(MessageKeys.Body, null);
        Dictionary<string, object> extra = ev.Get<Dictionary<string, object>>(MessageKeys.Data, null);

        UIManager.Instance.ShowScreenMessage(title, body, extra);
    }
    #endregion

    #region Convenience helpers (publishers)
    public void PublishWorld(GameObject target, string text, float duration = -1f)
    {
        if (target == null)
        {
            return;
        }

        float effectiveDuration = duration > 0f ? duration : defaultWorldDuration;

        var req = new WorldMessageRequest
        {
            target = target,
            text = text,
            duration = effectiveDuration,
        };

        if (currentWorldVisible < maxConcurrentWorldMessages)
        {
            StartCoroutine(SpawnWorldNow(req));
        }
        else
        {
            worldQueue.Enqueue(req);
        }
    }

    private IEnumerator SpawnWorldNow(WorldMessageRequest req)
    {
        currentWorldVisible++;

        var data = new Dictionary<string, object>()
        {
            { MessageKeys.Target, req.target },
            { MessageKeys.Title, req.text },
            { MessageKeys.Duration, req.duration }
        };
        EventBus.Instance.Publish(new SupervisorEvent("Message.SpawnWorld", gameObject, data));

        yield return new WaitForSecondsRealtime(req.duration);

        yield return new WaitForSecondsRealtime(worldMessageSpacing);

        currentWorldVisible = Mathf.Max(0, currentWorldVisible - 1);

        while (worldQueue.Count > 0 && currentWorldVisible < maxConcurrentWorldMessages)
        {
            WorldMessageRequest next = worldQueue.Dequeue();
            StartCoroutine(SpawnWorldNow(next));
        }
    }

    public void PublishScreen(string title, string body, Dictionary<string, object> extra = null)
    {
        var data = new Dictionary<string, object>()
        {
            { MessageKeys.Title, title },
            { MessageKeys.Body, body },
            { MessageKeys.Data, extra }
        };
        EventBus.Instance.Publish(new SupervisorEvent("Message.ScreenNotify", gameObject, data));
    }
    #endregion
}
