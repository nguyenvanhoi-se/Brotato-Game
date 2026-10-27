using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float damageCooldown = 0.75f;

    [SerializeField] private int currentHealth;

    private float nextDamageTime;
    private bool isDead;
    private bool canTakeDamage = true;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public event Action Died;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (!canTakeDamage || isDead || damage <= 0)
        {
            return;
        }

        if (Time.time < nextDamageTime)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        nextDamageTime = Time.time + damageCooldown;

        Debug.Log($"Player HP: {currentHealth}/{maxHealth}");

        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;

        Debug.Log("Player đã chết!");
        Died?.Invoke();
    }

    public void SetDamageEnabled(bool value)
    {
        canTakeDamage = value;
    }
}
