using UnityEngine;

public class DevCheatsHotkey : MonoBehaviour
{
    [SerializeField] private int playerTeam = 0;      // team gracza
    [SerializeField] private KeyCode killKey = KeyCode.G;

    private PieceManager pieceManager;
    private TurnManager turnManager;

    private void Awake()
    {
        pieceManager = FindAnyObjectByType<PieceManager>();
        turnManager = FindAnyObjectByType<TurnManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(killKey))
            KillAllEnemiesAndRegisterBestiary();
    }

    private void KillAllEnemiesAndRegisterBestiary()
    {
        if (pieceManager == null || pieceManager.chessPieces == null)
        {
            Debug.LogWarning("[DEV] Brak PieceManager/chessPieces w scenie walki.");
            return;
        }

        var grid = pieceManager.chessPieces;
        int w = grid.GetLength(0), h = grid.GetLength(1);
        int killed = 0, registered = 0;

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                var p = grid[x, y];
                if (p == null || p.team == playerTeam) continue;

                // --- BESTIARIUSZ: rejestruj typ zabitego wroga jako kill gracza ---
                var type = GetPieceType(p); // <- jeœli u Ciebie to 'pieceType', zmieñ w jednej linijce
                if (type != ChessPieceType.None && GameData.Instance != null)
                {
                    bool ok = GameData.Instance.TryRegisterKill(type, playerTeam, p.team);
                    if (ok) registered++;
                }

                // --- Usuñ wroga z planszy ---
                grid[x, y] = null;
                Destroy(p.gameObject);
                killed++;
            }

        Debug.Log($"[DEV] Killed enemies: {killed}, bestiary new types: {registered}");
        turnManager?.CheckGameOver(); // wywo³a GameOver() -> Day++ -> powrót do Map
    }

    // Dostosuj jeœli Twoja klasa ChessPieces nazywa to inaczej (np. 'pieceType')
    private ChessPieceType GetPieceType(ChessPieces p)
    {
        // Najczêœciej spotykane pole to 'type'
        return p != null ? p.type : ChessPieceType.None;
    }
}
