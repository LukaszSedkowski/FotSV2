using UnityEngine;
using System.Collections.Generic;

public enum GameMode
{
    SinglePlayer,
    MultiTeam
}

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public GameMode CurrentGameMode;

    // === SinglePlayer ===
    public List<ChessPieceType> playerCharacters = new List<ChessPieceType>();
    public List<ChessPieceType> enemyCharacters = new List<ChessPieceType>();

    // === MultiTeam ===
    public List<List<ChessPieceType>> selectedCharacters = new List<List<ChessPieceType>>();
    public bool[] isAIControlledTeams;

    [Header("Team Colors")]
    public List<Color> teamColors = new List<Color>();

    // === Bestiary – ustawienia globalne (stan trzyma BestiaryManager) ===
    [Header("Bestiary Settings")]
    public bool bestiaryBonusesEnabled = true; // przełącznik, gdybyś chciał łatwo wyłączyć bonusy

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // ⬇️ KLUCZ: przenieś na root zanim zrobisz DontDestroyOnLoad
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

        // domyślka, gdyby nic nie ustawiło
        if (CurrentGameMode == 0)
            CurrentGameMode = GameMode.SinglePlayer;
    }

    /// <summary>
    /// Czy Bestiariusz jest aktywny dla bieżącego trybu gry?
    /// (Na start: tylko SinglePlayer + globalny przełącznik)
    /// </summary>
    public bool IsBestiaryActiveForCurrentMode()
    {
        return bestiaryBonusesEnabled && CurrentGameMode == GameMode.SinglePlayer;
    }

    /// <summary>
    /// Wyczyść stan Bestiariusza (np. przy Nowej Grze / powrocie do Main Menu).
    /// </summary>
    public void ResetBestiary()
    {
        if (BestiaryManager.Instance != null)
        {
            BestiaryManager.Instance.ResetBestiary();
        }
    }

    /// <summary>
    /// Forward: rejestracja zabicia do BestiaryManager (używane w AttackManager).
    /// </summary>
    public bool TryRegisterKill(ChessPieceType type, int killerTeam, int killedTeam)
    {
        Debug.Log($"[Bestiary][GameData] Forward to BestiaryManager: type={type}, killer={killerTeam}, killed={killedTeam}");

        if (BestiaryManager.Instance == null)
        {
            Debug.LogWarning("[Bestiary][GameData] BestiaryManager.Instance is null – cannot forward.");
            return false;
        }

        return BestiaryManager.Instance.RegisterKill(type, killerTeam, killedTeam);
    }
}
