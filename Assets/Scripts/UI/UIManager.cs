using UnityEngine;
using UnityEngine.EventSystems;

public enum MenuType
{
    NONE,
    PAUSE
}

public class UIManager : MonoBehaviour
{
    private MenuType currentMenuType = MenuType.NONE;

    public static UIManager Instance { get; private set; }

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

    public void ChangeSelectedElement(GameObject toSelect)
    {
        EventSystem.current.SetSelectedGameObject(toSelect);
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
        //switch (type)
        //{

        //}
        GameManager.Instance.PauseGameState();
    }

    private void CloseMenu(MenuType type)
    {
        //switch (type)
        //{

        //}
        GameManager.Instance.ResumeGameState();
    }

    public void TooglePanel(GameObject panelToOpen, GameObject panelToClose)
    {
        panelToOpen.SetActive(true);
        panelToClose.SetActive(false);
    }
}
