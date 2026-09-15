using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>One row in the saved-character list. Make this a prefab.</summary>
public class CharacterSlotUI : MonoBehaviour
{
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] TMP_Text subLabel;      // optional: created date, level, etc.
    [SerializeField] Button playButton;
    [SerializeField] Button editButton;
    [SerializeField] Button deleteButton;

    public void Bind(CharacterData data,
                     Action<CharacterData> onPlay,
                     Action<CharacterData> onEdit,
                     Action<CharacterData> onDelete)
    {
        if (nameLabel != null) nameLabel.text = data.displayName;

        if (subLabel != null)
        {
            subLabel.text = DateTime.TryParse(data.createdUtc, out var created)
                ? $"Created {created.ToLocalTime():dd MMM yyyy}"
                : string.Empty;
        }

        Wire(playButton, () => onPlay?.Invoke(data));
        Wire(editButton, () => onEdit?.Invoke(data));
        Wire(deleteButton, () => onDelete?.Invoke(data));
    }

    static void Wire(Button button, Action action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => action());
    }
}