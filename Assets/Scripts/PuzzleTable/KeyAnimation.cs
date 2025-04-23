using UnityEngine;
using System.Collections;

public class KeyAnimation : MonoBehaviour
{
    [Header("Animation")]
    public SpriteRenderer keyRenderer;
    public Sprite[] keyAnimationSprites; // Sequência de sprites para a animação de aparecer
    public float animationFrameRate = 0.1f; // Tempo entre cada frame
    
    [Header("Floating Effect")]
    public float floatAmplitude = 0.1f;
    public float floatSpeed = 1.0f;
    public float floatHeight = 1.0f;
    
    [Header("Light Effect")]
    public Light keyLight;
    public float lightIntensityMin = 0.5f;
    public float lightIntensityMax = 1.5f;
    public float lightFlickerSpeed = 2.0f;

    private Vector3 startPosition;
    private bool animationComplete = false;
    private bool canBeCollected = false;

    void Start()
    {
        startPosition = transform.position;

        if (keyRenderer == null)
            keyRenderer = GetComponent<SpriteRenderer>();

        if (keyLight == null)
            keyLight = GetComponentInChildren<Light>();

        gameObject.SetActive(false); // Inicialmente desativado
        
        // Garantir que o sprite inicial esteja configurado
        if (keyRenderer != null && keyAnimationSprites.Length > 0)
            keyRenderer.sprite = keyAnimationSprites[0];
    }

    void Update()
    {
        if (animationComplete)
        {
            // Movimento de flutuação após a animação inicial
            float floatOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(
                startPosition.x, 
                startPosition.y + floatHeight + floatOffset, 
                startPosition.z
            );

            // Efeito de luz pulsante
            if (keyLight != null)
            {
                float lightIntensity = Mathf.Lerp(
                    lightIntensityMin, 
                    lightIntensityMax, 
                    (Mathf.Sin(Time.time * lightFlickerSpeed) + 1) / 2
                );
                keyLight.intensity = lightIntensity;
            }
        }
    }

    // Método para iniciar a animação da chave
    public void PlayAppearAnimation()
    {
        gameObject.SetActive(true);
        StartCoroutine(AnimateKeyAppearance());
    }
    
    // Coroutine para animação frame a frame
    private IEnumerator AnimateKeyAppearance()
    {
        canBeCollected = false;
        
        // Reproduzir cada frame da animação
        for (int i = 0; i < keyAnimationSprites.Length; i++)
        {
            keyRenderer.sprite = keyAnimationSprites[i];
            
            // Calcular a posição Y baseada no progresso da animação
            float progress = (float)i / (keyAnimationSprites.Length - 1);
            float yPos = Mathf.Lerp(startPosition.y, startPosition.y + floatHeight, progress);
            
            transform.position = new Vector3(startPosition.x, yPos, startPosition.z);
            
            yield return new WaitForSeconds(animationFrameRate);
        }
        
        animationComplete = true;
        canBeCollected = true;
    }
    
    // Método para verificar se a chave pode ser coletada
    public bool CanBeCollected()
    {
        return canBeCollected;
    }
    
    // Método para resetar a animação quando a chave é ativada
    public void ResetAnimation()
    {
        transform.position = startPosition;
        animationComplete = false;
        canBeCollected = false;
        
        if (keyRenderer != null && keyAnimationSprites.Length > 0)
            keyRenderer.sprite = keyAnimationSprites[0];
    }
}