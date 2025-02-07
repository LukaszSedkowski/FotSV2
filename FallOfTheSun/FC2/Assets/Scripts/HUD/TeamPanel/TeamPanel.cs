using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TeamPanel : MonoBehaviour
{
    public TextMeshProUGUI team;
    public Slider healthSlider;
    public TextMeshProUGUI type;
    public TextMeshProUGUI attackRange;
    public TextMeshProUGUI attack;
    public TextMeshProUGUI movmentRange;

    public void CurrentPiecesSetPanel(ChessPieces currentlyDragging)
    {
        if (currentlyDragging != null)
        {

            team.text = currentlyDragging.team.ToString();
            healthSlider.maxValue = currentlyDragging.maxHealth;
            healthSlider.value = currentlyDragging.health;
            type.text = currentlyDragging.type.ToString();
            attack.text = currentlyDragging.attack.ToString();
            attackRange.text = currentlyDragging.attackRange.ToString();
            movmentRange.text = currentlyDragging.movementRange.ToString() + " / " + currentlyDragging.maxMovementRange.ToString();
        }
    }
    public TeamPanel(List<ChessPieces> chessPieces)
    {
        foreach (var piece in chessPieces)
        {
            TeamPanel teamPanel = GetComponent<TeamPanel>();
            teamPanel.CurrentPiecesSetPanel(piece);
        }
    }
}
