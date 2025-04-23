using UnityEngine;

public class DepthSorter : MonoBehaviour
{
    private SpriteRenderer mainSpriteRenderer;
    private SpriteRenderer[] childSpriteRenderers;
    
    [SerializeField] private int sortingOrderBase = 0; // Valor base alto para garantir números positivos
    [SerializeField] private bool useAutoOffset = true; // Ativar cálculo automático de offset
    [SerializeField] private float manualOffset = 0f; // Offset manual caso não queira usar o automático
    [SerializeField] private bool useBottomAsReference = false; // Usar a base do sprite como referência
    [SerializeField] private bool applyToChildren = true; // Aplicar aos objetos filhos
    [SerializeField] private bool runOnlyOnce = false; // Se deve executar apenas uma vez

    // Offset para objetos filhos em relação ao principal
    [SerializeField] private int childrenOrderOffset = 1; // Offset de ordem para filhos (positivo = na frente do pai)
    
    private float autoOffset = 0f;

    private void Awake()
    {
        mainSpriteRenderer = GetComponent<SpriteRenderer>();
        
        // Coleta todos os SpriteRenderers filhos
        if (applyToChildren)
        {
            childSpriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        }
        
        CalculateAutoOffset();
    }

    private void CalculateAutoOffset()
    {
        if (mainSpriteRenderer == null || !useAutoOffset) return;

        // Obtém os limites do sprite principal
        Bounds bounds = mainSpriteRenderer.bounds;
        
        if (useBottomAsReference)
        {
            // Calcula a distância entre o pivot e a parte inferior do sprite
            float pivotToBottom = (bounds.center.y - bounds.extents.y) - transform.position.y;
            autoOffset = pivotToBottom * 100f;
        }
        else
        {
            // Usa metade da altura do sprite como offset padrão
            autoOffset = bounds.size.y * 50f;
        }
    }

    private void LateUpdate()
    {
        if (mainSpriteRenderer == null) return;

        float offsetToUse = useAutoOffset ? autoOffset : manualOffset;
        
        // Calcula o sorting order base para este objeto
        int calculatedOrder = (int)(sortingOrderBase - transform.position.y * 100 + offsetToUse);
        
        // Aplica ao sprite principal
        mainSpriteRenderer.sortingOrder = calculatedOrder;
        
        // Aplica a todos os sprites filhos com o offset adicional
        if (applyToChildren && childSpriteRenderers != null)
        {
            foreach (SpriteRenderer childRenderer in childSpriteRenderers)
            {
                // Pula o sprite principal que já foi configurado
                if (childRenderer == mainSpriteRenderer) continue;
                
                // Aplica o mesmo order mais o offset para filhos
                childRenderer.sortingOrder = calculatedOrder + childrenOrderOffset;
            }
        }

        if (runOnlyOnce)
        {
            Destroy(this);
        }
    }

    // Método público para recalcular o offset se necessário
    public void RecalculateOffset()
    {
        CalculateAutoOffset();
    }

    // Método para debug - Mostra visualmente o ponto de referência usado
    private void OnDrawGizmosSelected()
    {
        if (mainSpriteRenderer == null) return;
        
        Bounds bounds = mainSpriteRenderer.bounds;
        if (useBottomAsReference)
        {
            // Desenha um ponto na base do sprite
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(new Vector3(bounds.center.x, bounds.center.y - bounds.extents.y, bounds.center.z), 0.1f);
        }
    }
}