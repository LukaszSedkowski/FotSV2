using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Newtonsoft.Json;

public class LoginForm : MonoBehaviour
{
    public TextMeshProUGUI loginEmailField;
    public TextMeshProUGUI loginPasswordField;
    public TextMeshProUGUI validMessage;

    private string apiUrl = "https://localhost:7188/api/Auth/login";
    private string jwtToken;

    public void Login()
    {
        if (string.IsNullOrWhiteSpace(loginEmailField.text) || string.IsNullOrWhiteSpace(loginPasswordField.text))
        {
            validMessage.text = "Wype³nij wszystkie pola!";
            validMessage.color = Color.red;
            return;
        }

        StartCoroutine(SendLoginRequest());
    }

    private IEnumerator SendLoginRequest()
    {
        var requestData = new LoginRequest
        {
            Email = RemoveZeroWidthSpace(loginEmailField.text.Trim()),
            Password = RemoveZeroWidthSpace(loginPasswordField.text.Trim())
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        Debug.Log("Wysy³any JSON: " + jsonData);

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var response = request.downloadHandler.text;
            Debug.Log($"OdpowiedŸ z serwera: {response}");

            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(response);
            jwtToken = loginResponse.Token;

            PlayerPrefs.SetString("JwtToken", jwtToken);
            PlayerPrefs.Save();

            validMessage.text = "Zalogowano pomyœlnie!";
            validMessage.color = Color.green;
            yield return GetUserInfo();

            yield return new WaitForSeconds(2);

            Menus.Instance.IsLoggedIn = true;
            Menus.Instance.mainMenu.SetActive(true);
            Menus.Instance.loginForm.SetActive(false);
        }
        else
        {
            Debug.LogError($"B³¹d logowania: {request.responseCode} - {request.downloadHandler.text}");
            validMessage.text = request.responseCode == 401 ?
                "Nieprawid³owy e-mail lub has³o." :
                "Wyst¹pi³ b³¹d logowania.";
            validMessage.color = Color.red;
        }
    }

    private string RemoveZeroWidthSpace(string input)
    {
        return input.Replace("\u200B", "");
    }
    private IEnumerator GetUserInfo()
    {
        UnityWebRequest request = new UnityWebRequest("https://localhost:7188/info", "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", $"Bearer {jwtToken}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var userInfo = JsonConvert.DeserializeObject<UserInfoResponse>(request.downloadHandler.text);

            Menus.Instance.User = new User(userInfo.Username, "", userInfo.Email);  
            Debug.Log($"Pobrano dane u¿ytkownika: {userInfo.Username}, {userInfo.Email}");
        }
        else
        {
            Debug.LogError($"Nie uda³o siê pobraæ danych u¿ytkownika: {request.error}");
        }
    }
}

public class LoginRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

[System.Serializable]
public class LoginResponse
{
    public string Token;
    public string Username;
}

[System.Serializable]
public class UserInfoResponse
{
    public string Username;
    public string Email;
}
