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

    [Header("Camera Defaults")]
    [SerializeField] private Vector3 defaultIsoOffset = new Vector3(8f, 8f, 3f);
    [SerializeField] private bool resetOffsetOnSetTarget = true; // ← klucz

    private void Start()
    {
        // Ustawienie domyślnego offsetu
        offset = new Vector3(8, 8, 3);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (Input.GetKeyDown(KeyCode.E)) RotateCamera(90f);
        else if (Input.GetKeyDown(KeyCode.R)) RotateCamera(-90f);

        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0f)
        {
            Vector3 newOffset = offset + offset.normalized * scrollInput * scrollSensitivity * (-1);
            if (newOffset.magnitude < minOffsetMagnitude) newOffset = newOffset.normalized * minOffsetMagnitude;
            else if (newOffset.magnitude > maxOffsetMagnitude) newOffset = newOffset.normalized * maxOffsetMagnitude;
            offset = newOffset;
        }

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
        transform.LookAt(target.position);
    }

    public void RotateCamera(float angle)
    {
        // Zmiana offsetu w zależności od kąta
        Quaternion rotation = Quaternion.Euler(0, angle, 0);
        offset = rotation * offset; // Obrót offsetu
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target == null) return;

        if (resetOffsetOnSetTarget)
            offset = defaultIsoOffset; // ← wymuszamy izometryczny offset „jak bez bootstrapa”

        transform.position = target.position + offset; // snap
        transform.LookAt(target.position);
        Debug.Log($"[Cam][SetTarget] target={target.name}, offset={offset}, pos={transform.position}");
    }

    public void FitCameraToBoard(int boardWidth, int boardHeight, float tileSize)
    {
        // NO-OP: zostawiamy kamerę w spokoju.
        // Cały start ogarnia SetTarget na pionka + izometryczny offset.
        Debug.Log($"[Cam][FitCameraToBoard] skipped (keep iso offset)");
    }
}
