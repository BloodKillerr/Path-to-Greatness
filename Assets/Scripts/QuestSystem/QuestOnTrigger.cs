using UnityEngine;

public class QuestOnTrigger : MonoBehaviour
{
    public QuestSO Quest;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            QuestManager.Instance.AddQuest(Quest, Player.Instance.gameObject);
            Destroy(gameObject);
        }
    }
}
