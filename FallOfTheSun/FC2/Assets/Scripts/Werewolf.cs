using UnityEngine;

public class Werewolf : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 15;
        maxMovementRange = 15;
        health = 150;
        maxHealth = 150;
        attack = 30;
        attackRange = 1;
        attackCost = 5;
    }
    public override void TriggerPassiveAbility()
    {
        attack += 20;
        maxMovementRange += 3;
        Debug.Log($"Wilko³ak zwiêksza swoje obra¿enia oraz zasiêg ruchu. Aktualne obra¿enia {attack} i maksymalny zasiêg ruchu {maxMovementRange}");
    }
}