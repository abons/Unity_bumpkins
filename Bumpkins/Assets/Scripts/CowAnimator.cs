using UnityEngine;

/// <summary>
/// Koe wandelt willekeurig binnen de grenzen van zijn pen.
/// Stopt met bewegen zodra een bumpkin begint met melken.
/// </summary>
public class CowAnimator : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed    = 0.3f;
    public float minWaitTime  = 2f;
    public float maxWaitTime  = 5f;

    // Grens in local space waarbinnen de koe mag wandelen (gezet door GridMapBuilder)
    [HideInInspector] public Vector2 wanderBounds = new Vector2(1f, 0.5f);

    private SpriteRenderer _sr;
    private Vector3        _target;
    private float          _waitTimer;
    private bool           _milking;

    void Start()
    {
        _sr = GetComponent<SpriteRenderer>();
        _target    = transform.localPosition;
        _waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    void Update()
    {
        if (_milking) return;

        float dist = Vector3.Distance(transform.localPosition, _target);
        if (dist > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition, _target, moveSpeed * Time.deltaTime);

            // Flip sprite richting beweging
            if (_sr != null)
                _sr.flipX = (_target.x < transform.localPosition.x);
        }
        else
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
                PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        _target = new Vector3(
            Random.Range(-wanderBounds.x, wanderBounds.x),
            Random.Range(-wanderBounds.y, wanderBounds.y),
            0f);
        _waitTimer = Random.Range(minWaitTime, maxWaitTime);
    }

    public void SetMilking(bool milking)
    {
        _milking = milking;
        if (milking)
            _target = transform.localPosition; // stop op huidige plek
    }
}
