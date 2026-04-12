using UnityEngine;

/// <summary>
/// Toolshed deur animatie:
/// - Deur (ToolshedDoor) gaat open als een bumpkin de trigger raakt, sluit daarna automatisch
/// Attach op de Visual child van het Toolshed gebouw.
/// </summary>
public class ToolshedAnimator : MonoBehaviour
{
    private SpriteRenderer _doorSr;
    private AudioSource    _audioSource;

    void Start()
    {
        _audioSource          = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake  = false;
        _audioSource.spatialBlend = 0f;

        var sr = GetComponent<SpriteRenderer>();
        int baseSort = sr != null ? sr.sortingOrder : 0;

        // --- Deur overlay ---
        var doorSp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/ToolshedDoor");
        if (doorSp != null)
        {
            var doorGo = new GameObject("ToolshedDoor");
            doorGo.transform.SetParent(transform);
            doorGo.transform.localPosition = new Vector3(0.204f, -0.447f, 0f);
            doorGo.transform.localScale    = new Vector3(1f, 1f, 1f);

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

                var trigGo = new GameObject("ToolshedDoorTrigger");
                trigGo.transform.SetParent(rootGo.transform);
                trigGo.transform.localPosition = new Vector3(0.204f, -0.447f, 0f);

                var trig = trigGo.AddComponent<CircleCollider2D>();
                trig.isTrigger = true;
                trig.radius    = 0.3f;
                trigGo.AddComponent<ToolshedDoorTrigger>().toolshed = this;
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

    public void StartSaw() { }
    public void StopSaw()  { }
}

/// <summary>Kleine helper op de root collider van het Toolshed gebouw.</summary>
public class ToolshedDoorTrigger : MonoBehaviour
{
    public ToolshedAnimator toolshed;

    void OnTriggerEnter2D(Collider2D other)
    {
        var bc = other.GetComponent<BumpkinController>()
              ?? other.GetComponentInParent<BumpkinController>();
        if (bc != null)
            toolshed?.OpenDoor();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var bc = other.GetComponent<BumpkinController>()
              ?? other.GetComponentInParent<BumpkinController>();
        if (bc != null)
            toolshed?.CloseDoor();
    }
}
