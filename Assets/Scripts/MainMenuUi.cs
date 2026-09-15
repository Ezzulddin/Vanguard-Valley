using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

// UnityEngine.UI and UnityEngine.UIElements both define Button/Slider/Toggle/Image.
// These aliases pin us to the old uGUI ones and kill the CS0104 ambiguity error.
using Button = UnityEngine.UI.Button;

/// <summary>
/// Drives the title screen. Put this on an empty "MenuController" object in your
/// MainMenu scene and wire the buttons' OnClick to the public methods.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Scenes (must be in Build Settings)")]
    [SerializeField] string gameScene = "Game";
    [SerializeField] string characterCreationScene = "CharacterCreation";

    [Header("Panels")]
    [SerializeField] GameObject mainPanel;
    [SerializeField] GameObject optionsPanel;
    [SerializeField] GameObject characterSelectPanel;

    [Header("Character select")]
    [SerializeField] Transform slotContainer;      // the Content of a Scroll View / Vertical Layout Group
    [SerializeField] CharacterSlotUI slotPrefab;

    [Header("Main panel bits")]
    [SerializeField] TMP_Text startButtonLabel;    // optional: shows "Continue as <name>"
    [SerializeField] Button quitButton;

    void Start()
    {
        RefreshStartLabel();
        ShowMain();

#if UNITY_WEBGL
        if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void RefreshStartLabel()
    {
        if (startButtonLabel == null) return;
        var c = SaveSystem.GetActiveOrFirst();
        startButtonLabel.text = c != null ? $"Continue as {c.displayName}" : "Start Game";
    }

    // ---------- Buttons ----------

    /// <summary>Straight into the game if a character exists, otherwise into the creator.</summary>
    public void OnStartGame()
    {
        var character = SaveSystem.GetActiveOrFirst();
        if (character == null)
        {
            CreationContext.EditCharacterId = null;
            SceneManager.LoadScene(characterCreationScene);
            return;
        }

        SaveSystem.SetActive(character.id);
        SceneManager.LoadScene(gameScene);
    }

    /// <summary>
    /// No characters yet -> go straight to the creator.
    /// Characters exist -> show them first, then let the player add another.
    /// Existing saves are never touched.
    /// </summary>
    public void OnNewGame()
    {
        if (!SaveSystem.HasAnyCharacter)
        {
            CreationContext.EditCharacterId = null;
            SceneManager.LoadScene(characterCreationScene);
            return;
        }
        ShowCharacterSelect();
    }

    public void OnCreateAnotherCharacter()
    {
        CreationContext.EditCharacterId = null;   // null = brand new, existing ones stay saved
        SceneManager.LoadScene(characterCreationScene);
    }

    public void OnOptions() => ShowOptions();

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------- Panels ----------

    public void ShowMain()
    {
        RefreshStartLabel();
        SetPanels(main: true, options: false, select: false);
    }

    public void ShowOptions() => SetPanels(main: false, options: true, select: false);

    public void ShowCharacterSelect()
    {
        SetPanels(main: false, options: false, select: true);
        BuildCharacterList();
    }

    void SetPanels(bool main, bool options, bool select)
    {
        if (mainPanel != null) mainPanel.SetActive(main);
        if (optionsPanel != null) optionsPanel.SetActive(options);
        if (characterSelectPanel != null) characterSelectPanel.SetActive(select);
    }

    void BuildCharacterList()
    {
        if (slotContainer == null || slotPrefab == null) return;

        for (int i = slotContainer.childCount - 1; i >= 0; i--)
            Destroy(slotContainer.GetChild(i).gameObject);

        foreach (var character in SaveSystem.Db.characters)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            slot.Bind(
                character,
                onPlay: c =>
                {
                    SaveSystem.SetActive(c.id);
                    SceneManager.LoadScene(gameScene);
                },
                onEdit: c =>
                {
                    CreationContext.EditCharacterId = c.id;
                    SceneManager.LoadScene(characterCreationScene);
                },
                onDelete: c =>
                {
                    SaveSystem.Delete(c.id);
                    BuildCharacterList();
                });
        }
    }
}