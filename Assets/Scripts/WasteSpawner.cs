using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[System.Serializable]
public class WastePrefabEntry
{
    public GameObject prefab;
    public TrashType type;
}

public class WasteSpawner : MonoBehaviour
{
    public static WasteSpawner Instance { get; private set; }

    [Header("Prefabs de déchets")]
    [SerializeField] private List<WastePrefabEntry> wastePrefabs;

    [Header("Configuration spawn")]
    [SerializeField] private int baseWasteCount = 5;
    [SerializeField] private int wasteIncreasePerWave = 2;

    [Header("Distribution spatiale")]
    [SerializeField] private float exclusionRadiusAroundBins = 0.5f;
    [SerializeField] private float minDistanceBetweenWastes = 0.25f;
    [SerializeField] private float edgeMargin = 0.15f;

    [SerializeField] private BinPlacementManager binPlacementManager;

    private int remainingWastes;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SpawnWave(int waveNumber)
    {
        int count = baseWasteCount + (waveNumber - 1) * wasteIncreasePerWave;
        remainingWastes = count;

        ARPlane plane = binPlacementManager.SelectedPlane;
        if (plane == null)
        {
            Debug.LogError("[WasteSpawner] Aucun plan sélectionné !");
            return;
        }

        Bin[] bins = FindObjectsByType<Bin>(FindObjectsSortMode.None);
        List<Vector3> binPositions = new();
        foreach (var b in bins) binPositions.Add(b.transform.position);

        List<Vector3> usedPositions = new();
        int spawned = 0;

        for (int i = 0; i < count; i++)
        {
            if (TryGetValidSpawnPosition(plane, binPositions, usedPositions, out Vector3 pos))
            {
                SpawnWasteAt(pos);
                usedPositions.Add(pos);
                spawned++;
            }
        }

        remainingWastes = spawned;

        AudioManager.Instance?.PlayWaveStart();

        if (spawned < count)
            Debug.LogWarning($"[WasteSpawner] Seulement {spawned}/{count} déchets placés (espace limité).");
    }

    private void SpawnWasteAt(Vector3 position)
    {
        WastePrefabEntry entry = wastePrefabs[Random.Range(0, wastePrefabs.Count)];
        Quaternion rot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        GameObject obj = Instantiate(entry.prefab, position, rot);

        if (obj.GetComponent<Waste>() == null) obj.AddComponent<Waste>();
    }

    private bool TryGetValidSpawnPosition(ARPlane plane, List<Vector3> binPositions, List<Vector3> existingWastes, out Vector3 result)
    {
        Vector2 halfSize = plane.size * 0.5f;
        halfSize -= Vector2.one * edgeMargin;
        halfSize.x = Mathf.Max(0.05f, halfSize.x);
        halfSize.y = Mathf.Max(0.05f, halfSize.y);

        NativeArray<Vector2> boundary = plane.boundary;

        for (int tries = 0; tries < 40; tries++)
        {
            Vector2 localPoint = new Vector2(Random.Range(-halfSize.x, halfSize.x), Random.Range(-halfSize.y, halfSize.y));

            if (boundary.Length >= 3 && !IsPointInPolygon(localPoint, boundary)) continue;

            Vector3 worldPoint = plane.transform.TransformPoint(new Vector3(localPoint.x, 0, localPoint.y));

            if (IsTooCloseToAny(worldPoint, binPositions, exclusionRadiusAroundBins)) continue;

            if (IsTooCloseToAny(worldPoint, existingWastes, minDistanceBetweenWastes)) continue;

            result = worldPoint;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private bool IsTooCloseToAny(Vector3 point, List<Vector3> others, float minDist)
    {
        Vector3 flat = new Vector3(point.x, 0, point.z);
        foreach (var o in others)
        {
            Vector3 oFlat = new Vector3(o.x, 0, o.z);
            if (Vector3.Distance(flat, oFlat) < minDist) return true;
        }
        return false;
    }

    private bool IsPointInPolygon(Vector2 point, NativeArray<Vector2> polygon)
    {
        int intersections = 0;
        int n = polygon.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % n];
            if ((a.y > point.y) != (b.y > point.y))
            {
                float t = (point.y - a.y) / (b.y - a.y);
                float xIntersect = a.x + t * (b.x - a.x);
                if (point.x < xIntersect) intersections++;
            }
        }
        return (intersections & 1) == 1;
    }

    public void NotifyWasteProcessed()
    {
        remainingWastes--;
        if (remainingWastes <= 0)
        {
            AudioManager.Instance?.PlayWaveComplete();
            GameManager.Instance.OnWaveFinished();
        }
    }
}