using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using static UnityEditor.Experimental.GraphView.GraphView;
using UnityEngine.ProBuilder.Shapes;


public class DangeonBoard : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Material tileMaterial; // Materia³ dla kafelków
    [SerializeField] private Material hoverMaterial; // Materia³ do podœwietlenia
    [SerializeField] private Material pillarMaterial; // Materia³ dla filarów

    [Header("Pref")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject obstaclePrefabRock;
    [SerializeField] private GameObject rampPrefab;
    [SerializeField] private GameObject stairsPrefab;
    [SerializeField] private GameObject caveEntrancePrefab;

    [Header("HUD")]
    [SerializeField] private TeamPanel CurrentPiecePanel;

    [Header("Fog")]
    [SerializeField] private GameObject prefabFog;

    private GameObject[,] fogTiles;
    private float[,] TileWarFogHeight;

    private bool[,] obstacles;

    private ChessPieces[,] chessPieces;
    private ChessPieces currentlyDragging;
    private bool[] teamIsActive;
    private Color originalColor;
    public const int Tile_Count_X = 20;
    public const int Tile_Count_Y = 20;
    private float tileSize = 1.0f;

    private bool[,] highlightedTiles;

    private GameObject[,] tiles;
    private float[,] tileHeights;
    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private List<Vector2Int> highlightedTilesList = new List<Vector2Int>();
    private int currentTeam = 0; // Aktualna dru¿yna (zaczynamy od dru¿yny 0)
    private int numberOfTeams; // Przyk³adowo, ustawiamy na 4 dru¿yny

    private int p1StartX, p1StartY, p1Width, p1Height;
    private float p1HeightValue;
    private int p2StartX, p2StartY, p2Width, p2Height;
    private float p2HeightValue;

    // Pola do definiowania plateau (wzniesienia/do³ka)
    private int plateauStartX, plateauStartY, plateauWidth, plateauHeight;
    private float plateauHeightValue;

    private List<Node> currentPath = new List<Node>();

    public static DangeonBoard Instance { get; private set; }

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
        GenerateAllTiles(tileSize, Tile_Count_X, Tile_Count_Y);
        InitializeTileHeights();
        GenerateFogOfWar();
        //GenerateWarFog(tiles.GetLength(1), tiles.GetLength(0), TileWarFogHeight);
        SpawnTransferredPieces();
        PositionAllPieces();
        obstacles = new bool[Tile_Count_X, Tile_Count_Y]; // Inicjalizacja tablicy przeszkód

        // Dodajemy przeszkody w losowych miejscach (przyk³ad)
        AddRandomObstacles(5);
        // Wybór pionka z ID równym 1 na pocz¹tku gry
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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Jeœli nowa scena to DungeonScene, odœwie¿ elementy
        if (scene.name == "DungeonScene")
        {
            // Odœwie¿ mg³ê – zak³adaj¹c, ¿e metoda GenerateFogOfWar tworzy mg³ê
            GenerateFogOfWar();

            // Odœwie¿ przejœcia (schody, rampy) – jeœli potrzebujesz innych ustawieñ, mo¿esz dodaæ warunki
            // Przyjmujemy, ¿e plateau wci¹¿ s¹ przechowywane w zmiennych (p1StartX, p1StartY itd.)
            PlacePlateauTransitions(plateauStartX, plateauStartY, plateauWidth, plateauHeight, false);
            // Dla do³ka (wklês³ego plateau):
            PlacePlateauTransitions(p2StartX, p2StartY, p2Width, p2Height, true);

            // Mo¿esz te¿ odœwie¿yæ inne elementy – np. pozycje pionków, je¿eli to potrzebne
            // (jeœli pionki powinny byæ ustawione obok portalu, mo¿esz tutaj wywo³aæ metodê repositionuj¹c¹)
            Debug.Log("DungeonScene za³adowana – odœwie¿ono elementy mapy.");
        }
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

                    // Przywróæ podœwietlone pola
                    ReapplyHighlightedTiles();
                    HighLightPath((hitPosition.x, hitPosition.y));
                }

                // Zmieniamy kolor na ¿ó³ty
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
                        // Jeœli klikniêto na aktualnie przeci¹ganego pionka, nic nie rób
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
        // Zmiana tury po wciœniêciu Q
        if (Input.GetKeyDown(KeyCode.Q))
        {

            int attempts = numberOfTeams; // Ograniczenie liczby prób na wypadek braku pionków u wszystkich dru¿yn
            do
            {
                currentTeam = (currentTeam + 1) % numberOfTeams; // Prze³¹cz na nastêpn¹ dru¿ynê
                attempts--;

                CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);

                if (!DoesTeamHavePieces(currentTeam))
                {
                    Debug.Log("Dru¿yna " + (currentTeam + 1) + " nie ma pionków. Pomijam.");
                }
            } while (!DoesTeamHavePieces(currentTeam) && attempts > 0);

            currentlyDragging = null; // Anulowanie wyboru po zmianie tury

            // Resetowanie punktów ruchu dla dru¿yny, która skoñczy³a turê
            ResetMovementRangeForTeam(currentTeam);


            Debug.Log("Tura dru¿yny " + (currentTeam + 1));

            SelectPieceWithLowestId(currentTeam); // Wybieranie pionka z najni¿szym ID

            CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);

            if (currentlyDragging != null)
            {
                Camera.main.GetComponent<CameraController>().SetTarget(currentlyDragging.transform);
                HighlightPossibleMoves(currentlyDragging);
            }
            UpdateFogOfWar(currentlyDragging.currentX, currentlyDragging.currentY);
            UpdatePieceVisibility();
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
        // Sprawdzamy, czy jakikolwiek pionek nale¿y do danej dru¿yny i jest ¿ywy
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
        // Sprawdzamy, czy na planszy s¹ jeszcze pionki przeciwników
        for (int team = 0; team < numberOfTeams; team++)
        {
            if (team == currentTeam) continue; // Pomijamy aktualn¹ dru¿ynê

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
                return; // Wci¹¿ s¹ przeciwnicy, nie koñczymy gry
            }
        }

        // Jeœli nie znaleziono przeciwników, gra siê koñczy
        GameOver();
    }
    private void GameOver()
    {
        Debug.Log("Gra zakoñczona! Dru¿yna " + currentTeam + " wygrywa!");

        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu()
    {
        Debug.Log("£adowanie sceny MainMenu...");
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

        // Obliczamy ró¿nice w wspó³rzêdnych
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);

        // Iterujemy po linii miêdzy dwoma punktami
        int sx = (x1 < x2) ? 1 : -1;
        int sy = (y1 < y2) ? 1 : -1;
        int err = dx - dy;

        while (x1 != x2 || y1 != y2)
        {
            // Sprawdzamy, czy na tym polu jest przeszkoda (pomijaj¹c cel)
            if (obstacles[x1, y1] && !(x1 == target.currentX && y1 == target.currentY))
            {
                return true; // Znaleziono przeszkodê na drodze
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

            // Sprawdzamy, czy pole jest puste i nie jest przeszkod¹
            if (chessPieces[x, y] == null && !obstacles[x, y])
            {
                obstacles[x, y] = true;

                // Pobieramy wysokoœæ dla tego kafelka
                float tileHeight = tileHeights[x, y];

                // Uzyskujemy pozycjê kafelka i ustawiamy wysokoœæ przeszkody
                Vector3 position = GetTileCenter(x, y, currentlyDragging);
                position.y = tileHeight + 0.5f; // Ustawienie wysokoœci przeszkody zgodnie z wysokoœci¹ kafelka

                // Debugowanie pozycji
                Debug.Log("Placing obstacle at: " + position);

                // Dodajemy przeszkodê w tym miejscu
                Instantiate(obstaclePrefabRock, position, Quaternion.identity);
            }
        }
    }



    private void HighlightPossibleMoves(ChessPieces cp)
    {
        ResetTileColors(); // Reset kolorów przed podœwietleniem nowych
        highlightedTilesList.Clear(); // Wyczyœæ poprzedni¹ listê
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
                tileRenderer.material.color = Color.white; // Resetowanie koloru na bia³y
            }
        }
    }

    private void AttackEnemyPiece(int targetX, int targetY)
    {
        ChessPieces targetPiece = chessPieces[targetX, targetY];

        // Sprawdzenie, czy cel to przeciwnik
        if (targetPiece.team != currentlyDragging.team)
        {
            if (currentlyDragging.movementRange < currentlyDragging.attackCost)
            {
                Debug.Log("Za ma³o ruchu, aby wykonaæ atak.");
                return;
            }
            // Obliczanie odleg³oœci miêdzy pionkiem a celem, uwzglêdniaj¹c wysokoœæ
            float distance = Mathf.Sqrt(
                Mathf.Pow(currentlyDragging.currentX - targetPiece.currentX, 2) +
                Mathf.Pow(currentlyDragging.currentY - targetPiece.currentY, 2) +
                Mathf.Pow(tileHeights[currentlyDragging.currentX, currentlyDragging.currentY] - tileHeights[targetX, targetY], 2)
            );
            distance = Mathf.Round(distance * 100f) / 100f;
            // Sprawdzenie, czy odleg³oœæ jest mniejsza lub równa zasiêgowi ataku
            if (distance <= currentlyDragging.attackRange) // Zak³adam, ¿e masz pole attackRange w ChessPieces
            {
                // Sprawdzamy, czy ruch na skos nie wykracza poza dozwolony zasiêg
                if (distance > currentlyDragging.attackRange)
                {
                    Debug.Log($"Cel poza zasiêgiem ataku. Odleg³oœæ: {distance}");
                    return;
                }

                // Sprawdzanie, czy atakowany pionek jest w odleg³oœci 1 od przeszkody
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

                // Zmniejszenie obra¿eñ, jeœli oba warunki s¹ spe³nione
                int damage = currentlyDragging.attack; // Zak³adam, ¿e masz attackDamage w ChessPieces
                if (isNearObstacle && isObstacleBetween)
                {
                    damage -= 4;
                    damage = Mathf.Max(damage, 0); // Upewniamy siê, ¿e obra¿enia nie bêd¹ ujemne
                    Debug.Log("Obra¿enia zmniejszone o 4 z powodu przeszkody.");
                }

                // Zastosowanie obra¿eñ
                targetPiece.health -= damage; // Zak³adam, ¿e masz pole health w ChessPieces

                Debug.Log($"Zaatakowano pionek przeciwnika. Zadano {damage} obra¿eñ. Pozosta³e zdrowie: {targetPiece.health}. zasiêg - {distance}");
                currentlyDragging.movementRange = currentlyDragging.movementRange - currentlyDragging.attackCost;
                currentlyDragging.TriggerPassiveAbility();
                HighlightPossibleMoves(currentlyDragging);

                // Sprawdzenie, czy pionek zosta³ zniszczony
                if (targetPiece.health <= 0)
                {
                    Destroy(targetPiece.gameObject);
                    chessPieces[targetX, targetY] = null;
                    Debug.Log("Pionek przeciwnika zosta³ zniszczony.");
                }
            }
            else
            {
                Debug.Log("Cel jest poza zasiêgiem ataku.");
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
                // Przypisanie wysokoœci (np. wysokoœæ losowa lub zdefiniowana)
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
            Debug.Log("Brak pionków dla dru¿yny " + team);
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
        return x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1) && !obstacles[x, y] && highlightedTilesList.Contains(new Vector2Int(x, y));
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

    private bool MoveTo(ChessPieces cp, int targetX, int targetY)
    {
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Tworzenie tablicy odwiedzonych pól i zmiennej dla najkrótszego kosztu
        bool[,] visited = new bool[chessPieces.GetLength(0), chessPieces.GetLength(1)];
        int shortestCost = int.MaxValue;

        List<Node> path2 = AStarPathFind(tiles, (currentlyDragging.currentX, currentlyDragging.currentY), (targetX, targetY));
        if (path2.Count != 0) shortestCost = path2.Count - 1;

        // Sprawdzenie, czy istnieje najkrótsza œcie¿ka do celu
        if (path2.Count - 1 > cp.movementRange)
        {
            Debug.Log("Nie znaleziono œcie¿ki.");
            return false;
        }
        // Sprawdzenie, czy pionek ma wystarczaj¹co punktów ruchu
        if (shortestCost > cp.movementRange)
        {
            Debug.Log("Za ma³o punktów ruchu.");
            return false;
        }

        // Uruchom Coroutine do animacji ruchu
        StartCoroutine(MovePieceAlongPath(cp, path2));

        // Zaktualizuj pionka
        cp.currentX = targetX;
        cp.currentY = targetY;

        // Zaktualizuj planszê
        chessPieces[targetX, targetY] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        // Zaktualizuj pozosta³e punkty ruchu
        cp.movementRange -= shortestCost;

        CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);
        // (Opcjonalne) Podœwietlenie mo¿liwych ruchów po zakoñczeniu ruchu
        HighlightPossibleMoves(cp);

        Debug.Log($"Pionek przesuniêty na ({targetX}, {targetY}). Koszt ruchu: {shortestCost}, pozosta³e punkty ruchu: {cp.movementRange}");

        return true;
    }

    private IEnumerator MovePieceAlongPath(ChessPieces cp, List<Node> path)
    {
        float moveDuration = 0.5f; // Mo¿esz dostosowaæ czas trwania ruchu

        Vector3 startPosition = cp.transform.position;


        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int currentPos = new Vector2Int(path[i - 1].X, path[i - 1].Y);
            Vector2Int nextPos = new Vector2Int(path[i].X, path[i].Y);

            // Oblicz pozycjê docelow¹
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

            // Upewnij siê, ¿e pionek osi¹gn¹³ dok³adnie docelow¹ pozycjê
            cp.transform.position = targetPosition;

            // PrzejdŸ do nastêpnego punktu
            startPosition = targetPosition;
        }
        // Po zakoñczeniu animacji popraw pozycjê pionka za pomoc¹ bounding boxa
        PositionSinglePiece(cp.currentX, cp.currentY);

        // Po zakoñczeniu ruchu czyœcimy trasê, aby nie by³a widoczna stale
        currentPath.Clear();

        // Mo¿esz te¿ przywróciæ domyœlne kolory kafelków
        ResetTileColors();

        // Po zakoñczeniu ruchu, zaktualizuj planszê
        Debug.Log($"Pionek dotar³ na {path[path.Count - 1]}. Aktualizacja pozycji na planszy.");
    }


    private IEnumerator MovePieceWithAnimation(ChessPieces cp, Vector2Int startPos, Vector2Int targetPos)
    {
        // Oblicz czas trwania animacji
        float moveDuration = 0.8f; // Czas trwania animacji (w sekundach)
        float elapsedTime = 0f;

        // Pobierz aktualn¹ pozycjê pionka na planszy
        Vector3 startPosition = cp.transform.position;
        Vector3 targetPosition = GetTileCenter(targetPos.x, targetPos.y, cp); // Funkcja, która zwraca œrodek kafelka

        // Animuj ruch pionka
        while (elapsedTime < moveDuration)
        {
            // Interpolacja pozycji (p³ynne przejœcie od startowej do docelowej pozycji)
            cp.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null; // Czekaj na nastêpn¹ klatkê
        }

        // Zapewnij, ¿e pionek dotrze dok³adnie na docelow¹ pozycjê
        cp.transform.position = targetPosition;

        // Zaktualizuj jego pozycjê na planszy (po zakoñczeniu ruchu)
        chessPieces[targetPos.x, targetPos.y] = cp;
        chessPieces[startPos.x, startPos.y] = null;

        // Po zakoñczeniu animacji, mo¿esz równie¿ zaktualizowaæ inne elementy, jak np. punkty ruchu
        Debug.Log($"Pionek dotar³ na ({targetPos.x}, {targetPos.y}).");
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
                    Debug.Log($"Wybrano pionka z ID: {id} dla dru¿yny: {teamId}");
                    return;
                }
                else
                {
                    Debug.Log($"Nie mo¿na wybraæ pionka z ID: {id} - nale¿y do innej dru¿yny.");
                }
            }
        }
        Debug.Log("Nie znaleziono pionka z danym ID lub pionek nale¿y do innej dru¿yny.");
    }

    // Pomocnicza metoda, która sprawdza, czy dwa prostok¹tne obszary s¹ od siebie oddalone o co najmniej minDistance
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

        // 1. Ustaw bazow¹ wysokoœæ mapy na 5
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tileHeights[x, y] = 5f;
            }
        }

        // Parametry wspólne
        int plateauMinSize = 4;  // Plateau musi mieæ co najmniej 4x4 pola
        int borderOffset = 2;    // Plateau musi byæ co najmniej 2 pola od krawêdzi mapy
        int minDistance = 2;     // Plateau musz¹ byæ od siebie oddalone co najmniej o 2 pola

        // 2. Generowanie wypuk³ego plateau (górka)
        int p1Width = UnityEngine.Random.Range(plateauMinSize, tileCountX / 2);
        int p1Height = UnityEngine.Random.Range(plateauMinSize, tileCountY / 2);
        int p1StartX = UnityEngine.Random.Range(borderOffset, tileCountX - p1Width - borderOffset);
        int p1StartY = UnityEngine.Random.Range(borderOffset, tileCountY - p1Height - borderOffset);
        float p1HeightValue = 6f; // Wypuk³e plateau: baza (5) + 1 = 6

        // Nadpisujemy obszar wypuk³ego plateau
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
        // 3. Generowanie wklês³ego plateau (do³ek)
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
            p2HeightValue = 4f; // Wklês³e plateau: baza (5) - 1 = 4

            if (!RectanglesTooClose(p1StartX, p1StartY, p1Width, p1Height, p2StartX, p2StartY, p2Width, p2Height, minDistance))
            {
                validP2 = true;
            }
            attempts++;
        } while (!validP2 && attempts < 100);

        // Nadpisujemy obszar wklês³ego plateau
        for (int x = p2StartX; x < p2StartX + p2Width; x++)
        {
            for (int y = p2StartY; y < p2StartY + p2Height; y++)
            {
                tileHeights[x, y] = p2HeightValue;
            }
        }

        // 4. Generowanie kafelków wed³ug wartoœci w tablicy tileHeights
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {

                int heightLevel = Mathf.RoundToInt(tileHeights[x, y]);
                tiles[x, y] = GenerateSingleTile(tileSize, x, y, heightLevel);
                Debug.Log("Wysokoœæ: " + heightLevel);
            }
        }

        // 5. Generowanie przejœæ (schody/rampy) dla obu plateau:
        // Dla wypuk³ego plateau (p1) u¿ywamy schodów (isHole == false)
        // Dla wklês³ego plateau (p2) u¿ywamy ramp (isHole == true)
        PlacePlateauTransitions(p1StartX, p1StartY, p1Width, p1Height, false);
        PlacePlateauTransitions(p2StartX, p2StartY, p2Width, p2Height, true);
        PlaceCaveEntranceInHole(p2StartX, p2StartY, p2Width, p2Height);
    }

    private void PlaceCaveEntranceInHole(int startX, int startY, int width, int height)
    {
        int centerX = startX + width / 2;
        int centerY = startY + height / 2;
        float tileHeight = tileHeights[centerX, centerY];
        Vector3 position = new Vector3(centerX * tileSize, tileHeight + 0.5f, centerY * tileSize);
        Debug.Log("Placing cave entrance at: " + position);
        Instantiate(caveEntrancePrefab, position, Quaternion.identity);
    }



    private GameObject GenerateSingleTile(float tileSize, int x, int y, int heightLevel)
    {
        GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tileObject.transform.parent = transform;
        tileObject.transform.localScale = new Vector3(tileSize, tileSize, tileSize); // Zmiana na szeœcian
        tileObject.transform.position = new Vector3(x * tileSize, heightLevel * tileSize, y * tileSize); // Dopasowanie pozycji do rozmiaru szeœcianu
        tileObject.GetComponent<MeshRenderer>().material = tileMaterial; // Przypisanie materia³u do kafelka
        tileObject.layer = LayerMask.NameToLayer("Tile");

        // Generowanie filaru pod kafelkiem, jeœli jest na wy¿szej wysokoœci
        if (heightLevel > 0)
        {
            for (int h = 0; h < heightLevel; h++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.transform.parent = transform;
                pillar.transform.localScale = new Vector3(tileSize, tileSize, tileSize); // Ustaw rozmiar filaru na kafelek
                pillar.transform.position = new Vector3(x * tileSize, h * tileSize, y * tileSize); // Ustaw pozycjê filaru w odpowiednim miejscu
                pillar.GetComponent<MeshRenderer>().material = pillarMaterial; // Przypisanie materia³u do filaru
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
    private void PlacePlateauTransitions(int startX, int startY, int width, int height, bool isHole)
    {
        // Lewa krawêdŸ plateau
        for (int y = startY; y < startY + height; y++)
        {
            if (startX - 1 >= 0)
            {
                float hPlateau = tileHeights[startX, y];
                float hAdjacent = tileHeights[startX - 1, y];
                float diff = Mathf.Abs(hPlateau - hAdjacent);
                if (diff > 0.1f)
                {
                    Vector3 posPlateau = tiles[startX, y].transform.position;
                    Vector3 posAdjacent = tiles[startX - 1, y].transform.position;
                    Vector3 transitionPos = (posPlateau + posAdjacent) / 2f;

                    // Obliczamy kierunek na podstawie ró¿nicy, zerujemy Y:
                    Vector3 direction = posPlateau - posAdjacent;
                    direction.y = 0f;
                    Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);
                    // Dla lewej krawêdzi dodajemy dodatkow¹ rotacjê zale¿nie od typu:
                    if (isHole)
                        rot *= Quaternion.Euler(0, 180, 0);
                    else
                        rot *= Quaternion.Euler(0, 0, 0);

                    // Instancjujemy prefabrykat
                    Instantiate(isHole ? rampPrefab : stairsPrefab, transitionPos, rot);
                }
            }
        }

        // Prawa krawêdŸ plateau
        for (int y = startY; y < startY + height; y++)
        {
            if (startX + width < Tile_Count_X)
            {
                int rightTileX = startX + width - 1;
                float hPlateau = tileHeights[rightTileX, y];
                float hAdjacent = tileHeights[rightTileX + 1, y];
                float diff = Mathf.Abs(hPlateau - hAdjacent);
                if (diff > 0.1f)
                {
                    Vector3 posPlateau = tiles[rightTileX, y].transform.position;
                    Vector3 posAdjacent = tiles[rightTileX + 1, y].transform.position;
                    Vector3 transitionPos = (posPlateau + posAdjacent) / 2f;

                    Vector3 direction = posAdjacent - posPlateau;
                    direction.y = 0f;
                    Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);
                    // Dla prawej krawêdzi – inna korekta:
                    if (isHole)
                        rot *= Quaternion.Euler(0, 0, 0);
                    else
                        rot *= Quaternion.Euler(0, 180, 0);

                    Instantiate(isHole ? rampPrefab : stairsPrefab, transitionPos, rot);
                }
            }
        }
    }


    private void SpawnTransferredPieces()
    {
        // Pobieramy dane pionków zapisane w GameManagerze
        List<PieceData> piecesData = GameManager.Instance.transferredPieces;
        if (piecesData.Count == 0)
        {
            Debug.Log("Brak przekazanych danych pionków.");
            return;
        }

        // Inicjalizujemy tablicê pionków
        chessPieces = new ChessPieces[Tile_Count_X, Tile_Count_Y];
        int index = 0;
        foreach (PieceData data in piecesData)
        {
            // Tworzymy now¹ instancjê pionka
            ChessPieces cp = SpawnSinglePiece(data.type, data.team, data.id);
            if (cp == null)
            {
                Debug.LogError("SpawnTransferredPieces: cp jest null dla PieceData z ID " + data.id);
                continue;
            }
            cp.Init(data.type, data.team, data.id);
            cp.health = data.health;
            cp.maxHealth = data.maxHealth;
            cp.movementRange = data.movementRange;
            cp.maxMovementRange = data.maxMovementRange;
            cp.attack = data.attack;
            cp.attackRange = data.attackRange;
            cp.attackCost = data.attackCost;
            cp.groundOffset = data.groundOffset;
            cp.hasPassiveAbility = data.hasPassiveAbility;
            cp.visionRange = data.visionRange;

            // Ustal nowe wspó³rzêdne pionka.
            // Przyk³adowo: ustaw pionki obok portalu, wykorzystuj¹c œrodek do³ka (p2) jako bazê.
            // Mo¿esz u¿yæ wzoru: (p2StartX + p2Width/2, p2StartY + p2Height/2) jako punktu centralnego,
            // a nastêpnie dla kolejnych pionków dodawaæ offset.
            int offsetX = index % 3; // 3 pionki w jednym wierszu
            int offsetY = index / 3; // kolejne wiersze
            int newTileX = Mathf.Clamp((p2StartX + p2Width / 2) + offsetX, 0, Tile_Count_X - 1);
            int newTileY = Mathf.Clamp((p2StartY + p2Height / 2) + offsetY, 0, Tile_Count_Y - 1);
            cp.currentX = newTileX;
            cp.currentY = newTileY;

            if (cp == null)
            {
                Debug.LogError("SpawnTransferredPieces: cp jest null dla rekordu z ID " + data.id);
                continue;
            }
            // Ustaw pionka na planszy, wywo³uj¹c metodê PositionSinglePiece
            PositionSinglePiece(newTileX, newTileY, true);
            chessPieces[newTileX, newTileY] = cp;

            index++;
        }
        // Czyœcimy listê, aby przy kolejnym przejœciu nie tworzyæ duplikatów
        piecesData.Clear();
    }


    private ChessPieces SpawnSinglePiece(ChessPieceType type, int team, int id)
    {
        int index = (int)type - 1;
        if (index < 0 || index >= prefabs.Length)
        {
            Debug.LogError($"Nieprawid³owy indeks dla typu {type}: {index}. Upewnij siê, ¿e tablica prefabs ma odpowiedni¹ liczbê elementów.");
            return null;
        }

        ChessPieces cp = Instantiate(prefabs[index], transform).GetComponent<ChessPieces>();
        if (cp == null)
        {
            Debug.LogError("Nie uda³o siê uzyskaæ komponentu ChessPieces z instancji prefabrykatów.");
            return null;
        }

        cp.Init(type, team, id);
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        if (groundOffsets.TryGetValue(type, out float offset))
            cp.groundOffset = offset;
        else
            cp.groundOffset = 0.5f;
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

    // Ustawia pionek tak, aby jego dolna krawêdŸ (bounding box) styka³a siê z kafelkiem.
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        if (chessPieces[x, y] == null)
        {
            Debug.LogError($"PositionSinglePiece: chessPieces[{x},{y}] jest null!");
            return;
        }

        ChessPieces piece = chessPieces[x, y];
        piece.currentX = x;
        piece.currentY = y;

        float tileHeight = tiles[x, y].transform.position.y;
        // Ustaw tymczasowo pozycjê, aby bounding box zosta³ obliczony w world space
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
            float off = tileHeight - minY;
            float smallLift = 0.5f;
            float finalY = off + smallLift;
            piece.transform.position = new Vector3(x * tileSize, finalY, y * tileSize);
            Debug.Log($"PositionSinglePiece: Piece {piece.name} at tile({x},{y}): tileHeight={tileHeight}, bounds.min.y={minY}, offset={off}, finalY={finalY}");
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
        // Resetuj mg³ê wojny (zakryj ca³¹ mapê)
        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {
                fogTiles[x, y].SetActive(true);
            }
        }

        // Ods³oñ obszar wokó³ wszystkich pionków z aktywnej dru¿yny
        foreach (var piece in chessPieces)
        {
            if (piece != null && piece.team == currentTeam)
            {
                RevealArea(piece.currentX, piece.currentY, piece.visionRange); // Zakres widocznoœci: 3 pola
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
                // Ukryj pionek, jeœli znajduje siê w ukrytym obszarze mg³y
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

}
public class DangeonNode
{
    public int X, Y;
    public int G, H;
    public Node Parent;

    public int F => G + H;

    public DangeonNode(int x, int y)
    {
        X = x;
        Y = y;
    }
}