using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // Jeœli korzystasz z TextMeshPro
using UnityEngine.SceneManagement;

public class GameMenu : MonoBehaviour
{
    [Header("Main Menu Components")]
    public TMP_Dropdown playerCountDropdown;
    public TMP_Dropdown pawnCountDropdown;
    public Button nextButton;

    [Header("Character Selection Components")]
    public GameObject characterSelectionMenu;
    public Transform characterListContainer;
    public GameObject characterButtonPrefab;
    public Button startGameButton;

    private int playerCount;
    private int pawnCount;
    private int currentPlayer = 0;
    public List<List<ChessPieceType>> selectedCharacters = new List<List<ChessPieceType>>();

    public static GameMenu Instance { get; private set; }

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
        nextButton.onClick.AddListener(OpenCharacterSelectionMenu);
        startGameButton.onClick.AddListener(StartGame);

        // Default values
        UpdatePlayerCount();
        UpdatePawnCount();
    }
    public void GuestPlay()
    {
        if (selectedCharacters != null)
        {
            DisableCamera();
            SceneManager.LoadScene(1);
        }
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

    private void OpenCharacterSelectionMenu()
    {
        // Initialize character selection menu
        characterSelectionMenu.SetActive(true);
        selectedCharacters.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            selectedCharacters.Add(new List<ChessPieceType>());
        }
        currentPlayer = 0;
        UpdateCharacterList();
    }

    private void UpdateCharacterList()
    {
        // Clear existing buttons
        foreach (Transform child in characterListContainer)
        {
            Destroy(child.gameObject);
        }

        // Populate list with character buttons
        foreach (ChessPieceType piece in (ChessPieceType[])System.Enum.GetValues(typeof(ChessPieceType)))
        {
            if (piece == ChessPieceType.None) continue;

            GameObject buttonObj = Instantiate(characterButtonPrefab, characterListContainer);
            buttonObj.GetComponentInChildren<TMP_Text>().text = piece.ToString();

            Button button = buttonObj.GetComponent<Button>();
            button.onClick.AddListener(() => SelectCharacter(piece));
        }
    }

    private void SelectCharacter(ChessPieceType piece)
    {
        if (selectedCharacters[currentPlayer].Count < pawnCount)
        {
            selectedCharacters[currentPlayer].Add(piece);
            Debug.Log($"Player {currentPlayer + 1} selected {piece}");

            if (selectedCharacters[currentPlayer].Count == pawnCount)
            {
                currentPlayer++;

                if (currentPlayer >= playerCount)
                {
                    // All players have selected
                    startGameButton.gameObject.SetActive(true);
                    characterListContainer.parent.gameObject.SetActive(false); // Hide character list
                }
                else
                {
                    UpdateCharacterList();
                }
            }
        }
    }

    private void StartGame()
    {
        Debug.Log("Game starting...");
        foreach (var playerCharacters in selectedCharacters)
        {
            Debug.Log(string.Join(", ", playerCharacters));
        }
    }
}



