using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

public class Dog : ChessPieces
{
    protected override void SetStats()
    {
        movementRange = 25;
        maxMovementRange = 25;
        health = 30;
        maxHealth = 30;
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
                var dog = (Dog)user;
                dog.health = Mathf.Min(dog.health + dog.healAmount, dog.maxHealth);
                lightDarkness.ChangeLightDarkLevel(-5);
                ChangeLightDarkHealth(-4);

                Debug.Log($"{dog.type} healed for {dog.healAmount} HP.");
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
        // --- Granat (AoE) ---
        var grenade = new AreaAbility("Grenade", null, radius: 2, castRange: 6, dmg: 25f)
        {
            movementCost = 5,
            ldLevelCost = -5,
            ldHealthCost = -4,
            affectAllies = false,
            damageFalloff = true
        };

        // ExecuteAction ma tylko w³¹czyæ tryb celowania
        grenade.ExecuteAction = user =>
        {
            var board = Object.FindAnyObjectByType<ChessBoard>();
            if (board == null) { Debug.LogError("[AoE] ChessBoard not found"); return; }
            board.BeginTargeting(grenade, user);
        };

        abilities.Add(grenade);

    }
}