using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [TextArea]
    public string tooltipText; // Tekst ustawiany w inspektorze

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipUI.Instance.Show(tooltipText);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipUI.Instance.Hide();
    }
}
