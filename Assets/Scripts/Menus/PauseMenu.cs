using System.Collections.Generic;
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

    public void Save()
    {
        MasterSaveData data = new MasterSaveData();

        data.LastSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;

        SavePlayerData(data);
        SavePlayerStats(data);
        SaveDungeonData(data);
        SaveEnemiesAndSpawners(data);

        SaveManager.SaveGame(data);
        Debug.Log("Save");
    }

    private void SavePlayerData(MasterSaveData data)
    {
        GameObject playerObject = Player.Instance?.gameObject;
        if (playerObject == null)
        {
            Debug.LogError("[PauseMenu] No Player instance found to save position/rotation.");
        }
        else
        {
            Vector3 pos = playerObject.transform.position;
            Quaternion rot = playerObject.transform.rotation;
            data.PlayerData = new PlayerData(pos, rot);
        }
    }

    private void SavePlayerStats(MasterSaveData data)
    {
        PlayerStats ps = Player.Instance?.GetComponent<PlayerStats>();
        if (ps == null)
        {
            Debug.LogWarning("[PauseMenu] No PlayerStats component found; skipping stats save.");
            data.PlayerStatsData = null;
            return;
        }

        data.PlayerStatsData = ps.CollectPlayerStatsState();
    }

    private void SaveDungeonData(MasterSaveData data)
    {
        data.DungeonData = DungeonManager.Instance != null
            ? DungeonManager.Instance.CollectDungeonState()
            : null;
    }

    private void SaveEnemiesAndSpawners(MasterSaveData data)
    {
        Enemy[] enemies = FindObjectsOfType<Enemy>();
        data.enemies = new List<EnemySaveData>(enemies.Length);
        foreach (Enemy e in enemies)
        {
            if (e == null)
            {
                continue;
            }

            data.enemies.Add(e.CollectEnemyState());
        }

        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        data.spawners = new List<EnemySpawnerSaveData>(spawners.Length);
        foreach (EnemySpawner s in spawners)
        {
            if (s == null)
            {
                continue;
            }

            data.spawners.Add(s.CollectSpawnerState());
        }
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
