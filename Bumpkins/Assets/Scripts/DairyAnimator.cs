using UnityEngine;

/// <summary>
/// Dairy deur animatie:
/// - Deur (DairyDoor) gaat open als een bumpkin de trigger raakt, sluit daarna automatisch
/// Attach op de Visual child van het Dairy gebouw.
/// </summary>
public class DairyAnimator : MonoBehaviour
{
    private SpriteRenderer _doorSr;

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        int baseSort = sr != null ? sr.sortingOrder : 0;

        // --- Deur overlay ---
        var doorSp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/DairyDoor");
        if (doorSp != null)
        {
            var doorGo = new GameObject("DairyDoor");
            doorGo.transform.SetParent(transform);
            doorGo.transform.localPosition = new Vector3(0.42f, 0.384f, 0f);
            doorGo.transform.localScale    = new Vector3(1f, 1f, 1f);

            _doorSr = doorGo.AddComponent<SpriteRenderer>();
            _doorSr.sprite       = doorSp;
            _doorSr.sortingOrder = baseSort + 2;
            _doorSr.enabled      = false; // begin closed

            // Trigger op deurpositie op de root
            var rootGo = transform.parent?.gameObject;
            if (rootGo != null)
            {
                var rb = rootGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;

                var trigGo = new GameObject("DairyDoorTrigger");
                trigGo.transform.SetParent(rootGo.transform);
                trigGo.transform.localPosition = new Vector3(0f, 0f, 0f);

                var trig = trigGo.AddComponent<CircleCollider2D>();
                trig.isTrigger = true;
                trig.radius    = 0.3f;
                trigGo.AddComponent<DairyDoorTrigger>().dairy = this;
            }
        }
    }

    public void OpenDoor()
    {
        if (_doorSr != null) _doorSr.enabled = true;
    }

    public void CloseDoor()
    {
        if (_doorSr != null) _doorSr.enabled = false;
    }
}

/// <summary>Kleine helper op de root collider van het Dairy gebouw.</summary>
public class DairyDoorTrigger : MonoBehaviour
{
    public DairyAnimator dairy;

    void OnTriggerEnter2D(Collider2D other)
    {
        var bc = other.GetComponent<BumpkinController>()
              ?? other.GetComponentInParent<BumpkinController>();
        if (bc != null)
            dairy?.OpenDoor();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var bc = other.GetComponent<BumpkinController>()
              ?? other.GetComponentInParent<BumpkinController>();
        if (bc != null)
            dairy?.CloseDoor();
    }
}
