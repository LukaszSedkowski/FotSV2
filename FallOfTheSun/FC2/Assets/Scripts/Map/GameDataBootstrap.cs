using UnityEngine;

public class GameDataBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData");
            go.AddComponent<GameData>();
            DontDestroyOnLoad(go);
            Debug.Log("[Bestiary][Bootstrap] Created GameData singleton.");
        }
        else
        {
            Debug.Log("[Bestiary][Bootstrap] GameData already exists.");
        }
    }
}
