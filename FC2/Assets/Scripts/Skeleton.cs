using UnityEngine;

public class Skeleton : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 10;
        maxMovementRange = 10;
        health = 100;
        maxHealth = 100;
        attack = 50;
        attackRange = 6;
        attackCost = 5;
    }
}