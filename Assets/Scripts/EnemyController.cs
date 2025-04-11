using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [System.Serializable]
    public class EquipmentSprites
    {
        public SpriteRenderer renderer;
        public Sprite[] idleDown;
        public Sprite[] walkDown;
        public Sprite[] idleUp;
        public Sprite[] walkUp;
        public Sprite[] idleLeft;
        public Sprite[] walkLeft;
        public Sprite[] idleRight;
        public Sprite[] walkRight;
        public Sprite[] attackDown;
        public Sprite[] attackUp;
        public Sprite[] attackLeft;
        public Sprite[] attackRight;
    }

    public EquipmentSprites body;

    public float frameRate = 0.15f;
    public float attackFrameRate = 0.1f;
    public int attackHitFrame = 2; // Frame em que o hit e o som do ataque devem ocorrer (0-baseado)
    public float attackDamage = 10f; // Dano causado pelo ataque
    public float attackRadius = 0.7f; // Raio de detecção do hit

    private float animationTimer = 0f;
    private int currentFrame = 0;
    private EnemyAI ai;
    private bool isPlayingAttackAnimation = false;
    private Sprite[] currentAnimationSprites;
    private bool hitRegistered = false; // Para garantir que o hit ocorra apenas uma vez por ataque

    [Header("Footstep Audio")]
    public AudioSource footstepAudioSource;
    public AudioClip[] footstepClips;
    public float footstepDelay = 0.3f;
    private float footstepTimer = 0f;
    
    [Header("Attack Audio")]
    public AudioSource attackAudioSource;
    public AudioClip[] attackClips;
    private bool attackSoundPlayed = false;

    // Layer que contém o player
    public LayerMask playerLayer;

    void Start()
    {
        ai = GetComponent<EnemyAI>();
        footstepTimer = footstepDelay;
    }

    void Update()
    {
        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        // Determina quais sprites usar baseado no estado atual
        DetermineAnimationSprites();
        
        // Progride a animação baseado no tempo
        float currentFrameRate = isPlayingAttackAnimation ? attackFrameRate : frameRate;
        animationTimer += Time.deltaTime;

        if (animationTimer >= currentFrameRate)
        {
            animationTimer = 0f;
            AdvanceAnimationFrame();
        }
        
        // Atualiza o sprite atual
        if (currentAnimationSprites != null && currentAnimationSprites.Length > 0)
        {
            int frameIndex = Mathf.Min(currentFrame, currentAnimationSprites.Length - 1);
            body.renderer.sprite = currentAnimationSprites[frameIndex];
        }
    }
    
    void DetermineAnimationSprites()
    {
        // Lógica para animação de ataque
        if (ai.currentState == EnemyAI.EnemyState.Attacking && !ai.attackAnimationComplete)
        {
            // Se acabamos de entrar no estado de ataque
            if (!isPlayingAttackAnimation)
            {
                isPlayingAttackAnimation = true;
                currentFrame = 0;
                attackSoundPlayed = false;  // Resetamos o flag para garantir que o som toque na próxima animação
                hitRegistered = false;      // Reset do hit para a próxima animação
            }
            
            // Seleciona a animação de ataque baseada na direção
            currentAnimationSprites = ai.currentDirection switch
            {
                EnemyAI.FacingDirection.Down => body.attackDown,
                EnemyAI.FacingDirection.Up => body.attackUp,
                EnemyAI.FacingDirection.Left => body.attackLeft,
                EnemyAI.FacingDirection.Right => body.attackRight,
                _ => body.attackDown
            };
        }
        // Lógica para animação de movimento/idle
        else
        {
            isPlayingAttackAnimation = false;
            
            if (ai.isMoving)
            {
                currentAnimationSprites = ai.currentDirection switch
                {
                    EnemyAI.FacingDirection.Down => body.walkDown,
                    EnemyAI.FacingDirection.Up => body.walkUp,
                    EnemyAI.FacingDirection.Left => body.walkLeft,
                    EnemyAI.FacingDirection.Right => body.walkRight,
                    _ => body.walkDown
                };
            }
            else
            {
                currentAnimationSprites = ai.currentDirection switch
                {
                    EnemyAI.FacingDirection.Down => body.idleDown,
                    EnemyAI.FacingDirection.Up => body.idleUp,
                    EnemyAI.FacingDirection.Left => body.idleLeft,
                    EnemyAI.FacingDirection.Right => body.idleRight,
                    _ => body.idleDown
                };
            }
        }
    }
    
   void AdvanceAnimationFrame()
   {
        // Lógica específica para animação de ataque
        if (isPlayingAttackAnimation)
        {
            // Verifica se atingimos o frame específico para tocar o som e registrar o hit
            if (currentFrame == attackHitFrame && !attackSoundPlayed)
            {
                // Toca o som de ataque
                if (attackAudioSource != null && attackClips.Length > 0)
                {
                    int index = Random.Range(0, attackClips.Length);
                    attackAudioSource.PlayOneShot(attackClips[index]);
                    attackSoundPlayed = true;
                }
                
                // Registra o hit no player
                DetectHit();
            }
            
            currentFrame++;
            
            // Verifica se a animação de ataque terminou
            if (currentFrame >= currentAnimationSprites.Length)
            {
                ai.AttackAnimationFinished(); // Notifica o AI que a animação terminou
                isPlayingAttackAnimation = false;
                currentFrame = 0; // Reset para próxima animação
            }
        }
        // Lógica para animações normais (cíclicas)
        else
        {
            currentFrame = (currentFrame + 1) % currentAnimationSprites.Length;
            
            // Som de passos
            if (ai.isMoving)
            {
                footstepTimer -= frameRate;
                if (footstepTimer <= 0f)
                {
                    PlayFootstepSound();
                    footstepTimer = footstepDelay;
                }
            }
        }
    }


    void DetectHit()
    {
        if (hitRegistered) return;
        
        Vector2 hitPosition = transform.position;
        
        switch (ai.currentDirection)
        {
            case EnemyAI.FacingDirection.Up:
                hitPosition += Vector2.up * 0.5f;
                break;
            case EnemyAI.FacingDirection.Down:
                hitPosition += Vector2.down * 0.5f;
                break;
            case EnemyAI.FacingDirection.Left:
                hitPosition += Vector2.left * 0.5f;
                break;
            case EnemyAI.FacingDirection.Right:
                hitPosition += Vector2.right * 0.5f;
                break;
        }
        
        // Verifica colisão com qualquer objeto e procura pela tag "Player"
        Collider2D[] colliders = Physics2D.OverlapCircleAll(hitPosition, attackRadius);
        
        foreach (Collider2D col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                Debug.Log("Hit registrado no player!");
                
                PlayerHealth playerHealth = col.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(attackDamage);
                }
                
                hitRegistered = true;
                break;
            }
        }
    }

    void PlayFootstepSound()
    {
        if (footstepClips.Length > 0 && footstepAudioSource != null)
        {
            int index = Random.Range(0, footstepClips.Length);
            footstepAudioSource.PlayOneShot(footstepClips[index]);
        }
    }
    
    // Método para visualizar a área de ataque no editor
    void OnDrawGizmosSelected()
    {
        // Mostra o raio de ataque para facilitar o ajuste no editor
        Gizmos.color = Color.red;
        Vector2 hitPosition = transform.position;
        
        // Se o componente AI não estiver disponível durante o editor, use uma direção padrão
        if (ai != null)
        {
            switch (ai.currentDirection)
            {
                case EnemyAI.FacingDirection.Up:
                    hitPosition += Vector2.up * 0.5f;
                    break;
                case EnemyAI.FacingDirection.Down:
                    hitPosition += Vector2.down * 0.5f;
                    break;
                case EnemyAI.FacingDirection.Left:
                    hitPosition += Vector2.left * 0.5f;
                    break;
                case EnemyAI.FacingDirection.Right:
                    hitPosition += Vector2.right * 0.5f;
                    break;
            }
        }
        
        Gizmos.DrawWireSphere(hitPosition, attackRadius);
    }
}