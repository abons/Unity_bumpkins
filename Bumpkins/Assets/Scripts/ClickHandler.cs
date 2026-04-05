using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Centrale click handler voor alles in de scene.
/// Vervangt OnMouseDown op BumpkinClick, ProductionNode en DropOffNode.
/// Attach to een GameObject "ClickHandler" in de scene.
/// </summary>
public class ClickHandler : MonoBehaviour
{
    private Camera _cam;

    void Start() => _cam = Camera.main;

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // ---- Build mode intercept ----
        var bm = BuildManager.Instance;
        if (bm != null && bm.InBuildMode)
        {
            if (mouse.rightButton.wasPressedThisFrame)
            {
                bm.ExitBuildMode();
                return;
            }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (UIManager.IsPointerOverGUI)
                {
                    Debug.Log("[Build] klik geblokkeerd door GUI");
                    return;
                }
                Vector2 buildPos = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
                bm.HandleBuildClick(buildPos);
            }
            return;  // blokkeer normale klik-logica in build mode
        }

        // Rechtermuis → deselect
        if (mouse.rightButton.wasPressedThisFrame)
        {
            SelectionManager.Instance?.Deselect();
            return;
        }

        if (!mouse.leftButton.wasPressedThisFrame) return;

        // Niet doorklikken als de muis over een GUI-element staat
        if (UIManager.IsPointerOverGUI) return;

        Vector2 worldPos = _cam.ScreenToWorldPoint(mouse.position.ReadValue());

        // Alle colliders op deze positie
        var hit = Physics2D.Raycast(worldPos, Vector2.zero);
        if (hit.collider == null)
        {
            // Klik op lege ruimte → stuur geselecteerde bumpkin erheen
            SelectionManager.Instance?.SelectedBumpkin?.MoveTo(worldPos);
            return;
        }

        var go = hit.collider.gameObject;

        // Bumpkin aangeklikt → selecteer
        var bumpkin = go.GetComponent<BumpkinController>();
        if (bumpkin != null)
        {
            if (!bumpkin.IsDead)
                SelectionManager.Instance?.Select(bumpkin);
            return;
        }

        // ProductionNode aangeklikt → stuur geselecteerde bumpkin erheen
        var node = go.GetComponent<ProductionNode>();
        if (node != null)
        {
            SelectionManager.Instance?.SelectedBumpkin?.AssignToNode(node);
            return;
        }

        // DropOffNode aangeklikt → stuur geselecteerde bumpkin erheen
        var dropOff = go.GetComponent<DropOffNode>();
        if (dropOff != null)
        {
            SelectionManager.Instance?.SelectedBumpkin?.AssignToDropOff(dropOff);
            return;
        }

        // ConstructionSite aangeklikt → stuur geselecteerde male bumpkin erheen
        var site = go.GetComponent<ConstructionSite>();
        if (site != null && site.CanBeWorked)
        {
            var sel = SelectionManager.Instance?.SelectedBumpkin;
            if (sel != null && sel.IsMale)
            {
                var cell = site.TryReserveWorker(sel);
                if (cell != null)
                    sel.AssignToConstruction(site, cell);
            }
            return;
        }

        // Iets anders aangeklikt (terrain tile etc.) → beweeg erheen
        SelectionManager.Instance?.SelectedBumpkin?.MoveTo(worldPos);
    }
}
