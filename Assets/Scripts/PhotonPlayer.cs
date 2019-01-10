using System.Collections.Generic;
using Photon.Pun;
using UniRx;
using UnityEngine;

public class PhotonPlayer : MonoBehaviourPunCallbacks, IPunObservable
{
    IMessageBroker _message;
    LocalPlayer _local;
    RemotePlayer _remote;

    void Awake()
    {
        name = photonView.Owner.NickName;
        MessageBroker.Default.Receive<ResData>().Subscribe(x =>
        {
            if (GameConst.ResPlayerRemote == x.Type && x.Data != null && !photonView.IsMine && _remote == null)
            {
                var asset = x.Data.LoadAsset("RemotePlayer");
                asset.SetParent(transform, false);
                _message = new MessageBroker();
                _remote = asset.GetComponent<RemotePlayer>();
                _remote.Setup(_message, transform.position, transform.rotation);
            }
            if (GameConst.ResPlayerLocal == x.Type && x.Data != null && photonView.IsMine && _local == null)
            {
                var asset = x.Data.LoadAsset("LocalPlayer");
                asset.SetParent(transform, false);
                Setup(asset);
            }
        }).AddTo(this);
        var msg = new MsgData() { Type = GameConst.MsgEnterGame, Data = photonView.IsMine };
        MessageBroker.Default.Publish(msg);
    }

    void Setup(Transform player)
    {
        for (int i = 0; i < GameSetting.Instance.Equipments.Count; i++)
        {
            photonView.Owner.AddEquipment((int)GameSetting.Instance.Equipments[i]);
        }
        photonView.Owner.SetHealth(GameSetting.Instance.MaxHealth);
        _message = MessageBroker.Default;
        _message.Receive<RpcData>().Subscribe(x =>
        {
            switch (x.Type)
            {
                case GameConst.RpcPickup:
                    var view = (PhotonView)x.Data;
                    view.RPC(GameConst.RpcPickup, RpcTarget.MasterClient, photonView.ViewID);
                    break;
                case GameConst.RpcEquip:
                    photonView.RPC(GameConst.RpcEquip, RpcTarget.All, (int)x.Data);
                    break;
                case GameConst.RpcReload:
                    photonView.RPC(GameConst.RpcReload, RpcTarget.All);
                    break;
                case GameConst.RpcShoot:
                    photonView.RPC(GameConst.RpcShoot, RpcTarget.All);
                    break;
            }
        }).AddTo(this);
        _local = player.GetComponent<LocalPlayer>();
        _local.Pickup(GameSetting.Instance.Equipments);
    }

    [PunRPC]
    public void RpcPickup(int id)
    {
        var weapon = GameSetting.Instance.Weapons.Find(x => id == (int)x.Type);
        if (weapon != null)
        {
            _local.Pickup(new List<WeaponType>() { weapon.Type });
        }
    }

    [PunRPC]
    public void RpcEquip(int id)
    {
        var data = new EquipData();
        if (photonView.Owner.ContainEquipment(id))
        {
            var weapon = GameSetting.Instance.Weapons.Find(x => id == (int)x.Type);
            data.Weapon = id;
            data.HandOffset = weapon.HandOffset;
            data.FireRate = weapon.FireRate;
            data.BulletCount = photonView.Owner.GetBulletCount(id, weapon.MagCapacity);
            data.MagCount = photonView.Owner.GetMagCount(id, weapon.MagCount);
            photonView.Owner.SetWeapon(data.Weapon);
        }
        _message.Publish(new MsgData() { Type = GameConst.MsgPlayEquip, Data = data });
    }

    [PunRPC]
    public void RpcReload()
    {
        var data = new ReloadData();
        var type = (WeaponType)photonView.Owner.GetWeapon();
        var weapon = GameSetting.Instance.Weapons.Find(x => x.Type == type);
        if (weapon != null && photonView.IsMine)
        {
            var magCount = photonView.Owner.GetMagCount((int)type, weapon.MagCount);
            if (magCount > 0)
            {
                data.BulletCount = weapon.MagCapacity;
                data.MagCount = magCount - 1;
                photonView.Owner.SetBulletCount((int)type, data.BulletCount);
                photonView.Owner.SetMagCount((int)type, data.MagCount);
            }
            else
            {
                data.BulletCount = photonView.Owner.GetBulletCount((int)type, weapon.MagCapacity);
            }
        }
        _message.Publish(new MsgData() { Type = GameConst.MsgPlayReload, Data = data });
    }

    [PunRPC]
    public void RpcShoot()
    {
        var data = new ShootData();
        var hasBullet = false;
        var type = (WeaponType)photonView.Owner.GetWeapon();
        var weapon = GameSetting.Instance.Weapons.Find(x => x.Type == type);
        if (weapon != null && photonView.IsMine)
        {
            var bulletCount = photonView.Owner.GetBulletCount((int)type, weapon.MagCapacity);
            if (bulletCount > 0)
            {
                hasBullet = true;
                data.BulletCount = bulletCount - 1;
                photonView.Owner.SetBulletCount((int)type, data.BulletCount);
            }
            data.MagCount = photonView.Owner.GetMagCount((int)type, weapon.MagCount);
            data.CrosshairSize = weapon.CrosshairSize;
        }
        _message.Publish(new MsgData() { Type = GameConst.MsgShoot, Data = data });

        if (hasBullet)
        {
            Fire(weapon, _local.ShootPoint.position, _local.ShootPoint.rotation);
        }
    }

    void Fire(WeaponData weapon, Vector3 position, Quaternion rotation)
    {
        RaycastHit hit;
        var directions = new List<Vector3>();
        for (int i = 0; i < weapon.FireBullet; i++)
        {
            var rotateY = Quaternion.Euler(0, Random.Range(-weapon.Spread, weapon.Spread), 0);
            var rotateZ = Quaternion.Euler(0, 0, Random.Range(0, 360));
            var direction = rotation * rotateZ * rotateY * Vector3.forward;
            if (Physics.Raycast(position, direction, out hit, weapon.FireRange))
            {
                directions.Add(direction);
                if (hit.transform.CompareTag(GameConst.TagFlesh))
                {
                    var view = hit.transform.root.GetComponent<PhotonView>();
                    var injury = GameSetting.Instance.Injuries.Find(x => x.Name == hit.transform.name).Injury;
                    Hurt(view, photonView.ViewID, injury + weapon.Damage);
                }
            }
        }
        if (directions.Count > 0)
        {
            photonView.RPC(GameConst.RpcImpact, RpcTarget.All, position, directions.ToArray());
        }
    }

    void Hurt(PhotonView view, int attackerId, int damage)
    {
        if (view != null && view.Owner != null)
        {
            var health = view.Owner.GetHealth();
            if (health > 0)
            {
                health -= damage;
                view.Owner.SetHealth(health);
                if (health > 0)
                {
                    view.RPC(GameConst.RpcDamage, view.Owner, attackerId, health);
                }
                else
                {
                    view.RPC(GameConst.RpcDie, RpcTarget.All, attackerId);
                }
            }
        }
        else if (view != null && view.CompareTag(GameConst.TagAI))
        {
            var health = PhotonNetwork.CurrentRoom.GetBotHealth(view.ViewID);
            if (health > 0)
            {
                health -= damage;
                PhotonNetwork.CurrentRoom.SetBotHealth(view.ViewID, health);
                if (health > 0)
                {
                    view.RPC(GameConst.RpcDamage, RpcTarget.MasterClient, attackerId, health);
                }
                else
                {
                    view.RPC(GameConst.RpcDie, RpcTarget.All, attackerId);
                }
            }
        }
    }

    [PunRPC]
    public void RpcDamage(int attackerId, int health)
    {
        var data = new DamageData();
        data.Attacker = PhotonView.Find(attackerId).transform.GetChild(0);
        data.Character = _local.transform;
        data.Health = health;
        _message.Publish(new MsgData() { Type = GameConst.MsgDamage, Data = data });
    }

    [PunRPC]
    public void RpcDie(int killerId)
    {
        var data = new DieData();
        data.Victim = name;
        data.IsVictim = photonView.IsMine;
        var view = PhotonView.Find(killerId);
        data.Killer = view.name;
        data.IsKiller = view.Owner == PhotonNetwork.LocalPlayer;
        var msg = new MsgData() { Type = GameConst.MsgDie, Data = data };
        if (data.IsVictim)
        {
            Observable.Timer(System.TimeSpan.FromSeconds(3)).Subscribe(_ => PhotonNetwork.Destroy(gameObject)).AddTo(this);
        }
        else
        {
            _message.Publish(msg);
        }
        MessageBroker.Default.Publish(msg);
    }

    [PunRPC]
    public void RpcImpact(Vector3 position, Vector3[] directions)
    {
        RaycastHit hit;
        for (int i = 0; i < directions.Length; i++)
        {
            if (Physics.Raycast(position, directions[i], out hit))
            {
                MessageBroker.Default.Publish(hit);
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (_local != null)
            {
                _local.OnPhotonSerializeView(stream, info);
            }
        }
        else
        {
            if (_remote != null)
            {
                _remote.OnPhotonSerializeView(stream, info);
            }
        }
    }
}
