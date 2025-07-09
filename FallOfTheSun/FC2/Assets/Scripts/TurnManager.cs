using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.VFX;

public class TurnManager : MonoBehaviour
{
    private PieceManager pieceManager;
    private TeamPanel teamPanel;
    private HighlightManager highlightManager;
    private FogOfWarManager fogOfWarManager;
    private ChessBoard chessBoard;

    public int currentTeam = 0; // Aktualna dru¿yna (zaczynamy od dru¿yny 0)
    public bool[] isAIControlledTeam;
    private int numberOfTeams; // Przyk³adowo, ustawiamy na 4 dru¿yny

    void Start()
    {
        if (chessBoard == null)
            chessBoard = FindAnyObjectByType<ChessBoard>();
    }
    public void Init(int numTeams, bool[] aiTeams, PieceManager pieceMan, TeamPanel tPanel, HighlightManager hManager, FogOfWarManager fManager)
    {
        numberOfTeams = numTeams;
        isAIControlledTeam = aiTeams;
        pieceManager = pieceMan;
        teamPanel = tPanel;
        highlightManager = hManager;
        fogOfWarManager = fManager;
        currentTeam = 0;
    }
    public void ChangeTurn()
    {
        int attempts = numberOfTeams;

        do
        {
            currentTeam = (currentTeam + 1) % numberOfTeams;
            attempts--;

            if (!DoesTeamHavePieces(currentTeam))
            {
                Debug.Log("Dru¿yna " + (currentTeam + 1) + " nie ma pionków. Pomijam.");
            }
        } while (!DoesTeamHavePieces(currentTeam) && attempts > 0);

        ResetMovementRangeForTeam(currentTeam);

        Debug.Log("Tura dru¿yny " + (currentTeam + 1));

        SelectPieceWithLowestId(currentTeam);
        if (chessBoard.currentlyDragging != null && !isAIControlledTeam[chessBoard.currentlyDragging.team])
        {
            Camera.main.GetComponent<CameraController>().SetTarget(chessBoard.currentlyDragging.transform);
        }
        teamPanel.CurrentPiecesSetPanel(chessBoard.currentlyDragging);
        highlightManager.HighlightPossibleMoves(chessBoard.currentlyDragging);
        fogOfWarManager.UpdateFogOfWar(chessBoard.currentlyDragging.currentX, chessBoard.currentlyDragging.currentY, pieceManager.chessPieces);
        fogOfWarManager.UpdatePieceVisibility(pieceManager.chessPieces);


        if (isAIControlledTeam[currentTeam])
        {
            if (AIController.Instance != null)
            {
                AIController.Instance.PlayTurn(currentTeam);
            }
            else
            {
                Debug.LogError("AIController.Instance == null! Czy AIController jest w scenie?");
            }
        }
    }
    private bool DoesTeamHavePieces(int teamId)
    {
        // Sprawdzamy, czy jakikolwiek pionek nale¿y do danej dru¿yny i jest ¿ywy
        foreach (var piece in pieceManager.chessPieces)
        {
            if (piece != null && piece.team == teamId)
            {
                return true;
            }
        }
        return false;
    }
    public void CheckGameOver()
    {
        // Sprawdzamy, czy na planszy s¹ jeszcze pionki przeciwników
        for (int team = 0; team < numberOfTeams; team++)
        {
            if (team == currentTeam) continue; // Pomijamy aktualn¹ dru¿ynê

            bool enemyFound = false;
            foreach (var piece in pieceManager.chessPieces)
            {
                if (piece != null && piece.team == team)
                {
                    enemyFound = true;
                    break;
                }
            }

            if (enemyFound)
            {
                return; // Wci¹¿ s¹ przeciwnicy, nie koñczymy gry
            }
        }

        // Jeœli nie znaleziono przeciwników, gra siê koñczy
        GameOver();
    }
    private void GameOver()
    {
        Debug.Log("Gra zakoñczona! Dru¿yna " + currentTeam + " wygrywa!");

        StartCoroutine(LoadMainMenu());
    }

    private IEnumerator LoadMainMenu()
    {
        Debug.Log("£adowanie sceny MainMenu...");
        SceneManager.LoadScene("MainMenu");
        yield return new WaitForSeconds(1);
    }


    private void ResetMovementRangeForTeam(int team)
    {
        for (int x = 0; x < TileManager.Tile_Count_X; x++)
        {
            for (int y = 0; y < TileManager.Tile_Count_Y; y++)
            {
                ChessPieces cp = pieceManager.chessPieces[x, y];
                if (cp != null && cp.team == team)
                {
                    cp.movementRange = cp.maxMovementRange; // Resetowanie punktów ruchu na maksymalne
                }
            }
        }
    }
    private void SelectPieceWithLowestId(int team)
    {
        ChessPieces lowestIdPiece = null;

        foreach (var piece in pieceManager.chessPieces)
        {
            if (piece != null && piece.team == team)
            {
                if (lowestIdPiece == null || piece.Id < lowestIdPiece.Id)
                {
                    lowestIdPiece = piece;

                }
            }
        }

        if (lowestIdPiece != null)
        {
            chessBoard.currentlyDragging = lowestIdPiece;
            Debug.Log("Wybrany pionek z najmniejszym ID: " + chessBoard.currentlyDragging.Id);
        }
        else
        {
            Debug.Log("Brak pionków dla dru¿yny " + team);
        }
    }
}
