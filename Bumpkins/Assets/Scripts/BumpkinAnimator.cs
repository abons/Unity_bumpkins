using UnityEngine;

/// <summary>
/// Wisselt de bumpkin-sprite op basis van de state in BumpkinController.
/// Geen flip-animatie, geen overlay — één sprite per state op de bumpkin zelf.
/// </summary>
public class BumpkinAnimator : MonoBehaviour
{
    private BumpkinController _bc;
    private SpriteRenderer    _sr;
    private Transform         _visual;

    // Sprites
    private Sprite _sprIdle;
    private Sprite _sprHarvest;
    private Sprite _sprMilk;
    private Sprite _sprCarry;
    private Sprite _sprCarryMilk;
    private Sprite _sprDead;
    private Sprite _sprSkeleton;

    // Walk animation
    private Sprite[] _walkSprites;
    private int      _walkFrame;
    private float    _walkTimer;

    [Header("Walk Animation")]
    [SerializeField] private int _walkFps = 8;

    private string _lastState   = "";
    private bool   _lastIsChild  = false;
    private bool   _lastIsElder  = false;

    [Header("Idle Facing")]
    [SerializeField] private float _idleFacingIntervalMin = 3f;
    [SerializeField] private float _idleFacingIntervalMax = 7f;

    // Directions cycled during Idle: SE (default) then SW (flipX)
    private static readonly bool[] s_idleFlipX = { false, true };
    private int   _idleDirIndex;
    private float _idleFacingTimer;

    void Start()
    {
        _bc = GetComponent<BumpkinController>();

        // Maak visuele child zodat root-positie vrij blijft voor movement
        var visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(transform);
        visualGo.transform.localPosition = Vector3.zero;
        visualGo.transform.localScale    = Vector3.one;
        _visual = visualGo.transform;

        // Verplaats bestaande SpriteRenderer naar visual child
        _sr = GetComponent<SpriteRenderer>();
        if (_sr != null)
        {
            var newSr = visualGo.AddComponent<SpriteRenderer>();
            newSr.sprite       = _sr.sprite;
            newSr.color        = _sr.color;
            newSr.sortingOrder = _sr.sortingOrder;
            transform.localScale = _sr.transform.localScale;
            Destroy(_sr);
            _sr = newSr;
        }
        else
        {
            _sr = visualGo.AddComponent<SpriteRenderer>();
        }

        bool male = _bc.IsMale;
        bool kid  = _bc.isChild;
        _sprIdle      = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/{(kid ? (male ? "kidm" : "kidf") : (male ? "m_still" : "f_still"))}");
        _sprHarvest   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/{(male ? "m_harvest" : "f_harvest")}");
        _sprMilk      = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/milking");
        _sprCarry     = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/{(male ? "m_sack"    : "f_sack")}");
        _sprCarryMilk = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/f_milk");
        _sprDead      = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/{(kid ? (male ? "d_kidm" : "d_kidf") : (male ? "d_male" : "d_fema"))}");
        _sprSkeleton  = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/skeleton");
        _walkSprites  = Resources.LoadAll<Sprite>($"{GraphicsQuality.SpritePath}/Units/{(male ? "villager" : "woman")}");

        SetSprite(_sprIdle);
    }

    void Update()
    {
        if (_bc == null || _sr == null) return;

        // Herlaad idle sprite als kind opgroeit
        if (_bc.isChild != _lastIsChild)
        {
            _lastIsChild = _bc.isChild;
            bool male = _bc.IsMale;
            bool kid  = _bc.isChild;
            _sprIdle = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/{(kid ? (male ? "kidm" : "kidf") : (male ? "m_still" : "f_still"))}");
            _lastState = ""; // forceer state-update
        }

        // Herlaad idle sprite als bumpkin een elder wordt
        if (_bc.isElder != _lastIsElder)
        {
            _lastIsElder = _bc.isElder;
            if (_bc.isElder)
            {
                _sprIdle = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/{(_bc.IsMale ? "elderm" : "elderf")}");
                _lastState = ""; // forceer state-update
            }
        }

        string state        = _bc.CurrentState;
        bool   stateChanged  = state != _lastState;

        if (stateChanged)
        {
            _lastState = state;
            _sr.flipX  = false;
            _sr.flipY  = false;
            _visual.localRotation = Quaternion.identity;

            if (state == "Idle")
            {
                _idleDirIndex    = 0;
                _idleFacingTimer = UnityEngine.Random.Range(_idleFacingIntervalMin, _idleFacingIntervalMax);
            }

            if (IsWalkingState(state))
            {
                _walkFrame = 0;
                _walkTimer = 0f;
            }
        }

        // Per-frame idle direction timer — runs independently of state changes
        if (state == "Idle")
        {
            _idleFacingTimer -= Time.deltaTime;
            if (_idleFacingTimer <= 0f)
            {
                _idleFacingTimer = UnityEngine.Random.Range(_idleFacingIntervalMin, _idleFacingIntervalMax);
                _idleDirIndex    = (_idleDirIndex + 1) % s_idleFlipX.Length;
                _sr.flipX        = s_idleFlipX[_idleDirIndex];
            }
        }

        // Per-frame walk animation — runs every frame while moving
        if (IsWalkingState(state) && _walkSprites != null && _walkSprites.Length > 0)
        {
            _sr.flipX = _bc.MoveDirection.x > 0f;
            _walkTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1, _walkFps);
            if (_walkTimer >= frameDuration)
            {
                _walkTimer -= frameDuration;
                _walkFrame  = (_walkFrame + 1) % _walkSprites.Length;
                SetSprite(_walkSprites[_walkFrame]);
            }
        }

        if (!stateChanged) return;

        switch (state)
        {
            case "Working":
                var node = _bc.CurrentNode;
                if (node != null)
                {
                    if (node.nodeType == ProductionNode.NodeType.Cow)
                    {
                        // Snap bumpkin to waar de koe werkelijk staat (niet het node-centrum)
                        var cowAnim = node.GetComponentInChildren<CowAnimator>();
                        transform.position = cowAnim != null
                            ? cowAnim.transform.position
                            : node.transform.position;
                        SetSprite(_sprMilk);
                    }
                    else
                    {
                        transform.position = node.transform.position;
                        SetSprite(_sprHarvest);
                    }
                }
                break;

            case "Constructing":
                SetSprite(_sprIdle);
                break;

            case "Walking":
            case "WalkingToNode":
            case "WalkingToConstruction":
            case "WalkingToCampfire":
            case "WalkingToBuilding":
            case "WalkingToEgg":
            case "WalkingToMakeBaby":
            case "WalkingToMakeBabyFemale":
            case "Fleeing":
                if (_walkSprites != null && _walkSprites.Length > 0)
                    SetSprite(_walkSprites[0]);
                else
                    SetSprite(_sprIdle);
                break;

            case "WalkingToDropOff":
                if (_bc.CarriedMilk > 0)        SetSprite(_sprCarryMilk);
                else if (_bc.CarriedWheat > 0)  SetSprite(_sprCarry);
                else                            SetSprite(_sprIdle);
                break;

            case "Dying":
                SetSprite(_sprDead);
                break;

            case "DeadLying":
                SetSprite(_sprIdle);
                _visual.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;

            case "DeadSkeleton":
                SetSprite(_sprSkeleton);
                break;

            default:
                SetSprite(_sprIdle);
                break;
        }
    }

    private static bool IsWalkingState(string s) =>
        s == "Walking" || s == "WalkingToNode" || s == "WalkingToConstruction" || s == "Fleeing" ||
        s == "WalkingToCampfire" || s == "WalkingToBuilding" || s == "WalkingToEgg" ||
        s == "WalkingToMakeBaby" || s == "WalkingToMakeBabyFemale";

    private void SetSprite(Sprite sp)
    {
        if (_sr != null && sp != null)
            _sr.sprite = sp;
    }
}
