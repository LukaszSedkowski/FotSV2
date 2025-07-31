using UnityEngine;

[DefaultExecutionOrder(-100)]
public class EnsureAudioListener : MonoBehaviour
{
    void Awake()
    {
        if (Camera.main != null && Camera.main.GetComponent<AudioListener>() == null)
            Camera.main.gameObject.AddComponent<AudioListener>();
    }
}
