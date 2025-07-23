using UnityEngine;

public class AttackManager : MonoBehaviour
{
    private LightDarknessManager lightDarkness;
    private static AttackManager _instance;
    private void Start()
    {
        lightDarkness = FindAnyObjectByType<LightDarknessManager>();
    }
    public static AttackManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindAnyObjectByType<AttackManager>();
            return _instance;
        }
    }
    public void AttackEnemyPiece(ChessPieces attacker, ChessPieces target, float[,] tileHeights, bool[,] obstacles, ChessPieces[,] chessPieces)
    {

        // Sprawdzenie, czy cel to przeciwnik
        if (target.team != attacker.team)
        {
            // Weryfikacja, czy mamy wystarczaj¹co ruchu na atak
            if (attacker.movementRange < attacker.attackCost)
            {
                Debug.Log("Za ma³o ruchu, aby wykonaæ atak.");
                return;
            }

            // Obliczanie odleg³oœci z uwzglêdnieniem ró¿nicy wysokoœci
            float distance = Mathf.Sqrt(
                Mathf.Pow(attacker.currentX - target.currentX, 2) +
                Mathf.Pow(attacker.currentY - target.currentY, 2) +
                Mathf.Pow(tileHeights[attacker.currentX, attacker.currentY] - tileHeights[target.currentX, target.currentY], 2)
            );
            distance = Mathf.Round(distance * 100f) / 100f;

            // Sprawdzenie, czy cel jest w zasiêgu
            if (distance <= attacker.attackRange)
            {
                // (opcjonalnie) jeszcze raz upewniamy siê, ¿e nie atakujemy poza zasiêgiem ukoœnym
                if (distance > attacker.attackRange)
                {
                    Debug.Log($"Cel poza zasiêgiem ataku. Odleg³oœæ: {distance}");
                    return;
                }

                // Sprawdzamy przeszkody i ewentualnie zmniejszamy damage
                bool isNearObstacle = false;
                int[] dx = { 1, -1, 0, 0 };
                int[] dy = { 0, 0, 1, -1 };
                for (int i = 0; i < 4; i++)
                {
                    int checkX = target.currentX + dx[i];
                    int checkY = target.currentY + dy[i];

                    if (checkX >= 0 && checkY >= 0 && checkX < obstacles.GetLength(0) && checkY < obstacles.GetLength(1))
                    {
                        if (obstacles[checkX, checkY])
                        {
                            isNearObstacle = true;
                            break;
                        }
                    }
                }
                bool isObstacleBetween = IsObstacleBetween(attacker, target, obstacles);

                // Podstawowe obra¿enia
                float damage = attacker.attack * attacker.GetBonus(attacker.elementType);

                // *** TU DODAJEMY STRONG STRIKE BONUS ***
                if (attacker is Hunter hunter && hunter.ConsumeStrongStrike())
                {
                    damage += hunter.extraDamage;
                    Debug.Log($"{hunter.type} uses Strong Strike! +{hunter.extraDamage} bonus damage.");
                }

                // Obni¿enie obra¿eñ za przeszkodê, jeœli oba warunki
                if (isNearObstacle && isObstacleBetween)
                {
                    damage = Mathf.Max(damage - 4, 0);
                    Debug.Log("Obra¿enia zmniejszone o 4 z powodu przeszkody.");
                }

                // Zastosowanie obra¿eñ
                target.health -= damage;
                Debug.Log($"Zaatakowano pionek przeciwnika. Zadano {damage} obra¿eñ. Pozosta³e zdrowie: {target.health}. Zasiêg: {distance}");

                // Zu¿ycie ruchu i ewentualne pasywki
                attacker.movementRange -= attacker.attackCost;
                attacker.TriggerPassiveAbility();
///HighlightPossibleMoves(attacker);

                // Usuniêcie pionka, jeœli zdrowie <= 0
                if (target.health <= 0)
                {
                    Destroy(target.gameObject);
                    chessPieces[target.currentX, target.currentY] = null;
                    Debug.Log("Pionek przeciwnika zosta³ zniszczony.");
                }
            }
            else
            {
                Debug.Log("Cel jest poza zasiêgiem ataku.");
            }
        }
    }
    private bool IsObstacleBetween(ChessPieces attacker, ChessPieces target, bool[,] obstacles)
    {
        int x1 = attacker.currentX;
        int y1 = attacker.currentY;
        int x2 = target.currentX;
        int y2 = target.currentY;

        // Obliczamy ró¿nice w wspó³rzêdnych
        int dx = Mathf.Abs(x2 - x1);
        int dy = Mathf.Abs(y2 - y1);

        // Iterujemy po linii miêdzy dwoma punktami
        int sx = (x1 < x2) ? 1 : -1;
        int sy = (y1 < y2) ? 1 : -1;
        int err = dx - dy;

        while (x1 != x2 || y1 != y2)
        {
            // Sprawdzamy, czy na tym polu jest przeszkoda (pomijaj¹c cel)
            if (obstacles[x1, y1] && !(x1 == target.currentX && y1 == target.currentY))
            {
                return true; // Znaleziono przeszkodê na drodze
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x1 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y1 += sy;
            }
        }

        return false; // Nie znaleziono przeszkód
    }

}
