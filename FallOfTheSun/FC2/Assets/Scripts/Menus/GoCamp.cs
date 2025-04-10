using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Jeżeli korzystasz z TextMeshPro
using UnityEngine.SceneManagement;

public class GoCamp : MonoBehaviour
{
    [Header("Main Menu Components")]
    public TMP_Dropdown playerCountDropdown;
    public TMP_Dropdown pawnCountDropdown;
    public Button nextButton;

    [Header("Validation Components")]

    private int playerCount;
    private int pawnCount;
    public List<List<ChessPieceType>> selectedCharacters = new List<List<ChessPieceType>>();

    public static GoCamp Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Initialize dropdowns
        playerCountDropdown.onValueChanged.AddListener(delegate { UpdatePlayerCount(); });
        pawnCountDropdown.onValueChanged.AddListener(delegate { UpdatePawnCount(); });
        nextButton.onClick.AddListener(GuestPlay);

        // Default values
        UpdatePlayerCount();
        UpdatePawnCount();
    }

    public void GuestPlay()
    {


        DisableCamera();
        SceneManager.LoadScene(2);
    }

    public void Exit()
    {
        Menus.Instance.gameMenu.SetActive(false);
        Menus.Instance.mainMenu.SetActive(true);
    }

    void DisableCamera()
    {
        Camera cameraToDisable = GameObject.Find("Main Camera").GetComponent<Camera>();
        if (cameraToDisable != null)
        {
            cameraToDisable.enabled = false;
        }
    }

    private void UpdatePlayerCount()
    {
        playerCount = playerCountDropdown.value + 2; // Dropdown values start at 0
    }

    private void UpdatePawnCount()
    {
        pawnCount = pawnCountDropdown.value + 1;
    }
}
