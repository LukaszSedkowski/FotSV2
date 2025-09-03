using UnityEngine;

public enum BestiaryBonusType
{
    AttackPercent,        // np. +10% do ataku
    MaxHealthPercent,     // np. +10% do max HP (i skaluje current HP)
    MovementRangeFlat,    // np. +1 do movementRange
    MaxMovementRangeFlat, // np. +1 do maxMovementRange
    AttackRangeFlat       // np. +1 do attackRange (zasiêg ataku)
}

[System.Serializable]
public struct BestiaryBonus
{
    public ChessPieceType sourceType;   // który typ wroga odblokowa³ bonus
    public BestiaryBonusType type;      // jaki rodzaj bonusu
    public float value;                 // wartoœæ: 0.10f = +10% ; 1f = +1 (flat)

    public BestiaryBonus(ChessPieceType src, BestiaryBonusType t, float v)
    {
        sourceType = src;
        type = t;
        value = v;
    }
}

public struct BestiaryBonusTotals
{
    public float attackPct;      // kumulowany %
    public float maxHealthPct;   // kumulowany %
    public int movementAdd;      // kumulowane flat
    public int maxMovementAdd;   // kumulowane flat
    public int attackRangeAdd;   // kumulowane flat

    public override string ToString()
    {
        return $"attack%={attackPct:P0}, hp%={maxHealthPct:P0}, move+={movementAdd}, maxMove+={maxMovementAdd}, atkRange+={attackRangeAdd}";
    }
}
