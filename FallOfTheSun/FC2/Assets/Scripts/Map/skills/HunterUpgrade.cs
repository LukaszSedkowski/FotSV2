using UnityEngine;
using UnityEngine.UI;

public class HunterUpgrade : MonoBehaviour
{
    public Hunter hunter; // przypisz w Inspectorze
    public Button increaseAttackButton;

    void Start()
    {
        increaseAttackButton.onClick.AddListener(OnIncreaseAttackClicked);
    }

    void OnIncreaseAttackClicked()
    {
        if (hunter != null)
        {
            hunter.attack += 10000;
            Debug.Log($"{hunter.type} attack increased to {hunter.attack}");
        }
    }
}
