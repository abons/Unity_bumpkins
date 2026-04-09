using UnityEngine;

/// <summary>
/// Marker op gebouwen die bumpkins als idle-doel mogen gebruiken.
/// enterable = true → bumpkin loopt erdoorheen en "verdwijnt" even in het gebouw.
/// Kan gereserveerd worden voor de makeBaby-actie (alleen als isHouse = true).
/// </summary>
public class BuildingTag : UnityEngine.MonoBehaviour
{
    public bool enterable  = true;
    public bool isHouse    = false;
    public bool isToolshed = false;
    /// <summary>World-space offset from this root to the walkable door-exit tile.</summary>
    public Vector2 doorOffset = Vector2.zero;

    public bool IsReservedForBaby { get; private set; }

    public bool TryReserveForBaby()
    {
        if (IsReservedForBaby) return false;
        IsReservedForBaby = true;
        return true;
    }

    public void ReleaseReservation() => IsReservedForBaby = false;
}
