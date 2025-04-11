using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public Transform player;
    public float moveSpeed = 2f;
    public float chaseRange = 5f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;

    public enum FacingDirection { Down, Up, Left, Right }
    public enum EnemyState { Idle, Moving, Attacking }

    [HideInInspector] public FacingDirection currentDirection;
    [HideInInspector] public bool isMoving;
    [HideInInspector] public EnemyState currentState;

    private Rigidbody2D rb;
    private float lastAttackTime;
    
    [HideInInspector] public bool attackAnimationComplete = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        currentState = EnemyState.Idle;
    }

    void Update()
    {
        if (player == null) return;

        // Se estiver no meio de um ataque, não faz nada até terminar
        if (currentState == EnemyState.Attacking && !attackAnimationComplete)
        {
            return; // Impede qualquer movimento durante a animação de ataque
        }

        Vector2 direction = (player.position - transform.position);
        float distance = direction.magnitude;

        // Verifica se pode atacar
        if (distance <= attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            // Define a direção do ataque com base na posição do jogador
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                currentDirection = direction.x < 0 ? FacingDirection.Left : FacingDirection.Right;
            else
                currentDirection = direction.y < 0 ? FacingDirection.Down : FacingDirection.Up;
            
            // Inicia o ataque
            currentState = EnemyState.Attacking;
            isMoving = false;
            attackAnimationComplete = false;  // Indica que a animação de ataque está em andamento
            lastAttackTime = Time.time;
            return;
        }

        // Se estiver dentro do range de perseguição, mas fora do range de ataque
        if (distance < chaseRange && distance > attackRange)
        {
            currentState = EnemyState.Moving;
            isMoving = true;
            
            direction.Normalize();
            rb.MovePosition(rb.position + direction * moveSpeed * Time.deltaTime);

            // Atualiza a direção atual
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                currentDirection = direction.x < 0 ? FacingDirection.Left : FacingDirection.Right;
            else
                currentDirection = direction.y < 0 ? FacingDirection.Down : FacingDirection.Up;
        }
        else if (distance >= chaseRange) // Se estiver fora do alcance de perseguição
        {
            currentState = EnemyState.Idle;
            isMoving = false;
        }
    }
    
    // Este método é crucial e deve ser chamado quando a animação de ataque terminar
    public void AttackAnimationFinished()
    {
        Debug.Log("Animação de ataque finalizada"); // Debug para verificar se está sendo chamado
        attackAnimationComplete = true;
        currentState = EnemyState.Idle;
    }
}