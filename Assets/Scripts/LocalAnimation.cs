using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class LocalAnimation : MonoBehaviour
{
    void Awake()
    {
        object equipData = null;
        object reloadData = null;
        var weaponHash = Animator.StringToHash("Weapon");
        var shootHash = Animator.StringToHash("Shoot");
        var reloadHash = Animator.StringToHash("Reload");
        var takeinHash = Animator.StringToHash("TakeIn");
        var animator = GetComponent<Animator>();
        var trigger = animator.GetBehaviour<ObservableStateMachineTrigger>();
        trigger.OnStateEnterAsObservable().Subscribe(x =>
        {
            if (x.StateInfo.shortNameHash == takeinHash)
            {
                var msg = new MsgData() { Type = GameConst.MsgEquip, Data = equipData };
                MessageBroker.Default.Publish(msg);
            }
        }).AddTo(this);
        trigger.OnStateExitAsObservable().Subscribe(x =>
        {
            if (x.StateInfo.shortNameHash == reloadHash)
            {
                var msg = new MsgData() { Type = GameConst.MsgReload, Data = reloadData };
                MessageBroker.Default.Publish(msg);
            }
        }).AddTo(this);

        MessageBroker.Default.Receive<MsgData>().Subscribe(x =>
        {
            switch (x.Type)
            {
                case GameConst.MsgPlayEquip:
                    equipData = x.Data;
                    animator.SetInteger(weaponHash, (x.Data as EquipData).Weapon);
                    break;
                case GameConst.MsgPlayReload:
                    reloadData = x.Data;
                    animator.SetTrigger(reloadHash);
                    break;
                case GameConst.MsgShoot:
                    animator.SetTrigger(shootHash);
                    break;
                case GameConst.MsgDie:
                    if ((x.Data as DieData).IsVictim)
                    {
                        animator.SetInteger(weaponHash, -1);
                    }
                    break;
            }
        }).AddTo(this);
    }
}
