using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [SerializeField] private AudioMixer mainMixer;

    [SerializeField] private Slider masterVolume;
    [SerializeField] private Slider musicVolume;
    [SerializeField] private Slider sfxVolume;

    [SerializeField] private TMP_Text masterLabel;
    [SerializeField] private TMP_Text musicLabel;
    [SerializeField] private TMP_Text sfxLabel;

    [SerializeField] private TMP_Dropdown resolutionsDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [SerializeField] private TMP_Dropdown fpsDropdown;

    private Resolution[] resolutions;
    private int currentResolutionIndex = 0;

    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject optionsBackButton;
    [SerializeField] private GameObject controlsBackButton;

    [SerializeField] private List<GameObject> inputMaps;

    private void Start()
    {
        SetResolutionDropdown();
        SetQualityDropdown();
        SetDisplayModeDropdown();
        SetFpsDropdown();
        PrepareVolumes();
    }

    private void SetResolutionDropdown()
    {
        resolutionsDropdown.ClearOptions();
        var options = new List<string>();

        resolutions = Screen.resolutions;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            if (!options.Contains(option))
            {
                options.Add(option);
            }

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionsDropdown.AddOptions(options);
        resolutionsDropdown.value = currentResolutionIndex;
        resolutionsDropdown.RefreshShownValue();
        resolutionsDropdown.onValueChanged.AddListener(OnResolutionChange);

        int savedResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        resolutionsDropdown.value = savedResolutionIndex;
        resolutionsDropdown.RefreshShownValue();
    }

    private void SetQualityDropdown()
    {
        qualityDropdown.ClearOptions();

        string[] qualityLevels = QualitySettings.names;
        var qualityOptions = new List<string>(qualityLevels);

        qualityDropdown.AddOptions(qualityOptions);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
        qualityDropdown.onValueChanged.AddListener(OnQualityChange);

        int savedQualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        qualityDropdown.value = savedQualityIndex;
        qualityDropdown.RefreshShownValue();
    }

    private void SetDisplayModeDropdown()
    {
        displayModeDropdown.ClearOptions();

        displayModeDropdown.AddOptions(new List<string> { "Fullscreen", "Windowed Fullscreen", "Windowed" });
        displayModeDropdown.value = GetCurrentDisplayModeIndex();
        displayModeDropdown.RefreshShownValue();
        displayModeDropdown.onValueChanged.AddListener(SetDisplayMode);

        switch (PlayerPrefs.GetInt("ScreenMode", 0))
        {
            case 0:
                displayModeDropdown.value = 0; // Fullscreen
                break;
            case 1:
                displayModeDropdown.value = 1; // Windowed Fullscreen
                break;
            case 2:
                displayModeDropdown.value = 2; // Windowed
                break;
        }
        displayModeDropdown.RefreshShownValue();
    }

    private void SetFpsDropdown()
    {
        fpsDropdown.ClearOptions();
        fpsDropdown.AddOptions(new List<string> { "30", "60", "90", "120", "150", "240", "Unlimited" });
        fpsDropdown.RefreshShownValue();

        int savedFps = PlayerPrefs.GetInt("TargetFPS", 60);
        fpsDropdown.value = GetFpsIndex(savedFps);
        fpsDropdown.onValueChanged.AddListener(OnFpsChange);
    }

    private int GetFpsIndex(int fps)
    {
        switch (fps)
        {
            case 30: return 0;
            case 60: return 1;
            case 90: return 2;
            case 120: return 3;
            case 150: return 4;
            case 240: return 5;
            case 300: return 6;
            default: return 1;
        }
    }

    private void OnFpsChange(int index)
    {
        int fps = 60;

        switch (index)
        {
            case 0:
                fps = 30;
                break;
            case 1:
                fps = 60;
                break;
            case 2:
                fps = 90;
                break;
            case 3:
                fps = 120;
                break;
            case 4:
                fps = 150;
                break;
            case 5:
                fps = 240;
                break;
            case 6:
                fps = 300;
                break;
        }

        Application.targetFrameRate = fps;
        PlayerPrefs.SetInt("TargetFPS", fps);
        Debug.Log("FPS set to: " + fps);
    }

    private void OnResolutionChange(int index)
    {
        string[] dims = resolutionsDropdown.options[index].text.Split('x');
        int width = int.Parse(dims[0].Trim());
        int height = int.Parse(dims[1].Trim());

        Screen.SetResolution(width, height, Screen.fullScreen);

        PlayerPrefs.SetInt("ResolutionIndex", index);

        Debug.Log("Resolution changed to: " + width + " x " + height);
    }

    private int GetCurrentDisplayModeIndex()
    {
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.ExclusiveFullScreen:
                return 0; // Fullscreen
            case FullScreenMode.FullScreenWindow:
                return 1; // Windowed Fullscreen
            case FullScreenMode.Windowed:
                return 2; // Windowed
            default:
                return 0; // Default to Fullscreen
        }
    }

    private void SetDisplayMode(int index)
    {
        switch (index)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                PlayerPrefs.SetInt("ScreenMode", 0);
                Debug.Log("Display mode changed to: Fullscreen");
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                PlayerPrefs.SetInt("ScreenMode", 1);
                Debug.Log("Display mode changed to: Windowed Fullscreen");
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                PlayerPrefs.SetInt("ScreenMode", 2);
                Debug.Log("Display mode changed to: Windowed");
                break;
        }
    }

    private void OnQualityChange(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt("QualityLevel", index);
        Debug.Log("Quality changed to: " + QualitySettings.names[index]);
    }

    private void PrepareVolumes()
    {
        masterVolume.value = PlayerPrefs.GetFloat("MasterVolume", 100);
        musicVolume.value = PlayerPrefs.GetFloat("MusicVolume", 100);
        sfxVolume.value = PlayerPrefs.GetFloat("SfxVolume", 100);

        masterLabel.text = masterVolume.value.ToString();
        musicLabel.text = musicVolume.value.ToString();
        sfxLabel.text = sfxVolume.value.ToString();

        SetVolume("MasterVolume", masterVolume.value);
        SetVolume("MusicVolume", musicVolume.value);
        SetVolume("SFXVolume", sfxVolume.value);

        masterVolume.onValueChanged.AddListener((value) =>
        {
            SetVolume("MasterVolume", value);
            PlayerPrefs.SetFloat("MasterVolume", value);
            masterLabel.text = value.ToString();
        });

        musicVolume.onValueChanged.AddListener((value) =>
        {
            SetVolume("MusicVolume", value);
            PlayerPrefs.SetFloat("MusicVolume", value);
            musicLabel.text = value.ToString();
        });

        sfxVolume.onValueChanged.AddListener((value) =>
        {
            SetVolume("SFXVolume", value);
            PlayerPrefs.SetFloat("SfxVolume", value);
            sfxLabel.text = value.ToString();
        });
    }

    private void SetVolume(string parameterName, float sliderValue)
    {
        float volume = sliderValue > 0 ? Mathf.Log10(sliderValue / 100f) * 20f : -80f;
        mainMixer.SetFloat(parameterName, volume);
    }

    public void ControlsButton()
    {
        UIManager.Instance.TooglePanel(controlsPanel, optionsPanel);
        UIManager.Instance.ChangeSelectedElement(controlsBackButton);
    }

    public void ControlsBackButton()
    {
        UIManager.Instance.TooglePanel(optionsPanel, controlsPanel);
        UIManager.Instance.ChangeSelectedElement(optionsBackButton);
    }

    public void CycleInputMaps()
    {
        if (inputMaps == null || inputMaps.Count == 0)
        {
            return;
        }

        int currentIndex = -1;
        for (int i = 0; i < inputMaps.Count; i++)
        {
            if (inputMaps[i].activeSelf)
            {
                currentIndex = i;
                break;
            }
        }

        if (currentIndex == -1)
        {
            foreach (GameObject go in inputMaps)
            {
                go.SetActive(false);
            }

            inputMaps[0].SetActive(true);
            return;
        }

        inputMaps[currentIndex].SetActive(false);

        int nextIndex = (currentIndex + 1) % inputMaps.Count;

        inputMaps[nextIndex].SetActive(true);
    }
}
