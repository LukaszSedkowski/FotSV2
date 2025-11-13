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

    void Awake()
    {
        // Rend może być na tym GO albo na dziecku (SpriteRenderer/MeshRenderer)
        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
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
        var allEnemies = System.Enum.GetValues(typeof(ChessPieceType))
            .Cast<ChessPieceType>()
            .Where(t => t != ChessPieceType.None)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(count)
            .ToList();

        enemyCharacters = allEnemies;
    }

    public void DrawLinesToNeighbors()
    {
        foreach (Waypoint neighbor in neighbors)
        {
            if (neighbor == null) continue;

            var lineObj = new GameObject("LineTo_" + neighbor.name);
            lineObj.transform.SetParent(transform, false);

            var lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, transform.position);
            lr.SetPosition(1, neighbor.transform.position);
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
        }
    }


    private void UpdateColor()
    {
        if (rend == null) return; // <-- guard przeciwko NRE

        if (specialDayCount > 0)
        {
            rend.material.color = specialColor;
        }
        else
        {
            rend.material.color = defaultColor;
            enemyCharacters.Clear();
            isActivated = false;
        }
    }
}
    
    

