using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[RequireComponent(typeof(Collider))]
public class Waste : MonoBehaviour
{
    [SerializeField] private TrashType type;
    [SerializeField] private float dragHeight = 0.15f;
    [SerializeField] private float binDetectionRadius = 0.30f;
    [SerializeField] private float binHighlightRadius = 0.45f;

    public TrashType Type => type;

    private Camera arCamera;
    private bool isBeingDragged;
    private int activeTouchId = -1;
    private float initialYGround;
    private Bin currentHighlightedBin;

    private void Start()
    {
        arCamera = Camera.main;
        initialYGround = transform.position.y;
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.Playing) return;

        foreach (var touch in Touch.activeTouches)
        {
            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    TryStartDrag(touch);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    if (isBeingDragged && touch.touchId == activeTouchId) DoDrag(touch);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (isBeingDragged && touch.touchId == activeTouchId) EndDrag();
                    break;
            }
        }
    }

    private void TryStartDrag(Touch touch)
    {
        Ray ray = arCamera.ScreenPointToRay(touch.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.gameObject == gameObject)
            {
                isBeingDragged = true;
                activeTouchId = touch.touchId;
                transform.position = new Vector3(transform.position.x, initialYGround + dragHeight, transform.position.z);
                AudioManager.Instance?.PlayWastePickup();
            }
        }
    }

    private void DoDrag(Touch touch)
    {
        Ray ray = arCamera.ScreenPointToRay(touch.screenPosition);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, initialYGround + dragHeight, 0));

        if (groundPlane.Raycast(ray, out float distance))
        {
            transform.position = ray.GetPoint(distance);
        }

        UpdateBinHighlight();
    }

    private void UpdateBinHighlight()
    {
        Bin closest = FindClosestBin(binHighlightRadius);

        if (closest != currentHighlightedBin)
        {
            if (currentHighlightedBin != null) currentHighlightedBin.SetHighlight(false);
            if (closest != null) closest.SetHighlight(true);
            currentHighlightedBin = closest;
        }
    }

    private void EndDrag()
    {
        isBeingDragged = false;
        activeTouchId = -1;

        if (currentHighlightedBin != null)
        {
            currentHighlightedBin.SetHighlight(false);
            currentHighlightedBin = null;
        }

        Bin target = FindClosestBin(binDetectionRadius);
        if (target != null)
        {
            target.ReceiveWaste(this);
        }
        else
        {
            AudioManager.Instance?.PlayWasteDrop();
            transform.position = new Vector3(transform.position.x, initialYGround, transform.position.z);
        }
    }

    private Bin FindClosestBin(float maxRadius)
    {
        Bin[] all = FindObjectsByType<Bin>(FindObjectsSortMode.None);
        Bin closest = null;
        float minDist = maxRadius;

        Vector3 myFlat = new Vector3(transform.position.x, 0, transform.position.z);
        foreach (var bin in all)
        {
            Vector3 binFlat = new Vector3(bin.transform.position.x, 0, bin.transform.position.z);
            float d = Vector3.Distance(myFlat, binFlat);
            if (d < minDist) { minDist = d; closest = bin; }
        }
        return closest;
    }

    private void OnDestroy()
    {
        if (currentHighlightedBin != null) currentHighlightedBin.SetHighlight(false);
    }
}