using UnityEngine;
using UnityEngine.UI;

public class SpeedControl : MonoBehaviour
{
    public PlayerMovement playerMovement; // przypisz w Inspectorze
    public Button increaseSpeedButton;

    void Start()
    {
        increaseSpeedButton.onClick.AddListener(OnIncreaseSpeedClicked);
    }

    void OnIncreaseSpeedClicked()
    {
        if (playerMovement != null)
        {
            playerMovement.speed=playerMovement.speed+10000f;
        }
    }
}
