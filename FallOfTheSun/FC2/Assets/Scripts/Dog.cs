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
        visionRange = (attackRange + maxMovementRange) / 2;
    }
    void Awake()
    {
        // Usuñ stare umiejêtnoœci
        abilities.Clear();

        // 1) Leczenie
        abilities.Add(new Ability(
            "Heal",
            null,
            user =>
            {
                var hunter = (Hunter)user;
                hunter.health = Mathf.Min(hunter.health + hunter.healAmount, hunter.maxHealth);
                lightDarkness.ChangeLightDarkLevel(5);
                Debug.Log($"{hunter.type} healed for {hunter.healAmount} HP.");
            }
        ));

        // 2) Regeneracja ruchu
        abilities.Add(new Ability(
            "Regenerate Movement",
            null,
            user =>
            {
                user.movementRange = user.maxMovementRange;
                lightDarkness.ChangeLightDarkLevel(5);
                Debug.Log($"{user.type} movement range reset to {user.maxMovementRange}.");
            }
        ));

        // 3) Mocniejszy cios
        abilities.Add(new Ability(
            "Strong Strike",
            null,
            user => {
                var hunter = (Hunter)user;
                hunter.strongStrikeActive = true;
                lightDarkness.ChangeLightDarkLevel(5);
                Debug.Log($"{hunter.type} empowered next attack by {hunter.extraDamage} damage.");
            }
        ));
    }
}