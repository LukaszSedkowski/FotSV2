using UnityEngine;

public class UIButtons : MonoBehaviour
{
    public CameraController cameraController;
    public TurnManager turnManager;
    public ChessBoard chessBoard;

    public void RotateLeft()
    {
        cameraController.RotateCamera(-90f);
    }

    public void RotateRight()
    {
        cameraController.RotateCamera(90f);
    }

    public void EndTurn()
    {
        if (chessBoard.currentlyDragging != null && chessBoard.currentlyDragging.isMoving)
        {
            Debug.Log("Nie mo¿esz zakoñczyæ tury przed zakoñczeniem ruchu");
            return;
        }

        if (!turnManager.isAIControlledTeam[turnManager.currentTeam])
        {
            turnManager.ChangeTurn();
        }
    }
}
