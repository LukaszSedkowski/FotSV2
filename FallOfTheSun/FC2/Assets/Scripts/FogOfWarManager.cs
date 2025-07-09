using UnityEngine;

public class FogOfWarManager : MonoBehaviour
{
    [Header("Fog")]
    [SerializeField] private GameObject prefabFog;
    public GameObject[,] fogTiles;
    private TileManager tileManager;
    private ChessBoard board;
    private TurnManager turnManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();
    }
    public void Init(TileManager tileMan/*, PieceManager pieceMan*/)
    {
        tileManager = tileMan;
        //pieceManager = pieceMan;
        GenerateFogOfWar();
    }
    public void GenerateFogOfWar()
    {
        fogTiles = new GameObject[TileManager.Tile_Count_X, TileManager.Tile_Count_Y];

        for (int x = 0; x < TileManager.Tile_Count_X; x++)
        {
            for (int y = 0; y < TileManager.Tile_Count_Y; y++)
            {
                float height = tileManager.tileHeights[x, y];
                Vector3 pos = new Vector3((x * tileManager.tileSize) + 0.01f, height + 1, (y * tileManager.tileSize) + 0.01f);

                GameObject fog = Instantiate(prefabFog, pos, Quaternion.identity);
                fogTiles[x, y] = fog;
            }
        }
    }
    public void UpdateFogOfWar(int posX, int posY, ChessPieces[,] chessPieces)
    {
        // Resetuj mg³ê wojny (zakryj ca³¹ mapê)
        for (int x = 0; x < TileManager.Tile_Count_X; x++)
        {
            for (int y = 0; y < TileManager.Tile_Count_Y; y++)
            {
                fogTiles[x, y].SetActive(true);
            }
        }

        // Ods³oñ obszar wokó³ wszystkich pionków z aktywnej dru¿yny
        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.team == 0 && !turnManager.isAIControlledTeam[piece.team])
            {
                RevealArea(piece.currentX, piece.currentY, piece.visionRange); // Zakres widocznoœci: 3 pola
            }
        }
        /*if (currentlyDragging != null && currentlyDragging.team == currentTeam && !isAIControlledTeam[currentlyDragging.team])
        {
            RevealArea(posX, posY, currentlyDragging.visionRange);
        }*/
    }
    private void RevealArea(int centerX, int centerY, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int newX = centerX + dx;
                int newY = centerY + dy;

                if (newX >= 0 && newX < TileManager.Tile_Count_X && newY >= 0 && newY < TileManager.Tile_Count_Y)
                {
                    fogTiles[newX, newY].SetActive(false);
                }
            }
        }
    }
    public void UpdatePieceVisibility(ChessPieces[,] chessPieces)
    {
        foreach (var piece in chessPieces)
        {
            if (piece != null)
            {
                // Ukryj pionek, jeœli znajduje siê w ukrytym obszarze mg³y
                if (fogTiles[piece.currentX, piece.currentY].activeSelf && turnManager.isAIControlledTeam[piece.team])
                {
                    piece.gameObject.SetActive(false);
                }
                else
                {
                    piece.gameObject.SetActive(true);
                }
            }
        }
    }
}
