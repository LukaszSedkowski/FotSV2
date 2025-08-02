using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    private ChessBoard chessBoard;
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

    public List<Vector2Int> lightTiles = new List<Vector2Int>();
    public List<Vector2Int> darkTiles = new List<Vector2Int>();

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
        AddLightAndDarkTiles(10);
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
                /*  GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                  pillar.transform.parent = go.transform;
                  pillar.transform.localScale = new Vector3(1, height - 1f, 1);
                  pillar.transform.localPosition = new Vector3(0, -(height / 2), 0);
                  pillar.GetComponent<MeshRenderer>().material = pillarMaterial;
                */
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
        GenerateCombinedPillars();
    }
    private void GenerateCombinedPillars()
    {
        List<CombineInstance> combineInstances = new List<CombineInstance>();
        Mesh pillarMesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(GameObject.CreatePrimitive(PrimitiveType.Cube)); // usuwamy po u¿yciu

        for (int x = 0; x < Tile_Count_X; x++)
        {
            for (int y = 0; y < Tile_Count_Y; y++)
            {
                float height = tileHeights[x, y];
                if (height <= 1f) continue; // nie generuj filara, jeœli jest za niski

                Vector3 scale = new Vector3(1, height - 1f, 1);
                Vector3 position = new Vector3(x * tileSize, (height - 1f) / 2, y * tileSize); // pozycja na œrodku filara

                Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.identity, scale);

                combineInstances.Add(new CombineInstance
                {
                    mesh = pillarMesh,
                    transform = matrix
                });
            }
        }

        GameObject combinedPillars = new GameObject("Combined Tile Pillars");
        combinedPillars.transform.parent = transform;

        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combineInstances.ToArray());

        MeshFilter mf = combinedPillars.AddComponent<MeshFilter>();
        mf.mesh = combinedMesh;

        MeshRenderer mr = combinedPillars.AddComponent<MeshRenderer>();
        mr.material = pillarMaterial;

        // Opcjonalnie:
        // combinedPillars.AddComponent<MeshCollider>();
    }
    private void GenerateBorderPillars()
    {
        int tileCountX = tiles.GetLength(0);
        int tileCountY = tiles.GetLength(1);
        float pillarHeight = 3f;
        int borderWidth = 100;

        // Rozmiary ca³ego "obszaru obramowania"
        float totalWidthX = (tileCountX + 2 * borderWidth) * tileSize;
        float totalWidthY = (tileCountY + 2 * borderWidth) * tileSize;

        // Stwórz nowy GameObject, który bêdzie obramowaniem
        GameObject borderBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        borderBlock.name = "Border Pillars Block";
        borderBlock.transform.parent = transform;

        // Skaluj go tak, ¿eby mia³ szerokoœæ i d³ugoœæ ca³ej mapy plus border
        // Wysokoœæ na pillarHeight
        borderBlock.transform.localScale = new Vector3(totalWidthX, pillarHeight, totalWidthY);

        // Ustaw pozycjê na œrodek ca³ego tego obszaru, ale na po³owê wysokoœci filarów
        borderBlock.transform.position = new Vector3(
            (tileCountX / 2f) * tileSize,
            pillarHeight / 2f,
            (tileCountY / 2f) * tileSize);

        // Ustaw materia³ filarów
        MeshRenderer mr = borderBlock.GetComponent<MeshRenderer>();
        mr.material = pillarMaterial;

        // Opcjonalnie usuñ collider jeœli niepotrzebny:
        // Destroy(borderBlock.GetComponent<BoxCollider>());
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
        for (int i = 1; i < number - 1; i++)
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
                Vector3 position = GetTileCenter2(x, y);
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
                    // Ustal wysokoœæ p³otu
                    float fenceHeight = 6f;
                    Vector3 fencePosition = new Vector3(x * tileSize, fenceHeight, y * tileSize);

                    // Stwórz p³ot
                    GameObject fencePrefab = fencePrefabs[UnityEngine.Random.Range(0, fencePrefabs.Length)];
                    GameObject fence = Instantiate(fencePrefab, fencePosition, Quaternion.identity, transform);

                    // Ustaw rotacjê
                    if (x == -1) fence.transform.rotation = Quaternion.Euler(0, 90, 0);
                    else if (x == tileCountX) fence.transform.rotation = Quaternion.Euler(0, -90, 0);
                    else if (y == -1) fence.transform.rotation = Quaternion.Euler(0, 0, 0);
                    else if (y == tileCountY) fence.transform.rotation = Quaternion.Euler(0, 180, 0);

                    // === Dodaj filar pod p³ot ===
                    float pillarHeight = fenceHeight; // od 0 do wysokoœci p³otu
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pillar.transform.parent = fence.transform;
                    pillar.transform.localScale = new Vector3(5f, 25f, 5f);
                    pillar.transform.position = new Vector3(x * tileSize, pillarHeight / 2f, y * tileSize);
                    pillar.GetComponent<MeshRenderer>().material = pillarMaterial;

                    // Opcjonalnie usuñ collider, jeœli niepotrzebny
                    Destroy(pillar.GetComponent<Collider>());
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
    public Vector3 GetTileCenter2(int x, int y)
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
    private void AddLightAndDarkTiles(int count)
    {
        int tries = 0;
        int maxTries = 1000;

        while (lightTiles.Count < count && tries < maxTries)
        {
            int x = Random.Range(0, Tile_Count_X);
            int y = Random.Range(0, Tile_Count_Y);
            Vector2Int pos = new Vector2Int(x, y);

            if (!lightTiles.Contains(pos) && !darkTiles.Contains(pos) && !obstacles[x, y])
            {
                lightTiles.Add(pos);
                GameObject tile = tiles[x, y];
                //tile.tag = "LightTile";

                var renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = Color.cyan; // Jasne pole
            }

            tries++;
        }

        tries = 0;
        while (darkTiles.Count < count && tries < maxTries)
        {
            int x = Random.Range(0, Tile_Count_X);
            int y = Random.Range(0, Tile_Count_Y);
            Vector2Int pos = new Vector2Int(x, y);

            if (!darkTiles.Contains(pos) && !lightTiles.Contains(pos) && !obstacles[x, y])
            {
                darkTiles.Add(pos);
                GameObject tile = tiles[x, y];
                //tile.tag = "DarkTile";

                var renderer = tile.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material.color = Color.magenta; // Mroczne pole
            }

            tries++;
        }
    }

}
