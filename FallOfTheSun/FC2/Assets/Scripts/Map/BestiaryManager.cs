using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Centralny magazyn Bestiariusza (runtime). Singleton + DontDestroyOnLoad.
/// Na razie: tylko rejestr zabitych typów i event zmiany.
/// </summary>
public class BestiaryManager : MonoBehaviour
{
    private Dictionary<ChessPieceType, BestiaryBonus> _bonusMap;

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
        _bonusMap = new Dictionary<ChessPieceType, BestiaryBonus>()
        {
            { ChessPieceType.Werewolf,  new BestiaryBonus(ChessPieceType.Werewolf,  BestiaryBonusType.AttackPercent,        0.10f) }, // +10% ataku
            { ChessPieceType.Priestess, new BestiaryBonus(ChessPieceType.Priestess, BestiaryBonusType.MaxHealthPercent,     0.10f) }, // +10% max HP
            { ChessPieceType.Dog,       new BestiaryBonus(ChessPieceType.Dog,       BestiaryBonusType.MovementRangeFlat,    1f)    }, // +1 ruchu
            { ChessPieceType.Ogre,      new BestiaryBonus(ChessPieceType.Ogre,      BestiaryBonusType.AttackRangeFlat,      1f)    }, // +1 zasiêgu ataku
        };

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

    public IEnumerable<BestiaryBonus> GetActiveBonuses()
    {
        foreach (var t in defeatedTypes)
            if (_bonusMap.TryGetValue(t, out var b))
                yield return b;
    }

    public BestiaryBonusTotals GetTotals()
    {
        var totals = new BestiaryBonusTotals();
        foreach (var b in GetActiveBonuses())
        {
            switch (b.type)
            {
                case BestiaryBonusType.AttackPercent: totals.attackPct += b.value; break;
                case BestiaryBonusType.MaxHealthPercent: totals.maxHealthPct += b.value; break;
                case BestiaryBonusType.MovementRangeFlat: totals.movementAdd += Mathf.RoundToInt(b.value); break;
                case BestiaryBonusType.MaxMovementRangeFlat: totals.maxMovementAdd += Mathf.RoundToInt(b.value); break;
                case BestiaryBonusType.AttackRangeFlat: totals.attackRangeAdd += Mathf.RoundToInt(b.value); break;
            }
        }
        return totals;
    }

    public string GetBonusDescription(ChessPieceType type)
    {
        if (!_bonusMap.TryGetValue(type, out var b))
            return "Brak bonusu.";

        switch (b.type)
        {
            case BestiaryBonusType.AttackPercent: return $"+{Mathf.RoundToInt(b.value * 100)}% do ataku";
            case BestiaryBonusType.MaxHealthPercent: return $"+{Mathf.RoundToInt(b.value * 100)}% do zdrowia";
            case BestiaryBonusType.MovementRangeFlat: return $"+{Mathf.RoundToInt(b.value)} do ruchu";
            case BestiaryBonusType.MaxMovementRangeFlat: return $"+{Mathf.RoundToInt(b.value)} do maks. ruchu";
            case BestiaryBonusType.AttackRangeFlat: return $"+{Mathf.RoundToInt(b.value)} do zasiêgu ataku";
            default: return "Bonus";
        }
    }
}
