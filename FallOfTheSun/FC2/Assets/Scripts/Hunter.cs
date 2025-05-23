using UnityEngine;

public class Hunter : ChessPieces
{
    public bool strongStrikeActive { get; set; }
    public int healAmount = 20;
    public int extraDamage = 30;
    protected override void SetStats()
    {
        movementRange = 10;
        maxMovementRange = 10;
        health = 200;
        maxHealth = 200;
        attack = 26;
        attackRange = 10;
        attackCost = 5;
        visionRange = (attackRange+maxMovementRange)/2;

    }
    public override void TriggerPassiveAbility()
    {
        attack += 5;
        Debug.Log($"£owca zwiêksza swoje obra¿enia. Aktualne obra¿enia {attack}");
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
                Debug.Log($"{hunter.type} empowered next attack by {hunter.extraDamage} damage.");
            }
        ));
    }
    public bool ConsumeStrongStrike()
    {
        if (!strongStrikeActive) return false;
        strongStrikeActive = false;
        return true;
    }
}
