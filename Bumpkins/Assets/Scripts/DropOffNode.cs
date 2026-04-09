using UnityEngine;

/// <summary>
/// Attach to Mill or Dairy/Farm buildings.
/// When a bumpkin arrives and delivers, production is triggered exactly once.
/// </summary>
public class DropOffNode : MonoBehaviour
{
    public enum DropOffType { Mill, Dairy }

    [Header("Settings")]
    public DropOffType dropOffType = DropOffType.Mill;

    /// <summary>World-space offset from building root to the door position.</summary>
    public Vector2 doorOffset = Vector2.zero;

    /// <summary>Called by BumpkinController.OnReachedTarget when the bumpkin arrives.</summary>
    public void Deliver(BumpkinController bumpkin)
    {
        switch (dropOffType)
        {
            case DropOffType.Mill:
                int wheat = bumpkin.CarriedWheat;
                if (wheat <= 0)
                {
                    Debug.Log("[DropOff:Mill] Bumpkin has no wheat to deliver");
                    return;
                }
                // Clear carried wheat first, then process at mill
                bumpkin.PickUpWheat(-wheat);  // zero out
                GameManager.Instance.ProcessWheatAtMill(wheat);
                break;

            case DropOffType.Dairy:
                int milk = bumpkin.CarriedMilk;
                if (milk <= 0)
                {
                    Debug.Log("[DropOff:Dairy] Bumpkin has no milk to deliver");
                    return;
                }
                bumpkin.DropMilk();
                Debug.Log($"[DropOff:Dairy] Delivered {milk} milk. Total: {GameManager.Instance.Milk}");
                break;
        }
    }

    // --- Click to send selected bumpkin here ---
    void OnMouseDown()
    {
        var selected = SelectionManager.Instance?.SelectedBumpkin;
        if (selected != null)
            selected.AssignToDropOff(this);
    }
}
