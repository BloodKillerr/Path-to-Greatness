using System.Collections.Generic;
using UnityEngine;

public class OnTriggerAddAbility : MonoBehaviour
{
    public string abilityName;
    public string abilityDesc;
    public string bundleId;
    public bool singleGrant = true;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            var data = new Dictionary<string, object>()
            {
                { "target", other.gameObject },
                { "abilityName", abilityName },
                { "abilityDesc", abilityDesc },
                { "bundleId", bundleId },
                { "singleGrant", singleGrant }
            };
            EventBus.Instance.Publish(new SupervisorEvent("Supervisor.GrantAbilityRequest", gameObject, data));
        }
    }
}
