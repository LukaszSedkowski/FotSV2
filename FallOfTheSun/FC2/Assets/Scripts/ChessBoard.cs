using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class ChessBoard : MonoBehaviour
{
    [Header("Art")]
    [SerializeField] private Material tileMaterial; // Materiał dla kafelków
    [SerializeField] private Material hoverMaterial; // Materiał do podświetlenia
    [SerializeField] private Material pillarMaterial; // Materiał dla filarów




    [Header("Pref")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;
    [SerializeField] private GameObject obstaclePrefab;

    [Header("HUD")]
    [SerializeField] private TeamPanel CurrentPiecePanel;

    private bool[,] obstacles;

    private ChessPieces[,] chessPieces;
    private ChessPieces currentlyDragging;
    private bool[] teamIsActive;
    private Color originalColor;
    private const int Tile_Count_X = 20;
    private const int Tile_Count_Y = 20;
    private float tileSize = 1.0f;

    private bool[,] highlightedTiles;

    private GameObject[,] tiles;
    private float[,] tileHeights;
    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private List<Vector2Int> highlightedTilesList = new List<Vector2Int>();
    private int currentTeam = 0; // Aktualna drużyna (zaczynamy od drużyny 0)
    private int numberOfTeams; // Przykładowo, ustawiamy na 4 drużyny

    private void Awake()
    {

        numberOfTeams = teamMaterials.Length;
        GenerateAllTiles(tileSize, Tile_Count_X, Tile_Count_Y);
        InitializeTileHeights();
        SpawnAllPieces();
        PositionAllPieces();
        obstacles = new bool[Tile_Count_X, Tile_Count_Y]; // Inicjalizacja tablicy przeszkód

        // Dodajemy przeszkody w losowych miejscach (przykład)
        AddRandomObstacles(5);
        // Wybór pionka z ID równym 1 na początku gry
        SelectPieceById(1, currentTeam);

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
                        currentlyDragging.transform.position = GetTileCenter(previousPosition.x, previousPosition.y);
                    }
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
        if (Input.GetKeyDown(KeyCode.Q))
        {
            int attempts = numberOfTeams; // Ograniczenie liczby prób na wypadek braku pionków u wszystkich drużyn
            do
            {
                currentTeam = (currentTeam + 1) % numberOfTeams; // Przełącz na następną drużynę
                attempts--;

                CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);

                if (!DoesTeamHavePieces(currentTeam))
                {
                    Debug.Log("Drużyna " + (currentTeam + 1) + " nie ma pionków. Pomijam.");
                }
            } while (!DoesTeamHavePieces(currentTeam) && attempts > 0);

            currentlyDragging = null; // Anulowanie wyboru po zmianie tury

            // Resetowanie punktów ruchu dla drużyny, która skończyła turę
            ResetMovementRangeForTeam(currentTeam);


            Debug.Log("Tura drużyny " + (currentTeam + 1));

            SelectPieceWithLowestId(currentTeam); // Wybieranie pionka z najniższym ID

            CurrentPiecePanel.CurrentPiecesSetPanel(currentlyDragging);

            if (currentlyDragging != null)
            {
                Camera.main.GetComponent<CameraController>().SetTarget(currentlyDragging.transform);
                HighlightPossibleMoves(currentlyDragging);
            }
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
            int x = Random.Range(0, Tile_Count_X);
            int y = Random.Range(0, Tile_Count_Y);

            // Sprawdzamy, czy pole jest puste i nie jest przeszkodą
            if (chessPieces[x, y] == null && !obstacles[x, y])
            {
                obstacles[x, y] = true;

                // Pobieramy wysokość dla tego kafelka
                float tileHeight = tileHeights[x, y];

                // Uzyskujemy pozycję kafelka i ustawiamy wysokość przeszkody
                Vector3 position = GetTileCenter(x, y);
                position.y = tileHeight + 1.0f; // Ustawienie wysokości przeszkody zgodnie z wysokością kafelka

                // Debugowanie pozycji
                Debug.Log("Placing obstacle at: " + position);

                // Dodajemy przeszkodę w tym miejscu
                Instantiate(obstaclePrefab, position, Quaternion.identity);
            }
        }
    }



    private void HighlightPossibleMoves(ChessPieces cp)
    {
        ResetTileColors(); // Reset kolorów przed podświetleniem nowych
        highlightedTilesList.Clear(); // Wyczyść poprzednią listę

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

    private void AttackEnemyPiece(int targetX, int targetY)
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




    private bool FindPath(ChessPieces cp, int startX, int startY, int targetX, int targetY, int remainingMoves, bool[,] visited, ref int shortestCost, out List<Vector2Int> path)
    {
        Queue<(int x, int y, int remainingMoves, int cost, List<Vector2Int> currentPath)> queue = new Queue<(int, int, int, int, List<Vector2Int>)>();
        queue.Enqueue((startX, startY, remainingMoves, 0, new List<Vector2Int> { new Vector2Int(startX, startY) }));
        visited[startX, startY] = true;

        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        path = null;
        shortestCost = int.MaxValue;

        while (queue.Count > 0)
        {
            var (x, y, remaining, cost, currentPath) = queue.Dequeue();

            if (x == targetX && y == targetY)
            {
                if (cost < shortestCost)
                {
                    shortestCost = cost;
                    path = new List<Vector2Int>(currentPath);
                }
                return true;
            }

            for (int i = 0; i < 4; i++)
            {
                int newX = x + dx[i];
                int newY = y + dy[i];

                if (newX >= 0 && newY >= 0 && newX < chessPieces.GetLength(0) && newY < chessPieces.GetLength(1))
                {
                    if (!visited[newX, newY] && chessPieces[newX, newY] == null && !obstacles[newX, newY])
                    {
                        float currentHeight = tiles[x, y].transform.position.y;
                        float nextHeight = tiles[newX, newY].transform.position.y;
                        int heightDifference = Mathf.Abs(Mathf.RoundToInt(currentHeight - nextHeight));
                        int movementCost = 1 + Mathf.Min(heightDifference, 2);

                        if (remaining >= movementCost)
                        {
                            visited[newX, newY] = true;
                            var newPath = new List<Vector2Int>(currentPath) { new Vector2Int(newX, newY) };
                            queue.Enqueue((newX, newY, remaining - movementCost, cost + movementCost, newPath));
                        }
                    }
                }
            }
        }

        return false;
    }






    private bool MoveTo(ChessPieces cp, int targetX, int targetY)
    {
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Tworzenie tablicy odwiedzonych pól i zmiennej dla najkrótszego kosztu
        bool[,] visited = new bool[chessPieces.GetLength(0), chessPieces.GetLength(1)];
        int shortestCost = int.MaxValue;

        List<Vector2Int> path;
        // Sprawdzenie, czy istnieje najkrótsza ścieżka do celu
        if (!FindPath(cp, previousPosition.x, previousPosition.y, targetX, targetY, cp.movementRange, visited, ref shortestCost, out path))
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
        StartCoroutine(MovePieceAlongPath(cp, path));

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

    private IEnumerator MovePieceAlongPath(ChessPieces cp, List<Vector2Int> path)
    {
        float moveDuration = 0.5f; // Możesz dostosować czas trwania ruchu
        Vector3 startPosition = cp.transform.position;

        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int currentPos = path[i - 1];
            Vector2Int nextPos = path[i];

            // Oblicz pozycję docelową
            Vector3 targetPosition = GetTileCenter(nextPos.x, nextPos.y);
            float elapsedTime = 0f;

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
        Vector3 targetPosition = GetTileCenter(targetPos.x, targetPos.y); // Funkcja, która zwraca środek kafelka

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


    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[tileCountX, tileCountY];
        tileHeights = new float[tileCountX, tileCountY]; // Tablica przechowująca wysokości kafelków

        // Losowe położenie płaskowyżu
        int plateauHeight = tileCountY / 4; // Wysokość płaskowyżu
        int plateauStartX = Random.Range(0, tileCountX - tileCountX / 2); // Losowe położenie na osi X
        int plateauStartY = Random.Range(0, tileCountY - plateauHeight); // Losowe położenie na osi Y
        float plateauHeightValue = Random.Range(1, 3); // Wysokość płaskowyżu

        // Wskaźniki do kontrolowania szerokości płaskowyżu w poziomie
        int plateauWidth = plateauHeight + Random.Range(0, tileCountX / 2); // Szerokość płaskowyżu

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                // Obliczanie szerokości płaskowyżu w zależności od pozycji Y (aby uzyskać efekt schodzenia)
                float distanceFromPlateau = Mathf.Abs(y - plateauStartY); // Odległość od centralnej linii płaskowyżu

                // Jeśli kafelek jest w obrębie płaskowyżu
                if (x >= plateauStartX && x < plateauStartX + plateauWidth &&
                    y >= plateauStartY && y < plateauStartY + plateauHeight)
                {
                    // Generowanie równego terenu dla płaskowyżu
                    tileHeights[x, y] = plateauHeightValue;
                }
                else
                {
                    // Generowanie spadków, kraterów lub schodków wokół terenu
                    // Zmieniamy wysokość w zależności od odległości od płaskowyżu
                    float distanceToPlateau = Mathf.Min(
                        Mathf.Abs(x - plateauStartX), Mathf.Abs((plateauStartX + plateauWidth - 1) - x),
                        Mathf.Abs(y - plateauStartY), Mathf.Abs((plateauStartY + plateauHeight - 1) - y)
                    );

                    if (distanceToPlateau == 1)
                    {
                        // Schodki o stałej różnicy wysokości
                        tileHeights[x, y] = plateauHeightValue - 1;
                    }
                    else if (distanceToPlateau == 2)
                    {
                        // Spadek lub krater o losowej różnicy wysokości
                        tileHeights[x, y] = plateauHeightValue - Random.Range(1, 2);
                    }
                    else
                    {
                        // Zmniejszająca się wysokość w miarę oddalania się od płaskowyżu
                        tileHeights[x, y] = plateauHeightValue - Mathf.Min(distanceFromPlateau, Random.Range(0, 2));
                    }
                }

                // Generowanie pojedynczego kafelka
                tiles[x, y] = GenerateSingleTile(tileSize, x, y, (int)tileHeights[x, y]);
            }
        }
    }





    private GameObject GenerateSingleTile(float tileSize, int x, int y, int heightLevel)
    {
        GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tileObject.transform.parent = transform;
        tileObject.transform.localScale = new Vector3(tileSize, tileSize, tileSize); // Zmiana na sześcian
        tileObject.transform.position = new Vector3(x * tileSize, heightLevel * tileSize, y * tileSize); // Dopasowanie pozycji do rozmiaru sześcianu
        tileObject.GetComponent<MeshRenderer>().material = tileMaterial; // Przypisanie materiału do kafelka
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
        int i = 0, team =0;
        // Przykładowa konfiguracja pionków
        /*chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Hunter, whiteTeam, whiteId++);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam, blackId++);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Priestess, whiteTeam, whiteId++);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Dog, blackTeam, blackId++);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Ogre, redTeam, redId++);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Skeleton, redTeam, redId++);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Vampir, blueTeam, blueId++);
        chessPieces[7, 1] = SpawnSinglePiece(ChessPieceType.Werewolf, blueTeam, blueId++);*/
        // Możesz dodać więcej pionków w podobny sposób


        foreach (var pieces in GameMenu.Instance.selectedCharacters) 
        {
            int pieceID = 1;
        foreach(var piece in pieces)
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

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;

        float tileHeight = tiles[x, y].transform.position.y; // Dopasowanie do wysokości kafelka
        float pieceHeight = tileHeight + 1.5f; // Ustawienie wysokości pionka na kafelku
        chessPieces[x, y].transform.position = new Vector3(x * tileSize, pieceHeight, y * tileSize);

        // Ustawienie rotacji pionka na 0
        chessPieces[x, y].transform.rotation = Quaternion.identity; // Pionki stoją prosto
    }


    private Vector3 GetTileCenter(int x, int y)
    {
        float tileHeight = tiles[x, y].transform.position.y;
        return new Vector3(x * tileSize, tileHeight + 1.5f, y * tileSize);
    }
}
