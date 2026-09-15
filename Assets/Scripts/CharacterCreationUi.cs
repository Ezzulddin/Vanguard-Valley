using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

using Slider = UnityEngine.UI.Slider;

/// <summary>
/// Character customisation screen. Creates a new character, or edits an existing one
/// if CreationContext.EditCharacterId was set before the scene loaded.
/// Saving a new character never removes the old ones.
/// </summary>
public class CharacterCreationUI : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] string gameScene = "Game";
    [SerializeField] string mainMenuScene = "MainMenu";

    [Header("UI")]
    [SerializeField] TMP_InputField nameField;
    [SerializeField] TMP_Dropdown bodyTypeDropdown;
    [SerializeField] TMP_Dropdown hairStyleDropdown;
    [SerializeField] Slider skinToneSlider;
    [SerializeField] TMP_Text headerLabel;
    [SerializeField] TMP_Text warningLabel;

    [Header("Preview (optional)")]
    [SerializeField] CharacterPreview preview;

    CharacterData working;

    void Start()
    {
        var existing = string.IsNullOrEmpty(CreationContext.EditCharacterId)
            ? null
            : SaveSystem.Db.characters.Find(c => c.id == CreationContext.EditCharacterId);

        if (existing != null)
        {
            // Edit a copy so cancelling leaves the save untouched.
            working = JsonUtility.FromJson<CharacterData>(JsonUtility.ToJson(existing));
            if (headerLabel != null) headerLabel.text = "Edit Character";
        }
        else
        {
            working = CharacterData.CreateNew();
            working.displayName = $"Character {SaveSystem.Db.characters.Count + 1}";
            if (headerLabel != null) headerLabel.text = "Create Your Character";
        }

        PushToUI();
        HookEvents();
        UpdatePreview();
    }

    void PushToUI()
    {
        if (nameField != null) nameField.text = working.displayName;
        if (bodyTypeDropdown != null) bodyTypeDropdown.value = working.bodyType;
        if (hairStyleDropdown != null) hairStyleDropdown.value = working.hairStyle;
        if (skinToneSlider != null) skinToneSlider.value = working.skinTone;
    }

    void HookEvents()
    {
        if (nameField != null)
            nameField.onValueChanged.AddListener(v => { working.displayName = v; ClearWarning(); });

        if (bodyTypeDropdown != null)
            bodyTypeDropdown.onValueChanged.AddListener(v => { working.bodyType = v; UpdatePreview(); });

        if (hairStyleDropdown != null)
            hairStyleDropdown.onValueChanged.AddListener(v => { working.hairStyle = v; UpdatePreview(); });

        if (skinToneSlider != null)
            skinToneSlider.onValueChanged.AddListener(v => { working.skinTone = v; UpdatePreview(); });
    }

    void UpdatePreview()
    {
        if (preview != null) preview.Render(working);
    }

    // ---------- Buttons ----------

    public void OnConfirm()
    {
        var trimmed = (working.displayName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            if (warningLabel != null) warningLabel.text = "Give your character a name.";
            return;
        }
        working.displayName = trimmed;

        SaveSystem.AddOrUpdate(working, makeActive: true);
        CreationContext.EditCharacterId = null;
        SceneManager.LoadScene(gameScene);
    }

    public void OnSaveAndReturnToMenu()
    {
        working.displayName = (working.displayName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(working.displayName)) working.displayName = "Unnamed";

        SaveSystem.AddOrUpdate(working, makeActive: true);
        CreationContext.EditCharacterId = null;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OnCancel()
    {
        CreationContext.EditCharacterId = null;
        SceneManager.LoadScene(mainMenuScene);
    }

    void ClearWarning()
    {
        if (warningLabel != null) warningLabel.text = string.Empty;
    }
}