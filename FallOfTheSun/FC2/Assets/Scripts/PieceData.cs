using System;

[Serializable]
public class PieceData
{
    public ChessPieceType type;
    public int team;
    public int id;
    public int currentX;
    public int currentY;
    public int health;
    public int maxHealth;
    public int movementRange;
    public int maxMovementRange;
    public int attack;
    public int attackRange;
    public int attackCost;
    public float groundOffset;
    public bool hasPassiveAbility;
    public int visionRange;
}
