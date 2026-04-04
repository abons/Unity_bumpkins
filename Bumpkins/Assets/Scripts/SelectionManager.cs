using UnityEngine;

/// <summary>
/// Tracks which bumpkin is currently selected.
/// Click a bumpkin to select it; then click ground/node/building to give orders.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    public BumpkinController SelectedBumpkin { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Select(BumpkinController b)
    {
        SelectedBumpkin = b;
        Debug.Log($"[Selection] Selected: {b.name} ({b.bumpkinType})");
    }

    public void Deselect()
    {
        SelectedBumpkin = null;
    }
}
