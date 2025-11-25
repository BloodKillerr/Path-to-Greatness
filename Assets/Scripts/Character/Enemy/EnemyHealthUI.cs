using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;

    private EnemyStats stats;
    private Camera mainCam;

    public void Initialize(EnemyStats stats)
    {
        this.stats = stats;
        nameText.text = stats.CharacterName;

        healthText.text = string.Format("{0}/{1}", stats.CurrentHealth, stats.MaxHealth);

        stats.HPChanged.AddListener(OnHealthChanged);

        mainCam = Camera.main;
    }

    private void OnDestroy()
    {
        if (stats != null)
        {
            stats.HPChanged.RemoveListener(OnHealthChanged);
        }
    }

    private void OnHealthChanged(int current, int max)
    {
        healthText.text = string.Format("{0}/{1}", current, max);
    }

    private void LateUpdate()
    {
        if (stats != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
        }
    }
}