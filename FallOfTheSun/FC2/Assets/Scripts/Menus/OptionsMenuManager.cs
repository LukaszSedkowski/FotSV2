using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class OptionsMenuManager : MonoBehaviour
{
    public static OptionsMenuManager Instance;
    public GameObject optionsMenuPrefab;

    [Header("ESC disabled in these scenes")]
    [SerializeField] private string[] disableEscInScenes = new string[] { "MainMenu" };

    private GameObject currentMenu;
    private bool isMenuOpen = false;
    private Canvas globalCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCanvas();
            EnsureEventSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ⛔ Nie reaguj na ESC w scenach z listy (np. MainMenu)
            if (IsEscDisabledInThisScene())
                return;

            if (!isMenuOpen) OpenMenu();
            else CloseMenu();
        }
    }

    public void OpenMenuFromButton()
    {
        Debug.Log("[OptionsMenuManager] OpenMenuFromButton() wywołane");

        if (Menus.Instance != null && Menus.Instance.mainMenu != null)
        {
            Menus.Instance.mainMenu.SetActive(false);
            Debug.Log("[OptionsMenuManager] Ukryto MainMenu");
        }

        OpenMenu();
    }

    public void OpenMenu()
    {
        Debug.Log("[OptionsMenuManager] OpenMenu() start");

        if (optionsMenuPrefab == null)
        {
            Debug.LogError("[OptionsMenuManager] optionsMenuPrefab NIE przypisany w Inspectorze!");
            return;
        }

        EnsureCanvas();
        EnsureEventSystem();

        if (currentMenu == null)
        {
            Debug.Log("[OptionsMenuManager] Tworzymy instancję menu opcji z prefabu");
            currentMenu = Instantiate(optionsMenuPrefab);
            currentMenu.transform.SetParent(globalCanvas.transform, false);

            // 🔹 Resetujemy RectTransform, żeby zakrywało cały ekran
            RectTransform rect = currentMenu.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.localScale = Vector3.one;
            }
        }

        currentMenu.SetActive(true);
        isMenuOpen = true;
        Time.timeScale = 0f;

        // 🔹 DEBUG: sprawdzamy co z obiektem
        Debug.Log($"[OptionsMenuManager] currentMenu activeInHierarchy={currentMenu.activeInHierarchy}");
        Debug.Log($"[OptionsMenuManager] currentMenu layer={LayerMask.LayerToName(currentMenu.layer)}");
        Debug.Log($"[OptionsMenuManager] globalCanvas sortingOrder={globalCanvas.sortingOrder}, layer={LayerMask.LayerToName(globalCanvas.gameObject.layer)}, renderMode={globalCanvas.renderMode}");
        Debug.Log($"[OptionsMenuManager] globalCanvas camera={(globalCanvas.worldCamera != null ? globalCanvas.worldCamera.name : "NULL")}");
        RectTransform rt = currentMenu.GetComponent<RectTransform>();
        if (rt != null)
        {
            Debug.Log($"[OptionsMenuManager] RectTransform pos={rt.position}, sizeDelta={rt.sizeDelta}, anchorsMin={rt.anchorMin}, anchorsMax={rt.anchorMax}");
        }

        Debug.Log("[OptionsMenuManager] OpenMenu() KONIEC");
    }


    public void CloseMenu()
    {
        if (currentMenu != null) currentMenu.SetActive(false);

        isMenuOpen = false;
        Time.timeScale = 1f;

        // Spróbuj pokazać MainMenu, jeśli istnieje
        if (Menus.Instance != null && Menus.Instance.mainMenu != null)
        {
            Menus.Instance.mainMenu.SetActive(true);
            Debug.Log("[OptionsMenuManager] Pokazano MainMenu");
        }
        else
        {
            Debug.LogWarning("[OptionsMenuManager] Nie znaleziono Menus.Instance.mainMenu");
        }
    }

    private void EnsureCanvas()
    {
        if (globalCanvas != null && globalCanvas.gameObject.activeInHierarchy)
        {
            // upewnij się, że kamera jest przypisana jeśli tryb to Camera
            if (globalCanvas.renderMode == RenderMode.ScreenSpaceCamera && globalCanvas.worldCamera == null)
            {
                globalCanvas.worldCamera = Camera.main;
            }
            return;
        }

        var go = new GameObject("GlobalCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        globalCanvas = go.GetComponent<Canvas>();

        // 🔹 Wymuszamy tryb Overlay, żeby nie zależał od kamery
        globalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        globalCanvas.sortingOrder = 9999;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        DontDestroyOnLoad(go);
    }


    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(es);
    }

    private void OnEnable()
    {
        if (globalCanvas != null && globalCanvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            globalCanvas.worldCamera = Camera.main;
        }
    }
    private bool IsEscDisabledInThisScene()
    {
        string active = SceneManager.GetActiveScene().name;
        foreach (var s in disableEscInScenes)
            if (string.Equals(s, active, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }
}
