using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameMenuControll : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text playersText;           // Tekst pokazuj¹cy liczbê graczy
    public TMP_Text pawnsText;             // Tekst pokazuj¹cy liczbê armii
    public Button playersLeftButton;       // "<" przy liczbie graczy
    public Button playersRightButton;      // ">"
    public Button pawnsLeftButton;         // "<" przy liczbie armii
    public Button pawnsRightButton;        // ">"
    public Button nextButton;              // Przycisk „Dalej”

    private int playersCount = 2; // domyœlnie 2 graczy
    private int pawnsCount = 1; // domyœlnie 1 armia na dru¿ynê

    private void Start()
    {
        UpdateUI();
        playersLeftButton.onClick.AddListener(() => ChangePlayers(-1));
        playersRightButton.onClick.AddListener(() => ChangePlayers(+1));
        pawnsLeftButton.onClick.AddListener(() => ChangePawns(-1));
        pawnsRightButton.onClick.AddListener(() => ChangePawns(+1));
        nextButton.onClick.AddListener(OnNext);
    }

    private void ChangePlayers(int delta)
    {
        playersCount = Mathf.Clamp(playersCount + delta, 2, 4);
        UpdateUI();
    }

    private void ChangePawns(int delta)
    {
        pawnsCount = Mathf.Clamp(pawnsCount + delta, 1, 4);
        UpdateUI();
    }

    private void UpdateUI()
    {
        playersText.text = playersCount.ToString();
        pawnsText.text = pawnsCount.ToString();
    }

    private void OnNext()
    {
        Debug.Log($"[MainMenu] Players={playersCount}, Armies={pawnsCount}");
    }
}
