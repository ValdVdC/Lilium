using UnityEngine;
using UnityEngine.Tilemaps;

public class TorchLightSpawner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
 public Tilemap torchTilemap; // O Tilemap onde as tochas estão desenhadas
    public GameObject lightPrefab; // O Prefab da luz (LightTorch)

    void Start()
    {
        BoundsInt bounds = torchTilemap.cellBounds; // Pega os limites do tilemap
        TileBase[] allTiles = torchTilemap.GetTilesBlock(bounds); // Pega todos os tiles desenhados

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                Vector3Int tilePos = new Vector3Int(x + bounds.xMin, y + bounds.yMin, 0);
                TileBase tile = torchTilemap.GetTile(tilePos);

                if (tile != null)
                {
                    // Instancia a luz no centro do tile
                    Vector3 worldPos = torchTilemap.GetCellCenterWorld(tilePos);
                    Instantiate(lightPrefab, worldPos, Quaternion.identity);
                }
            }
        }
    }
}
