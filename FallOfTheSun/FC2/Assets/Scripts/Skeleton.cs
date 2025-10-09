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
                var skeleton = (Skeleton)user;
                skeleton.health = Mathf.Min(skeleton.health + skeleton.healAmount, skeleton.maxHealth);
                lightDarkness.ChangeLightDarkLevel(5);
                ChangeLightDarkHealth(-4);
                Debug.Log($"{skeleton.type} healed for {skeleton.healAmount} HP.");
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
                ChangeLightDarkHealth(-4);
                Debug.Log($"{user.type} movement range reset to {user.maxMovementRange}.");
            }
        ));

        // 3) Mocniejszy cios
        abilities.Add(new Ability(
            "Strong Strike",
            null,
            user => {
                var skeleton = (Skeleton)user;
                skeleton.strongStrikeActive = true;
                lightDarkness.ChangeLightDarkLevel(5);
                ChangeLightDarkHealth(-4);
                Debug.Log($"{skeleton.type} empowered next attack by {skeleton.extraDamage} damage.");
            }
        ));
    }
}