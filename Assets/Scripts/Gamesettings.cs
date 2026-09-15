using System;
using UnityEngine;

/// <summary>
/// All player-facing settings. Stored in PlayerPrefs, applied at startup before any
/// scene loads, and broadcast via Changed so live objects (like the camera) can react.
/// </summary>
public static class GameSettings
{
    public static event Action Changed;

    const string K_SensX = "set.sensX";
    const string K_SensY = "set.sensY";
    const string K_Width = "set.width";
    const string K_Height = "set.height";
    const string K_Window = "set.windowMode";
    const string K_Quality = "set.quality";
    const string K_VSync = "set.vsync";
    const string K_InvertY = "set.invertY";

    public const float MinSensitivity = 0.05f;
    public const float MaxSensitivity = 20f;

    // ---------- Mouse ----------
    public static float MouseSensitivityX
    {
        get => PlayerPrefs.GetFloat(K_SensX, 1f);
        set => PlayerPrefs.SetFloat(K_SensX, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));
    }

    public static float MouseSensitivityY
    {
        get => PlayerPrefs.GetFloat(K_SensY, 1f);
        set => PlayerPrefs.SetFloat(K_SensY, Mathf.Clamp(value, MinSensitivity, MaxSensitivity));
    }

    public static bool InvertY
    {
        get => PlayerPrefs.GetInt(K_InvertY, 0) == 1;
        set => PlayerPrefs.SetInt(K_InvertY, value ? 1 : 0);
    }

    /// <summary>Convenience for a single sensitivity field that drives both axes.</summary>
    public static void SetSensitivity(float value)
    {
        MouseSensitivityX = value;
        MouseSensitivityY = value;
    }

    // ---------- Display ----------
    public static int ResolutionWidth
    {
        get => PlayerPrefs.GetInt(K_Width, Screen.currentResolution.width);
        set => PlayerPrefs.SetInt(K_Width, value);
    }

    public static int ResolutionHeight
    {
        get => PlayerPrefs.GetInt(K_Height, Screen.currentResolution.height);
        set => PlayerPrefs.SetInt(K_Height, value);
    }

    public static FullScreenMode WindowMode
    {
        get => (FullScreenMode)PlayerPrefs.GetInt(K_Window, (int)FullScreenMode.FullScreenWindow);
        set => PlayerPrefs.SetInt(K_Window, (int)value);
    }

    // ---------- Graphics (placeholder for now) ----------
    public static int QualityLevel
    {
        get => PlayerPrefs.GetInt(K_Quality, QualitySettings.GetQualityLevel());
        set => PlayerPrefs.SetInt(K_Quality, value);
    }

    public static bool VSync
    {
        get => PlayerPrefs.GetInt(K_VSync, 1) == 1;
        set => PlayerPrefs.SetInt(K_VSync, value ? 1 : 0);
    }

    // ---------- Apply / persist ----------
    public static void Apply()
    {
        QualitySettings.SetQualityLevel(Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1), true);
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Screen.SetResolution(ResolutionWidth, ResolutionHeight, WindowMode);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyOnBoot()
    {
        // Don't fight the editor's Game view resolution.
        if (Application.isEditor)
        {
            QualitySettings.SetQualityLevel(Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1), true);
            return;
        }
        Apply();
    }
}