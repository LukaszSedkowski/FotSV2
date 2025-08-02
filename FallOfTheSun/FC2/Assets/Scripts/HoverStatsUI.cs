using UnityEngine;
using TMPro;

public class HoverStatsUI : MonoBehaviour
{
    public TMP_Text statsText;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        Hide();
    }

    public void Show(ChessPieces piece)
    {
        string stats = $"Typ: {piece.type}\n" +
                       $"Element: {piece.elementType}\n" +
                       $"HP: {piece.health}/{piece.maxHealth}\n" +
                       $"Ruch: {piece.movementRange}/{piece.maxMovementRange}\n" +
                       $"Zasiêg Widzenia: {piece.visionRange}\n" +
                       $"Umiejêtnoœci: {(piece.abilities.Count > 0 ? string.Join(", ", piece.abilities.ConvertAll(a => a.abilityName)) : "Brak")}\n";

        statsText.text = stats;
        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
    }
}
