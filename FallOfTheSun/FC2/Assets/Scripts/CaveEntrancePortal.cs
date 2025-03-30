using UnityEngine;
using UnityEngine.SceneManagement;

public class PortalTransfer : MonoBehaviour
{
    [SerializeField] private string targetSceneName;

    private bool isLoading = false;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Portal triggered by: " + other.name);

        if (isLoading) return;

        // Przyk³adowo, jeœli pionek ma komponent ChessPieces – zapisujemy jego stan
        ChessPieces cp = other.GetComponent<ChessPieces>();
        if (cp != null)
        {
            // Stwórz obiekt danych dla tego pionka
            PieceData data = new PieceData();
            data.type = cp.type;
            data.team = cp.team;
            data.id = cp.Id;
            data.currentX = cp.currentX;
            data.currentY = cp.currentY;
            data.health = cp.health;
            data.maxHealth = cp.maxHealth;
            data.movementRange = cp.movementRange;
            data.maxMovementRange = cp.maxMovementRange;
            data.attack = cp.attack;
            data.attackRange = cp.attackRange;
            data.attackCost = cp.attackCost;
            data.groundOffset = cp.groundOffset;
            data.hasPassiveAbility = cp.hasPassiveAbility;
            data.visionRange = cp.visionRange;

            // Dodaj dane do listy (mo¿esz te¿ zapisaæ tylko pionki gracza)
            GameManager.Instance.transferredPieces.Add(data);

        }
        else
        {
            Debug.Log("Obiekt " + other.name + " nie ma komponentu ChessPieces.");
        }

        isLoading = true;
        Debug.Log("£adowanie sceny: " + targetSceneName);

        SceneManager.LoadScene(targetSceneName);
    }
}
