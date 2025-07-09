using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    private static TileManager _instance;
    public static TileManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<TileManager>();
            return _instance;
        }
    }

    [Header("Art")]
    [SerializeField] private Material[] tileMaterials; // Materia³ dla kafelków
    [SerializeField] private Material hoverMaterial; // Materia³ do podœwietlenia
    [SerializeField] private Material pillarMaterial; // Materia³ dla filarów
    [SerializeField] private GameObject[] tilePrefabs;
    [SerializeField] private GameObject[] gameEdgePrefabs;
    [SerializeField] private GameObject[] fencePrefabs;
    [SerializeField] private GameObject obstaclePrefabRock;


    [SerializeField] private List<GameObject> hideoutPrefabs;


    public GameObject[,] tiles;
    public float[,] tileHeights;
    public float tileSize = 1.0f;
    public bool[,] obstacles;
    private List<Vector2Int> hideoutPositions = new List<Vector2Int>();

    public const int Tile_Count_X = 40;
    public const int Tile_Count_Y = 40;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        _instance = this;
        InitializeMap();
    }
    public void InitializeMap()
    {
        tiles = new GameObject[Tile_Count_X, Tile_Count_Y];
        tileHeights = new float[Tile_Count_X, Tile_Count_Y];
        obstacles = new bool[Tile_Count_X, Tile_Count_Y];
        hideoutPositions = new List<Vector2Int>();

        GenerateAllTiles(tileSize, Tile_Count_X, Tile_Count_Y);
        InitializeTileHeights();
        AddRandomObstacles(10);
        AddRandomHideouts(10);
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
        // Zmieniamy ten fragment, który tworzy kafelek:
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                float height = tileHeights[x, y];
                GameObject tilePrefabToUse = tilePrefabs[0]; // domyœlnie p³aski
                float heightOffset = 0f;

                // Czy ten kafelek ma byæ pochylni¹?
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
                    heightOffset = 1f; // bo schody maj¹ wy¿szy koniec
                }

                // Pozycja kafelka
                Vector3 pos = new Vector3(x * tileSize, height + heightOffset, y * tileSize);
                GameObject go = Instantiate(tilePrefabToUse, pos, Quaternion.identity, transform);
                go.name = $"Tile {x},{y}";
                tiles[x, y] = go;
                go.layer = LayerMask.NameToLayer("Tile");

                // Ustaw materia³
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

                // Obróæ schody w stronê wy¿szego kafelka
                if (isRamp)
                {
                    float angle = 0f;
                    if (rampDirection == Vector3.left) angle = -90f;
                    else if (rampDirection == Vector3.right) angle = 90f;
                    else if (rampDirection == Vector3.back) angle = 180f;
                    // forward (czyli domyœlnie) to 0°

                    go.transform.rotation = Quaternion.Euler(0, angle, 0);
                }
            }
        }



        GenerateBorderPillars();
        AddFenceAroundMap(tileCountX, tileCountY);

    }
    private void GenerateBorderPillars()
    {
        int tileCountX = tiles.GetLength(0);
        int tileCountY = tiles.GetLength(1);
        float pillarHeight = 5.0f; // dopasuj do swojego prefab'u
        int borderWidth = 35;

        for (int x = -borderWidth; x < tileCountX + borderWidth; x++)
        {
            for (int y = -borderWidth; y < tileCountY + borderWidth; y++)
            {
                // sprawdzamy, czy kafelek znajduje siê *poza* plansz¹
                bool isOutside = x < 0 || y < 0 || x >= tileCountX || y >= tileCountY;

                // sprawdzamy, czy jesteœmy w obszarze nale¿¹cym do granicy
                bool isInBorderArea = x < 0 + borderWidth || y < 0 + borderWidth ||
                                      x >= tileCountX - borderWidth || y >= tileCountY - borderWidth;

                if (isOutside && isInBorderArea)
                {
                    Vector3 pillarPosition = new Vector3(x * tileSize, pillarHeight, y * tileSize);
                    if (gameEdgePrefabs.Length > 0 && gameEdgePrefabs[0] != null)
                    {
                        Instantiate(gameEdgePrefabs[0], pillarPosition, Quaternion.identity, transform);
                    }
                }
            }
        }
    }
    /*private GameObject GenerateSingleTile(float tileSize, int x, int y, int heightLevel)
    {
        int prefabIndex = (x + y) % tilePrefabs.Length; // lub inny sposób wyboru
        GameObject tileObject = Instantiate(tilePrefabs[prefabIndex], transform);
        tileObject.transform.parent = transform;
        tileObject.transform.localScale = new Vector3(tileSize, tileSize, tileSize); // Zmiana na szeœcian
        tileObject.transform.position = new Vector3(x * tileSize, heightLevel * tileSize, y * tileSize); // Dopasowanie pozycji do rozmiaru szeœcianu
        int materialIndex = UnityEngine.Random.Range(0, tileMaterials.Length); // logiku od textur i tego jak siê generuj¹
        tileObject.GetComponent<MeshRenderer>().material = tileMaterials[materialIndex]; // Przypisanie materia³u do kafelka
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
    }*/
    private void AddRandomObstacles(int number)
    {
        for (int i = 1; i < number-1; i++)
        {
            int x = UnityEngine.Random.Range(0, Tile_Count_X);
            int y = UnityEngine.Random.Range(0, Tile_Count_Y);

            // Sprawdzamy, czy pole jest puste i nie jest przeszkod¹
            if (/*chessPieces[x, y] == null &&*/ !obstacles[x, y])
            {
                obstacles[x, y] = true;

                // Pobieramy wysokoœæ dla tego kafelka
                float tileHeight = tileHeights[x, y];

                // Uzyskujemy pozycjê kafelka i ustawiamy wysokoœæ przeszkody
                Vector3 position = GetTileCenter(x, y);
                position.y = tileHeight + 0.8f; // Ustawienie wysokoœci przeszkody zgodnie z wysokoœci¹ kafelka

                // Debugowanie pozycji
                Debug.Log("Placing obstacle at: " + position);

                // Dodajemy przeszkodê w tym miejscu
                Instantiate(obstaclePrefabRock, position, Quaternion.identity);
            }
        }
    }
    private void AddRandomHideouts(int count)
    {
        int tries = 0;
        int maxTries = 1000;

        while (hideoutPositions.Count < count && tries < maxTries)
        {
            int x = UnityEngine.Random.Range(0, Tile_Count_X);
            int y = UnityEngine.Random.Range(0, Tile_Count_Y);

            Vector2Int pos = new Vector2Int(x, y);

            // SprawdŸ czy pole nie jest ju¿ kryjówk¹ i nie jest przeszkod¹
            if (!hideoutPositions.Contains(pos) && !obstacles[x, y])
            {
                hideoutPositions.Add(pos);

                // Ustaw tag "Hideout" na kafelku
                tiles[x, y].tag = "Hideout";

                // Zmieñ kolor kafelka na zielony

                // Losuj prefab do instancji
                int prefabIndex = UnityEngine.Random.Range(0, hideoutPrefabs.Count);
                GameObject prefabToSpawn = hideoutPrefabs[prefabIndex];

                // Pobierz pozycjê kafelka
                Vector3 spawnPosition = tiles[x, y].transform.position;

                // Opcjonalnie mo¿esz podnieœæ obiekt nieco ponad kafelek (np. y + 0.5f)
                spawnPosition.y += 0.5f;

                // Stwórz instancjê obiektu w scenie
                Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            }
            tries++;
        }

    }
    private void AddFenceAroundMap(int tileCountX, int tileCountY)
    {
        for (int x = -1; x <= tileCountX; x++)
        {
            for (int y = -1; y <= tileCountY; y++)
            {
                bool isOuterEdge =
                    (x == -1 || x == tileCountX || y == -1 || y == tileCountY) &&
                    !(x < -1 || x > tileCountX || y < -1 || y > tileCountY);

                if (isOuterEdge)
                {
                    // Ustal wysokoœæ na podstawie s¹siaduj¹cego kafelka w planszy (jeœli istnieje)
                    int innerX = Mathf.Clamp(x, 0, tileCountX - 1);
                    int innerY = Mathf.Clamp(y, 0, tileCountY - 1);
                    float height = 6f;

                    Vector3 position = new Vector3(x * tileSize, height, y * tileSize);

                    // Wybierz losowy prefab p³otka
                    GameObject fencePrefab = fencePrefabs[UnityEngine.Random.Range(0, fencePrefabs.Length)];
                    GameObject fence = Instantiate(fencePrefab, position, Quaternion.identity, transform);

                    // Opcjonalne: obróæ w zale¿noœci od krawêdzi
                    if (x == -1) fence.transform.rotation = Quaternion.Euler(0, 90, 0);
                    else if (x == tileCountX) fence.transform.rotation = Quaternion.Euler(0, -90, 0);
                    else if (y == -1) fence.transform.rotation = Quaternion.Euler(0, 0, 0);
                    else if (y == tileCountY) fence.transform.rotation = Quaternion.Euler(0, 180, 0);
                }
            }
        }
    }
    private bool RectanglesTooClose(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh, int minDistance)
    {
        int aLeft = ax - minDistance;
        int aRight = ax + aw + minDistance;
        int aTop = ay - minDistance;
        int aBottom = ay + ah + minDistance;

        return (bx < aRight && (bx + bw) > aLeft && by < aBottom && (by + bh) > aTop);
    }
    /*public Vector3 GetTileCenter(int x, int y, ChessPieces movingPiece)
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
    }*/
    public Vector3 GetTileCenter(int x, int y)
    {
        float tileHeight = tiles[x, y].transform.position.y;
        return new Vector3(x * tileSize, tileHeight, y * tileSize);
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
}
