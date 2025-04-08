using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 2f;
    public float chaseRange = 5f;

    public enum FacingDirection { Down, Up, Left, Right }

    [HideInInspector] public FacingDirection currentDirection;
    [HideInInspector] public bool isMoving;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (player == null) return;

        Vector2 direction = (player.position - transform.position);
        float distance = direction.magnitude;

        isMoving = distance < chaseRange;

        if (isMoving)
        {
            direction.Normalize();
            rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);

            // Atualiza a direção atual
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                currentDirection = direction.x < 0 ? FacingDirection.Left : FacingDirection.Right;
            else
                currentDirection = direction.y < 0 ? FacingDirection.Down : FacingDirection.Up;
        }
    }
}
