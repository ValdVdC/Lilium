using UnityEngine;

public class EnemyAreaManager : MonoBehaviour
{
    // Esta classe serve apenas para indicar a área onde o inimigo pode operar
    // O objeto que contém este script deve ter um Collider2D configurado como trigger
    
    public Color areaColor = new Color(0.2f, 0.4f, 0.8f, 0.2f);
    
    // ID da área que pode ser usado para identificar quais inimigos podem operar nela
    public string areaID = "default";
    
    void OnDrawGizmos()
    {
        // Visualiza a área de operação no editor
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Gizmos.color = areaColor;
            
            // Se for um BoxCollider2D, desenha um cubo
            if (collider is BoxCollider2D boxCollider)
            {
                // Para BoxCollider2D, obter dimensões e offset
                Vector3 size = new Vector3(
                    boxCollider.size.x * transform.localScale.x,
                    boxCollider.size.y * transform.localScale.y,
                    0.1f
                );
                
                Vector3 center = transform.TransformPoint(boxCollider.offset);
                Gizmos.DrawCube(center, size);
            }
            // Se for um CircleCollider2D, desenha uma esfera
            else if (collider is CircleCollider2D circleCollider)
            {
                // Para CircleCollider2D, obter raio e offset
                float radius = circleCollider.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
                Vector3 center = transform.TransformPoint(circleCollider.offset);
                Gizmos.DrawSphere(center, radius);
            }
            // Para outros tipos de collider, desenha um wireframe do GameObject
            else
            {
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
            
            // Adiciona o ID da área como texto para facilitar a identificação
            UnityEditor.Handles.Label(transform.position, "Area: " + areaID);
        }
    }
}