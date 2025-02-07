using UnityEngine;

public class Ogre : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 3;
        maxMovementRange = 3;
        health = 400;
        maxHealth = 400;
        attack = 130;
        attackRange = 1;
        attackCost = 1;
    }
}