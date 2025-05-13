using System.Collections.Generic;
using UnityEngine;



public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Lista stanu pionk�w, kt�re przenosimy mi�dzy scenami
    public List<PieceData> transferredPieces = new List<PieceData>();
public GameMode CurrentGameMode = GameMode.MultiTeam;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("GameManager ustawiony jako Instance.");

    }
}
