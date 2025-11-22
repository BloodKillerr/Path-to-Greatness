using UnityEngine;

public class TriggerStartingLocation : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            SetPlayerStartingPosition startingPosition = FindFirstObjectByType<SetPlayerStartingPosition>();

            if (startingPosition != null)
            {
                startingPosition.TriggerTeleportation();
            }
        }
    }
}
