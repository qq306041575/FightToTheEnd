using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

public class ResourceManager
{
    private static ResourceManager _instance;
    public static ResourceManager Instance
    {
        get
        {
            if (null == _instance)
            {
                _instance = new ResourceManager();
            }
            return _instance;
        }
    }

    AssetBundleManifest _manifest;
    List<AssetBundleInfo> _bundles;
    Queue<string> _requests;

    public int LuaGetPlatform()
    {
        return (int)Application.platform;
    }

    public void LuaLoadManifest(string manifestName)
    {
        var path = Application.streamingAssetsPath + "/" + manifestName;
        var request = UnityWebRequest.GetAssetBundle(path);
        request.Send()
        .AsAsyncOperationObservable()
        .Last()
        .Select(x => { return DownloadHandlerAssetBundle.GetContent(request); })
        .Subscribe(x =>
        {
            _manifest = x.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            _bundles = new List<AssetBundleInfo>();
            _requests = new Queue<string>();
            x.Unload(false);
            MessageBroker.Default.Publish(new ResData() { Type = GameConst.ResManifest });
        }, Debug.LogError);
    }

    public void LuaUnloadUnused()
    {
        Resources.UnloadUnusedAssets();
    }

    public void LuaUnloadAsync(string bundleName, bool allDestroyed, float seconds)
    {
        Observable.Timer(TimeSpan.FromSeconds(seconds)).Subscribe(_ => LuaUnload(bundleName, allDestroyed));
    }

    public void LuaUnload(string bundleName, bool allDestroyed)
    {
        var bundle = _bundles.Find(x => x.Name == bundleName);
        if (null == bundle)
        {
            return;
        }
        if (!bundle.Unload(allDestroyed))
        {
            return;
        }
        _bundles.Remove(bundle);

        var items = _manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < items.Length; i++)
        {
            LuaUnload(items[i], allDestroyed);
        }
    }

    public void LuaLoadBundle(string bundleName)
    {
        _requests.Enqueue(bundleName);
        if (_requests.Count == 1)
        {
            LoadAsync(bundleName);
        }
    }

    void LoadAsync(string bundleName)
    {
        var bundle = _bundles.Find(x => x.Name == bundleName);
        if (null != bundle)
        {
            bundle.AddReference();
            OnLoadSuccess(bundle);
            return;
        }
        var stream = Observable.NextFrame();
        var path = Application.streamingAssetsPath + "/";
        var items = _manifest.GetAllDependencies(bundleName);
        for (int i = 0; i < items.Length; i++)
        {
            bundle = _bundles.Find(x => string.Equals(x.Name, items[i]));
            if (null != bundle)
            {
                bundle.AddReference();
                continue;
            }
            stream = stream.ContinueWith(LoadDependency(path, items[i]));
        }
        stream.ContinueWith(LoadBundle(path, bundleName)).Subscribe(OnLoadSuccess, OnLoadError);
    }

    IObservable<Unit> LoadDependency(string path, string bundleName)
    {
        var hash = _manifest.GetAssetBundleHash(bundleName);
        var request = UnityWebRequest.GetAssetBundle(path + bundleName, hash, 0);
        return request.Send()
               .AsAsyncOperationObservable()
               .Last()
               .Select(x =>
               {
                   var ab = DownloadHandlerAssetBundle.GetContent(request);
                   _bundles.Add(new AssetBundleInfo(bundleName, ab));
                   return Unit.Default;
               });
    }

    IObservable<AssetBundleInfo> LoadBundle(string path, string bundleName)
    {
        var hash = _manifest.GetAssetBundleHash(bundleName);
        var request = UnityWebRequest.GetAssetBundle(path + bundleName, hash, 0);
        return request.Send()
               .AsAsyncOperationObservable()
               .Last()
               .Select(x =>
               {
                   var ab = DownloadHandlerAssetBundle.GetContent(request);
                   var info = new AssetBundleInfo(bundleName, ab);
                   _bundles.Add(info);
                   return info;
               });
    }

    void OnLoadSuccess(AssetBundleInfo bundle)
    {
        var msg = new ResData() { Type = _requests.Dequeue(), Data = bundle };
        if (_requests.Count > 0)
        {
            LoadAsync(_requests.Peek());
        }
        MessageBroker.Default.Publish(msg);
    }

    void OnLoadError(Exception ex)
    {
        var msg = new ResData() { Type = _requests.Dequeue() };
        if (_requests.Count > 0)
        {
            LoadAsync(_requests.Peek());
        }
        MessageBroker.Default.Publish(msg);
        Debug.LogErrorFormat("Bundle Name:{0}, Error:{1}", msg.Type, ex.Message);
    }
}

public class AssetBundleInfo
{
    public string Name { get; private set; }

    AssetBundle _bundle;
    List<GameObject> _assets;
    int _refCount;

    public AssetBundleInfo(string bundleName, AssetBundle bundle)
    {
        Name = bundleName;
        _bundle = bundle;
        _refCount = 1;
        _assets = new List<GameObject>();
    }

    public void AddReference()
    {
        _refCount++;
    }

    public Transform LoadAsset(string assetName)
    {
        var prefab = _bundle.LoadAsset<GameObject>(assetName);
        var asset = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _assets.Add(asset);
        return asset.transform;
    }

    public bool Unload(bool allDestroyed)
    {
        _refCount--;
        if (_refCount > 0)
        {
            return false;
        }
        for (int i = 0; i < _assets.Count && allDestroyed; i++)
        {
            GameObject.Destroy(_assets[i]);
        }
        _assets.Clear();
        if (null != _bundle)
        {
            _bundle.Unload(allDestroyed);
            _bundle = null;
        }
        return true;
    }
}
