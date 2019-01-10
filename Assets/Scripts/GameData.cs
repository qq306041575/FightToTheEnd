using System;
using UnityEngine;

public class RpcData
{
    public string Type;
    public object Data;
}

public class MsgData
{
    public string Type;
    public object Data;
}

public class ResData
{
    public string Type;
    public AssetBundleInfo Data;
}

public class EquipData
{
    public int Weapon;
    public int BulletCount;
    public int MagCount;
    public float FireRate;
    public Vector3 HandOffset;
}

public class ReloadData
{
    public int BulletCount;
    public int MagCount;
}

public class ShootData
{
    public int BulletCount;
    public int MagCount;
    public int CrosshairSize;
}

public class DamageData
{
    public Transform Attacker;
    public Transform Character;
    public int Health;
}

public class DieData
{
    public string Killer;
    public string Victim;
    public bool IsKiller;
    public bool IsVictim;
}

public class TransformData
{
    public Vector3 Position;
    public Quaternion Rotation;
}

public class RoomData
{
    public string Name;
    public string RoomName;
    public int PlayerCount;
    public int MaxPlayers;
}

public class PlayerData
{
    public int ActorNumber;
    public string NickName;
    // public Color DisplayColor;
}

[Serializable]
public class BotData
{
    public string Name;
    public int Count;
    public int ScanAngle;
    public int ScanLine;
    public float LockTime;
}

[Serializable]
public class InjuryData
{
    public string Name;
    public int Injury;
}

[Serializable]
public class WeaponData
{
    public string Name;
    public WeaponType Type;
    public Vector3 HandOffset;
    public int CrosshairSize;
    public int Damage;
    public float Spread;
    public int FireRange;
    public float FireRate;
    public int FireBullet;
    public int MagCapacity;
    public int MagCount;
}

public enum WeaponType
{
    Pistol = 0,
    Rifle = 1,
    Shotgun = 2
}

public enum RemotePlayerState
{
    Sync,
    Patrol,
    Lock
}
