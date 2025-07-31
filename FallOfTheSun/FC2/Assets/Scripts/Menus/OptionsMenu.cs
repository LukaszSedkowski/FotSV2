using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OptionsMenu : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider volumeSlider;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown;

    private Resolution[] resolutions;

    void Start()
    {
        // Wype³nienie dropdowna rozdzielczoœci
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new System.Collections.Generic.List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = $"{resolutions[i].width}x{resolutions[i].height}";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        // Tryb ekranu
        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Fullscreen", "Windowed" });
        screenModeDropdown.value = Screen.fullScreen ? 0 : 1;

        float initVol = AudioListener.volume;
        if (MusicPlayer.Instance != null)
            initVol = MusicPlayer.Instance.GetComponent<AudioSource>().volume;
        volumeSlider.value = initVol;
        volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    public void SetVolume(float value)
    {
        if (MusicPlayer.Instance != null)
        {
            MusicPlayer.Instance.SetVolume(value);
        }
        else
        {
            // W przeciwnym razie ustaw globalnie
            AudioListener.volume = value;
        }
    }

    public void SetResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void SetScreenMode(int index)
    {
        bool fullscreen = (index == 0);
        Screen.fullScreen = fullscreen;
    }
}
