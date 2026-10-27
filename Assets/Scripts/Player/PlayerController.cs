using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Transform playerVisual;

    private Rigidbody2D playerRigidbody;
    private Vector2 movementInput;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            movementInput = Vector2.zero;
            return;
        }

        float horizontalInput = 0f;
        float verticalInput = 0f;

        if (keyboard.aKey.isPressed)
        {
            horizontalInput -= 1f;
        }

        if (keyboard.dKey.isPressed)
        {
            horizontalInput += 1f;
        }

        if (keyboard.sKey.isPressed)
        {
            verticalInput -= 1f;
        }

        if (keyboard.wKey.isPressed)
        {
            verticalInput += 1f;
        }

        movementInput = new Vector2(horizontalInput, verticalInput).normalized;
        UpdateFacingDirection();
    }



private void UpdateFacingDirection()
{
    if (playerVisual != null && movementInput.sqrMagnitude > 0f)
    {
        float angle = Mathf.Atan2(movementInput.y, movementInput.x) * Mathf.Rad2Deg;
        playerVisual.rotation = Quaternion.Euler(0f, 0f, angle);
    }
}

    private void FixedUpdate()
    {
        Vector2 nextPosition = playerRigidbody.position
            + movementInput * moveSpeed * Time.fixedDeltaTime;

        playerRigidbody.MovePosition(nextPosition);
    }
}
