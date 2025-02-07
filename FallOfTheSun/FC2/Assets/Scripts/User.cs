using UnityEngine;

public class User
{
    int IconId = 0;
    string Nick;
    string Password;
    string Email;
    public User(string nick, string password, string email)
    {
        Nick = nick;
        Password = password;
        Email = email;
    }
    public string GetEmail()
    {
        return Email;
    }
    public string GetPassword()
    {
        return Password;
    }
    public string GetNick()
    {
        return Nick;
    }
    public void SetPassword(string password)
    {
        Password = password;
    }
}
