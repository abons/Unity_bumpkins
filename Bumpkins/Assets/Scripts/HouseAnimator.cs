using UnityEngine;

/// <summary>
/// Huis animatie:
/// - Deur (HouseDoor) gaat open als een bumpkin de trigger raakt, sluit daarna automatisch
/// Attach op de Visual child van het House gebouw.
/// </summary>
public class HouseAnimator : MonoBehaviour
{
    private SpriteRenderer _doorSr;

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        int baseSort = sr != null ? sr.sortingOrder : 0;

        // --- Deur overlay ---
        var doorSp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/HouseDoor");
        if (doorSp != null)
        {
            var doorGo = new GameObject("HouseDoor");
            doorGo.transform.SetParent(transform);
            doorGo.transform.localPosition = new Vector3(-0.248f, -0.194f, 0f);
            doorGo.transform.localScale    = new Vector3(0.5f, 0.5f, 1f);

            _doorSr = doorGo.AddComponent<SpriteRenderer>();
            _doorSr.sprite       = doorSp;
            _doorSr.sortingOrder = baseSort + 2;
            _doorSr.enabled      = false; // begint gesloten

            // Trigger op deurpositie op de root
            var rootGo = transform.parent?.gameObject;
            if (rootGo != null)
            {
                var rb = rootGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;

                var trigGo = new GameObject("DoorTrigger");
                trigGo.transform.SetParent(rootGo.transform);
                trigGo.transform.localPosition = new Vector3(-0.248f, -0.194f, 0f);

                var trig = trigGo.AddComponent<CircleCollider2D>();
                trig.isTrigger = true;
                trig.radius    = 0.3f;
                trigGo.AddComponent<HouseDoorTrigger>().house = this;
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

/// <summary>Kleine helper op de root collider van het House gebouw.</summary>
public class HouseDoorTrigger : MonoBehaviour
{
    public HouseAnimator house;

    void OnTriggerEnter2D(Collider2D other)
    {
        var bc = other.GetComponent<BumpkinController>()
              ?? other.GetComponentInParent<BumpkinController>();
        if (bc != null)
            house?.OpenDoor();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var bc = other.GetComponent<BumpkinController>()
              ?? other.GetComponentInParent<BumpkinController>();
        if (bc != null)
            house?.CloseDoor();
    }
}
