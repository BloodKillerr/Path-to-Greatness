using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public enum MenuType
{
    NONE = 0,
    PAUSE,
    STATUS,
    ABILITIES,
    QUESTS
}

public class UIManager : MonoBehaviour
{
    private MenuType currentMenuType = MenuType.NONE;

    [SerializeField] private CanvasGroup pausePanel;
    [SerializeField] private CanvasGroup statusPanel;
    [SerializeField] private CanvasGroup abilitiesPanel;
    [SerializeField] private CanvasGroup questsPanel;

    [SerializeField] private GameObject statusSelectObject;
    [SerializeField] private GameObject abilitiesSelectObject;
    [SerializeField] private GameObject questsSelectObject;

    [Header("Stats")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text HPText;
    [SerializeField] private TMP_Text MPText;
    [SerializeField] private TMP_Text strengthText;
    [SerializeField] private TMP_Text agilityText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text magicText;

    public static UIManager Instance { get; private set; }
    public MenuType CurrentMenuType { get => currentMenuType; set => currentMenuType = value; }

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

    private void Start()
    {
        if (pausePanel != null)
        {
            pausePanel.alpha = 0f;
            pausePanel.blocksRaycasts = false;
            pausePanel.interactable = false;
        }

        if (Player.Instance == null)
        {
            return;
        }

        StatsSetup();
    }

    private void OnDisable()
    {
        if (Player.Instance == null)
        {
            return;
        }

        UnsubscribeStats();
    }

    public void ChangeSelectedElement(GameObject toSelect)
    {
        EventSystem.current.SetSelectedGameObject(toSelect);
    }

    public void OnClickToogleMenu(MenuTypeHolder holder)
    {
        if (holder == null)
        {
            return;
        }
        ToogleMenu(holder.type);
    }

    public void ToogleMenu(MenuType type)
    {
        if (currentMenuType == type)
        {
            CloseMenu(type);
            currentMenuType = MenuType.NONE;
        }
        else
        {
            if (currentMenuType == MenuType.NONE)
            {
                OpenMenu(type);
                currentMenuType = type;
            }
        }
    }

    private void OpenMenu(MenuType type)
    {
        switch (type)
        {
            case MenuType.PAUSE:
                if (pausePanel != null)
                {
                    pausePanel.alpha = 1f;
                    pausePanel.blocksRaycasts = true;
                    pausePanel.interactable = true;
                    ChangeSelectedElement(PauseMenu.Instance.ResumeButton);
                }
                break;
            case MenuType.STATUS:
                if (statusPanel != null)
                {
                    statusPanel.alpha = 1f;
                    statusPanel.blocksRaycasts = true;
                    statusPanel.interactable = true;
                    ChangeSelectedElement(statusSelectObject);
                }
                break;
            case MenuType.ABILITIES:
                if (abilitiesPanel != null)
                {
                    abilitiesPanel.alpha = 1f;
                    abilitiesPanel.blocksRaycasts = true;
                    abilitiesPanel.interactable = true;
                    ChangeSelectedElement(abilitiesSelectObject);
                }
                break;
            case MenuType.QUESTS:
                if (questsPanel != null)
                {
                    questsPanel.alpha = 1f;
                    questsPanel.blocksRaycasts = true;
                    questsPanel.interactable = true;
                    ChangeSelectedElement(questsSelectObject);
                }
                break;
        }
        GameManager.Instance.PauseGameState();
    }

    private void CloseMenu(MenuType type)
    {
        switch (type)
        {
            case MenuType.PAUSE:
                if (pausePanel != null)
                {
                    pausePanel.alpha = 0f;
                    pausePanel.blocksRaycasts = false;
                    pausePanel.interactable = false;
                    PauseMenu.Instance.Hide();
                }
                break;
            case MenuType.STATUS:
                if (statusPanel != null)
                {
                    statusPanel.alpha = 0f;
                    statusPanel.blocksRaycasts = false;
                    statusPanel.interactable = false;
                }
                break;
            case MenuType.ABILITIES:
                if (abilitiesPanel != null)
                {
                    abilitiesPanel.alpha = 0f;
                    abilitiesPanel.blocksRaycasts = false;
                    abilitiesPanel.interactable = false;
                }
                break;
            case MenuType.QUESTS:
                if (questsPanel != null)
                {
                    questsPanel.alpha = 0f;
                    questsPanel.blocksRaycasts = false;
                    questsPanel.interactable = false;
                }
                break;
        }
        GameManager.Instance.ResumeGameState();
    }

    public void TooglePanel(GameObject panelToOpen, GameObject panelToClose)
    {
        panelToOpen.SetActive(true);
        panelToClose.SetActive(false);
    }

    private void StatsSetup()
    {
        PlayerStats playerStats = Player.Instance.GetComponent<PlayerStats>();

        playerNameText.text = playerStats.CharacterName;

        playerStats.HealthChanged.AddListener(OnHealthChanged);
        playerStats.StrengthChanged.AddListener(OnStrengthChanged);
        playerStats.AgilityChanged.AddListener(OnAgilityChanged);
        playerStats.MagicChanged.AddListener(OnMagicChanged);
        playerStats.HPChanged.AddListener(OnHPChanged);
        playerStats.MPChanged.AddListener(OnMPChanged);

        playerStats.InvokeAllStats();
    }

    private void UnsubscribeStats()
    {
        PlayerStats playerStats = Player.Instance.GetComponent<PlayerStats>();
        playerStats.HealthChanged.RemoveListener(OnHealthChanged);
        playerStats.StrengthChanged.RemoveListener(OnStrengthChanged);
        playerStats.AgilityChanged.RemoveListener(OnAgilityChanged);
        playerStats.MagicChanged.RemoveListener(OnMagicChanged);
        playerStats.HPChanged.RemoveListener(OnHPChanged);
        playerStats.MPChanged.RemoveListener(OnMPChanged);
    }

    private void OnHealthChanged(int baseValue, int modifierValue)
    {
        healthText.text = modifierValue == 0 ? $"{baseValue}" : modifierValue < 0 ? $"{baseValue}({modifierValue})" : $"{baseValue}(+{modifierValue})";
    }

    private void OnStrengthChanged(int baseValue, int modifierValue)
    {
        strengthText.text = modifierValue == 0 ? $"{baseValue}" : modifierValue < 0 ? $"{baseValue}({modifierValue})" : $"{baseValue}(+{modifierValue})";
    }

    private void OnAgilityChanged(int baseValue, int modifierValue)
    {
        agilityText.text = modifierValue == 0 ? $"{baseValue}" : modifierValue < 0 ? $"{baseValue}({modifierValue})" : $"{baseValue}(+{modifierValue})";
    }

    private void OnMagicChanged(int baseValue, int modifierValue)
    {
        magicText.text = modifierValue == 0 ? $"{baseValue}" : modifierValue < 0 ? $"{baseValue}({modifierValue})" : $"{baseValue}(+{modifierValue})";
    }

    private void OnHPChanged(int current, int max)
    {
        HPText.text = $"HP: {current} / {max}";
    }

    private void OnMPChanged(int current, int max)
    {
        MPText.text = $"MP: {current} / {max}";
    }
}
