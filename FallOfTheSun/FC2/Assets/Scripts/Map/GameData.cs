using UnityEngine;
using System.Collections.Generic; 

    public enum GameMode
    {
    SinglePlayer,
    MultiTeam
    }

public class GameData : MonoBehaviour
{
    public static GameData Instance;
    
    public List<ChessPieceType> playerCharacters = new List<ChessPieceType>();
    public List<ChessPieceType> enemyCharacters = new List<ChessPieceType>();




    public GameMode CurrentGameMode = GameMode.SinglePlayer;
    
    private void Awake()
    {
        // Jeśli nie ma instancji, to przypisz ją
        if (Instance == null)
        {
            Instance = this;
            GameData.Instance.CurrentGameMode = GameMode.SinglePlayer;
            DontDestroyOnLoad(gameObject); // Ta instancja nie zniknie przy zmianie sceny
        }
        else
        {
            Destroy(gameObject); // Zniszczy tą instancję, jeśli już istnieje
        }
    }
}
