using System.Collections.Generic;
using UnityEngine;

public class PieceManager : MonoBehaviour
{
    private static readonly Dictionary<ChessPieceType, float> groundOffsets = new Dictionary<ChessPieceType, float>
    {
        { ChessPieceType.Ogre, 1.5f },
        { ChessPieceType.Hunter, 1.5f },
        { ChessPieceType.Priestess, 1.5f },
        { ChessPieceType.Skeleton, 1.5f },
        { ChessPieceType.Dog, 1f },
        { ChessPieceType.Knight, 1f },
        { ChessPieceType.Werewolf, 1f },
        { ChessPieceType.Vampir, 1f }
    };

    public TileManager tileManager;
    public FogOfWarManager fogOfWarManager;
    [Header("Pref")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] public Material[] teamMaterials;

    public ChessPieces[,] chessPieces;
    public const int BoardSizeX = 40;
    public const int BoardSizeY = 40;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        tileManager = TileManager.Instance;
        if (tileManager == null)
            tileManager = FindAnyObjectByType<TileManager>();
        if (fogOfWarManager == null)
            fogOfWarManager = FindAnyObjectByType<FogOfWarManager>();
        if (tileManager == null)
        {
            Debug.LogError("TileManager nie znaleziony!");
            return;
        }
        chessPieces = new ChessPieces[BoardSizeX, BoardSizeY];

    }
    public void SpawnAllPieces()
    {
        chessPieces = new ChessPieces[TileManager.Tile_Count_X, TileManager.Tile_Count_Y];

        if (GameData.Instance.CurrentGameMode == GameMode.SinglePlayer)
            SpawnSinglePlayerPieces();
        else if (GameData.Instance.CurrentGameMode == GameMode.MultiTeam)
            SpawnMultiTeamPieces();
    }


    private void SpawnSinglePlayerPieces()
    {
        int whiteTeam = 0;
        int blackTeam = 1;
        int whiteId = 1;
        int blackId = 1;
        int x = 0;

        foreach (var piece in GameData.Instance.playerCharacters)
        {
            chessPieces[x, 0] = SpawnSinglePiece(piece, whiteTeam, whiteId++);
            x++;
        }

        x = 0;

        foreach (var piece in GameData.Instance.enemyCharacters)
        {
            chessPieces[x, TileManager.Tile_Count_Y - 1] = SpawnSinglePiece(piece, blackTeam, blackId++);
            x++;
        }
    }
    private void SpawnMultiTeamPieces()
    {
        int team = 0;
        int height = TileManager.Tile_Count_Y;

        foreach (var pieces in GameData.Instance.selectedCharacters)
        {
            int x = 0, pieceID = 1;
            // wybierz wiersz zale¿nie od teamu
            int yStart = (team == 0) ? 0 : (height - 1);

            foreach (var type in pieces)
            {
                chessPieces[x, yStart] = SpawnSinglePiece(type, team, pieceID++);
                Debug.Log($"[PieceManager] Stworzony pionek {type} dla dru¿yny {team} na pozycji ({x},{yStart})");
                x++;
            }
            team++;
        }
    }


    private ChessPieces SpawnSinglePiece(ChessPieceType type, int team, int id)
    {
        ChessPieces cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPieces>();
        cp.Init(type, team, id);

        var mr = cp.GetComponent<MeshRenderer>();

        // --- Bezpieczne pobranie materia³u ---
        Material baseMat;
        if (team < teamMaterials.Length)
        {
            baseMat = teamMaterials[team];
        }
        else
        {
            Debug.LogWarning($"[PieceManager] Brak teamMaterials[{team}] (Length={teamMaterials.Length}), u¿ywam teamMaterials[0].");
            baseMat = teamMaterials[0];
        }

        // --- Utworzenie instancji materia³u, ¿eby nie modyfikowaæ sharedMaterial ---
        Material instMat = new Material(baseMat);

        // --- Bezpieczne pobranie koloru ---
        Color col = Color.white;
        if (team < GameData.Instance.teamColors.Count)
        {
            col = GameData.Instance.teamColors[team];
        }
        else
        {
            Debug.LogWarning($"[PieceManager] Brak teamColors[{team}] (Count={GameData.Instance.teamColors.Count}), u¿ywam White.");
        }

        instMat.color = col;
        mr.material = instMat;

        // offsety i fog:
        if (groundOffsets.TryGetValue(type, out float offset))
            cp.groundOffset = offset;
        else
            cp.groundOffset = 0.5f;

        cp.fogOfWarManager = fogOfWarManager;
        return cp;
    }



    public void PositionAllPieces()
    {
        for (int x = 0; x < TileManager.Tile_Count_X; x++)
        {
            for (int y = 0; y < TileManager.Tile_Count_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    PositionSinglePiece(x, y, true);
                }
            }
        }
    }

    // Ustawia pionek tak, aby jego dolna krawêdŸ (bounding box) styka³a siê z kafelkiem.
    public void PositionSinglePiece(int x, int y, bool force = false)
    {
        ChessPieces piece = chessPieces[x, y];
        piece.currentX = x;
        piece.currentY = y;

        float tileHeight = tileManager.tiles[x, y].transform.position.y;
        // Ustaw tymczasowo pozycjê, aby bounding box zosta³ obliczony w world space
        piece.transform.position = new Vector3(x * tileManager.tileSize, 0f, y * tileManager.tileSize);

        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            float minY = combinedBounds.min.y;
            float offset = tileHeight - minY;
            float smallLift = 0.5f;
            float finalY = offset + smallLift;
            piece.transform.position = new Vector3(x * tileManager.tileSize, finalY, y * tileManager.tileSize);
            Debug.Log($"PositionSinglePiece: Piece {piece.name} at tile({x},{y}): tileHeight={tileHeight}, bounds.min.y={minY}, offset={offset}, finalY={finalY}");
        }
        else
        {
            piece.transform.position = new Vector3(x * tileManager.tileSize, tileHeight, y * tileManager.tileSize);
        }
        piece.transform.rotation = Quaternion.identity;
    }
    public ChessPieces GetPieceAt(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < TileManager.Tile_Count_X && y < TileManager.Tile_Count_Y)
        {
            return chessPieces[x, y];
        }
        return null;
    }
}
