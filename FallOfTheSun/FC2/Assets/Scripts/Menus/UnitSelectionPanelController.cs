using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // jeśli używasz TextMeshPro, inaczej użyj UnityEngine.UI.Text

public class UnitSelectionPanelController : MonoBehaviour
{
    [Header("UI References")]
    public Transform buttonsContainer;        // kontener, w którym tworzymy przyciski jednostek
    public GameObject unitButtonPrefab;       // prefab zwykłego UI Button z dzieckiem Text/TMP_Text
    public Button confirmButton;              // przycisk Potwierdź

    private int maxSelection;
    private Action<List<int>> onConfirm;
    private List<int> chosenIndices = new List<int>();
    private Dictionary<int, Button> buttonMap = new Dictionary<int, Button>();

    /// <summary>
    /// Inicjalizuje panel.
    /// </summary>
    /// <param name="maxSelection">Maksymalna liczba do wyboru</param>
    /// <param name="onConfirm">Callback po naciśnięciu Potwierdź</param>
    public void Init(int maxSelection, Action<List<int>> onConfirm)
    {
        this.maxSelection = maxSelection;
        this.onConfirm = onConfirm;

        // 1) Generujemy przyciski dla każdego ChessPieceType (pomijając None)
        var values = Enum.GetValues(typeof(ChessPieceType));
        int idx = 0;
        foreach (ChessPieceType type in values)
        {
            if (type == ChessPieceType.None) continue;

            // Instantiate button
            var go = Instantiate(unitButtonPrefab, buttonsContainer);
            go.name = $"UnitButton_{type}";
            var btn = go.GetComponent<Button>();
            buttonMap[idx] = btn;

            // Ustaw tekst
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = type.ToString();
            else
            {
                var txt = go.GetComponentInChildren<Text>();
                if (txt != null) txt.text = type.ToString();
            }

            int capture = idx; // na zamknięcie
            btn.onClick.AddListener(() => OnUnitButtonClicked(capture));

            idx++;
        }

        // 2) Podpinamy Potwierdź
        confirmButton.onClick.AddListener(OnConfirm);
        // 3) Na start – wyłączamy przycisk, aż wybiorą maxSelection
        confirmButton.interactable = false;
    }

    private void OnUnitButtonClicked(int idx)
    {
        // Toggle wyboru
        if (chosenIndices.Contains(idx))
        {
            chosenIndices.Remove(idx);
            SetButtonHighlight(idx, false);
        }
        else if (chosenIndices.Count < maxSelection)
        {
            chosenIndices.Add(idx);
            SetButtonHighlight(idx, true);
        }

        // Aktywuj przycisk Potwierdź tylko, gdy osiągnięto limit
        confirmButton.interactable = (chosenIndices.Count == maxSelection);
    }

    private void SetButtonHighlight(int idx, bool highlight)
    {
        // Prosta zmiana koloru tła przycisku
        if (buttonMap.TryGetValue(idx, out var btn))
        {
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = highlight ? Color.cyan : Color.white;
        }
    }

    private void OnConfirm()
    {
        // Wywołujemy callback z listą wybranych indeksów
        onConfirm?.Invoke(new List<int>(chosenIndices));
        // Niszczenie panelu
        Destroy(gameObject);
    }
}
