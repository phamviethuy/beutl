﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using Beutl.Animation;
using Beutl.Commands;
using Beutl.Graphics.Transformation;
using Beutl.ProjectSystem;
using Beutl.ViewModels;

namespace Beutl.Views;

static file class CommandHelper
{
    public static IRecordableCommand? Compose(IRecordableCommand? first, IRecordableCommand? second)
    {
        if (second != null)
        {
            if (first != null)
            {
                return first.Append(second);
            }
            else
            {
                return second;
            }
        }
        else
        {
            return first;
        }
    }
}

public partial class EditView
{
    private sealed class KeyFrameState
    {
        public KeyFrameState(KeyFrame<float>? previous, KeyFrame<float>? next)
        {
            Previous = previous;
            Next = next;
            OldPreviousValue = previous?.Value ?? 0;
            OldNextValue = next?.Value ?? 0;
        }

        public KeyFrame<float>? Previous { get; }

        public KeyFrame<float>? Next { get; }

        public float OldPreviousValue { get; }

        public float OldNextValue { get; }

        public IRecordableCommand? CreateCommand()
        {
            return CommandHelper.Compose(
                Previous != null && Previous.Value != OldPreviousValue
                    ? new ChangePropertyCommand<float>(Previous, KeyFrame<float>.ValueProperty, Previous.Value, OldPreviousValue)
                    : null,
                Next != null && Next.Value != OldNextValue
                    ? new ChangePropertyCommand<float>(Next, KeyFrame<float>.ValueProperty, Next.Value, OldNextValue)
                    : null);
        }
    }

    private sealed class MouseControlState
    {
        public bool _imagePressed;
        public Point _scaledStartPosition;
        public Element? _mouseSelectedElement;
        public Graphics.Drawable? _mouseSelected;
        public TranslateTransform? _translateTransform;
        public Graphics.Point _oldTranslation;
        public KeyFrameState? _xKeyFrame;
        public KeyFrameState? _yKeyFrame;

        public required Image Image { get; init; }

        public required EditViewModel viewModel { get; init; }

        private static double Length(Point point)
        {
            return Math.Sqrt((point.X * point.X) + (point.Y * point.Y));
        }

        private static TranslateTransform? FindOrCreateTranslation(Graphics.Drawable drawable)
        {
            switch (drawable.Transform)
            {
                case TranslateTransform translateTransform:
                    return translateTransform;

                case TransformGroup transformGroup:
                    TranslateTransform? obj = transformGroup.Children.OfType<TranslateTransform>().FirstOrDefault();
                    if (obj == null)
                    {
                        obj = new TranslateTransform();
                        transformGroup.Children.BeginRecord<ITransform>()
                            .Insert(0, obj)
                            .ToCommand()
                            .DoAndRecord(CommandRecorder.Default);
                    }

                    return obj;
            }

            return null;
        }

        private KeyFrameState? FindKeyFramePairOrNull(CoreProperty<float> property)
        {
            int rate = viewModel.Scene.FindHierarchicalParent<Project>() is { } proj ? proj.GetFrameRate() : 30;
            TimeSpan globalkeyTime = viewModel.Scene.CurrentFrame;
            TimeSpan localKeyTime = _mouseSelectedElement != null ? globalkeyTime - _mouseSelectedElement.Start : globalkeyTime;

            if (_translateTransform!.Animations.FirstOrDefault(v => v.Property == property) is KeyFrameAnimation<float> animation)
            {
                TimeSpan keyTime = animation.UseGlobalClock ? globalkeyTime : localKeyTime;
                keyTime = keyTime.RoundToRate(rate);

                (IKeyFrame? prev, IKeyFrame? next) = animation.KeyFrames.GetPreviousAndNextKeyFrame(keyTime);

                if (next?.KeyTime == keyTime)
                    return new(next as KeyFrame<float>, null);

                return new(prev as KeyFrame<float>, next as KeyFrame<float>);
            }

            return default;
        }

        public void OnMoved(PointerEventArgs e)
        {
            if (_imagePressed && _mouseSelected != null)
            {
                PointerPoint pointerPoint = e.GetCurrentPoint(Image);
                Point imagePosition = pointerPoint.Position;
                double scaleX = Image.Bounds.Size.Width / viewModel.Scene.Width;
                Point scaledPosition = imagePosition / scaleX;
                Point delta = scaledPosition - _scaledStartPosition;
                if (_translateTransform == null && Length(delta) >= 1)
                {
                    _translateTransform = FindOrCreateTranslation(_mouseSelected);

                    // 最初の一回だけ、キーフレームを探す
                    if (_translateTransform != null)
                    {
                        _oldTranslation = new(_translateTransform.X, _translateTransform.Y);
                        _xKeyFrame = FindKeyFramePairOrNull(TranslateTransform.XProperty);
                        _yKeyFrame = FindKeyFramePairOrNull(TranslateTransform.YProperty);
                    }
                }

                if (_translateTransform != null)
                {
                    if (!SetKeyFrameValue(_xKeyFrame, (float)delta.X))
                    {
                        _translateTransform.X += (float)delta.X;
                    }

                    if (!SetKeyFrameValue(_yKeyFrame, (float)delta.Y))
                    {
                        _translateTransform.Y += (float)delta.Y;
                    }
                }

                _scaledStartPosition = scaledPosition;
            }
        }

        // keyframesが両方nullの場合、falseを返す
        private static bool SetKeyFrameValue(KeyFrameState? keyframes, float delta)
        {
            switch ((keyframes?.Previous, keyframes?.Next))
            {
                case (null, null):
                    return false;

                case ({ } prev, { } next):
                    prev.Value += delta;
                    next.Value += delta;
                    break;

                case ({ } prev, null):
                    prev.Value += delta;
                    break;

                case (null, { } next):
                    next.Value += delta;
                    break;
            }

            return true;
        }

        private IRecordableCommand? CreateTranslationCommand()
        {
            if (_translateTransform != null)
            {
                return CommandHelper.Compose(
                    _translateTransform.X != _oldTranslation.X
                        ? new ChangePropertyCommand<float>(_translateTransform, TranslateTransform.XProperty, _translateTransform.X, _oldTranslation.X)
                        : null,
                    _translateTransform.Y != _oldTranslation.Y
                        ? new ChangePropertyCommand<float>(_translateTransform, TranslateTransform.YProperty, _translateTransform.Y, _oldTranslation.Y)
                        : null);
            }

            return null;
        }

        public void OnReleased()
        {
            if (_imagePressed)
            {
                _imagePressed = false;

                IRecordableCommand? command = CommandHelper.Compose(
                    CreateTranslationCommand(),
                    CommandHelper.Compose(_xKeyFrame?.CreateCommand(), _yKeyFrame?.CreateCommand()));
                command?.DoAndRecord(CommandRecorder.Default);

                _mouseSelectedElement = null;
                _translateTransform = null;
                _mouseSelected = null;
                _xKeyFrame = default;
                _yKeyFrame = default;
            }
        }

        public void OnPressed(PointerPoint pointerPoint)
        {
            _imagePressed = true;
            Point imagePosition = pointerPoint.Position;
            double scaleX = Image.Bounds.Size.Width / viewModel.Scene.Width;
            _scaledStartPosition = imagePosition / scaleX;

            _mouseSelected = viewModel.Scene.Renderer.HitTest(new((float)_scaledStartPosition.X, (float)_scaledStartPosition.Y));

            if (_mouseSelected != null)
            {
                int zindex = (_mouseSelected as Graphics.DrawableDecorator)?.OriginalZIndex ?? _mouseSelected.ZIndex;
                Scene scene = viewModel.Scene;

                _mouseSelectedElement = scene.Children.FirstOrDefault(v =>
                    v.ZIndex == zindex
                    && v.Start <= scene.CurrentFrame
                    && scene.CurrentFrame < v.Range.End);

                if (_mouseSelectedElement != null)
                {
                    viewModel.SelectedObject.Value = _mouseSelectedElement;
                }
            }
        }
    }

    private MouseControlState? _mouseState;

    private void OnImagePointerMoved(object? sender, PointerEventArgs e)
    {
        _mouseState?.OnMoved(e);
    }

    private void OnImagePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _mouseState?.OnReleased();
        _mouseState = null;
    }

    private void OnImagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint pointerPoint = e.GetCurrentPoint(Image);
        if (pointerPoint.Properties.IsLeftButtonPressed && DataContext is EditViewModel viewModel)
        {
            _mouseState = new MouseControlState
            {
                Image = Image,
                viewModel = viewModel
            };

            _mouseState.OnPressed(pointerPoint);
        }
    }
}
