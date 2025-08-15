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
                var priestess = (Priestess)user;
                priestess.health = Mathf.Min(priestess.health + priestess.healAmount, priestess.maxHealth);
                lightDarkness.ChangeLightDarkLevel(-5);
                ChangeLightDarkHealth(-4);
                Debug.Log($"{priestess.type} healed for {priestess.healAmount} HP.");
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
                ChangeLightDarkHealth(-4);
                Debug.Log($"{user.type} movement range reset to {user.maxMovementRange}.");
            }
        ));

        // 3) Mocniejszy cios
        abilities.Add(new Ability(
            "Strong Strike",
            null,
            user => {
                var priestess = (Priestess)user;
                priestess.strongStrikeActive = true;
                lightDarkness.ChangeLightDarkLevel(-5);
                ChangeLightDarkHealth(-4);
                Debug.Log($"{priestess.type} empowered next attack by {priestess.extraDamage} damage.");
            }
        ));
    }
}
