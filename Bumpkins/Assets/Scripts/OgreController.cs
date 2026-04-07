using System.Collections;
using UnityEngine;

/// <summary>
/// Ogre state machine: Roaming → Hunting → Attacking → Dead.
/// Slow heavy hitter targeting bumpkins and cows.
/// Sprites: ogrestil (still), ogrewalk (walk), ogreatta (attack), ogredead (dead).
/// </summary>
public class OgreController : MonoBehaviour
{
    [Header("Radii")]
    public float huntRadius   = 3.5f;
    public float attackRadius = 1f;

    [Header("Speed")]
    public float moveSpeed  = 1.2f;
    public float chaseSpeed = 2f;

    private enum State { Roaming, Hunting, Attacking, Dead }
    private State _state = State.Roaming;

    private SpriteRenderer _sr;
    private Sprite _sprStill;
    private Sprite _sprWalk;
    private Sprite _sprAttack;
    private Sprite _sprDead;

    private Transform _target;
    private Vector2   _roamTarget;
    private float     _roamWaitTimer;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sprStill  = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/ogrestil");
        _sprWalk   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/ogrewalk");
        _sprAttack = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/ogreatta");
        _sprDead   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Animals/ogredead");

        SetSprite(_sprStill);
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

    private void UpdateRoaming()
    {
        float dist = Vector2.Distance(transform.position, _roamTarget);
        if (dist <= 0.15f)
        {
            SetSprite(_sprStill);
            _roamWaitTimer -= Time.deltaTime;
            if (_roamWaitTimer <= 0f)
                PickNewRoamTarget();
        }
        else
        {
            SetSprite(_sprWalk);
            MoveToward(_roamTarget, moveSpeed);
        }
    }

    private void UpdateHunting()
    {
        if (_target == null) { _state = State.Roaming; return; }

        float dist = Vector2.Distance(transform.position, _target.position);
        if (dist <= attackRadius)
            StartCoroutine(AttackCoroutine());
        else
        {
            SetSprite(_sprWalk);
            MoveToward(_target.position, chaseSpeed);
        }
    }

    private IEnumerator AttackCoroutine()
    {
        _state = State.Attacking;
        SetSprite(_sprAttack);

        yield return new WaitForSeconds(3f);

        if (_target != null)
        {
            var bumpkin = _target.GetComponent<BumpkinController>();
            if (bumpkin != null)
                bumpkin.TakeDamage("ogre");
            else
                Destroy(_target.gameObject);
            _target = null;
        }

        _state = State.Dead;
        SetSprite(_sprDead);
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

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
        foreach (var c in FindObjectsByType<CowAnimator>(FindObjectsSortMode.None))
        {
            if (c == null) continue;
            float d = Vector2.Distance(transform.position, c.transform.position);
            if (d < best) { best = d; nearest = c.transform; }
        }
        return nearest;
    }

    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        transform.position = (Vector2)transform.position + dir * speed * Time.deltaTime;
        if (_sr != null) _sr.flipX = dir.x > 0f;
    }

    private void PickNewRoamTarget()
    {
        _roamTarget    = new Vector2(Random.Range(-8f, 10f), Random.Range(0.5f, 8.5f));
        _roamWaitTimer = Random.Range(2f, 5f);
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
