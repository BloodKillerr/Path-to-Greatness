using System.Collections.Generic;
using UnityEngine;

public class Orb : MonoBehaviour
{
    public string eventId = "Orbs.Collected";

    public void Interact(GameObject player)
    {
        var data = new Dictionary<string, object>()
        {
            { "target", player },
            { "amount", 1 }
        };
        EventBus.Instance.Publish(new SupervisorEvent(eventId, gameObject, data));

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Interact(other.gameObject);
        }
    }
}
