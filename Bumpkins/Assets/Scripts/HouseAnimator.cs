using UnityEngine;

/// <summary>
/// Huis animatie:
/// - Deur (HouseDoor) gaat open als een bumpkin de trigger raakt, sluit daarna automatisch
/// Attach op de Visual child van het House gebouw.
/// </summary>
public class HouseAnimator : MonoBehaviour
{
    private SpriteRenderer _doorSr;
    private AudioClip _clipOpen;
    private AudioClip _clipClose;
    private AudioSource _audioSource;

    void Start()
    {
        _clipOpen  = Resources.Load<AudioClip>("Audio/door_open");
        _clipClose = Resources.Load<AudioClip>("Audio/door_close");
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;

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
        if (_clipOpen != null) _audioSource.PlayOneShot(_clipOpen);
    }

    public void CloseDoor()
    {
        if (_doorSr != null) _doorSr.enabled = false;
        if (_clipClose != null) _audioSource.PlayOneShot(_clipClose);
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
