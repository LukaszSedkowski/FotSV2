using UnityEngine;

public class Hunter : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 10;
        maxMovementRange = 10;
        health = 200;
        maxHealth = 200;
        attack = 26;
        attackRange = 10;
        attackCost = 5;


    }
    public override void TriggerPassiveAbility()
    {
        attack += 10;
        Debug.Log($"£owca zwiêksza swoje obra¿enia. Aktualne obra¿enia {attack}");
    }

}
