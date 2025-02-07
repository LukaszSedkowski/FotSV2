using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;



public class RegistrationForm : MonoBehaviour
{

    public TextMeshProUGUI nickField;
    public TextMeshProUGUI emailField;
    public TextMeshProUGUI passwordField;
    public TextMeshProUGUI repeatPasswordField;
    public TextMeshProUGUI emailValidationField;
    public TextMeshProUGUI passwordValidationField;
    public TextMeshProUGUI responseMessageField;

    private string apiUrl = "https://localhost:7188/api/Auth/register";

    private string CleanInput(string input)
    {
        return input.Replace("\u200B", "").Trim();
    }

    public void EmailValidationMessage()
    {
        string email = emailField.text;
        if (IsValidEmail(email))
        {
            emailValidationField.text = "E-mail poprawny!";
            emailValidationField.color = Color.green;
        }
        else
        {
            emailValidationField.text = "E-mail niepoprawny";
            emailValidationField.color = Color.red;
        }
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;

        string emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, emailRegex);
    }
    public void Registration()
    {
        string nick = CleanInput(nickField.text);
        string email = CleanInput(emailField.text);
        string password = CleanInput(passwordField.text);
        string repeatPassword = CleanInput(repeatPasswordField.text);

        if (!string.IsNullOrEmpty(email) && IsValidEmail(email)
            && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(repeatPassword)
            && password == repeatPassword && !string.IsNullOrEmpty(nick))
        {
            StartCoroutine(SendRegistrationRequest(nick, email, password));
        }
        else
        {
            responseMessageField.text = "Wszystkie pola muszπ byÊ wype≥nione i poprawne.";
        }
    }

    private IEnumerator SendRegistrationRequest(string nick, string email, string password)
    {
        var requestData = new
        {
            Username = nick,
            Email = email,
            Password = password,
            RepeatPassword = password
        };
        string jsonData = JsonConvert.SerializeObject(requestData);
        Debug.Log("Wysy≥any JSON: " + jsonData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Odpowiedü serwera: " + request.downloadHandler.text);
            responseMessageField.text = "Uøytkownik zosta≥ pomyúlnie zarejestrowany. Sprawdü e-mail.";
            responseMessageField.color = Color.green;
        }
        else
        {
            Debug.LogError("B≥πd rejestracji: " + request.error);
            responseMessageField.text = $"B≥πd: {request.responseCode}\n{request.downloadHandler.text}";
            responseMessageField.color = Color.red;
        }
    }

    private bool IsValidPassword()
    {
        string password = passwordField.text.Replace("\u200B", "").Trim();
        Debug.Log(password.Length);
        if (string.IsNullOrEmpty(password))
            return false;

        // Sprawdzanie d≥ugoúci
        if (password.Length < 8)
            return false;

        // Wyraøenie regularne sprawdzajπce:
        // - co najmniej 1 ma≥π literÍ
        // - co najmniej 1 wielkπ literÍ
        // - co najmniej 1 cyfrÍ
        // - co najmniej 1 znak specjalny
        string passwordRegex = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";

        return Regex.IsMatch(password, passwordRegex);
    }
    public void RepeatPasswordValid()
    {
        if (passwordField.text.Replace("\u200B", "").Trim() != repeatPasswordField.text.Replace("\u200B", "").Trim())
        {
            passwordValidationField.text = "Has≥a nie sπ takie same";
        }
        else
        {
            passwordValidationField.text = "";
        }
    }
    public void PasswordValidText()
    {
        if (!IsValidPassword())
        {
            passwordValidationField.text = "Ha≥o powinno mieÊ co najmneij jednπ cyfrÍ, znak specjalny, ma≥π i duøπ literÍ.";
            passwordValidationField.color = Color.red;
        }
        else
        {
            passwordValidationField.text = "";
        }
    }
}
