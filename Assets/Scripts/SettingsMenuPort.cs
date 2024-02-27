using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SettingsMenuPort : MonoBehaviour
{
    public AudioMixer audioMixer;

    public TMPro.TMP_Dropdown resolutionDropdown;

    Resolution[] resolutions;

    public bool initialized = false;

    public Slider mouseSensitivitySlider;

    void Start ()
    {
        if (PlayerPrefs.HasKey("Sensitivity"))
        {
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat ("Sensitivity");
            Debug.Log("Loaded a sensitivity of " + mouseSensitivitySlider.value);
        }
        initialized = true;

        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetMouseSensitivity(float val)
    {
        if (! initialized) return;
        if (! Application.isPlaying) return;


        PlayerPrefs.SetFloat("Sensitivity", val);
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
           //replace "PlayerMovement" with wherever the current sens is controlled //player.GetComponent<PlayerMovement>().ChangeSens(val);
        }
        Debug.Log("Set sensitivity to " + val);
    }

    public void SetDifficulty(int val)
    {
        if (! initialized) return;
        if (! Application.isPlaying) return;


        PlayerPrefs.SetInt("Difficulty", val);
        Debug.Log(val);

        switch (val) {
            case 0:
                Debug.Log("Set difficulty to Normal");
                break;
            case 1:
                Debug.Log("Set difficulty to Hard");
                break;
            default:
                break;
        }
    }

    public void SetResolution (int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }

    public void SetVolume (float volume)
    {
        audioMixer.SetFloat("volume", volume);

    }

    public void SetQuality (int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void SetFullscreen (bool isFullscreen)
    {
        Screen.fullScreen =isFullscreen;
    }
}