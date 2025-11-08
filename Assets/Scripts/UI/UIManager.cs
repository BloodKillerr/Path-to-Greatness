using System;
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
}
