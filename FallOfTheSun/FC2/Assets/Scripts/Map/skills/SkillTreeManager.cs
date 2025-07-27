using UnityEngine;
using System.Collections;

public class SkillTreeManager : MonoBehaviour
{
    public SkillNode rootNode; // <- tu przypisz pierwszy przycisk w Inspectorze

    IEnumerator Start()
    {
        if (rootNode != null)
        {
            rootNode.Unlock(); // odblokuj pierwszy przycisk
            yield return null; // poczekaj jedną klatkę, aż wszystkie linie się narysują
            rootNode.ForceClick(); // teraz kliknij – linie będą już istnieć
        }
    }
}
