using UnityEngine;

public class Controller : MonoBehaviour
{
    // Tablica nazw obiektów do wywołania
    public string[] nazwyObiektow;

    private int index = 0;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (index < nazwyObiektow.Length)
            {
                GameObject obj = GameObject.Find(nazwyObiektow[index]);
                if (obj != null)
                {
                    Debug.Log("Wywołuję obiekt: " + obj.name);
                    obj.SetActive(true); // lub inna akcja
                    index++;
                }
                else
                {
                    Debug.LogWarning("Nie znaleziono obiektu o nazwie: " + nazwyObiektow[index]);
                    index++;
                }
            }
            else
            {
                Debug.Log("Wszystkie obiekty już zostały wywołane.");
            }
        }
    }
}
