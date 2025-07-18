using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Hunter = 1,
    Priestess = 2,
    Dog=3,
    Knight=4,
    Ogre=5,
    Skeleton=6,
    Vampir=7,
    Werewolf=8
}
public enum ElementType
{
    Light,
    Dark
}

public class ChessPieces : MonoBehaviour
{

    public FogOfWarManager fogOfWarManager;
    public LightDarknessManager lightDarkness;
    [Header("Active Abilities")]
    public List<Ability> abilities = new List<Ability>();

    public bool isMoving = false;

    public int team;               // Drużyna (0 lub 1)
    public int currentX;           // Aktualna pozycja X
    public int currentY;           // Aktualna pozycja Y
    public ChessPieceType type;    // Typ pionka
    public ElementType elementType;
    public int Id { get; private set; } // Unikalne ID pionka
      public int movementRange;
      public int maxMovementRange;  // Maksymalny zasięg ruchu
    public float health;
    public float maxHealth;
    public float attack;
    public int attackRange;
    public int attackCost;
    public float groundOffset = 0.5f;
    public int visionRange = 5;


    public bool strongStrikeActive { get; set; }
    public int healAmount = 20;
    public int extraDamage = 30;

    public bool hasPassiveAbility;

    private Vector3 desiredPosition;
    private Vector3 desiredScale;

    private void Start()
    {
        lightDarkness = FindAnyObjectByType<LightDarknessManager>();
    }
    // Metoda inicjalizacyjna
    public void Init(ChessPieceType type, int team, int id)
    {
        
        this.type = type;
        this.team = team;
        this.Id = id;
        SetStats(); // Ustawienie specyficznych statystyk
        Debug.Log($"Initialized piece {Id} of type {type} with movement range {movementRange} and health {health}");
    }

    // Metoda do ustawienia statystyk – będzie nadpisywana w podklasach
    protected virtual void SetStats()
    {
        health = 100;
        attack=20;
    }

    public virtual void TriggerPassiveAbility()
    {
    }
    public void UseAbility(int index)
    {
        Debug.Log($"[UseAbility] Called on {this.type} with index={index}");
        if (index >= 0 && index < abilities.Count)
        {
            Debug.Log($"[UseAbility] Executing \"{abilities[index].abilityName}\"");
            abilities[index].ExecuteAction(this);
        }
        else
        {
            Debug.LogWarning($"[UseAbility] Invalid ability index {index} on {this.type}");
        }
    }
    public bool IsVisibleToPlayer()
    {
        return !fogOfWarManager.fogTiles[currentX, currentY].activeSelf;
    }
    public float GetBonus(ElementType type)
    {
        if (type == ElementType.Light)
        {
            return lightDarkness.lightBonus;
        }
        if (type == ElementType.Dark)
        {
            return lightDarkness.darknessBonus;
        }
        return 1;
    }
    public bool ConsumeStrongStrike()
    {
        if (!strongStrikeActive) return false;
        strongStrikeActive = false;
        return true;
    }
}
