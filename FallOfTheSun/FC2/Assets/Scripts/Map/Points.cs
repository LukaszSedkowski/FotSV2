using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Waypoint : MonoBehaviour
{
    public List<Waypoint> neighbors;
    private Renderer rend;

    public Color defaultColor = Color.red;
    public Color specialColor = Color.green; // Kolor na kilka dni
   public bool isActivated = false;
    public int specialDayCount = 0; // Ile dni jeszcze ma być inny kolor
    public List<ChessPieceType> enemyCharacters = new List<ChessPieceType>();

    void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateColor();
        DrawLinesToNeighbors();
    }

    public void ActivateSpecialColor(int days)
    {
        specialDayCount = days;
        UpdateColor();
    }

    public void UpdateDay()
    {
        if (specialDayCount > 0)
        {
            specialDayCount--;
            UpdateColor();
        }
    }
    public void AssignRandomEnemies(int count)
{
    // Wybieramy losowe postacie wrogów
    List<ChessPieceType> allEnemies = System.Enum.GetValues(typeof(ChessPieceType))
        .Cast<ChessPieceType>()
        .Where(t => t != ChessPieceType.None)
        .ToList();

    System.Random rng = new System.Random();
    allEnemies = allEnemies.OrderBy(x => rng.Next()).ToList();

    enemyCharacters = allEnemies.Take(count).ToList();

    // Debug info
    foreach (var enemy in enemyCharacters)
    {
        Debug.Log("Dodano wroga " + enemy + " do waypointa " + name);
    }
}

public void DrawLinesToNeighbors()
{
    foreach (Waypoint neighbor in neighbors)
    {
        if (neighbor != null)
        {
            GameObject lineObj = new GameObject("LineTo_" + neighbor.name);
            lineObj.transform.parent = this.transform;

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, this.transform.position);
            lr.SetPosition(1, neighbor.transform.position);

            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
        }
    }
}


private void UpdateColor()
{
    if (specialDayCount > 0)
    {
        rend.material.color = specialColor;
    }
    else
    {
        rend.material.color = defaultColor;
        enemyCharacters.Clear(); // Usunięcie wrogów
        isActivated = false;
        Debug.Log("Usunięto wrogów z waypointa " + name);
    }
}
    
    
}
