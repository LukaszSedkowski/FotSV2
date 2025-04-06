using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 
using UnityEngine.SceneManagement; // do zmiany sceny





public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private float minSpeed = 1f;
    private float maxSpeed = 10f;

    private Queue<Waypoint> pathQueue = new Queue<Waypoint>();
    private bool isMoving = false;
    private bool isFrozen = false;
    private Waypoint currentWaypoint;
    private Waypoint selectedWaypoint; // Przechowuje wybrany waypoint

    public Text dayText;
    private int dayCount = 0;
    private float timeMoving = 0f;

    public Waypoint startPoint;
    public GameObject movePanel; // Panel UI
    public Button moveButton; // Przycisk do potwierdzenia ruchu

    public GameObject CheckPanel;
    public Button sceneChangeButton; // przypisz w Inspectorze
    public string Map = "SampleScene"; // podaj nazwę sceny z Build

    

void Start()
{
    if (startPoint != null)
    {
        currentWaypoint = startPoint;
        transform.position = startPoint.transform.position;
    }
    else
    {
        currentWaypoint = FindClosestWaypoint();
        transform.position = currentWaypoint.transform.position;
    }

    Camera mainCamera = Camera.main;
    movePanel.SetActive(false);
    CheckPanel.SetActive(false); // Ukrywamy panel na start
    moveButton.onClick.AddListener(OnMoveConfirmed); // Podpinamy metodę do przycisku
    UpdateDayUI();

    sceneChangeButton.gameObject.SetActive(false);
    sceneChangeButton.onClick.AddListener(ChangeScene);

    // Przesunięcie aktywacji waypointów o jeden dzień
    if (dayCount > 0)
    {
        ActivateRandomWaypoints(2, 3); // Wybierz losowo 2 waypointy na 3 dni
    }
}

    

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            speed = Mathf.Clamp(speed + 1f, minSpeed, maxSpeed);
        }
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            speed = Mathf.Clamp(speed - 1f, minSpeed, maxSpeed);
        }



    if (Input.GetMouseButtonDown(0)) 
    {
        // Sprawdzamy, czy myszka jest nad UI - jeśli tak, ignorujemy kliknięcie
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        Waypoint target = FindClosestWaypoint();
        
        if (target != null && target != currentWaypoint)
        {
            selectedWaypoint = target;
            movePanel.SetActive(true); // Pokazujemy panel
        }
        else
        {
            movePanel.SetActive(false); // Ukrywamy panel, jeśli kliknięto poza waypointem
        }
    }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            isFrozen = !isFrozen;
            isMoving = !isFrozen;
        }

        if (isMoving && pathQueue.Count > 0)
        {
            MoveToNextWaypoint();
        }

        if (isMoving)
        {
            timeMoving += Time.deltaTime;
            if (timeMoving >= 5f)
            {
                dayCount++;
                timeMoving = 0f;
                UpdateDayUI();
            }
        }
        if (isMoving && CheckPanel.activeSelf)
        {
        CheckPanel.SetActive(false);
        }
    }
public void ChangeScene()
{
    Debug.Log("Zmiana sceny na: " + Map); // Sprawdzamy, czy scena jest poprawnie wybrana
    SceneManager.LoadScene("SampleScene");
}

    void OnMoveConfirmed()
    {
        if (selectedWaypoint == null) return;

        movePanel.SetActive(false); // Ukrywamy panel

        Waypoint start = pathQueue.Count > 0 ? pathQueue.Peek() : currentWaypoint;
        pathQueue.Clear();
        List<Waypoint> path = FindPathAStar(start, selectedWaypoint);

        if (path.Count > 0)
        {
            foreach (var wp in path)
            {
                pathQueue.Enqueue(wp);
            }
            isMoving = true;
        }
    }

void MoveToNextWaypoint()
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

            // Pokazujemy przycisk po dotarciu do celu
            sceneChangeButton.gameObject.SetActive(true);

            // Pokaż panel tylko jeśli waypoint jest aktywowany
            if (currentWaypoint.isActivated)
            {
                CheckPanel.SetActive(true);
                Debug.Log("CheckPanel shown at: " + currentWaypoint.name); // Debugowanie
            }
        }
    }
}



void UpdateDayUI()
{
    dayText.text = "Dzień: " + dayCount.ToString();

    Waypoint[] waypoints = FindObjectsOfType<Waypoint>();
    foreach (Waypoint wp in waypoints)
    {
        wp.UpdateDay();
    }

    // Losowanie waypointów zaczyna się od 1 dnia, więc sprawdzamy, czy dayCount > 0
    if (dayCount > 0 && dayCount % 2 == 0) // Co 5 dni losujemy nowe waypointy
    {
        ActivateRandomWaypoints(2, 3); // Wybierz losowo 2 waypointy na 3 dni
    }
}


void ActivateRandomWaypoints(int count, int days)
{
    Waypoint[] waypoints = FindObjectsOfType<Waypoint>();

    if (waypoints.Length == 0)
    {
        Debug.LogError("Brak punktów na mapie!");
        return;
    }

    List<Waypoint> shuffledWaypoints = new List<Waypoint>(waypoints);
    System.Random rng = new System.Random();
    
    // Tasujemy listę waypointów
    for (int i = 0; i < shuffledWaypoints.Count; i++)
    {
        int randomIndex = rng.Next(i, shuffledWaypoints.Count);
        Waypoint temp = shuffledWaypoints[i];
        shuffledWaypoints[i] = shuffledWaypoints[randomIndex];
        shuffledWaypoints[randomIndex] = temp;
    }

    // Wybieramy pierwsze 'count' waypointów
    for (int i = 0; i < Mathf.Min(count, shuffledWaypoints.Count); i++)
    {
        if (shuffledWaypoints[i] != null)
        {
            shuffledWaypoints[i].ActivateSpecialColor(days);
            shuffledWaypoints[i].isActivated = true;
        }
        else
        {
            Debug.LogError("Waypoint " + i + " jest null!");
        }
    }
}




    Waypoint FindClosestWaypoint()
    {
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

    List<Waypoint> FindPathAStar(Waypoint start, Waypoint goal)
    {
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

    float Heuristic(Waypoint a, Waypoint b)
    {
        return Vector3.Distance(a.transform.position, b.transform.position);
    }
}
