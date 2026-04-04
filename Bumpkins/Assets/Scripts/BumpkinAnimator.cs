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

    private string _lastState  = "";
    private bool   _lastIsChild = false;

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
        _sprIdle      = Resources.Load<Sprite>($"Sprites/Units/{(kid ? (male ? "kidm" : "kidf") : (male ? "m_still" : "f_still"))}");
        _sprHarvest   = Resources.Load<Sprite>($"Sprites/Units/{(male ? "m_harvest" : "f_harvest")}");
        _sprMilk      = Resources.Load<Sprite>("Sprites/Units/milking");
        _sprCarry     = Resources.Load<Sprite>($"Sprites/Units/{(male ? "m_sack"    : "f_sack")}");
        _sprCarryMilk = Resources.Load<Sprite>("Sprites/Units/f_milk");
        _sprDead      = Resources.Load<Sprite>($"Sprites/Units/{(kid ? (male ? "d_kidm" : "d_kidf") : (male ? "d_male" : "d_fema"))}");
        _sprSkeleton  = Resources.Load<Sprite>("Sprites/Units/skeleton");

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
            _sprIdle = Resources.Load<Sprite>($"Sprites/Units/{(kid ? (male ? "kidm" : "kidf") : (male ? "m_still" : "f_still"))}");
            _lastState = ""; // forceer state-update
        }

        string state = _bc.CurrentState;
        if (state == _lastState) return;

        _lastState = state;
        _sr.flipX  = false;
        _sr.flipY  = false;
        _visual.localRotation = Quaternion.identity;

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
                SetSprite(_sprHarvest);  // bouwen = dezelfde hack-animatie als oogsten
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

    private void SetSprite(Sprite sp)
    {
        if (_sr != null && sp != null)
            _sr.sprite = sp;
    }
}
