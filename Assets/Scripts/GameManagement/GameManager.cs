using UnityEngine;

public class GameManager : MonoBehaviour
{
    private bool isGameStatePaused = false;

    public bool IsGameStatePaused { get => isGameStatePaused; set => isGameStatePaused = value; }

    public static GameManager Instance { get; private set; }

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
        ResumeGameState();
    }

    public void PauseGameState()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isGameStatePaused = true;
    }

    public void ResumeGameState()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isGameStatePaused = false;
    }
}
