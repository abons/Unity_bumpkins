/// <summary>
/// Persists the player's map choice across scene loads.
/// Static fields in C# survive Unity scene loads for the lifetime of the process,
/// so no DontDestroyOnLoad is needed.
///
/// Usage from a menu scene:
///   MapSelection.Select("Mission1Layout");
///   SceneManager.LoadScene("Game");
///
/// Valid names match the ScriptableObject asset name exactly:
///   "Mission1Layout", "Map2Layout", "EnemyTestLayout"
/// </summary>
public static class MapSelection
{
    /// <summary>
    /// Asset name of the chosen MapLayoutData, or null/empty to use the
    /// serialized default wired in the GridMapBuilder Inspector field.
    /// </summary>
    public static string SelectedLayoutName { get; private set; }

    /// <summary>
    /// Record the layout to load when the Game scene starts.
    /// Call this from your menu scene before loading "Game".
    /// </summary>
    public static void Select(string layoutName)
    {
        SelectedLayoutName = layoutName;
    }

    /// <summary>
    /// Clear the selection so GridMapBuilder falls back to its Inspector default.
    /// </summary>
    public static void Clear()
    {
        SelectedLayoutName = null;
    }
}
