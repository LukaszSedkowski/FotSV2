using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Ability
{
    public string abilityName;
    public Sprite icon;
    public int atack;
    public int healing;
    public Action<ChessPieces> ExecuteAction;

    // konstruktor u³atwiaj¹cy inicjalizacjê
    public Ability(string name, Sprite icon, Action<ChessPieces> action)
    {
        this.abilityName = name;
        this.icon = icon;
        this.ExecuteAction = action;
    }
}
