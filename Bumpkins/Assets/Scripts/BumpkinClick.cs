using UnityEngine;

/// <summary>
/// Attach to any Bumpkin GameObject.
/// Left-click → select the bumpkin.
/// </summary>
public class BumpkinClick : MonoBehaviour
{
    private BumpkinController _controller;

    void Awake() => _controller = GetComponent<BumpkinController>();

    void OnMouseDown()
    {
        SelectionManager.Instance?.Select(_controller);
    }
}
