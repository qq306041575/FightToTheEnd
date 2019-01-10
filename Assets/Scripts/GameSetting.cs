using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class GameSetting : ScriptableObject
{
    public int MaxHealth;
    public int MaxPlayers;
    public float CoolDown;
    public BotData Bot;
    public List<Color> Colors;
    public List<InjuryData> Injuries;
    public List<WeaponType> Equipments;
    public List<WeaponData> Weapons;

    private static GameSetting _instance;
    public static GameSetting Instance
    {
        get
        {
            if (null == _instance)
            {
                _instance = Resources.Load<GameSetting>("GameSetting");
            }
            return _instance;
        }
    }
}
