using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage), typeof(AudioSource))]
public class SmackerPlayerRenderer : MonoBehaviour
{
    public string SMKPath;

    private RawImage _rawImage;
    private SmackerPlayer _player;
    private AudioSource _audioSource;

    // Use this for initialization
    void Start()
    {
        var buffer =
            File.ReadAllBytes(SMKPath);

        _player = new SmackerPlayer(buffer);

        _rawImage = GetComponent<RawImage>();
        _rawImage.texture = _player.Texture;

        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = _player.AudioClip;
        _audioSource.Play();

    }

    private double _passedMs;

    // Update is called once per frame
    void Update()
    {
        _passedMs += Time.deltaTime * 1000;
        if (_passedMs >= _player.MillisecondsPerFrame)
        {
            _passedMs = _passedMs - _player.MillisecondsPerFrame;
            _player.RenderVideo();
        }
    }

    void OnDestroy()
    {
        _player.Dispose();
    }
}
