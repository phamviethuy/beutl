﻿using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Beutl.Media.Music;
using Beutl.Media.Music.Samples;

namespace Beutl.Audio.Effects;

sealed unsafe file class SimpleCircularBuffer<T> : IDisposable
    where T : unmanaged
{
    private readonly T* _buffer;
    private readonly int _length = 1024;
    private readonly int _wrapMask = 1023;
    private int _writeIndex = 0;
    private bool _disposedValue;

    public SimpleCircularBuffer(int length)
    {
        _writeIndex = 0;
        _length = (int)Math.Pow(2, Math.Ceiling(Math.Log(length) / Math.Log(2)));
        _wrapMask = _length - 1;
        //Debug.WriteLine($"Original Length: {length}, Length: {_length}");

        _buffer = (T*)NativeMemory.AllocZeroed((nuint)(_length * sizeof(T)));
    }

    ~SimpleCircularBuffer()
    {
        Dispose();
    }

    public T Read(int offset)
    {
        ThrowIfDisposed();

        int readIndex = _writeIndex - offset;
        // インデックスを折り返す
        readIndex &= _wrapMask;

        if (readIndex >= _length)
            throw new ArgumentOutOfRangeException(nameof(offset));

        return _buffer[readIndex];
    }

    public void Write(T input)
    {
        ThrowIfDisposed();

        if (_writeIndex >= _length)
            throw new IndexOutOfRangeException("書き込みインデックが範囲外です。");

        _buffer[_writeIndex++] = input;
        // インデックスを折り返す
        _writeIndex &= _wrapMask;
    }

    public void Dispose()
    {
        if (!_disposedValue)
        {
            NativeMemory.Free(_buffer);
            _disposedValue = true;
        }

        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (_disposedValue)
            throw new ObjectDisposedException(GetType().Name);
    }
}

public sealed class Delay : SoundEffect
{
    public static readonly CoreProperty<float> DelayTimeProperty;
    public static readonly CoreProperty<float> FeedbackProperty;
    public static readonly CoreProperty<float> DryMixProperty;
    public static readonly CoreProperty<float> WetMixProperty;
    private const float MaxDelayTime = 5;
    private float _delayTime = 0.2f;
    private float _feedback = 0.5f;
    private float _dryMix = 0.6f;
    private float _wetMix = 0.4f;

    static Delay()
    {
        DelayTimeProperty = ConfigureProperty<float, Delay>(o => o.DelayTime)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .SerializeName("delay-time")
            .DefaultValue(0.2f)
            .Maximum(MaxDelayTime)
            .Register();

        FeedbackProperty = ConfigureProperty<float, Delay>(o => o.Feedback)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .SerializeName("feedback")
            .DefaultValue(0.5f)
            .Minimum(0)
            .Register();

        DryMixProperty = ConfigureProperty<float, Delay>(o => o.DryMix)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .SerializeName("dry-mix")
            .DefaultValue(0.6f)
            .Minimum(0)
            .Register();

        WetMixProperty = ConfigureProperty<float, Delay>(o => o.WetMix)
            .PropertyFlags(PropertyFlags.All & ~PropertyFlags.Animatable)
            .SerializeName("wet-mix")
            .DefaultValue(0.4f)
            .Minimum(0)
            .Register();

        AffectsRender<Delay>(
            DelayTimeProperty, FeedbackProperty,
            DryMixProperty, WetMixProperty);
    }

    public float DelayTime
    {
        get => _delayTime;
        set => SetAndRaise(DelayTimeProperty, ref _delayTime, value);
    }

    public float Feedback
    {
        get => _feedback;
        set => SetAndRaise(FeedbackProperty, ref _feedback, value);
    }

    public float DryMix
    {
        get => _dryMix;
        set => SetAndRaise(DryMixProperty, ref _dryMix, value);
    }

    public float WetMix
    {
        get => _wetMix;
        set => SetAndRaise(WetMixProperty, ref _wetMix, value);
    }

    public override ISoundProcessor CreateProcessor()
    {
        return new DelayProcessor(_delayTime, _feedback, _dryMix, _wetMix);
    }

    private sealed class DelayProcessor : ISoundProcessor
    {
        private readonly float _delayTime;
        private readonly float _feedback;
        private readonly float _dryMix;
        private readonly float _wetMix;
        private SimpleCircularBuffer<Vector2>? _delayBuffer;

        public DelayProcessor(float delayTime, float feedback, float dryMix, float wetMix)
        {
            _delayTime = delayTime;
            _feedback = feedback;
            _dryMix = dryMix;
            _wetMix = wetMix;
        }

        ~DelayProcessor()
        {
            _delayBuffer?.Dispose();
            _delayBuffer = null;
        }

        public void Dispose()
        {
            _delayBuffer?.Dispose();
            _delayBuffer = null;
            GC.SuppressFinalize(this);
        }

        [MemberNotNull(nameof(_delayBuffer))]
        private void Initialize(Pcm<Stereo32BitFloat> pcm)
        {
            _delayBuffer ??= new SimpleCircularBuffer<Vector2>((int)(MaxDelayTime * pcm.SampleRate));
        }

        public void Process(in Pcm<Stereo32BitFloat> src, out Pcm<Stereo32BitFloat> dst)
        {
            int sampleRate = src.SampleRate;

            Initialize(src);
            Span<Stereo32BitFloat> channel_data = src.DataSpan;

            for (int sample = 0; sample < channel_data.Length; sample++)
            {
                ref Vector2 input = ref Unsafe.As<Stereo32BitFloat, Vector2>(ref channel_data[sample]);

                Vector2 delay = _delayBuffer.Read((int)(_delayTime * sampleRate));

                _delayBuffer.Write(input + (_feedback * delay));

                input = ((_dryMix) * input) + (_wetMix * delay);
            }

            dst = src;
        }
    }
}