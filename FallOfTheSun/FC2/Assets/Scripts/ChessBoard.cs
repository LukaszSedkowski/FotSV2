using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
//using static UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.EventSystems;


public class ChessBoard : MonoBehaviour
{
    public TileManager tileManager;
    public PieceManager pieceManager;
    public AttackManager attackManager;
    public FogOfWarManager fogOfWarManager;
    public HighlightManager highlightManager;
    public TurnManager turnManager;
    
    [Header("Pref")]
    [SerializeField] private GameObject obstaclePrefab;
    
    [Header("HUD")]
    [SerializeField] private TeamPanel CurrentPiecePanel;
    [SerializeField] public SkillsPanel skillsPanel;

    [Header("References")]
    public HighlightManager HighlightManager;

    private float[,] TileWarFogHeight;

    public ChessPieces currentlyDragging;
    public ChessPieces lastPlayerPiece;

    private bool[] teamIsActive;
    private Color originalColor;
   
    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    
    private int p1StartX, p1StartY, p1Width, p1Height;
    private float p1HeightValue;
    private int p2StartX, p2StartY, p2Width, p2Height;
    private float p2HeightValue;

    // Pola do definiowania plateau (wzniesienia/dołka)
    private int plateauStartX, plateauStartY, plateauWidth, plateauHeight;
    private float plateauHeightValue;

    public static ChessBoard Instance { get; private set; }
    [Header("UI")]
    [SerializeField] private HoverStatsUI hoverStatsUI;
    private void Start()
    {
        //Inicjalizacja managerów
        tileManager = FindAnyObjectByType<TileManager>();
        pieceManager = FindAnyObjectByType<PieceManager>();
        highlightManager.Init(tileManager, pieceManager, this);
        pieceManager = FindObjectOfType<PieceManager>();
        highlightManager.Init(tileManager, pieceManager, this);
        fogOfWarManager = FindAnyObjectByType<FogOfWarManager>();
        highlightManager = FindAnyObjectByType<HighlightManager>();
        turnManager = FindAnyObjectByType<TurnManager>();

        //fragment od tryby gry a raczej żeby się nie zbugowało nic 

        if (GameData.Instance.CurrentGameMode == GameMode.SinglePlayer)
        {
            List<ChessPieceType> playerCharacters = GameData.Instance.playerCharacters;
            List<ChessPieceType> enemyCharacters = GameData.Instance.enemyCharacters;
        }

        if (AIController.Instance == null)
        {
            GameObject aiObj = new GameObject("AIController");
            aiObj.AddComponent<AIController>();
        }

        pieceManager.SpawnAllPieces();
        pieceManager.PositionAllPieces();
        Camera.main.GetComponent<CameraController>().FitCameraToBoard(TileManager.Tile_Count_X, TileManager.Tile_Count_Y, tileManager.tileSize);


        fogOfWarManager.Init(tileManager);
        highlightManager.Init(tileManager, pieceManager, this);

        int numberOfTeams = GameData.Instance.selectedCharacters.Count;
        bool[] isAIControlled = GameData.Instance.isAIControlledTeams;

        turnManager.Init(numberOfTeams, isAIControlled, pieceManager, CurrentPiecePanel, highlightManager, fogOfWarManager);

        // Wybór pionka z ID równym 1 na początku gry
        SelectPieceById(1, turnManager.currentTeam);

        if (currentlyDragging != null)
        {
            fogOfWarManager.UpdateFogOfWar(currentlyDragging.currentX, currentlyDragging.currentY, pieceManager.chessPieces);
        }

        CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);

        highlightManager.HighlightPossibleMoves(currentlyDragging);
        highlightManager.highlightedTiles = new bool[TileManager.Tile_Count_X, TileManager.Tile_Count_Y];

        // Ustawienie kamery na wybrany pionek
        Camera.main.GetComponent<CameraController>().SetTarget(pieceManager.chessPieces[0, 0].transform);
    }

    private void Update()
    {

        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        HandleMouseInput();
        HandleKeyboardShortcuts();
        HandleMousePathHighlight();
        ShowHoverStats();
        
    }
    void HandleMousePathHighlight()
    {
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit info, 100, LayerMask.GetMask("Tile")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if (currentHover != hitPosition)
            {
                if (currentHover != -Vector2Int.one)
                    tileManager.tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material.color = Color.white;

                // przelicz i podświetl zasięg
                highlightManager.HighlightPossibleMoves(currentlyDragging);

                highlightManager.currentPath.Clear();
                highlightManager.HighLightPath((hitPosition.x, hitPosition.y));

                tileManager.tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>().material.color = Color.red;
                currentHover = hitPosition;
            }
        }
    }
    private void ShowHoverStats()
    {
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        int tileMask = LayerMask.GetMask("Tile");

        if (Physics.Raycast(ray, out RaycastHit hit, 100, tileMask))
        {
            Vector2Int tilePos = LookupTileIndex(hit.transform.gameObject);
            ChessPieces piece = pieceManager.chessPieces[tilePos.x, tilePos.y];

            if (piece != null
                && piece.IsVisibleToPlayer()
                && piece.team != turnManager.currentTeam
                && piece.GetComponent<MeshRenderer>().enabled)
            {
                hoverStatsUI.Show(piece);
                return;
            }
        }

        hoverStatsUI.Hide();
    }



    private void HandleMouseInput()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

        // --- 1) Spróbuj wybrać pionek ---
        int pieceMask = LayerMask.GetMask("Piece");
        if (Physics.Raycast(ray, out RaycastHit hitPiece, 100, pieceMask))
        {
            var cp = hitPiece.transform.GetComponent<ChessPieces>();
            if (cp != null && cp.team == turnManager.currentTeam && !turnManager.isAIControlledTeam[turnManager.currentTeam])
            {
                currentlyDragging = cp;
                lastPlayerPiece = cp;
                Camera.main.GetComponent<CameraController>().SetTarget(cp.transform);
                skillsPanel.SetCurrentPiece(cp);
                Debug.Log("[ChessBoard] Wybrano pionek: " + cp.name);
                return;
            }
        }

        // --- 2) Dopiero teraz obsługa kliknięcia w kafelek ---
        int tileMask = LayerMask.GetMask("Tile");
        if (Physics.Raycast(ray, out RaycastHit hitTile, 100, tileMask))
        {
            Debug.Log("[ChessBoard] Click na kafelek: " + hitTile.transform.name);
            Vector2Int pos = LookupTileIndex(hitTile.transform.gameObject);

            // Jeśli jest pionek na kafelku – to atak
            var target = pieceManager.chessPieces[pos.x, pos.y];
            if (target != null && currentlyDragging != null)
            {
                attackManager.AttackEnemyPiece(currentlyDragging, target,
                    tileManager.tileHeights, tileManager.obstacles, pieceManager.chessPieces);
                CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);
                turnManager.CheckGameOver();
                return;
            }

            // Ruch
            if (currentlyDragging != null && currentlyDragging.team == turnManager.currentTeam)
            {

                bool moved = MoveTo(currentlyDragging, pos.x, pos.y);
                if (!moved)
                    currentlyDragging.transform.position = tileManager.GetTileCenter(
                        currentlyDragging.currentX, currentlyDragging.currentY, currentlyDragging);
                fogOfWarManager.UpdateFogOfWar(currentlyDragging.currentX, currentlyDragging.currentY, pieceManager.chessPieces);
                fogOfWarManager.UpdatePieceVisibility(pieceManager.chessPieces);
            }
        }

    }

    void HandleKeyboardShortcuts()
    {
        // Zmiana tury po wciśnięciu Q
        if (Input.GetKeyDown(KeyCode.Q) && !turnManager.isAIControlledTeam[turnManager.currentTeam])
        {
            if (currentlyDragging != null && currentlyDragging.isMoving)
            {
                Debug.Log("Nie możesz zakończyć swojej tury przed zakończeniem swojego ruchu");
                return;
            }
            turnManager.ChangeTurn();
        }

        // Zmiana pionka na podstawie klawiszy od 1 do 9
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
            {
                if (currentlyDragging != null && currentlyDragging.isMoving)
                {
                    Debug.Log("Nie możesz zmienić pionka — aktualny jeszcze się porusza.");
                    return;
                }
                SelectPieceById(i, turnManager.currentTeam);

                CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);

                highlightManager.HighlightPossibleMoves(currentlyDragging);
            }
        }

        highlightManager.currentPath.Clear();
    }

    void OnSelectPiece(ChessPieces piece)
    {
        Debug.Log($"[SkillsPanel] currentPiece ustawione na {piece.type}");
        currentlyDragging = piece;
        skillsPanel.SetCurrentPiece(piece);
    }
    private void HideCharacter(ChessPieces piece)
{
    // Ukryj model 3D (mesh)
    var meshRenderers = piece.GetComponentsInChildren<MeshRenderer>();
    foreach (var renderer in meshRenderers)
    {
        renderer.enabled = false;
    }

    Debug.Log($"Pionek {piece.name} schowany w kryjówce.");
}
private void UnHideCharacter(ChessPieces piece)
{
    // Ukryj model 3D (mesh)
    var meshRenderers = piece.GetComponentsInChildren<MeshRenderer>();
    foreach (var renderer in meshRenderers)
    {
        renderer.enabled = true;
    }

}
private bool IsOnHideoutTile(ChessPieces piece)
{
    if (piece == null) return false;

    int x = piece.currentX;
    int y = piece.currentY;

    if (x < 0 || y < 0 || x >= TileManager.Tile_Count_X || y >= TileManager.Tile_Count_Y) return false;

    GameObject tile = tileManager.tiles[x, y];
    return tile != null && tile.CompareTag("Hideout");
}
    public GameObject[,] GetTiles()
    {
        return tileManager.tiles;
    }
    public float[,] GetTilesHeight()
    {
        return tileManager.tileHeights;
    }
    public bool IsObstacle(int x, int y)
    {
        return tileManager.obstacles[x, y];
    }
    private void HealTeam(int team)
    {
        foreach (var piece in pieceManager.chessPieces)
        {
            if (piece != null && piece.team == team)
            {
                piece.health = Mathf.Min(piece.health + 20, piece.maxHealth);
                Debug.Log($"Healed {piece.type} on team {team}. Current health: {piece.health}/{piece.maxHealth}");
            }
        }
    }
    
    private bool IsObstacleBetween(ChessPieces attacker, ChessPieces target)
    {
        int x1 = attacker.currentX;
        int y1 = attacker.currentY;
        int x2 = target.currentX;
        int y2 = target.currentY;

        // Obliczamy różnice w współrzędnych
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);

        // Iterujemy po linii między dwoma punktami
        int sx = (x1 < x2) ? 1 : -1;
        int sy = (y1 < y2) ? 1 : -1;
        int err = dx - dy;

        while (x1 != x2 || y1 != y2)
        {
            // Sprawdzamy, czy na tym polu jest przeszkoda (pomijając cel)
            if (tileManager.obstacles[x1, y1] && !(x1 == target.currentX && y1 == target.currentY))
            {
                return true; // Znaleziono przeszkodę na drodze
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }

        return false; // Nie znaleziono przeszkód
    }

    private static readonly (int, int)[] Directions = { (0, 1), (1, 0), (0, -1), (-1, 0) };

    public List<Node> AStarPathFind(GameObject[,] grid, (int, int) start, (int, int) end)
    {
        var openList = new List<Node>();
        var closedList = new HashSet<(int, int)>();
        var startNode = new Node(start.Item1, start.Item2);
        var endNode = new Node(end.Item1, end.Item2);

        openList.Add(startNode);

        while (openList.Count > 0)
        {
            var currentNode = openList.OrderBy(n => n.F).First();

            if (currentNode.X == endNode.X && currentNode.Y == endNode.Y)
                return ReconstructPath(currentNode);

            openList.Remove(currentNode);
            closedList.Add((currentNode.X, currentNode.Y));

            foreach (var (dx, dy) in Directions)
            {
                int newX = currentNode.X + dx;
                int newY = currentNode.Y + dy;

                if (!IsValid(grid, newX, newY) || closedList.Contains((newX, newY)))
                    continue;

                var neighbor = new Node(newX, newY)
                {
                    G = currentNode.G + 1,
                    H = Math.Abs(newX - endNode.X) + Math.Abs(newY - endNode.Y),
                    Parent = currentNode
                };

                if (openList.Any(n => n.X == newX && n.Y == newY && n.G <= neighbor.G))
                    continue;

                openList.Add(neighbor);
            }
        }

        return new List<Node>();
    }

    private bool IsValid(GameObject[,] grid, int x, int y)
    {
        return x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1) && !tileManager.obstacles[x, y] /*&& highlightedTilesList.Contains(new Vector2Int(x, y))*/;
    }

    private List<Node> ReconstructPath(Node node)
    {
        var path = new List<Node>();
        while (node != null)
        {
            path.Add(node);
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }

    public bool MoveTo(ChessPieces cp, int targetX, int targetY)
    {
        if (pieceManager.chessPieces[targetX, targetY] != null)
        {
            Debug.LogWarning($"[MoveTo] Pole ({targetX},{targetY}) zajęte! Ruch anulowany.");
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Tworzenie tablicy odwiedzonych pól i zmiennej dla najkrótszego kosztu
        bool[,] visited = new bool[pieceManager.chessPieces.GetLength(0), pieceManager.chessPieces.GetLength(1)];
        int shortestCost = int.MaxValue;

        List<Node> path2 = AStarPathFind(tileManager.tiles, (currentlyDragging.currentX, currentlyDragging.currentY), (targetX, targetY));
        if (path2.Count != 0) shortestCost = path2.Count - 1;

        // Sprawdzenie, czy istnieje najkrótsza ścieżka do celu
        if (path2.Count - 1 > cp.movementRange)
        {
            Debug.Log("Nie znaleziono ścieżki.");
            return false;
        }
        // Sprawdzenie, czy pionek ma wystarczająco punktów ruchu
        if (shortestCost > cp.movementRange)
        {
            Debug.Log("Za mało punktów ruchu.");
            return false;
        }

        // Uruchom Coroutine do animacji ruchu
        StartCoroutine(MovePieceAlongPath(cp, path2));

        // Zaktualizuj pionka
        cp.currentX = targetX;
        cp.currentY = targetY;

        // Zaktualizuj planszę
        pieceManager.chessPieces[targetX, targetY] = cp;
        pieceManager.chessPieces[previousPosition.x, previousPosition.y] = null;

        // Zaktualizuj pozostałe punkty ruchu
        cp.movementRange -= shortestCost;

        CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);
        //Podświetlenie możliwych ruchów po zakończeniu ruchu
        if (!turnManager.isAIControlledTeam[cp.team])
        {
            highlightManager.HighlightPossibleMoves(cp);
        }

        Debug.Log($"Pionek przesunięty na ({targetX}, {targetY}). Koszt ruchu: {shortestCost}, pozostałe punkty ruchu: {cp.movementRange}");

        return true;
    }

    private IEnumerator MovePieceAlongPath(ChessPieces cp, List<Node> path)
    {
        cp.isMoving = true;
        float moveDuration = 0.5f;
        Vector3 startPosition = cp.transform.position;

        // Sprawdź, czy pionek zaczyna na kryjówce
        bool wasOnHideout = IsOnHideoutTile(cp);

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int currentPos = new Vector2Int(path[i - 1].X, path[i - 1].Y);
            Vector2Int nextPos = new Vector2Int(path[i].X, path[i].Y);

            Vector3 targetPosition = tileManager.GetTileCenter(nextPos.x, nextPos.y, cp);
            float elapsedTime = 0f;

            // Aktualizuj pozycję w tablicy pionków
            cp.currentX = nextPos.x;
            cp.currentY = nextPos.y;

            fogOfWarManager.UpdateFogOfWar(nextPos.x, nextPos.y, pieceManager.chessPieces);

            // Sprawdź, czy pionek właśnie wchodzi lub wychodzi z kryjówki
            bool isOnHideout = IsOnHideoutTile(cp);
            if (wasOnHideout && !isOnHideout)
            {
                // Wychodzi z kryjówki - odsłoń pionek
                UnHideCharacter(cp);
            }
            else if (!wasOnHideout && isOnHideout)
            {
                // Wchodzi do kryjówki - schowaj pionek
                HideCharacter(cp);
            }
            wasOnHideout = isOnHideout;

            fogOfWarManager.UpdatePieceVisibility(pieceManager.chessPieces);

            while (elapsedTime < moveDuration)
            {
                cp.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            cp.transform.position = targetPosition;
            startPosition = targetPosition;
        }

        pieceManager.PositionSinglePiece(cp.currentX, cp.currentY);
        highlightManager.currentPath.Clear();
        highlightManager.ResetTileColors();

        // Na koniec upewnij się, że pionek jest ukryty lub odkryty zgodnie z miejscem
        if (IsOnHideoutTile(cp))
        {
            HideCharacter(cp);
        }
        else
        {
            UnHideCharacter(cp);
        }
        cp.isMoving = false;
        Debug.Log($"Pionek dotarł na {path[path.Count - 1]}. Aktualizacja pozycji na planszy.");
    }

    private IEnumerator MovePieceWithAnimation(ChessPieces cp, Vector2Int startPos, Vector2Int targetPos)
    {
        // Oblicz czas trwania animacji
        float moveDuration = 0.8f; // Czas trwania animacji (w sekundach)
        float elapsedTime = 0f;

        // Pobierz aktualną pozycję pionka na planszy
        Vector3 startPosition = cp.transform.position;
        Vector3 targetPosition = tileManager.GetTileCenter(targetPos.x, targetPos.y, cp); // Funkcja, która zwraca środek kafelka

        // Animuj ruch pionka
        while (elapsedTime < moveDuration)
        {
            // Interpolacja pozycji (płynne przejście od startowej do docelowej pozycji)
            cp.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // Czekaj na następną klatkę
        }

        // Zapewnij, że pionek dotrze dokładnie na docelową pozycję
        cp.transform.position = targetPosition;

        // Zaktualizuj jego pozycję na planszy (po zakończeniu ruchu)
        pieceManager.chessPieces[targetPos.x, targetPos.y] = cp;
        pieceManager.chessPieces[startPos.x, startPos.y] = null;

        // Po zakończeniu animacji, możesz również zaktualizować inne elementy, jak np. punkty ruchu
        Debug.Log($"Pionek dotarł na ({targetPos.x}, {targetPos.y}).");
    }

    private void SelectPieceById(int id, int teamId)
    {
        foreach (var piece in pieceManager.chessPieces)
        {
            if (piece != null && piece.Id == id)
            {
                if (piece.team == teamId)
                {
                    currentlyDragging = piece;

                    if (!turnManager.isAIControlledTeam[teamId])
                    {
                        lastPlayerPiece = piece;
                    }

                    Camera.main.GetComponent<CameraController>().SetTarget(piece.transform);
                    Debug.Log($"Wybrano pionka z ID: {id} dla drużyny: {teamId}");
                    return;
                }
                else
                {
                    Debug.Log($"Nie można wybrać pionka z ID: {id} - należy do innej drużyny.");
                }
            }
        }
        Debug.Log("Nie znaleziono pionka z danym ID lub pionek należy do innej drużyny.");
    }

    // Pomocnicza metoda, która sprawdza, czy dwa prostokątne obszary są od siebie oddalone o co najmniej minDistance
    
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TileManager.Tile_Count_X; x++)
        {
            for (int y = 0; y < TileManager.Tile_Count_Y; y++)
            {
                if (tileManager.tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one;
    }

    public IEnumerator MovePieceAlongPathStep(ChessPieces cp, Vector2Int nextPos)
    {
        cp.isMoving = true;
        float moveDuration = 0.2f; // czas ruchu na jedno pole (możesz zmienić)

        Vector3 startPosition = cp.transform.position;
        Vector3 targetPosition = tileManager.GetTileCenter(nextPos.x, nextPos.y, cp);

        // Przenieś pionek w tablicy na nową pozycję
        pieceManager.chessPieces[cp.currentX, cp.currentY] = null;
        pieceManager.chessPieces[nextPos.x, nextPos.y] = cp;

        // Aktualizuj pozycję logiczną
        cp.currentX = nextPos.x;
        cp.currentY = nextPos.y;

        // Tu możesz dorzucić: UpdateFogOfWar i UpdatePieceVisibility, jeśli chcesz
        fogOfWarManager.UpdateFogOfWar(cp.currentX, cp.currentY, pieceManager.chessPieces);
        fogOfWarManager.UpdatePieceVisibility(pieceManager.chessPieces);

        float elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            cp.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        cp.transform.position = targetPosition;
        cp.isMoving = false;
    }
}


