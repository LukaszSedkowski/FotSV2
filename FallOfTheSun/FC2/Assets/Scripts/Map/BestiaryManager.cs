using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralny magazyn Bestiariusza (runtime). Singleton + DontDestroyOnLoad.
/// Na razie: tylko rejestr zabitych typów i event zmiany.
/// </summary>
public class BestiaryManager : MonoBehaviour
{
    public static BestiaryManager Instance { get; private set; }

    /// <summary> Zestaw unikalnych, pokonanych typów w tej rozgrywce. </summary>
    public HashSet<ChessPieceType> defeatedTypes = new HashSet<ChessPieceType>();

    public event Action OnChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[Bestiary][Manager] Ready.");
    }

    /// <summary>
    /// Rejestruje zabicie przeciwnika. Zwraca true, jeœli to pierwszy kill danego typu.
    /// Warunki: aktywne w SinglePlayer, killerTeam==0 (gracz), killedTeam!=0, type != None.
    /// </summary>
    public bool RegisterKill(ChessPieceType type, int killerTeam, int killedTeam)
    {
        Debug.Log($"[Bestiary][Manager] RegisterKill(type={type}, killerTeam={killerTeam}, killedTeam={killedTeam})");

        if (!IsActiveForCurrentMode())
        {
            Debug.Log("[Bestiary][Manager] Not active for current mode – skip.");
            return false;
        }
        if (killerTeam != 0 || killedTeam == 0)
        {
            Debug.Log($"[Bestiary][Manager] Ignored: killerTeam must be 0 and killedTeam != 0 (got {killerTeam}/{killedTeam}).");
            return false;
        }
        if (type == ChessPieceType.None)
        {
            Debug.Log("[Bestiary][Manager] Ignored: type == None.");
            return false;
        }

        bool added = defeatedTypes.Add(type);
        Debug.Log($"[Bestiary][Manager] defeatedTypes.Add({type}) -> {added}");

        if (added)
            OnChanged?.Invoke();

        return added;
    }

    public bool HasDefeated(ChessPieceType type) => defeatedTypes.Contains(type);

    public IEnumerable<ChessPieceType> GetDefeatedTypes() => defeatedTypes;

    public void ResetBestiary()
    {
        defeatedTypes.Clear();
        OnChanged?.Invoke();
        Debug.Log("[Bestiary][Manager] Reset.");
    }

    private bool IsActiveForCurrentMode()
    {
        if (GameData.Instance == null)
        {
            Debug.LogWarning("[Bestiary][Manager] GameData.Instance is null.");
            return false;
        }
        return GameData.Instance.CurrentGameMode == GameMode.SinglePlayer;
    }
}
