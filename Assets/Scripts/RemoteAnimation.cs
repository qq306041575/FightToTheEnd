using System.Collections;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class RemoteAnimation : MonoBehaviour
{
    public IMessageBroker Message { get; set; }
    public Vector3 AimPosition { get; set; }
    public bool IsGrounded { get; set; }
    public float VelocityX { get; set; }
    public float VelocityZ { get; set; }

    void Start()
    {
        var animator = GetComponent<Animator>();
        var horizontalHash = Animator.StringToHash("Horizontal");
        var verticalHash = Animator.StringToHash("Vertical");
        var isGroundedHash = Animator.StringToHash("IsGrounded");
        var horizontal = VelocityX;
        var vertical = VelocityZ;
        var isGrounded = IsGrounded;
        this.UpdateAsObservable().Subscribe(_ =>
        {
            horizontal = Mathf.Lerp(horizontal, VelocityX, Time.deltaTime * 10);
            vertical = Mathf.Lerp(vertical, VelocityZ, Time.deltaTime * 10);
            animator.SetFloat(horizontalHash, horizontal);
            animator.SetFloat(verticalHash, vertical);
            if (isGrounded != IsGrounded)
            {
                isGrounded = IsGrounded;
                animator.SetBool(isGroundedHash, isGrounded);
            }
        });

        var spineEuler = animator.GetBoneTransform(HumanBodyBones.Spine).localEulerAngles;
        var aimPosition = AimPosition;
        var isDead = false;
        this.OnAnimatorIKAsObservable().Subscribe(x =>
        {
            if (isDead)
            {
                animator.SetLookAtWeight(0);
                return;
            }
            animator.SetLookAtWeight(1f, 0.8f, 1f, 1f, 1f);
            aimPosition = Vector3.Slerp(aimPosition, AimPosition, Time.deltaTime * 10);
            animator.SetLookAtPosition(aimPosition);
            spineEuler.z = animator.GetBoneTransform(HumanBodyBones.Spine).localEulerAngles.z;
            animator.SetBoneLocalRotation(HumanBodyBones.Spine, Quaternion.Euler(spineEuler));
        });

        var weaponHash = Animator.StringToHash("Weapon");
        var shootHash = Animator.StringToHash("Shoot");
        var reloadHash = Animator.StringToHash("Reload");
        var dieHash = Animator.StringToHash("Die");
        Message.Receive<MsgData>().Subscribe(x =>
        {
            switch (x.Type)
            {
                case GameConst.MsgPlayEquip:
                    animator.SetInteger(weaponHash, (x.Data as EquipData).Weapon);
                    break;
                case GameConst.MsgPlayReload:
                    animator.SetTrigger(reloadHash);
                    break;
                case GameConst.MsgShoot:
                    animator.SetTrigger(shootHash);
                    break;
                case GameConst.MsgDie:
                    isDead = true;
                    animator.SetTrigger(dieHash);
                    animator.SetInteger(weaponHash, -1);
                    break;
            }
        }).AddTo(this);
    }
}
