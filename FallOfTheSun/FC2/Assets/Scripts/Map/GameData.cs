using UnityEngine;
using System.Collections.Generic;

public enum GameMode { SinglePlayer, MultiTeam }

public class GameData : MonoBehaviour
{
    public static GameData Instance { get; private set; }

    public GameMode CurrentGameMode;

    // === GLOBALNY DZIEŃ ===
    public int currentDay = 0;

    // Flaga: wróciliśmy właśnie z walki i dzień został już zwiększony
    public bool lastBattleJustEnded = false;

    // SinglePlayer:
    public List<ChessPieceType> playerCharacters = new List<ChessPieceType>();
    public List<ChessPieceType> enemyCharacters = new List<ChessPieceType>();

    // MultiTeam:
    public List<List<ChessPieceType>> selectedCharacters = new List<List<ChessPieceType>>();
    public bool[] isAIControlledTeams;

    [Header("Team Colors")]
    public List<Color> teamColors = new List<Color>();

    [Header("Bestiary Settings")]
    public bool bestiaryBonusesEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        if (CurrentGameMode == 0) CurrentGameMode = GameMode.SinglePlayer;
    }

    public void AdvanceDay(int amount = 1)
    {
        currentDay = Mathf.Max(0, currentDay + amount);
        lastBattleJustEnded = true; // Map wie, że ma „przerobić” nowy dzień
        Debug.Log($"[GameData] Day advanced to {currentDay}");
    }

    public bool IsBestiaryActiveForCurrentMode()
    {
        return bestiaryBonusesEnabled && CurrentGameMode == GameMode.SinglePlayer;
    }

    public void ResetBestiary()
    {
        if (BestiaryManager.Instance != null)
            BestiaryManager.Instance.ResetBestiary();
    }

    public bool TryRegisterKill(ChessPieceType type, int killerTeam, int killedTeam)
    {
        if (BestiaryManager.Instance == null)
            return false;
        return BestiaryManager.Instance.RegisterKill(type, killerTeam, killedTeam);
    }
}
