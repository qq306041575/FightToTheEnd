using Photon.Pun;
using Photon.Realtime;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class PhotonBot : MonoBehaviourPunCallbacks, IPunObservable
{
    IMessageBroker _message;
    RemotePlayer _bot;
    Transform _route;
    Vector3 _routePoint;
    WeaponData _weapon;
    int _pointIndex;
    bool _isMasterClient;
    bool _canShoot;
    float _shootTimer;
    float _lockTimer;

    void Awake()
    {
        name = GameSetting.Instance.Bot.Name;
        _message = new MessageBroker();
        _route = GameObject.Find("Position/Bot").transform;

        MessageBroker.Default.Receive<ResData>().Subscribe(x =>
        {
            if (GameConst.ResPlayerRemote == x.Type && x.Data != null)
            {
                var asset = x.Data.LoadAsset("RemoteBot");
                asset.SetParent(transform, false);
                _bot = asset.GetComponent<RemotePlayer>();
                _bot.Setup(_message, transform.position + Vector3.one * 0.0001f, transform.rotation);
            }
        }).AddTo(this);
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient || null == _bot)
        {
            return;
        }
        if (!_isMasterClient)
        {
            _isMasterClient = true;
            _pointIndex = GetClosestPointIndex(_route, transform.position);
            _routePoint = _route.GetChild(_pointIndex).position;
            _bot.Patrol(_routePoint);
            if (null == _weapon)
            {
                PhotonNetwork.CurrentRoom.SetBotHealth(photonView.ViewID, GameSetting.Instance.MaxHealth);
                photonView.RPC(GameConst.RpcEquip, RpcTarget.All, Random.Range(0, GameSetting.Instance.Weapons.Count));
            }
        }

        if (_shootTimer > 0)
        {
            _shootTimer -= Time.deltaTime;
        }
        else if (_canShoot)
        {
            _shootTimer = _weapon.FireRate;
            photonView.RPC(GameConst.RpcShoot, RpcTarget.All);
        }

        if (_lockTimer > 0)
        {
            _lockTimer -= Time.deltaTime;
            if (_lockTimer <= 0)
            {
                _bot.transform.rotation = Quaternion.LookRotation(_routePoint - _bot.transform.position);
                _bot.Patrol(_routePoint);
            }
        }
        else if (Vector3.Distance(_bot.transform.position, _routePoint) < 2f)
        {
            _pointIndex = _route.childCount > _pointIndex + 1 ? _pointIndex + 1 : 0;
            _routePoint = _route.GetChild(_pointIndex).position;
            _bot.Patrol(_routePoint);
        }
    }

    void FixedUpdate()
    {
        if (!_isMasterClient || null == _weapon)
        {
            return;
        }
        RaycastHit hit;
        if (_lockTimer > 0)
        {
            _canShoot = HasHit(_bot.ShootPoint.position, _bot.ShootPoint.forward, _weapon.FireRange, out hit);
            if (_canShoot)
            {
                _lockTimer = GameSetting.Instance.Bot.LockTime;
            }
            else if (_shootTimer <= 0)
            {
                _shootTimer = _weapon.FireRate * Random.Range(0.1f, 0.9f);
            }
        }
        else
        {
            var data = GameSetting.Instance.Bot;
            var angle = data.ScanAngle / data.ScanLine;
            for (int i = 0; i < data.ScanLine; i++)
            {
                var y = angle * i + Mathf.Repeat(angle * Time.time, angle) - data.ScanAngle * 0.5f;
                var direction = _bot.ShootPoint.rotation * Quaternion.Euler(0, y, 0) * Vector3.forward;
                if (HasHit(_bot.ShootPoint.position, direction, _weapon.FireRange, out hit))
                {
                    _bot.Lock(hit.transform.root.GetChild(0));
                    _lockTimer = data.LockTime;
                    _shootTimer = _weapon.FireRate;
                    break;
                }
            }
        }
    }

    int GetClosestPointIndex(Transform route, Vector3 position)
    {
        var index = 0;
        var distance = float.MaxValue;
        for (int i = 0; i < route.childCount; i++)
        {
            var d = Vector3.Distance(route.GetChild(i).position, position);
            if (distance > d)
            {
                distance = d;
                index = i;
            }
        }
        return index;
    }

    bool HasHit(Vector3 position, Vector3 direction, float distance, out RaycastHit hit)
    {
        // Debug.DrawRay(position, direction * distance, Color.red);
        return Physics.Raycast(position, direction, out hit, distance, Physics.AllLayers)
               && hit.transform.CompareTag(GameConst.TagFlesh)
               && !hit.transform.root.CompareTag(GameConst.TagAI);
    }

    [PunRPC]
    public void RpcEquip(int id)
    {
        _weapon = GameSetting.Instance.Weapons.Find(x => id == (int)x.Type);
        var data = new EquipData() { Weapon = id };
        _message.Publish(new MsgData() { Type = GameConst.MsgPlayEquip, Data = data });
    }

    [PunRPC]
    public void RpcShoot()
    {
        _message.Publish(new MsgData() { Type = GameConst.MsgShoot });
        Fire(_weapon, _bot.ShootPoint.position, _bot.ShootPoint.rotation);
    }

    void Fire(WeaponData weapon, Vector3 position, Quaternion rotation)
    {
        RaycastHit hit;
        for (int i = 0; i < weapon.FireBullet; i++)
        {
            var rotateY = Quaternion.Euler(0, Random.Range(-weapon.Spread, weapon.Spread), 0);
            var rotateZ = Quaternion.Euler(0, 0, Random.Range(0, 360));
            var direction = rotation * rotateZ * rotateY * Vector3.forward;
            if (HasHit(position, direction, weapon.FireRange, out hit))
            {
                var view = hit.transform.root.GetComponent<PhotonView>();
                Hurt(view, photonView.ViewID, weapon.Damage + Random.Range(1, 20));
            }
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
    }

    [PunRPC]
    public void RpcDamage(int attackerId, int health)
    {
        if (_lockTimer > 0)
        {
            _shootTimer += GameSetting.Instance.CoolDown;
            return;
        }
        var attacker = PhotonView.Find(attackerId).transform.GetChild(0);
        _bot.Lock(attacker);
        _lockTimer = GameSetting.Instance.Bot.LockTime;
        _shootTimer = _weapon.FireRate;
    }

    [PunRPC]
    public void RpcDie(int killerId)
    {
        if (photonView.IsMine)
        {
            enabled = false;
            Observable.Timer(System.TimeSpan.FromSeconds(3)).Subscribe(_ => PhotonNetwork.Destroy(gameObject)).AddTo(this);
        }
        var data = new DieData();
        data.Victim = name;
        var view = PhotonView.Find(killerId);
        data.Killer = view.name;
        data.IsKiller = view.Owner == PhotonNetwork.LocalPlayer;
        var msg = new MsgData() { Type = GameConst.MsgDie, Data = data };
        _message.Publish(msg);
        MessageBroker.Default.Publish(msg);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        _bot.OnPhotonSerializeView(stream, info);
    }
}
