using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamColorController : MonoBehaviour
{
    [Header("UI References")]
    public Button colorLeftButton;
    public Button colorRightButton;
    public Image colorDisplay;

    [Header("Color Palette")]
    public Color[] palette = new Color[]
    {
        Color.red,
        Color.yellow,
        Color.green,
        new Color(0.5f, 0f, 0.5f), // purple
        Color.blue
    };

    // Shared across all slots to prevent duplicates
    private static List<int> usedIndices = new List<int>();

    private int currentIndex = -1;

    private void Start()
    {
        colorLeftButton.onClick.AddListener(() => CycleColor(-1));
        colorRightButton.onClick.AddListener(() => CycleColor(+1));
        AssignDefaultColor();
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        // Free up this color for others
        if (currentIndex >= 0)
            usedIndices.Remove(currentIndex);
    }

    private void AssignDefaultColor()
    {
        // find first unused palette index
        for (int i = 0; i < palette.Length; i++)
        {
            if (!usedIndices.Contains(i))
            {
                currentIndex = i;
                usedIndices.Add(i);
                return;
            }
        }
        // fallback
        currentIndex = 0;
        usedIndices.Add(0);
    }

    private void CycleColor(int delta)
    {
        int start = currentIndex;
        int idx = currentIndex;
        do
        {
            idx = (idx + delta + palette.Length) % palette.Length;
        }
        while (usedIndices.Contains(idx) && idx != start);

        if (!usedIndices.Contains(idx))
        {
            // free old
            usedIndices.Remove(currentIndex);
            // take new
            currentIndex = idx;
            usedIndices.Add(currentIndex);
            UpdateDisplay();
        }
    }

    private void UpdateDisplay()
    {
        colorDisplay.color = palette[currentIndex];
        Debug.Log($"{gameObject.name} – kolor indeks {currentIndex} = {palette[currentIndex]}");
    }

    /// <summary>
    /// Returns the currently selected team color.
    /// </summary>
    public Color GetSelectedColor()
    {
        return palette[currentIndex];
    }
}
