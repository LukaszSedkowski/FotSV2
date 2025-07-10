using UnityEngine;
using UnityEngine.UI;

public class WaypointInfoButton : MonoBehaviour
{
    private Waypoint waypoint;

    public void SetWaypoint(Waypoint wp)
    {
        waypoint = wp;
    }

    public void ShowInfo()
    {
        if (waypoint.enemyCharacters == null || waypoint.enemyCharacters.Count == 0)
        {
            Debug.Log("Brak przeciwników w tym punkcie.");
        }
        else
        {
            string enemyList = string.Join(", ", waypoint.enemyCharacters);
            Debug.Log("Przeciwnicy w tym punkcie: " + enemyList);
        }
    }
}
