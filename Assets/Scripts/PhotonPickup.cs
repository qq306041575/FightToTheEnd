using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UniRx;
using UnityEngine;

public class PhotonPickup : MonoBehaviourPunCallbacks
{
    int _weapon;

	void Awake()
	{
        if (null == photonView.InstantiationData)
        {
            gameObject.SetActive(false);
            return;
        }
        _weapon = (int)photonView.InstantiationData[0];
        MessageBroker.Default.Receive<ResData>().Subscribe(x =>
        {
            if (GameConst.ResPlayerLocal == x.Type && x.Data != null)
            {
                var asset = x.Data.LoadAsset("Weapon" + _weapon);
                asset.SetParent(transform, false);
            }
        }).AddTo(this);
	}

    [PunRPC]
    public void RpcPickup(int viewId)
    {
        var view = PhotonView.Find(viewId);
        if (view != null && view.Owner != null && view.Owner.GetHealth() > 0 && !view.Owner.ContainEquipment(_weapon))
        {
            view.Owner.AddEquipment(_weapon);
            view.RPC(GameConst.RpcPickup, view.Owner, _weapon);
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
