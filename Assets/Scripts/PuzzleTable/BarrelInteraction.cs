using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using System;

public class BarrelInteraction : MonoBehaviour, IInteractable , ISaveable
{
    [Serializable]
    public class SaveData
    {
        public bool isOpen;
        public bool isItemCollected;
    }

    public object GetSaveData()
    {
        SaveData data = new SaveData
        {
            isOpen = this.isOpen,
            isItemCollected = this.IsItemCollected
        };
        return data;
    }

    public void LoadFromSaveData(object saveData)
    {
        if (saveData is SaveData data)
        {
            this.isOpen = data.isOpen;
            if (this.isOpen)
            {
                OpenBarrel();
            }
            
            // Se o item já foi coletado, desativá-lo
            if (data.isItemCollected && puzzleItem != null)
            {
                puzzleItem.SetActive(false);
                puzzleManager.MarkItemCollected(gameObject.name);
            }
        }       
    }

    [Header("References")]
    public Transform barrelCameraPosition;
    public float barrelCameraSize = 2.0f;
    public GameObject barrelOpenView;
    public GameObject puzzleItem;
    public PuzzleItemType itemType;
    public PuzzleManager puzzleManager;
    public PuzzleAudioManager audioManager;
    public CameraController cameraController;
    
    [Header("Interface")]
    public GameObject barrelUI;
    public Button closeButton;
    public GameObject clickIndicator;
    public RectTransform itemHighlight;
    
    [Header("Interaction Icon")]
    public GameObject interactionKeyIconPrefab;
    public float iconYOffset = 1.5f;

    public bool isOpen = false;
    public bool IsItemCollected => puzzleManager.IsItemCollected(gameObject.name);
    private PlayerController playerController;
    private GameObject interactionIcon;
    private bool isHoveringOverItem = false;

    [SerializeField] private CursorController cursorController;

    private void Start()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        barrelOpenView.SetActive(false);
        
        if (audioManager == null)
            audioManager = FindFirstObjectByType<PuzzleAudioManager>();
        if (puzzleManager == null)
            puzzleManager = FindFirstObjectByType<PuzzleManager>();
        if (cameraController == null)
            cameraController = FindFirstObjectByType<CameraController>();
            
        // Criar o ícone de interação
        CreateInteractionIcon();
        
        // Configuração inicial da UI
        if (barrelUI != null)
            barrelUI.SetActive(false);
            
        // Adicionar listener ao botão de fechar
        if (closeButton != null)
            closeButton.onClick.AddListener(CloseBarrel);
            
        // Configuração inicial do indicador de clique
        if (clickIndicator != null)
            clickIndicator.SetActive(false);
            
        // Configuração inicial do highlight do item
        if (itemHighlight != null)
            itemHighlight.gameObject.SetActive(false);
    }
    
    private void CreateInteractionIcon()
    {
        if (interactionKeyIconPrefab != null)
        {
            // Criar o ícone como instância independente primeiro
            interactionIcon = Instantiate(interactionKeyIconPrefab);
            
            // Guardar a escala original do prefab
            Vector3 originalScale = interactionIcon.transform.localScale;

            // Guardar a posição original do prefab
            Vector3 originalPosition = interactionIcon.transform.localPosition;
            
            // Tornar o ícone filho do barril para que se mova junto
            interactionIcon.transform.SetParent(transform, false); // false é importante - mantém a escala mundial
            interactionIcon.transform.localPosition = Vector3.up * iconYOffset;
            
            // Garantir que a escala permaneça a mesma do prefab
            interactionIcon.transform.localScale = originalScale;
            
            // Garantir que a posição permaneça a mesma do prefab
            interactionIcon.transform.localPosition = originalPosition;

            // Desativar inicialmente
            interactionIcon.SetActive(false);
        }
    }
    
    // Este método será chamado pelo InteractorController quando o barril estiver próximo
    public void ShowInteractionIcon()
    {
        if (interactionIcon != null && !isOpen)
            interactionIcon.SetActive(true);
    }
    
    // Este método será chamado quando o jogador se afastar
    public void HideInteractionIcon()
    {
        if (interactionIcon != null)
            interactionIcon.SetActive(false);
    }

    public void Interact()
    {
        if (!isOpen)
        {
            OpenBarrel();
        }
        else
        {
            CloseBarrel();
        }
    }

    private void OpenBarrel()
    {
        isOpen = true;

        // Ocultar o ícone de interação quando abrir o barril
        if (interactionIcon != null)
            interactionIcon.SetActive(false);

        cursorController.ShowCursor();
        cursorController.SetInteractiveCursor();

        // Disable player movement and interaction
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
            playerController.spriteRenderer.enabled = false;
            
            // Desativar também o InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
                interactor.enabled = false;
                
            // Se houver componentes de luz/sombra, desative-os
            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(false);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(false);
            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) shadowCaster.enabled = false;
        }

        // Usar o CameraController para ativar a câmera do barril
        if (cameraController != null)
        {
            cameraController.ActivateBarrelCamera(barrelCameraPosition, barrelCameraSize);
        }
        
        barrelOpenView.SetActive(true);
        
        // Ativar a interface do barril
        if (barrelUI != null)
            barrelUI.SetActive(true);

        // Verificar se o item já foi coletado
        string barrelId = gameObject.name;
        bool alreadyCollected = puzzleManager.IsItemCollected(barrelId);
        
        if (puzzleItem != null)
        {
            puzzleItem.SetActive(!alreadyCollected);
            
            // Se o item não foi coletado, ativar o highlight
            if (itemHighlight != null && !alreadyCollected)
            {
                itemHighlight.gameObject.SetActive(true);
                
                // Posicionar o highlight ao redor do item
                PositionItemHighlight();
                
                // CORREÇÃO: Verificar se o mouse já está sobre o item logo ao abrir o barril
                CheckMouseHoverOnItem();
            }
        }
        
        if (audioManager != null)
            audioManager.PlayBarrelOpen();
    }
    
    // Posiciona o retângulo highlight ao redor do puzzle item
    private void PositionItemHighlight()
    {
        if (itemHighlight == null || puzzleItem == null)
            return;
            
        // Converte a posição do puzzle item para posição na tela
        Camera barrelCamera = cameraController.barrelCamera;
        if (barrelCamera == null)
            return;
            
        // Pegar o renderer do item para obter seus limites
        Renderer itemRenderer = puzzleItem.GetComponent<Renderer>();
        if (itemRenderer == null)
            return;
            
        // Obter os limites do item
        Bounds itemBounds = itemRenderer.bounds;
        
        // Converter as posições dos cantos do item para a tela
        Vector3 minScreenPoint = barrelCamera.WorldToScreenPoint(itemBounds.min);
        Vector3 maxScreenPoint = barrelCamera.WorldToScreenPoint(itemBounds.max);
        
        // Converter para coordenadas de canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            itemHighlight.parent as RectTransform,
            minScreenPoint,
            null, // Se o canvas estiver no modo ScreenSpace-Overlay, use null
            out Vector2 minLocalPoint);
            
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            itemHighlight.parent as RectTransform,
            maxScreenPoint,
            null,
            out Vector2 maxLocalPoint);
            
        // Configurar a posição e tamanho do highlight
        itemHighlight.anchoredPosition = (minLocalPoint + maxLocalPoint) / 2;
        itemHighlight.sizeDelta = new Vector2(
            Mathf.Abs(maxLocalPoint.x - minLocalPoint.x),
            Mathf.Abs(maxLocalPoint.y - minLocalPoint.y)
        );
        
        // Adicionar um pequeno padding ao retângulo
        float padding = 10f;
        itemHighlight.sizeDelta += new Vector2(padding * 2, padding * 2);
    }

    public void CloseBarrel()
    {
        isOpen = false;

        cursorController.HideCursor();

        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
            playerController.spriteRenderer.enabled = true;
            
            // Reativar o InteractorController
            InteractorController interactor = playerController.GetComponent<InteractorController>();
            if (interactor != null)
            {
                interactor.enabled = true;
                
                // Permitir que o InteractorController detecte novamente este barril
                // na próxima atualização de frame
                interactor.ResetCurrentInteractable();
            }

            Transform perception = playerController.transform.Find("Perception");
            if (perception != null) perception.gameObject.SetActive(true);
            
            Transform flashLight = playerController.transform.Find("FlashLight");
            if (flashLight != null) flashLight.gameObject.SetActive(true);

            ShadowCaster2D shadowCaster = GetComponent<ShadowCaster2D>();
            if (shadowCaster != null) shadowCaster.enabled = true;
        }

        // Voltar para a câmera principal
        if (cameraController != null)
        {
            cameraController.ReturnToMainCamera();
        }
        
        barrelOpenView.SetActive(false);
        
        // Desativar a UI do barril
        if (barrelUI != null)
            barrelUI.SetActive(false);
            
        // Desativar o highlight e o indicador de clique
        if (itemHighlight != null)
            itemHighlight.gameObject.SetActive(false);
            
        if (clickIndicator != null)
            clickIndicator.SetActive(false);
            
        if (audioManager != null)
            audioManager.PlayBarrelClose();
            
        // Não mostrar o ícone de interação logo após fechar
        // Ele será mostrado novamente pelo InteractorController
    }

    private void Update()
    {
        if (isOpen)
        {
            // Verificar o hover do mouse sobre o item
            CheckMouseHoverOnItem();
            
            // Verificar clique do mouse para pegar o item
            if (Input.GetMouseButtonDown(0) && isHoveringOverItem)
            {
                // Verificar se clicou no item
                string barrelId = gameObject.name;
                bool alreadyCollected = puzzleManager.IsItemCollected(barrelId);
                
                if (!alreadyCollected && puzzleItem != null && puzzleItem.activeSelf)
                {
                    Debug.Log("Clique no item detectado. Tipo: " + itemType);
                    
                    // Referência ao inventário
                    InventoryManager inventory = FindFirstObjectByType<InventoryManager>();
                    
                    if (inventory == null)
                    {
                        Debug.LogError("InventoryManager não encontrado!");
                        return;
                    }
                    
                    // Tentar adicionar o item ao inventário
                    bool added = inventory.AddItem(itemType);
                    
                    Debug.Log("Tentativa de adicionar ao inventário: " + (added ? "Sucesso" : "Falha"));
                    
                    if (added)
                    {
                        // Disable the puzzle item permanently
                        puzzleItem.SetActive(false);
                        
                        // Also hide the highlight
                        if (itemHighlight != null)
                            itemHighlight.gameObject.SetActive(false);
                        
                        // Store that this barrel's item was collected
                        puzzleManager.MarkItemCollected(barrelId);
                        
                        // Adicionar ao inventário do PuzzleManager também para manter compatibilidade
                        puzzleManager.AddItemToInventory(itemType);
                        
                        // Fechar o barril após coletar o item
                        CloseBarrel();
                    }
                    // Se não foi adicionado, a UI já mostrará o feedback visual
                }
            }
            
            // Verificar se pressionou E novamente para fechar o barril
            if (Input.GetKeyDown(KeyCode.E))
            {
                CloseBarrel();
            }
        }
    }
    
    private void CheckMouseHoverOnItem()
    {
        // Somente verificar se o puzzle item está ativo
        if (puzzleItem == null || !puzzleItem.activeSelf)
        {
            isHoveringOverItem = false;
            
            if (clickIndicator != null)
                clickIndicator.SetActive(false);
                
            return;
        }
        
        // Usar a câmera do barril para o raycasting
        Camera currentCamera = cameraController.barrelCamera;
        if (currentCamera == null)
            return;
            
        // Verificar se o mouse está sobre o item
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        
        // CORREÇÃO: Guardar estado anterior só após atualizar o atual
        bool wasHovering = isHoveringOverItem;
        isHoveringOverItem = (hit.collider != null && hit.collider.gameObject == puzzleItem);
        
        // CORREÇÃO: Sempre atualizar o estado do indicador, não apenas quando houver mudança
        if (clickIndicator != null)
        {
            clickIndicator.SetActive(isHoveringOverItem);
            
            // Posicionar o indicador de clique no cursor do mouse
            if (isHoveringOverItem)
            {
                // Posicionar o indicador no centro do item
                RectTransform clickIndicatorRect = clickIndicator.GetComponent<RectTransform>();
                if (clickIndicatorRect != null && puzzleItem != null)
                {
                    // Obtém a posição mundial do item
                    Vector3 itemWorldPos = puzzleItem.transform.position;
                    
                    // Converte para posição na tela
                    Vector2 screenPos = currentCamera.WorldToScreenPoint(itemWorldPos);
                    
                    // Converte para coordenadas locais do canvas
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        clickIndicatorRect.parent as RectTransform,
                        screenPos,
                        null, // Se o canvas estiver no modo ScreenSpace-Overlay, use null
                        out Vector2 localPoint
                    );
                    
                    // Posiciona o indicador de clique no centro do item
                    clickIndicatorRect.anchoredPosition = localPoint;
                }
            }
        }
    }
}