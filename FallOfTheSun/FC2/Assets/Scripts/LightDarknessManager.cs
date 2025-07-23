using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
public class LightDarknessManager : MonoBehaviour
{
    private ChessPieces chessPieces;
    public Slider LightDarkSlider;
    public int lightDarkLevel { get; set; } = 50;
    public float lightBonus = 1;
    public float darknessBonus = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        chessPieces = FindAnyObjectByType<ChessPieces>();
        LightDarkSlider.value = lightDarkLevel;
    }

    // Update is called once per frame
    void Update()
    {
        LightDarkSlider.value = lightDarkLevel;
        UpdateElementBonus();
    }
    public void ChangeLightLevel(int value)
    {
            lightDarkLevel = Mathf.Clamp(lightDarkLevel + value, 0, 100);
    }
    public void UpdateElementBonus()
    {
        // LightBonus: bonus dla œwiat³a gdy przewaga œwiat³a
        lightBonus = 1f + Mathf.Clamp01((lightDarkLevel - 50) / 50f) * 0.3f; // max +30%

        // DarknessBonus: bonus dla mroku gdy przewaga mroku
        darknessBonus = 1f + Mathf.Clamp01((50 - lightDarkLevel) / 50f) * 0.3f; // max +30%
    }
    public void ChangeLightDarkLevel(int value)
    {
        if (!(lightDarkLevel <= 0 && value < 0) && !(lightDarkLevel >= 100 && value > 0))
        {
            lightDarkLevel += value;
            UpdateElementBonus();
            Debug.Log(lightDarkLevel);
        }
    }

}
