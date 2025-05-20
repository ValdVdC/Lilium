using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [Header("Botões do Menu")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    
    private Button[] menuButtons;
    private int currentButtonIndex = 0;
    
    void Awake()
    {
        // Armazena todos os botões em um array para facilitar a navegação
        menuButtons = new Button[3];
        menuButtons[0] = continueButton;
        menuButtons[1] = mainMenuButton;
        menuButtons[2] = quitButton;
    }
    
    void OnEnable()
    {
        // Quando o menu é ativado, seleciona o primeiro botão
        currentButtonIndex = 0;
        UpdateButtonsVisual();
    }
    
    void Update()
    {
        // Só processa entradas quando o menu está ativo
        if (!gameObject.activeSelf) return;
        
        // Navegação para cima (W)
        if (Input.GetKeyDown(KeyCode.W))
        {
            currentButtonIndex--;
            if (currentButtonIndex < 0)
                currentButtonIndex = menuButtons.Length - 1;
                
            UpdateButtonsVisual();
        }
        
        // Navegação para baixo (S)
        if (Input.GetKeyDown(KeyCode.S))
        {
            currentButtonIndex++;
            if (currentButtonIndex >= menuButtons.Length)
                currentButtonIndex = 0;
                
            UpdateButtonsVisual();
        }
        
        // Seleção (Enter)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ActivateCurrentButton();
        }
    }
    
    private void UpdateButtonsVisual()
    {
        // Atualiza a aparência de todos os botões
        for (int i = 0; i < menuButtons.Length; i++)
        {
            // Verifica se o botão atual tem TextMeshPro
            TextMeshProUGUI buttonText = menuButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.color = (i == currentButtonIndex) ? selectedColor : normalColor;
            }
            else
            {
                // Caso use o Text padrão do Unity
                Text legacyText = menuButtons[i].GetComponentInChildren<Text>();
                if (legacyText != null)
                {
                    legacyText.color = (i == currentButtonIndex) ? selectedColor : normalColor;
                }
            }
        }
    }
    
    private void ActivateCurrentButton()
    {
        switch (currentButtonIndex)
        {
            case 0: // Continuar
                PauseManager.Instance.Resume();
                break;
                
            case 1: // Menu Principal
                PauseManager.Instance.ReturnToMainMenu();
                break;
                
            case 2: // Sair
                PauseManager.Instance.QuitGame();
                break;
        }
    }
}