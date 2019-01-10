using UniRx;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public WeaponType Type;
    public ParticleSystem FireParticle;
    public ParticleSystem ShellParticle;
    public AudioSource Audio;
    public AudioClip EquipSound;
    public AudioClip FireSound;
    public AudioClip ReloadSound;

    bool _hasSetuped;

    void Start()
    {
        MessageBroker.Default.Receive<MsgData>().Subscribe(x =>
        {
            if (_hasSetuped)
            {
                return;
            }
            switch (x.Type)
            {
                case GameConst.MsgEquip:
                    Equip((x.Data as EquipData).Weapon);
                    break;
                case GameConst.MsgPlayReload:
                    Reload();
                    break;
                case GameConst.MsgShoot:
                    Shoot();
                    break;
                case GameConst.MsgDie:
                    if ((x.Data as DieData).IsVictim)
                    {
                        Audio.gameObject.SetActive(false);
                    }
                    break;
            }
        }).AddTo(this);
    }

    public void Setup(IMessageBroker message)
    {
        message.Receive<MsgData>().Subscribe(x =>
        {
            switch (x.Type)
            {
                case GameConst.MsgPlayEquip:
                    Equip((x.Data as EquipData).Weapon);
                    break;
                case GameConst.MsgPlayReload:
                    Reload();
                    break;
                case GameConst.MsgShoot:
                    Shoot();
                    break;
                case GameConst.MsgDie:
                    Audio.gameObject.SetActive(false);
                    break;
            }
        }).AddTo(this);
        _hasSetuped = true;
    }

    void Equip(int weapon)
    {
        var type = (WeaponType)weapon;
        Audio.gameObject.SetActive(Type == type);
        if (Type == type && EquipSound != null)
        {
            Audio.clip = EquipSound;
            Audio.Play();
        }
    }

    void Shoot()
    {
        if (!Audio.isActiveAndEnabled)
        {
            return;
        }
        if (FireParticle != null)
        {
            FireParticle.Play();
        }
        if (ShellParticle != null)
        {
            ShellParticle.Play();
        }
        if (FireSound != null)
        {
            Audio.clip = FireSound;
            Audio.Play();
        }
    }

    void Reload()
    {
        if (!Audio.isActiveAndEnabled)
        {
            return;
        }
        if (ReloadSound != null)
        {
            Audio.clip = ReloadSound;
            Audio.Play();
        }
    }
}
