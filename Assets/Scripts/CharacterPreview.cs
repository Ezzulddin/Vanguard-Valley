using UnityEngine;

/// <summary>
/// Optional hook for the 3D model standing in the creation scene.
/// Swap the guts of Render() for your own mesh/material logic later.
/// </summary>
public class CharacterPreview : MonoBehaviour
{
    [SerializeField] Renderer bodyRenderer;
    [SerializeField] GameObject[] hairStyles;

    public void Render(CharacterData data)
    {
        if (bodyRenderer != null)
        {
            var skin = Color.Lerp(new Color(0.35f, 0.24f, 0.17f), new Color(1f, 0.87f, 0.77f), data.skinTone);
            bodyRenderer.material.color = skin;
        }

        if (hairStyles != null)
        {
            for (int i = 0; i < hairStyles.Length; i++)
                if (hairStyles[i] != null) hairStyles[i].SetActive(i == data.hairStyle);
        }
    }
}