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
    }

    public EquipmentSprites body;

    public float frameRate = 0.15f;

    private float animationTimer = 0f;
    private int currentFrame = 0;
    private EnemyAI ai;

    [Header("Footstep Audio")]
    public AudioSource footstepAudioSource;
    public AudioClip[] footstepClips;
    public float footstepDelay = 0.3f;
    private float footstepTimer = 0f;

    void Start()
    {
        ai = GetComponent<EnemyAI>();
    }

    void Update()
    {
        UpdateAnimation();
    }

    void UpdateAnimation()
    {
        Sprite[] GetSprites(EquipmentSprites part)
        {
            if (ai.isMoving)
            {
                footstepTimer -= frameRate;
                if (footstepTimer <= 0f)
                {
                     PlayFootstepSound();
                     footstepTimer = footstepDelay;
                }
                return ai.currentDirection switch
                {
                    EnemyAI.FacingDirection.Down => part.walkDown,
                    EnemyAI.FacingDirection.Up => part.walkUp,
                    EnemyAI.FacingDirection.Left => part.walkLeft,
                    EnemyAI.FacingDirection.Right => part.walkRight,
                    _ => part.walkDown
                };
            }
            else
            {
                return ai.currentDirection switch
                {
                    EnemyAI.FacingDirection.Down => part.idleDown,
                    EnemyAI.FacingDirection.Up => part.idleUp,
                    EnemyAI.FacingDirection.Left => part.idleLeft,
                    EnemyAI.FacingDirection.Right => part.idleRight,
                    _ => part.idleDown
                };
            }
        }

        animationTimer += Time.deltaTime;

        if (animationTimer >= frameRate)
        {
            animationTimer = 0;
            currentFrame++;

            // Atualiza cada parte
            UpdatePart(body, GetSprites(body));
        }
    }

    void UpdatePart(EquipmentSprites part, Sprite[] frames)
    {
        if (frames.Length > 0)
        {
            int frameIndex = currentFrame % frames.Length;
            part.renderer.sprite = frames[frameIndex];
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
}
