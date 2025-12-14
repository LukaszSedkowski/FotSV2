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
        { ChessPieceType.Dog, 0.2f },
        { ChessPieceType.Knight, 1f },
        { ChessPieceType.Werewolf, 1f },
        { ChessPieceType.Vampir, 1f }
    };

    private static readonly Vector2Int[] CornerOffsets = new Vector2Int[]
    {
        // mała siatka 4x4 przy rogu (wystarczy na 4 pionki, ale daję więcej “w zapasie”)
        new Vector2Int(0,0),
        new Vector2Int(1,0), new Vector2Int(0,1),
        new Vector2Int(1,1), new Vector2Int(2,0),
        new Vector2Int(0,2), new Vector2Int(2,1),
        new Vector2Int(1,2), new Vector2Int(2,2),
        new Vector2Int(3,0), new Vector2Int(0,3),
        new Vector2Int(3,1), new Vector2Int(1,3),
        new Vector2Int(3,2), new Vector2Int(2,3),
        new Vector2Int(3,3)
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
        int width = TileManager.Tile_Count_X;
        int height = TileManager.Tile_Count_Y;

        // Kotwice narożników: 0=BL, 1=TR, 2=BR, 3=TL
        Vector2Int[] anchors = new Vector2Int[]
        {
        new Vector2Int(0, 0),                     // team 0: bottom-left
        new Vector2Int(width - 1, height - 1),    // team 1: top-right
        new Vector2Int(width - 1, 0),             // team 2: bottom-right
        new Vector2Int(0, height - 1)             // team 3: top-left
        };

        // Kierunki “rozsuwania się” od kotwicy
        Vector2Int[] signs = new Vector2Int[]
        {
        new Vector2Int(+1, +1),   // BL: rośnij X i Y
        new Vector2Int(-1, -1),   // TR: malej X i Y
        new Vector2Int(-1, +1),   // BR: malej X, rośnij Y
        new Vector2Int(+1, -1)    // TL: rośnij X, malej Y
        };

        int team = 0;
        foreach (var pieces in GameData.Instance.selectedCharacters)
        {
            int t = Mathf.Clamp(team, 0, 3); // zabezpieczenie do 4 rogów
            Vector2Int anchor = anchors[t];
            Vector2Int sign = signs[t];

            int pieceID = 1;

            foreach (var type in pieces)
            {
                bool placed = false;

                // próbujemy w obrębie siatki przy rogu
                for (int i = 0; i < CornerOffsets.Length && !placed; i++)
                {
                    int px = anchor.x + sign.x * CornerOffsets[i].x;
                    int py = anchor.y + sign.y * CornerOffsets[i].y;

                    if (px < 0 || py < 0 || px >= width || py >= height)
                        continue;
                    if (tileManager.obstacles[px, py])
                        continue;
                    if (chessPieces[px, py] != null)
                        continue;

                    chessPieces[px, py] = SpawnSinglePiece(type, team, pieceID++);
                    Debug.Log($"[PieceManager] {type} → team {team} @ corner cell ({px},{py})");
                    placed = true;
                }

                // awaryjnie: jeśli wszystkie komórki przy rogu są zajęte, szukamy najbliższej wolnej
                if (!placed)
                {
                    int bestX = -1, bestY = -1;
                    int bestDist = int.MaxValue;

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if (chessPieces[x, y] == null && !tileManager.obstacles[x, y])
                            {
                                int dist = Mathf.Abs(x - anchor.x) + Mathf.Abs(y - anchor.y);
                                if (dist < bestDist)
                                {
                                    bestDist = dist;
                                    bestX = x; bestY = y;
                                }
                            }
                        }
                    }

                    if (bestX >= 0)
                    {
                        chessPieces[bestX, bestY] = SpawnSinglePiece(type, team, pieceID++);
                        Debug.LogWarning($"[PieceManager] {type} → team {team} (FALLBACK) @ ({bestX},{bestY})");
                    }
                    else
                    {
                        Debug.LogError($"[PieceManager] Brak miejsca na spawn dla team {team} – {type}");
                    }
                }
            }

            team++;
        }
    }

    private ChessPieces SpawnSinglePiece(ChessPieceType type, int team, int id)
    {
        ChessPieces cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPieces>();
        cp.Init(type, team, id);

        var mr = cp.GetComponent<MeshRenderer>();

        // --- Bezpieczne pobranie materiału ---
        Material baseMat;
        if (team < teamMaterials.Length)
        {
            baseMat = teamMaterials[team];
        }
        else
        {
            Debug.LogWarning($"[PieceManager] Brak teamMaterials[{team}] (Length={teamMaterials.Length}), używam teamMaterials[0].");
            baseMat = teamMaterials[0];
        }

        // --- Utworzenie instancji materiału, żeby nie modyfikować sharedMaterial ---
        Material instMat = new Material(baseMat);

        // --- Bezpieczne pobranie koloru ---
        Color col = Color.white;
        if (team < GameData.Instance.teamColors.Count)
        {
            col = GameData.Instance.teamColors[team];
        }
        else
        {
            Debug.LogWarning($"[PieceManager] Brak teamColors[{team}] (Count={GameData.Instance.teamColors.Count}), używam White.");
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

    // Ustawia pionek tak, aby jego dolna krawędź (bounding box) stykała się z kafelkiem.
    public void PositionSinglePiece(int x, int y, bool force = false)
    {
        ChessPieces piece = chessPieces[x, y];
        if (piece == null)
        {
            Debug.LogWarning($"PositionSinglePiece: chessPieces[{x},{y}] == null – pomijam pozycjonowanie.");
            return;
        }

        piece.currentX = x;
        piece.currentY = y;

        float tileHeight = tileManager.tiles[x, y].transform.position.y;
        // Ustaw tymczasowo pozycję, aby bounding box został obliczony w world space
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
