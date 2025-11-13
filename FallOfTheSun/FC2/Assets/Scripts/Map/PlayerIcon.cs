using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    private float minSpeed = 1f;
    private float maxSpeed = 10f;

    private Queue<Waypoint> pathQueue = new Queue<Waypoint>();
    private bool isMoving = false;
    private bool isFrozen = false;

    private Waypoint currentWaypoint;
    private Waypoint selectedWaypoint; // Wybrany waypoint

    [Header("UI")]
    public Text dayText;
    public GameObject movePanel;      // Panel z potwierdzeniem ruchu
    public Button moveButton;         // Przycisk "Idź"
    public GameObject CheckPanel;     // Panel po dotarciu (walka / akcja)
    public Button sceneChangeButton;  // Przycisk przejścia do walki
    public Button infoButton;         // Przycisk "info" o wrogach

    [Header("Scene names")]
    public string Map = "SampleScene"; // Nazwa sceny walki (ty zostawiłeś "SampleScene")

    [Header("Debug/Flow")]
    public DialogueManager dialogueManager;

    [Header("Selection")]
    public Waypoint startPoint;

    // Timer "dnia" na mapie
    private float timeMoving = 0f;

    // Losowy wybór postaci gracza (tymczasowo)
    private List<ChessPieceType> selectedCharacters;

    [SerializeField] private TMP_Text dayTextTMP;


    // UI z informacją o wrogach (opcjonalnie)
    public Text enemyInfoText;
    public Text autoEnemyInfoText;


    void Awake()
    {
        // Na wszelki wypadek – przy powrocie z walki często coś zostaje w paused
        Time.timeScale = 1f;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private bool Alive(Object o) => o != null; // UnityNull-safe

    private Waypoint FindClosestToPosition(Vector3 pos)
    {
        Waypoint[] wps = FindObjectsOfType<Waypoint>();
        Waypoint best = null;
        float bestD = float.PositiveInfinity;
        foreach (var wp in wps)
        {
            if (!Alive(wp)) continue;
            float d = Vector3.Distance(pos, wp.transform.position);
            if (d < bestD) { bestD = d; best = wp; }
        }
        return best;
    }

    private Waypoint FindClosestUnderMouse(float clickRange = 0.4f)
    {
        if (Camera.main == null) return null;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Waypoint[] wps = FindObjectsOfType<Waypoint>();
        Waypoint best = null;
        float bestD = float.PositiveInfinity;

        foreach (var wp in wps)
        {
            float d = Vector3.Distance(mousePos, wp.transform.position);
            if (d < bestD) { bestD = d; best = wp; }
        }
        return (best != null && bestD <= clickRange) ? best : null;
    }

    private void UpdateDayUI_Safe()
    {
        int day = (GameData.Instance != null) ? GameData.Instance.currentDay : 0;
        string txt = $"Dzień: {day}";

        if (dayTextTMP) dayTextTMP.text = txt;     // TMP preferowany
        if (dayText) dayText.text = txt;     // Legacy fallback
        if (!dayTextTMP && !dayText)
            Debug.LogError("[MAP] Brak DayText/DayTextTMP – przypnij w Inspektorze!");

    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        RebindUI();

        // UI dnia (obsługuje TMP i Legacy)
        UpdateDayUI_Safe();

        // Reset referencji po powrocie ze sceny walki
        selectedWaypoint = null;
        isMoving = false;
        pathQueue.Clear();

        // Ustal punkt startowy po pozycji gracza (gdy stary waypoint został zniszczony)
        currentWaypoint = FindClosestToPosition(transform.position);

        // Info + UI dnia
        DumpState("[OnSceneLoaded]");
        UpdateDayUI_Safe();

        // Nowy dzień, jeśli flaga po walce
        if (GameData.Instance != null && GameData.Instance.lastBattleJustEnded)
        {
            GameData.Instance.lastBattleJustEnded = false;
            OnNewDay();
            UpdateDayUI_Safe();
        }

        // ZAWSZE na starcie mapy schowaj panele
        if (sceneChangeButton != null) sceneChangeButton.gameObject.SetActive(false);
        if (CheckPanel != null) CheckPanel.SetActive(false);
        if (movePanel != null) movePanel.SetActive(false);  // ⬅️ DODANE
    }



    private void Start()
    {
        // A) upewnij się, że czas nie jest zatrzymany po walce
        Time.timeScale = 1f;
        UpdateDayUI_Safe();

        RebindUI();

        DumpState("[MAP][Start after Rebind]");

        // Losujemy 4 postaci gracza (tymczasowo)
        selectedCharacters = GetRandomCharacters();

        foreach (var character in selectedCharacters)
            Debug.Log("Wylosowano postać: " + character);

        // Początkowe ustawienie pozycji na starcie
        if (startPoint != null)
        {
            currentWaypoint = startPoint;
            transform.position = startPoint.transform.position;
        }
        else
        {
            currentWaypoint = FindClosestToPosition(transform.position); // <– pozycja gracza, NIE myszki
            if (currentWaypoint != null)
                transform.position = currentWaypoint.transform.position;
            else
                Debug.LogWarning("[MAP] Brak waypointów w scenie – currentWaypoint == null");
        }

        // UI: bezpieczne podpięcie
        if (movePanel != null) movePanel.SetActive(false);
        if (CheckPanel != null) CheckPanel.SetActive(false);

        if (sceneChangeButton != null)
        {
            sceneChangeButton.gameObject.SetActive(false);
            sceneChangeButton.onClick.AddListener(ChangeScene);
        }

        if (infoButton != null)
            infoButton.onClick.AddListener(ShowEnemiesAtSelectedWaypoint);

        // Start dialogu (opcjonalnie)
        StartCoroutine(DelayedDialogueStart());
    }

    private void RebindUI()
    {
        // === DAY TEXT / ENEMY INFO (DayTextTMP) ===
        if (dayText == null || dayTextTMP == null || enemyInfoText == null)
        {
            var go = GameObject.Find("DayTextTMP");
            if (go != null)
            {
                // legacy Text
                if (dayText == null)
                    dayText = go.GetComponent<Text>();

                // TMP_Text (jakbyś kiedyś zmienił komponent)
                if (dayTextTMP == null)
                    dayTextTMP = go.GetComponent<TMP_Text>();

                // w Twojej scenie EnemyInfoText = ten sam obiekt
                if (enemyInfoText == null)
                    enemyInfoText = go.GetComponent<Text>();
            }
        }

        // === AUTO ENEMY INFO (Text (Legacy) (1)) ===
        if (autoEnemyInfoText == null)
        {
            var go = GameObject.Find("Text (Legacy) (1)");
            if (go != null)
                autoEnemyInfoText = go.GetComponent<Text>();
        }

        // === START POINT (F) ===
        if (startPoint == null)
        {
            var go = GameObject.Find("F");   // Twój początkowy waypoint
            if (go != null)
                startPoint = go.GetComponent<Waypoint>();
        }

        // === RUN PANEL ===
        if (movePanel == null)
        {
            var go = GameObject.Find("RunPanel");
            if (go != null) movePanel = go;
        }

        if (moveButton == null)
        {
            var go = GameObject.Find("RunButton");
            if (go != null) moveButton = go.GetComponent<Button>();
        }

        // === START PANEL / CHECK PANEL ===
        if (CheckPanel == null)
        {
            var go = GameObject.Find("CheckPanel");
            if (go == null) go = GameObject.Find("StartPanel");
            if (go != null) CheckPanel = go;
        }

        if (sceneChangeButton == null)
        {
            var go = GameObject.Find("StartButton");
            if (go != null) sceneChangeButton = go.GetComponent<Button>();
        }

        // === INFO BUTTON ===
        if (infoButton == null)
        {
            var go = GameObject.Find("InfoButton");
            if (go != null) infoButton = go.GetComponent<Button>();
        }

        // === LISTENERY ===
        if (moveButton != null)
        {
            moveButton.onClick.RemoveListener(OnMoveConfirmed);
            moveButton.onClick.AddListener(OnMoveConfirmed);
        }

        if (sceneChangeButton != null)
        {
            sceneChangeButton.onClick.RemoveListener(ChangeScene);
            sceneChangeButton.onClick.AddListener(ChangeScene);
        }

        if (infoButton != null)
        {
            infoButton.onClick.RemoveAllListeners();
            infoButton.onClick.AddListener(ShowEnemiesAtSelectedWaypoint);
        }

        // === STAN POCZĄTKOWY UI ===
        if (movePanel != null) movePanel.SetActive(false);
        if (CheckPanel != null) CheckPanel.SetActive(false);
        if (sceneChangeButton != null) sceneChangeButton.gameObject.SetActive(false);

        DumpState("[MAP][RebindUI]");
    }



    private void DumpState(string tag)
    {
        Debug.Log($"{tag}\n" +
                  $"dayText={(dayText ? dayText.name : "NULL")}, " +
                  $"movePanel={(movePanel ? movePanel.name : "NULL")}, " +
                  $"CheckPanel={(CheckPanel ? CheckPanel.name : "NULL")}, " +
                  $"StartBtn={(sceneChangeButton ? sceneChangeButton.name : "NULL")}, " +
                  $"InfoBtn={(infoButton ? infoButton.name : "NULL")}");
    }

    private IEnumerator DelayedDialogueStart()
    {
        yield return null; // odczekaj 1 frame
        if (dialogueManager != null)
            dialogueManager.StartDialogueByName("a");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("[MAP][Hotkey] R pressed -> OnMoveConfirmed()");
            OnMoveConfirmed();
        }

        // Debug speed
        if (Input.GetKeyDown(KeyCode.Equals))
            speed = Mathf.Clamp(speed + 1f, minSpeed, maxSpeed);

        if (Input.GetKeyDown(KeyCode.Minus))
            speed = Mathf.Clamp(speed - 1f, minSpeed, maxSpeed);

        // Dialog przewijanie
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetMouseButtonDown(1))
        {
            if (dialogueManager != null)
                dialogueManager.NextLine();
        }

        // LPM – wybór celu ruchu (chyba że klik na UI)
        if (Input.GetMouseButtonDown(0))
        {
            if (!EventSystem.current || !EventSystem.current.IsPointerOverGameObject())
            {
                Waypoint target = FindClosestUnderMouse(); // <– pod kursorem
                if (target != null && target != currentWaypoint)
                {
                    selectedWaypoint = target;
                    Debug.Log($"[MAP] Selected wp name={target.name}");

                    if (movePanel == null)
                    {
                        Debug.LogError("[MAP] movePanel == NULL przy kliknięciu!");
                    }
                    else
                    {
                        Debug.Log($"[MAP] movePanel activeSelf={movePanel.activeSelf}, inHierarchy={movePanel.activeInHierarchy}, parentActive={movePanel.transform.parent.gameObject.activeInHierarchy}");
                        movePanel.SetActive(true);
                    }
                }
                else
                {
                    if (movePanel) movePanel.SetActive(false);
                }
            }
        }

        // Spacja – pauza/ruszanie
        if (Input.GetKeyDown(KeyCode.Space))
        {
            isFrozen = !isFrozen;
            isMoving = !isFrozen;
        }

        // Ruch po kolejce
        if (isMoving && pathQueue.Count > 0)
            MoveToNextWaypoint();

        // "Zegar" dnia – liczy tylko gdy poruszamy się
        if (isMoving)
        {
            timeMoving += Time.deltaTime;
            if (timeMoving >= 5f)
            {
                timeMoving = 0f;

                if (GameData.Instance != null)
                {
                    GameData.Instance.AdvanceDay(1);         // +1 globalny dzień
                    GameData.Instance.lastBattleJustEnded = false; // to nie powrót z walki
                }

                OnNewDay(); // tick waypointów + losowanie + UI
            }
        }

        // Jeśli w trakcie ruchu był otwarty panel – schowaj go
        if (isMoving && CheckPanel != null && CheckPanel.activeSelf)
            CheckPanel.SetActive(false);
    }


    // === Nowy "pipeline" nowego dnia ===
    private void OnNewDay()
    {
        // 1) Decrementy/sprzątanie na waypointach
        Waypoint[] waypoints = FindObjectsOfType<Waypoint>();
        foreach (Waypoint wp in waypoints)
            wp.UpdateDay();

        // 2) Wylosuj kilka waypointów na 1 dzień (dopasuj parametry pod design)
        ActivateRandomWaypoints(2, 1);

        // 3) UI
        UpdateDayUI_Safe();
    }
    // === Właściwe przejście do sceny walki ===
    private void ChangeScene()
    {
        if (selectedWaypoint == null)
        {
            Debug.LogError("Nie wybrano waypointa!");
            return;
        }

        if (GameData.Instance == null)
        {
            Debug.LogError("GameData.Instance jest null! Upewnij się, że obiekt GameData istnieje w scenie.");
            return;
        }

        GameData.Instance.playerCharacters = selectedCharacters;
        GameData.Instance.enemyCharacters = currentWaypoint.enemyCharacters;

        Debug.Log("Zmiana sceny na: " + Map);
        SceneManager.LoadScene("SampleScene"); // jeśli chcesz użyć pola Map – podmień tutaj na Map
    }

    // === Potwierdzenie ruchu ===
    private void OnMoveConfirmed()
    {
        Debug.Log($"[MAP] OnMoveConfirmed() sel={(Alive(selectedWaypoint) ? selectedWaypoint.name : "NULL")}, cur={(Alive(currentWaypoint) ? currentWaypoint.name : "NULL")}");

        if (!Alive(selectedWaypoint))
        {
            Debug.Log("[MAP] Run clicked but no target.");
            return;
        }

        // Po powrocie z walki currentWaypoint mógł zniknąć — ustal pozycją gracza
        if (!Alive(currentWaypoint))
            currentWaypoint = FindClosestToPosition(transform.position);

        if (!Alive(currentWaypoint))
        {
            Debug.LogError("[MAP] currentWaypoint still NULL – nie mogę policzyć ścieżki.");
            return;
        }

        if (movePanel) movePanel.SetActive(false);
        Debug.Log($"[MAP] Run confirmed -> moving to {selectedWaypoint.name}");

        var start = currentWaypoint;
        pathQueue.Clear();

        var path = FindPathAStar(start, selectedWaypoint);
        if (path.Count == 0)
        {
            Debug.LogWarning("[MAP] Brak ścieżki.");
            return;
        }

        foreach (var wp in path) pathQueue.Enqueue(wp);
        isMoving = true;
    }


    // === Ruch bohatera pomiędzy waypointami ===
    private void MoveToNextWaypoint()
    {
        if (pathQueue.Count == 0) return;

        Waypoint targetWaypoint = pathQueue.Peek();
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.transform.position, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetWaypoint.transform.position) < 0.1f)
        {
            pathQueue.Dequeue();
            if (pathQueue.Count == 0)
            {
                isMoving = false;
                currentWaypoint = targetWaypoint;

                // Pokaż przycisk do walki
                if (sceneChangeButton != null)
                    sceneChangeButton.gameObject.SetActive(true);

                // Jeśli waypoint aktywny – pokaż panel
                if (currentWaypoint.isActivated && CheckPanel != null)
                {
                    CheckPanel.SetActive(true);
                    Debug.Log("CheckPanel shown at: " + currentWaypoint.name);
                }

                ShowEnemiesAutoAtWaypoint(currentWaypoint);
            }
        }
    }

    // === Losowanie waypointów do aktywacji ===
    private void ActivateRandomWaypoints(int count, int days)
    {
        Waypoint[] waypoints = FindObjectsOfType<Waypoint>();
        if (waypoints.Length == 0)
        {
            Debug.LogError("Brak punktów na mapie!");
            return;
        }

        List<Waypoint> shuffled = waypoints.ToList();
        System.Random rng = new System.Random();

        for (int i = 0; i < shuffled.Count; i++)
        {
            int r = rng.Next(i, shuffled.Count);
            (shuffled[i], shuffled[r]) = (shuffled[r], shuffled[i]);
        }

        for (int i = 0; i < Mathf.Min(count, shuffled.Count); i++)
        {
            if (shuffled[i] != null)
            {
                shuffled[i].ActivateSpecialColor(days);
                shuffled[i].isActivated = true;
                shuffled[i].AssignRandomEnemies(3);
            }
            else
            {
                Debug.LogError("Wylosowany waypoint jest null!");
            }
        }
    }

    // === Najbliższy waypoint pod kursorem ===
    private Waypoint FindClosestWaypoint()
    {
        if (Camera.main == null) return null;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Waypoint[] waypoints = FindObjectsOfType<Waypoint>();
        Waypoint closest = null;
        float minDist = Mathf.Infinity;
        float clickRange = 0.4f;

        foreach (Waypoint wp in waypoints)
        {
            float dist = Vector3.Distance(mousePos, wp.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = wp;
            }
        }

        return closest != null && minDist <= clickRange ? closest : null;
    }

    // === A* po grafie waypointów ===
    private List<Waypoint> FindPathAStar(Waypoint start, Waypoint goal)
    {
        var result = new List<Waypoint>();
        if (!Alive(start) || !Alive(goal))
            return result;
        if (start == goal)
        {
            result.Add(goal);
            return result;
        }
        Dictionary<Waypoint, Waypoint> cameFrom = new Dictionary<Waypoint, Waypoint>();
        Dictionary<Waypoint, float> costSoFar = new Dictionary<Waypoint, float>();
        PriorityQueue<Waypoint> frontier = new PriorityQueue<Waypoint>();

        frontier.Enqueue(start, 0);
        cameFrom[start] = null;
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            Waypoint current = frontier.Dequeue();
            if (current == goal) break;

            foreach (Waypoint neighbor in current.neighbors)
            {
                float newCost = costSoFar[current] + Vector3.Distance(current.transform.position, neighbor.transform.position);

                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    float priority = newCost + Heuristic(neighbor, goal);
                    frontier.Enqueue(neighbor, priority);
                    cameFrom[neighbor] = current;
                }
            }
        }

        List<Waypoint> path = new List<Waypoint>();
        Waypoint step = goal;

        while (step != null)
        {
            path.Insert(0, step);
            step = cameFrom.ContainsKey(step) ? cameFrom[step] : null;
        }

        return path;
    }

    private float Heuristic(Waypoint a, Waypoint b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }

    // === UI wrogów ===
    public void ShowEnemiesAtSelectedWaypoint()
    {
        if (enemyInfoText == null) return;

        if (selectedWaypoint != null)
        {
            if (selectedWaypoint.enemyCharacters.Count > 0)
            {
                string enemiesLine = string.Join(", ", selectedWaypoint.enemyCharacters.Select(e => e.ToString()));
                string info = "Wrogowie: " + enemiesLine;
                Debug.Log("Wrogowie na punkcie " + selectedWaypoint.name + ": " + enemiesLine);
                enemyInfoText.text = info;
            }
            else
            {
                string msg = "Brak wrogów na punkcie: " + selectedWaypoint.name;
                enemyInfoText.text = msg;
                Debug.Log(msg);
            }
        }
        else
        {
            string msg = "Nie wybrano żadnego punktu.";
            enemyInfoText.text = msg;
            Debug.Log(msg);
        }
    }

    public void ShowEnemiesAutoAtWaypoint(Waypoint waypoint)
    {
        if (autoEnemyInfoText == null) return;

        if (waypoint != null)
        {
            if (waypoint.enemyCharacters.Count > 0)
            {
                string enemiesLine = string.Join("\n- ", waypoint.enemyCharacters.Select(e => e.ToString()));
                string info = "Wrogowie:\n- " + enemiesLine;
                autoEnemyInfoText.text = info;
                Debug.Log("Auto info o wrogach na punkcie " + waypoint.name + ":\n" + info);
            }
            else
            {
                string msg = "Brak przeciwników na punkcie: " + waypoint.name;
                autoEnemyInfoText.text = msg;
            }
        }
    }

    public void OnRunClicked()
    {
        Debug.Log("[MAP] RUN click");
        OnMoveConfirmed();   // Twoja istniejąca logika ruszania
    }

    public void OnStartClicked()
    {
        Debug.Log("[MAP] START click");
        ChangeScene();       // Twoja istniejąca logika startu walki
    }

    // === Utility ===
    public List<ChessPieceType> GetRandomCharacters(int count = 4)
    {
        List<ChessPieceType> all = System.Enum.GetValues(typeof(ChessPieceType))
            .Cast<ChessPieceType>()
            .Where(t => t != ChessPieceType.None)
            .ToList();

        System.Random rng = new System.Random();
        all = all.OrderBy(x => rng.Next()).ToList();
        return all.Take(count).ToList();
    }
}
