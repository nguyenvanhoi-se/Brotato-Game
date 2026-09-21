using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemiesParent;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField, Min(0.1f)] private float spawnMargin = 1f;
    [SerializeField, Min(0f)] private float minimumPlayerDistance = 3f;

    private WaveSettings settings;
    private float nextSpawnTime;
    public int AliveCount
    {
        get
        {
            if (enemiesParent == null) return 0;
            int count = 0;
            foreach (EnemyHealth enemy in enemiesParent.GetComponentsInChildren<EnemyHealth>())
                if (enemy.CurrentHealth > 0) count++;
            return count;
        }
    }

    public void ConfigureForWave(WaveSettings wave)
    {
        settings = wave;
        nextSpawnTime = Time.time;
        // Đồng bộ cả quái đặt sẵn trong scene ở Wave 1.
        if (enemiesParent != null)
            foreach (EnemyHealth enemy in enemiesParent.GetComponentsInChildren<EnemyHealth>())
                ConfigureEnemy(enemy.gameObject);
    }

    private void Update()
    {
        if (settings == null || enemyPrefab == null || player == null ||
            enemiesParent == null || gameplayCamera == null || Time.time < nextSpawnTime)
            return;

        nextSpawnTime = Time.time + Mathf.Max(0.05f, settings.spawnInterval);
        if (AliveCount < Mathf.Max(1, settings.maxEnemiesAlive)) SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        // Không sinh ngay trên Player, kể cả khi camera chưa kịp cập nhật frame đầu.
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector3 position = GetSpawnPosition();
            if (Vector2.Distance(position, player.position) < minimumPlayerDistance) continue;
            GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity, enemiesParent);
            ConfigureEnemy(enemy);
            return;
        }
    }

    private void ConfigureEnemy(GameObject enemy)
    {
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.SetTarget(player);
            controller.SetMoveSpeed(settings.enemySpeed);
        }
        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null) health.InitializeHealth(settings.enemyHealth);
    }

    public void ClearEnemies()
    {
        if (enemiesParent == null) return;
        foreach (EnemyHealth enemy in enemiesParent.GetComponentsInChildren<EnemyHealth>())
        {
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }
    }

    private Vector3 GetSpawnPosition()
    {
        float halfHeight = gameplayCamera.orthographicSize;
        float halfWidth = halfHeight * gameplayCamera.aspect;
        Vector3 center = gameplayCamera.transform.position;
        float margin = Mathf.Max(0.1f, spawnMargin);
        float left = center.x - halfWidth - margin;
        float right = center.x + halfWidth + margin;
        float bottom = center.y - halfHeight - margin;
        float top = center.y + halfHeight + margin;
        switch (Random.Range(0, 4))
        {
            case 0: return new Vector3(left, Random.Range(bottom, top), player.position.z);
            case 1: return new Vector3(right, Random.Range(bottom, top), player.position.z);
            case 2: return new Vector3(Random.Range(left, right), bottom, player.position.z);
            default: return new Vector3(Random.Range(left, right), top, player.position.z);
        }
    }
}
