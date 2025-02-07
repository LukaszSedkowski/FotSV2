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

public class ChessPieces : MonoBehaviour
{
    public int team;               // Drużyna (0 lub 1)
    public int currentX;           // Aktualna pozycja X
    public int currentY;           // Aktualna pozycja Y
    public ChessPieceType type;    // Typ pionka
    public int Id { get; private set; } // Unikalne ID pionka
      public int movementRange;
      public int maxMovementRange;  // Maksymalny zasięg ruchu
    public int health;
    public int maxHealth;
    public int attack;
    public int attackRange;
    public int attackCost;

    public bool hasPassiveAbility;

    private Vector3 desiredPosition;
    private Vector3 desiredScale;

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

}
