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
