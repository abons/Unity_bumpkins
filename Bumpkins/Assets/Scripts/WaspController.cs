using System.Collections;
using UnityEngine;

/// <summary>
/// Wasp state machine: Roaming → Hunting → Attacking → Dead.
/// Roaming: random movement across the map.
/// Hunting:  moves toward nearest bumpkin within huntRadius.
/// Attacking: plays attack sprite, stings target after 1.5s, then dies.
/// Dead: death sprite shown for 2s, then destroyed.
/// </summary>
public class WaspController : MonoBehaviour
{
    [Header("Radii")]
    public float huntRadius   = 4f;
    public float attackRadius = 0.6f;

    [Header("Speed")]
    public float moveSpeed  = 2.5f;
    public float chaseSpeed = 4.5f;

    private enum State { Roaming, Hunting, Attacking, Dead }
    private State _state = State.Roaming;

    private SpriteRenderer _sr;
    private Sprite _sprFly;
    private Sprite _sprAttack;
    private Sprite _sprDead;

    private Transform _target;
    private Vector2   _roamTarget;
    private float     _roamWaitTimer;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sprFly    = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/wasp");
        _sprAttack = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/waspat");
        _sprDead   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/waspdead");

        SetSprite(_sprFly);
        PickNewRoamTarget();
    }

    void Update()
    {
        if (_state == State.Attacking || _state == State.Dead) return;

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

        _sr.sortingOrder = Mathf.RoundToInt(-transform.position.y / 0.768f) + 50;
    }

    // ---- Roaming ----
    private void UpdateRoaming()
    {
        float dist = Vector2.Distance(transform.position, _roamTarget);
        if (dist <= 0.15f)
        {
            _roamWaitTimer -= Time.deltaTime;
            if (_roamWaitTimer <= 0f)
                PickNewRoamTarget();
        }
        else
        {
            SetSprite(_sprFly);
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
            SetSprite(_sprFly);
            MoveToward(_target.position, chaseSpeed);
        }
    }

    // ---- Attack ----
    private IEnumerator AttackCoroutine()
    {
        _state = State.Attacking;
        SetSprite(_sprAttack);

        yield return new WaitForSeconds(1.5f);

        if (_target != null)
        {
            var bumpkin = _target.GetComponent<BumpkinController>();
            if (bumpkin != null)
                bumpkin.TakeDamage("wasp");
            _target = null;
        }

        _state = State.Dead;
        SetSprite(_sprDead);
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    // ---- Target search ----
    private Transform FindTarget()
    {
        Transform nearest = null;
        float     best    = huntRadius;

        foreach (var b in FindObjectsByType<BumpkinController>(FindObjectsSortMode.None))
        {
            if (b == null || b.IsDead) continue;
            float d = Vector2.Distance(transform.position, b.transform.position);
            if (d < best) { best = d; nearest = b.transform; }
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
        _roamTarget    = new Vector2(Random.Range(-8f, 10f), Random.Range(0.5f, 8.5f));
        _roamWaitTimer = Random.Range(1f, 3f);
    }

    private void SetSprite(Sprite s)
    {
        if (_sr != null && s != null) _sr.sprite = s;
    }
}
