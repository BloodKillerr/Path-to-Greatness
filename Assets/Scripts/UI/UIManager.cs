using System;
using System.Collections.Generic;
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

    [Header("Panels")]
    [SerializeField] private CanvasGroup pausePanel;
    [SerializeField] private CanvasGroup statusPanel;
    [SerializeField] private CanvasGroup abilitiesPanel;
    [SerializeField] private CanvasGroup questsPanel;
    [SerializeField] private CanvasGroup messagePanel;

    [Header("Select Objects")]

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

    [Header("Abilities")]
    [SerializeField] private Transform passiveAbilityHolder;
    [SerializeField] private GameObject passiveAbilityPrefab;
    [SerializeField] private Transform activeAbilityHolder;
    [SerializeField] private GameObject activeAbilityPrefab;

    [Header("Quests")]
    [SerializeField] private Transform questsHolder;
    [SerializeField] private GameObject questBlockPrefab;
    [SerializeField] private TMP_Text questText;

    [Header("Message")]
    [SerializeField] private TMP_Text messageTitleText;
    [SerializeField] private TMP_Text messageBodyText;
    private bool messageOpen = false;

    public static UIManager Instance { get; private set; }
    public MenuType CurrentMenuType { get => currentMenuType; set => currentMenuType = value; }
    public bool MessageOpen { get => messageOpen; set => messageOpen = value; }

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
                    UpdateAbilitiesUI();
                }
                break;
            case MenuType.ABILITIES:
                if (abilitiesPanel != null)
                {
                    abilitiesPanel.alpha = 1f;
                    abilitiesPanel.blocksRaycasts = true;
                    abilitiesPanel.interactable = true;
                    ChangeSelectedElement(abilitiesSelectObject);
                    UpdateAbilitiesUI();
                }
                break;
            case MenuType.QUESTS:
                if (questsPanel != null)
                {
                    questsPanel.alpha = 1f;
                    questsPanel.blocksRaycasts = true;
                    questsPanel.interactable = true;
                    ChangeSelectedElement(questsSelectObject);
                    UpdateQuestsUI();
                    ClearQuestView();
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
                    BindingDialog.Instance.HideCanvas();
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

    public void OpenMenuTemporarily(MenuType type)
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
                    UpdateAbilitiesUI();
                }
                break;
            case MenuType.ABILITIES:
                if (abilitiesPanel != null)
                {
                    abilitiesPanel.alpha = 1f;
                    abilitiesPanel.blocksRaycasts = true;
                    abilitiesPanel.interactable = true;
                    ChangeSelectedElement(abilitiesSelectObject);
                    UpdateAbilitiesUI();
                }
                break;
            case MenuType.QUESTS:
                if (questsPanel != null)
                {
                    questsPanel.alpha = 1f;
                    questsPanel.blocksRaycasts = true;
                    questsPanel.interactable = true;
                    ChangeSelectedElement(questsSelectObject);
                    UpdateQuestsUI();
                    ClearQuestView();
                }
                break;
        }
    }

    public void CloseMenuTemporarily(MenuType type)
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

    public void UpdateAbilitiesUI()
    {
        if (passiveAbilityHolder == null)
        {
            return;
        }

        foreach (Transform child in passiveAbilityHolder)
        {
            Destroy(child.gameObject);
        }

        foreach (Ability ability in AbilityManager.Instance.CurrentPassiveAbilities)
        {
            GameObject go = Instantiate(passiveAbilityPrefab, passiveAbilityHolder);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = ability.AbilityName;
        }

        if (activeAbilityHolder == null)
        {
            return;
        }

        foreach (Transform child in activeAbilityHolder)
        {
            Destroy(child.gameObject);
        }

        foreach (Ability ability in AbilityManager.Instance.CurrentActiveAbilities)
        {
            List<int> slots = new List<int>();

            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                Ability bound = AbilityManager.Instance.GetBoundAbility(slotIndex);
                if (bound != null && bound.AbilityName == ability.AbilityName)
                {
                    slots.Add(slotIndex + 1);
                }
            }

            string badge = slots.Count > 0 ? $" ({string.Join(",", slots)})" : "";

            GameObject go = Instantiate(activeAbilityPrefab, activeAbilityHolder);
            TMP_Text text = go.GetComponent<TMP_Text>();
            text.text = ability.AbilityName + badge;
            go.GetComponent<AbilityButton>().abilityInstance = ability;
        }
    }

    public void UpdateQuestsUI()
    {
        if(questsHolder == null)
        {
            return;
        }

        foreach (Transform child in questsHolder)
        {
            Destroy(child.gameObject);
        }

        foreach (QuestInstance quest in QuestManager.Instance.GetActiveQuests(Player.Instance.gameObject))
        {
            GameObject go = Instantiate(questBlockPrefab, questsHolder);
            QuestBlock block = go.GetComponent<QuestBlock>();
            block.InitQuestBlock(quest);
        }
    }

    public void UpdateQuestsView(QuestInstance questInstance)
    {
        QuestSO quest = questInstance.quest;
        questText.text = string.Format("{0}\n\n{1}\n\nRequirements:\n{2}\n\nRewards:\n{3}", quest.title, quest.description, GetQuestRequirements(questInstance), GetRewardDisplay(quest));
    }

    public void ClearQuestView()
    {
        questText.text = "";
    }

    private string GetRewardDisplay(QuestSO quest)
    {
        if (quest.rewardType == QuestRewardType.StatUpgrade)
        {
            return $"+{quest.rewardAmount} {quest.rewardStat}";
        }

        if (quest.rewardType == QuestRewardType.AbilityGrant)
        {
            return $"Ability: {quest.rewardAbilityName}";
        }

        return "Reward";
    }

    private string GetQuestRequirements(QuestInstance questInstance)
    {
        QuestSO quest = questInstance.quest;
        string requirements = "";

        foreach(QuestRequirement requirement in quest.requirements)
        {
            int prog = questInstance.GetProgress(requirement.eventId);
            requirements += $"{prog}/{requirement.requiredAmount} — {HumanizeEventId(requirement.eventId)}\n";
        }

        return requirements;
    }

    private string HumanizeEventId(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return "";
        }

        return eventId.Replace('.', ' ').Replace('_', ' ');
    }

    public void ShowScreenMessage(string title, string body, Dictionary<string, object> extraData = null)
    {
        if (messagePanel == null)
        {
            return;
        }

        messageOpen = true;

        SoundManager.PlaySound(SoundType.MESSAGE, Player.Instance.GetComponent<AudioSource>(), 1);

        GameManager.Instance.PauseGameState();

        messagePanel.alpha = 1f;
        messagePanel.blocksRaycasts = true;
        messagePanel.interactable = true;

        if (messageTitleText != null)
        {
            messageTitleText.text = title ?? "";
        }

        if (messageBodyText != null)
        {
            messageBodyText.text = body ?? "";
        }

        if (extraData != null)
        {
            if (extraData.TryGetValue("rewardType", out var rtObj) && rtObj is string rt)
            {
                
                string display = "";
                if (rt == "StatUpgrade")
                {
                    if (extraData.TryGetValue("stat", out var sObj) && extraData.TryGetValue("amount", out var aObj))
                    {
                        display = $"+{aObj} {sObj}";

                        if (messageBodyText != null)
                        {
                            messageBodyText.text += string.Format("\n\nRewards:\n{0}", display);
                        }
                    }
                }
                else if (rt == "AbilityGrant")
                {
                    if (extraData.TryGetValue("abilityName", out var aName))
                    {
                        display = (string)aName;

                        if (messageBodyText != null)
                        {
                            messageBodyText.text += string.Format("\n\nRewards:\n{0}", display);
                        }
                    }
                }
            }
        }
    }

    public void CloseScreenMessage()
    {
        if (messagePanel != null)
        {
            messagePanel.alpha = 0f;
            messagePanel.blocksRaycasts = false;
            messagePanel.interactable = false;
            messageOpen = false;

            GameManager.Instance.ResumeGameState();
        }
    }
}
