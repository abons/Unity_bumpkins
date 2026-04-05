using System.Collections;
using UnityEngine;

/// <summary>
/// Blood Wasp state machine: Roaming → Hunting → Attacking → Dead.
/// More aggressive variant of the wasp — faster, wider hunt radius, kills in one sting.
/// Sprites: bloodwsp (fly/attack), bloodead (dead).
/// </summary>
public class BloodWaspController : MonoBehaviour
{
    [Header("Radii")]
    public float huntRadius   = 6f;
    public float attackRadius = 0.5f;

    [Header("Speed")]
    public float moveSpeed  = 3.5f;
    public float chaseSpeed = 6f;

    private enum State { Roaming, Hunting, Attacking, Dead }
    private State _state = State.Roaming;

    private SpriteRenderer _sr;
    private Sprite _sprFly;
    private Sprite _sprDead;

    private Transform _target;
    private Vector2   _roamTarget;
    private float     _roamWaitTimer;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _sprFly  = Resources.Load<Sprite>("Sprites/Animals/bloodwsp");
        _sprDead = Resources.Load<Sprite>("Sprites/Animals/bloodead");

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

    private IEnumerator AttackCoroutine()
    {
        _state = State.Attacking;

        yield return new WaitForSeconds(0.8f);

        if (_target != null)
        {
            var bumpkin = _target.GetComponent<BumpkinController>();
            if (bumpkin != null)
                bumpkin.TakeDamage("blood wasp");
            _target = null;
        }

        _state = State.Dead;
        SetSprite(_sprDead);
        yield return new WaitForSeconds(1.5f);
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
        // NE quadrant of the 48×36 grid. BloodWasp spawns at tile (44,33) ≈ world (16.5, 59.1).
        // Bumpkin starts are near world (3–6, 34–35); keeping y ≥ 42 clears the 6-unit hunt radius.
        _roamTarget    = new Vector2(Random.Range(8f, 25f), Random.Range(42f, 62f));
        _roamWaitTimer = Random.Range(0.5f, 2f);
    }

    private void SetSprite(Sprite s)
    {
        if (_sr != null && s != null) _sr.sprite = s;
    }
}
