using UnityEngine;

public class MapCont : MonoBehaviour
{
    public SpriteRenderer mapRenderer;

    void Start()
    {
        if (mapRenderer != null)
        {
            // Ustawienie kamery na œrodek mapy
            Vector3 newPosition = mapRenderer.bounds.center;
            newPosition.z = -10f; // Kamera powinna byæ odsuniêta w osi Z
            transform.position = newPosition;

            // Dopasowanie rozmiaru kamery
            Camera.main.orthographicSize = mapRenderer.bounds.size.y / 2;
        }
    }
}
