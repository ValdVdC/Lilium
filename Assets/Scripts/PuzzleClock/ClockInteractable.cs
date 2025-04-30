using UnityEngine;

public class ClockInteractable : MonoBehaviour, IInteractable
{
    [Header("Referências")]
    public ClockPuzzleManager clockPuzzleManager;  // Referência ao gerenciador do puzzle
    
    [Header("Configurações")]
    public bool requiresBookToBeRead = true;       // Indica se o livro precisa ser lido antes
    
    public void Interact()
    {
        if (clockPuzzleManager == null)
        {
            Debug.LogError("ClockPuzzleManager não encontrado!");
            return;
        }
        
        // Verificar se o puzzle já foi resolvido
        if (clockPuzzleManager.puzzleSolved)
        {
            Debug.Log("O puzzle do relógio já foi resolvido!");
            return;
        }
        
        // Verificar se o livro foi lido, se necessário
        if (requiresBookToBeRead && !clockPuzzleManager.bookHasBeenRead)
        {
            Debug.Log("O jogador precisa ler o livro primeiro!");
            // Você poderia adicionar um feedback para o jogador aqui
            return;
        }
        
        // Iniciar interação com o relógio
        clockPuzzleManager.StartClockInteraction();
    }
}