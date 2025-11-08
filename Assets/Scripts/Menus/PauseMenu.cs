using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject controlsPanel;
    public GameObject RebindingUI;

    public GameObject ResumeButton;
    public GameObject OptionsButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Resume()
    {
        Debug.Log("Resume");
        UIManager.Instance.ToogleMenu(MenuType.PAUSE);
    }

    public void Options()
    {
        if (optionsPanel.activeInHierarchy)
        {
            optionsPanel.SetActive(false);
            pausePanel.SetActive(true);
            UIManager.Instance.ChangeSelectedElement(ResumeButton);
        }
        else
        {
            optionsPanel.SetActive(true);
            pausePanel.SetActive(false);
            controlsPanel.SetActive(false);
            UIManager.Instance.ChangeSelectedElement(OptionsButton);
        }
    }

    public void OptionsBackButton()
    {
        UIManager.Instance.TooglePanel(pausePanel, optionsPanel);
        UIManager.Instance.ChangeSelectedElement(ResumeButton);
    }

    public void Hide()
    {
        pausePanel.SetActive(true);
        optionsPanel.SetActive(false);
        controlsPanel.SetActive(false);
    }

    public void MainMenu()
    {
        Debug.Log("Main Menu");
        SceneManager.LoadScene(0);
        GameManager.Instance.ResumeGameState();
    }

    public void Quit()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
}
