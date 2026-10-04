using System;
using UnityEngine;

[Serializable]
public class WaveSettings
{
    [Min(1f)] public float duration = 30f;
    [Min(1)] public int maxEnemiesAlive = 10;
    [Min(1)] public int enemyHealth = 10;
    [Min(0.05f)] public float spawnInterval = 1.5f;
    [Min(0.1f)] public float enemySpeed = 2f;

    public WaveSettings(float seconds, int count, int health, float interval, float speed)
    {
        duration = seconds;
        maxEnemiesAlive = count;
        enemyHealth = health;
        spawnInterval = interval;
        enemySpeed = speed;
    }
}

public class WaveManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField, Min(0.1f)] private float intermissionDuration = 2f;
    [SerializeField] private WaveSettings[] waves =
    {
        new WaveSettings(30f, 10, 10, 1.5f, 2f),
        new WaveSettings(30f, 15, 20, 1f, 2.5f),
        new WaveSettings(45f, 30, 30, 0.6f, 3f)
    };

    public float RemainingTime { get; private set; }
    public float IntermissionRemaining { get; private set; }
    public int CurrentWave { get; private set; } = 1;
    public int TotalWaves => waves.Length;
    public bool IsLastWave => CurrentWave >= TotalWaves;
    public WaveSettings CurrentSettings => waves[CurrentWave - 1];

    private void Awake()
    {
        RemainingTime = Mathf.Max(1f, CurrentSettings.duration);
    }

    private void Start()
    {
        if (gameManager == null || enemySpawner == null)
        {
            Debug.LogError("WaveManager: hãy gắn Game Manager và Enemy Spawner trong Inspector.", this);
            enabled = false;
            return;
        }

        enemySpawner.ConfigureForWave(CurrentSettings);
    }

    private void Update()
    {
        if (gameManager == null) return;

        if (gameManager.State == GameManager.GameState.WaveComplete)
        {
            // Gameplay đã dừng; thời gian nghỉ vẫn chạy bằng unscaledDeltaTime.
            IntermissionRemaining = Mathf.Max(0f, IntermissionRemaining - Time.unscaledDeltaTime);
            if (IntermissionRemaining <= 0f) AdvanceWave();
            return;
        }

        if (!gameManager.IsPlaying) return;
        RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
        if (RemainingTime <= 0f) gameManager.CompleteWave();
    }

    public void BeginIntermission()
    {
        IntermissionRemaining = Mathf.Max(0.1f, intermissionDuration);
    }

    private void AdvanceWave()
    {
        if (IsLastWave || gameManager.State != GameManager.GameState.WaveComplete) return;
        CurrentWave++;
        RemainingTime = Mathf.Max(1f, CurrentSettings.duration);
        enemySpawner.ConfigureForWave(CurrentSettings);
        gameManager.BeginNextWave();
        Debug.Log($"Wave {CurrentWave} bắt đầu!");
    }
}
