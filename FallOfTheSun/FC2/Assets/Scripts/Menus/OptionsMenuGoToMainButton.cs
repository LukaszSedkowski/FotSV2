using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Button))]
public class OptionsMenuGoToMainButton : MonoBehaviour
{
    [Tooltip("Nazwy scen, w których ten przycisk ma byæ ukryty.")]
    [SerializeField] private string[] hideInScenes = { "MainMenu" };

    [Tooltip("Zaznacz, jeœli chcesz te¿ zniszczyæ MusicPlayer przy powrocie do menu.")]
    [SerializeField] private bool destroyMusicPlayer = false;

    private Button _btn;
    private CanvasGroup _cg;

    void Awake()
    {
        _btn = GetComponent<Button>();
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        ApplyVisibilityForActiveScene();

        // Rebind onClick tylko gdy widoczny
        _btn.onClick.RemoveAllListeners();
        if (IsHiddenInCurrentScene() == false)
            _btn.onClick.AddListener(OnBackToMainMenu);

        // (opcjonalnie) aktualizuj widocznoœæ, gdyby scena zmieni³a siê, gdy menu jest otwarte
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyVisibilityForActiveScene();
        _btn.onClick.RemoveAllListeners();
        if (IsHiddenInCurrentScene() == false)
            _btn.onClick.AddListener(OnBackToMainMenu);
    }

    private bool IsHiddenInCurrentScene()
    {
        string active = SceneManager.GetActiveScene().name;
        foreach (var n in hideInScenes)
        {
            if (string.Equals(n, active, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private void ApplyVisibilityForActiveScene()
    {
        bool hide = IsHiddenInCurrentScene();
        // Nie wy³¹czamy ca³ego GO — ¿eby OnEnable wykonywa³ siê poprawnie po zmianie sceny
        _cg.alpha = hide ? 0f : 1f;
        _cg.interactable = !hide;
        _cg.blocksRaycasts = !hide;
    }

    public void OnBackToMainMenu()
    {
        // 1) Czas gry na normalny (na wypadek pauzy)
        Time.timeScale = 1f;

        // 2) Zamknij ewentualne menu opcji
        if (OptionsMenuManager.Instance != null)
            OptionsMenuManager.Instance.CloseMenu();

        // 3) Wyczyœæ i ZNISZCZ GameData (DDOL), aby wróciæ do czystego stanu
        if (GameData.Instance != null)
        {
            var gd = GameData.Instance;
            gd.CurrentGameMode = GameMode.SinglePlayer;
            gd.teamColors?.Clear();
            gd.selectedCharacters?.Clear();
            gd.playerCharacters?.Clear();
            gd.enemyCharacters?.Clear();
            GameData.Instance.ResetBestiary();
            gd.isAIControlledTeams = null;
            Object.Destroy(gd.gameObject);
        }

        // 4) Usuñ inne singletony/DDOL, jeœli s¹
        TryDestroy<AIController>();
        TryDestroy<GameManager>();     // je¿eli masz taki u siebie
        if (destroyMusicPlayer) TryDestroy<MusicPlayer>();

        // 5) Wczytaj MainMenu
        SceneManager.LoadScene("MainMenu");
    }

    private static void TryDestroy<T>() where T : MonoBehaviour
    {
        var obj = Object.FindObjectOfType<T>();
        if (obj != null) Object.Destroy(obj.gameObject);
    }
}
