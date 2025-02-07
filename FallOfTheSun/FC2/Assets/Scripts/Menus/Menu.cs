using UnityEngine;
using UnityEngine.SceneManagement;

public  class Menu : MonoBehaviour
{
    public void GuestPlay()
    {
        DisableCamera();
        SceneManager.LoadScene(1);
    }
    void DisableCamera()
    {
        Camera cameraToDisable = GameObject.Find("Main Camera").GetComponent<Camera>();
        if (cameraToDisable != null)
        {
            cameraToDisable.enabled = false;
        }
    }
}
