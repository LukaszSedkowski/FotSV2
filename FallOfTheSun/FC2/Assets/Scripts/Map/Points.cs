using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Waypoint : MonoBehaviour
{
    public List<Waypoint> neighbors;
    private Renderer rend;

    public Color defaultColor = Color.red;
    public Color specialColor = Color.green; // Kolor na kilka dni

    private int specialDayCount = 0; // Ile dni jeszcze ma być inny kolor

    void Start()
    {
        rend = GetComponent<Renderer>();
        UpdateColor();
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

    private void UpdateColor()
    {
        if (specialDayCount > 0)
        {
            rend.material.color = specialColor;
        }
        else
        {
            rend.material.color = defaultColor;
        }
    }
    
}
