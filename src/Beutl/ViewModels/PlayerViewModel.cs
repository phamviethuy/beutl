﻿using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using Beutl.Audio.Platforms.OpenAL;
using Beutl.Audio.Platforms.XAudio2;
using Beutl.Configuration;
using Beutl.Media.Music;
using Beutl.Media.Music.Samples;
using Beutl.Media.Pixel;
using Beutl.ProjectSystem;
using Beutl.Rendering;
using Beutl.Services;

using OpenTK.Audio.OpenAL;

using Reactive.Bindings;

using Serilog;

using Vortice.Multimedia;

namespace Beutl.ViewModels;

public sealed class PlayerViewModel : IDisposable
{
    private static readonly TimeSpan s_second = TimeSpan.FromSeconds(1);
    private readonly ILogger _logger = Log.ForContext<PlayerViewModel>();
    private readonly CompositeDisposable _disposables = new();
    private readonly ReactivePropertySlim<bool> _isEnabled;
    private readonly EditViewModel _editViewModel;
    private CancellationTokenSource? _cts;

    public PlayerViewModel(EditViewModel editViewModel)
    {
        _editViewModel = editViewModel;
        Scene = editViewModel.Scene;
        _isEnabled = editViewModel.IsEnabled;
        PlayPause = new ReactiveCommand(_isEnabled)
            .WithSubscribe(() =>
            {
                if (IsPlaying.Value)
                {
                    Pause();
                }
                else
                {
                    Play();
                }
            })
            .DisposeWith(_disposables);

        Next = new ReactiveCommand(_isEnabled)
            .WithSubscribe(() =>
            {
                int rate = GetFrameRate();

                Scene.CurrentFrame += TimeSpan.FromSeconds(1d / rate);
            })
            .DisposeWith(_disposables);

        Previous = new ReactiveCommand(_isEnabled)
            .WithSubscribe(() =>
            {
                int rate = GetFrameRate();

                Scene.CurrentFrame -= TimeSpan.FromSeconds(1d / rate);
            })
            .DisposeWith(_disposables);

        Start = new ReactiveCommand(_isEnabled)
            .WithSubscribe(() => Scene.CurrentFrame = TimeSpan.Zero)
            .DisposeWith(_disposables);

        End = new ReactiveCommand(_isEnabled)
            .WithSubscribe(() => Scene.CurrentFrame = Scene.Duration)
            .DisposeWith(_disposables);

        Scene.Renderer.RenderInvalidated += Renderer_RenderInvalidated;
        Scene.GetPropertyChangedObservable(Scene.RendererProperty)
            .Subscribe(a =>
            {
                if (a.OldValue != null)
                {
                    a.OldValue.RenderInvalidated -= Renderer_RenderInvalidated;
                }

                if (a.NewValue != null)
                {
                    a.NewValue.RenderInvalidated += Renderer_RenderInvalidated;
                }
            })
            .DisposeWith(_disposables);

        _isEnabled.Subscribe(v =>
            {
                if (!v && IsPlaying.Value)
                {
                    Pause();
                }
            })
            .DisposeWith(_disposables);

        CurrentFrame = Scene.GetObservable(Scene.CurrentFrameProperty)
            .ToReactiveProperty()
            .DisposeWith(_disposables);
        CurrentFrame.Subscribe(UpdateCurrentFrame)
            .DisposeWith(_disposables);

        Duration = Scene.GetObservable(Scene.DurationProperty)
            .ToReadOnlyReactiveProperty()
            .DisposeWith(_disposables);
    }

    public Scene? Scene { get; set; }

    public Project? Project => Scene?.FindHierarchicalParent<Project>();

    public ReactivePropertySlim<IImage> PreviewImage { get; } = new();

    public ReactivePropertySlim<bool> IsPlaying { get; } = new();

    public ReactiveProperty<TimeSpan> CurrentFrame { get; }

    public ReadOnlyReactiveProperty<TimeSpan> Duration { get; }

    public ReactiveCommand PlayPause { get; }

    public ReactiveCommand Next { get; }

    public ReactiveCommand Previous { get; }

    public ReactiveCommand Start { get; }

    public ReactiveCommand End { get; }

    public event EventHandler? PreviewInvalidated;

    // View側から設定
    public Size MaxFrameSize { get; set; }

    public async void Play()
    {
        if (!_isEnabled.Value || Scene == null)
            return;

        IRenderer renderer = Scene.Renderer;
        renderer.RenderInvalidated -= Renderer_RenderInvalidated;

        IsPlaying.Value = true;
        int rate = GetFrameRate();

        PlayAudio(Scene);

        TimeSpan tick = TimeSpan.FromSeconds(1d / rate);
        TimeSpan curFrame = Scene.CurrentFrame;
        TimeSpan duration = Scene.Duration;

        using (var timer = new PeriodicTimer(tick))
        {
            DateTime dateTime = DateTime.UtcNow;
            while (await timer.WaitForNextTickAsync()
                && curFrame <= duration
                && IsPlaying.Value)
            {
                curFrame += tick;
                Render(renderer, curFrame);
            }
        }

        renderer.RenderInvalidated += Renderer_RenderInvalidated;
    }

    private int GetFrameRate()
    {
        int rate = Project?.GetFrameRate() ?? 30;
        if (rate <= 0)
        {
            rate = 30;
        }

        return rate;
    }

    private async void PlayAudio(Scene scene)
    {
        if (OperatingSystem.IsWindows())
        {
            using var audioContext = new XAudioContext();
            await PlayWithXA2(audioContext, scene).ConfigureAwait(false);
        }
        else
        {
            using var audioContext = new AudioContext();
            await PlayWithOpenAL(audioContext, scene).ConfigureAwait(false);
        }
    }

    private static Pcm<Stereo32BitFloat>? FillAudioData(TimeSpan f, IComposer composer)
    {
        if (composer.Compose(f) is { } pcm)
        {
            return pcm;
        }
        else
        {
            return null;
        }
    }

    private static void Swap<T>(ref T x, ref T y)
    {
        T temp = x;
        x = y;
        y = temp;
    }

    private async Task PlayWithXA2(XAudioContext audioContext, Scene scene)
    {
        IComposer composer = scene.Composer;
        int sampleRate = composer.SampleRate;
        TimeSpan cur = scene.CurrentFrame;
        var fmt = new WaveFormat(sampleRate, 32, 2);
        var source = new XAudioSource(audioContext);
        var primaryBuffer = new XAudioBuffer();
        var secondaryBuffer = new XAudioBuffer();

        void PrepareBuffer(XAudioBuffer buffer)
        {
            Pcm<Stereo32BitFloat>? pcm = FillAudioData(cur, composer);
            if (pcm != null)
            {
                buffer.BufferData(pcm.DataSpan, fmt);
            }
            source.QueueBuffer(buffer);
        }

        IDisposable revoker = IsPlaying.Where(v => !v)
            .Subscribe(_ => source.Stop());

        try
        {
            PrepareBuffer(primaryBuffer);

            cur += s_second;
            PrepareBuffer(secondaryBuffer);

            source.Play();

            await Task.Delay(1000).ConfigureAwait(false);

            // primaryBufferが終了、secondaryが開始

            while (cur < scene.Duration)
            {
                if (!IsPlaying.Value)
                {
                    source.Stop();
                    break;
                }

                cur += s_second;

                PrepareBuffer(primaryBuffer);

                // バッファを入れ替える
                Swap(ref primaryBuffer, ref secondaryBuffer);

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Telemetry.Exception(ex);
            NotificationService.ShowError(Message.AnUnexpectedErrorHasOccurred, Message.An_exception_occurred_during_audio_playback);
            _logger.Error(ex, "An exception occurred during audio playback.");
            IsPlaying.Value = false;
        }
        finally
        {
            revoker.Dispose();
            source.Dispose();
            primaryBuffer.Dispose();
            secondaryBuffer.Dispose();
        }
    }

    private async Task PlayWithOpenAL(AudioContext audioContext, Scene scene)
    {
        try
        {
            audioContext.MakeCurrent();

            IComposer composer = scene.Composer;
            TimeSpan cur = scene.CurrentFrame;
            var buffers = AL.GenBuffers(2);
            var source = AL.GenSource();

            foreach (var buffer in buffers)
            {
                using var pcmf = FillAudioData(cur, composer);
                cur += s_second;
                if (pcmf != null)
                {
                    using var pcm = pcmf.Convert<Stereo16BitInteger>();

                    AL.BufferData<Stereo16BitInteger>(buffer, ALFormat.Stereo16, pcm.DataSpan, pcm.SampleRate);
                }

                AL.SourceQueueBuffer(source, buffer);
            }

            AL.SourcePlay(source);

            while (IsPlaying.Value)
            {
                AL.GetSource(source, ALGetSourcei.BuffersProcessed, out var processed);
                while (processed > 0)
                {
                    using Pcm<Stereo32BitFloat>? pcmf = FillAudioData(cur, composer);
                    cur += s_second;
                    int buffer = AL.SourceUnqueueBuffer(source);
                    if (pcmf != null)
                    {
                        using var pcm = pcmf.Convert<Stereo16BitInteger>();

                        AL.BufferData<Stereo16BitInteger>(buffer, ALFormat.Stereo16, pcm.DataSpan, pcm.SampleRate);
                    }

                    AL.SourceQueueBuffer(source, buffer);
                    processed--;
                }

                await Task.Delay(1000).ConfigureAwait(false);
                if (cur > scene.Duration)
                    break;
            }

            while (AL.GetSourceState(source) == ALSourceState.Playing)
            {
                await Task.Delay(100).ConfigureAwait(false);
            }

            AL.DeleteBuffers(buffers);
            AL.DeleteSource(source);
        }
        catch (Exception ex)
        {
            Telemetry.Exception(ex);
            NotificationService.ShowError(Message.AnUnexpectedErrorHasOccurred, Message.An_exception_occurred_during_audio_playback);
            _logger.Error(ex, "An exception occurred during audio playback.");
            IsPlaying.Value = false;
        }
    }

    public void Pause()
    {
        IsPlaying.Value = false;
    }

    private void Render(IRenderer renderer, TimeSpan timeSpan)
    {
        if (renderer.IsGraphicsRendering)
            return;

        RenderThread.Dispatcher.Dispatch(() =>
        {
            try
            {
                if (IsPlaying.Value && renderer.Render(timeSpan))
                {
                    using var bitmap = renderer.Snapshot();
                    UpdateImage(bitmap);

                    if (Scene != null)
                        Scene.CurrentFrame = timeSpan;
                }
            }
            catch (Exception ex)
            {
                Telemetry.Exception(ex);
                NotificationService.ShowError(Message.AnUnexpectedErrorHasOccurred, Message.An_exception_occurred_while_drawing_frame);
                _logger.Error(ex, "An exception occurred while drawing the frame.");
                IsPlaying.Value = false;
            }
        });
    }

    private unsafe void UpdateImage(Media.Bitmap<Bgra8888> source)
    {
        WriteableBitmap bitmap;

        if (PreviewImage.Value is WriteableBitmap bitmap1 &&
            bitmap1.PixelSize.Width == source.Width &&
            bitmap1.PixelSize.Height == source.Height)
        {
            bitmap = bitmap1;
        }
        else
        {
            bitmap = new WriteableBitmap(
                new(source.Width, source.Height),
                new(96, 96),
                PixelFormat.Bgra8888, AlphaFormat.Premul);
        }

        PreviewImage.Value = bitmap;
        using (ILockedFramebuffer buf = bitmap.Lock())
        {
            int size = source.ByteCount;
            Buffer.MemoryCopy((void*)source.Data, (void*)buf.Address, size, size);
        }

        PreviewInvalidated?.Invoke(this, EventArgs.Empty);
    }

    private void DrawBoundaries(Renderer renderer)
    {
        int? selected = _editViewModel.SelectedLayerNumber.Value;
        if (selected.HasValue)
        {
            var frameSize = new Size(renderer.FrameSize.Width, renderer.FrameSize.Height);
            float scale = (float)Stretch.Uniform.CalculateScaling(MaxFrameSize, frameSize, StretchDirection.Both).X;
            if (scale == 0)
                scale = 1;

            Graphics.ImmediateCanvas canvas = Renderer.GetInternalCanvas(renderer);
            Graphics.Rect[] boundary = renderer.RenderScene[selected.Value].GetBoundaries();
            if (boundary.Length > 0)
            {
                var pen = new Media.Immutable.ImmutablePen(Media.Brushes.White, null, 0, 1 / scale);
                bool exactBounds = GlobalConfiguration.Instance.ViewConfig.ShowExactBoundaries;

                foreach (Graphics.Rect item in renderer.RenderScene[selected.Value].GetBoundaries())
                {
                    var rect = item;
                    if (!exactBounds)
                    {
                        rect = item.Inflate(4 / scale);
                    }

                    canvas.DrawRectangle(rect, null, pen);
                }
            }
        }
    }

    private void Renderer_RenderInvalidated(object? sender, TimeSpan e)
    {
        void RenderOnRenderThread()
        {
            RenderThread.Dispatcher.Dispatch(() =>
            {
                try
                {
                    if (Scene is { Renderer: Renderer renderer }
                        && renderer.Render(Scene.CurrentFrame))
                    {
                        DrawBoundaries(renderer);

                        using Media.Bitmap<Bgra8888> bitmap = renderer.Snapshot();
                        UpdateImage(bitmap);
                    }
                }
                catch (Exception ex)
                {
                    Telemetry.Exception(ex);
                    NotificationService.ShowError(Message.AnUnexpectedErrorHasOccurred, Message.An_exception_occurred_while_drawing_frame);
                    _logger.Error(ex, "An exception occurred while drawing the frame.");
                }
            });
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
            RenderOnRenderThread,
            Avalonia.Threading.DispatcherPriority.Background,
            _cts.Token);
    }

    private void UpdateCurrentFrame(TimeSpan timeSpan)
    {
        if (Scene == null) return;
        if (Scene.CurrentFrame != timeSpan)
        {
            int rate = Project?.GetFrameRate() ?? 30;
            Scene.CurrentFrame = timeSpan.RoundToRate(rate);
        }
    }

    public void Dispose()
    {
        Pause();
        _disposables.Dispose();
        PreviewInvalidated = null;
        Scene = null!;
    }
}
