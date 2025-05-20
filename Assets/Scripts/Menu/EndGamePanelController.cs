using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndGamePanelController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text thankYouText;
    [SerializeField] private Text descriptionText;
    [SerializeField] private Text pressEnterText;
    [SerializeField] private Button returnButton;
    [SerializeField] private Image panelBackground;
    [SerializeField] private Image[] decorativeElements; // Para bordas decorativas ou ícones

    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0f;
    [SerializeField] private float contentFadeInDelay = 0f;
    [SerializeField] private float pulsateSpeed = 1.5f;
    [SerializeField] private float pulsateAmount = 0.2f;
    [SerializeField] private bool animatePromptText = true;
    [SerializeField] private float creditsScrollSpeed = 50f; // pixels por segundo
    [SerializeField] private bool showCredits = false;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip buttonSound;
    [SerializeField] private AudioClip panelOpenSound;
    [SerializeField] private AudioClip creditsMusic;
    [SerializeField] private float musicVolume = 0.6f;

    [Header("Credits Settings")]
    [SerializeField] private RectTransform creditsContainer;
    [SerializeField] private float creditsEndPosition = -1000f; // Y position where credits end

    // Variáveis privadas para controle
    private bool isActive = false;
    private float originalFontSize;
    private bool isScrollingCredits = false;
    private Vector3 creditsStartPosition;
    private bool debugMode = true;
    private CanvasGroup panelCanvasGroup;
    private List<CanvasGroup> contentCanvasGroups = new List<CanvasGroup>();
    private MenuManager menuManager;

    private void Awake()
    {
        menuManager = FindFirstObjectByType<MenuManager>();
        // Garantir que temos um CanvasGroup para o painel principal
        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Inicializar o painel como invisível
        panelCanvasGroup.alpha = 0;
        
        // Adicionar CanvasGroups para os elementos de conteúdo se não tiverem
        AddCanvasGroupIfNeeded(thankYouText?.gameObject);
        AddCanvasGroupIfNeeded(descriptionText?.gameObject);
        AddCanvasGroupIfNeeded(pressEnterText?.gameObject);
        AddCanvasGroupIfNeeded(returnButton?.gameObject);
        
        foreach (var element in decorativeElements)
        {
            if (element != null)
                AddCanvasGroupIfNeeded(element.gameObject);
        }
        
        // Guardar posição inicial dos créditos
        if (creditsContainer != null)
        {
            creditsStartPosition = creditsContainer.localPosition;
            AddCanvasGroupIfNeeded(creditsContainer.gameObject);
        }
        
        // Garantir que o painel está ativo para que possamos configurá-lo, mas invisível
        gameObject.SetActive(true);
        
        // Configurar todos os elementos como invisíveis inicialmente
        HideAllContent();
    }

    private void AddCanvasGroupIfNeeded(GameObject obj)
    {
        if (obj == null) return;
        
        CanvasGroup group = obj.GetComponent<CanvasGroup>();
        if (group == null)
            group = obj.AddComponent<CanvasGroup>();
        
        group.alpha = 0;
        contentCanvasGroups.Add(group);
    }

    private void HideAllContent()
    {
        // Definir todos os elementos como invisíveis
        foreach (var group in contentCanvasGroups)
        {
            group.alpha = 0;
        }
    }

    public void Show()
    {
        isActive = true;
        
        // Reiniciar os créditos se necessário
        if (creditsContainer != null)
            creditsContainer.localPosition = creditsStartPosition;
        
        // Iniciar a animação de fade in
        StartCoroutine(FadeInPanel());
        
        // Guardar o tamanho original da fonte para a animação
        if (pressEnterText != null)
        {
            originalFontSize = pressEnterText.fontSize;
        }
        
        // Configurar listener para o botão
        if (returnButton != null)
        {
            returnButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        // Iniciar música dos créditos se habilitado
        if (showCredits && audioSource != null && creditsMusic != null)
        {
            audioSource.clip = creditsMusic;
            audioSource.volume = musicVolume;
            audioSource.loop = true;
            audioSource.Play();
        }
        // Caso contrário, tocar som de abertura do painel
        else if (audioSource != null && panelOpenSound != null)
        {
            audioSource.clip = panelOpenSound;
            audioSource.volume = 1.0f;
            audioSource.loop = false;
            audioSource.Play();
        }
        
        if (debugMode)
            Debug.Log("Painel de fim de jogo inicializado");
    }

    private IEnumerator FadeInPanel()
    {
        float timeElapsed = 0;
        
        // Fade-in do painel de fundo
        while (timeElapsed < fadeInDuration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(0, 1, timeElapsed / fadeInDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        panelCanvasGroup.alpha = 1;
        
        // Esperar um pouco antes de mostrar o conteúdo
        yield return new WaitForSeconds(contentFadeInDelay);
        
        // Fade-in para cada elemento do conteúdo com pequeno intervalo entre eles
        foreach (var group in contentCanvasGroups)
        {
            StartCoroutine(FadeInElement(group));
            yield return new WaitForSeconds(0.1f); // pequeno atraso entre cada elemento
        }
        
        // Iniciar rolagem dos créditos se habilitado
        if (showCredits && creditsContainer != null)
        {
            yield return new WaitForSeconds(1.0f);
            isScrollingCredits = true;
        }
    }

    private IEnumerator FadeInElement(CanvasGroup group)
    {
        float timeElapsed = 0;
        float fadeDuration = 0.5f;
        
        while (timeElapsed < fadeDuration)
        {
            group.alpha = Mathf.Lerp(0, 1, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        group.alpha = 1;
    }

    private void Update()
    {
        if (isActive)
        {
            // Verificar se o jogador pressionou Enter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (debugMode)
                    Debug.Log("Tecla Enter pressionada, retornando ao menu principal");
                
                ReturnToMainMenu();
            }
            
            // Animar o texto "Pressione Enter"
            if (animatePromptText && pressEnterText != null)
            {
                AnimatePressEnterText();
            }
            
            // Rolar créditos se habilitado
            if (isScrollingCredits && creditsContainer != null)
            {
                ScrollCredits();
            }
        }
    }

    private void AnimatePressEnterText()
    {
        // Efeito de pulsação simples no texto
        float pulse = 1.0f + Mathf.Sin(Time.time * pulsateSpeed) * pulsateAmount;
        pressEnterText.fontSize = Mathf.RoundToInt(originalFontSize * pulse);
        
        // Também podemos fazer o texto piscar alterando a cor ou o alpha
        Color c = pressEnterText.color;
        c.a = 0.7f + Mathf.PingPong(Time.time * 0.5f, 0.3f);
        pressEnterText.color = c;
    }

    private void ScrollCredits()
    {
        // Mover os créditos para cima
        creditsContainer.localPosition += Vector3.up * (creditsScrollSpeed * Time.deltaTime);
        
        // Verificar se os créditos chegaram ao fim
        if (creditsContainer.localPosition.y > creditsEndPosition)
        {
            // Opção 1: Reiniciar créditos (loop)
            // creditsContainer.localPosition = creditsStartPosition;
            
            // Opção 2: Parar os créditos
            isScrollingCredits = false;
        }
    }

    public void ReturnToMainMenu()
    {
        menuManager.mainMenuCanvas.SetActive(true);
    }



    private void OnDisable()
    {
        isActive = false;
        isScrollingCredits = false;
        
        // Remover listener do botão
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnToMainMenu);
        }
        
        // Parar qualquer áudio
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
}