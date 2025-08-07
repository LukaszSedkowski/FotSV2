// GameMenuController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameMenuController : MonoBehaviour
{
    [Header("Etap 1 – Setup")]
    public GameObject setupPanel;
    public TMP_Text playersText, pawnsText;
    public Button playersLeftButton, playersRightButton;
    public Button pawnsLeftButton, pawnsRightButton;
    public Button nextButton;

    [Header("Etap 2 – Player Slots")]
    public GameObject playersPanel;
    public Transform playersContainer;
    public GameObject playerSlotPrefab;
    public Button startGameButton;

    private int playersCount = 2;
    private int pawnsCount = 1;
    private int confirmedSlots;

    private void Awake()
    {
        if (GameData.Instance == null)
        {
            GameObject go = new GameObject("GameData");
            go.AddComponent<GameData>();
        }
    }

    private void Start()
    {
        Debug.Log($"[GMC] setupPanel={(setupPanel == null ? "NULL" : "OK")}, " +
              $"playersPanel={(playersPanel == null ? "NULL" : "OK")}, " +
              $"playersContainer={(playersContainer == null ? "NULL" : "OK")}, " +
              $"playerSlotPrefab={(playerSlotPrefab == null ? "NULL" : "OK")}, " +
              $"playersLeftButton={(playersLeftButton == null ? "NULL" : "OK")}, " +
              $"nextButton={(nextButton == null ? "NULL" : "OK")}, " +
              $"startGameButton={(startGameButton == null ? "NULL" : "OK")}");
        playersLeftButton.onClick.AddListener(() => ChangePlayers(-1));
        playersRightButton.onClick.AddListener(() => ChangePlayers(+1));
        pawnsLeftButton.onClick.AddListener(() => ChangePawns(-1));
        pawnsRightButton.onClick.AddListener(() => ChangePawns(+1));
        nextButton.onClick.AddListener(OnNext);
        startGameButton.onClick.AddListener(OnStartGame);

        UpdateSetupUI();
        playersPanel.SetActive(false);
        startGameButton.gameObject.SetActive(false);
    }

    private void ChangePlayers(int delta)
    {
        playersCount = Mathf.Clamp(playersCount + delta, 2, 4);
        UpdateSetupUI();
    }

    private void ChangePawns(int delta)
    {
        pawnsCount = Mathf.Clamp(pawnsCount + delta, 1, 4);
        UpdateSetupUI();
    }

    private void UpdateSetupUI()
    {
        playersText.text = playersCount.ToString();
        pawnsText.text = pawnsCount.ToString();
    }

    private void OnNext()
    {
        setupPanel.SetActive(false);
        playersPanel.SetActive(true);
        startGameButton.gameObject.SetActive(false);
        confirmedSlots = 0;

        // usuñ stare sloty
        foreach (Transform t in playersContainer) Destroy(t.gameObject);

        // utwórz nowe
        for (int i = 0; i < playersCount; i++)
        {
            var slotGO = Instantiate(playerSlotPrefab, playersContainer);
            slotGO.name = $"PlayerSlot_{i + 1}";
            var ctrl = slotGO.GetComponent<PlayerSlotController>();
            ctrl.Setup(pawnsCount);
            ctrl.OnSlotConfirmed += HandleSlotConfirmed;
        }
    }

    private void HandleSlotConfirmed(PlayerSlotController slot)
    {
        confirmedSlots++;
        if (confirmedSlots >= playersCount)
            startGameButton.gameObject.SetActive(true);
    }

    private void OnStartGame()
    {
        Debug.Log("[GMC] OnStartGame() wywo³ane");
        Debug.Log($"[GMC] playersCount={playersCount}, pawnsCount={pawnsCount}, confirmedSlots={confirmedSlots}");

        // 1) Reset listy kolorów i przygotowanie GameData
        GameData.Instance.teamColors.Clear();
        GameData.Instance.CurrentGameMode = GameMode.MultiTeam;
        GameData.Instance.isAIControlledTeams = new bool[playersCount];
        GameData.Instance.selectedCharacters.Clear();

        // 2) Zbierz dane ze slotów
        for (int i = 0; i < playersCount; i++)
        {
            var slot = playersContainer.GetChild(i).GetComponent<PlayerSlotController>();
            GameData.Instance.isAIControlledTeams[i] = !slot.IsHuman;

            // 2a) Jednostki
            var list = new List<ChessPieceType>();
            foreach (var idx in slot.GetSelectedUnits())
                list.Add((ChessPieceType)(idx + 1));
            GameData.Instance.selectedCharacters.Add(list);

            // 2b) Kolor
            var color = slot.colorController.GetSelectedColor();
            GameData.Instance.teamColors.Add(color);
            Debug.Log($"[GMC] Team {i + 1} color zapisany: {color}");
        }

        // 3) Load
        SceneManager.LoadScene("SampleScene");
    }

    public void OnBackToMainMenu()
    {
        // Wyczyœæ dane gry
        if (GameData.Instance != null)
        {
            GameData.Instance.teamColors.Clear();
            GameData.Instance.selectedCharacters.Clear();
            GameData.Instance.playerCharacters.Clear();
            GameData.Instance.enemyCharacters.Clear();
            GameData.Instance.isAIControlledTeams = null;
        }

        // UI: wy³¹cz obecne menu gry, w³¹cz main menu
        Menus.Instance.gameMenu.SetActive(false);
        Menus.Instance.mainMenu.SetActive(true);

        // Prze³aduj scenê g³ównego menu
        SceneManager.LoadScene("MainMenu");
    }

}
