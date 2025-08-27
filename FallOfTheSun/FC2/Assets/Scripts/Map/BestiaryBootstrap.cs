using UnityEngine;

public class BestiaryBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (BestiaryManager.Instance == null)
        {
            var go = new GameObject("BestiaryManager");
            go.AddComponent<BestiaryManager>();
            DontDestroyOnLoad(go);
            Debug.Log("[Bestiary][Bootstrap] Created BestiaryManager.");
        }
        else
        {
            Debug.Log("[Bestiary][Bootstrap] BestiaryManager already exists.");
        }
    }
}
