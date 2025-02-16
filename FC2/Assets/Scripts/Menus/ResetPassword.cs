using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class ResetPassword : MonoBehaviour
{
    public TextMeshProUGUI passwordField;
    public TextMeshProUGUI repeatPasswordField;
    public TextMeshProUGUI passwordValidText;

    public bool SamePassword()
    {
        return (!string.IsNullOrEmpty(passwordField.text) && passwordField.text == repeatPasswordField.text);
    }
    public void ValidMessage()
    {
        if (!SamePassword())
        {
            passwordValidText.text = "Has³o nie jest takie samo.";

        }
    }

    public void ChangPassword()
    {
        if (SamePassword() && IsValidPassword())
        {
            Menus.Instance.User.SetPassword(passwordField.text.Replace("\u200B", "").Trim());
            Menus.Instance.myAccount.SetActive(true);
            Menus.Instance.resetPassword.SetActive(false);
        }
        else
        {
            Debug.Log("Nie zmieniono chas³a");
            passwordValidText.text = "Coœ posz³o nie tak! Has³o nie zosta³o zmienione.";
        }
    }
    private bool IsValidPassword()
    {
        string password = passwordField.text.Replace("\u200B", "").Trim();
        Debug.Log(password.Length);
        if (string.IsNullOrEmpty(password))
            return false;

        // Sprawdzanie d³ugoœci
        if (password.Length < 8)
            return false;

        // Wyra¿enie regularne sprawdzaj¹ce:
        // - co najmniej 1 ma³¹ literê
        // - co najmniej 1 wielk¹ literê
        // - co najmniej 1 cyfrê
        // - co najmniej 1 znak specjalny
        string passwordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

        return Regex.IsMatch(password, passwordRegex);
    }
    public void RepeatPasswordValid()
    {
        if (passwordField.text.Replace("\u200B", "").Trim() != repeatPasswordField.text.Replace("\u200B", "").Trim())
        {
            passwordValidText.text = "Has³a nie s¹ takie same";
        }
        else
        {
            passwordValidText.text = "";
        }
    }
}
