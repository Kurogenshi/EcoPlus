using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Bin : MonoBehaviour
{
    [Header("Type accepté")]
    [SerializeField] private TrashType acceptedType;

    [Header("Effets")]
    [SerializeField] private ParticleSystem goodEffect;
    [SerializeField] private ParticleSystem badEffect;

    [Header("Highlight")]
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private Renderer[] highlightRenderers;
    [SerializeField] private Color highlightEmission = new Color(1f, 0.9f, 0.3f);
    [SerializeField] private float highlightIntensity = 0.8f;
    [SerializeField] private float hoverScaleMultiplier = 1.08f;

    public TrashType AcceptedType => acceptedType;

    private Vector3 lockedPosition;
    private Quaternion lockedRotation;
    private Vector3 originalScale;
    private bool transformLocked;

    private bool isHighlighted;
    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var anim in GetComponentsInChildren<Animator>())
            anim.enabled = false;

        propBlock = new MaterialPropertyBlock();

        if (highlightObject != null) highlightObject.SetActive(false);
    }

    private void Start()
    {
        lockedPosition = transform.position;
        lockedRotation = transform.rotation;
        originalScale = transform.localScale;
        transformLocked = true;
    }

    private void LateUpdate()
    {
        if (!transformLocked) return;

        transform.position = lockedPosition;
        transform.rotation = lockedRotation;
    }

    public void SetHighlight(bool on)
    {
        if (isHighlighted == on) return;
        isHighlighted = on;

        if (highlightObject != null) highlightObject.SetActive(on);

        if (highlightRenderers != null)
        {
            foreach (var r in highlightRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor",
                    on ? highlightEmission * highlightIntensity : Color.black);
                r.SetPropertyBlock(propBlock);
            }
        }

        transform.localScale = on ? originalScale * hoverScaleMultiplier : originalScale;

        if (on) AudioManager.Instance?.PlayBinHighlight();
    }

    public void ReceiveWaste(Waste waste)
    {
        bool isCorrect = waste.Type == acceptedType;
        GameManager.Instance.AddScore(isCorrect);

        if (isCorrect)
        {
            if (goodEffect != null) goodEffect.Play();
            AudioManager.Instance?.PlayCorrect();
        }
        else
        {
            if (badEffect != null) badEffect.Play();
            AudioManager.Instance?.PlayWrong();
        }

        SetHighlight(false);
        Destroy(waste.gameObject);
        WasteSpawner.Instance.NotifyWasteProcessed();
    }
}