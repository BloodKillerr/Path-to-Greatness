using System.Collections.Generic;
using UnityEngine;
using static StatUpgradeEffectSO;

public class Arrow : MonoBehaviour
{
    [SerializeField] private string eventId = "Arrow.Strength";

    [SerializeField] private StatType statToUpgrade = StatType.Strength;

    [SerializeField] private int amount = 1;

    public void TriggerUpgrade(GameObject playerTarget)
    {
        var data = new Dictionary<string, object>()
        {
            { "target", playerTarget },
            { "stat", statToUpgrade },
            { "amount", amount },
            { "eventId", eventId }
        };

        var ev = new SupervisorEvent("Supervisor.StatUpgradeRequest", source: gameObject, data: data);
        EventBus.Instance.Publish(ev);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && eventId == "Arrow.Health")
        {
            TriggerUpgrade(other.gameObject);
            Destroy(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && eventId == "Arrow.Agility")
        {
            TriggerUpgrade(other.gameObject);
            Destroy(this);
        }
    }
}
