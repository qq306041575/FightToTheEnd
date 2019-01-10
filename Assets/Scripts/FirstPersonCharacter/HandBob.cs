using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class HandBob
{
    public float BobRange;

    Transform _hand;
    Vector3 _originalPosition;
    float _timer;

    public void Setup(Transform hand)
    {
        _hand = hand;
        _originalPosition = hand.localPosition;
    }

    public void DoHandBob(float speed)
    {
        var pos = _originalPosition;
        pos.x += Mathf.Sin(_timer) * speed * BobRange;
        pos.y += Mathf.Sin(_timer * 2) * speed * BobRange;
        _timer += Time.deltaTime * speed;
        _hand.localPosition = pos;
    }

    public void Reset()
    {
        _hand.localPosition = Vector3.Lerp(_hand.localPosition, _originalPosition, Time.deltaTime * 10);
        _timer = 0f;
    }
}
