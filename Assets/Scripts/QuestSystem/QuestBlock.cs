using TMPro;
using UnityEngine;

public class QuestBlock : MonoBehaviour
{
    public TMP_Text QuestTitleText;

    public QuestInstance QuestInstance;

    public void InitQuestBlock(QuestInstance questInstance)
    {
        QuestInstance = questInstance;

        QuestTitleText.text = QuestInstance.quest.title;
    }

    public void ShowQuestInfo()
    {
        UIManager.Instance.UpdateQuestsView(QuestInstance);
    }
}
