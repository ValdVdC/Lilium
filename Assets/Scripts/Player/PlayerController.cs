using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float movementThreshold = 0.1f;
    public float accelerationTime = 0.1f;

    [Header("Animation")]
    public SpriteRenderer spriteRenderer;
    public Sprite[] idleDownSprites;
    public Sprite[] walkDownSprites;
    public Sprite[] idleUpSprites;
    public Sprite[] walkUpSprites;
    public Sprite[] idleLeftSprites;
    public Sprite[] walkLeftSprites;
    public Sprite[] idleRightSprites;
    public Sprite[] walkRightSprites;

    public BoxCollider2D colliderDown;
    public BoxCollider2D colliderUp;
    public BoxCollider2D colliderLeft;
    public BoxCollider2D colliderRight;

    [Header("Footstep Audio")]
    public AudioSource footstepAudioSource;
    public AudioClip[] footstepClips;
    public float footstepDelay = 0.3f;
    private float footstepTimer = 0f;

    [Header("Interaction")]
    public InteractorController interactor;

    private Vector2 movement;
    private Vector2 currentVelocity;
    private Vector2 targetVelocity;
    private Vector2 velocitySmoothingRef;

    public enum FacingDirection { Down, Up, Left, Right }
    public FacingDirection currentDirection = FacingDirection.Down;

    private float animationTimer = 0f;
    public int currentFrame = 0;
    private float frameRate = 0.15f;
    public bool isMoving = false;

    void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        spriteRenderer.sprite = idleDownSprites[0];
        UpdateColliderForDirection();

        if (interactor == null)
            interactor = GetComponent<InteractorController>();
    }

    void Update()
    {
        // Movimento do jogador
        targetVelocity.x = Input.GetAxisRaw("Horizontal");
        targetVelocity.y = Input.GetAxisRaw("Vertical");

        if (targetVelocity.magnitude > 0)
            targetVelocity.Normalize();

        currentVelocity = Vector2.SmoothDamp(
            currentVelocity,
            targetVelocity,
            ref velocitySmoothingRef,
            accelerationTime
        );

        isMoving = currentVelocity.magnitude > movementThreshold;

        if (isMoving)
        {
            if (Mathf.Abs(currentVelocity.x) > Mathf.Abs(currentVelocity.y))
            {
                currentDirection = currentVelocity.x < 0 ? FacingDirection.Left : FacingDirection.Right;
            }
            else
            {
                currentDirection = currentVelocity.y < 0 ? FacingDirection.Down : FacingDirection.Up;
            }
        }

        UpdateAnimation();

        if (isMoving)
        {
            transform.Translate(currentVelocity * moveSpeed * Time.deltaTime);
        }
    }

    void UpdateAnimation()
    {
        Sprite[] currentAnimationSprites;

        if (isMoving)
        {
            switch (currentDirection)
            {
                case FacingDirection.Down:
                    currentAnimationSprites = walkDownSprites;
                    break;
                case FacingDirection.Up:
                    currentAnimationSprites = walkUpSprites;
                    break;
                case FacingDirection.Left:
                    currentAnimationSprites = walkLeftSprites;
                    break;
                case FacingDirection.Right:
                    currentAnimationSprites = walkRightSprites;
                    break;
                default:
                    currentAnimationSprites = walkDownSprites;
                    break;
            }
        }
        else
        {
            switch (currentDirection)
            {
                case FacingDirection.Down:
                    currentAnimationSprites = idleDownSprites;
                    break;
                case FacingDirection.Up:
                    currentAnimationSprites = idleUpSprites;
                    break;
                case FacingDirection.Left:
                    currentAnimationSprites = idleLeftSprites;
                    break;
                case FacingDirection.Right:
                    currentAnimationSprites = idleRightSprites;
                    break;
                default:
                    currentAnimationSprites = idleDownSprites;
                    break;
            }
        }

        if (currentAnimationSprites.Length > 0)
        {
            animationTimer += Time.deltaTime;

            if (animationTimer >= frameRate)
            {
                animationTimer = 0;
                currentFrame = (currentFrame + 1) % currentAnimationSprites.Length;
                spriteRenderer.sprite = currentAnimationSprites[currentFrame];

                // Som de passo sincronizado com o frame
                if (isMoving)
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

        UpdateColliderForDirection();
    }

    void UpdateColliderForDirection()
    {
        colliderDown.enabled = false;
        colliderUp.enabled = false;
        colliderLeft.enabled = false;
        colliderRight.enabled = false;

        switch (currentDirection)
        {
            case FacingDirection.Down:
                colliderDown.enabled = true;
                break;
            case FacingDirection.Up:
                colliderUp.enabled = true;
                break;
            case FacingDirection.Left:
                colliderLeft.enabled = true;
                break;
            case FacingDirection.Right:
                colliderRight.enabled = true;
                break;
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

    // Método para ser chamado externamente para desativar ou reativar o movimento
    public void SetMovementEnabled(bool enabled)
    {
        if (!enabled)
        {
            currentVelocity = Vector2.zero;
            targetVelocity = Vector2.zero;
            isMoving = false;
        }
        
        // O script permanece habilitado, mas quando desabilitado o movimento
        // não processará entradas, mantendo o jogador estático
        this.enabled = enabled;
    }
}