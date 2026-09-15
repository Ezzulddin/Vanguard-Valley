using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Put this on the same GameObject as your Cinemachine Input Axis Controller
/// (Camera2 in your setup). It remembers the gains you set in the Inspector and
/// multiplies them by the player's saved sensitivity whenever it changes.
/// </summary>
[RequireComponent(typeof(CinemachineInputAxisController))]
public class CameraSensitivity : MonoBehaviour
{
    [Tooltip("Axis names exactly as they appear under Driven Axes in the Inspector.")]
    [SerializeField] string horizontalAxisName = "Look Orbit X";
    [SerializeField] string verticalAxisName = "Look Orbit Y";

    [Tooltip("Baseline gain used if the Inspector gain is left at 0.")]
    [SerializeField] float fallbackHorizontalGain = 1f;
    [SerializeField] float fallbackVerticalGain = -1f;

    CinemachineInputAxisController controller;
    readonly Dictionary<string, float> baseGains = new Dictionary<string, float>();

    void Awake()
    {
        controller = GetComponent<CinemachineInputAxisController>();
        CacheBaseGains();
    }

    void CacheBaseGains()
    {
        baseGains.Clear();
        foreach (var c in controller.Controllers)
        {
            float gain = c.Input.Gain;
            if (Mathf.Approximately(gain, 0f))
            {
                if (c.Name == horizontalAxisName) gain = fallbackHorizontalGain;
                else if (c.Name == verticalAxisName) gain = fallbackVerticalGain;
            }
            baseGains[c.Name] = gain;
        }
    }

    void OnEnable()
    {
        GameSettings.Changed += ApplySensitivity;
        ApplySensitivity();
    }

    void OnDisable()
    {
        GameSettings.Changed -= ApplySensitivity;
    }

    public void ApplySensitivity()
    {
        if (controller == null) return;

        float sensX = GameSettings.MouseSensitivityX;
        float sensY = GameSettings.MouseSensitivityY * (GameSettings.InvertY ? -1f : 1f);

        foreach (var c in controller.Controllers)
        {
            if (!baseGains.TryGetValue(c.Name, out float baseGain)) continue;

            if (c.Name == horizontalAxisName)
                c.Input.Gain = baseGain * sensX;
            else if (c.Name == verticalAxisName)
                c.Input.Gain = baseGain * sensY;
        }
    }
}