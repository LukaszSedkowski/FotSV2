using System.Collections.Generic;
using UnityEngine;
public class HighlightManager : MonoBehaviour
{
    private TileManager tileManager;
    private PieceManager pieceManager;
    private ChessBoard chessBoard;
    public bool[,] highlightedTiles;
    public List<Vector2Int> highlightedTilesList = new List<Vector2Int>();
    public List<Node> currentPath = new List<Node>();
    private HashSet<Vector2Int> damageZoneCells = new();
    private HashSet<Vector2Int> healZoneCells = new();
    private HighlightType[,] tileHighlightPriority;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public enum HighlightType
    {
        None = 0,
        Move = 1,
        LightTile = 5,
        DarkTile = 6,
        Path = 2,

        ZoneDamage = 3,
        ZoneHeal = 4
    }
    private static readonly Dictionary<HighlightType, Color> highlightColors = new()
    {
    { HighlightType.None, Color.white },
    { HighlightType.Move, Color.yellow },
    { HighlightType.LightTile, new Color(10f, 10f, 10f)}, // lub Color.magenta dynamicznie
    { HighlightType.DarkTile, new Color(-255f, -255f, -255f) }, // lub Color.magenta dynamicznie
    { HighlightType.Path, Color.blue },
    { HighlightType.ZoneDamage, Color.red },
    { HighlightType.ZoneHeal, Color.green },
    };
    public void Init(TileManager tileManager, PieceManager pieceManager, ChessBoard chessBoard)
    {
        this.tileManager = tileManager;
        this.pieceManager = pieceManager;
        this.chessBoard = chessBoard;
    }
    void Start()
    {
        if (chessBoard == null)
            chessBoard = FindAnyObjectByType<ChessBoard>();
        tileHighlightPriority = new HighlightType[TileManager.Tile_Count_X, TileManager.Tile_Count_Y];
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void HighlightPossibleMoves(ChessPieces cp)
    {
        if (tileManager == null)
        {
            Debug.LogError("[HM] tileManager jest NULL – nie wywo³ano Init?");
            return;
        }
        if (pieceManager == null)
        {
            Debug.LogError("[HM] pieceManager jest NULL – nie wywo³ano Init?");
            return;
        }
        if (cp == null)
        {
            Debug.LogWarning("[HM] HighlightPossibleMoves: cp == null – pomijam");
            return;
        }
        ResetTileColors(); // Reset kolorów przed podœwietleniem nowych
        highlightedTilesList.Clear(); // Wyczyœæ poprzedni¹ listê
        currentPath.Clear();
        int startX = cp.currentX;
        int startY = cp.currentY;
        int remainingMoves = cp.movementRange;
        int width = pieceManager.chessPieces.GetLength(0);
        int height = pieceManager.chessPieces.GetLength(1);

        int[,] cost = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                cost[x, y] = int.MaxValue;
            }
        }
        cost[startX, startY] = 0;

        Queue<(int x, int y)> queue = new Queue<(int, int)>();
        queue.Enqueue((startX, startY));

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        while (queue.Count > 0)
        {
            var (currentX, currentY) = queue.Dequeue();
            float currentHeight = tileManager.tiles[currentX, currentY].transform.position.y;

            for (int i = 0; i < 4; i++)
            {
                int newX = currentX + dx[i];
                int newY = currentY + dy[i];

                if (newX >= 0 && newY >= 0 && newX < width && newY < height)
                {
                    if (pieceManager.chessPieces[newX, newY] == null && !tileManager.obstacles[newX, newY])
                    {
                        float nextHeight = tileManager.tiles[newX, newY].transform.position.y;
                        int heightDifference = Mathf.Abs(Mathf.RoundToInt(currentHeight - nextHeight));
                        int movementCost = 1 + Mathf.Min(heightDifference, 2);

                        if (cost[currentX, currentY] + movementCost < cost[newX, newY] && cost[currentX, currentY] + movementCost <= remainingMoves)
                        {
                            cost[newX, newY] = cost[currentX, currentY] + movementCost;
                            queue.Enqueue((newX, newY));

                            SetTileHighlight(newX, newY, HighlightType.Move);
                            highlightedTilesList.Add(new Vector2Int(newX, newY)); // Dodaj wspó³rzêdne do listy

                        }
                    }
                }
            }
        }
    }
    private void ReapplyHighlightedTiles()
    {
        foreach (Vector2Int pos in highlightedTilesList)
        {
            SetTileHighlight(pos.x, pos.y, HighlightType.Move);
        }
        foreach (Node pos in currentPath)
        {
            SetTileHighlight(pos.X, pos.Y, HighlightType.Path);
        }
        ApplyZones();
    }

    public void HighLightPath((int, int) end)
    {
        if (chessBoard == null)
        {
            Debug.LogError("[HM] HighLightPath: chessBoard == null");
            return;
        }
        if (tileManager == null)
        {
            Debug.LogError("[HM] HighLightPath: tileManager == null");
            return;
        }
        if (chessBoard.currentlyDragging == null)
        {
            Debug.LogWarning("[HM] HighLightPath: currentlyDragging == null, pomijam");
            return;
        }
        List<Node> pathList = chessBoard.AStarPathFind(tileManager.tiles, (chessBoard.currentlyDragging.currentX, chessBoard.currentlyDragging.currentY), (end.Item1, end.Item2));
        foreach (var pos in pathList)
        {
            SetTileHighlight(pos.X, pos.Y, HighlightType.Path);
            currentPath.Add(new Node(pos.X, pos.Y));
        }
    }

    public void ResetTileColors()
    {
        tileHighlightPriority = new HighlightType[TileManager.Tile_Count_X, TileManager.Tile_Count_Y];

        var lightTiles = TileManager.Instance.lightTiles;
        var darkTiles = TileManager.Instance.darkTiles;

        for (int x = 0; x < TileManager.Tile_Count_X; x++)
        {
            for (int y = 0; y < TileManager.Tile_Count_Y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);

                if (lightTiles.Contains(pos))
                    SetTileHighlight(x, y, HighlightType.LightTile);
                else if (darkTiles.Contains(pos))
                    SetTileHighlight(x, y, HighlightType.DarkTile);
                else
                    SetTileHighlight(x, y, HighlightType.None);
            }
        }
        ApplyZones();
    }
    public void SetTileHighlight(int x, int y, HighlightType type)
    {
        if (x < 0 || y < 0 || x >= TileManager.Tile_Count_X || y >= TileManager.Tile_Count_Y)
            return;

        if ((int)type >= (int)tileHighlightPriority[x, y])
        {
            var renderer = tileManager.tiles[x, y].GetComponent<MeshRenderer>();
            if (renderer != null && highlightColors.TryGetValue(type, out Color color))
            {
                renderer.material.color = color;
            }

            tileHighlightPriority[x, y] = type;
        }
    }
    public void AddZoneCells(IEnumerable<Vector2Int> cells, bool isDamage)
    {
        var set = isDamage ? damageZoneCells : healZoneCells;
        foreach (var c in cells) set.Add(c);
        ApplyZones(); // natychmiast przemaluj
    }

    public void RemoveZoneCells(IEnumerable<Vector2Int> cells, bool isDamage)
    {
        var set = isDamage ? damageZoneCells : healZoneCells;
        foreach (var c in cells) set.Remove(c);
        ApplyZones();
    }

    private void ApplyZones()
    {
        // Nadaj najwy¿szy priorytet – strefy maj¹ byæ „nad” ruchem/œwiat³em/ciemnoœci¹
        foreach (var c in damageZoneCells)
            SetTileHighlight(c.x, c.y, HighlightType.ZoneDamage);

        foreach (var c in healZoneCells)
            SetTileHighlight(c.x, c.y, HighlightType.ZoneHeal);
    }

}