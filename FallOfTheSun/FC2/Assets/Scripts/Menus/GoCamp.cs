using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GoCamp : MonoBehaviour
{
    [Header("Main Menu Components")]
    public TMP_Dropdown playerCountDropdown;  // (tylko w scenie menu)
    public TMP_Dropdown pawnCountDropdown;    // (tylko w scenie menu)
    public Button nextButton;                 // (tylko w scenie menu)

    private int playerCount = 2;
    private int pawnCount = 1;

    public static GoCamp Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Jeśli GoCamp ma żyć TYLKO w scenie menu, możesz to zakomentować.
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Podpinaj listenery tylko jeśli UI istnieje (w scenie menu).
        if (playerCountDropdown != null)
            playerCountDropdown.onValueChanged.AddListener(_ => UpdatePlayerCount());

        if (pawnCountDropdown != null)
            pawnCountDropdown.onValueChanged.AddListener(_ => UpdatePawnCount());

        if (nextButton != null)
            nextButton.onClick.AddListener(GuestPlay);

        // Domyślne wartości także wtedy, gdy UI nie ma (np. w scenie mapy)
        UpdatePlayerCount();
        UpdatePawnCount();
    }

    public void GuestPlay()
    {
        // Wymuś tryb kampanii i wyczyść pozostałości po MultiTeam
        if (GameData.Instance != null)
        {
            GameData.Instance.CurrentGameMode = GameMode.SinglePlayer;

            GameData.Instance.selectedCharacters.Clear();
            GameData.Instance.teamColors.Clear();
            GameData.Instance.isAIControlledTeams = null;

            // (opcjonalnie) wyczyść też listy SP – PlayerMovement ustawi je przed walką
            GameData.Instance.playerCharacters.Clear();
            GameData.Instance.enemyCharacters.Clear();
        }

        DisableCamera();
        SceneManager.LoadScene(2); // scena mapy
    }

    public void Exit()
    {
        if (Menus.Instance != null)
        {
            Menus.Instance.gameMenu.SetActive(false);
            Menus.Instance.mainMenu.SetActive(true);
        }
    }

    private void DisableCamera()
    {
        var cam = Camera.main;
        if (cam != null) cam.enabled = false;
    }

    private void UpdatePlayerCount()
    {
        playerCount = (playerCountDropdown != null) ? playerCountDropdown.value + 2 : 2;
    }

    private void UpdatePawnCount()
    {
        pawnCount = (pawnCountDropdown != null) ? pawnCountDropdown.value + 1 : 1;
    }
}
