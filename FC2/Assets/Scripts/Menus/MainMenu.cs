using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;

public class MainMenu : MonoBehaviour
{
    public void Play()
    {

        if (Menus.Instance.IsLoggedIn)
        {
            Menus.Instance.gameMenu.SetActive(true);
            Menus.Instance.mainMenu.SetActive(false);
        }
        else
        {
            Menus.Instance.menu.SetActive(true);
            Menus.Instance.mainMenu.SetActive(false);

        }
    }
    public void MyAccount()
    {

        if (Menus.Instance.IsLoggedIn) 
        { 
        Menus.Instance.myAccount.SetActive(true);
        Menus.Instance.mainMenu.SetActive(false);
        }
        else
        {
            Menus.Instance.loginRegistration.SetActive(true);
            Menus.Instance.mainMenu.SetActive(false);
        }
    }
    public void Exit()
    {
        Application.Quit();
    }
    void DisableCamera()
    {
        Camera cameraToDisable = GameObject.Find("Main Camera").GetComponent<Camera>();
        if (cameraToDisable != null)
        {
            cameraToDisable.enabled = false;
        }
    }
}



