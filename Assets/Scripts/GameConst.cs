using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConst
{
	public const string RpcConnectedToMaster = "RpcConnectedToMaster";
	public const string RpcDisconnected = "RpcDisconnected";
	public const string RpcJoinedRoom = "RpcJoinedRoom";
	public const string RpcRoomListUpdate = "RpcRoomListUpdate";
	public const string RpcPlayerEnteredRoom = "RpcPlayerEnteredRoom";
	public const string RpcPlayerLeftRoom = "RpcPlayerLeftRoom";
    public const string RpcPickup = "RpcPickup";
    public const string RpcEquip = "RpcEquip";
    public const string RpcReload = "RpcReload";
    public const string RpcShoot = "RpcShoot";
    public const string RpcImpact = "RpcImpact";
    public const string RpcDamage = "RpcDamage";
    public const string RpcDie = "RpcDie";

    public const string MsgEnterGame = "MsgEnterGame";
    public const string MsgExitGame = "MsgExitGame";
    public const string MsgPlayEquip = "MsgPlayEquip";
    public const string MsgPlayReload = "MsgPlayReload";
    public const string MsgEquip = "MsgEquip";
    public const string MsgReload = "MsgReload";
    public const string MsgShoot = "MsgShoot";
    public const string MsgDamage = "MsgDamage";
    public const string MsgDie = "MsgDie";
    public const string MsgCameraVisible = "MsgCameraVisible";
    public const string MsgCursorVisible = "MsgCursorVisible";
    public const string MsgApplicationFocus = "MsgApplicationFocus";

    public const string ResManifest = "manifest";
    public const string ResUI = "ui";
    public const string ResEffect = "effect";
    public const string ResPlayerLocal = "player_local";
    public const string ResPlayerRemote = "player_remote";

    public const string TagFlesh = "Flesh";
    public const string TagSand = "Sand";
    public const string TagWood = "Wood";
    public const string TagAI = "AI";
    public const string TagPickup = "Pickup";
    
    public const string FmtPlayerName = "玩家{0}";
	public const string FmtRoomName = "{0}的房间";
}
