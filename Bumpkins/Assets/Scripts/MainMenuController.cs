using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Renders an OnGUI start menu with map selection buttons.
/// Attach this to a GameObject in the MainMenu scene.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialised;

    private void InitStyles()
    {
        if (stylesInitialised) return;

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            fixedHeight = 60
        };

        stylesInitialised = true;
    }

    private void OnGUI()
    {
        InitStyles();

        float w = Screen.width;
        float h = Screen.height;

        float panelWidth = 420f;
        float panelX = (w - panelWidth) / 2f;

        // Title
        GUI.Label(new Rect(0, h * 0.15f, w, 80), "Beasts & Bumpkins", titleStyle);

        bool hasSave = !string.IsNullOrEmpty(MapSelection.SelectedLayoutName);

        // Continue button (only shown when a map was already loaded)
        float btnY = h * 0.32f;
        float btnSpacing = 80f;
        int offset = 0;

        if (hasSave)
        {
            if (GUI.Button(new Rect(panelX, btnY, panelWidth, 60), $"Doorgaan  ({MapSelection.SelectedLayoutName})", buttonStyle))
                SceneManager.LoadScene("Game");
            offset = 1;
        }

        // Map buttons
        if (GUI.Button(new Rect(panelX, btnY + btnSpacing * offset, panelWidth, 60), "Mission 1", buttonStyle))
            LoadMap("Mission1Layout");

        if (GUI.Button(new Rect(panelX, btnY + btnSpacing * (offset + 1), panelWidth, 60), "Map 2", buttonStyle))
            LoadMap("Map2Layout");

        if (GUI.Button(new Rect(panelX, btnY + btnSpacing * (offset + 2), panelWidth, 60), "Enemy Test", buttonStyle))
            LoadMap("EnemyTestLayout");

        // Quit button
        if (GUI.Button(new Rect(panelX, btnY + btnSpacing * (offset + 3.5f), panelWidth, 60), "Quit", buttonStyle))
            Application.Quit();
    }

    private void LoadMap(string layoutName)
    {
        MapSelection.Select(layoutName);
        SceneManager.LoadScene("Game");
    }
}
