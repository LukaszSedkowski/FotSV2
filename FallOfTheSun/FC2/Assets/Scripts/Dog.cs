using UnityEngine;

public class Dog : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 25;
        maxMovementRange = 25;
        health = 30;
        maxHealth=30;
        attack = 10;
        attackRange = 1;
        attackCost = 5;
    }
}