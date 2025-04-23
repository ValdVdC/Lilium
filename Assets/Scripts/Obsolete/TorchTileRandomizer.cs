using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TorchTileRandomizer : MonoBehaviour
{
    public Tilemap tilemap;
    public RandomTorchTile torchTileAsset; // Asset base com os sprites
    public float minInterval = 0.4f;
    public float maxInterval = 0.7f;

    private class TorchTileState
    {
        public Vector3Int position;
        public int currentSpriteIndex;
        public float timer;
        public float interval;
    }

    private List<TorchTileState> activeTorchTiles = new List<TorchTileState>();

    void Start()
    {
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(pos);
            if (tile == torchTileAsset)
            {
                int index = Random.Range(0, torchTileAsset.possibleSprites.Length);

                var tileInstance = ScriptableObject.CreateInstance<RandomTorchTile>();
                tileInstance.possibleSprites = torchTileAsset.possibleSprites;
                tileInstance.SetSprite(torchTileAsset.possibleSprites[index]);

                tilemap.SetTile(pos, tileInstance);

                activeTorchTiles.Add(new TorchTileState
                {
                    position = pos,
                    currentSpriteIndex = index,
                    timer = Random.Range(0f, maxInterval),
                    interval = Random.Range(minInterval, maxInterval)
                });
            }
        }
    }

    void Update()
    {
        foreach (var torch in activeTorchTiles)
        {
            torch.timer -= Time.deltaTime;
            if (torch.timer <= 0f)
            {
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, torchTileAsset.possibleSprites.Length);
                } while (newIndex == torch.currentSpriteIndex && torchTileAsset.possibleSprites.Length > 1);

                torch.currentSpriteIndex = newIndex;

                var tileInstance = ScriptableObject.CreateInstance<RandomTorchTile>();
                tileInstance.possibleSprites = torchTileAsset.possibleSprites;
                tileInstance.SetSprite(torchTileAsset.possibleSprites[newIndex]);

                tilemap.SetTile(torch.position, tileInstance);

                torch.interval = Random.Range(minInterval, maxInterval);
                torch.timer = torch.interval;
            }
        }
    }
}
