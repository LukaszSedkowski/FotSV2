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
        visionRange = (attackRange + maxMovementRange) / 2;
    }

    public override void TriggerPassiveAbility()
    {
        health += 30;
        Debug.Log($"Wampir wzmacnia siê. Aktualne zdrowie {health}");
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
                var vampir = (Vampir)user;
                vampir.health = Mathf.Min(vampir.health + vampir.healAmount, vampir.maxHealth);
                lightDarkness.ChangeLightDarkLevel(5);
                ChangeLightDarkHealth(-4);
                Debug.Log($"{vampir.type} healed for {vampir.healAmount} HP.");
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
                var vampir = (Vampir)user;
                vampir.strongStrikeActive = true;
                lightDarkness.ChangeLightDarkLevel(5);
                ChangeLightDarkHealth(-4);
                Debug.Log($"{vampir.type} empowered next attack by {vampir.extraDamage} damage.");
            }
        ));
    }
}