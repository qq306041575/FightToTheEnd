using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

public static class PhotonExtensions
{
    const string IndexKey = "Index";
    const string BulletKey = "Bullet";
    const string MagKey = "Mag";
    const string EquipmentsKey = "Equipments";
    const string WeaponKey = "Weapon";
    const string HealthKey = "Health";
    const string TimeKey = "Time";
    const int DefaultWeapon = -1;
    const int DefaultHealth = 999;
    const int DefaultTime = 0;

    public static int GetIndex(this Player player)
    {
        var index = 0;
        foreach (var item in PhotonNetwork.CurrentRoom.Players.Values)
        {
            if (item.ActorNumber < player.ActorNumber)
            {
                index++;
            }
        }
        return index;
    }

    public static void SetBulletCount(this Player player, int id, int value)
    {
        player.SetCustomProperties(new Hashtable() { { BulletKey + id, value } });
    }

    public static int GetBulletCount(this Player player, int id, int defaultValue)
    {
        object value;
        if (player.CustomProperties.TryGetValue(BulletKey + id, out value))
        {
            return (int)value;
        }
        player.SetBulletCount(id, defaultValue);
        return defaultValue;
    }

    public static void SetMagCount(this Player player, int id, int value)
    {
        player.SetCustomProperties(new Hashtable() { { MagKey + id, value } });
    }

    public static int GetMagCount(this Player player, int id, int defaultValue)
    {
        object value;
        if (player.CustomProperties.TryGetValue(MagKey + id, out value))
        {
            return (int)value;
        }
        player.SetMagCount(id, defaultValue);
        return defaultValue;
    }

    public static void SetWeapon(this Player player, int value)
    {
        player.SetCustomProperties(new Hashtable() { { WeaponKey, value } });
    }

    public static int GetWeapon(this Player player)
    {
        object value;
        if (player.CustomProperties.TryGetValue(WeaponKey, out value))
        {
            return (int)value;
        }
        return DefaultWeapon;
    }

    public static void SetEquipments(this Player player, string value)
    {
        player.SetCustomProperties(new Hashtable() { { EquipmentsKey, value } });
    }

    public static string GetEquipments(this Player player)
    {
        object value;
        if (player.CustomProperties.TryGetValue(EquipmentsKey, out value))
        {
            return (string)value;
        }
        return "";
    }

    public static void AddEquipment(this Player player, int value)
    {
        var equipments = string.Format("{0}{1},", player.GetEquipments(), value);
        player.SetEquipments(equipments);
    }

    public static bool ContainEquipment(this Player player, int value)
    {
        return player.GetEquipments().Contains(value + ",");
    }

    public static void SetHealth(this Player player, int value)
    {
        player.SetCustomProperties(new Hashtable() { { HealthKey, value } });
    }

    public static int GetHealth(this Player player)
    {
        object value;
        if (player.CustomProperties.TryGetValue(HealthKey, out value))
        {
            return (int)value;
        }
        return DefaultHealth;
    }

    public static void SetBotHealth(this Room room, int id, int value)
    {
        room.SetCustomProperties(new Hashtable() { { HealthKey + id, value } });
    }

    public static int GetBotHealth(this Room room, int id)
    {
        object value;
        if (room.CustomProperties.TryGetValue(HealthKey + id, out value))
        {
            return (int)value;
        }
        return DefaultHealth;
    }

    public static void SetStartTime(this Room room, int time)
    {
        room.SetCustomProperties(new Hashtable() { { TimeKey, time } });
    }

    public static int GetStartTime(this Room room)
    {
        object value;
        if (room.CustomProperties.TryGetValue(TimeKey, out value))
        {
            return (int)value;
        }
        return DefaultTime;
    }

    public static bool IsGameOver(this Room room)
    {
        var count = 0;
        foreach (var item in room.Players.Values)
        {
            if (!item.IsLocal && item.GetHealth() > 0)
            {
                count++;
            }
        }
        if (count > 1)
        {
            return false;
        }
        if (count == 1)
        {
            return PhotonNetwork.LocalPlayer.GetHealth() <= 0;
        }
        foreach (var item in room.CustomProperties)
        {
            var key = System.Convert.ToString(item.Key);
            if (key != null && key.StartsWith(HealthKey) && (int)item.Value > 0)
            {
                return false;
            }
        }
        return true;
    }

    public static bool HasIndexKey(this Hashtable table)
    {
        return table.ContainsKey(IndexKey);
    }

    public static bool HasTimeKey(this Hashtable table)
    {
        return table.ContainsKey(TimeKey);
    }
}
