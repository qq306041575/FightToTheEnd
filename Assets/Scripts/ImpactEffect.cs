using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class ImpactEffect : MonoBehaviour
{
    ParticleSystem _effect;
    AudioSource _audio;
    float _timer;

    void Awake()
    {
        _effect = GetComponent<ParticleSystem>();
        _audio = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (_timer > 0)
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0)
            {
                MessageBroker.Default.Publish(this);
            }
        }
    }

    public void Setup(RaycastHit hit)
    {
        transform.position = hit.point;
        transform.rotation = Quaternion.LookRotation(hit.normal);
        transform.SetParent(hit.transform);
        if (_effect != null)
        {
            _effect.Play();
        }
        if (_audio != null)
        {
            _audio.Play();
        }
        _timer = 10f;
    }
}
