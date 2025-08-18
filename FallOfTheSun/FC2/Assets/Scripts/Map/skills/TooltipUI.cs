using UnityEngine;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance; // Singleton do łatwego wywoływania
    public GameObject panel;           // Panel tooltipa
    public TextMeshProUGUI textElement; // Tekst w tooltipie
    public Vector2 offset = new Vector2(0, 150); // Odległość od myszki (lewa strona)

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    private void Update()
    {
        if (panel.activeSelf)
        {
            Vector2 mousePos = Input.mousePosition;
            panel.transform.position = mousePos + offset;
        }
    }

    public void Show(string message)
    {
        textElement.text = message;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}
