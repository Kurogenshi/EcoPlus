using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class BinPlacementManager : MonoBehaviour
{
    [Header("Références AR")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private Camera arCamera;

    [Header("Prefabs des 3 poubelles (dans l'ordre de placement)")]
    [SerializeField] private GameObject binNoirePrefab;
    [SerializeField] private GameObject binJaunePrefab;
    [SerializeField] private GameObject binVertePrefab;

    [Header("Zone de jeu")]
    [SerializeField] private GameObject playAreaIndicatorPrefab;

    [Header("Shadow receiver")]
    [SerializeField] private Material shadowReceiverMaterial;
    [SerializeField] private float shadowPlaneSize = 4f;

    private readonly List<ARRaycastHit> hits = new();
    private readonly List<GameObject> placedBins = new();
    private int currentBinIndex = 0;
    private ARPlane selectedPlane;

    public ARPlane SelectedPlane => selectedPlane;

    private void OnEnable()
    {
        TouchSimulation.Enable();
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        TouchSimulation.Disable();
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.PlacingBins) return;
        if (Touch.activeTouches.Count == 0) return;

        Touch touch = Touch.activeTouches[0];
        if (touch.phase != UnityEngine.InputSystem.TouchPhase.Began) return;

        if (raycastManager.Raycast(touch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;
            ARPlane hitPlane = planeManager.GetPlane(hits[0].trackableId);

            if (hitPlane.alignment != PlaneAlignment.HorizontalUp) return;

            PlaceBin(pose, hitPlane);
        }
    }

    private void PlaceBin(Pose pose, ARPlane plane)
    {
        GameObject prefab = GetPrefabForIndex(currentBinIndex);
        if (prefab == null) return;

        Vector3 cameraDir = arCamera.transform.position - pose.position;
        cameraDir.y = 0;
        Quaternion rotation = cameraDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(cameraDir) : Quaternion.identity;

        GameObject bin = Instantiate(prefab, pose.position, rotation);
        placedBins.Add(bin);
        AudioManager.Instance?.PlayBinPlaced();

        if (currentBinIndex == 0) selectedPlane = plane;

        currentBinIndex++;
        UIManager.Instance?.UpdatePlacementInstruction(currentBinIndex);

        if (currentBinIndex >= 3) FinishPlacement();
    }

    private GameObject GetPrefabForIndex(int index)
    {
        return index switch
        {
            0 => binNoirePrefab,
            1 => binJaunePrefab,
            2 => binVertePrefab,
            _ => null
        };
    }

    private void FinishPlacement()
    {
        planeManager.enabled = false;
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(false);

        if (shadowReceiverMaterial != null && selectedPlane != null)
        {
            GameObject shadowPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadowPlane.name = "AR_ShadowReceiver";
            shadowPlane.transform.position = selectedPlane.center + Vector3.up * 0.001f;
            shadowPlane.transform.rotation = Quaternion.Euler(90, 0, 0);
            shadowPlane.transform.localScale = Vector3.one * shadowPlaneSize;

            Destroy(shadowPlane.GetComponent<Collider>());
            shadowPlane.GetComponent<MeshRenderer>().material = shadowReceiverMaterial;
        }

        if (playAreaIndicatorPrefab != null && selectedPlane != null)
        {
            Instantiate(playAreaIndicatorPrefab, selectedPlane.center, Quaternion.identity);
        }

        UIManager.Instance?.ShowStartButton();
    }

    public void ResetPlacement()
    {
        foreach (var bin in placedBins) Destroy(bin);
        placedBins.Clear();
        currentBinIndex = 0;
        planeManager.enabled = true;
        foreach (var plane in planeManager.trackables)
            plane.gameObject.SetActive(true);
    }
}