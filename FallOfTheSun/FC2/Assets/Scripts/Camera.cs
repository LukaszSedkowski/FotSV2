using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private Transform target;  // Obiekt, za którym kamera ma podążać
    [SerializeField] private Vector3 offset;    // Offset kamery względem obiektu
    [SerializeField] private float smoothSpeed = 0.125f; // Szybkość gładkiego ruchu kamery
    [SerializeField] private float scrollSensitivity = 2f; // Wrażliwość scrolla
    [SerializeField] private float minOffsetMagnitude = 2f; // Minimalny zasięg kamery
    [SerializeField] private float maxOffsetMagnitude = 10f; // Maksymalny zasięg kamery

    private void Start()
    {
        // Ustawienie domyślnego offsetu
        offset = new Vector3(8, 8, 3);
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            // Sprawdzanie, czy wciśnięto E lub R
            if (Input.GetKeyDown(KeyCode.E))
            {
                RotateCamera(90f); // Zmiana offsetu w prawo
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RotateCamera(-90f); // Zmiana offsetu w lewo
            }

            // Obsługa scrolla myszy
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            if (scrollInput != 0f)
            {
                // Zmiana offsetu na podstawie scrolla
                Vector3 newOffset = offset + offset.normalized * scrollInput * scrollSensitivity*(-1);

                // Ustawienie offsetu w granicach
                if (newOffset.magnitude < minOffsetMagnitude)
                {
                    newOffset = newOffset.normalized * minOffsetMagnitude; // Ustaw na minimalny
                }
                else if (newOffset.magnitude > maxOffsetMagnitude)
                {
                    newOffset = newOffset.normalized * maxOffsetMagnitude; // Ustaw na maksymalny
                }

                offset = newOffset; // Przypisz nowy offset
            }

            // Obliczenie nowej pozycji kamery z uwzględnieniem offsetu
            Vector3 desiredPosition = target.position + offset;
            // Gładkie przejście kamery do nowej pozycji
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // Opcjonalnie: obrót kamery w stronę obiektu
            transform.LookAt(target.position);
        }
    }

    private void RotateCamera(float angle)
    {
        // Zmiana offsetu w zależności od kąta
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        offset = rotation * offset; // Obrót offsetu
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget; // Ustawienie nowego obiektu docelowego
    }
    public void FitCameraToBoard(int boardWidth, int boardHeight, float tileSize)
    {
        float screenAspect = (float)Screen.width / (float)Screen.height;
        float boardAspect = (float)boardWidth / (float)boardHeight;

        float distance;

        if (screenAspect >= boardAspect)
        {
            // Ekran szerszy niż plansza
            distance = boardHeight * tileSize / (2f * Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad));
        }
        else
        {
            // Ekran węższy niż plansza
            float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(Camera.main.fieldOfView * Mathf.Deg2Rad * 0.5f) * screenAspect);
            distance = boardWidth * tileSize / (2f * Mathf.Tan(horizontalFOV * 0.5f));
        }

        Vector3 center = new Vector3((boardWidth - 1) * tileSize / 2f, 0, (boardHeight - 1) * tileSize / 2f);
        transform.position = center + Vector3.up * distance;
        transform.LookAt(center);
    }
}
