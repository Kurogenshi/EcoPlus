using System;
using UnityEngine;

public enum TrashType { OrduresMenageres, Emballages, Verre }
public enum GameState { PlacingBins, Playing, WaveComplete, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Score")]
    [SerializeField] private int scorePerGoodTrash = 10;
    [SerializeField] private int scorePerBadTrash = -5;

    [Header("Vagues")]
    [SerializeField] private int totalWaves = 5;

    [Header("Chronomètre")]
    [SerializeField] private float timePerWave = 45f;
    [SerializeField] private float baseTimeReduction = 5f;
    [SerializeField] private float minWaveTime = 20f;
    [SerializeField] private float tickWarningThreshold = 10f;

    public GameState CurrentState { get; private set; } = GameState.PlacingBins;
    public int Score { get; private set; }
    public int CurrentWave { get; private set; }
    public int TotalWaves => totalWaves;
    public float TimeRemaining { get; private set; }

    public event Action<int> OnScoreChanged;
    public event Action<GameState> OnStateChanged;
    public event Action<int> OnWaveChanged;
    public event Action<float> OnTimeChanged;

    private bool tickPlayedThisSecond;
    private int lastWholeSecond = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (CurrentState != GameState.Playing) return;

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining < 0) TimeRemaining = 0;

        OnTimeChanged?.Invoke(TimeRemaining);

        int whole = Mathf.CeilToInt(TimeRemaining);
        if (TimeRemaining <= tickWarningThreshold && whole != lastWholeSecond && whole > 0)
        {
            AudioManager.Instance?.PlayTick();
            lastWholeSecond = whole;
        }

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            HandleTimeout();
        }
    }

    private void HandleTimeout()
    {
        AudioManager.Instance?.PlayGameOver();
        ChangeState(GameState.GameOver);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] État : {newState}");
    }

    public void AddScore(bool isCorrect)
    {
        Score += isCorrect ? scorePerGoodTrash : scorePerBadTrash;
        OnScoreChanged?.Invoke(Score);
    }

    public void StartGame()
    {
        Score = 0;
        CurrentWave = 0;
        OnScoreChanged?.Invoke(Score);
        StartNextWave();
    }

    public void StartNextWave()
    {
        CurrentWave++;
        OnWaveChanged?.Invoke(CurrentWave);

        if (CurrentWave > totalWaves)
        {
            AudioManager.Instance?.PlayGameOver();
            ChangeState(GameState.GameOver);
            return;
        }

        float computedTime = timePerWave - (CurrentWave - 1) * baseTimeReduction;
        TimeRemaining = Mathf.Max(minWaveTime, computedTime);
        lastWholeSecond = -1;
        OnTimeChanged?.Invoke(TimeRemaining);

        ChangeState(GameState.Playing);
        WasteSpawner.Instance?.SpawnWave(CurrentWave);
    }

    public void OnWaveFinished()
    {
        if (CurrentWave >= totalWaves)
        {
            AudioManager.Instance?.PlayGameOver();
            ChangeState(GameState.GameOver);
        }
        else
        {
            ChangeState(GameState.WaveComplete);
        }
    }

    public void RestartGame()
    {
        Score = 0;
        CurrentWave = 0;
        OnScoreChanged?.Invoke(Score);
        StartGame();
    }
}