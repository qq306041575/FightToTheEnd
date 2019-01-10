using System.Collections.Generic;
using Photon.Pun;
using UniRx;
using UnityEngine;

public class LocalPlayer : MonoBehaviour
{
    public CharacterController Controller;
    public AudioSource Audio;
    public AudioClip DieSound;
    public AudioClip NoBulletSound;
    public Transform AimPoint;
    public Transform ShootPoint;
    public Transform Hand;

    EquipData _data;
    List<int> _equipments;
    float _shootTimer;
    bool _isEquiping;
    bool _isReloading;
    bool _isShooting;

    void Awake()
    {
        _equipments = new List<int>();
        MessageBroker.Default.Receive<MsgData>().Subscribe(x =>
        {
            switch (x.Type)
            {
                case GameConst.MsgEquip:
                    _data = (EquipData)x.Data;
                    Hand.localPosition = _data.HandOffset;
                    _isEquiping = false;
                    break;
                case GameConst.MsgReload:
                    var reload = (ReloadData)x.Data;
                    _data.BulletCount = reload.BulletCount;
                    _data.MagCount = reload.MagCount;
                    _isReloading = false;
                    break;
                case GameConst.MsgShoot:
                    var shoot = (ShootData)x.Data;
                    _data.BulletCount = shoot.BulletCount;
                    _data.MagCount = shoot.MagCount;
                    break;
                case GameConst.MsgDie:
                    var die = (DieData)x.Data;
                    if (die.IsVictim)
                    {
                        Audio.clip = DieSound;
                        Audio.PlayDelayed(0.2f);
                        Controller.enabled = false;
                        enabled = false;
                    }
                    break;
            }
        }).AddTo(this);
    }

    void Update()
    {
        if (_isEquiping || Cursor.visible)
        {
            return;
        }
        if (_shootTimer > 0)
        {
            _shootTimer -= Time.deltaTime;
        }

        _isShooting = Input.GetButton("Fire1");
        if (Input.GetKeyDown(KeyCode.Alpha1) && !_isShooting && _equipments.Count > 0 && _equipments[0] != _data.Weapon)
        {
            OnEquip(_equipments[0]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && !_isShooting && _equipments.Count > 1 && _equipments[1] != _data.Weapon)
        {
            OnEquip(_equipments[1]);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && !_isShooting && _equipments.Count > 2 && _equipments[2] != _data.Weapon)
        {
            OnEquip(_equipments[2]);
        }
        if (Input.GetButtonDown("Fire2") && !_isShooting && !_isReloading)
        {
            OnReload();
        }
        if (_isShooting && !_isReloading && _shootTimer <= 0)
        {
            OnShoot();
        }
    }

    void OnEquip(int weapon)
    {
        _isEquiping = true;
        var rpc = new RpcData() { Type = GameConst.RpcEquip, Data = weapon };
        MessageBroker.Default.Publish(rpc);
    }

    void OnReload()
    {
        if (_data.MagCount < 1)
        {
            return;
        }
        _isReloading = true;
        var rpc = new RpcData() { Type = GameConst.RpcReload };
        MessageBroker.Default.Publish(rpc);
    }

    void OnShoot()
    {
        if (_data.BulletCount < 1)
        {
            Audio.clip = NoBulletSound;
            Audio.Play();
            return;
        }
        _shootTimer = _data.FireRate;
        var rpc = new RpcData() { Type = GameConst.RpcShoot };
        MessageBroker.Default.Publish(rpc);
    }

    public void Pickup(List<WeaponType> weapons)
    {
        var weapon = -1;
        for (int i = 0; i < weapons.Count; i++)
        {
            var id = (int)weapons[i];
            if (!_equipments.Contains(id))
            {
                weapon = id;
                _equipments.Add(id);
            }
        }
        if (weapon != -1 && !_isShooting && !_isReloading)
        {
            Observable.NextFrame().Subscribe(_ => OnEquip(weapon));
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        var velocity = transform.InverseTransformDirection(Controller.velocity);
        stream.SendNext(transform.position);
        stream.SendNext(transform.rotation);
        stream.SendNext(AimPoint.position);
        stream.SendNext(Controller.isGrounded);
        stream.SendNext(velocity.x);
        stream.SendNext(velocity.z);
    }
}
