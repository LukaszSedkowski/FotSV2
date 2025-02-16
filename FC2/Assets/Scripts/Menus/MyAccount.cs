using TMPro;
using UnityEngine;

public class MyAccount : MonoBehaviour
{
    public GameObject iconeImage;
    public TextMeshProUGUI nickField;
    public TextMeshProUGUI emailField;
    void Start()
    {
        if (Menus.Instance != null && Menus.Instance.User != null)
        {
            nickField.text = Menus.Instance.User.GetNick();
            emailField.text = Menus.Instance.User.GetEmail();
        }
        else
        {
            Debug.LogError("Menus.Instance lub Menus.Instance.User jest null.");
            nickField.text = "Brak danych";
            emailField.text = "Brak danych";
        }
    }
    public void LogoutButton()
    {
        Menus.Instance.IsLoggedIn = false;
        Menus.Instance.myAccount.SetActive(false);
        Menus.Instance.mainMenu.SetActive(true);
    }
}
