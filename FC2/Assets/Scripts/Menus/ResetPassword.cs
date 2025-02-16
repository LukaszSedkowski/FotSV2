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
            passwordValidText.text = "Has�o nie jest takie samo.";

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
            Debug.Log("Nie zmieniono chas�a");
            passwordValidText.text = "Co� posz�o nie tak! Has�o nie zosta�o zmienione.";
        }
    }
    private bool IsValidPassword()
    {
        string password = passwordField.text.Replace("\u200B", "").Trim();
        Debug.Log(password.Length);
        if (string.IsNullOrEmpty(password))
            return false;

        // Sprawdzanie d�ugo�ci
        if (password.Length < 8)
            return false;

        // Wyra�enie regularne sprawdzaj�ce:
        // - co najmniej 1 ma�� liter�
        // - co najmniej 1 wielk� liter�
        // - co najmniej 1 cyfr�
        // - co najmniej 1 znak specjalny
        string passwordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

        return Regex.IsMatch(password, passwordRegex);
    }
    public void RepeatPasswordValid()
    {
        if (passwordField.text.Replace("\u200B", "").Trim() != repeatPasswordField.text.Replace("\u200B", "").Trim())
        {
            passwordValidText.text = "Has�a nie s� takie same";
        }
        else
        {
            passwordValidText.text = "";
        }
    }
}
