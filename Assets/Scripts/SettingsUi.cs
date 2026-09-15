using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using TMPro;

using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UI.Toggle;

/// <summary>
/// Options panel. Works in the main menu and in an in-game pause menu.
/// Sensitivity can be typed in as a number or dragged on the slider — they stay in sync.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] TMP_Dropdown resolutionDropdown;
    [SerializeField] TMP_Dropdown windowModeDropdown;

    [Header("Graphics (placeholder — expand later)")]
    [SerializeField] TMP_Dropdown qualityDropdown;
    [SerializeField] Toggle vsyncToggle;

    [Header("Mouse")]
    [SerializeField] TMP_InputField sensitivityInput;   // Content Type: Decimal Number
    [SerializeField] Slider sensitivitySlider;          // Min 0.05, Max 10
    [SerializeField] Toggle invertYToggle;

    readonly List<Resolution> resolutions = new List<Resolution>();
    bool suppressCallbacks;

    void OnEnable()
    {
        BuildResolutionList();
        BuildQualityList();
        BuildWindowModeList();
        PushToUI();
        HookEvents();
    }

    // ---------- Building the lists ----------

    void BuildResolutionList()
    {
        if (resolutionDropdown == null) return;

        resolutions.Clear();
        var seen = new HashSet<string>();
        foreach (var r in Screen.resolutions)
        {
            var key = $"{r.width}x{r.height}";
            if (seen.Add(key)) resolutions.Add(r);
        }

        var options = new List<string>();
        int current = 0;
        for (int i = 0; i < resolutions.Count; i++)
        {
            options.Add($"{resolutions[i].width} x {resolutions[i].height}");
            if (resolutions[i].width == GameSettings.ResolutionWidth &&
                resolutions[i].height == GameSettings.ResolutionHeight)
                current = i;
        }

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(current);
        resolutionDropdown.RefreshShownValue();
    }

    void BuildWindowModeList()
    {
        if (windowModeDropdown == null) return;
        windowModeDropdown.ClearOptions();
        windowModeDropdown.AddOptions(new List<string> { "Fullscreen", "Borderless Window", "Windowed" });
        windowModeDropdown.SetValueWithoutNotify(WindowModeToIndex(GameSettings.WindowMode));
        windowModeDropdown.RefreshShownValue();
    }

    void BuildQualityList()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        qualityDropdown.SetValueWithoutNotify(
            Mathf.Clamp(GameSettings.QualityLevel, 0, QualitySettings.names.Length - 1));
        qualityDropdown.RefreshShownValue();
    }

    // ---------- Sync ----------

    void PushToUI()
    {
        suppressCallbacks = true;

        float sens = GameSettings.MouseSensitivityX;
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(sens);
        if (sensitivityInput != null) sensitivityInput.SetTextWithoutNotify(Format(sens));
        if (invertYToggle != null) invertYToggle.SetIsOnWithoutNotify(GameSettings.InvertY);
        if (vsyncToggle != null) vsyncToggle.SetIsOnWithoutNotify(GameSettings.VSync);

        suppressCallbacks = false;
    }

    void HookEvents()
    {
        if (sensitivityInput != null)
        {
            sensitivityInput.onEndEdit.RemoveListener(OnSensitivityTyped);
            sensitivityInput.onEndEdit.AddListener(OnSensitivityTyped);
        }
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSensitivitySlider);
            sensitivitySlider.onValueChanged.AddListener(OnSensitivitySlider);
        }
    }

    static string Format(float v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    // ---------- Mouse ----------

    void OnSensitivityTyped(string text)
    {
        if (suppressCallbacks) return;

        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            value = GameSettings.MouseSensitivityX;   // bad input -> snap back

        value = Mathf.Clamp(value, GameSettings.MinSensitivity, GameSettings.MaxSensitivity);
        ApplySensitivity(value);
    }

    void OnSensitivitySlider(float value)
    {
        if (suppressCallbacks) return;
        ApplySensitivity(value);
    }

    void ApplySensitivity(float value)
    {
        GameSettings.SetSensitivity(value);
        if (invertYToggle != null) GameSettings.InvertY = invertYToggle.isOn;

        suppressCallbacks = true;
        if (sensitivityInput != null) sensitivityInput.SetTextWithoutNotify(Format(value));
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(value);
        suppressCallbacks = false;

        PlayerPrefs.Save();
        GameSettings.Apply();   // fires Changed so the live camera picks it up immediately
    }

    public void OnInvertYChanged(bool on)
    {
        GameSettings.InvertY = on;
        PlayerPrefs.Save();
        GameSettings.Apply();
    }

    // ---------- Apply / Back ----------

    /// <summary>Hook this to an "Apply" button.</summary>
    public void OnApply()
    {
        if (resolutionDropdown != null && resolutions.Count > 0)
        {
            var r = resolutions[Mathf.Clamp(resolutionDropdown.value, 0, resolutions.Count - 1)];
            GameSettings.ResolutionWidth = r.width;
            GameSettings.ResolutionHeight = r.height;
        }

        if (windowModeDropdown != null)
            GameSettings.WindowMode = IndexToWindowMode(windowModeDropdown.value);

        if (qualityDropdown != null)
            GameSettings.QualityLevel = qualityDropdown.value;

        if (vsyncToggle != null)
            GameSettings.VSync = vsyncToggle.isOn;

        GameSettings.Apply();
    }

    public void OnResetToDefaults()
    {
        GameSettings.SetSensitivity(1f);
        GameSettings.InvertY = false;
        GameSettings.VSync = true;
        GameSettings.QualityLevel = QualitySettings.names.Length - 1;
        GameSettings.WindowMode = FullScreenMode.FullScreenWindow;
        GameSettings.ResolutionWidth = Screen.currentResolution.width;
        GameSettings.ResolutionHeight = Screen.currentResolution.height;
        GameSettings.Apply();
        OnEnable();
    }

    static int WindowModeToIndex(FullScreenMode mode)
    {
        switch (mode)
        {
            case FullScreenMode.ExclusiveFullScreen: return 0;
            case FullScreenMode.FullScreenWindow: return 1;
            default: return 2;
        }
    }

    static FullScreenMode IndexToWindowMode(int index)
    {
        switch (index)
        {
            case 0: return FullScreenMode.ExclusiveFullScreen;
            case 1: return FullScreenMode.FullScreenWindow;
            default: return FullScreenMode.Windowed;
        }
    }
}