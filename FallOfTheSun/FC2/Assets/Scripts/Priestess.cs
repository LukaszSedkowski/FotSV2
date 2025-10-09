using UnityEngine;

public class Priestess : ChessPieces
{
    protected override void SetStats()
    {
        base.SetStats();                // USTAWI LDHealth = 100 (z klasy bazowej)
        LDMaxHealth = 100;              // jawnie
        LDHealth = LDMaxHealth;      // jawnie

        movementRange = 3;
        maxMovementRange = 3;
        health = 80;
        maxHealth = 80;
        attack = 5;
        attackRange = 1;
        attackCost = 1;
        visionRange = (attackRange + maxMovementRange) / 2;
        elementType = ElementType.Light; // doprecyzowanie
    }
    void Awake()
    {
        // Usuñ stare umiejêtnoœci
        abilities.Clear();

        // 1) Leczenie obszarowe
        var healingZone = new PersistentHealZoneAbility(
    name: "Sanctuary",
    icon: null,
    radius: 2,
    castRange: 6,
    hpt: 12f,
    duration: 3
)
        {
            movementCost = 5,
            ldLevelCost = -2,
            ldHealthCost = -2
        };

        healingZone.ExecuteAction = user =>
        {
            var board = Object.FindAnyObjectByType<ChessBoard>();
            if (board == null) { Debug.LogError("[HealZone] ChessBoard not found"); return; }
            board.BeginTargeting(healingZone, user);
        };

        abilities.Add(healingZone);

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
