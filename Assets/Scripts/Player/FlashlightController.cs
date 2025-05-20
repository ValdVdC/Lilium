using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

[System.Serializable]
public class LightAnchorPoint
{
    public PlayerController.FacingDirection direction;
    public bool isWalking;
    public int frameIndex;
    public Vector2 offset; // Offset relativo ao centro do personagem
}

public class FlashlightController : MonoBehaviour, ISaveable
{
    [Header("Referencias")]
    public Transform flashlightTransform; // Objeto da luz da lanterna
    public PlayerController playerController; // Referência ao seu controlador existente
    
    [Header("Configurações")]
    public float transitionSpeed = 5f; // Velocidade de transição entre posições
    public bool smoothTransition = false; // Ativar transição suave entre posições
    
    [Header("Pontos de Ancoragem")]
    public List<LightAnchorPoint> lightAnchorPoints = new List<LightAnchorPoint>();
    
    private Vector2 currentOffset = Vector2.zero;
    private Vector2 targetOffset = Vector2.zero;

    [Serializable]
    public class SaveData
    {
        public Vector2 currentOffset;
        public Vector2 targetOffset;
    }
    public object GetSaveData()
    {
        SaveData data = new SaveData
        {
            currentOffset = this.currentOffset,
            targetOffset = this.targetOffset
        };
        return data;
    }

    public void LoadFromSaveData(object saveData)
    {
        if (saveData is SaveData data)
        {
            currentOffset = data.currentOffset;
            targetOffset = data.targetOffset;
            // Atualizar posição da lanterna imediatamente
            UpdateFlashlightPosition();
            UpdateFlashlightRotation();
        }
    }

    void Start()
    {
        // Verificação de referências
        if (flashlightTransform == null)
        {
            Debug.LogError("FlashlightTransform não está configurado no FlashlightController!");
        }
        
        if (playerController == null)
        {
            playerController = GetComponentInParent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController não encontrado!");
            }
        }
    }
    
    void Update()
    {
        // Obter valores atuais do PlayerController
        PlayerController.FacingDirection direction = playerController.currentDirection;
        bool isWalking = playerController.isMoving;
        int frameIndex = playerController.currentFrame;
        
        // Encontrar o ponto de ancoragem correspondente
        LightAnchorPoint matchingPoint = lightAnchorPoints.Find(point => 
            point.direction == direction && 
            point.isWalking == isWalking && 
            point.frameIndex == frameIndex);
        
        if (matchingPoint != null)
        {
            targetOffset = matchingPoint.offset;
            
            // Aplicar o offset imediatamente ou suavemente
            if (smoothTransition)
            {
                currentOffset = Vector2.Lerp(currentOffset, targetOffset, Time.deltaTime * transitionSpeed);
            }
            else
            {
                currentOffset = targetOffset;
            }
        }
        else
        {
            // Se não encontrou um ponto específico, tentar encontrar um genérico para a direção
            LightAnchorPoint defaultPoint = lightAnchorPoints.Find(point => 
                point.direction == direction && 
                point.frameIndex == 0);
                
            if (defaultPoint != null)
            {
                targetOffset = defaultPoint.offset;
                
                if (smoothTransition)
                {
                    currentOffset = Vector2.Lerp(currentOffset, targetOffset, Time.deltaTime * transitionSpeed);
                }
                else
                {
                    currentOffset = targetOffset;
                }
            }
            // Se nenhum ponto for encontrado, mantenha o offset atual
        }
        
        // Atualizar posição da lanterna
        UpdateFlashlightPosition();
        
        // Atualizar rotação da lanterna baseado na direção
        UpdateFlashlightRotation();
    }
    
    void UpdateFlashlightPosition()
    {
        // Posiciona a lanterna no offset correto relativo ao personagem
        flashlightTransform.position = (Vector2)playerController.transform.position + currentOffset;
    }
    
    void UpdateFlashlightRotation()
    {
        // Ajusta a rotação da luz baseado na direção que o personagem está olhando
        float rotation = 0f;
        
        switch (playerController.currentDirection)
        {
            case PlayerController.FacingDirection.Down:
                rotation = -180f;
                break;
            case PlayerController.FacingDirection.Up:
                rotation = 0;
                break;
            case PlayerController.FacingDirection.Left:
                rotation = 90f;
                break;
            case PlayerController.FacingDirection.Right:
                rotation = -90f;
                break;
        }
        
        flashlightTransform.rotation = Quaternion.Euler(0, 0, rotation);
    }
    
    // Método para facilitar debug
    void OnDrawGizmos()
    {
        if (playerController != null && Application.isPlaying)
        {
            // Desenhar uma linha da posição do player até a lanterna
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playerController.transform.position, flashlightTransform.position);
            
            // Desenhar um pequeno círculo na posição da lanterna
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(flashlightTransform.position, 0.1f);
        }
    }
}

#if UNITY_EDITOR
[UnityEditor.CustomEditor(typeof(FlashlightController))]
public class FlashlightControllerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        FlashlightController controller = (FlashlightController)target;
        
        UnityEditor.EditorGUILayout.Space();
        UnityEditor.EditorGUILayout.LabelField("Ferramentas de Captura", UnityEditor.EditorStyles.boldLabel);
        
        if (GUILayout.Button("Capturar Posição Atual"))
        {
            // Captura a posição atual da luz relativa ao personagem
            Vector2 playerPos = controller.playerController.transform.position;
            Vector2 lightPos = controller.flashlightTransform.position;
            Vector2 offset = lightPos - playerPos;
            
            // Adiciona ou atualiza o ponto de ancoragem para o frame atual
            PlayerController.FacingDirection direction = controller.playerController.currentDirection;
            bool isWalking = controller.playerController.isMoving;
            int frameIndex = controller.playerController.currentFrame;
            
            // Procura por ponto existente para atualizar
            LightAnchorPoint existingPoint = controller.lightAnchorPoints.Find(point => 
                point.direction == direction && 
                point.isWalking == isWalking && 
                point.frameIndex == frameIndex);
                
            if (existingPoint != null)
            {
                existingPoint.offset = offset;
                UnityEditor.EditorUtility.SetDirty(controller);
                Debug.Log($"Ponto atualizado: Dir={direction}, Andando={isWalking}, Frame={frameIndex}, Offset={offset}");
            }
            else
            {
                // Cria novo ponto
                LightAnchorPoint newPoint = new LightAnchorPoint
                {
                    direction = direction,
                    isWalking = isWalking,
                    frameIndex = frameIndex,
                    offset = offset
                };
                controller.lightAnchorPoints.Add(newPoint);
                UnityEditor.EditorUtility.SetDirty(controller);
                Debug.Log($"Novo ponto criado: Dir={direction}, Andando={isWalking}, Frame={frameIndex}, Offset={offset}");
            }
        }
        
        // Adiciona um botão para capturar todas as direções no frame atual
        if (GUILayout.Button("Capturar Posição Atual para Todas Direções"))
        {
            Vector2 offset = controller.flashlightTransform.position - controller.playerController.transform.position;
            bool isWalking = controller.playerController.isMoving;
            int frameIndex = controller.playerController.currentFrame;
            
            // Para cada direção
            foreach (PlayerController.FacingDirection dir in System.Enum.GetValues(typeof(PlayerController.FacingDirection)))
            {
                // Ajustar o offset com base na direção
                Vector2 adjustedOffset = RotateOffsetForDirection(offset, controller.playerController.currentDirection, dir);
                
                // Procura por ponto existente
                LightAnchorPoint existingPoint = controller.lightAnchorPoints.Find(point => 
                    point.direction == dir && 
                    point.isWalking == isWalking && 
                    point.frameIndex == frameIndex);
                    
                if (existingPoint != null)
                {
                    existingPoint.offset = adjustedOffset;
                }
                else
                {
                    LightAnchorPoint newPoint = new LightAnchorPoint
                    {
                        direction = dir,
                        isWalking = isWalking,
                        frameIndex = frameIndex,
                        offset = adjustedOffset
                    };
                    controller.lightAnchorPoints.Add(newPoint);
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(controller);
            Debug.Log($"Pontos capturados para todas as direções no frame {frameIndex}.");
        }
    }
    
    // Método para rotacionar o offset baseado na mudança de direção
    private Vector2 RotateOffsetForDirection(Vector2 offset, PlayerController.FacingDirection fromDir, PlayerController.FacingDirection toDir)
    {
        if (fromDir == toDir) return offset;
        
        // Transformar offset para rotação baseada em mudança de direção
        // Este é um método simples que funciona para sprites com vista superior típica
        
        if ((fromDir == PlayerController.FacingDirection.Down && toDir == PlayerController.FacingDirection.Up) ||
            (fromDir == PlayerController.FacingDirection.Up && toDir == PlayerController.FacingDirection.Down) ||
            (fromDir == PlayerController.FacingDirection.Left && toDir == PlayerController.FacingDirection.Right) ||
            (fromDir == PlayerController.FacingDirection.Right && toDir == PlayerController.FacingDirection.Left))
        {
            // Rotação de 180 graus
            return new Vector2(-offset.x, -offset.y);
        }
        else if ((fromDir == PlayerController.FacingDirection.Down && toDir == PlayerController.FacingDirection.Right) ||
                 (fromDir == PlayerController.FacingDirection.Up && toDir == PlayerController.FacingDirection.Left) ||
                 (fromDir == PlayerController.FacingDirection.Left && toDir == PlayerController.FacingDirection.Down) ||
                 (fromDir == PlayerController.FacingDirection.Right && toDir == PlayerController.FacingDirection.Up))
        {
            // Rotação de 90 graus
            return new Vector2(offset.y, -offset.x);
        }
        else
        {
            // Rotação de -90 graus (270 graus)
            return new Vector2(-offset.y, offset.x);
        }
    }
}
#endif