using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class SavePointController : MonoBehaviour
{
    [Header("Sprites de Animação")]
    [SerializeField] private Sprite[] savePointSprites;
    [SerializeField] private float animationSpeed = 0.2f;
    
    [Header("Configurações de Interação")]
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 1.5f;
    [SerializeField] private Vector2 interactionOffset = new Vector2(0, 0);
    [SerializeField] private Transform interactionIcon;
    
    [Header("Configurações de Efeito Visual")]
    [SerializeField] private GameObject outlineLight;
    [SerializeField] private Color activeLightColor = Color.green;
    [SerializeField] private float lightIntensity = 1.0f;
    [SerializeField] private float lightOuterRadius = 0.5f;
    [SerializeField] private float pulseSpeed = 1.0f;
    [SerializeField] private float pulseAmount = 0.2f;
    
    [Header("Configurações de Som")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip saveStartSound;
    [SerializeField] private AudioClip saveCompleteSound;
    [SerializeField] private float soundVolume = 1.0f;
    
    [Header("Configurações do Gizmo")]
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private bool showPlayerPosition = true;
    
    [Header("Configurações do Menu de Save")]
    [SerializeField] private SaveMenuUI saveMenuUI;

    [Header("Configurações de Save Point")]
    [SerializeField] private string savePointDisplayName = "";

    // Referências de componentes
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private PlayerController playerController;
    private bool playerInRange = false;
    private Light2D outlineLightComponent;
    private int currentSpriteIndex = 0;
    private float animationTimer = 0f;
    private bool isSaving = false;
    
    // Controle de estado do menu
    private bool menuIsOpen = false;
    private float interactionCooldown = 0.25f;
    private float lastInteractionTime = 0f;
    
    // Para debug
    private bool debugMode = true;
    
    private void Start()
    {
        // Obter referências dos componentes
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Verificar se temos sprites para animar
        if (savePointSprites == null || savePointSprites.Length == 0)
        {
            if (debugMode)
                Debug.LogWarning("Nenhum sprite de animação configurado para o SavePoint!");
                
            // Se não tivermos sprites de animação, use o sprite atual
            savePointSprites = new Sprite[1] { spriteRenderer.sprite };
        }
        
        // Configurar som
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;  // Som 3D
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 10.0f;
            
            if (debugMode)
                Debug.Log("AudioSource adicionado ao objeto do SavePoint");
        }
        
        // Buscar o jogador
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            player = GameObject.Find("Player")?.transform;
            
            if (debugMode && player == null)
                Debug.LogError("Jogador não encontrado! Verifique se existe um objeto com tag 'Player' ou nome 'Player'");
        }
        
        // Configurar a luz de contorno
        SetupOutlineLight();
        
        // Esconder o ícone de interação inicialmente
        if (interactionIcon != null)
        {
            interactionIcon.gameObject.SetActive(false);
        }
        else if (debugMode)
        {
            Debug.LogWarning("interactionIcon não está atribuído no Inspector!");
        }
        
        // Verificar se temos referência para o menu
        if (saveMenuUI == null)
        {
            // Tentar encontrar o SaveMenuUI na cena
            saveMenuUI = FindFirstObjectByType<SaveMenuUI>();
            
            if (saveMenuUI == null && debugMode)
            {
                Debug.LogError("SaveMenuUI não encontrado na cena! Configure-o no inspector.");
            }
        }
    }
    
    private void SetupOutlineLight()
    {
        if (outlineLight == null)
        {
            // Criar luz de contorno se não existir
            outlineLight = new GameObject("OutlineLight");
            outlineLight.transform.SetParent(transform);
            outlineLight.transform.localPosition = Vector3.zero;
            
            outlineLightComponent = outlineLight.AddComponent<Light2D>();
            outlineLightComponent.lightType = Light2D.LightType.Sprite;
            
            // Criar um sprite renderer para a luz se basear
            SpriteRenderer lightSprite = outlineLight.AddComponent<SpriteRenderer>();
            lightSprite.sprite = spriteRenderer.sprite;
            lightSprite.enabled = false;  // Não renderize este sprite, apenas use-o para a luz
            
            outlineLightComponent.lightCookieSprite = lightSprite.sprite;
            outlineLightComponent.color = activeLightColor;
            outlineLightComponent.intensity = 0;  // Inicialmente desligado
            outlineLightComponent.pointLightOuterRadius = lightOuterRadius;
            outlineLightComponent.pointLightInnerRadius = lightOuterRadius * 0.8f;
            outlineLightComponent.falloffIntensity = 0.8f;
            outlineLightComponent.shapeLightFalloffSize = 0.5f;
            
            if (debugMode)
                Debug.Log("Luz de contorno criada para o SavePoint");
        }
        else
        {
            // Usar a luz existente
            outlineLightComponent = outlineLight.GetComponent<Light2D>();
            if (outlineLightComponent == null)
            {
                outlineLightComponent = outlineLight.AddComponent<Light2D>();
                if (debugMode)
                    Debug.Log("Componente Light2D adicionado ao outlineLight existente");
            }
            
            // Inicialmente desligado
            outlineLightComponent.intensity = 0;
        }
    }
    
    private void Update()
    {
        // Sempre animar o save point
        AnimateSavePoint();
        
        // Verificar a distância do jogador
        if (player != null)
        {
            // Verificar estado do menu ANTES de qualquer interação
            if (menuIsOpen && saveMenuUI != null && !saveMenuUI.gameObject.activeSelf)
            {
                if (debugMode)
                    Debug.Log("Fechando menu de salvamento - detectado fechamento");
                    
                menuIsOpen = false;
            }
            
            // Calcular o centro de interação
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
            
            // Atualizar visibilidade dos indicadores baseado na proximidade e no estado do menu
            UpdateVisualIndicators(playerInRange && !menuIsOpen);
            
            // Verificar entrada do jogador e respeitar o cooldown
            if (Input.GetKeyDown(interactionKey) && Time.time - lastInteractionTime > interactionCooldown)
            {
                lastInteractionTime = Time.time;
                HandleInteraction();
            }
        }
        
        // Animar a intensidade da luz se o jogador estiver próximo e o menu não estiver aberto
        if (playerInRange && !menuIsOpen && outlineLightComponent != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            outlineLightComponent.intensity = lightIntensity + pulse;
            
            // Sempre atualizar o sprite da luz para corresponder ao sprite atual
            UpdateLightSprite();
        }
        
        // Manter controle do estado do menu (se foi fechado por outros meios)
        if (saveMenuUI != null && !saveMenuUI.gameObject.activeSelf)
        {
            menuIsOpen = false;
        }
    }
    
    // Processa a interação com o ponto de salvamento com base no contexto atual
    private void HandleInteraction()
    {
        if (saveMenuUI == null) return;
        
        // Se o menu não estiver aberto e o jogador estiver no alcance, abrir o menu
        if (!menuIsOpen && playerInRange && !isSaving)
        {
            StartSaving();
            menuIsOpen = true;
            return;
        }
        
        // Se o menu estiver aberto, verificar os painéis internos
        if (menuIsOpen)
        {
            // Se o painel de confirmação estiver aberto, não fazer nada (a lógica é tratada pelo SaveMenuUI)
            if (saveMenuUI.isConfirmationOpen)
            {
                return;
            }
            
            // Se as opções de slot estiverem abertas, não fazer nada (a lógica é tratada pelo SaveMenuUI)
            if (saveMenuUI.isSlotOptionsOpen)
            {
                return;
            }
            
            // Se estiver apenas no menu principal, fechar
            CloseSaveMenu();
            menuIsOpen = false;
        }
    }
    
    // Fechamento explícito do menu de salvamento
    private void CloseSaveMenu()
    {
        if (saveMenuUI != null)
        {
            if (debugMode)
                Debug.Log("Fechando menu de salvamento");
                
            saveMenuUI.CloseSaveMenu();
            menuIsOpen = false;
        }
    }

    public void OnSaveMenuClosed()
    {
        if (debugMode)
            Debug.Log("Fechando menu de salvamento - callback recebido");
            
        menuIsOpen = false;
    }

    private void AnimateSavePoint()
    {
        if (savePointSprites.Length <= 1)
            return;
            
        // Avançar o temporizador de animação
        animationTimer += Time.deltaTime;
        
        // Mudar o sprite quando o tempo de animação for atingido
        if (animationTimer >= animationSpeed)
        {
            animationTimer = 0f;
            currentSpriteIndex = (currentSpriteIndex + 1) % savePointSprites.Length;
            spriteRenderer.sprite = savePointSprites[currentSpriteIndex];
            
            // Também atualiza o sprite da luz
            UpdateLightSprite();
        }
    }
    
    // Atualiza o sprite da luz para corresponder ao sprite atual
    private void UpdateLightSprite()
    {
        if (outlineLightComponent != null && outlineLightComponent.lightType == Light2D.LightType.Sprite)
        {
            // Precisamos atualizar o sprite da luz para corresponder ao sprite atual
            SpriteRenderer lightSprite = outlineLightComponent.GetComponent<SpriteRenderer>();
            if (lightSprite != null)
            {
                lightSprite.sprite = spriteRenderer.sprite;
                outlineLightComponent.lightCookieSprite = lightSprite.sprite;
            }
        }
    }
    
    // Atualiza os indicadores visuais com base na proximidade do jogador
    private void UpdateVisualIndicators(bool showIndicators)
    {
        // Controle da luz de contorno
        if (outlineLightComponent != null)
        {
            outlineLightComponent.intensity = showIndicators ? lightIntensity : 0f;
        }
        
        // Mostrar ou esconder o ícone de interação
        if (interactionIcon != null)
        {
            interactionIcon.gameObject.SetActive(showIndicators);
        }
    }
    
    // Inicia o processo de salvamento
    private void StartSaving()
    {
        if (debugMode)
            Debug.Log($"Abrindo menu de salvamento com nome: '{savePointDisplayName}'");
        
        // Verificar se temos referência para o menu
        if (saveMenuUI == null)
        {
            // Tentar encontrar o SaveMenuUI na cena
            saveMenuUI = FindFirstObjectByType<SaveMenuUI>();
            
            if (saveMenuUI == null)
            {
                Debug.LogError("SaveMenuUI não encontrado na cena! Configure-o no inspector.");
                return;
            }
        }
        
        // Tocar som de início de salvamento
        if (audioSource != null && saveStartSound != null)
        {
            audioSource.clip = saveStartSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }
        
        // Exemplo: aumentar a intensidade da luz durante a interação
        if (outlineLightComponent != null)
        {
            outlineLightComponent.intensity = lightIntensity * 2f;
            outlineLightComponent.color = Color.white;
        }
        
        // Verificar se o nome do savePoint está vazio e usar nome do objeto como fallback
        string displayName = string.IsNullOrEmpty(savePointDisplayName) ? gameObject.name : savePointDisplayName;
        Debug.Log($"Nome final do savePoint para o menu: '{displayName}'");
        
        // Abrir o menu de salvamento
        saveMenuUI.OpenSaveMenu(displayName);
        menuIsOpen = true;
    }
    
    // Coroutine para simular o tempo de salvamento e executar o salvamento
    private IEnumerator SaveGameCoroutine()
    {
        // Aguardar um pequeno tempo para simular o salvamento
        yield return new WaitForSeconds(1.5f);
        
        // Executar o salvamento
        SaveGame();
        
        // Tocar som de conclusão do salvamento
        if (audioSource != null && saveCompleteSound != null)
        {
            audioSource.clip = saveCompleteSound;
            audioSource.volume = soundVolume;
            audioSource.Play();
        }
        
        // Restaurar a luz para o estado normal
        if (outlineLightComponent != null)
        {
            outlineLightComponent.intensity = lightIntensity;
            outlineLightComponent.color = activeLightColor;
        }
        
        // Aguardar um tempo antes de permitir salvar novamente
        yield return new WaitForSeconds(0.5f);
        
        isSaving = false;
    }
    
    // Função para salvar o jogo (implementação básica)
    private void SaveGame()
    {
        if (debugMode)
            Debug.Log("Salvando o jogo...");
            
        // Verificar se o GameSaveSystem existe
        GameSaveSystem saveSystem = GameSaveSystem.Instance;
        
        if (saveSystem != null)
        {
            // Salvar o jogo usando o GameSaveSystem
            saveSystem.SaveGame(gameObject.name);
            
            if (debugMode)
                Debug.Log("Jogo salvo com sucesso!");
        }
        else
        {
            Debug.LogError("GameSaveSystem não encontrado! Certifique-se de adicioná-lo à cena.");
        }
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
        DrawWireCircle(interactionCenter, interactionDistance, 32);
        
        // Desenhar uma linha do save point até o centro de interação
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