using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class GameView : MonoBehaviour
{
    public Action<float> LuaUpdate;
    public Action<object> LuaHandle;

    List<Action> _luaClicks;

    void Awake()
    {
        _luaClicks = new List<Action>();
        MessageBroker.Default.Receive<MsgData>().Subscribe(x =>
        {
            if (null != LuaHandle)
            {
                LuaHandle(x);
            }
        }).AddTo(this);
    }

    void Update()
    {
        if (null != LuaUpdate)
        {
            LuaUpdate(Time.deltaTime);
        }
    }

    void OnDestroy()
    {
        LuaUpdate = null;
        LuaHandle = null;
        for (int i = 0; i < _luaClicks.Count; i++)
        {
            _luaClicks[i] = null;
        }
        _luaClicks.Clear();
    }

    void OnApplicationFocus(bool focus)
    {
        if (null != LuaHandle)
        {
            LuaHandle(new MsgData() { Type = GameConst.MsgApplicationFocus, Data = focus });
        }
    }

    public void LuaButtonClick(string path, Action click)
    {
        Button button = null;
        if (string.IsNullOrEmpty(path))
        {
            button = GetComponent<Button>();
        }
        else
        {
            button = transform.Find(path).GetComponent<Button>();
        }
        if (null == button)
        {
            Debug.LogWarningFormat("Button component not found, path = {0}", path);
            return;
        }
        var index = _luaClicks.Count;
        _luaClicks.Add(click);
        button.onClick.AddListener(() => _luaClicks[index]());
    }

    public void LuaLerpSize(RectTransform ui, float size, float delta)
    {
        ui.sizeDelta = Vector2.Lerp(ui.sizeDelta, Vector2.one * size, delta);
    }

    public void LuaSetScale(Text ui, float scale)
    {
        ui.transform.localScale = Vector3.one * scale;
    }

    public void LuaSetAlpha(Graphic ui, float alpha)
    {
        var color = ui.color;
        color.a = alpha;
        ui.color = color;
    }

    public void LuaRotateIndicator(GameObject uiIndicator, Transform from, Transform to)
    {
        var direction = to.position - from.position;
        direction.y = 0;
        direction /= Mathf.Abs(direction.x) + Mathf.Abs(direction.z);
        var angle = 1 - Vector3.Dot(from.forward, direction);
        angle *= Vector3.Cross(from.forward, direction).y > 0 ? -90 : 90;
        uiIndicator.transform.eulerAngles = Vector3.forward * angle;
    }

    public void LuaSetCursor(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void LuaQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif !UNITY_WEBGL
        Application.Quit();
#endif
    }
}
