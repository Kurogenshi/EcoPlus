using UnityEngine;

public class ARPerformanceSettings : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60;
    [SerializeField] private int shadowQualityLowEndThreshold = 4;

    private void Awake()
    {
        Application.targetFrameRate = targetFrameRate;
        QualitySettings.vSyncCount = 0;

        if (SystemInfo.systemMemorySize < shadowQualityLowEndThreshold * 1024)
        {
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.shadowDistance = 5f;
        }
        else
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.Medium;
            QualitySettings.shadowDistance = 10f;
        }

        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
    }
}