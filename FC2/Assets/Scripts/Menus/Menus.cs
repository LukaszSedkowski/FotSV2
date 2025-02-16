#nullable enable
using UnityEngine;

public  class Menus : MonoBehaviour
{
    public static Menus Instance { get; private set; }
    public bool IsLoggedIn = false;
    public User ?User = new User("ABC", "a", "a@a.a");
    public  GameObject menu;
    public  GameObject mainMenu;
    public  GameObject loginForm;
    public  GameObject registrationForm;
    public  GameObject myAccount;
    public  GameObject loginRegistration;
    public  GameObject resetPassword;
    public  GameObject gameMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Usuwa duplikat
        }
        else
        {
            Instance = this;
        }
    }
}
