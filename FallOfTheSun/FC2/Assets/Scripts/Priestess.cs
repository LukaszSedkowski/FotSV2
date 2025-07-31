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
                lightDarkness.ChangeLightDarkLevel(-5);
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
                lightDarkness.ChangeLightDarkLevel(-5);
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
                lightDarkness.ChangeLightDarkLevel(-5);
                Debug.Log($"{hunter.type} empowered next attack by {hunter.extraDamage} damage.");
            }
        ));
    }
}
