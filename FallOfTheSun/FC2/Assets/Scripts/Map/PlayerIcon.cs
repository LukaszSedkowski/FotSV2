using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    private Queue<Waypoint> pathQueue = new Queue<Waypoint>();
    private bool isMoving = false;
    private bool isFrozen = false; // Nowa zmienna
    private Waypoint currentWaypoint;

    void Start()
    {
        // ZnajdŸ najbli¿szy punkt na starcie
        currentWaypoint = FindClosestWaypoint();
        transform.position = currentWaypoint.transform.position;
    }

    void Update()
    {
        // Sprawdzanie klikniêcia myszk¹
        if (Input.GetMouseButtonDown(0)) // Klikniêcie myszk¹
        {
            // ZnajdŸ nowy cel
            Waypoint target = FindClosestWaypoint();
            Waypoint start = pathQueue.Count > 0 ? pathQueue.Peek() : currentWaypoint; // Start = najbli¿szy waypoint

            if (start != target) // SprawdŸ, czy nie klikniêto tego samego punktu
            {
                // Wyczyœæ poprzedni¹ trasê, aby zacz¹æ now¹
                pathQueue.Clear();

                // Oblicz now¹ œcie¿kê
                List<Waypoint> path = FindPathAStar(start, target);

                // Je¿eli jest œcie¿ka
                if (path.Count > 0)
                {
                    // Wstaw punkty trasy do kolejki
                    foreach (var wp in path)
                    {
                        pathQueue.Enqueue(wp);
                    }

                    isMoving = true;
                }
            }
        }

        // Sprawdzanie klikniêcia spacji do zatrzymywania i wznawiania ruchu
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isFrozen)
            {
                isFrozen = false; // Wznów ruch
                isMoving = true;  // Kontynuuj poruszanie
            }
            else
            {
                isFrozen = true;  // ZamroŸ postaæ
                isMoving = false; // Zatrzymaj ruch
            }
        }

        // Ruszaj w kierunku nastêpnego waypointa w kolejce, tylko jeœli nie jest zamro¿ona
        if (isMoving && pathQueue.Count > 0)
        {
            MoveToNextWaypoint();
        }
    }

    // Funkcja do zatrzymywania ruchu
    void StopMovement()
    {
        isMoving = false;
        pathQueue.Clear(); // Wyczyœæ poprzedni¹ trasê
    }

    Waypoint FindClosestWaypoint()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        Waypoint[] waypoints = FindObjectsOfType<Waypoint>();
        Waypoint closest = null;
        float minDist = Mathf.Infinity;

        foreach (Waypoint wp in waypoints)
        {
            float dist = Vector3.Distance(mousePos, wp.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = wp;
            }
        }

        return closest;
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
        return Vector3.Distance(a.transform.position, b.transform.position); // Odleg³oœæ euklidesowa
    }

    void MoveToNextWaypoint()
    {
        if (pathQueue.Count == 0) return;

        Waypoint targetWaypoint = pathQueue.Peek();
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.transform.position, speed * Time.deltaTime);

        // Jeœli postaæ dotrze do waypointa
        if (Vector3.Distance(transform.position, targetWaypoint.transform.position) < 0.1f)
        {
            pathQueue.Dequeue(); // Usuñ waypoint z kolejki

            // Jeœli nie ma ju¿ kolejnych waypointów, zatrzymaj ruch
            if (pathQueue.Count == 0)
            {
                isMoving = false;
                currentWaypoint = targetWaypoint; // Ustaw obecny waypoint na ostatni punkt
            }
        }
    }
}
