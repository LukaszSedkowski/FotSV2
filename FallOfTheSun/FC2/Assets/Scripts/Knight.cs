using UnityEngine;

public class Knight : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 6;
        maxMovementRange = 6;
        health = 400;
        maxHealth = 400;
        attack = 40;
        attackRange = 1;
        attackCost = 2;
    }
    public override void TriggerPassiveAbility()
    {
        maxHealth += 10;
        health += 10;
        Debug.Log($"Rycerz zwiêksza swoje max zdrowie i zdrowie. Aktualne max zdrowie {maxHealth} i zdrowie {health}");
    }
}