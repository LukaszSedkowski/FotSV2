using UnityEngine;
using UnityEngine.UI;

public class OptionsMenuBackButton : MonoBehaviour
{
    private void OnEnable()
    {
        Button btn = GetComponent<Button>();
        if (btn == null)
        {
            btn = GetComponentInChildren<Button>();
        }

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnBackClick);
        }
        else
        {
            Debug.LogError("[OptionsMenuBackButton] Nie znaleziono komponentu Button!");
        }
    }

    private void OnBackClick()
    {
        if (OptionsMenuManager.Instance != null)
        {
            OptionsMenuManager.Instance.CloseMenu();
            Debug.Log("[OptionsMenuBackButton] Zamkniêto menu opcji");
        }
        else
        {
            Debug.LogError("[OptionsMenuBackButton] Brak OptionsMenuManager.Instance!");
        }
    }
}
