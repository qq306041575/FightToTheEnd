using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UniRx;
using UnityEngine;

public class PhotonConnection : MonoBehaviourPunCallbacks
{
    public Transform[] PlayerPoints;
    public Transform[] BotPoints;
    public Transform[] WeaponPoints;
    public System.Action<object> LuaHandle;

    List<RoomData> _rooms;

    void Awake()
    {
        _rooms = new List<RoomData>();
        PhotonNetwork.LocalPlayer.NickName = string.Format(GameConst.FmtPlayerName, Random.Range(1000, 10000));
    }

    void OnDestroy()
    {
        LuaHandle = null;
    }

    void PublishData(string type, object data)
    {
        if (LuaHandle != null)
        {
            LuaHandle(new MsgData() { Type = type, Data = data });
        }
    }

    public bool LuaIsMasterClient()
    {
        return PhotonNetwork.IsMasterClient;
    }

    public bool LuaIsGameOver()
    {
        return PhotonNetwork.CurrentRoom.IsGameOver();
    }

    public void LuaPlayOffline()
    {
        PhotonNetwork.OfflineMode = true;
    }

    public void LuaPlayOnline(string version)
    {
        PhotonNetwork.GameVersion = version;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void LuaCreateRoom()
    {
        var count = Mathf.Clamp(GameSetting.Instance.MaxPlayers, 2, GameSetting.Instance.Colors.Count);
        var roomName = Random.Range(10, 100) + PhotonNetwork.LocalPlayer.NickName;
        PhotonNetwork.CreateRoom(roomName, new RoomOptions() { MaxPlayers = (byte)count }, null);
    }

    public void LuaJoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    public void LuaLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        if (!PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    public void LuaDisconnect()
    {
        PhotonNetwork.Disconnect();
        _rooms.Clear();
    }

    public void LuaBackToRoom()
    {
        PhotonNetwork.DestroyAll();
        PhotonNetwork.CurrentRoom.CustomProperties.Clear();
        PhotonNetwork.CurrentRoom.IsOpen = true;
        PhotonNetwork.CurrentRoom.IsVisible = true;
        PhotonNetwork.CurrentRoom.SetStartTime((int)PhotonNetwork.Time);
    }

    public void LuaBackToLobby()
    {
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.OfflineMode = false;
        PhotonNetwork.LocalPlayer.CustomProperties.Clear();
        PhotonNetwork.DestroyAll(true);
        ExitGame();
    }

    public void LuaPlayGame()
    {
        AddPickup();
        PhotonNetwork.CurrentRoom.IsVisible = false;
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.SetStartTime((int)PhotonNetwork.Time);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (DisconnectCause.DisconnectByClientLogic == cause)
        {
            return;
        }
        Debug.LogWarningFormat("[{0}]OnDisconnected: ", System.DateTime.Now, cause);
        PublishData(GameConst.RpcDisconnected, cause);
    }

    public override void OnConnectedToMaster()
    {
        if (PhotonNetwork.OfflineMode)
        {
            PhotonNetwork.JoinRandomRoom();
            return;
        }
        if (!PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
        PublishData(GameConst.RpcConnectedToMaster, PhotonNetwork.LocalPlayer.NickName);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.OfflineMode)
        {
            LuaPlayGame();
            return;
        }

        PhotonNetwork.LeaveLobby();
        _rooms.Clear();
        var players = new List<PlayerData>();
        foreach (var item in PhotonNetwork.CurrentRoom.Players.Values)
        {
            players.Add(new PlayerData()
            {
                ActorNumber = item.ActorNumber,
                NickName = item.NickName,
                //DisplayColor = GameSetting.Instance.Colors[item.GetIndex()]
            });
        }
        players.Sort((x, y) => x.ActorNumber - y.ActorNumber);
        PublishData(GameConst.RpcJoinedRoom, players);
    }

    public override void OnRoomListUpdate(List<RoomInfo> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var name = list[i].Name;
            if (!list[i].IsOpen || !list[i].IsVisible || list[i].RemovedFromList)
            {
                _rooms.RemoveAll(x => x.Name == name);
                continue;
            }
            var room = _rooms.Find(x => x.Name == name);
            if (null == room)
            {
                room = new RoomData();
                room.Name = name;
                room.RoomName = string.Format(GameConst.FmtRoomName, name.Substring(2));
                _rooms.Add(room);
            }
            room.PlayerCount = list[i].PlayerCount;
            room.MaxPlayers = list[i].MaxPlayers;
        }
        PublishData(GameConst.RpcRoomListUpdate, _rooms);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        var data = new PlayerData();
        data.ActorNumber = newPlayer.ActorNumber;
        data.NickName = newPlayer.NickName;
        // data.DisplayColor = GameSetting.Instance.Colors[newPlayer.GetIndex()];
        PublishData(GameConst.RpcPlayerEnteredRoom, data);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        var data = new PlayerData();
        data.ActorNumber = otherPlayer.ActorNumber;
        data.NickName = otherPlayer.NickName;
        PublishData(GameConst.RpcPlayerLeftRoom, data);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged != null && propertiesThatChanged.HasTimeKey())
        {
            PhotonNetwork.LocalPlayer.CustomProperties.Clear();
            if (PhotonNetwork.CurrentRoom.IsOpen)
            {
                ExitGame();
            }
            else
            {
                EnterGame();
            }
        }
    }

    void AddPickup()
    {
        for (int i = 0; i < WeaponPoints.Length; i++)
        {
            var point = WeaponPoints[i];
            var data = new object[] { Random.Range(1, GameSetting.Instance.Weapons.Count) };
            PhotonNetwork.InstantiateSceneObject("PhotonPickup", point.position, point.rotation, 0, data);
        }
    }

    void EnterGame()
    {
        var n = PhotonNetwork.CurrentRoom.GetStartTime() + PhotonNetwork.LocalPlayer.GetIndex();
        var p = PlayerPoints[n % PlayerPoints.Length];
        PhotonNetwork.Instantiate("PhotonPlayer", p.position, p.rotation, 0);

        if (PhotonNetwork.OfflineMode)
        {
            var count = Mathf.Clamp(GameSetting.Instance.Bot.Count, 1, BotPoints.Length);
            var index = Random.Range(0, BotPoints.Length);
            for (int i = 0; i < count; i++)
            {
                if (index >= BotPoints.Length)
                {
                    index -= BotPoints.Length;
                }
                var point = BotPoints[index];
                PhotonNetwork.InstantiateSceneObject("PhotonBot", point.position, point.rotation, 0);
                index++;
            }
        }
    }

    void ExitGame()
    {
        MessageBroker.Default.Publish(new MsgData() { Type = GameConst.MsgExitGame });
    }
}
