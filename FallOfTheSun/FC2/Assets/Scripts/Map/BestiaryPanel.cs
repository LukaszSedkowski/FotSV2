using UnityEngine;
using TMPro;
using System.Linq;

public class BestiaryPanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI typesListText; // lista: Typ – opis bonusu
    [SerializeField] private TextMeshProUGUI totalsText;    // sumaryczne bonusy (opcjonalnie)

    private void OnEnable()
    {
        Refresh();
        if (BestiaryManager.Instance != null)
            BestiaryManager.Instance.OnChanged += Refresh;
    }

    private void OnDisable()
    {
        if (BestiaryManager.Instance != null)
            BestiaryManager.Instance.OnChanged -= Refresh;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        // Bezpieczniki
        if (BestiaryManager.Instance == null)
        {
            if (typesListText) typesListText.text = "(Brak BestiaryManager)";
            if (totalsText) totalsText.text = "";
            return;
        }
        var types = BestiaryManager.Instance.GetDefeatedTypes().ToList();

        // Lista pokonanych z opisami bonusów
        if (typesListText)
        {
            if (types.Count == 0)
            {
                typesListText.text = "Jeszcze nie pokonano ¿adnego typu.";
            }
            else
            {
                var lines = types.Select(t => $"{t} – {BestiaryManager.Instance.GetBonusDescription(t)}");
                typesListText.text = "Pokonane typy:\n• " + string.Join("\n• ", lines);
            }
        }

        // Sumaryczne bonusy (tylko to co niezerowe)
        if (totalsText)
        {
            var totals = BestiaryManager.Instance.GetTotals();
            System.Text.StringBuilder sb = new System.Text.StringBuilder("Aktywne premie:\n");

            bool any = false;
            if (totals.attackPct > 0f) { sb.AppendLine($"• Atak: +{Mathf.RoundToInt(totals.attackPct * 100)}%"); any = true; }
            if (totals.maxHealthPct > 0f) { sb.AppendLine($"• Zdrowie: +{Mathf.RoundToInt(totals.maxHealthPct * 100)}%"); any = true; }
            if (totals.movementAdd != 0) { sb.AppendLine($"• Ruch: +{totals.movementAdd}"); any = true; }
            if (totals.maxMovementAdd != 0) { sb.AppendLine($"• Max ruch: +{totals.maxMovementAdd}"); any = true; }
            if (totals.attackRangeAdd != 0) { sb.AppendLine($"• Zasiêg ataku: +{totals.attackRangeAdd}"); any = true; }

            totalsText.text = any ? sb.ToString() : "Aktywne premie:\n(Brak)";
        }
    }
}
