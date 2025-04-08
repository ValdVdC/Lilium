using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "RandomTorchTile", menuName = "2D/Tiles/Random Torch Tile")]
public class RandomTorchTile : TileBase
{
    public Sprite[] possibleSprites;

    // Sprite atual que será usado na instância
    [HideInInspector] public Sprite currentSprite;

    // Chamado quando o Tilemap precisa saber como desenhar o tile
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.sprite = currentSprite != null
            ? currentSprite
            : possibleSprites != null && possibleSprites.Length > 0
                ? possibleSprites[Random.Range(0, possibleSprites.Length)]
                : null;
    }

    // Método para alterar dinamicamente o sprite da instância
    public void SetSprite(Sprite sprite)
    {
        currentSprite = sprite;
    }
}
