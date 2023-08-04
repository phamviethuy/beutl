﻿using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

using Beutl.Animation;
using Beutl.Media;
using Beutl.Media.Decoding;
using Beutl.Media.Source;
using Beutl.Rendering;
using Beutl.Utilities;
using Beutl.Validation;

namespace Beutl.Graphics.Drawables;

public enum VideoPositionMode
{
    Manual,
    Automatic
}

public class VideoFrame : Drawable
{
    public static readonly CoreProperty<TimeSpan> OffsetPositionProperty;
    public static readonly CoreProperty<TimeSpan> PlaybackPositionProperty;
    public static readonly CoreProperty<VideoPositionMode> PositionModeProperty;
    public static readonly CoreProperty<FileInfo?> SourceFileProperty;
    private TimeSpan _offsetPosition;
    private TimeSpan _playbackPosition;
    private VideoPositionMode _positionMode;
    private FileInfo? _sourceFile;
    private MediaReader? _mediaReader;
    private TimeSpan _requestedPosition;
    private IImageSource? _previousBitmap;
    private double _previousFrame;

    static VideoFrame()
    {
        OffsetPositionProperty = ConfigureProperty<TimeSpan, VideoFrame>(nameof(OffsetPosition))
            .Accessor(o => o.OffsetPosition, (o, v) => o.OffsetPosition = v)
            .DefaultValue(TimeSpan.Zero)
            .Register();

        PlaybackPositionProperty = ConfigureProperty<TimeSpan, VideoFrame>(nameof(PlaybackPosition))
            .Accessor(o => o.PlaybackPosition, (o, v) => o.PlaybackPosition = v)
            .DefaultValue(TimeSpan.Zero)
            .Register();

        PositionModeProperty = ConfigureProperty<VideoPositionMode, VideoFrame>(nameof(PositionMode))
            .Accessor(o => o.PositionMode, (o, v) => o.PositionMode = v)
            .DefaultValue(VideoPositionMode.Automatic)
            .Register();

        SourceFileProperty = ConfigureProperty<FileInfo?, VideoFrame>(nameof(SourceFile))
            .Accessor(o => o.SourceFile, (o, v) => o.SourceFile = v)
#if DEBUG
            //.Validator(new FileInfoExtensionValidator()
            //{
            //    FileExtensions = new[] { "mp4" }
            //})
#else
#warning Todo: DecoderRegistryからファイル拡張子を取得してセットする。
#endif
            .Register();

        AffectsRender<VideoFrame>(
            OffsetPositionProperty,
            PlaybackPositionProperty,
            PositionModeProperty,
            SourceFileProperty);
    }

    public TimeSpan OffsetPosition
    {
        get => _offsetPosition;
        set => SetAndRaise(OffsetPositionProperty, ref _offsetPosition, value);
    }

    public TimeSpan PlaybackPosition
    {
        get => _playbackPosition;
        set => SetAndRaise(PlaybackPositionProperty, ref _playbackPosition, value);
    }

    public VideoPositionMode PositionMode
    {
        get => _positionMode;
        set => SetAndRaise(PositionModeProperty, ref _positionMode, value);
    }

    [NotAutoSerialized]
    public FileInfo? SourceFile
    {
        get => _sourceFile;
        set => SetAndRaise(SourceFileProperty, ref _sourceFile, value);
    }

    public override void ReadFromJson(JsonObject json)
    {
        base.ReadFromJson(json);
        if (json.TryGetPropertyValue("source-file", out JsonNode? fileNode)
            && fileNode is JsonValue fileValue
            && fileValue.TryGetValue(out string? fileStr)
            && File.Exists(fileStr))
        {
            SourceFile = new FileInfo(fileStr);
        }
    }

    public override void WriteToJson(JsonObject json)
    {
        base.WriteToJson(json);
        if (_sourceFile != null)
        {
            json["source-file"] = _sourceFile.FullName;
        }
    }

    public override void ApplyAnimations(IClock clock)
    {
        base.ApplyAnimations(clock);
        if (PositionMode == VideoPositionMode.Automatic)
        {
            _requestedPosition = clock.CurrentTime;
            _requestedPosition -= clock.BeginTime;
        }
    }

    protected override Size MeasureCore(Size availableSize)
    {
        if (_mediaReader?.IsDisposed == false)
        {
            return _mediaReader.VideoInfo.FrameSize.ToSize(1);
        }
        else
        {
            return Size.Empty;
        }
    }

    protected override void OnDraw(ICanvas canvas)
    {
        if (_mediaReader?.IsDisposed == false)
        {
            if (PositionMode == VideoPositionMode.Manual)
            {
                _requestedPosition = _playbackPosition;
            }

            TimeSpan pos = _requestedPosition + _offsetPosition;
            Rational rate = _mediaReader.VideoInfo.FrameRate;
            double frameNum = pos.TotalSeconds * (rate.Numerator / (double)rate.Denominator);

            if (_previousBitmap?.IsDisposed == false
                && MathUtilities.AreClose(frameNum, _previousFrame))
            {
                canvas.DrawImageSource(_previousBitmap, Foreground, null);
            }
            else if (_mediaReader.ReadVideo((int)frameNum, out IBitmap? bmp)
                && bmp?.IsDisposed == false)
            {
                _previousBitmap?.Dispose();
                _previousBitmap = new BitmapSource(Ref<IBitmap>.Create(bmp), "Temp");

                canvas.DrawImageSource(_previousBitmap, Foreground, null);

                _previousFrame = frameNum;
            }
        }
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (args.PropertyName is nameof(SourceFile))
        {
            _previousBitmap?.Dispose();
            _previousBitmap = null;
            _previousFrame = -1;
            _mediaReader?.Dispose();
            _mediaReader = null;

            TryOpenMediaFile();
        }
    }

    [MemberNotNullWhen(true, "_mediaReader")]
    private bool TryOpenMediaFile()
    {
        if (_sourceFile?.Exists == true)
        {
            try
            {
                if (_mediaReader?.IsDisposed != false)
                {
                    _mediaReader = MediaReader.Open(_sourceFile.FullName, new MediaOptions()
                    {
                        StreamsToLoad = MediaMode.Video
                    });

                    if (!_mediaReader.HasVideo)
                    {
                        _mediaReader.Dispose();
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }
}