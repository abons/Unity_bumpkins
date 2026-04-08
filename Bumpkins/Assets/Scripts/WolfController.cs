using System.Collections;
using UnityEngine;

/// <summary>
/// Wolf state machine: Roaming → Hunting → Attacking → Dead.
/// Roaming: random movement across the map.
/// Hunting:  moves toward nearest bumpkin or cow within huntRadius.
/// Attacking: plays attack sprite, kills target after 2s, then dies.
/// Dead: death sprite shown for 1s, then destroyed.
/// </summary>
public class WolfController : MonoBehaviour
{
    [Header("Radii")]
    public float huntRadius   = 3f;
    public float attackRadius = 0.8f;

    [Header("Speed")]
    public float moveSpeed  = 2f;
    public float chaseSpeed = 3.5f;

    private enum State { Roaming, Hunting, Attacking, Dead }
    private State _state = State.Roaming;

    private SpriteRenderer _sr;
    private Transform       _wolfVisual;
    private Sprite _sprWolf;
    private Sprite _sprStill;
    private Sprite _sprAttack;
    private Sprite _sprDead;

    // Walk animation
    private Sprite[] _walkSprites;
    private int      _walkFrame;
    private float    _walkTimer;
    private float    _walkBobTimer;
    [SerializeField] private int   _walkFps          = 4;
    [SerializeField] private float _walkBobAmplitude = 0.03f;
    [SerializeField] private float _walkBobFrequency = 4f;

    private Transform _target;
    private Vector2   _roamTarget;
    private float     _roamWaitTimer;

    void Start()
    {
        // Create Visual child so the root position stays clean for movement/sort;
        // we bob the Visual child, never the root.
        var visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(transform);
        visualGo.transform.localPosition = Vector3.zero;
        visualGo.transform.localScale    = Vector3.one;
        _wolfVisual = visualGo.transform;

        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            var newSr = visualGo.AddComponent<SpriteRenderer>();
            newSr.sprite       = _sr.sprite;
            newSr.color        = _sr.color;
            newSr.sortingOrder = _sr.sortingOrder;
            Destroy(_sr);
            _sr = newSr;
        }
        else
        {
            _sr = visualGo.AddComponent<SpriteRenderer>();
        }

        _sprWolf   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/wolf");
        _sprStill  = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/wolfstil");
        _sprAttack = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/wolfatta");
        _sprDead   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/wolfdead");
        _walkSprites = new[] { _sprStill, _sprWolf };

        SetSprite(_sprStill);
        PickNewRoamTarget();
    }

    void Update()
    {
        if (_state == State.Attacking || _state == State.Dead) return;

        // Check for nearby targets every frame
        Transform nearest = FindTarget();
        if (nearest != null)
        {
            _target = nearest;
            _state  = State.Hunting;
        }
        else if (_state == State.Hunting)
        {
            _target = null;
            _state  = State.Roaming;
        }

        switch (_state)
        {
            case State.Roaming: UpdateRoaming(); break;
            case State.Hunting: UpdateHunting(); break;
        }

        // Iso sort order: -(col+row) = -y/isoHalfH.
        // +50 ensures wolf always renders on top of all grass/terrain (max grass sortOrder = 0).
        _sr.sortingOrder = Mathf.RoundToInt(-transform.position.y / 0.768f) + 50;
    }

    // ---- Roaming ----
    private void UpdateRoaming()
    {
        float dist = Vector2.Distance(transform.position, _roamTarget);
        if (dist <= 0.15f)
        {
            StopWolfWalk();
            SetSprite(_sprStill);
            _roamWaitTimer -= Time.deltaTime;
            if (_roamWaitTimer <= 0f)
                PickNewRoamTarget();
        }
        else
        {
            TickWolfWalk(_roamTarget);
            MoveToward(_roamTarget, moveSpeed);
        }
    }

    // ---- Hunting ----
    private void UpdateHunting()
    {
        if (_target == null) { _state = State.Roaming; return; }

        float dist = Vector2.Distance(transform.position, _target.position);
        if (dist <= attackRadius)
            StartCoroutine(AttackCoroutine());
        else
        {
            TickWolfWalk(_target.position);
            MoveToward(_target.position, chaseSpeed);
        }
    }

    private void TickWolfWalk(Vector2 target)
    {
        _walkTimer    += Time.deltaTime;
        _walkBobTimer += Time.deltaTime;
        float interval = 1f / Mathf.Max(1, _walkFps);
        if (_walkTimer >= interval)
        {
            _walkTimer -= interval;
            _walkFrame  = (_walkFrame + 1) % _walkSprites.Length;
            SetSprite(_walkSprites[_walkFrame]);
        }
        float bob = Mathf.Sin(_walkBobTimer * _walkBobFrequency * Mathf.PI * 2f) * _walkBobAmplitude;
        if (_wolfVisual != null) _wolfVisual.localPosition = new Vector3(0f, bob, 0f);
    }

    private void StopWolfWalk()
    {
        _walkTimer    = 0f;
        _walkBobTimer = 0f;
        _walkFrame    = 0;
        if (_wolfVisual != null) _wolfVisual.localPosition = Vector3.zero;
    }

    // ---- Attack ----
    private IEnumerator AttackCoroutine()
    {
        _state = State.Attacking;
        SetSprite(_sprAttack);

        yield return new WaitForSeconds(2f);

        // Kill target
        if (_target != null)
        {
            var bumpkin = _target.GetComponent<BumpkinController>();
            if (bumpkin != null)
                bumpkin.TakeDamage("wolf");
            else
                Destroy(_target.gameObject); // cow or other animal
            _target = null;
        }

        // Wolf dies
        _state = State.Dead;
        SetSprite(_sprDead);
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    // ---- Target search ----
    private Transform FindTarget()
    {
        Transform nearest = null;
        float     best    = huntRadius;

        foreach (var b in FindObjectsByType<BumpkinController>(FindObjectsSortMode.None))
        {
            if (b == null) continue;
            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < best) { best = d; nearest = b.transform; }
        }
        foreach (var c in FindObjectsByType<CowAnimator>(FindObjectsSortMode.None))
        {
            if (c == null) continue;
            float d = Vector2.Distance(transform.position, c.transform.position);
            if (d < best) { best = d; nearest = c.transform; }
        }
        return nearest;
    }

    // ---- Helpers ----
    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        transform.position = (Vector2)transform.position + dir * speed * Time.deltaTime;
        if (_sr != null) _sr.flipX = dir.x > 0f;
    }

    private void PickNewRoamTarget()
    {
        // Stay within the iso map world bounds (~24×18 grid)
        _roamTarget    = new Vector2(Random.Range(-8f, 10f), Random.Range(0.5f, 8.5f));
        _roamWaitTimer = Random.Range(1.5f, 4f);
    }

    public void TakeDamage(string reason = "unknown")
    {
        if (_state == State.Dead) return;
        StopAllCoroutines();
        _state = State.Dead;
        SetSprite(_sprDead);
        StartCoroutine(DestroyAfter(3f));
    }

    private IEnumerator DestroyAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Destroy(gameObject);
    }

    private void SetSprite(Sprite s)
    {
        if (_sr != null && s != null) _sr.sprite = s;
    }
}
