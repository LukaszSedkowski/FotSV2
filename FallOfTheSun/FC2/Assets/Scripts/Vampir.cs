using UnityEngine;

public class Vampir : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 15;
        maxMovementRange = 15;
        health = 120;
        maxHealth = 120;
        attack = 30;
        attackRange = 1;
        attackCost = 5;
    }

    public override void TriggerPassiveAbility()
    {
        health += 30;
        Debug.Log($"Wampir wzmacnia siê. Aktualne zdrowie {health}");
    }
}