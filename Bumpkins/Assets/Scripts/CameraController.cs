using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down 2D camera: pan met WASD/pijltjes, zoom met scrollwiel, middelmuis drag.
/// Gebruikt het nieuwe Unity Input System.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Pan")]
    public float panSpeed = 10f;

    [Header("Zoom")]
    public float zoomSpeed = 2f;
    public float zoomMin   = 12f;
    public float zoomMax   = 48f;

    [Header("Bounds (map 24x18, iso)")]
    public float minX = -28f;
    public float maxX = 38f;
    public float minY = 0f;
    public float maxY = 36f;

    [Header("Start Position")]
    public Vector3 startPosition = new Vector3(3f, 18f, -10f);
    public float   startSize     = 12f;

    private Camera _cam;
    private Vector3 _dragOrigin;
    private bool _dragging;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        transform.position    = startPosition;
        _cam.orthographicSize = startSize;
        _cam.backgroundColor  = new Color(0.35f, 0.55f, 0.25f); // gras groen
    }

    void Start()
    {
        var builder = FindFirstObjectByType<GridMapBuilder>();
        if (builder?.layout != null)
            AdaptToLayout(builder.layout);
    }

    /// <summary>Recalculate pan bounds and re-center camera for the given layout.</summary>
    public void AdaptToLayout(MapLayoutData layout)
    {
        float hw = layout.isoHalfW;
        float hh = layout.isoHalfH;
        int   c  = layout.cols - 1;
        int   r  = layout.rows - 1;
        minX = -(r * hw) - 4f;
        maxX =  (c * hw) + 4f;
        minY = -2f;
        maxY =  (c + r) * hh + 4f;

        // Center on campfire if present, otherwise center of map
        Vector3 center = new Vector3((c - r) * hw * 0.5f, (c + r) * hh * 0.5f, transform.position.z);
        if (layout.buildings != null)
        {
            foreach (var b in layout.buildings)
            {
                if (b.type == BuildingType.Campfire)
                {
                    var wp = layout.TileToWorld(b.position.x, b.position.y);
                    center = new Vector3(wp.x, wp.y, transform.position.z);
                    break;
                }
            }
        }
        transform.position = center;
    }

    void Update()
    {
        HandleKeyboardPan();
        HandleMiddleMouseDrag();
        HandleZoom();
        HandleTouch();
        ClampPosition();
    }

    private void HandleKeyboardPan()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        var dir = Vector3.zero;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    dir.y += 1;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  dir.y -= 1;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  dir.x -= 1;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) dir.x += 1;

        transform.position += dir.normalized * panSpeed * Time.deltaTime;
    }

    private void HandleMiddleMouseDrag()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool pressedThisFrame  = mouse.middleButton.wasPressedThisFrame  || mouse.rightButton.wasPressedThisFrame;
        bool releasedThisFrame = mouse.middleButton.wasReleasedThisFrame  || mouse.rightButton.wasReleasedThisFrame;
        bool held              = mouse.middleButton.isPressed             || mouse.rightButton.isPressed;

        if (pressedThisFrame)
        {
            _dragOrigin = _cam.ScreenToWorldPoint(mouse.position.ReadValue());
            _dragging = true;
        }
        if (releasedThisFrame || !held) _dragging = false;

        if (_dragging)
        {
            var diff = _dragOrigin - _cam.ScreenToWorldPoint((Vector3)mouse.position.ReadValue());
            transform.position += diff;
        }
    }

    private void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.001f) return;
        _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize - scroll * zoomSpeed * 0.05f,
                                             zoomMin, zoomMax);
    }

    private Vector2 _touchLastPos;
    private float   _touchLastDist;

    private void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == UnityEngine.TouchPhase.Began)
            {
                _touchLastPos = t.position;
            }
            else if (t.phase == UnityEngine.TouchPhase.Moved)
            {
                Vector3 prev = _cam.ScreenToWorldPoint(new Vector3(_touchLastPos.x, _touchLastPos.y, 0));
                Vector3 curr = _cam.ScreenToWorldPoint(new Vector3(t.position.x,    t.position.y,    0));
                transform.position += prev - curr;
                _touchLastPos = t.position;
            }
        }
        else if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            float dist = Vector2.Distance(t0.position, t1.position);

            if (t1.phase == UnityEngine.TouchPhase.Began)
            {
                _touchLastDist = dist;
            }
            else if (t0.phase == UnityEngine.TouchPhase.Moved || t1.phase == UnityEngine.TouchPhase.Moved)
            {
                float delta = _touchLastDist - dist;
                _cam.orthographicSize = Mathf.Clamp(
                    _cam.orthographicSize + delta * 0.01f, zoomMin, zoomMax);
                _touchLastDist = dist;
            }
        }
    }

    private void ClampPosition()
    {
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }
}
