using UnityEngine;

public class PanelSwitcher : MonoBehaviour
{
    public GameObject defaultPanel;
    public GameObject panel1;
    public GameObject panel2;

    public void ShowDefaultPanel()
    {
        defaultPanel.SetActive(true);
        panel1.SetActive(false);
        panel2.SetActive(false);
    }

    public void ShowPanel1()
    {
        defaultPanel.SetActive(false);
        panel1.SetActive(true);
        panel2.SetActive(false);
    }

    public void ShowPanel2()
    {
        defaultPanel.SetActive(false);
        panel1.SetActive(false);
        panel2.SetActive(true);
    }
}
