using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 30;

    private int currentHealth;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0 || damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        Debug.Log($"{gameObject.name} nhận {damage} sát thương. Máu còn lại: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void InitializeHealth(int health)
    {
        maxHealth = Mathf.Max(1, health);
        currentHealth = maxHealth;
    }
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
    private void Die()
    {
        Debug.Log($"{gameObject.name} đã bị tiêu diệt!");
        Destroy(gameObject);
    }
}
