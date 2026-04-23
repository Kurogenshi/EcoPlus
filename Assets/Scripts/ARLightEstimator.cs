using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(Light))]
public class ARLightEstimator : MonoBehaviour
{
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private float smoothSpeed = 3f;

    [Header("Clamp")]
    [SerializeField] private float minIntensity = 0.3f;
    [SerializeField] private float maxIntensity = 2.0f;

    private Light directionalLight;
    private float targetIntensity = 1f;
    private Color targetColor = Color.white;
    private Vector3 targetDirection;

    private void Awake()
    {
        directionalLight = GetComponent<Light>();
        targetDirection = transform.forward;
    }

    private void OnEnable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived += OnFrameReceived;
    }

    private void OnDisable()
    {
        if (cameraManager != null)
            cameraManager.frameReceived -= OnFrameReceived;
    }

    private void OnFrameReceived(ARCameraFrameEventArgs args)
    {
        var lightEst = args.lightEstimation;

        if (lightEst.averageMainLightBrightness.HasValue)
            targetIntensity = Mathf.Clamp(lightEst.averageMainLightBrightness.Value, minIntensity, maxIntensity);
        else if (lightEst.averageBrightness.HasValue)
            targetIntensity = Mathf.Clamp(lightEst.averageBrightness.Value, minIntensity, maxIntensity);

        if (lightEst.colorCorrection.HasValue)
            targetColor = lightEst.colorCorrection.Value;

        if (lightEst.mainLightDirection.HasValue)
            targetDirection = lightEst.mainLightDirection.Value;

        if (lightEst.ambientSphericalHarmonics.HasValue)
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
    }

    private void Update()
    {
        directionalLight.intensity = Mathf.Lerp(directionalLight.intensity, targetIntensity, Time.deltaTime * smoothSpeed);
        directionalLight.color = Color.Lerp(directionalLight.color, targetColor, Time.deltaTime * smoothSpeed);

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
        }
    }
}