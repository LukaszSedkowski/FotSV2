using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillsPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;           // Panel zawsze widoczny
    public List<Button> buttons;       // Lista przycisków w inspektorze

    private ChessBoard chessBoard;
    private ChessPieces lastPiece;
    private ChessPieces currentPiece;

    void Start()
    {
        // Panel zawsze aktywny
        if (panel != null)
            panel.SetActive(true);

        // ZnajdŸ referencjê do ChessBoard
        chessBoard = FindObjectOfType<ChessBoard>();
        if (chessBoard == null)
            Debug.LogError("SkillsPanel: Nie znaleziono ChessBoard w scenie!");

        // Przygotuj listener-y do przycisków i aktywuj je
        for (int i = 0; i < buttons.Count; i++)
        {
            int idx = i;
            var btn = buttons[i];
            btn.gameObject.SetActive(true);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnAbilityButtonClicked(idx));
        }

        // Pierwotne uaktualnienie
        RefreshPanel();
    }

    void Update()
    {
        if (chessBoard == null) return;

        // Polluj automatycznie wybran¹ figurê z ChessBoard
        var newPiece = chessBoard.currentlyDragging;
        if (newPiece != lastPiece)
        {
            lastPiece = newPiece;
            SetCurrentPiece(newPiece);
        }
    }

    /// <summary>
    /// Ustawia bie¿¹c¹ figurê i odœwie¿a panel
    /// </summary>
    public void SetCurrentPiece(ChessPieces piece)
    {
        currentPiece = piece;
        RefreshPanel();
    }

    /// <summary>
    /// Odœwie¿a tekst przycisków. Przyciski zawsze interaktywne.
    /// </summary>
    private void RefreshPanel()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            var btn = buttons[i];
            btn.interactable = currentPiece != null && i < currentPiece.abilities.Count;
            var txt = btn.GetComponentInChildren<Text>();
            if (currentPiece != null && i < currentPiece.abilities.Count)
                txt.text = currentPiece.abilities[i].abilityName;
            else
                txt.text = "--";
        }
    }

    /// <summary>
    /// Obs³uga klikniêcia przycisku umiejêtnoœci
    /// </summary>
    private void OnAbilityButtonClicked(int index)
    {
        if (currentPiece != null && index >= 0 && index < currentPiece.abilities.Count)
            currentPiece.UseAbility(index);
        else
            Debug.LogWarning($"Brak umiejêtnoœci pod indeksem {index} lub brak wybranej postaci.");
    }
}
