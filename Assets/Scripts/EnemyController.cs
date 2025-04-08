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
    public EquipmentSprites helmet;
    public EquipmentSprites chestplate;
    public EquipmentSprites gloves;
    public EquipmentSprites sword;
    public EquipmentSprites leggings;
    public EquipmentSprites boots;

    public float frameRate = 0.15f;

    private float animationTimer = 0f;
    private int currentFrame = 0;
    private EnemyAI ai;

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
            UpdatePart(helmet, GetSprites(helmet));
            UpdatePart(chestplate, GetSprites(chestplate));
            UpdatePart(gloves, GetSprites(gloves));
            UpdatePart(sword, GetSprites(sword));
            UpdatePart(leggings, GetSprites(leggings));
            UpdatePart(boots, GetSprites(boots));
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
}
