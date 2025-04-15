using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DoorController : MonoBehaviour
{
    [Header("Sprites")]
    [SerializeField] private Sprite doorClosedSprite;
    [SerializeField] private Sprite doorOpenSprite;
    
    [Header("Configurações de Interação")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private Vector2 interactionOffset = new Vector2(0, -0.5f); // Offset para baixo
    [SerializeField] private Transform interactionIcon;
    
    [Header("Configurações de Estado da Porta")]
    [SerializeField] private GameObject doorOpenLight;    // Luz para porta aberta
    [SerializeField] private GameObject doorClosedLight;  // Luz para porta fechada
    
    [Header("Configurações de Som")]
    public AudioSource footstepAudioSource;
    [SerializeField] private AudioClip doorOpenSound;     // Som para porta abrindo
    [SerializeField] private AudioClip doorCloseSound;    // Som para porta fechando
    [SerializeField] private float soundVolume = 1.0f;    // Volume dos sons
    
    [Header("Configurações de Collider")]
    [SerializeField] private BoxCollider2D closedDoorCollider;
    [SerializeField] private BoxCollider2D[] openDoorCollider;
    [SerializeField] private EdgeCollider2D openDoorEdgeCollider;
    
    [Header("Configurações do Gizmo")]
    [SerializeField] private Color gizmoColor = Color.yellow;
    [SerializeField] private int gizmoSegments = 32; // Número de segmentos para o desenho do gizmo
    [SerializeField] private bool showPlayerPosition = true; // Mostrar posição do jogador no gizmo
    
    // Referências de componentes
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool isOpen = false;
    private bool playerInRange = false;
    private Light2D doorOpenLightComponent;    // Componente da luz de porta aberta
    private Light2D doorClosedLightComponent;  // Componente da luz de porta fechada
    private AudioSource audioSource;           // Componente para tocar sons
    
    // Para debug
    private bool debugMode = true;
    
    private void Start()
    {
        // Obter referências dos componentes
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Configurar ou adicionar AudioSource para sons da porta
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;  // Som 3D
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 10.0f;
            
            if (debugMode)
                Debug.Log("AudioSource adicionado ao objeto da porta");
        }
        
        // Se os colliders não foram atribuídos, tente encontrá-los automaticamente
        if (closedDoorCollider == null)
        {
            closedDoorCollider = GetComponent<BoxCollider2D>();
            if (debugMode && closedDoorCollider == null)
                Debug.LogWarning("BoxCollider2D para porta fechada não encontrado!");
        }
        
        // Configure o collider de porta aberta (se houver)
        if (openDoorCollider != null && openDoorCollider.Length > 0)
        {
            // Desativa cada collider de porta aberta inicialmente
            foreach (var collider in openDoorCollider)
            {
                if (collider != null)
                    collider.enabled = false;
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("Colliders para porta aberta não configurados!");
        }
        
        // Desativa o EdgeCollider da porta aberta inicialmente
        if (openDoorEdgeCollider != null)
        {
            openDoorEdgeCollider.enabled = false;
        }
        else if (debugMode)
        {
            Debug.LogWarning("EdgeCollider para porta aberta não configurado!");
        }
        
        // Buscar o jogador (usando GameObject.Find se a tag não funcionar)
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            // Tente encontrar por nome se a tag não funcionar
            player = GameObject.Find("Player")?.transform;
            
            if (debugMode && player == null)
                Debug.LogError("Jogador não encontrado! Verifique se existe um objeto com tag 'Player' ou nome 'Player'");
            else if (debugMode)
                Debug.Log("Jogador encontrado pelo nome: " + player.name);
        }
        else if (debugMode)
        {
            Debug.Log("Jogador encontrado pela tag: " + player.name);
        }
        
        // Configurar a luz de porta aberta
        if (doorOpenLight != null)
        {
            doorOpenLightComponent = doorOpenLight.GetComponent<Light2D>();
            if (doorOpenLightComponent == null && debugMode)
                Debug.LogWarning("Objeto doorOpenLight não tem componente Light2D!");
            
            // Inicialmente desativada
            doorOpenLight.SetActive(false);
        }
        else if (debugMode)
        {
            Debug.LogWarning("doorOpenLight não está atribuído no Inspector!");
        }
        
        // Configurar a luz de porta fechada
        if (doorClosedLight != null)
        {
            doorClosedLightComponent = doorClosedLight.GetComponent<Light2D>();
            if (doorClosedLightComponent == null && debugMode)
                Debug.LogWarning("Objeto doorClosedLight não tem componente Light2D!");
            
            // Inicialmente desativada (só aparece quando o jogador está na área de interação)
            doorClosedLight.SetActive(false);
        }
        else if (debugMode)
        {
            Debug.LogWarning("doorClosedLight não está atribuído no Inspector!");
        }
        
        // Esconder o ícone de interação inicialmente
        if (interactionIcon != null)
        {
            interactionIcon.gameObject.SetActive(false);
        }
        else if (debugMode)
        {
            Debug.LogWarning("interactionIcon não está atribuído no Inspector!");
        }
        
        // Verificar se temos os sprites necessários
        if (doorClosedSprite == null)
        {
            doorClosedSprite = spriteRenderer.sprite;
            if (debugMode)
                Debug.Log("Usando sprite atual como doorClosedSprite");
        }
        
        if (doorOpenSprite == null && debugMode)
        {
            Debug.LogWarning("Sprite de porta aberta não configurado!");
        }
        
        // Verificar se temos os sons necessários
        if (doorOpenSound == null && debugMode)
        {
            Debug.LogWarning("Som de porta abrindo não configurado!");
        }
        
        if (doorCloseSound == null && debugMode)
        {
            Debug.LogWarning("Som de porta fechando não configurado!");
        }
    }
    
    private void Update()
    {
        // Verificar a distância do jogador com offset
        if (player != null)
        {
            // Calcular o centro de interação (posição da porta + offset)
            Vector2 interactionCenter = (Vector2)transform.position + interactionOffset;
            
            // Calcular a distância entre o jogador e o centro de interação
            float distanceToPlayer = Vector2.Distance(interactionCenter, player.position);
            
            bool wasInRange = playerInRange;
            playerInRange = distanceToPlayer <= interactionDistance;
            
            // Só exibe log quando há mudança de estado
            if (debugMode && wasInRange != playerInRange)
            {
                Debug.Log($"Estado de proximidade alterado: {(playerInRange ? "Dentro" : "Fora")} do alcance. " +
                         $"Distância: {distanceToPlayer}, Limite: {interactionDistance}");
            }
            
            // Atualizar visibilidade dos indicadores baseado na proximidade
            UpdateLightIndicators(playerInRange);
            
            // Mostrar ou esconder o ícone de interação
            if (interactionIcon != null)
            {
                interactionIcon.gameObject.SetActive(playerInRange);
            }
            
            // Verificar entrada do jogador se estiver no alcance
            if (playerInRange && Input.GetKeyDown(interactionKey))
            {
                if (isOpen)
                {
                    if (debugMode)
                        Debug.Log("Tecla " + interactionKey + " pressionada! Fechando a porta.");
                    
                    CloseDoor();
                }
                else
                {
                    if (debugMode)
                        Debug.Log("Tecla " + interactionKey + " pressionada! Abrindo a porta.");
                    
                    OpenDoor();
                }
            }
        }
    }
    
    // Método para atualizar a visibilidade das luzes indicadoras
    private void UpdateLightIndicators(bool playerNearby)
    {
        // Se o jogador não está próximo, ambas as luzes ficam invisíveis
        if (!playerNearby)
        {
            if (doorOpenLight != null)
                doorOpenLight.SetActive(false);
                
            if (doorClosedLight != null)
                doorClosedLight.SetActive(false);
                
            return;
        }
        
        // Se o jogador está próximo, mostra a luz apropriada para o estado atual da porta
        if (isOpen)
        {
            // Porta aberta: mostrar luz de aberta, esconder luz de fechada
            if (doorOpenLight != null)
                doorOpenLight.SetActive(true);
                
            if (doorClosedLight != null)
                doorClosedLight.SetActive(false);
        }
        else
        {
            // Porta fechada: mostrar luz de fechada, esconder luz de aberta
            if (doorOpenLight != null)
                doorOpenLight.SetActive(false);
                
            if (doorClosedLight != null)
                doorClosedLight.SetActive(true);
        }
    }
    
    private void OpenDoor()
    {
        // Trocar o sprite
        if (doorOpenSprite != null)
        {
            spriteRenderer.sprite = doorOpenSprite;
            if (debugMode)
                Debug.Log("Sprite da porta alterado para o aberto");
        }
        
        // Desativar collider da porta fechada
        if (closedDoorCollider != null)
        {
            closedDoorCollider.enabled = false;
            if (debugMode)
                Debug.Log("Collider da porta fechada desativado");
        }
        
        // Ativar colliders da porta aberta
        if (openDoorCollider != null)
        {
            foreach (var collider in openDoorCollider)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                    if (debugMode)
                        Debug.Log("BoxCollider da porta aberta ativado");
                }
            }
        }
        
        if (openDoorEdgeCollider != null)
        {
            openDoorEdgeCollider.enabled = true;
            if (debugMode)
                Debug.Log("EdgeCollider da porta aberta ativado");
        }
        
        // Atualizar as luzes indicadoras se o jogador estiver próximo
        if (playerInRange)
        {
            if (doorOpenLight != null)
                doorOpenLight.SetActive(true);
                
            if (doorClosedLight != null)
                doorClosedLight.SetActive(false);
        }
            
        // Tocar som de porta abrindo
        if (audioSource != null && doorOpenSound != null)
        {
            audioSource.clip = doorOpenSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
            
            if (debugMode)
                Debug.Log("Som de porta abrindo reproduzido");
        }
        
        isOpen = true;
    }
    
    public void CloseDoor()
    {
        if (doorClosedSprite != null)
        {
            spriteRenderer.sprite = doorClosedSprite;
            if (debugMode)
                Debug.Log("Sprite da porta alterado para o fechado");
        }
        
        // Ativar collider da porta fechada
        if (closedDoorCollider != null)
        {
            closedDoorCollider.enabled = true;
            if (debugMode)
                Debug.Log("Collider da porta fechada ativado");
        }
        
        // Desativar colliders da porta aberta
        if (openDoorCollider != null)
        {
            foreach (var collider in openDoorCollider)
            {
                if (collider != null)
                {
                    collider.enabled = false;
                    if (debugMode)
                        Debug.Log("BoxCollider da porta aberta desativado");
                }
            }
        }
        
        if (openDoorEdgeCollider != null)
        {
            openDoorEdgeCollider.enabled = false;
            if (debugMode)
                Debug.Log("EdgeCollider da porta aberta desativado");
        }
        
        // Atualizar as luzes indicadoras se o jogador estiver próximo
        if (playerInRange)
        {
            if (doorOpenLight != null)
                doorOpenLight.SetActive(false);
                
            if (doorClosedLight != null)
                doorClosedLight.SetActive(true);
        }
            
        // Tocar som de porta fechando
        if (audioSource != null && doorCloseSound != null)
        {
            audioSource.clip = doorCloseSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
            
            if (debugMode)
                Debug.Log("Som de porta fechando reproduzido");
        }
        
        isOpen = false;
    }
    
    // Método para visualizar a área de interação no editor
    private void OnDrawGizmosSelected()
    {
        // Calcular o centro de interação com offset
        Vector2 interactionCenter = (Vector2)transform.position + interactionOffset;
        
        // Desenhar uma cruz no centro de interação
        Gizmos.color = Color.red;
        Gizmos.DrawLine(interactionCenter + Vector2.left * 0.1f, interactionCenter + Vector2.right * 0.1f);
        Gizmos.DrawLine(interactionCenter + Vector2.down * 0.1f, interactionCenter + Vector2.up * 0.1f);
        
        // Desenhar o círculo de interação
        Gizmos.color = gizmoColor;
        DrawWireCircle(interactionCenter, interactionDistance, gizmoSegments);
        
        // Desenhar uma linha da porta até o centro de interação
        Gizmos.DrawLine(transform.position, interactionCenter);
        
        // Se showPlayerPosition estiver ativado e o jogo estiver rodando, mostrar a posição do jogador
        if (showPlayerPosition && Application.isPlaying && player != null)
        {
            Gizmos.color = playerInRange ? Color.green : Color.red;
            Gizmos.DrawSphere(player.position, 0.1f);
            
            // Desenhar linha do centro de interação até o jogador
            Gizmos.DrawLine(interactionCenter, player.position);
        }
    }
    
    // Método para desenhar um círculo
    private void DrawWireCircle(Vector2 center, float radius, int segments)
    {
        float angle = 0f;
        float angleStep = 2 * Mathf.PI / segments;
        
        Vector2 previousPoint = Vector2.zero;
        
        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(angle) * radius;
            float y = Mathf.Cos(angle) * radius;
            
            Vector2 currentPoint = center + new Vector2(x, y);
            
            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, currentPoint);
            }
            
            previousPoint = currentPoint;
            angle += angleStep;
        }
    }
}