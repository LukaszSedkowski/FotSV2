using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotController : MonoBehaviour
{
    [Header("UI References")]
    public Button switchButton;              // prze³¹cznik Gracz/Komputer
    public Image switchIcon;                 // ikona mózg/komputer
    public Button armySlotButton;            // czerwony przycisk otwieraj¹cy panel


    [Header("Game Settings")]
    [Tooltip("Ile jednostek mo¿na wybraæ w tym slocie")]
    public int pawnsCount = 1;

    [Header("Sprites")]
    public Sprite humanSprite;
    public Sprite aiSprite;

    [Header("Team Color")]
    public TeamColorController colorController;

    [Header("Unit Selection")]
    public GameObject unitSelectionPanelPrefab;

    private bool isHuman = true;
    private List<int> selectedUnitIndices = new List<int>();

    // event, który powiadomi menu, ¿e slot zakoñczy³ wybór
    public event Action<PlayerSlotController> OnSlotConfirmed;

    public bool IsHuman => isHuman;
    public List<int> GetSelectedUnits() => new List<int>(selectedUnitIndices);

    public void Setup(int pawnLimit)
    {
        pawnsCount = pawnLimit;
        switchButton.onClick.AddListener(ToggleHumanAI);
        UpdateSwitchVisual();
        armySlotButton.onClick.AddListener(ShowUnitSelectionPanel);
    }
    private void ShowUnitSelectionPanel()
    {
        // 1) ZnajdŸ g³ówny Canvas w scenie
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Nie znalaz³em Canvasu w scenie!");
            return;
        }

        // 2) Utwórz instancjê prefab’u panelu wyboru jednostek
        GameObject panelGO = Instantiate(unitSelectionPanelPrefab, canvas.transform);

        // 3) Pobierz kontroler i zainicjuj go limitem oraz callbackiem
        var panelCtrl = panelGO.GetComponent<UnitSelectionPanelController>();
        panelCtrl.Init(pawnsCount, OnUnitsChosen);
    }

    private void ToggleHumanAI()
    {
        isHuman = !isHuman;
        UpdateSwitchVisual();
    }

    private void UpdateSwitchVisual()
    {
        if (switchIcon != null)
        {
            switchIcon.sprite = isHuman ? humanSprite : aiSprite;
            switchIcon.rectTransform.sizeDelta = new Vector2(90f, 90f);
            switchIcon.color = Color.white;
        }
    }



    private void OnUnitsChosen(List<int> chosen)
    {
        selectedUnitIndices = new List<int>(chosen);
        armySlotButton.interactable = false;
        OnSlotConfirmed?.Invoke(this);
    }
}
