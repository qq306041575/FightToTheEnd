using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class RemotePlayer : MonoBehaviour
{
    public RemoteAnimation PlayerAnimation;
    public PlayerWeapon[] PlayerWeapons;
    public AudioSource DieSound;
    public Transform AimPoint;
    public Transform ShootPoint;

    Transform _target;
    Vector3 _position;
    Quaternion _rotation;
    RemotePlayerState _state;

    void Update()
    {
        switch (_state)
        {
            case RemotePlayerState.Sync:
                transform.position = Vector3.Lerp(transform.position, _position, Time.deltaTime * 10);
                transform.rotation = Quaternion.Lerp(transform.rotation, _rotation, Time.deltaTime * 10);
                break;
            case RemotePlayerState.Patrol:
                _rotation = Quaternion.LookRotation(_position - transform.position, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, _rotation, Time.deltaTime * 60);
                transform.Translate(Vector3.forward * Time.deltaTime * 2);
                PlayerAnimation.AimPosition = AimPoint.position;
                break;
            case RemotePlayerState.Lock:
                _position = _target.position - transform.position;
                AimPoint.parent.rotation = Quaternion.LookRotation(_position);
                _position.y = 0;
                _rotation = Quaternion.LookRotation(_position);
                transform.rotation = Quaternion.Lerp(transform.rotation, _rotation, Time.deltaTime * 10);
                PlayerAnimation.AimPosition = AimPoint.position;
                break;
        }
    }

    public void Setup(IMessageBroker message, Vector3 position, Quaternion rotation)
    {
        message.Receive<MsgData>().Subscribe(x =>
        {
            if (GameConst.MsgDie == x.Type)
            {
                DieSound.PlayDelayed(0.2f);
                transform.localPosition += Vector3.up * 0.03f;
                enabled = false;
            }
        }).AddTo(this);
        for (int i = 0; i < PlayerWeapons.Length; i++)
        {
            PlayerWeapons[i].Setup(message);
        }
        PlayerAnimation.Message = message;
        _position = position;
        _rotation = rotation;
        _state = RemotePlayerState.Sync;
    }

    public void Patrol(Vector3 position)
    {
        _target = null;
        _position = position;
        _state = RemotePlayerState.Patrol;
        AimPoint.parent.rotation = transform.rotation;
        PlayerAnimation.IsGrounded = true;
        PlayerAnimation.VelocityX = 0;
        PlayerAnimation.VelocityZ = 4;
    }

    public void Lock(Transform target)
    {
        _target = target;
        _state = RemotePlayerState.Lock;
        PlayerAnimation.IsGrounded = true;
        PlayerAnimation.VelocityX = 0;
        PlayerAnimation.VelocityZ = 0;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(PlayerAnimation.AimPosition);
            stream.SendNext(PlayerAnimation.IsGrounded);
            stream.SendNext(PlayerAnimation.VelocityX);
            stream.SendNext(PlayerAnimation.VelocityZ);
        }
        else
        {
            _position = (Vector3)stream.ReceiveNext();
            _rotation = (Quaternion)stream.ReceiveNext();
            PlayerAnimation.AimPosition = (Vector3)stream.ReceiveNext();
            PlayerAnimation.IsGrounded = (bool)stream.ReceiveNext();
            PlayerAnimation.VelocityX = (float)stream.ReceiveNext();
            PlayerAnimation.VelocityZ = (float)stream.ReceiveNext();
        }
    }
}
