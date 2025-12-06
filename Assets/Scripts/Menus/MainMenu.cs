using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject newGamePanel;

    [SerializeField] private Button mainMenuSelectedButton;
    [SerializeField] private Button optionsMenuSelectedButton;
    [SerializeField] private Button newGameMenuSelectedButton;
    [SerializeField] private Button continueButton;

    [SerializeField] private int nextSceneIndex = 1;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        UIManager.Instance.ChangeSelectedElement(mainMenuSelectedButton.gameObject);

        MasterSaveData data = SaveManager.LoadGame();

        if (data == null)
        {
            continueButton.interactable = false;
        }
        else
        {
            continueButton.interactable = true;
        }
    }

    public void StartGame()
    {
        SaveManager.DeleteSave();
        SceneManager.LoadScene(nextSceneIndex);
    }

    public void ContinueGame()
    {
        MasterSaveData data = SaveManager.LoadGame();

        if (data == null)
        {
            Debug.Log("[MainMenu] No valid save found. Starting a New Game instead.");
            return;
        }
        SceneManager.LoadScene(data.LastSceneBuildIndex);
    }

    public void Options()
    {
        UIManager.Instance.TooglePanel(optionsPanel, menuPanel);
        UIManager.Instance.ChangeSelectedElement(optionsMenuSelectedButton.gameObject);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Quit game");
    }

    public void NewGameBackButton()
    {
        UIManager.Instance.TooglePanel(menuPanel, newGamePanel);
        UIManager.Instance.ChangeSelectedElement(mainMenuSelectedButton.gameObject);
    }

    public void OptionsBackButton()
    {
        UIManager.Instance.TooglePanel(menuPanel, optionsPanel);
        UIManager.Instance.ChangeSelectedElement(mainMenuSelectedButton.gameObject);
    }

    public void ShowNewGamePanel()
    {
        MasterSaveData data = SaveManager.LoadGame();

        if (data == null)
        {
            StartGame();
            return;
        }

        UIManager.Instance.TooglePanel(newGamePanel, menuPanel);
        UIManager.Instance.ChangeSelectedElement(newGameMenuSelectedButton.gameObject);
    }
}
