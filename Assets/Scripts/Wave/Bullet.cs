using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Bullet : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 3f;
    [SerializeField] private int damage = 10;

    private Rigidbody2D bulletRigidbody;
    private bool hasHit;

    private void Awake()
    {
        bulletRigidbody = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void FixedUpdate()
    {
        Vector2 nextPosition =
            bulletRigidbody.position
            + (Vector2)transform.right
            * speed
            * Time.fixedDeltaTime;

        bulletRigidbody.MovePosition(nextPosition);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit || !gameObject.activeInHierarchy)
        {
            return;
        }

        EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null && enemyHealth.CurrentHealth > 0)
        {
            // Destroy diễn ra cuối frame; chặn các callback từ collider khác ngay lập tức.
            hasHit = true;
            enemyHealth.TakeDamage(damage);
            gameObject.SetActive(false);
            Destroy(gameObject); 
        }
    }
}
