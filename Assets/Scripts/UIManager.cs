using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Textes")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Timer (visuel)")]
    [SerializeField] private Image timerFillImage;
    [SerializeField] private Color timerNormalColor = Color.white;
    [SerializeField] private Color timerWarningColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color timerCriticalColor = Color.red;
    [SerializeField] private float warningThreshold = 15f;
    [SerializeField] private float criticalThreshold = 7f;

    [Header("Panels")]
    [SerializeField] private GameObject placementPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject wavePanel;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Boutons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button nextWaveButton;
    [SerializeField] private Button restartButton;

    [Header("Fin de partie")]
    [SerializeField] private TextMeshProUGUI finalScoreText;

    private float maxWaveTime = 45f;

    private readonly string[] placementMessages = new[]
    {
        "Placez la poubelle NOIRE (ordures ménagères)",
        "Placez la poubelle JAUNE (emballages)",
        "Placez la poubelle VERTE (verre)",
        "Toutes les poubelles sont placées !"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnScoreChanged += UpdateScore;
        GameManager.Instance.OnStateChanged += HandleStateChange;
        GameManager.Instance.OnWaveChanged += UpdateWave;
        GameManager.Instance.OnTimeChanged += UpdateTimer;

        startButton.onClick.AddListener(() => {
            startButton.gameObject.SetActive(false);
            GameManager.Instance.StartGame();
        });
        nextWaveButton.onClick.AddListener(() => GameManager.Instance.StartNextWave());
        restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());

        UpdatePlacementInstruction(0);
        gameplayPanel.SetActive(false);
        wavePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        startButton.gameObject.SetActive(false);
    }

    public void UpdatePlacementInstruction(int index)
    {
        if (instructionText != null && index < placementMessages.Length)
            instructionText.text = placementMessages[index];
    }

    public void ShowStartButton() => startButton.gameObject.SetActive(true);

    private void UpdateScore(int score) => scoreText.text = $"Score : {score}";

    private void UpdateWave(int wave)
    {
        waveText.text = $"Vague {wave}/{GameManager.Instance.TotalWaves}";
        maxWaveTime = Mathf.Max(GameManager.Instance.TimeRemaining, 1f);
    }

    private void UpdateTimer(float time)
    {
        if (timerText != null)
        {
            int sec = Mathf.CeilToInt(time);
            timerText.text = $"{sec}s";

            Color c = time <= criticalThreshold ? timerCriticalColor : time <= warningThreshold ? timerWarningColor : timerNormalColor;
            timerText.color = c;
        }

        if (timerFillImage != null)
            timerFillImage.fillAmount = Mathf.Clamp01(time / maxWaveTime);
    }

    private void HandleStateChange(GameState state)
    {
        placementPanel.SetActive(state == GameState.PlacingBins);
        gameplayPanel.SetActive(state == GameState.Playing);
        wavePanel.SetActive(state == GameState.WaveComplete);
        gameOverPanel.SetActive(state == GameState.GameOver);

        if (state == GameState.GameOver && finalScoreText != null)
            finalScoreText.text = $"Score final : {GameManager.Instance.Score}";
    }
}