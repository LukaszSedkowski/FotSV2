using UnityEngine;

public class GameDataBootstrap : MonoBehaviour
{
    private void Awake()
    {
        if (GameData.Instance == null)
        {
            var go = new GameObject("GameData");
            go.AddComponent<GameData>(); // Awake -> DontDestroyOnLoad
            Debug.Log("[Bootstrap] GameData created.");
        }
        else
        {
            Debug.Log("[Bootstrap] GameData already exists.");
        }
    }
}
