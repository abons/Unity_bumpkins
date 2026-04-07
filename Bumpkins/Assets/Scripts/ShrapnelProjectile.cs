using System.Collections;
using UnityEngine;

/// <summary>
/// Rock projectile spawned by a bumpkin throw attack.
/// Travels toward the target's world position at launch time.
/// Has a 17% miss chance: the rock flies slightly off-course and deals no damage.
/// </summary>
public class ShrapnelProjectile : MonoBehaviour
{
    private const float MissChance  = 0.17f;
    private const float TravelSpeed = 6f;

    private SpriteRenderer _sr;
    private Vector2        _destination;
    private Transform      _target;   // null = this throw will miss
    private bool           _arrived;

    void Awake()
    {
        _sr = gameObject.AddComponent<SpriteRenderer>();
        var spr = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Effects/shrapnel");
        if (spr != null) _sr.sprite = spr;
    }

    /// <summary>
    /// Initialise the projectile immediately after AddComponent.
    /// </summary>
    public void Launch(Transform target, Vector2 origin)
    {
        transform.position = origin;
        Vector2 targetPos  = target.position;
        bool    miss       = Random.value < MissChance;

        if (miss)
        {
            // Fly slightly to the side so the rock skims past the target
            Vector2 dir  = (targetPos - origin).normalized;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float   side = Random.value < 0.5f ? 1f : -1f;
            _destination = targetPos + perp * (Random.Range(0.4f, 0.9f) * side) + dir * 0.5f;
            _target      = null;   // flagged as miss — no damage on arrival
        }
        else
        {
            _destination = targetPos;
            _target      = target;
        }

        _sr.sortingOrder = Mathf.RoundToInt(-origin.y / 0.256f) + 10;
    }

    void Update()
    {
        if (_arrived) return;

        Vector2 pos  = transform.position;
        Vector2 diff = _destination - pos;
        float   dist = diff.magnitude;
        float   step = TravelSpeed * Time.deltaTime;

        // Rotate sprite to face direction of travel
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Update iso sort order as it travels
        _sr.sortingOrder = Mathf.RoundToInt(-pos.y / 0.256f) + 10;

        if (dist <= step)
        {
            transform.position = _destination;
            OnArrived();
            return;
        }

        transform.position = pos + diff.normalized * step;
    }

    private void OnArrived()
    {
        _arrived = true;
        if (_target != null)
            ApplyDamage(_target.gameObject);
        Destroy(gameObject);
    }

    private void ApplyDamage(GameObject enemy)
    {
        if (enemy == null) return;
        if (enemy.TryGetComponent<WolfController>(out var wolf))         { wolf.TakeDamage("shrapnel");  return; }
        if (enemy.TryGetComponent<ZombieController>(out var zombie))     { zombie.TakeDamage("shrapnel"); return; }
        if (enemy.TryGetComponent<OgreController>(out var ogre))         { ogre.TakeDamage("shrapnel");   return; }
        if (enemy.TryGetComponent<GiantController>(out var giant))       { giant.TakeDamage("shrapnel");  return; }
        if (enemy.TryGetComponent<WaspController>(out var wasp))         { wasp.TakeDamage("shrapnel");   return; }
        if (enemy.TryGetComponent<BloodWaspController>(out var bwasp))   { bwasp.TakeDamage("shrapnel"); return; }
        if (enemy.TryGetComponent<BatController>(out var bat))           { bat.TakeDamage("shrapnel");    return; }
        Destroy(enemy);   // unknown enemy type — remove it
    }
}
