using System.Collections;
using System.Collections.Generic;
using System.IO;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage), typeof(AudioSource))]
public class SmackerPlayerRenderer : MonoBehaviour
{
    public string SMKPath = "01-02A.SMK";

    private RawImage _rawImage;
    private SmackerPlayer _player;
    private AudioSource _audioSource;

    // Use this for initialization
    void Start ()
	{
	    var buffer =
	        File.ReadAllBytes(@"D:\spel\steam\steamapps\common\Star Wars Jedi Knight\Resource\VIDEO\" + SMKPath);

        _player = new SmackerPlayer(buffer);
	    
	    _rawImage = GetComponent<RawImage>();
	    _rawImage.texture = _player.Texture;

	    _audioSource = GetComponent<AudioSource>();
	    _audioSource.clip = _player.AudioClip;
	    _audioSource.Play();
    }

    private double _passedMs;
    private int framecnt = 0;

    // Update is called once per frame
	void Update ()
	{
	    _passedMs += Time.deltaTime * 1000;
	    if (_passedMs >= _player.MillisecondsPerFrame)
	    {
            _passedMs = 0;
            _player.Next();
	        _player.RenderVideo();
	        framecnt += 1;
	    }
	}

    void OnDestroy()
    {
        _player.Dispose();
    }
}
