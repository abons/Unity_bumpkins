using UnityEngine;

/// <summary>
/// Base class for any node that a bumpkin works at over time (wheat field, cow).
/// </summary>
public class ProductionNode : MonoBehaviour
{
    public enum NodeType { WheatField, Cow, Chicken }

    [Header("Node Settings")]
    public NodeType nodeType      = NodeType.WheatField;
    public int      yieldAmount   = 1;
    public float    workDuration  = 5f;
    [Tooltip("Offset van het node-centrum waar de bumpkin gaat staan")]
    public Vector2  workOffset    = new Vector2(0f, -0.2f);

    [Header("Wheat grow timer")]
    public float    growDuration     = 180f; // 3 minuten tot tarwe klaar is

    [Header("Cow cooldown")]
    public float    cowCooldown      = 120f; // 2 minuten voor volgende melkbeurt

    [Header("Visual")]
    public SpriteRenderer visualSpriteRenderer;   // gezet door GridMapBuilder

    [Header("Audio")]
    private AudioClip   _harvestClip;
    private AudioClip   _milkClip;
    private AudioSource _audioSource;

    [Header("State")]
    [SerializeField] private bool   _occupied;
    [SerializeField] private float  _workTimer;
    [SerializeField] private bool   _ready = false;
    [SerializeField] private float  _growTimer = 0f;

    private BumpkinController _worker;
    private Sprite _sprNotReady;
    private Sprite _sprReady;

    void Start()
    {
        _harvestClip             = Resources.Load<AudioClip>("Audio/harvest_wheat");
        _milkClip                = Resources.Load<AudioClip>("Audio/cow_milking");
        _audioSource             = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake  = false;
        _audioSource.spatialBlend = 0f;

        if (GameManager.Instance != null)
        {
            var cfg = GameManager.Instance.config;
            workDuration = nodeType == NodeType.Cow ? cfg.milkTickSeconds : cfg.harvestTickSeconds;
        }

        if (nodeType == NodeType.WheatField)
        {
            _sprNotReady = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/WheatField");
            _sprReady    = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/WheatField_grown");
            _ready       = false;
            UpdateVisual();
        }
        else if (nodeType == NodeType.Cow)
        {
            _ready = true; // Koe begint beschikbaar
        }
        else
        {
            _ready = true;
        }
    }

    void Update()
    {
        // Groei-timer voor tarwe
        if (nodeType == NodeType.WheatField && !_ready && !_occupied)
        {
            _growTimer += Time.deltaTime;
            if (_growTimer >= growDuration)
            {
                _growTimer = 0f;
                _ready     = true;
                UpdateVisual();
                Debug.Log($"[WheatField] {name} is klaar voor oogst!");
                TryAutoAssign();
            }
        }

        // Cooldown-timer voor koe
        if (nodeType == NodeType.Cow && !_ready && !_occupied)
        {
            _growTimer += Time.deltaTime;
            if (_growTimer >= cowCooldown)
            {
                _growTimer = 0f;
                _ready     = true;
                Debug.Log($"[Cow] {name} klaar voor volgende melkbeurt.");
                TryAutoAssign();
            }
        }

        // Werken
        if (!_occupied || _worker == null) return;
        _workTimer += Time.deltaTime;
        if (_workTimer >= workDuration)
        {
            _workTimer = 0f;
            ProduceYield();
        }
    }

    /// <summary>Zoek de dichtstbijzijnde idle bumpkin en stuur hem hiernaartoe.</summary>
    private void TryAutoAssign()
    {
        var bumpkins = FindObjectsByType<BumpkinController>(FindObjectsSortMode.None);
        BumpkinController nearest = null;
        float bestDist = float.MaxValue;
        foreach (var b in bumpkins)
        {
            if (b.CurrentState != "Idle") continue;
            if (!CanBeWorkedBy(b)) continue;
            float d = Vector2.Distance(b.transform.position, transform.position);
            if (d < bestDist) { bestDist = d; nearest = b; }
        }
        if (nearest != null)
        {
            Debug.Log($"[AutoAssign] {nearest.name} → {name}");
            nearest.AssignToNode(this);
        }
    }

    public bool CanBeWorkedBy(BumpkinController b)
    {
        if (nodeType == NodeType.WheatField && !_ready)
            return false;
        if (nodeType == NodeType.Cow && !_ready)
            return false;
        if (nodeType == NodeType.Cow && b.IsMale)
            return false;
        if (_occupied)
            return false;
        return true;
    }

    /// <summary>Reserveer de node zodra een bumpkin op weg gaat. Voorkomt dubbele toewijzing.</summary>
    public bool TryReserve(BumpkinController b)
    {
        if (!CanBeWorkedBy(b)) return false;
        _occupied = true;   // direct blokkeren
        return true;
    }

    public void StartWork(BumpkinController worker)
    {
        _worker    = worker;
        _occupied  = true;
        _workTimer = 0f;
        Debug.Log($"[Node:{nodeType}] Work started by {worker.bumpkinType}");
        if (nodeType == NodeType.Cow)
        {
            GetComponentInChildren<CowAnimator>()?.SetMilking(true);
            if (_milkClip != null) { _audioSource.clip = _milkClip; _audioSource.loop = true; _audioSource.Play(); }
        }
        else if (nodeType == NodeType.WheatField)
        {
            if (_harvestClip != null) { _audioSource.clip = _harvestClip; _audioSource.loop = true; _audioSource.Play(); }
        }
    }

    private void ProduceYield()
    {
        _audioSource.Stop();

        switch (nodeType)
        {
            case NodeType.WheatField:
                _worker.PickUpWheat(yieldAmount);
                // Reset veld
                _ready     = false;
                _growTimer = 0f;
                _occupied  = false;
                UpdateVisual();
                Debug.Log($"[Node:Wheat] Geoogst. Bumpkin loopt naar Mill.");
                // Auto-walk naar dichtstbijzijnde Mill
                var mill = _worker.FindNearestDropOff(DropOffNode.DropOffType.Mill);
                if (mill != null)
                    _worker.AssignToDropOff(mill);
                else
                    _worker.DropWheat(); // fallback: direct dumpen
                _worker = null;
                break;

            case NodeType.Cow:
                _worker.PickUpMilk(yieldAmount);
                _ready    = false;   // koe op cooldown
                _growTimer = 0f;
                _occupied = false;
                GetComponentInChildren<CowAnimator>()?.SetMilking(false);
                Debug.Log($"[Node:Cow] Gemolken. Female loopt naar Farm. Koe cooldown {cowCooldown}s.");
                var dairy = _worker.FindNearestDropOff(DropOffNode.DropOffType.Dairy);
                if (dairy != null)
                    _worker.AssignToDropOff(dairy);
                else
                    _worker.DropMilk(); // fallback
                _worker = null;
                break;

            case NodeType.Chicken:
                GameManager.Instance.AddEgg();
                break;
        }
    }

    private void UpdateVisual()
    {
        if (visualSpriteRenderer == null) return;
        var sp = _ready ? _sprReady : _sprNotReady;
        if (sp != null) visualSpriteRenderer.sprite = sp;
    }

    public void Release()
    {
        _worker   = null;
        _occupied = false;
    }

    void OnMouseDown()
    {
        var selected = SelectionManager.Instance?.SelectedBumpkin;
        if (selected != null)
            selected.AssignToNode(this);
    }
}
