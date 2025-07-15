using UnityEngine;

public class SkillTreeManager : MonoBehaviour
{
    public SkillNode rootNode; // <- tu przypisz pierwszy przycisk w Inspectorze

    void Start()
    {
        if (rootNode != null)
        {
            rootNode.Unlock(); // odblokuj pierwszy przycisk
        }
    }
}
