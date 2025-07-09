using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public static AIController Instance;
    private ChessBoard board;
    public TileManager tileManager;
    public PieceManager pieceManager;
    public TurnManager turnManager;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        if (tileManager == null)
            tileManager = FindAnyObjectByType<TileManager>();
        if (pieceManager == null)
            pieceManager = FindAnyObjectByType<PieceManager>();
        if (turnManager == null)
            turnManager = FindAnyObjectByType<TurnManager>();
        if (board == null)
            board = FindAnyObjectByType<ChessBoard>();

        if (tileManager == null) Debug.LogError("tileManager is null in AIController!");
        if (board == null) Debug.LogError("chessBoard is null in AIController!");
    }

    public void PlayTurn(int teamId)
    {
        StartCoroutine(ExecuteAITurn(teamId));
    }

    private IEnumerator ExecuteAITurn(int teamId)
    {
        yield return new WaitForSeconds(1f); // ma³e "zastanowienie"

        List<ChessPieces> pieces = GetTeamPieces(teamId);

        foreach (var piece in pieces)
        {
            if (piece == null || piece.health <= 0) continue;

            board.currentlyDragging = piece;

            ChessPieces target = FindBestTarget(piece);
            if (target == null) continue;

            float dist = Vector2Int.Distance(
                new Vector2Int(piece.currentX, piece.currentY),
                new Vector2Int(target.currentX, target.currentY)
            );

            // --- RUCH AI ---
            if (dist > piece.attackRange || piece.movementRange < piece.attackCost)
            {
                Vector2Int? targetPos = FindAttackPosition(piece, target);

                if (targetPos.HasValue)
                {
                    List<Node> path = ChessBoard.Instance.AStarPathFind(
                        ChessBoard.Instance.GetTiles(),
                        (piece.currentX, piece.currentY),
                        (targetPos.Value.x, targetPos.Value.y)
                    );

                    if (path.Count > 1)
                    {
                        // PRZED rozpoczêciem ruchu: sprawdŸ czy piece jest widoczny
                        bool wasVisible = piece.IsVisibleToPlayer();

                        // Zacznij coroutine ruchu
                        yield return ChessBoard.Instance.StartCoroutine(MovePieceAndHandleCamera(piece, path, wasVisible));
                    }
                }
            }

            // --- ATAK AI ---
            dist = Vector2Int.Distance(
                new Vector2Int(piece.currentX, piece.currentY),
                new Vector2Int(target.currentX, target.currentY)
            );

            if (dist <= piece.attackRange && piece.movementRange >= piece.attackCost)
            {
                AttackManager.Instance.AttackEnemyPiece(piece, target, tileManager.tileHeights, tileManager.obstacles, pieceManager.chessPieces);
                //ChessBoard.Instance.AttackEnemyPiece(target.currentX, target.currentY);
                yield return new WaitForSeconds(0.4f);
            }
        }

        yield return new WaitForSeconds(1f);
        turnManager.ChangeTurn();
    }

    // NOWA METODA (dodaj j¹ do AIController)
    private IEnumerator MovePieceAndHandleCamera(ChessPieces piece, List<Node> path, bool wasVisibleAtStart)
    {
        var cameraController = Camera.main.GetComponent<CameraController>();
        bool cameraSwitched = false;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int nextPos = new Vector2Int(path[i].X, path[i].Y);
            // Zrób faktyczny ruch pionka (ten fragment skopiuj z ChessBoard.MovePieceAlongPath)

            // Tutaj fragment animacji ruchu pionka – najlepiej wywo³aæ coroutine z ChessBoard
            yield return ChessBoard.Instance.StartCoroutine(
                ChessBoard.Instance.MovePieceAlongPathStep(piece, nextPos) // Musisz dodaæ tak¹ metodê, która wykonuje jeden krok
            );

            // W TRAKCIE ruchu: sprawdŸ czy pionek wszed³ w pole widzenia gracza
            if (!cameraSwitched && piece.IsVisibleToPlayer())
            {
                cameraController.SetTarget(piece.transform);
                cameraSwitched = true;
            }
        }

        // Po zakoñczonym ruchu wróæ kamer¹ na ostatni pionek gracza
        if (cameraSwitched && ChessBoard.Instance.lastPlayerPiece != null)
        {
            yield return new WaitForSeconds(0.5f);
            cameraController.SetTarget(ChessBoard.Instance.lastPlayerPiece.transform);
        }
    }


    private List<ChessPieces> GetTeamPieces(int teamId)
    {
        List<ChessPieces> result = new List<ChessPieces>();

        ChessPieces[,] board = pieceManager.chessPieces;

        for (int x = 0; x < board.GetLength(0); x++)
        {
            for (int y = 0; y < board.GetLength(1); y++)
            {
                var cp = board[x, y];
                if (cp != null && cp.team == teamId)
                    result.Add(cp);
            }
        }

        return result;
    }

    private ChessPieces FindNearestEnemy(ChessPieces from)
    {
        ChessPieces nearest = null;
        float minDistance = float.MaxValue;

        ChessPieces[,] board = ChessBoard.Instance.GetComponent<ChessBoard>().pieceManager.chessPieces;

        for (int x = 0; x < board.GetLength(0); x++)
        {
            for (int y = 0; y < board.GetLength(1); y++)
            {
                var cp = board[x, y];
                if (cp != null && cp.team != from.team)
                {
                    float dist = Vector2Int.Distance(
                        new Vector2Int(from.currentX, from.currentY),
                        new Vector2Int(cp.currentX, cp.currentY)
                    );
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = cp;
                    }
                }
            }
        }

        return nearest;
    }
    private ChessPieces FindBestTarget(ChessPieces attacker)
    {
        ChessPieces[,] board = pieceManager.chessPieces;
        ChessPieces bestTarget = null;
        float bestScore = float.MaxValue; // im mniejsze HP, tym lepszy cel

        for (int x = 0; x < board.GetLength(0); x++)
        {
            for (int y = 0; y < board.GetLength(1); y++)
            {
                ChessPieces target = board[x, y];

                if (target != null && target.team != attacker.team)
                {
                    float dist = Vector2Int.Distance(
                        new Vector2Int(attacker.currentX, attacker.currentY),
                        new Vector2Int(target.currentX, target.currentY)
                    );

                    // Sprawdzamy czy cel jest w maksymalnym mo¿liwym zasiêgu ruchu + ataku
                    if (dist <= attacker.maxMovementRange + attacker.attackRange)
                    {
                        float score = target.health; // tutaj mo¿emy dodaæ wiêcej czynników

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestTarget = target;
                        }
                    }
                }
            }
        }

        return bestTarget;
    }

    private Vector2Int? FindAttackPosition(ChessPieces attacker, ChessPieces target)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        int range = attacker.attackRange;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int tx = target.currentX + dx;
                int ty = target.currentY + dy;

                if (tx < 0 || ty < 0 || tx >= TileManager.Tile_Count_X || ty >= TileManager.Tile_Count_Y)
                    continue;

                if (Mathf.RoundToInt(Vector2Int.Distance(new Vector2Int(tx, ty), new Vector2Int(target.currentX, target.currentY))) > range)
                    continue;

                if (pieceManager.GetPieceAt(tx, ty) != null)
                    continue;

                if (ChessBoard.Instance.IsObstacle(tx, ty))
                    continue;

                candidates.Add(new Vector2Int(tx, ty));
            }
        }

        // ZnajdŸ najbli¿sze osi¹galne pole
        Vector2Int? best = null;
        int shortestPath = int.MaxValue;

        foreach (var pos in candidates)
        {
            var path = ChessBoard.Instance.AStarPathFind(ChessBoard.Instance.GetTiles(),
                                                         (attacker.currentX, attacker.currentY),
                                                         (pos.x, pos.y));
            if (path.Count > 0 && path.Count < shortestPath)
            {
                shortestPath = path.Count;
                best = pos;
            }
        }

        return best;
    }

}
