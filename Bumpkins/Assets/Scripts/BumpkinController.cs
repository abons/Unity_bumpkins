using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Click-to-move bumpkin controller.
/// Attach to a Bumpkin GameObject that has a Rigidbody2D (or use transform.position for top-down).
/// </summary>
public class BumpkinController : MonoBehaviour
{
    public enum BumpkinType { Male, Female }

    [Header("Identity")]
    public BumpkinType bumpkinType = BumpkinType.Male;
    [Tooltip("Kinderen mogen niet werken en zijn kleiner")]
    public bool isChild = false;
    public bool isElder = false;

    [Header("Autonomie")]
    public bool freeWill = true;  // als false: bumpkin blijft idle

    [Header("Movement")]
    public float moveSpeed      = 3f;
    public float stopDistance   = 0.15f;  // stops within this distance of target

    [Header("Attack Ranges")]
    public const float ThrowRange = 4.0f;
    public const float MeleeRange = 1.0f;

    [Header("State (read-only in inspector)")]
    [SerializeField] private string _currentState = "Idle";
    [SerializeField] private Vector2 _target;
    [SerializeField] private bool _moving;

    // The node the bumpkin is heading to (null = just moving to ground)
    private ProductionNode  _targetNode;
    private DropOffNode     _targetDropOff;
    private ChickenAnimator _targetChicken;
    private BuildingTag     _targetHouse;   // voor makeBaby
    private ConstructionSite _targetSite;   // voor bouwen
    private WorkCell          _targetWorkCell;  // specific cell assigned at that site

    // Pathfinding
    private List<Vector2> _path;
    private int           _pathIndex;

    public bool IsMale   => bumpkinType == BumpkinType.Male;
    public bool IsFemale => bumpkinType == BumpkinType.Female;
    public string CurrentState => _currentState;
    public ProductionNode CurrentNode => _targetNode;
    public Vector2 MoveDirection { get; private set; }

    // Carried resources
    public int CarriedWheat { get; private set; }
    public int CarriedMilk  { get; private set; }

    public bool IsDead => _currentState == "Dying" || _currentState == "DeadLying" || _currentState == "DeadSkeleton";

    // ---- Called by click handler on ground ----
    public void MoveTo(Vector2 worldPos)
    {
        if (IsDead) return;
        _targetNode?.Release();
        _target        = worldPos;
        _moving        = true;
        _cancelEntry   = true;
        ComputePath(worldPos);
        _targetNode    = null;
        _targetDropOff = null;
        _playerMoved   = true;
        // If InBuilding, the exit coroutine will set Walking after repositioning
        if (_currentState != "InBuilding")
            SetState("Walking");
    }

    private bool _playerMoved       = false;
    private bool _cancelEntry        = false;
    private bool _justExitedBuilding = false;
    private Coroutine _hideCoroutine = null;

    private void ComputePath(Vector2 target)
    {
        _path      = NavGrid.FindPath(transform.position, target);
        _pathIndex = 0;
    }

    // Attack
    private float _attackCooldownTimer;
    private float _attackScanTimer;
    private const float AttackScanInterval = 0.5f;
    private const float AttackCooldown     = 2.5f;

    // ---- Called when player clicks a production node ----
    public void AssignToNode(ProductionNode node)
    {
        if (!node.TryReserve(this))
        {
            Debug.Log($"[Bumpkin] {bumpkinType} kon {node.name} niet reserveren");
            return;
        }
        _targetNode    = node;
        _targetDropOff = null;
        _target        = (Vector2)node.transform.position + node.workOffset;
        _moving        = true;
        ComputePath(_target);
        SetState("WalkingToNode");
    }

    // ---- Called when player clicks a drop-off building ----
    public void AssignToDropOff(DropOffNode node)
    {
        _targetDropOff = node;
        _targetNode    = null;
        _target        = (Vector2)node.transform.position + node.doorOffset;
        _moving        = true;
        ComputePath(_target);
        SetState("WalkingToDropOff");
    }

    // ---- Called when assigned to a ConstructionSite ----
    public void AssignToConstruction(ConstructionSite site, WorkCell cell)
    {
        _targetSite     = site;
        _targetWorkCell = cell;
        // Walk near the cell centre; small random offset so bumpkin doesn't snap exactly onto it
        _target = (Vector2)cell.transform.position + Random.insideUnitCircle * 0.2f;
        _moving = true;
        ComputePath(_target);
        SetStateRaw("WalkingToConstruction");
    }

    void Start()
    {
        if (!isChild)
            StartCoroutine(AgeToElder(300f));
    }

    private IEnumerator AgeToElder(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!IsDead)
        {
            isElder = true;
            // Release any work the bumpkin was doing
            _targetNode?.Release();
            _targetNode    = null;
            _targetDropOff = null;
            _targetSite    = null;
            _moving        = false;
            SetState("Idle");
            Debug.Log($"[Bumpkin] {name} is een elder geworden!");
            StartCoroutine(ElderDeathTimer(60f));
        }
    }

    private IEnumerator ElderDeathTimer(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (!IsDead)
            TakeDamage("old age");
    }

    // How fast this bumpkin actually moves (half speed for elders)
    private float EffectiveSpeed => isElder ? moveSpeed * 0.5f : moveSpeed;

    private float _fleeCheckTimer;
    private const float FleeCheckInterval = 0.5f;

    void Update()
    {
        if (IsDead) return;

        // Attack scan for non-child bumpkins
        if (!isChild)
        {
            if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
            _attackScanTimer -= Time.deltaTime;
            if (_attackScanTimer <= 0f)
            {
                _attackScanTimer = AttackScanInterval;
                if (_attackCooldownTimer <= 0f && CanInitiateAttack())
                    ScanForAttackTarget();
            }
        }

        // Elder flee logic — periodically check for nearby enemies
        if (isElder)
        {
            _fleeCheckTimer -= Time.deltaTime;
            if (_fleeCheckTimer <= 0f)
            {
                _fleeCheckTimer = FleeCheckInterval;
                TryFlee();
            }
        }

        if (!_moving) return;

        Vector2 pos = transform.position;

        // Advance through completed waypoints
        while (_path != null && _pathIndex < _path.Count
               && Vector2.Distance(pos, _path[_pathIndex]) <= stopDistance)
            _pathIndex++;

        bool    pathDone = _path == null || _pathIndex >= _path.Count;
        Vector2 waypoint = pathDone ? _target : _path[_pathIndex];

        float dist = Vector2.Distance(pos, waypoint);
        if (dist <= stopDistance)
        {
            _moving = false;
            if (_currentState != "Fleeing")
                OnReachedTarget();
            return;
        }

        Vector2 dir = (waypoint - pos).normalized;
        MoveDirection = dir;
        transform.position = (Vector2)transform.position + dir * EffectiveSpeed * Time.deltaTime;
    }

    private void TryFlee()
    {
        // Find nearest enemy within 5 units
        float   bestDist  = 5f;
        Vector2 threatPos = Vector2.zero;
        bool    found     = false;

        foreach (var w in FindObjectsByType<WolfController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(transform.position, w.transform.position);
            if (d < bestDist) { bestDist = d; threatPos = w.transform.position; found = true; }
        }
        foreach (var w in FindObjectsByType<WaspController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(transform.position, w.transform.position);
            if (d < bestDist) { bestDist = d; threatPos = w.transform.position; found = true; }
        }
        foreach (var w in FindObjectsByType<BloodWaspController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(transform.position, w.transform.position);
            if (d < bestDist) { bestDist = d; threatPos = w.transform.position; found = true; }
        }
        foreach (var w in FindObjectsByType<BatController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(transform.position, w.transform.position);
            if (d < bestDist) { bestDist = d; threatPos = w.transform.position; found = true; }
        }
        foreach (var w in FindObjectsByType<OgreController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(transform.position, w.transform.position);
            if (d < bestDist) { bestDist = d; threatPos = w.transform.position; found = true; }
        }
        foreach (var w in FindObjectsByType<ZombieController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(transform.position, w.transform.position);
            if (d < bestDist) { bestDist = d; threatPos = w.transform.position; found = true; }
        }
        foreach (var w in FindObjectsByType<GiantController>(FindObjectsSortMode.None))
        {
            float d = Vector2.Distance(transform.position, w.transform.position);
            if (d < bestDist) { bestDist = d; threatPos = w.transform.position; found = true; }
        }

        if (!found) return;

        // Run away: move in opposite direction, 6 units
        Vector2 away = ((Vector2)transform.position - threatPos).normalized;
        _target  = (Vector2)transform.position + away * 6f;
        _moving  = true;
        ComputePath(_target);
        SetStateRaw("Fleeing");
    }

    private void OnReachedTarget()
    {
        if (_targetNode != null)
        {
            _targetNode.StartWork(this);
            SetState("Working");
        }
        else if (_targetDropOff != null)
        {
            var dropOff = _targetDropOff;
            _targetDropOff = null;
            dropOff.Deliver(this);
            var millAnim  = dropOff.GetComponentInChildren<MillAnimator>();
            var dairyAnim = dropOff.GetComponentInChildren<DairyAnimator>();
            StartCoroutine(EnterBuildingWithDoor(Random.Range(1.5f, 3f), millAnim, dairyAnim));
        }
        else if (_currentState == "WalkingToCampfire")
        {
            // Hang out at campfire for a while
            SetStateRaw("IdleAtCampfire");
            StartCoroutine(WaitThenIdle(Random.Range(5f, 10f)));
        }
        else if (_currentState == "WalkingToBuilding")
        {
            // Enter building — disappear briefly
            if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
            _hideCoroutine = StartCoroutine(HideInBuilding(Random.Range(3f, 6f)));
        }
        else if (_currentState == "WalkingToEgg")
        {
            if (_targetChicken != null)
            {
                _targetChicken.CollectEgg();
                _targetChicken = null;
            }
            SetStateRaw("Eating");
            StartCoroutine(WaitThenIdle(2f));
        }
        else if (_currentState == "WalkingToMakeBaby")
        {
            // Man: verdwijn kort, dan Idle. Vrouw: wacht op BabySystem.ReleaseFromBaby.
            if (IsMale)
            {
                if (_hideCoroutine != null) StopCoroutine(_hideCoroutine);
                _hideCoroutine = StartCoroutine(HideInBuilding(Random.Range(2f, 4f)));
            }
            // Female werd gestuurd via AssignToMakeBaby — BabySystem handelt haar af
        }
        else if (_currentState == "WalkingToMakeBabyFemale")
        {
            if (_targetHouse != null)
                BabySystem.Instance.OnFemaleArrived(this, _targetHouse);
            _targetHouse = null;
        }
        else if (_currentState == "WalkingToConstruction")
        {
            if (_targetSite != null && _targetSite.CurrentStage != ConstructionSite.Stage.Done)
            {
                _targetWorkCell?.Activate();  // show active sprite now that we've arrived; bumpkin hides below
                SetStateRaw("Constructing");
                StartCoroutine(DoConstruction());
            }
            else
            {
                _targetSite = null;
                SetState("Idle");
            }
        }
        else
        {
            if (_playerMoved && !_justExitedBuilding)
            {
                // Speler heeft de bumpkin verplaatst: even rust voor autonome actie
                _playerMoved = false;
                SetStateRaw("IdleWaiting");
                StartCoroutine(WaitThenIdle(4f));
            }
            else
            {
                _playerMoved        = false;
                _justExitedBuilding = false;
                SetState("Idle");
            }
        }
    }

    // ---- Baby system hooks ----
    /// <summary>Geroepen door BabySystem nadat de baby verdwenen is.</summary>
    public void ReleaseFromBaby()
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.enabled = true;
        SetState("Idle");
    }

    /// <summary>Kind groeit op na geveb seconden.</summary>
    public void StartGrowUp(float seconds) => StartCoroutine(GrowUpAfter(seconds));

    private IEnumerator GrowUpAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isChild = false;
        transform.localScale = new Vector3(3f, 3f, 1f);  // normale grootte
        Debug.Log($"[BabySystem] {name} is volwassen geworden!");
        SetState("Idle");
    }

    // ---- Coroutines for idle activities ----
    private IEnumerator WaitThenIdle(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SetState("Idle");
    }

    private IEnumerator EnterBuildingWithDoor(float seconds, MillAnimator mill, DairyAnimator dairy)
    {
        _cancelEntry = false;
        if (_hideCoroutine != null) { StopCoroutine(_hideCoroutine); _hideCoroutine = null; }

        // Open door first, let player see the unit step in
        mill?.OpenDoor();
        dairy?.OpenDoor();
        yield return new WaitForSeconds(0.2f);
        if (_cancelEntry) { mill?.CloseDoor(); dairy?.CloseDoor(); yield break; }

        // Walk directly into the building (bypasses navgrid)
        Vector2 doorPos     = (Vector2)transform.position;          // remember exit spot
        Vector2 enterTarget = doorPos + new Vector2(-0.35f, 0.52f);
        Vector2 startPos    = transform.position;
        MoveDirection = (enterTarget - startPos).normalized;
        while (Vector2.Distance((Vector2)transform.position, enterTarget) > 0.02f)
        {
            if (_cancelEntry) { mill?.CloseDoor(); dairy?.CloseDoor(); yield break; }
            transform.position = Vector2.MoveTowards(transform.position, enterTarget, EffectiveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = enterTarget;
        if (_cancelEntry) { mill?.CloseDoor(); dairy?.CloseDoor(); yield break; }

        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.enabled = false;
        // Close door once unit is fully inside
        mill?.CloseDoor();
        dairy?.CloseDoor();
        SetStateRaw("InBuilding");

        if (!freeWill)
        {
            // Stay inside indefinitely; cancelled only by MoveTo (which sets _cancelEntry)
            while (!_cancelEntry) yield return null;
            // Re-open door to exit
            mill?.OpenDoor();
            dairy?.OpenDoor();
            yield return new WaitForSeconds(0.2f);
            transform.position = doorPos;
            if (sr) sr.enabled = true;
            SetStateRaw("Walking");  // now safe to show unit at door
            yield return new WaitForSeconds(0.3f);
            mill?.CloseDoor();
            dairy?.CloseDoor();
            yield break;
        }

        yield return new WaitForSeconds(seconds);

        // Re-open door to exit
        mill?.OpenDoor();
        dairy?.OpenDoor();
        yield return new WaitForSeconds(0.2f);
        transform.position = doorPos;
        if (sr) sr.enabled = true;
        yield return new WaitForSeconds(0.3f);
        mill?.CloseDoor();
        dairy?.CloseDoor();
        _justExitedBuilding = true;
        ExitBuilding();
    }

    private IEnumerator HideInBuilding(float seconds)
    {
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr) sr.enabled = false;
        SetStateRaw("InBuilding");
        yield return new WaitForSeconds(seconds);
        if (_currentState != "InBuilding") yield break;
        _hideCoroutine = null;
        if (sr) sr.enabled = true;
        SetState("Idle");
    }

    private IEnumerator DoConstruction()
    {
        var site = _targetSite;
        var cell = _targetWorkCell;
        yield return new WaitForSeconds(site != null ? site.workDuration : 4f);
        if (site != null && site.CurrentStage != ConstructionSite.Stage.Done)
            site.DeliverWork(cell);
        else
            cell?.Release();
        _targetWorkCell = null;
        _targetSite     = null;
        SetState("Idle");
    }

    // ---- Damage ----
    public void TakeDamage(string reason = "unknown")
    {
        Debug.LogWarning($"[Bumpkin:{bumpkinType}] {name} died — reason: {reason} — pos: {transform.position}");
        StopAllCoroutines();
        _targetNode?.Release();
        _moving = false;
        SetStateRaw("Dying");
        SelectionManager.Instance?.DeselectIfSelected(this);
        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine()
    {
        yield return new WaitForSeconds(1f);   // d_male falling
        SetStateRaw("DeadLying");               // m_still rotated
        yield return new WaitForSeconds(10f);
        SetStateRaw("DeadSkeleton");            // stays until buried
    }

    // ---- Resource carrying ----
    public void PickUpWheat(int amount)   { CarriedWheat += amount; }
    public void DropWheat()               { GameManager.Instance.AddWheat(CarriedWheat); CarriedWheat = 0; }
    public void PickUpMilk(int amount)    { CarriedMilk += amount; }
    public void DropMilk()               { GameManager.Instance.AddMilk(CarriedMilk);  CarriedMilk = 0; }

    // ---- Find nearest drop-off ----
    public DropOffNode FindNearestDropOff(DropOffNode.DropOffType type)
    {
        var nodes = FindObjectsByType<DropOffNode>(FindObjectsSortMode.None);
        DropOffNode nearest = null;
        float bestDist = float.MaxValue;
        foreach (var n in nodes)
        {
            if (n.dropOffType != type) continue;
            float d = Vector2.Distance(transform.position, n.transform.position);
            if (d < bestDist) { bestDist = d; nearest = n; }
        }
        return nearest;
    }

    /// <summary>Zoek een beschikbare ProductionNode. Geeft true terug als er werk gevonden is.</summary>
    private bool TryFindWork()
    {
        if (isChild)   return false;
        if (isElder)   return false;
        if (!freeWill) return false;

        // Check construction sites (males only)
        if (IsMale)
        {
            var sites = FindObjectsByType<ConstructionSite>(FindObjectsSortMode.None);
            ConstructionSite bestSite = null;
            float bestDist = float.MaxValue;
            foreach (var s in sites)
            {
                if (!s.CanBeWorked) continue;
                float d = Vector2.Distance(transform.position, s.transform.position);
                if (d < bestDist) { bestDist = d; bestSite = s; }
            }
            if (bestSite != null)
            {
                var cell = bestSite.TryReserveWorker(this);
                if (cell != null)
                {
                    AssignToConstruction(bestSite, cell);
                    return true;
                }
            }
        }

        var nodes = FindObjectsByType<ProductionNode>(FindObjectsSortMode.None);
        ProductionNode best = null;
        float bestNodeDist = float.MaxValue;
        foreach (var n in nodes)
        {
            if (!n.CanBeWorkedBy(this)) continue;
            float d = Vector2.Distance(transform.position, n.transform.position);
            if (d < bestNodeDist) { bestNodeDist = d; best = n; }
        }
        if (best != null)
        {
            Debug.Log($"[BumpkinWork] {name} gaat naar {best.name}");
            AssignToNode(best);
            return true;
        }
        return false;
    }

    /// <summary>Kies een willekeurige leisure-activiteit als er geen werk is.</summary>
    private void TryIdleActivity()
    {
        if (!freeWill) return;  // freewill uit: blijf idle
        // Kinderen: alleen leisure, geen baby-actie
        int maxRoll = (!isChild && IsMale) ? 5 : 4;
        int roll = _justExitedBuilding ? Random.Range(1, maxRoll) : Random.Range(0, maxRoll);
        _justExitedBuilding = false;
        switch (roll)
        {
            case 0: // Sta gewoon stil een tijdje
                SetStateRaw("IdleWaiting");
                StartCoroutine(WaitThenIdle(Random.Range(3f, 7f)));
                break;

            case 1: // Loop naar het kampvuur — stop op aangrenzende tegel, niet op het vuur
                var camp = FindFirstObjectByType<CampfireAnimator>();
                if (camp != null && BuildManager.Instance != null)
                {
                    var campTile = BuildManager.Instance.WorldToTile(camp.transform.position);
                    Vector2 bumpkinPos2 = transform.position;
                    Vector2 bestAdj = Vector2.zero;
                    float bestAdjDist = float.MaxValue;
                    bool adjFound = false;
                    for (int dc = -1; dc <= 1; dc++)
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        if (dc == 0 && dr == 0) continue; // sla campfire-tegel zelf over
                        var neighbor = new Vector2Int(campTile.x + dc, campTile.y + dr);
                        if (BuildManager.Instance.IsTileOccupied(neighbor)) continue;
                        Vector2 worldPos = BuildManager.Instance.TileToWorld(neighbor);
                        float d = Vector2.Distance(bumpkinPos2, worldPos);
                        if (d < bestAdjDist) { bestAdjDist = d; bestAdj = worldPos; adjFound = true; }
                    }
                    if (adjFound)
                    {
                        _target = bestAdj;
                        _moving = true;
                        ComputePath(_target);
                        SetStateRaw("WalkingToCampfire");
                    }
                    else goto case 0;
                }
                else goto case 0;
                break;

            case 2: // Loop een gebouw in
                var tags = FindObjectsByType<BuildingTag>(FindObjectsSortMode.None);
                if (tags.Length > 0)
                {
                    var picked = tags[Random.Range(0, tags.Length)];
                    _target = picked.transform.position;
                    _moving = true;
                    ComputePath(_target);
                    SetStateRaw("WalkingToBuilding");
                }
                else goto case 0;
                break;

            case 3: // Ga een ei eten
                var chickens = FindObjectsByType<ChickenAnimator>(FindObjectsSortMode.None);
                ChickenAnimator eggSource = null;
                foreach (var c in chickens)
                    if (c.HasEgg) { eggSource = c; break; }
                if (eggSource != null)
                {
                    _targetChicken = eggSource;
                    _target = (Vector2)eggSource.transform.position + new Vector2(0.15f, 0f);
                    _moving = true;
                    ComputePath(_target);
                    SetStateRaw("WalkingToEgg");
                }
                else goto case 0;
                break;

            case 4: // makeBaby — alleen volwassen male
                // Zoek een vrij huis en een idle vrouw
                BumpkinController idleFemale = null;
                foreach (var b in FindObjectsByType<BumpkinController>(FindObjectsSortMode.None))
                    if (b.IsFemale && !b.isChild && b.CurrentState == "Idle") { idleFemale = b; break; }
                if (idleFemale == null) goto case 0;

                BuildingTag freeHouse = null;
                foreach (var t in FindObjectsByType<BuildingTag>(FindObjectsSortMode.None))
                    if (t.isHouse && t.TryReserveForBaby()) { freeHouse = t; break; }
                if (freeHouse == null) goto case 0;

                // Male loopt naar huis
                _targetHouse = freeHouse;
                _target = freeHouse.transform.position;
                _moving = true;
                ComputePath(_target);
                SetStateRaw("WalkingToMakeBaby");

                // Female loopt ook naar huis
                idleFemale._targetHouse = freeHouse;
                idleFemale._target = (Vector2)freeHouse.transform.position + new Vector2(0.1f, 0f);
                idleFemale._moving = true;
                idleFemale.ComputePath(idleFemale._target);
                idleFemale.SetStateRaw("WalkingToMakeBabyFemale");
                Debug.Log($"[MakeBaby] {name} + {idleFemale.name} → {freeHouse.name}");
                break;
        }
    }

    // ---- Attack helpers ----
    private bool CanInitiateAttack()
    {
        switch (_currentState)
        {
            case "Idle":
            case "IdleWaiting":
            case "IdleAtCampfire":
            case "Walking":
            case "WalkingToBuilding":
            case "WalkingToCampfire":
            case "Fleeing":
                return true;
            default:
                return false;
        }
    }

    private void ScanForAttackTarget()
    {
        Transform nearest = null;
        float     bestDist = ThrowRange;

        void Check(Transform t)
        {
            float d = Vector2.Distance(transform.position, t.position);
            if (d < bestDist) { bestDist = d; nearest = t; }
        }

        foreach (var e in FindObjectsByType<WolfController>(FindObjectsSortMode.None))        if (e != null) Check(e.transform);
        foreach (var e in FindObjectsByType<ZombieController>(FindObjectsSortMode.None))      if (e != null) Check(e.transform);
        foreach (var e in FindObjectsByType<OgreController>(FindObjectsSortMode.None))        if (e != null) Check(e.transform);
        foreach (var e in FindObjectsByType<GiantController>(FindObjectsSortMode.None))       if (e != null) Check(e.transform);
        foreach (var e in FindObjectsByType<WaspController>(FindObjectsSortMode.None))        if (e != null) Check(e.transform);
        foreach (var e in FindObjectsByType<BloodWaspController>(FindObjectsSortMode.None))   if (e != null) Check(e.transform);
        foreach (var e in FindObjectsByType<BatController>(FindObjectsSortMode.None))         if (e != null) Check(e.transform);

        if (nearest == null) return;

        float dist = Vector2.Distance(transform.position, nearest.position);
        if (dist <= MeleeRange)
            StartCoroutine(MeleeAttackCoroutine(nearest));
        else
            StartCoroutine(ThrowRockCoroutine(nearest));
    }

    private IEnumerator MeleeAttackCoroutine(Transform target)
    {
        _attackCooldownTimer = AttackCooldown;
        MoveDirection = target != null
            ? ((Vector2)target.position - (Vector2)transform.position).normalized
            : Vector2.right;
        SetStateRaw("MeleeAttacking");
        yield return new WaitForSeconds(0.5f);
        if (target != null)
            DealDamageToEnemy(target.gameObject);
        SetState("Idle");
    }

    private IEnumerator ThrowRockCoroutine(Transform target)
    {
        _attackCooldownTimer = AttackCooldown;
        MoveDirection = target != null
            ? ((Vector2)target.position - (Vector2)transform.position).normalized
            : Vector2.right;
        SetStateRaw("ThrowingRock");
        yield return new WaitForSeconds(0.5f);   // wind-up
        if (target != null)
        {
            var projGo = new GameObject("ShrapnelProjectile");
            var proj   = projGo.AddComponent<ShrapnelProjectile>();
            proj.Launch(target, transform.position);
        }
        yield return new WaitForSeconds(1.5f);   // recovery
        SetState("Idle");
    }

    private void DealDamageToEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        if (enemy.TryGetComponent<WolfController>(out var wolf))         { wolf.TakeDamage("bumpkin");  return; }
        if (enemy.TryGetComponent<ZombieController>(out var zombie))     { zombie.TakeDamage("bumpkin"); return; }
        if (enemy.TryGetComponent<OgreController>(out var ogre))         { ogre.TakeDamage("bumpkin");   return; }
        if (enemy.TryGetComponent<GiantController>(out var giant))       { giant.TakeDamage("bumpkin");  return; }
        if (enemy.TryGetComponent<WaspController>(out var wasp))         { wasp.TakeDamage("bumpkin");   return; }
        if (enemy.TryGetComponent<BloodWaspController>(out var bwasp))   { bwasp.TakeDamage("bumpkin"); return; }
        if (enemy.TryGetComponent<BatController>(out var bat))           { bat.TakeDamage("bumpkin");    return; }
        Destroy(enemy);
    }

    /// <summary>Zet state zonder work/idle-logica te triggeren.</summary>
    private void SetStateRaw(string s)
    {
        _currentState = s;
        Debug.Log($"[Bumpkin:{bumpkinType}] State → {s}");
    }

    /// <summary>Called after exiting a building — skips Idle state, goes straight to work or activity.</summary>
    private void ExitBuilding()
    {
        _justExitedBuilding = false;
        if (!TryFindWork())
        {
            TryIdleActivity();
            // Fallback: if freeWill=false or no activity found, ensure a valid visible state
            if (_currentState == "InBuilding")
                SetStateRaw("Idle");
        }
    }

    private void SetState(string s)
    {
        _currentState = s;
        Debug.Log($"[Bumpkin:{bumpkinType}] State → {s}");
        if (s == "Idle")
        {
            if (!TryFindWork())
                TryIdleActivity();
        }
    }
}
