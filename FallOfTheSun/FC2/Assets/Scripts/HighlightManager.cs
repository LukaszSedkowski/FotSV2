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
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Init(TileManager tileManager, PieceManager pieceManager)
    {
        this.tileManager = tileManager;
        this.pieceManager = pieceManager;
    }
    void Start()
    {
        if (chessBoard == null)
            chessBoard = FindAnyObjectByType<ChessBoard>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void HighlightPossibleMoves(ChessPieces cp)
    {
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

                            Renderer tileRenderer = tileManager.tiles[newX, newY].GetComponent<Renderer>();
                            if (tileRenderer != null)
                            {
                                tileRenderer.material.color = Color.yellow;
                                highlightedTilesList.Add(new Vector2Int(newX, newY)); // Dodaj wspó³rzêdne do listy
                            }
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
            Renderer tileRenderer = tileManager.tiles[pos.x, pos.y].GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material.color = Color.yellow;
            }
        }
        foreach (Node pos in currentPath)
        {
            Renderer tileRenderer = tileManager.tiles[pos.X, pos.Y].GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material.color = Color.blue;
            }
        }
    }

    public void HighLightPath((int, int) end)
    {
        List<Node> pathList = chessBoard.AStarPathFind(tileManager.tiles, (chessBoard.currentlyDragging.currentX, chessBoard.currentlyDragging.currentY), (end.Item1, end.Item2));
        foreach (var pos in pathList)
        {
            Renderer tileRenderer = tileManager.tiles[pos.X, pos.Y].GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material.color = Color.blue;
                currentPath.Add(new Node(pos.X, pos.Y));
            }
        }
    }

    public void ResetTileColors()
    {
        for (int x = 0; x < TileManager.Tile_Count_X; x++)
        {
            for (int y = 0; y < TileManager.Tile_Count_Y; y++)
            {
                MeshRenderer tileRenderer = tileManager.tiles[x, y].GetComponent<MeshRenderer>();
                tileRenderer.material.color = Color.white; // Resetowanie koloru na bia³y
            }
        }
    }
}
