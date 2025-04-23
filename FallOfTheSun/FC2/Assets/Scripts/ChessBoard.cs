using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using static UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine.ProBuilder.Shapes;


public class ChessBoard : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Material[] tileMaterials; // Materiał dla kafelków
    [SerializeField] private Material hoverMaterial; // Materiał do podświetlenia
    [SerializeField] private Material pillarMaterial; // Materiał dla filarów
    [SerializeField] private GameObject[] tilePrefabs;

    [Header("Pref")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject obstaclePrefabRock;


    [Header("HUD")]
    [SerializeField] private TeamPanel CurrentPiecePanel;

    [Header("Fog")]
    [SerializeField] private GameObject prefabFog;

    private GameObject[,] fogTiles;
    private float[,] TileWarFogHeight;

    private bool[,] obstacles;

    public ChessPieces[,] chessPieces;
    public ChessPieces currentlyDragging;
    private bool[] teamIsActive;
    private Color originalColor;
    public const int Tile_Count_X = 40;
    public const int Tile_Count_Y = 40;
    private float tileSize = 1.0f;

    private bool[,] highlightedTiles;

    private GameObject[,] tiles;
    private float[,] tileHeights;
    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private List<Vector2Int> highlightedTilesList = new List<Vector2Int>();
    private int currentTeam = 0; // Aktualna drużyna (zaczynamy od drużyny 0)
    private bool[] isAIControlledTeam;
    private int numberOfTeams; // Przykładowo, ustawiamy na 4 drużyny

    private int p1StartX, p1StartY, p1Width, p1Height;
    private float p1HeightValue;
    private int p2StartX, p2StartY, p2Width, p2Height;
    private float p2HeightValue;

    // Pola do definiowania plateau (wzniesienia/dołka)
    private int plateauStartX, plateauStartY, plateauWidth, plateauHeight;
    private float plateauHeightValue;

    private List<Node> currentPath = new List<Node>();

    public static ChessBoard Instance { get; private set; }

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

    private void Awake()
    {
        Instance = this;
        numberOfTeams = teamMaterials.Length;

        isAIControlledTeam = new bool[numberOfTeams];

        // Zakładamy że drużyna 1 to AI (możesz dostosować)
        isAIControlledTeam[0] = false; // Gracz
        isAIControlledTeam[1] = true;  // AI
                                       // Jeśli 4 drużyny, możesz dodać więcej
        if (AIController.Instance == null)
        {
            GameObject aiObj = new GameObject("AIController");
            aiObj.AddComponent<AIController>();
        }
        GenerateAllTiles(tileSize, Tile_Count_X, Tile_Count_Y);
        InitializeTileHeights();
        GenerateFogOfWar();
        //GenerateWarFog(tiles.GetLength(1), tiles.GetLength(0), TileWarFogHeight);
        SpawnAllPieces();
        PositionAllPieces();
        obstacles = new bool[Tile_Count_X, Tile_Count_Y]; // Inicjalizacja tablicy przeszkód

        // Dodajemy przeszkody w losowych miejscach (przykład)
        AddRandomObstacles(5);
        // Wybór pionka z ID równym 1 na początku gry
        SelectPieceById(1, currentTeam);

        if (currentlyDragging != null)
        {
            UpdateFogOfWar(currentlyDragging.currentX, currentlyDragging.currentY);
        }

        CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);

        HighlightPossibleMoves(currentlyDragging);
        highlightedTiles = new bool[Tile_Count_X, Tile_Count_Y];

        // Ustawienie kamery na wybrany pionek
        Camera.main.GetComponent<CameraController>().SetTarget(chessPieces[0, 0].transform);
    }




    private void Update()
    {

        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit info, 100, LayerMask.GetMask("Tile")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if (currentHover != hitPosition)
            {
                if (currentHover != -Vector2Int.one)
                {
                    tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material.color = Color.white;
                    currentHover = -Vector2Int.one;

                    // Przywróć podświetlone pola
                    ReapplyHighlightedTiles();
                    HighLightPath((hitPosition.x, hitPosition.y));
                }

                // Zmieniamy kolor na żółty
                tiles[hitPosition.x, hitPosition.y].GetComponent<MeshRenderer>().material.color = Color.red;
                currentHover = hitPosition;
            }


            // Wybieranie i przenoszenie pionka
            if (Input.GetMouseButtonDown(0))
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    if (currentlyDragging == chessPieces[hitPosition.x, hitPosition.y])
                    {
                        // Jeśli kliknięto na aktualnie przeciąganego pionka, nic nie rób
                    }
                    else if (chessPieces[hitPosition.x, hitPosition.y].team == currentTeam)
                    {
                        currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                        Camera.main.GetComponent<CameraController>().SetTarget(currentlyDragging.transform);
                    }
                    else
                    {
                        // Atak na przeciwnika
                        AttackEnemyPiece(hitPosition.x, hitPosition.y);
                        CheckGameOver();
                    }
                }
                else if (currentlyDragging != null)
                {
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                    bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    if (!validMove)
                    {
                        currentlyDragging.transform.position = GetTileCenter(previousPosition.x, previousPosition.y, currentlyDragging);

                    }
                    UpdateFogOfWar(currentlyDragging.currentX, currentlyDragging.currentY);
                    UpdatePieceVisibility();
                }
            }
        }
        else if (currentHover != -Vector2Int.one)
        {
            tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material.color = Color.white;
            currentHover = -Vector2Int.one;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentlyDragging != null && currentlyDragging.type == ChessPieceType.Priestess)
            {
                if (currentlyDragging.maxMovementRange == currentlyDragging.movementRange)
                {

                    HealTeam(currentTeam);
                    Debug.Log($"Healing applied for team {currentTeam}.");
                    currentlyDragging.movementRange = 0;
                    HighlightPossibleMoves(currentlyDragging);
                }
            }
        }
        // Zmiana tury po wciśnięciu Q
        if (Input.GetKeyDown(KeyCode.Q) && !isAIControlledTeam[currentTeam])
        {
            ChangeTurn();
        }

        // Zmiana pionka na podstawie klawiszy od 1 do 9
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
            {
                SelectPieceById(i, currentTeam);

                CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);

                HighlightPossibleMoves(currentlyDragging);
            }
        }

        currentPath.Clear();
    }
    public GameObject[,] GetTiles()
    {
        return tiles;
    }
    public float[,] GetTilesHeight()
    {
        return tileHeights;
    }
    public bool IsObstacle(int x, int y)
    {
        return obstacles[x, y];
    }
    private void HealTeam(int team)
    {
        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.team == team)
            {
                piece.health = Mathf.Min(piece.health + 20, piece.maxHealth);
                Debug.Log($"Healed {piece.type} on team {team}. Current health: {piece.health}/{piece.maxHealth}");
            }
        }
    }
    private bool DoesTeamHavePieces(int teamId)
    {
        // Sprawdzamy, czy jakikolwiek pionek należy do danej drużyny i jest żywy
        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.team == teamId)
            {
                return true;
            }
        }
        return false;
    }
    private void CheckGameOver()
    {
        // Sprawdzamy, czy na planszy są jeszcze pionki przeciwników
        for (int team = 0; team < numberOfTeams; team++)
        {
            if (team == currentTeam) continue; // Pomijamy aktualną drużynę

            bool enemyFound = false;
            foreach (var piece in chessPieces)
            {
                if (piece != null && piece.team == team)
                {
                    enemyFound = true;
                    break;
                }
            }

            if (enemyFound)
            {
                return; // Wciąż są przeciwnicy, nie kończymy gry
            }
        }

        // Jeśli nie znaleziono przeciwników, gra się kończy
        GameOver();
    }
    private void GameOver()
    {
        Debug.Log("Gra zakończona! Drużyna " + currentTeam + " wygrywa!");

        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu()
    {
        Debug.Log("Ładowanie sceny MainMenu...");
        SceneManager.LoadScene("MainMenu");
        yield return new WaitForSeconds(1);
    }


    private void ResetMovementRangeForTeam(int team)
    {
        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {
                ChessPieces cp = chessPieces[x, y];
                if (cp != null && cp.team == team)
                {
                    cp.movementRange = cp.maxMovementRange; // Resetowanie punktów ruchu na maksymalne
                }
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
            if (obstacles[x1, y1] && !(x1 == target.currentX && y1 == target.currentY))
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


    private void AddRandomObstacles(int number)
    {
        for (int i = 0; i < number; i++)
        {
            int x = UnityEngine.Random.Range(0, Tile_Count_X);
            int y = UnityEngine.Random.Range(0, Tile_Count_Y);

            // Sprawdzamy, czy pole jest puste i nie jest przeszkodą
            if (chessPieces[x, y] == null && !obstacles[x, y])
            {
                obstacles[x, y] = true;

                // Pobieramy wysokość dla tego kafelka
                float tileHeight = tileHeights[x, y];

                // Uzyskujemy pozycję kafelka i ustawiamy wysokość przeszkody
                Vector3 position = GetTileCenter(x, y, currentlyDragging);
                position.y = tileHeight + 0.8f; // Ustawienie wysokości przeszkody zgodnie z wysokością kafelka

                // Debugowanie pozycji
                Debug.Log("Placing obstacle at: " + position);

                // Dodajemy przeszkodę w tym miejscu
                Instantiate(obstaclePrefabRock, position, Quaternion.identity);
            }
        }
    }



    public void HighlightPossibleMoves(ChessPieces cp)
    {
        ResetTileColors(); // Reset kolorów przed podświetleniem nowych
        highlightedTilesList.Clear(); // Wyczyść poprzednią listę
        currentPath.Clear();
        int startX = cp.currentX;
        int startY = cp.currentY;
        int remainingMoves = cp.movementRange;
        int width = chessPieces.GetLength(0);
        int height = chessPieces.GetLength(1);

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
            float currentHeight = tiles[currentX, currentY].transform.position.y;

            for (int i = 0; i < 4; i++)
            {
                int newX = currentX + dx[i];
                int newY = currentY + dy[i];

                if (newX >= 0 && newY >= 0 && newX < width && newY < height)
                {
                    if (chessPieces[newX, newY] == null && !obstacles[newX, newY])
                    {
                        float nextHeight = tiles[newX, newY].transform.position.y;
                        int heightDifference = Mathf.Abs(Mathf.RoundToInt(currentHeight - nextHeight));
                        int movementCost = 1 + Mathf.Min(heightDifference, 2);

                        if (cost[currentX, currentY] + movementCost < cost[newX, newY] && cost[currentX, currentY] + movementCost <= remainingMoves)
                        {
                            cost[newX, newY] = cost[currentX, currentY] + movementCost;
                            queue.Enqueue((newX, newY));

                            Renderer tileRenderer = tiles[newX, newY].GetComponent<Renderer>();
                            if (tileRenderer != null)
                            {
                                tileRenderer.material.color = Color.yellow;
                                highlightedTilesList.Add(new Vector2Int(newX, newY)); // Dodaj współrzędne do listy
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
            Renderer tileRenderer = tiles[pos.x, pos.y].GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material.color = Color.yellow;
            }
        }
        foreach (Node pos in currentPath)
        {
            Renderer tileRenderer = tiles[pos.X, pos.Y].GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material.color = Color.blue;
            }
        }
    }

    private void HighLightPath((int, int) end)
    {
        List<Node> pathList = AStarPathFind(tiles, (currentlyDragging.currentX, currentlyDragging.currentY), (end.Item1, end.Item2));
        foreach (var pos in pathList)
        {
            Renderer tileRenderer = tiles[pos.X, pos.Y].GetComponent<Renderer>();
            if (tileRenderer != null)
            {
                tileRenderer.material.color = Color.blue;
                currentPath.Add(new Node(pos.X, pos.Y));
            }
        }
    }

    private void ResetTileColors()
    {
        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {
                MeshRenderer tileRenderer = tiles[x, y].GetComponent<MeshRenderer>();
                tileRenderer.material.color = Color.white; // Resetowanie koloru na biały
            }
        }
    }

    public void AttackEnemyPiece(int targetX, int targetY)
    {
        ChessPieces targetPiece = chessPieces[targetX, targetY];

        // Sprawdzenie, czy cel to przeciwnik
        if (targetPiece.team != currentlyDragging.team)
        {
            if (currentlyDragging.movementRange < currentlyDragging.attackCost)
            {
                Debug.Log("Za mało ruchu, aby wykonać atak.");
                return;
            }
            // Obliczanie odległości między pionkiem a celem, uwzględniając wysokość
            float distance = Mathf.Sqrt(
                Mathf.Pow(currentlyDragging.currentX - targetPiece.currentX, 2) +
                Mathf.Pow(currentlyDragging.currentY - targetPiece.currentY, 2) +
                Mathf.Pow(tileHeights[currentlyDragging.currentX, currentlyDragging.currentY] - tileHeights[targetX, targetY], 2)
            );
            distance = Mathf.Round(distance * 100f) / 100f;
            // Sprawdzenie, czy odległość jest mniejsza lub równa zasięgowi ataku
            if (distance <= currentlyDragging.attackRange) // Zakładam, że masz pole attackRange w ChessPieces
            {
                // Sprawdzamy, czy ruch na skos nie wykracza poza dozwolony zasięg
                if (distance > currentlyDragging.attackRange)
                {
                    Debug.Log($"Cel poza zasięgiem ataku. Odległość: {distance}");
                    return;
                }

                // Sprawdzanie, czy atakowany pionek jest w odległości 1 od przeszkody
                bool isNearObstacle = false;
                int[] dx = { 1, -1, 0, 0 };
                int[] dy = { 0, 0, 1, -1 };
                for (int i = 0; i < 4; i++)
                {
                    int checkX = targetX + dx[i];
                    int checkY = targetY + dy[i];

                    if (checkX >= 0 && checkY >= 0 && checkX < Tile_Count_X && checkY < Tile_Count_Y)
                    {
                        if (obstacles[checkX, checkY])
                        {
                            isNearObstacle = true;
                            break;
                        }
                    }
                }

                // Sprawdzanie, czy przeszkoda stoi na drodze
                bool isObstacleBetween = IsObstacleBetween(currentlyDragging, targetPiece);

                // Zmniejszenie obrażeń, jeśli oba warunki są spełnione
                int damage = currentlyDragging.attack; // Zakładam, że masz attackDamage w ChessPieces
                if (isNearObstacle && isObstacleBetween)
                {
                    damage -= 4;
                    damage = Mathf.Max(damage, 0); // Upewniamy się, że obrażenia nie będą ujemne
                    Debug.Log("Obrażenia zmniejszone o 4 z powodu przeszkody.");
                }

                // Zastosowanie obrażeń
                targetPiece.health -= damage; // Zakładam, że masz pole health w ChessPieces

                Debug.Log($"Zaatakowano pionek przeciwnika. Zadano {damage} obrażeń. Pozostałe zdrowie: {targetPiece.health}. zasięg - {distance}");
                currentlyDragging.movementRange = currentlyDragging.movementRange - currentlyDragging.attackCost;
                currentlyDragging.TriggerPassiveAbility();
                HighlightPossibleMoves(currentlyDragging);

                // Sprawdzenie, czy pionek został zniszczony
                if (targetPiece.health <= 0)
                {
                    Destroy(targetPiece.gameObject);
                    chessPieces[targetX, targetY] = null;
                    Debug.Log("Pionek przeciwnika został zniszczony.");
                }
            }
            else
            {
                Debug.Log("Cel jest poza zasięgiem ataku.");
            }
        }
    }

    private void InitializeTileHeights()
    {
        tileHeights = new float[Tile_Count_X, Tile_Count_Y];
        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {
                // Przypisanie wysokości (np. wysokość losowa lub zdefiniowana)
                tileHeights[x, y] = tiles[x, y].transform.position.y;
            }
        }
    }

    private void SelectPieceWithLowestId(int team)
    {
        ChessPieces lowestIdPiece = null;

        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.team == team)
            {
                if (lowestIdPiece == null || piece.Id < lowestIdPiece.Id)
                {
                    lowestIdPiece = piece;

                }
            }
        }

        if (lowestIdPiece != null)
        {
            currentlyDragging = lowestIdPiece;
            Debug.Log("Wybrany pionek z najmniejszym ID: " + currentlyDragging.Id);
        }
        else
        {
            Debug.Log("Brak pionków dla drużyny " + team);
        }
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
        return x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1) && !obstacles[x, y] /*&& highlightedTilesList.Contains(new Vector2Int(x, y))*/;
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
        if (chessPieces[targetX, targetY] != null)
        {
            Debug.LogWarning($"[MoveTo] Pole ({targetX},{targetY}) zajęte! Ruch anulowany.");
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Tworzenie tablicy odwiedzonych pól i zmiennej dla najkrótszego kosztu
        bool[,] visited = new bool[chessPieces.GetLength(0), chessPieces.GetLength(1)];
        int shortestCost = int.MaxValue;

        List<Node> path2 = AStarPathFind(tiles, (currentlyDragging.currentX, currentlyDragging.currentY), (targetX, targetY));
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
        chessPieces[targetX, targetY] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        // Zaktualizuj pozostałe punkty ruchu
        cp.movementRange -= shortestCost;

        CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);
        // (Opcjonalne) Podświetlenie możliwych ruchów po zakończeniu ruchu
        HighlightPossibleMoves(cp);

        Debug.Log($"Pionek przesunięty na ({targetX}, {targetY}). Koszt ruchu: {shortestCost}, pozostałe punkty ruchu: {cp.movementRange}");

        return true;
    }

    private IEnumerator MovePieceAlongPath(ChessPieces cp, List<Node> path)
    {
        float moveDuration = 0.5f; // Możesz dostosować czas trwania ruchu

        Vector3 startPosition = cp.transform.position;


        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int currentPos = new Vector2Int(path[i - 1].X, path[i - 1].Y);
            Vector2Int nextPos = new Vector2Int(path[i].X, path[i].Y);

            // Oblicz pozycję docelową
            Vector3 targetPosition = GetTileCenter(nextPos.x, nextPos.y, cp);
            float elapsedTime = 0f;
            UpdateFogOfWar(nextPos.x, nextPos.y);
            UpdatePieceVisibility();
            while (elapsedTime < moveDuration)
            {
                cp.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Upewnij się, że pionek osiągnął dokładnie docelową pozycję
            cp.transform.position = targetPosition;

            // Przejdź do następnego punktu
            startPosition = targetPosition;
        }
        // Po zakończeniu animacji popraw pozycję pionka za pomocą bounding boxa
        PositionSinglePiece(cp.currentX, cp.currentY);

        // Po zakończeniu ruchu czyścimy trasę, aby nie była widoczna stale
        currentPath.Clear();

        // Możesz też przywrócić domyślne kolory kafelków
        ResetTileColors();

        // Po zakończeniu ruchu, zaktualizuj planszę
        Debug.Log($"Pionek dotarł na {path[path.Count - 1]}. Aktualizacja pozycji na planszy.");
    }


    private IEnumerator MovePieceWithAnimation(ChessPieces cp, Vector2Int startPos, Vector2Int targetPos)
    {
        // Oblicz czas trwania animacji
        float moveDuration = 0.8f; // Czas trwania animacji (w sekundach)
        float elapsedTime = 0f;

        // Pobierz aktualną pozycję pionka na planszy
        Vector3 startPosition = cp.transform.position;
        Vector3 targetPosition = GetTileCenter(targetPos.x, targetPos.y, cp); // Funkcja, która zwraca środek kafelka

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
        chessPieces[targetPos.x, targetPos.y] = cp;
        chessPieces[startPos.x, startPos.y] = null;

        // Po zakończeniu animacji, możesz również zaktualizować inne elementy, jak np. punkty ruchu
        Debug.Log($"Pionek dotarł na ({targetPos.x}, {targetPos.y}).");
    }

    private void SelectPieceById(int id, int teamId)
    {
        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.Id == id)
            {
                if (piece.team == teamId)
                {
                    currentlyDragging = piece;
                    Camera.main.GetComponent<CameraController>().SetTarget(piece.transform); // Ustawienie nowego celu kamery
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
    private bool RectanglesTooClose(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh, int minDistance)
    {
        int aLeft = ax - minDistance;
        int aRight = ax + aw + minDistance;
        int aTop = ay - minDistance;
        int aBottom = ay + ah + minDistance;

        return (bx < aRight && (bx + bw) > aLeft && by < aBottom && (by + bh) > aTop);
    }

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        // Inicjalizacja tablic
        tiles = new GameObject[tileCountX, tileCountY];
        tileHeights = new float[tileCountX, tileCountY];

        // 1. Ustaw bazową wysokość mapy na 5
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tileHeights[x, y] = 5f;
            }
        }

        // Parametry wspólne
        int plateauMinSize = 4;  // Plateau musi mieć co najmniej 4x4 pola
        int borderOffset = 2;    // Plateau musi być co najmniej 2 pola od krawędzi mapy
        int minDistance = 2;     // Plateau muszą być od siebie oddalone co najmniej o 2 pola

        // 2. Generowanie wypukłego plateau (górka)
        int p1Width = UnityEngine.Random.Range(plateauMinSize, tileCountX / 2);
        int p1Height = UnityEngine.Random.Range(plateauMinSize, tileCountY / 2);
        int p1StartX = UnityEngine.Random.Range(borderOffset, tileCountX - p1Width - borderOffset);
        int p1StartY = UnityEngine.Random.Range(borderOffset, tileCountY - p1Height - borderOffset);
        float p1HeightValue = 6f; // Wypukłe plateau: baza (5) + 1 = 6

        // Nadpisujemy obszar wypukłego plateau
        for (int x = p1StartX; x < p1StartX + p1Width; x++)
        {
            for (int y = p1StartY; y < p1StartY + p1Height; y++)
            {
                tileHeights[x, y] = p1HeightValue;
            }
        }
        p1Width = UnityEngine.Random.Range(plateauMinSize, tileCountX / 2);
        p1Height = UnityEngine.Random.Range(plateauMinSize, tileCountY / 2);
        p1StartX = UnityEngine.Random.Range(borderOffset, tileCountX - p1Width - borderOffset);
        p1StartY = UnityEngine.Random.Range(borderOffset, tileCountY - p1Height - borderOffset);
        p1HeightValue = 6f;
        // 3. Generowanie wklęsłego plateau (dołek)
        int p2Width, p2Height, p2StartX, p2StartY;
        float p2HeightValue;
        bool validP2 = false;
        int attempts = 0;
        do
        {
            p2Width = UnityEngine.Random.Range(plateauMinSize, tileCountX / 2);
            p2Height = UnityEngine.Random.Range(plateauMinSize, tileCountY / 2);
            p2StartX = UnityEngine.Random.Range(borderOffset, tileCountX - p2Width - borderOffset);
            p2StartY = UnityEngine.Random.Range(borderOffset, tileCountY - p2Height - borderOffset);
            p2HeightValue = 4f; // Wklęsłe plateau: baza (5) - 1 = 4

            if (!RectanglesTooClose(p1StartX, p1StartY, p1Width, p1Height, p2StartX, p2StartY, p2Width, p2Height, minDistance))
            {
                validP2 = true;
            }
            attempts++;
        } while (!validP2 && attempts < 100);

        // Nadpisujemy obszar wklęsłego plateau
        for (int x = p2StartX; x < p2StartX + p2Width; x++)
        {
            for (int y = p2StartY; y < p2StartY + p2Height; y++)
            {
                tileHeights[x, y] = p2HeightValue;
            }
        }

        // 4. Generowanie kafelków według wartości w tablicy tileHeights
// Zmieniamy ten fragment, który tworzy kafelek:
for (int x = 0; x < tileCountX; x++)
{
    for (int y = 0; y < tileCountY; y++)
    {
        float height = tileHeights[x, y];
        GameObject tilePrefabToUse = tilePrefabs[0]; // domyślnie płaski
        float heightOffset = 0f;

        // Czy ten kafelek ma być pochylnią?
        bool isRamp = false;
        Vector3 rampDirection = Vector3.zero;

        if (x > 0 && tileHeights[x - 1, y] > height)
        {
            isRamp = true;
            rampDirection = Vector3.left;
        }
        else if (x < tileCountX - 1 && tileHeights[x + 1, y] > height)
        {
            isRamp = true;
            rampDirection = Vector3.right;
        }
        else if (y > 0 && tileHeights[x, y - 1] > height)
        {
            isRamp = true;
            rampDirection = Vector3.back;
        }
        else if (y < tileCountY - 1 && tileHeights[x, y + 1] > height)
        {
            isRamp = true;
            rampDirection = Vector3.forward;
        }

        if (isRamp)
        {
            tilePrefabToUse = tilePrefabs[1]; // prefab schodów
            heightOffset = 1f; // bo schody mają wyższy koniec
        }

        // Pozycja kafelka
        Vector3 pos = new Vector3(x * tileSize, height + heightOffset, y * tileSize);
        GameObject go = Instantiate(tilePrefabToUse, pos, Quaternion.identity, transform);
        go.name = $"Tile {x},{y}";
        tiles[x, y] = go;
        go.layer = LayerMask.NameToLayer("Tile");

        // Ustaw materiał
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.material = tileMaterials[(x + y) % 2];
        }

        // Filar pod kafelek
        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pillar.transform.parent = go.transform;
        pillar.transform.localScale = new Vector3(1, height - 1f, 1);
        pillar.transform.localPosition = new Vector3(0, -(height / 2), 0);
        pillar.GetComponent<MeshRenderer>().material = pillarMaterial;

        // Obróć schody w stronę wyższego kafelka
        if (isRamp)
        {
            float angle = 0f;
            if (rampDirection == Vector3.left) angle = -90f;
            else if (rampDirection == Vector3.right) angle = 90f;
            else if (rampDirection == Vector3.back) angle = 180f;
            // forward (czyli domyślnie) to 0°

            go.transform.rotation = Quaternion.Euler(0, angle, 0);
        }
    }
}






    }





    private GameObject GenerateSingleTile(float tileSize, int x, int y, int heightLevel)
    {
int prefabIndex = (x + y) % tilePrefabs.Length; // lub inny sposób wyboru
GameObject tileObject = Instantiate(tilePrefabs[prefabIndex], transform);
        tileObject.transform.parent = transform;
        tileObject.transform.localScale = new Vector3(tileSize, tileSize, tileSize); // Zmiana na sześcian
        tileObject.transform.position = new Vector3(x * tileSize, heightLevel * tileSize, y * tileSize); // Dopasowanie pozycji do rozmiaru sześcianu
int materialIndex = UnityEngine.Random.Range(0, tileMaterials.Length); // logiku od textur i tego jak się generują
tileObject.GetComponent<MeshRenderer>().material = tileMaterials[materialIndex]; // Przypisanie materiału do kafelka
        tileObject.layer = LayerMask.NameToLayer("Tile");
        

        // Generowanie filaru pod kafelkiem, jeśli jest na wyższej wysokości
        if (heightLevel > 0)
        {
            for (int h = 0; h < heightLevel; h++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.transform.parent = transform;
                pillar.transform.localScale = new Vector3(tileSize, tileSize, tileSize); // Ustaw rozmiar filaru na kafelek
                pillar.transform.position = new Vector3(x * tileSize, h * tileSize, y * tileSize); // Ustaw pozycję filaru w odpowiednim miejscu
                pillar.GetComponent<MeshRenderer>().material = pillarMaterial; // Przypisanie materiału do filaru
            }
        }

        return tileObject;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one;
    }



    private void SpawnAllPieces()
    {
        chessPieces = new ChessPieces[Tile_Count_X, Tile_Count_Y];

        int whiteTeam = 0, blackTeam = 1, redTeam = 2, blueTeam = 3;
        int whiteId = 1, blackId = 1, redId = 1, blueId = 1; // ID dla obu drużyn zaczynają się od 1
        int i = 0, team = 0;
        
        foreach (var pieces in GameMenu.Instance.selectedCharacters)
        {
            int pieceID = 1;
            foreach (var piece in pieces)
            {

                chessPieces[i, 0] = SpawnSinglePiece(piece, team, pieceID++);
                Debug.Log("Stworzony pionek" + piece);
                i++;
            }
            team++;
        }

    }

    private ChessPieces SpawnSinglePiece(ChessPieceType type, int team, int id)
    {
        ChessPieces cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPieces>();
        cp.Init(type, team, id); // Przekazanie ID
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];

        if (groundOffsets.TryGetValue(type, out float offset))
            cp.groundOffset = offset;
        else
            cp.groundOffset = 0.5f; // domyślny offset

        return cp;
    }


    private void PositionAllPieces()
    {
        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    PositionSinglePiece(x, y, true);
                }
            }
        }
    }

    // Ustawia pionek tak, aby jego dolna krawędź (bounding box) stykała się z kafelkiem.
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        ChessPieces piece = chessPieces[x, y];
        piece.currentX = x;
        piece.currentY = y;

        float tileHeight = tiles[x, y].transform.position.y;
        // Ustaw tymczasowo pozycję, aby bounding box został obliczony w world space
        piece.transform.position = new Vector3(x * tileSize, 0f, y * tileSize);

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
            piece.transform.position = new Vector3(x * tileSize, finalY, y * tileSize);
            Debug.Log($"PositionSinglePiece: Piece {piece.name} at tile({x},{y}): tileHeight={tileHeight}, bounds.min.y={minY}, offset={offset}, finalY={finalY}");
        }
        else
        {
            piece.transform.position = new Vector3(x * tileSize, tileHeight, y * tileSize);
        }
        piece.transform.rotation = Quaternion.identity;
    }


    public Vector3 GetTileCenter(int x, int y, ChessPieces movingPiece)
    {
        float tileHeight = tiles[x, y].transform.position.y;
        if (movingPiece == null)
        {
            return new Vector3(x * tileSize, tileHeight, y * tileSize);
        }
        else
        {
            return new Vector3(x * tileSize, tileHeight + movingPiece.groundOffset, y * tileSize);
        }
    }



    //FogOfWar scripts



    private void GenerateFogOfWar()
    {
        fogTiles = new GameObject[Tile_Count_X, Tile_Count_Y];

        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {

                GameObject fog = Instantiate(prefabFog, new Vector3((x * tileSize) + 0.01f, tileHeights[x, y] + 1, (y * tileSize) + 0.01f), Quaternion.identity);
                fog.layer = LayerMask.NameToLayer("Fog");
                fogTiles[x, y] = fog;
            }
        }
    }

    private void UpdateFogOfWar(int posX, int posY)
    {
        // Resetuj mgłę wojny (zakryj całą mapę)
        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {
                fogTiles[x, y].SetActive(true);
            }
        }

        // Odsłoń obszar wokół wszystkich pionków z aktywnej drużyny
        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.team == currentTeam)
            {
                RevealArea(piece.currentX, piece.currentY, piece.visionRange); // Zakres widoczności: 3 pola
            }
        }
        if (currentlyDragging != null && currentlyDragging.team == currentTeam)
        {
            RevealArea(posX, posY, currentlyDragging.visionRange);
        }
    }


    private void RevealArea(int centerX, int centerY, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                int newX = centerX + dx;
                int newY = centerY + dy;

                if (newX >= 0 && newX < Tile_Count_X && newY >= 0 && newY < Tile_Count_Y)
                {
                    fogTiles[newX, newY].SetActive(false);
                }
            }
        }
    }
    private void UpdatePieceVisibility()
    {
        foreach (var piece in chessPieces)
        {
            if (piece != null)
            {
                // Ukryj pionek, jeśli znajduje się w ukrytym obszarze mgły
                if (fogTiles[piece.currentX, piece.currentY].activeSelf)
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
    
    public void ChangeTurn()
    {
        int attempts = numberOfTeams;

        do
        {
            currentTeam = (currentTeam + 1) % numberOfTeams;
            attempts--;

            if (!DoesTeamHavePieces(currentTeam))
            {
                Debug.Log("Drużyna " + (currentTeam + 1) + " nie ma pionków. Pomijam.");
            }
        } while (!DoesTeamHavePieces(currentTeam) && attempts > 0);

        currentlyDragging = null;
        ResetMovementRangeForTeam(currentTeam);

        Debug.Log("Tura drużyny " + (currentTeam + 1));

        SelectPieceWithLowestId(currentTeam);
        CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);
        HighlightPossibleMoves(currentlyDragging);
        UpdateFogOfWar(currentlyDragging.currentX, currentlyDragging.currentY);
        UpdatePieceVisibility();

        
        if (isAIControlledTeam[currentTeam])
        {
            if (AIController.Instance != null)
            {
                AIController.Instance.PlayTurn(currentTeam);
            }
            else
            {
                Debug.LogError("AIController.Instance == null! Czy AIController jest w scenie?");
            }
        }
    }
    public ChessPieces GetPieceAt(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < Tile_Count_X && y < Tile_Count_Y)
        {
            return chessPieces[x, y];
        }
        return null;
    }

}
public class Node
{
    public int X, Y;
    public int G, H;
    public Node Parent;

    public int F => G + H;

    public Node(int x, int y)
    {
        X = x;
        Y = y;
    }
}

