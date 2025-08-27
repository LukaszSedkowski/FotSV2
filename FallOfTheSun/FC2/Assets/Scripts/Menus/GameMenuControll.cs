// GameMenuController.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class GameMenuController : MonoBehaviour
{

    [SerializeField] private GameObject unitSelectionPanel;

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
        // 1) Przywróæ normalny czas gry (na wypadek pauzy)
        Time.timeScale = 1f;
        CloseUnitSelectionAny();                 
        if (OptionsMenuManager.Instance != null)  // jeœli masz globalne menu opcji
            OptionsMenuManager.Instance.CloseMenu();


        var unitSelection = GameObject.Find("UnitSelectionPanel");
        if (unitSelection != null)
        {
            unitSelection.SetActive(false);
            Debug.Log("[OnBackToMainMenu] Zamkniêto UnitSelectionPanel");
        }

        // 2) Wyczyœæ i ZNISZCZ GameData, ¿eby przy powrocie powsta³a zupe³nie œwie¿a instancja
        if (GameData.Instance != null)
        {
            GameData.Instance.CurrentGameMode = GameMode.SinglePlayer;
            GameData.Instance.teamColors.Clear();
            GameData.Instance.selectedCharacters.Clear();
            GameData.Instance.playerCharacters.Clear();
            GameData.Instance.enemyCharacters.Clear();
            GameData.Instance.ResetBestiary();
            GameData.Instance.isAIControlledTeams = null;

            // zniszcz DDOL, ¿eby nowa scena utworzy³a œwie¿¹ GameData w Awake
            Destroy(GameData.Instance.gameObject);
        }

        // 3) Usuñ inne ewentualne singletony DDOL, które mog³y zostawiæ stan
        TryDestroy<AIController>();
        TryDestroy<GameManager>();

        // (UI mo¿esz zostawiæ jak jest; i tak zaraz wczytamy scenê)
        // Menus.Instance.gameMenu.SetActive(false);
        // Menus.Instance.mainMenu.SetActive(true);

        // 4) Wczytaj menu od zera
        SceneManager.LoadScene("MainMenu");
    }

    private static void TryDestroy<T>() where T : MonoBehaviour
    {
        var obj = FindObjectOfType<T>();
        if (obj != null) Destroy(obj.gameObject);
    }

    private void CloseUnitSelectionAny()
    {
        // 0) Jeœli masz przypiêty referencj¹ – zamknij od razu
        if (unitSelectionPanel != null && unitSelectionPanel.activeInHierarchy)
        {
            unitSelectionPanel.SetActive(false);
            Debug.Log("[GMC] UnitSelectionPanel zamkniêty (via serialized ref).");
            return;
        }

        // 1) Spróbuj przez GameMenu.Instance.characterSelectionMenu (stary flow)
        if (GameMenu.Instance != null && GameMenu.Instance.characterSelectionMenu != null &&
            GameMenu.Instance.characterSelectionMenu.activeInHierarchy)
        {
            GameMenu.Instance.characterSelectionMenu.SetActive(false);
            Debug.Log("[GMC] UnitSelectionPanel zamkniêty (via GameMenu.Instance.characterSelectionMenu).");
            return;
        }

        // 2) Skany nazw – odporne na ró¿ne warianty
        string[] hints = { "UnitSelection", "Unit Selection", "Selection", "CharacterSelection", "Character Selection", "Army", "Choose", "Pick" };

        // Pobierz WSZYSTKIE RectTransformy w scenie (tylko scenowe, nie assety)
        var all = Resources.FindObjectsOfTypeAll<RectTransform>()
            .Where(rt => rt.gameObject.scene.IsValid()) // pomiñ assety/prefaby
            .Select(rt => rt.gameObject)
            .Distinct()
            .ToList();

        // Log diagnostyczny: poka¿ 10 pierwszych aktywnych, które wygl¹daj¹ jak UI
        int diagShown = 0;
        foreach (var go in all)
        {
            if (!go.activeInHierarchy) continue;
            if (go.GetComponentInParent<Canvas>() == null) continue;
            if (diagShown < 10 && hints.Any(h => go.name.ToLowerInvariant().Contains(h.ToLowerInvariant())))
            {
                Debug.Log($"[GMC][SCAN] Kandydat: {GetFullPath(go)} (active)");
                diagShown++;
            }
        }

        // ZnajdŸ pierwszy aktywny UI, którego nazwa zawiera któryœ hint
        GameObject candidate = all.FirstOrDefault(go =>
            go.activeInHierarchy &&
            go.GetComponentInParent<Canvas>() != null &&
            hints.Any(h => go.name.ToLowerInvariant().Contains(h.ToLowerInvariant())));

        if (candidate != null)
        {
            // Upewnij siê, ¿e wy³¹czamy ROOT pod Canvasem (nie losowe dziecko)
            var root = GetRootUnderCanvas(candidate);

            // Jeœli jest CanvasGroup – wy³¹cz interakcje i alpha
            var cg = root.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            root.SetActive(false);
            Debug.Log($"[GMC] Zamkniêto panel wyboru: {GetFullPath(root)}");
            return;
        }

        Debug.Log("[GMC] Nie znaleziono aktywnego UnitSelectionPanel do zamkniêcia (po skanie nazw).");
    }

    // ———— helpers ————

    private static GameObject GetRootUnderCanvas(GameObject go)
    {
        Transform t = go.transform;
        Transform last = t;
        while (t.parent != null)
        {
            if (t.parent.GetComponent<Canvas>() != null)
                return t.gameObject; // dziecko bezpoœrednio pod Canvasem
            last = t;
            t = t.parent;
        }
        return last != null ? last.gameObject : go;
    }

    private static string GetFullPath(GameObject go)
    {
        if (go == null) return "NULL";
        var t = go.transform;
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

}
