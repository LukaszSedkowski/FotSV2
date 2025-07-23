using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SkillNode : MonoBehaviour
{
    public Button button;
    public List<SkillNode> children;

    private bool unlocked = false;
    private List<GameObject> lines = new List<GameObject>();

    void Start()
    {
        button.onClick.AddListener(OnClick);
        button.interactable = false;

        DrawLines();
    }

    public void Unlock()
    {
        unlocked = true;
        button.interactable = true;
    }

    void OnClick()
    {
        if (!unlocked)
            return;

        button.interactable = false;

        foreach (SkillNode child in children)
        {
            child.Unlock();
        }
    }

    void DrawLines()
    {
        foreach (SkillNode child in children)
        {
            CreateLineBetween(button.transform as RectTransform, child.button.transform as RectTransform);
        }
    }

    void CreateLineBetween(RectTransform start, RectTransform end)
{
    GameObject line = new GameObject("Line");
    line.transform.SetParent(transform.parent);
    line.transform.SetSiblingIndex(0); // <-- TU zmiana, linia będzie pod przyciskami

    Image img = line.AddComponent<Image>();
    img.color = Color.white;

    RectTransform rt = line.GetComponent<RectTransform>();
    rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
    rt.pivot = new Vector2(0, 0.5f);

    Vector3 startPos = start.position;
    Vector3 endPos = end.position;

    Vector3 direction = endPos - startPos;
    float distance = direction.magnitude;

    rt.sizeDelta = new Vector2(distance, 2f);
    rt.position = startPos;
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    rt.rotation = Quaternion.Euler(0, 0, angle);

    lines.Add(line);
}

}
