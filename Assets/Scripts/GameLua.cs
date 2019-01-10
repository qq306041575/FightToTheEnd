using System;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using UniRx;
using UniRx.Toolkit;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using XLua;

public class GameLua : MonoBehaviour
{
    public Action<float> LuaUpdate;
    public Action<object> LuaHandle;

    Dictionary<string, ImpactEffectPool> _pools;
    LuaEnv _lua;
    float _lastGCTime;

    void Awake()
    {
        _lastGCTime = float.MaxValue;
        _pools = new Dictionary<string, ImpactEffectPool>();
        MessageBroker.Default.Receive<RaycastHit>().Subscribe(x =>
        {
            ImpactEffectPool pool;
            if (_pools.TryGetValue(x.transform.tag, out pool))
            {
                pool.Rent().Setup(x);
            }
        }).AddTo(this);
        MessageBroker.Default.Receive<ImpactEffect>().Subscribe(x =>
        {
            ImpactEffectPool pool;
            if (_pools.TryGetValue(x.tag, out pool))
            {
                pool.Return(x);
                x.transform.SetParent(transform);
            }
        }).AddTo(this);
        MessageBroker.Default.Receive<MsgData>().Subscribe(x =>
        {
            if (null != LuaHandle)
            {
                LuaHandle(x);
            }
        }).AddTo(this);
        MessageBroker.Default.Receive<ResData>().Subscribe(x =>
        {
            if (null != LuaHandle)
            {
                LuaHandle(x);
            }
        }).AddTo(this);
    }

    void Start()
    {
        var request = UnityWebRequest.GetAssetBundle(Application.streamingAssetsPath + "/lua");
        request.Send()
        .AsAsyncOperationObservable()
        .Last()
        .Select(x => { return DownloadHandlerAssetBundle.GetContent(request); })
        .Subscribe(x =>
        {
            var assets = x.LoadAllAssets<TextAsset>();
            x.Unload(false);
            _lua = new LuaEnv();
            _lua.AddLoader((ref string filepath) =>
            {
                var name = filepath.Substring(filepath.LastIndexOf('/') + 1);
                for (int i = 0; i < assets.Length; i++)
                {
                    if (assets[i].name == name)
                    {
                        return assets[i].bytes;
                    }
                }
                return null;
            });
            _lua.DoString("require('Lua/game')");
            _lastGCTime = 0;
        }, Debug.LogError);
    }

    void Update()
    {
        if (Time.time > _lastGCTime)
        {
            _lua.Tick();
            _lastGCTime = Time.time + 5;
        }
        if (null != LuaUpdate)
        {
            LuaUpdate(Time.deltaTime);
        }
    }

    void OnDestroy()
    {
        LuaUpdate = null;
        LuaHandle = null;
    }

    public void LuaAddEffect(AssetBundleInfo bundle, string effect)
    {
        var pool = new ImpactEffectPool(bundle, effect);
        pool.StartShrinkTimer(TimeSpan.FromSeconds(1), 0.9f, 10);
        pool.PreloadAsync(5, 1).Subscribe();
        _pools[effect] = pool;
    }

    public void LuaClearEffects()
    {
        foreach (var item in _pools.Values)
        {
            item.Dispose();
        }
        _pools.Clear();
    }
}

public class ImpactEffectPool : ObjectPool<ImpactEffect>
{
    AssetBundleInfo _bundle;
    string _effect;

    public ImpactEffectPool(AssetBundleInfo bundle, string effect)
    {
        _bundle = bundle;
        _effect = effect;
    }

    protected override ImpactEffect CreateInstance()
    {
        var asset = _bundle.LoadAsset(_effect);
        return asset.GetComponent<ImpactEffect>();
    }
}
