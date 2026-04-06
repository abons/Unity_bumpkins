using UnityEngine;

/// <summary>
/// Molen animatie:
/// - Wieken (MillSails) roteren continu
/// - Deur (MillDoor) gaat open als een bumpkin de trigger raakt, sluit daarna automatisch
/// Attach op de Visual child van het Mill gebouw.
/// </summary>
public class MillAnimator : MonoBehaviour
{
    [Header("Wieken")]
    public float sailSpeed = 45f;   // graden per seconde

    [Header("Deur")]
    public float doorOffset = 0.0f;  // Y-offset van deur t.o.v. molen-center

    private Transform _sailTransform;
    private SpriteRenderer _doorSr;

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        int baseSort = sr != null ? sr.sortingOrder : 0;

        // --- Wieken overlay ---
        // bakeanim = bovenste deel molen + wieken als één sprite → draaien als geheel
        var sailSp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/MillSails");
        if (sailSp != null)
        {
            var sailGo = new GameObject("MillSails");
            sailGo.transform.SetParent(transform);

            // Schaal zodat breedte overeenkomt met de molen
            var baseSp = sr?.sprite;
            float targetW = baseSp != null
                ? (transform.localScale.x * baseSp.bounds.size.x * 0.55f)
                : transform.localScale.x * 0.5f;
            float scale = targetW / sailSp.bounds.size.x;
            // Positioneer bovenaan de molen sprite
            sailGo.transform.localPosition = new Vector3(0.6f, 0.4f, 0f);
            sailGo.transform.localScale    = new Vector3(1f, 1f, 1f);

            var sailSr = sailGo.AddComponent<SpriteRenderer>();
            sailSr.sprite       = sailSp;
            sailSr.sortingOrder = baseSort + 1;
            _sailTransform      = sailGo.transform;
        }

        // --- Deur overlay ---
        var doorSp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/MillDoor");
        if (doorSp != null)
        {
            var doorGo = new GameObject("MillDoor");
            doorGo.transform.SetParent(transform);

            float targetH = transform.localScale.y * 0.25f;
            float scale   = targetH / doorSp.bounds.size.y;
            doorGo.transform.localPosition = new Vector3(0.69f, -0.36f, 0f);
            doorGo.transform.localScale    = new Vector3(1f, 1f, 1f);

            _doorSr = doorGo.AddComponent<SpriteRenderer>();
            _doorSr.sprite       = doorSp;
            _doorSr.sortingOrder = baseSort + 2;
            _doorSr.enabled      = false; // begint gesloten

            // Trigger collider voor bumpkin detectie op de root
            // Trigger op deurpositie (niet op centrum molen)
            var rootGo = transform.parent?.gameObject;
            if (rootGo != null)
            {
                var rb = rootGo.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;

                // Aparte trigger-child op de deurpositie
                var trigGo = new GameObject("DoorTrigger");
                trigGo.transform.SetParent(rootGo.transform);
                trigGo.transform.localPosition = new Vector3(0.69f, -0.36f, 0f);

                var trig = trigGo.AddComponent<CircleCollider2D>();
                trig.isTrigger = true;
                trig.radius    = 0.01f;
                trigGo.AddComponent<MillDoorTrigger>().mill = this;
            }
        }
    }

    void Update()
    {
        // Wieken: uitgeschakeld voor nu
        // if (_sailTransform != null)
        //     _sailTransform.Rotate(0f, 0f, sailSpeed * Time.deltaTime);

        // Deur timer
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

/// <summary>Kleine helper op de root collider van het Mill gebouw.</summary>
public class MillDoorTrigger : MonoBehaviour
{
    public MillAnimator mill;

    void OnTriggerEnter2D(Collider2D other)
    {
        var bc = other.GetComponent<BumpkinController>()
              ?? other.GetComponentInParent<BumpkinController>();
        if (bc != null)
            mill?.OpenDoor();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var bc = other.GetComponent<BumpkinController>()
              ?? other.GetComponentInParent<BumpkinController>();
        if (bc != null)
            mill?.CloseDoor();
    }
}
