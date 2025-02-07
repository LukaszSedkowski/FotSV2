using UnityEngine;

public class Priestess : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 3;
        maxMovementRange= 3;
        health = 80;
        maxHealth = 80;
        attack = 5;
        attackRange = 1;
        attackCost = 1;
    }
}
