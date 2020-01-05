using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    class SmackerPlayer : IDisposable
    {
        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr smk_open_memory(byte[] buffer, uint size);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern void smk_close(IntPtr smk);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern sbyte smk_info_all(IntPtr smk, out uint frame, out uint frame_count, out double usf);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern sbyte smk_info_video(IntPtr smk, out uint w, out uint h, out byte y_scale_mode);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern sbyte smk_info_audio(IntPtr smk, out byte track_mask, byte[] channels, byte[] bitdepth, uint[] audio_rate);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern sbyte smk_enable_video(IntPtr smk, byte enable);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern sbyte smk_enable_audio(IntPtr smk, byte track, byte enable);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr smk_get_audio(IntPtr smk, byte track);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint smk_get_audio_size(IntPtr smk, byte track);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern sbyte smk_first(IntPtr smk);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern sbyte smk_next(IntPtr smk);

        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern sbyte smk_seek_keyframe(IntPtr smk, uint frame);

        //render24bpp(smk object, unsigned char * destBuffer, unsigned int bufferSize, unsigned int videoBufferSize) {
        [DllImport(@"libsmacker", CallingConvention = CallingConvention.Cdecl)]
        private static extern void render24bpp(IntPtr smk, IntPtr destBuffer, uint videoBufferSize, bool bgr);

        private IntPtr _smk;
        private uint _w, _h;
        private byte _yScaleMode;
        private byte _trackMask;
        private readonly byte[] _channels = new byte[7];
        private readonly byte[] _bitdepth = new byte[7];
        private readonly uint[] _audioRate = new uint[7];
        private uint _frame;
        private uint _frame_count;
        private double _usf;

        private IntPtr _videoBuffer24bpp;

        public uint Width { get { return _w; } }
        public uint Height { get { return _h; } }
        public double MillisecondsPerFrame { get { return _usf / 1000.0; } }
        public Texture2D Texture { get; private set; }
        public AudioClip AudioClip { get; private set; }

        public SmackerPlayer(byte[] buffer)
        {
            _smk = smk_open_memory(buffer, (uint)buffer.Length);

            if (smk_info_all(_smk, out _frame, out _frame_count, out _usf) < 0)
                throw new Exception("Failed to get all info");

            if (smk_info_video(_smk, out _w, out _h, out _yScaleMode) < 0)
                throw new Exception("Failed to get video info");

            Texture = new Texture2D((int)_w, (int)_h, TextureFormat.RGB24, false);
            _videoBuffer24bpp = Marshal.AllocHGlobal((int)(_w * _h * 3));

            if (smk_info_audio(_smk, out _trackMask, _channels, _bitdepth, _audioRate) < 0)
                throw new Exception("Failed to get audio info");

            var samplerate = (int)_audioRate[0];
            var secondsPerFrame = _usf / 1000000.0;
            var videoLengthInSecs = (_frame_count + 1) * secondsPerFrame;
            AudioClip = AudioClip.Create("Smacker Audio", (int)(samplerate * videoLengthInSecs), _channels[0], samplerate, true, OnReadSample);

            smk_enable_video(_smk, 1);
            smk_enable_audio(_smk, 0, 1);

            smk_first(_smk);
        }

        public void Dispose()
        {
            smk_close(_smk);
            Marshal.FreeHGlobal(_videoBuffer24bpp);
        }

        private uint _audioSize;
        private IntPtr _audioPtr;
        private byte[] _currentAudioBuffer;
        private Queue<byte[]> _buffers = new Queue<byte[]>();
        private int _audioBufferIdx;

        void OnReadSample(float[] data)
        {
            var bitDepth = _bitdepth[0];
            for (int i = 0; i < data.Length; i++)
            {
                float value = 0;
                bool gotBuffer = true;
                if (_currentAudioBuffer == null || _audioBufferIdx == _currentAudioBuffer.Length)
                {
                    if (_buffers.Count > 0)
                    {
                        _currentAudioBuffer = _buffers.Dequeue();
                        _audioBufferIdx = 0;
                    }
                    else
                    {
                        _currentAudioBuffer = null;
                        gotBuffer = false;
                    }
                }
                if (gotBuffer)
                {
                    if (bitDepth == 8)
                        value = _currentAudioBuffer[_audioBufferIdx] / 255.0f - 0.5f;
                    else if (bitDepth == 16)
                        value = (short)((_currentAudioBuffer[_audioBufferIdx + 1] << 8) | _currentAudioBuffer[_audioBufferIdx]) / (float)short.MaxValue;
                }

                data[i] = value;
                if (bitDepth == 16)
                    _audioBufferIdx += 2;
                else if (bitDepth == 8)
                    _audioBufferIdx++;
            }
        }

        public void RenderVideo()
        {
            smk_next(_smk);

            _audioSize = smk_get_audio_size(_smk, 0);
            _audioPtr = smk_get_audio(_smk, 0);
            var audio = new byte[_audioSize];
            Marshal.Copy(_audioPtr, audio, 0, audio.Length);
            _buffers.Enqueue(audio);

            render24bpp(_smk, _videoBuffer24bpp, _w * _h, false);

            Texture.LoadRawTextureData(_videoBuffer24bpp, (int)(_w * _h * 3));
            Texture.Apply(false);
        }
    }
}
